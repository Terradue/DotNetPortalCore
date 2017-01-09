using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;
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

    
    public class ServiceParameterArray : IEnumerable<ServiceParameter> {

        private Dictionary<string, ServiceParameter> dict = new Dictionary<string, ServiceParameter>();
        private ServiceParameter[] items = new ServiceParameter[0];
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the request parameter with the specified name.</summary>
        public ServiceParameter this[string name] {
            get { if (dict.ContainsKey(name)) return dict[name]; else return ServiceParameter.Empty; } 
            set { if (dict.ContainsKey(name)) dict[name] = value; } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the request parameter at the specified position in the list.</summary>
        public ServiceParameter this[int index] { 
            get { return items[index]; } 
            set { items[index] = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of request parameters in the collection.</summary>
        public int Count {
            get { return items.Length; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether parameter validity errors are collected and reported together.</summary>
        /// <summary>If this property is set to <c>true</c>, only a summary exception is thrown after the validity check of all parameters, otherwise an invalid parameter value causes an immediate exception upon detection.</summary>
        public bool CollectValidityErrors { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public bool ContainsParameter(string name) {
            return dict.ContainsKey(name);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public void Add(ServiceParameter param) {
            if (param == null) return;
            if (param.Name == null) throw new ArgumentNullException("Parameter name cannot be empty");
            
            if (dict.ContainsKey(param.Name)) throw new Exception(String.Format("Parameter already defined", param.Name));
            dict.Add(param.Name, param);
            Array.Resize(ref items, items.Length + 1);
            items[items.Length - 1] = param;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void Check() {
            foreach (ServiceParameter param in this) param.Check();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        IEnumerator<ServiceParameter> System.Collections.Generic.IEnumerable<ServiceParameter>.GetEnumerator() {
            return dict.Values.GetEnumerator();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        IEnumerator IEnumerable.GetEnumerator() {
            return dict.Values.GetEnumerator();
        }

    }

    
    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    public class ServiceParameterSet : ServiceParameterArray {
        
        //private double minPriority = 0, maxPriority = 0; // TODO set these values
        
        //---------------------------------------------------------------------------------------------------------------------

        public IfyContext Context { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public int ServiceId { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual Service Service { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) an array of the allowed values for the task priority.</summary>
        public double[] AllowedPriorities { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the list of the input parameter sets.</summary>
        /// <remarks>Every service parameter set contains at least one input parameter set. Usually there is only one.</remarks>
        public List<ProcessingInputSet> Inputs { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the parameter that is used for naming the task or scheduler.</summary>
        public ServiceProcessNameParameter NameParameter { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the parameter that is used for specifying the computing resource used by the task or scheduler.</summary>
        public ServiceComputingResourceParameter ComputingResourceParameter { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the parameter that is used for specifying the priority given to the task or scheduler.</summary>
        public ServicePriorityParameter PriorityParameter { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the list of the output parameter sets.</summary>
        /// <remarks>Every service parameter set contains at least one output parameter set. Usually there is only one.</remarks>
        public List<ProcessingOutputSet> Outputs { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether empty parameters for which a value is required are considered valid.</summary>
        /// <remarks>This setting is useful for eliminating unwanted error messages, e.g. when the task definition interface is shown and parameters are still unset.</remarks>
        public bool AllowEmptyParameters { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines (protected) whether the parameter set is ready for the creation of a task.</summary>
        /// <remarks>This is the case in most situations after the parameter definition is obtained, however, in some situations (e.g. service with dynamic parameters) the parameter set is ready only after a second step.</remarks>
        public bool IsReady { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool IsValid { 
            get {
                foreach (ServiceParameter param in this) if (!param.IsValid) return false;
                return true;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceParameterSet(IfyContext context, Service service) {
            this.Context = context;
            this.Service = service;
            this.Inputs = new List<ProcessingInputSet>();
            this.Outputs = new List<ProcessingOutputSet>();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected void GetDefinedProcessingParameters(XmlElement element) {
            foreach (XmlNode node in element.ChildNodes) {
                XmlElement subElement = node as XmlElement;
                if (subElement == null) continue;

                switch (subElement.Name) {
                    case "computingResource" :
                        if (ComputingResourceParameter == null) ComputingResourceParameter = new ServiceComputingResourceParameter(this, subElement);
                        else throw new Exception("Multiple computing resources defined");
                        break;
                    case "priority" :
                        if (PriorityParameter == null) PriorityParameter = new ServicePriorityParameter(this, subElement);
                        else throw new Exception("Multiple priorities defined");
                        break;
                    case "parameter" :
                        Add(new ServiceParameter(this, subElement));
                        break;
                    default :
                        // TODO throw exception
                        break;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void AddValue(string name, string value) {
            if (!ContainsParameter(name)) throw new ServiceParameterNotFoundException(String.Format("Parameter \"{0}\" does not exist for {1}", name, Service.Name));
            this[name].AddValue(value);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void GetInputData() {
            foreach (ProcessingInputSet inputSet in Inputs) inputSet.GetInputData();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void GetValuesFromTask(Task task) {
            IDbConnection dbConnection = Context.GetDbConnection();
            IDataReader reader = Context.GetQueryResult(String.Format("SELECT name, value FROM taskparam WHERE id_task={0} AND id_job IS NULL AND metadata IS NOT NULL;", task.Id), dbConnection);
            while (reader.Read()) {
                string name = reader.GetString(0);
                if (ContainsParameter(name)) this[name].Value = reader.GetString(1);
            }
            Context.CloseQueryResult(reader, dbConnection);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void GetValuesFromScheduler(Scheduler scheduler) {
            IDbConnection dbConnection = Context.GetDbConnection();
            IDataReader reader = Context.GetQueryResult(String.Format("SELECT name, value FROM schedulerparam WHERE id_scheduler={0};", scheduler.Id), dbConnection);
            while (reader.Read()) {
                string name = reader.GetString(0);
                if (ContainsParameter(name)) this[name].Value = reader.GetString(1);
            }
            Context.CloseQueryResult(reader, dbConnection);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void ReplaceWithRequestValues() {
            if (HttpContext.Current != null) {
                foreach (ServiceParameter param in this) {
                    param.Value = HttpContext.Current.Request[param.Name];
                }
                return;
            }
            ComputingResourceParameter.Value = "11";
            Inputs[0].CollectionParameter.Value = "MER_RR__2P";
            this["cellsize"].Value = "2";
            this["lcw"].Value = "false";
            this["startdate"].Value = "2004-10-23";
            this["stopdate"].Value = "2004-10-27";
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void WriteFields(XmlWriter output) {
            foreach (ServiceParameter param in this) {
                if (param.Ignore) continue;
                
                param.WriteField(output);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the XML element <i>item</i> containing the service derivate attribute and parameter values within the single item output.  </summary>
        public virtual void WriteValues(XmlWriter output) {
            
            foreach (ServiceParameter param in this) {
                
                // Skip unused parameters or empty parameters if they are valid
                if (param.Ignore || param.IsValid && param.Values.Length == 0) continue;
                
                param.WriteValue(output);
            }
        }
        
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    public class XmlServiceParameterSet : ServiceParameterSet {
        
        private ScriptBasedService service; 

        //---------------------------------------------------------------------------------------------------------------------

        public new virtual ScriptBasedService Service {
            get {
                return service;
            }
            protected set {
                this.service = value;
                base.Service = service;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public XmlServiceParameterSet(IfyContext context, ScriptBasedService service) : base(context, service) {
            this.Service = service;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void LoadFromXml(XmlDocument definitionDocument) {
            
            XmlElement paramsElement = null, element;

            foreach (XmlNode node in definitionDocument.DocumentElement.ChildNodes) {
                if ((element = node as XmlElement) != null && element.Name == "params") {
                    paramsElement = element;
                    break;
                }
            }
            
            if (paramsElement == null) return;

            if (String.Compare(Service.DefinitionVersion, "2.0") >= 0) { // new service.xml format

                foreach (XmlNode node in paramsElement.ChildNodes) {
                    element = node as XmlElement;
                    if (element == null) continue;
                    
                    switch (element.Name) {
                        case "input" :
                            //ProcessingInputSet input = new ProcessingInputSet(this, element);
                            break;
                        case "processing" :
                            GetDefinedProcessingParameters(element);
                            break;
                        case "output" :
                            //ProcessingOutputSet output = new ProcessingOutputSet(this, element);
                            break;
                    }
                }
                
            } else { // old service.xml format
                Inputs.Add(new ProcessingInputSet(this));
                Outputs.Add(new ProcessingOutputSet(this));
                
                foreach (XmlNode node in paramsElement.ChildNodes) {
                    
                    XmlElement subElement = node as XmlElement;
                    if (subElement == null) continue;

                    string source = (subElement.HasAttribute("source") ? subElement.Attributes["source"].Value : null);
                    string type = (subElement.HasAttribute("type") ? subElement.Attributes["type"].Value : null);
                    
                    switch (source) {
                        case "series" :
                        case "Task.Series" :
                            Inputs[0].CollectionParameter = new ServiceInputCollectionParameter(Inputs[0], subElement);
                            Add(Inputs[0].CollectionParameter);
                            break;
                        case "dataset" :
                        case "Task.InputFiles" :
                            Inputs[0].ProductParameter = new ServiceInputProductParameter(Inputs[0], subElement);
                            Add(Inputs[0].ProductParameter);
                            break;
                        case "computingResource" :
                        case "Task.ComputingResource" :
                        case "computingElement" :
                        case "Task.ComputingElement" :
                            ComputingResourceParameter = new ServiceComputingResourceParameter(this, subElement);
                            Add(ComputingResourceParameter);
                            break;
                        case "priority" :
                        case "Task.Priority" :
                            PriorityParameter = new ServicePriorityParameter(this, subElement);
                            Add(PriorityParameter);
                            break;
                        case "publishServer" :
                        case "Task.PublishServer" :
                            Outputs[0].LocationParameter = new ServiceOutputLocationParameter(Outputs[0], subElement);
                            Add(Outputs[0].LocationParameter);
                            break;
                        case "compress" :
                        case "Task.Compression" :
                            Outputs[0].CompressionParameter = new ServiceOutputCompressionParameter(Outputs[0], subElement);
                            Add(Outputs[0].CompressionParameter);
                            break;
                        /*case "register" :
                        case "Task.RegisterOutput" :
                            CheckAutoRegister(param);
                            break;*/
                        default :
                            if (type == "caption") {
                                NameParameter = new ServiceProcessNameParameter(this, subElement);
                                Add(NameParameter);
                            } else {
                                ServiceParameter param = new ServiceParameter(this, subElement);
                                Add(param);
                                if (param.SearchExtension != null) Inputs[0].SearchParameters.Add(param);
                            }
                            break;
                    }
                    
                }
    
            }

        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Check() {
            foreach (ServiceParameter param in this) {
                param.Check();
                // Execute the custom callback function (if implemented) in order to verify that the task parameter is correct
                if (Service.OnCheckParameter != null) {
                    RequestParameter requestParameter = null; // RequestParameter.FromServiceParameter(param);
                    Service.OnCheckParameter(requestParameter);
                    //param.Update(requestParameter);   // TODO RequestParameter <-> ServiceParameter
                }
                
                //if (!param.AllValid) Error = true;
            }

        }
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ServiceParameterNotFoundException : Exception {
        public ServiceParameterNotFoundException(string message) : base(message) {}
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class InvalidServiceParameterException : Exception {
        public InvalidServiceParameterException(string message) : base(message) {}
    }

}

