using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;
using Terradue.Util;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    
    
    /// <summary>Represents an application that is able to process all requests required for a Web Processing Service (Open Geospatial Consortium).</summary>
    public class WpsApplication : Application {
        
        private bool errorOpen = false;
        
        //public const string Wps100NamespaceUri = ;
        
        private XmlDocument configurationDocument;
        private XmlDocument requestDocument;
        private XmlElement requestElement;
        private XmlNamespaceManager namespaceManager;
        private TaskTemplate taskTemplate;
        private Task task;
        private List<string> unavailableServices;
        
        private bool storeExecuteResponse = false, outputAsReference = false, lineage = false, status = false;
        private string lineageXml = null;
        private string xslTransformation;
        private RequestParameter fileListParameter;
        
        public int SeriesId { get; set; }
        
        public const string NAMESPACE_URI_WPS = "http://www.opengis.net/wps/1.0.0";
        public const string NAMESPACE_URI_OWS = "http://www.opengis.net/ows/1.1";
        public const string NAMESPACE_URI_XLINK = "http://www.w3.org/1999/xlink";
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public string OutputFormatParameterName { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public bool AbbreviatedAbstract { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public bool UseEPSG4326BoundingBox { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public bool AtomMetadata { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool ForceSslForExecute { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the WPS request type application type (GET, POST or SOAP).</summary>
        public WpsRequestType RequestType { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines what HTTP status code has to be returned in case of an error (400 for client error, 500 for server error).</summary>
        protected bool ServerResponsibility { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether an error has occurred during the processing of the request.</summary>
        public bool Error { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public string XslDirectory { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new WpsApplication instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        */
        public WpsApplication(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new WpsApplication instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created WpsApplication object</returns>
        */
        public static new WpsApplication GetInstance(IfyContext context) {
            return new WpsApplication(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new WpsApplication instance representing the application with the specified ID.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the application ID</param>
        /// <returns>the created Application object</returns>
        */
        public static new WpsApplication FromId(IfyContext context, int id) {
            WpsApplication result = new WpsApplication(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new WpsApplication instance representing the application with the specified unique name.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="name">the unique application name</param>
        /// <returns>the created Application object</returns>
        */
        public static WpsApplication FromName(IfyContext context, string name) {
            return FromIdentifier(context, name);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static new WpsApplication FromIdentifier(IfyContext context, string identifier) {
            WpsApplication result = new WpsApplication(context);
            result.Identifier = identifier;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        private void GetUnavailableServices() {
            unavailableServices = new List<string>();
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(
                    String.Format("SELECT DISTINCT name FROM service AS t{0} WHERE NOT t.available{1};", 
                            SeriesId == 0 ? String.Empty : " LEFT JOIN service_series AS t1 ON t.id=t1.id_service AND t1.id_series=" + SeriesId,
                            SeriesId == 0 ? String.Empty : " OR t1.id_service IS NULL"
                    ),
                    dbConnection
            );
            while (reader.Read()) unavailableServices.Add(reader.GetString(0));
            context.CloseQueryResult(reader, dbConnection);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Loads the information of the task with the specified UID if it was created using the WPS application.</summary>
        /// <param name="identifier">the task UID</param>
        public void LoadTask(string identifier) {
            string sql = String.Format("SELECT t.id_task, t.template_name, t.store, t.status, t.lineage_xml, t.ref_output, xsl_name FROM wpstask AS t INNER JOIN task AS t1 ON t.id_task=t1.id WHERE t.id_application={0} AND t1.identifier={1}", Id, StringUtils.EscapeSql(identifier));
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            if (!reader.Read()) {
                reader.Close();
                throw new WpsNoApplicableCodeException(String.Format("The requested task does not exist or was not created with this Web Processing Service"));
            }
            int taskId = reader.GetInt32(0);
            string templateName = reader.GetString(1);
            storeExecuteResponse = context.GetBooleanValue(reader, 2);
            status = context.GetBooleanValue(reader, 3);
            lineageXml = context.GetValue(reader, 4);
            lineage = (lineageXml != null);
            outputAsReference = context.GetBooleanValue(reader, 5);
            xslTransformation = context.GetValue(reader, 6);
            context.CloseQueryResult(reader, dbConnection);
            try {
                task = Task.FromId(context, taskId);
                taskTemplate = ReadConfigurationFile(templateName);
            } catch (Exception e) {
                throw new WpsNoApplicableCodeException(String.Format("Could not retrieve task information: {0}", e.Message));
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates and submits a task based on the received input parameters.</summary>
        /// <remarks>The method creates the task using a sessionless request to the task creation interface and, in case of success, submits the task. The exceptions that may occur during the task creation and submission are translated into a WPS ExceptionReport considering the error origin, i.e. client (e.g. invalid parameters) or server (e.g. network problem).</remarks>
        /// <param name="template">the task template on which the new task is based</param>
        protected virtual void SubmitTask() {
/*
            //task = context.NewTask(taskTemplate.Service);
            task = Task.ForService(context, taskTemplate.Service);
            GetParameters(task.RequestParameters, taskTemplate, false);
            if (task.RequestParameters == null) return;

            string[] names, values;
            
            switch (RequestType) {
                case WpsRequestType.Get :
                    string dataInputsParam = System.Web.HttpContext.Current.Request.QueryString["DataInputs"]; 
                    string[] dataInputs = (dataInputsParam == null ? null : dataInputsParam.Split(';'));
                    if (dataInputs == null) {
                        throw new WpsMissingParameterValueException("DataInputs");
                    }
                    names = new string[dataInputs.Length];
                    values = new string[dataInputs.Length];

                    for (int i = 0; i < dataInputs.Length; i++) {
                        string dataInput = dataInputs[i];
                        int pos = dataInput.IndexOf('=');
                        if (pos == -1) continue;
                        
                        names[i] = Uri.UnescapeDataString(dataInput.Substring(0, pos));
                        string value = dataInput.Substring(pos + 1);
                        if ((pos = value.IndexOf('@')) != -1) value = value.Substring(0, pos);
                        values[i] = Uri.UnescapeDataString(value);
                    }
                    
                    break;

                case WpsRequestType.Post :
                case WpsRequestType.Soap :
                    XmlElement dataInputsElement = requestElement.SelectSingleNode("wps:DataInputs", namespaceManager) as XmlElement;
                    
                    if (dataInputsElement == null) {
                        throw new WpsMissingParameterValueException("DataInputs");
                    }
                    
                    XmlNodeList identifierNodes = dataInputsElement.SelectNodes("wps:Input/ows:Identifier", namespaceManager);

                    names = new string[identifierNodes.Count];
                    values = new string[identifierNodes.Count];

                    for (int i = 0; i < identifierNodes.Count; i++) {
                        XmlElement inputElem = identifierNodes[i].ParentNode as XmlElement;
                        names[i] = identifierNodes[i].InnerText;
                        XmlNode dataNode = inputElem.SelectSingleNode("wps:Data/wps:LiteralData", namespaceManager);
                        if (dataNode != null) values[i] = dataNode.InnerText;
                    }
                    break;
                    
                default :
                    names = new string[0];
                    values = new string[0];
                    break;
            }

            for (int i = 0; i < names.Length; i++) {
                //AddException("TEST", null, names[i] + " = " + values[i]);
            }

            for (int i = 0; i < task.RequestParameters.Count; i++) {
                RequestParameter param = task.RequestParameters[i];
                if (taskTemplate.FixedParameters.Contains(param.Name)) { 
                    param.Value = taskTemplate.FixedParameters[param.Name].Value;
                    //param.Level = RequestParameterLevel.Custom;
                } else {
                    bool found = false;
                    switch (RequestType) {
                        case WpsRequestType.Get :
                            for (int j = 0; j < names.Length; j++) {
                                if (names[j] != param.Name) continue;
                                
                                found = true;
                                
                                if (param.Value != null && !param.Multiple && param.Source != "dataset") { // TODO: remove special handling of "dataset" (files) parameter
                                    throw new WpsNoApplicableCodeException(String.Format("Multiple values for parameter \"{0}\"", param.Name));
                                } else {
                                    if (values[j] != null) {
                                        if (param.Multiple) {
                                            if (param.Value == null) param.Value = String.Empty; else param.Value += param.Separator;
                                        }
                                        param.Value += values[j];
                                    }
                                }
                            }
                            break;
                            
                        case WpsRequestType.Post :
                        case WpsRequestType.Soap :
                            XmlElement dataInputsElement = requestElement.SelectSingleNode("wps:DataInputs", namespaceManager) as XmlElement;
                            
                            if (dataInputsElement == null) {
                                throw new WpsMissingParameterValueException("DataInputs");
                            }
                            
                            XmlNodeList identifierNodes = dataInputsElement.SelectNodes("wps:Input/ows:Identifier[.='" + param.Name + "']", namespaceManager);
        
                            found = identifierNodes.Count != 0;
                            if (!found) break;
                            
                            if (identifierNodes.Count > 1 && !param.Multiple && param.Source != "dataset") { // TODO: remove special handling of "dataset" (files) parameter
                                throw new WpsNoApplicableCodeException(String.Format("Multiple values for parameter \"{0}\"", param.Name));
                            }
                            
                            param.Values = new string[identifierNodes.Count];
                            for (int j = 0; j < identifierNodes.Count; j++) {
                                XmlElement inputElem = identifierNodes[j].ParentNode as XmlElement;
                                if (param.Source == "dataset" && taskTemplate.AllowInputFilesParameter) {
                                    XmlElement referenceElem = inputElem.SelectSingleNode("wps:Reference", namespaceManager) as XmlElement;
                                    if (referenceElem == null || !referenceElem.HasAttribute("href", NAMESPACE_URI_XLINK)) {
                                        throw new WpsNoApplicableCodeException(String.Format("Input for parameter \"{0}\" must be a reference", param.Name));
                                    }
                                    if (referenceElem.HasAttribute("mimeType", NAMESPACE_URI_XLINK) && referenceElem.GetAttribute("mimeType", NAMESPACE_URI_XLINK) != (AtomMetadata ? "application/atom+xml" : "application/rdf+xml")) {
                                        throw new WpsNoApplicableCodeException(String.Format("MIME type for parameter \"{0}\" not supported", param.Name));
                                    }
                                    if (referenceElem.HasAttribute("encoding", NAMESPACE_URI_XLINK) && referenceElem.GetAttribute("encoding", NAMESPACE_URI_XLINK) != "utf-8") {
                                        throw new WpsNoApplicableCodeException(String.Format("Encoding type for parameter \"{0}\" not supported", param.Name));
                                    }

                                    if (param.Value == null) param.Value = String.Empty; else param.Value += param.Separator;
                                    string url = referenceElem.GetAttribute("href", NAMESPACE_URI_XLINK);
                                    //if (AtomMetadata) url = Regex.Replace(url, @"/atom(/)?(\?)?", "/rdf$1$2"); // !!! change this (transformation of ATOM URL in RDF URL
                                    param.Value += url;
                                    
                                } else if (param.Type == "bbox" && UseEPSG4326BoundingBox) {
                                    XmlNode dataNode = inputElem.SelectSingleNode("wps:Data/wps:BoundingBoxData", namespaceManager);
                                    XmlElement dataElem = dataNode as XmlElement;
                                    if (dataElem == null) break;
                                    
                                    if (dataElem.HasAttribute("crs") && dataElem.Attributes["crs"].Value != "EPSG:4326") {
                                        throw new WpsNoApplicableCodeException(String.Format("CRS for parameter \"{0}\" not supported", param.Name));
                                    }
                                    if (dataElem.HasAttribute("dimensions") && dataElem.Attributes["dimensions"].Value != "2") {
                                        throw new WpsNoApplicableCodeException(String.Format("Number of dimensions for parameter \"{0}\" must be 2", param.Name));
                                    }
                                    Regex regex = new Regex(@"^-?([0-9]+|([0-9]+)?\.[0-9]+) -?([0-9]+|([0-9]+)?\.[0-9]+)$");
                                    string coord = String.Empty;
                                    XmlNode coordNode;
                                    coordNode = dataNode.SelectSingleNode("ows:LowerCorner", namespaceManager);
                                    if (coordNode == null) {
                                        throw new WpsNoApplicableCodeException(String.Format("Missing lower/left coordinates for parameter \"{0}\"", param.Name));
                                    }
                                    if (!regex.Match(coordNode.InnerText).Success) {
                                        throw new WpsNoApplicableCodeException(String.Format("Invalid lower/left coordinates for parameter \"{0}\"", param.Name));
                                    }
                                    coord += coordNode.InnerText.Replace(' ', ','); 
                                    coordNode = dataNode.SelectSingleNode("ows:UpperCorner", namespaceManager);
                                    if (coordNode == null) {
                                        throw new WpsNoApplicableCodeException(String.Format("Missing upper/right coordinates for parameter \"{0}\"", param.Name));
                                    }
                                    if (!regex.Match(coordNode.InnerText).Success) {
                                        throw new WpsNoApplicableCodeException(String.Format("Invalid upper/right coordinates for parameter \"{0}\"", param.Name));
                                    }
                                    coord += "," + coordNode.InnerText.Replace(' ', ',');
                                    param.Value = coord;
                                    
                                } else {
                                    XmlNode dataNode = inputElem.SelectSingleNode("wps:Data/wps:LiteralData", namespaceManager);
                                    if (dataNode != null) {
                                        if (param.Value == null || j == 0) param.Value = String.Empty; else param.Value += param.Separator;
                                        param.Value += dataNode.InnerText;
                                    }
                                }
                            }
                            break;
                            
                    }
                    if (param.Value == null && taskTemplate.DefaultParameters.Contains(param.Name)) {
                        param.Value = taskTemplate.DefaultParameters[param.Name].Value;
                        found = (param.Value != null);
                    }
                    if (!found && param.Value == null && !param.Optional) {
                        throw new WpsNoApplicableCodeException(String.Format("Missing value for parameter \"{0}\"", param.Name));
                    } else {
                        //param.Check();
                        //if (!param.AllValid) {
                        //    AddException("NoApplicableCode", param.Name, param.Message == null ? String.Format("Invalid value for parameter \"{0}\"", param.Name) : param.Message + " (" + param.Value + ")");
                        //    return;
                        //}
                    }
                }
            }

            if (fileListParameter != null) {
                xslTransformation = fileListParameter.Value;
                if (xslTransformation != null) {
                    fileListParameter.Check();
                    if (!fileListParameter.AllValid) {
                        throw new WpsNoApplicableCodeException(OutputFormatParameterName, fileListParameter.Message == null ? "Invalid value for " + OutputFormatParameterName + " parameter" : fileListParameter.Message);
                    }
                }
            }
          
            if (Error) return;
            
            // A fatal error (exception) during the task creation indicates a server error
            ServerResponsibility = true;
            
            string errorMessage = null;

            try {
                task.AllowEmpty = true; // tasks with empty input will be considered as failed
                task.Build();
            } catch (Exception e) {
                errorMessage = String.Format("Could not create task: {0}", e.Message);
            }

            // An invalid parameter indicates a client error
            if (task.Error) {
                for (int i = 0; i < task.RequestParameters.Count; i++) {
                    RequestParameter param = task.RequestParameters[i];
                    ServerResponsibility = false;
                    //test += ", (2)" + param.Name + " " + param.Level + " " + (param.AllValid ? "valid" : "invalid") + param.Message;            
                    if (!param.AllValid || param.Message != null && param.Message.Contains("specified")) { // !!! change this
                        throw new WpsNoApplicableCodeException(String.Format("Invalid input: \"{0}\"{1}", param.Name, param.Message == null ? String.Empty : ": " + param.Message));
                    }
                }
            }
            //throw new Exception(test);
            
            if (!errorOpen && errorMessage != null) {
                throw new WpsNoApplicableCodeException(errorMessage);
            }
            
            if (Error) return;

            // From here on, all errors are to be considered server-side errors
            ServerResponsibility = true;

            try {
                task.Submit();
            } catch (Exception e) {
                throw new WpsNoApplicableCodeException(String.Format("Could not submit task: {0}", e.Message));
            }*/
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Translates the type of the specified service parameter into a W3C type name and reference URL.</summary>
        /*!
        /// <param name="param">the service parameter</param>
        /// <param name="w3cType">contains the W3C type name (output parameter)</param>
        /// <param name="w3cReference">contains the W3C reference URL (output parameter)</param>
        */
        protected virtual void GetW3cTypeReference(RequestParameter param, out string w3cType, out string w3cReference) {    
            w3cReference = null;
            switch (param.Type) {
                case "bool" :
                    w3cType = "boolean";
                    break;
                case "int" :
                    w3cType = "integer";
                    break;
                case "long" :
                    w3cType = "long";
                    break;
                case "float" :
                    w3cType = "float";
                    break;
                case "double" :
                    w3cType = "double";
                    break;
                case "date" :
                case "datetime" :
                case "startdate" :
                case "enddate" :
                    w3cType = (param.Type == "date" ? "date" : "dateTime");
                    break;
                default :
                    w3cType = "string";
                    break;
            }
            if (w3cReference == null) w3cReference = "http://www.w3.org/TR/xmlschema-2/#" + w3cType;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        protected bool LoadConfigurationDocument() { 
            configurationDocument = new XmlDocument();
            //XmlElement configElem = null;
            //bool success = false;
            try {
                configurationDocument.Load(ConfigurationFilename);
            } catch (Exception e) {
                ServerResponsibility = true;
                throw new WpsNoApplicableCodeException("Could not load process configurations" + (context.UserLevel == UserLevel.Administrator ? ": " + e.Message : String.Empty));
                return false;
            }
            return true;
        }
            
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the service parameters for the specified task template.</summary>
        /*!
        /// <param name="parameters">the service parameter collection to be filled</param>
        /// <param name="template">the task template referring to the service to be used </param>
        /// <param name="assignValueSets">determines whether value sets are instantiated for the parameters that maintain value sets </param>
        */
        protected void GetParameters(RequestParameterCollection parameters, TaskTemplate template, bool assignValueSets) {
            try {
                template.Service.GetParameters();
                if (template.AllowInputFilesParameter) {
                    for (int i = 0; i < parameters.Count; i++) {
                        RequestParameter param = parameters[i];
                        if (param.Source == "dataset") param.Optional = !template.ForceInputFilesParameter;
                    }
                }

                /*if (template.Service.HasDynamicDefinition) {
                    bool isSwitchValueSet = false;
                    
                    for (int i = 0; i < parameters.Count; i++) {
                        RequestParameter param = parameters[i];
                        if (!param.IsSwitch) continue; 
                        
                        if (template.FixedParameters.Contains(param.Name)) param.Value = template.FixedParameters[param.Name].Value; 
                        if (param.Value != null) isSwitchValueSet = true;
                    }
                    
                    if (isSwitchValueSet) template.Service.GetDynamicParameters(parameters, false, assignValueSets, true);
                    
                }*/ // TODO-NEW-SERVICE
                
                if (OutputFormatParameterName != null) {
                    FileListParameterValueSet formatValueSet = new FileListParameterValueSet(context, template.Service, OutputFormatParameterName, String.Format("{0}/*.xsl", XslDirectory));
                
                    if (formatValueSet.GetValues().Length != 0) {
                        fileListParameter = new RequestParameter(context, template.Service, OutputFormatParameterName, "select", "XSL Transformation for Output", null);
                        fileListParameter.Optional = true;
                        fileListParameter.ValueSet = formatValueSet;
                        parameters.Add(fileListParameter);
                    }
                }
                
            } catch (Exception e) {
                ServerResponsibility = true;
                throw new WpsNoApplicableCodeException(e.Message);
                return;
            }
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    
    
    
    /// <summary>Different request types for Web Processing Services.</summary>
    public enum WpsRequestType {
        
        /// <summary>GET request</summary>
        Get, 
        
        /// <summary>POST request</summary>
        Post,
        
        /// <summary>SOAP request</summary>
        Soap
        
    }



    public class WpsNoApplicableCodeException : Exception {

        public string Locator { get; set; }

        public WpsNoApplicableCodeException(string message) : base(message) {}

        public WpsNoApplicableCodeException(string locator, string message) : this(message) {
            this.Locator = locator;
        }


    }


    public class WpsMissingParameterValueException : Exception {

        public WpsMissingParameterValueException(string message) : base(message) {}

    }


}

