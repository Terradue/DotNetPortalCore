using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
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

    

    /// <summary>Represents a script-based processing service that is provided by an ASPX script and other resources on the portal host.</summary>
    /// <remarks>
    /// A script-based service, like other entity types is stored as a record in a database table, but requires more information in the <b>service root directory</b>, which is a dedicated directory accessible through the web server:
    /// <list type="bullet">
    ///     <item><b>%Service definition file</b>: a file named <i>service.xml</i> containing the definition of parameters.</item>
    ///     <item><b>%Service script</b>: an ASPX script file named <i>index.aspx</i> that instantiates the correct service and contains methods for parameter validity checks (optional) and a method for building a task.</item>
    ///     <item>Additional files (optional), such as additional XSL and Javascript.</item>
    /// </list>
    /// </remarks>
    /// \ingroup Service
    [EntityTable("scriptservice", EntityTableConfiguration.Custom)]
    public class ScriptBasedService : Service {

        private double minPriority = 0, maxPriority = 0;
        private bool serviceChecked, validService;

        private string fileRootDir;
        private string relativeUrl;
        private int defaultComputingResourceId, defaultSeriesId;
        
        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("root")]
        public string RootDirectory { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the file system root directory of the service.</summary>
        public override string FileRootDir {
            get {
                GetRootDirectories();
                return fileRootDir;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the root directory of the service for web access.</summary>
        public override string RelativeUrl {
            get {
                GetRootDirectories();
                return relativeUrl; 
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the root directory of the service for web access.</summary>
        public override string AbsoluteUrl {
            get {
                GetRootDirectories();
                return context.HostUrl + relativeUrl; 
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the root directory of the service for web access.</summary>
        public string UnprotectedAbsoluteUrl {
            get {
                GetRootDirectories();
                return context.BaseUrl + relativeUrl;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the root directory of the service for web access.</summary>
        public override string SchedulerRelativeUrl {
            get { 
                GetRootDirectories();
                return base.SchedulerRelativeUrl; 
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the version of the service.xml file.</summary>
        public string DefinitionVersion { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the service definition document has a dynamic extension.</summary>
        public bool HasDynamicDefinition { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the URL from where to obtain the dynamic parameter set.</summary>
        public string DynamicDefinitionUrl { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the dynamic service definition document.</summary>
        /// <remarks>The dynamic service definition document is an optional extension of the base service definition document. The dynamic service definition document may define service parameters that depend on the value of a parameter defined in the base service definition document.</remarks>
        public XmlDocument DynamicDefinitionDocument { get; private set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets <c>&lt;params&gt;</c> element of the dynamic service definition document.</summary>
        /// <remarks>The dynamic service definition document is an optional extension of the base service definition document. The dynamic service definition document may define service parameters that depend on the value of a parameter defined in the base service definition document.</remarks>
        protected XmlElement DynamicParamsElement { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new ScriptBasedService instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public ScriptBasedService(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the service information from the database.</summary>
        public override void Load() {
            base.Load();
            
            GetRootDirectories();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads service the root directories for file system and web access.</summary>
        protected void GetRootDirectories() {
            if (fileRootDir != null || relativeUrl != null) return;
            if (RootDirectory != null) {
                if (RootDirectory.StartsWith("$(SERVICEROOT)")) {
                    fileRootDir = (RootDirectory.Replace("$(SERVICEROOT)", context.ServiceFileRoot)).Replace('/', System.IO.Path.DirectorySeparatorChar);
                    relativeUrl = RootDirectory.Replace("$(SERVICEROOT)", context.ServiceWebRoot);
                } else {
                    fileRootDir = (context.SiteRootFolder + "/" + RootDirectory).Replace('/', System.IO.Path.DirectorySeparatorChar);
                    relativeUrl = RootDirectory;
                }
                relativeUrl = Regex.Replace(relativeUrl, "/+$", String.Empty);
            }
            if (IconUrl != null) IconUrl = IconUrl.Replace("$(SERVICEROOT)", context.ServiceWebRoot);
            if (ViewUrl != null) ViewUrl = ViewUrl.Replace("$(SERVICEROOT)", context.ServiceWebRoot);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the service definition XML document (service.xml).</summary>
        /// <returns>the service definition XML document</summary>
        public XmlDocument GetDefinitionDocument() {

            GetRootDirectories();
            string message;
            try {
                return context.LoadXmlFile(fileRootDir + "/service.xml");
            } catch (Exception e) {
                message = String.Format("Service definition document could not be loaded: {0}", e.Message);
            }
            context.ReturnError(message);

            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the service can be used by the user and prepares the service parameter validity checks.</summary>
        /// <returns><c>true</c> if the service can be used and the validity checks can be performed</returns>
        public override bool Check(int userId) {
            if (serviceChecked) return validService;
            
            minPriority = 0;
            maxPriority = 1;

            UserId = userId;
            validService = true;
            
            if (!CanView) {
                if (UserId == context.UserId) {
                    validService = (context.UserLevel >= UserLevel.Administrator);
                    if (validService) context.AddWarning("You are not authorized to use this service", "notAllowedService"); // !!! use exception and set class also for other entities
                    else context.ReturnError("You are not authorized to use this service", "notAllowedService"); // !!! use exception and set class also for other entities
                } else {
                    context.ReturnError("The owner is not authorized to use this service", "notAllowedService"); // !!! use exception and set class also for other entities
                }
            }

            serviceChecked = true;
            return validService;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override ServiceParameterSet GetParameters() {
            XmlServiceParameterSet result;
            
            XmlDocument definitionDocument = GetDefinitionDocument();
            
            if (definitionDocument == null) throw new InvalidOperationException("Service definition document not found");
            
            DefinitionVersion = (definitionDocument.DocumentElement.HasAttribute("version") ? definitionDocument.DocumentElement.Attributes["version"].Value : "1.0");
            if (definitionDocument.DocumentElement.HasAttribute("dynamic")) {
                HasDynamicDefinition = true;
                string value = definitionDocument.DocumentElement.Attributes["dynamic"].Value;
                if (value == "true") {
                    DynamicDefinitionUrl = String.Format("{0}/?_request=dyndef&_pass=allow", UnprotectedAbsoluteUrl);
                } else {
                    if (value.StartsWith("/")) DynamicDefinitionUrl = String.Format("{0}{1}", context.BaseUrl, value);
                    else if (value.StartsWith("?")) DynamicDefinitionUrl = String.Format("{0}{1}", UnprotectedAbsoluteUrl, value);
                    else DynamicDefinitionUrl = value;
                }
            }
                

            result = new XmlServiceParameterSet(context, this);
            GetConstants(definitionDocument);
            result.LoadFromXml(definitionDocument);
            
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Obtains information on the service constants defined in the service definition document.</summary>
        /// <param name="constants">The request parameter collection holding the service constants.</param>
        protected void GetConstants(XmlDocument definitionDocument) {
            XmlElement elem;
            Constants = new ServiceParameterArray();
            
            foreach (XmlNode xmlNode in definitionDocument.DocumentElement.ChildNodes) {
                if ((elem = xmlNode as XmlElement) == null) continue;
                
                if (elem.Name == "const") {
                    string elemName = (elem.HasAttribute("name") ? elem.Attributes["name"].Value : null);
                    string elemSource = (elem.HasAttribute("source") ? elem.Attributes["source"].Value : null);
                    bool configurable = (elem.HasAttribute("configurable") && elem.Attributes["configurable"].Value == "true");
                    string elemValue = (elem.HasAttribute("value") ? elem.Attributes["value"].Value : null);
    
                    IDbConnection dbConnection = context.GetDbConnection();
                    IDataReader reader;
                    switch (elemSource) {
                        case "Task.MaxPriority" :
                            if (!Double.TryParse(elemValue, out maxPriority)) maxPriority = 1;
                            if (configurable) {
                                reader = context.GetQueryResult(GetConfigQuery(elemSource), dbConnection);
                                maxPriority = GetConfigDoubleValue(reader, true, maxPriority); // !!! MaxPriority
                                reader.Close();
                            }
                            break;
                        default :
                            if (!configurable || Constants == null || elemSource == null) break;
                            ServiceParameter constant = new ServiceParameter(null, elem);
                            Constants.Add(constant);
                            //context.AddInfo("constant " + constant.Name + " " + constant.Type + " " + constant.Value);
                            if (constant == null) break;
                            reader = context.GetQueryResult(GetConfigQuery(elemName), dbConnection);
                            switch (constant.Type) {
                                case "bool":
                                    bool boolValue, defaultBoolValue = constant.AsBoolean;
                                    boolValue = GetConfigBooleanValue(reader, true, defaultBoolValue);
                                    if (boolValue != defaultBoolValue) constant.Value = boolValue.ToString().ToLower();
                                    break;
                                case "int":
                                    int intValue, defaultIntValue = constant.AsInteger;
                                    intValue = GetConfigIntegerValue(reader, true, defaultIntValue);
                                    if (intValue != defaultIntValue) constant.Value = intValue.ToString(); 
                                    break;
                                case "float":
                                case "double":
                                    double doubleValue, defaultDoubleValue = constant.AsInteger;
                                    doubleValue = GetConfigDoubleValue(reader, true, defaultDoubleValue);
                                    if (doubleValue != defaultDoubleValue) constant.Value = doubleValue.ToString(); 
                                    break;
                                case "date":
                                case "datetime":
                                    DateTime dateTimeValue, defaultDateTimeValue = constant.AsDateTime;
                                    dateTimeValue = GetConfigDateTimeValue(reader, true, defaultDateTimeValue);
                                    if (dateTimeValue != defaultDateTimeValue) constant.Value = dateTimeValue.ToString(dateTimeValue.ToString(constant.Type == "date" ? @"yyyy\-MM\-dd" : @"yyyy\-MM\-dd\THH\:mm\:ss")); 
                                    break;
                                default :
                                    string value, defaultValue = constant.Value;
                                    value = GetConfigValue(reader, true, defaultValue);
                                    if (value != defaultValue) constant.Value = value; 
                                    break;
                            }
                            reader.Close();
                            break;
                    }
                    context.CloseDbConnection(dbConnection);
                }
            }
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the XML element containing the parameter information for the service parameter with the specified name from the service definition file. </summary>
        /// <param name="name">the name of the service parameter</param>
        /// <returns>the <c>&lt;param&gt;</c> element in the service definition file defining the service parameter.</returns>
        public XmlElement GetParameterElement(string name) { // !!! context.UserId or this.userId ???, this is called for the configurable parameters with no user id set
            if (!CanView && context.UserLevel < UserLevel.Administrator) context.ReturnError(new UnauthorizedAccessException("You are not authorized to use this service"), "notAllowedService"); // !!! use exception and set class also for other entities
            
            XmlDocument definitionDoc = GetDefinitionDocument();
            
            XmlElement paramElem = definitionDoc.SelectSingleNode("//definition/params/param[@name='" + name + "']") as XmlElement;
            
            if (paramElem == null) {
                paramElem = definitionDoc.SelectSingleNode("//definition/const[@name='" + name + "' or @source='" + name + "']") as XmlElement;
            }
            
            if (paramElem == null) context.ReturnError(new ArgumentException("Unknown service parameter"), null);
            
            return paramElem;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a collection of configurable parameters and constants.</summary>
        public override void GetConfigurableParameters(RequestParameterCollection parameters) {
            XmlDocument definitionDocument = GetDefinitionDocument();
            
/*            XmlElement elem;

            foreach (XmlNode xmlNode in DefinitionDocument.DocumentElement.ChildNodes) {
                if ((elem = xmlNode as XmlElement) == null) continue;
                
                if (elem.Name == "params") {
                    ParamsElement = elem;
                    continue;
                }
                if (elem.Name == "const") {
                    if (!elem.HasAttribute("configurable") || elem.Attributes["configurable"].Value != "true") continue;
                    parameters.GetParameter(context, this, elem, false, false);
                }
            }

            if (ParamsElement == null) {
                foreach (XmlNode xmlNode in DefinitionDocument.DocumentElement.ChildNodes) {
                    if ((elem = xmlNode as XmlElement) != null && elem.Name == "params") {
                        ParamsElement = elem;
                        break;
                    }
                }
                if (ParamsElement == null) return;
            }

            foreach (XmlNode xmlNode in ParamsElement.ChildNodes) {
                if ((elem = xmlNode as XmlElement) == null || elem.HasAttribute("source") || !elem.HasAttribute("scope") || elem.HasAttribute("type") && elem.Attributes["type"].Value == "textfile") continue;
                parameters.GetParameter(context, this, elem, false, false);
            }
            return;*/
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void BuildTask(Task task) {
            // TODO-NEW-SERVICE script.BuildTask (OnBuildTask)
            
/*            string url = String.Format("{0}/?_user={1}{2}&_request=create{3}{4}{5}{6}&_format=xml", 
                    UnprotectedAbsoluteUrl,
                    UserId,
                    task.Status == ProcessingStatus.None ? String.Empty : "&_task=" + Identifier,
                    task.Status != ProcessingStatus.None || task.SchedulerId == 0 ? String.Empty : "&_scheduler=" + task.SchedulerId,
                    task.AllowEmpty ? "&_empty=true" : String.Empty,
                    context.DebugLevel > 0 ? "&_debug=" + context.DebugLevel : String.Empty
            );
            
            string test = url + " === ";
            //throw new Exception(test);
            
            HttpWebRequest request;
            
            try {
                string dataStr = String.Empty;
                if (task.SchedulerId == 0) {
                    for (int i = 0; i < task.RequestParameters.Count; i++) {
                        RequestParameter param = task.RequestParameters[i];
                        string value = param.Value;
                        if (param.Value == null) {
                            switch (param.Source) {
                                case "computingResource" :
                                case "Task.ComputingResource" :
                                case "computingElement" :
                                case "Task.ComputingElement" :
                                    if (task.ComputingResourceId != 0) value = task.ComputingResourceId.ToString();
                                    break;
                                case "publishServer" :
                                case "Task.PublishServer" :
                                    if (task.ComputingResourceId != 0) value = task.PublishServerId.ToString();
                                    break;
                                case "priority" :
                                case "Task.Priority" :
                                    if (task.Priority != 0) value = task.Priority.ToString();
                                    break;
                                case "compress" :
                                case "Task.Compression" :
                                    value = task.Compression;
                                    break;
                                case "register" :
                                case "Task.RegisterOutput" :
                                    if (task.AutoRegister) value = "true";
                                    break;
                            }
                        }
                        if (i != 0) dataStr += "&";
                        dataStr += (Uri.EscapeDataString(param.Name) + "=" + (value == null ? String.Empty : Uri.EscapeDataString(value)));
                    }
                }
    
                if (context.DebugLevel >= 3) {
                    context.AddDebug(3, "Task creation URL: " + url);
                    if (task.SchedulerId == 0) context.AddDebug(3, "Task parameters: " + dataStr);
                }
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] data = encoding.GetBytes(dataStr);
                //webContext.XmlWriter.WriteStartElement("container");
                //webContext.XmlWriter.WriteElementString("URL", url);
                //webContext.XmlWriter.WriteElementString("DATA", dataStr);
                request = WebRequest.Create(url) as HttpWebRequest;
                if (request == null) throw new Exception("Invalid URL type for task creation");
                request.Method = "POST";
                request.AllowAutoRedirect = false;
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
            } catch (Exception e) {
                //context.WriteError(e.Message);
                context.AddError(e.Message);
                throw e;
            }
            
            context.AddDebug(3, "Task cration request sent");
            
            XmlDocument taskDoc = new XmlDocument();
    
            try {
                bool redirect = false;
                int requestCount = 0;
                string redirectUrl = null;
                
                while (requestCount == 0 || redirect && requestCount <= 10) {
                    
                    HttpWebRequest request2 = (requestCount == 0 ? request : WebRequest.Create(redirectUrl) as HttpWebRequest);
                    if (requestCount != 0) context.AddDebug(3, "Redirection URL: " + redirectUrl);
                    
                    if (requestCount != 0) {
                        request2.Method = "GET";
                        request2.AllowAutoRedirect = false;
                    }
                
                    using (HttpWebResponse response = (HttpWebResponse)request2.GetResponse()) {
                        // Get response stream.
                        //response = (HttpWebResponse)request.GetResponse();
            
            //            Console.WriteLine(response.ContentType);
                        redirect = (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found || response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.SeeOther || response.StatusCode == HttpStatusCode.RedirectMethod); 
                        
                        if (redirect) {
                            redirectUrl = response.Headers["Location"];
                            if (redirectUrl == null || redirectUrl == String.Empty) {
                                redirect = false;
                            } else {
                                Match match = Regex.Match(url, @"^([a-z0-9]+://)([^/\?]+)([^\?]*)");
                                if (match.Success) {
                                    string protocol = match.Groups[1].Value;
                                    string hostname = match.Groups[2].Value;
                                    string script = match.Groups[3].Value;
                                    if (script == String.Empty) script = "/";
                                    if (redirectUrl.StartsWith("/")) redirectUrl = protocol + hostname + redirectUrl;
                                    //else if (!redirectUrl.Matches("^[a-z0-9]+://") redirectUrl = protocol + hostname + script
                                } else {
                                    redirect = false;
                                }
                            }
                        }
                        if (!redirect) taskDoc.Load(response.GetResponseStream());
                        response.Close();
                    }
                    requestCount++;
                }
                
                    
                //// Get response stream.
                //response = (HttpWebResponse)request.GetResponse();
                ////context.WriteInfo("RESPONSE = " + (response != null));
                ////taskDoc = new XmlDocument();
                //taskDoc.Load(response.GetResponseStream());
                ////context.WriteInfo("GOT DOC");
                //response.Close();

            } catch (WebException we) {
                //webContext.XmlWriter.WriteElementString("ERROR", url + " " +  we.Message + " " + we.StackTrace);
                //context.WriteError("RESPONSE ERROR " + we.Message + " " + we.StackTrace);
                if (we.Response != null) context.AddError(StringUtils.GetXmlFromStream(we.Response.GetResponseStream(), true));
                //context.WriteError(StringUtils.GetXmlFromStream(we.Response.GetResponseStream(), true));
                throw we;
            }
            
            // In case of messages, log the messages
            string errorMessage = null;
            
            //context.WriteInfo("taskDoc null? " + (taskDoc == null));
            XmlNodeList nodes = taskDoc.SelectNodes("/content/message");
            for (int i = 0; i < nodes.Count; i++) {
                XmlElement elem = nodes[i] as XmlElement;
                if (elem == null || !elem.HasAttribute("type")) continue;
                switch (elem.Attributes["type"].Value) {
                    case "error" :
                        errorMessage = elem.InnerText;
                        task.Invalidate(errorMessage);
                        if (!context.IsInteractive) context.AddError(errorMessage);
                        break;
                    case "warning" :
                        if (!context.IsInteractive) context.AddWarning(elem.InnerText);
                        break;
                    case "info" :
                        if (!context.IsInteractive) context.AddInfo(elem.InnerText);
                        break;
                    case "debug" :
                        if (context.IsInteractive || !elem.HasAttribute("level")) continue;
                        int level = 0;
                        Int32.TryParse(elem.Attributes["level"].Value, out level);  
                        if (level > context.DebugLevel) continue;
                        context.AddDebug(level, elem.InnerText);
                        break;
                }
            }
            
            XmlNode uidNode;
            bool taskCreated = (uidNode = taskDoc.SelectSingleNode("/content/task/@uid")) != null;
            if (taskCreated) Identifier = uidNode.InnerXml;

            // Get errors for parameters
            for (int i = 0; i < task.RequestParameters.Count; i++) task.RequestParameters[i].AllValid = true;
            
            if (!taskCreated && task.Error) {
                nodes = taskDoc.SelectNodes("/content/singleItem/item/*[@valid='false']");
                
                //webContext.XmlWriter.WriteElementString("DOC", taskDoc.OuterXml);
                
                foreach (XmlNode node in nodes) {
                    XmlElement elem = node as XmlElement;
                    if (elem == null) continue;
                    RequestParameter param = task.RequestParameters[node.Name];
                    test += "-ivp:" + param.Name;
                    param.Invalidate(elem.HasAttribute("message") ? elem.Attributes["message"].Value : null);
                }
                //throw new Exception(test);
                
                if (nodes.Count == 0 || context.IsInteractive) context.ReturnError(errorMessage);
                else return;
            }
            
            // In case of success, reload the task data
            try {
                task.Load();
                task.LoadJobs();
            } catch (Exception e) {
                task.Invalidate(null);
                if (context.IsInteractive) throw e;
            }*/
        }
        
    }

}

