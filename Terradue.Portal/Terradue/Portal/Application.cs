using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Util;

namespace Terradue.Portal {

    /// <summary>Represents an external application of an Ify installation, such as a web service.</summary>
    [EntityTable("application", EntityTableConfiguration.Full)]
    public class Application : Entity {
        
        private bool configurationRead;
        
        //---------------------------------------------------------------------------------------------------------------------

        // Indicates whether the application is available.
        [EntityDataField("available")]
        public bool Available { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        // Gets the location of the application configuration file.
        [EntityDataField("config_file")]
        public string ConfigurationFilename { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        // Gets the log level of the application.
        public int LogLevel { get; protected set; } 
        
        //---------------------------------------------------------------------------------------------------------------------

        // Gets the location of the application log file.
        public string LogFilename { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        // Gets the debug level of the application.
        public int DebugLevel { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        // Gets the location of the application debug log file.
        public string DebugFilename { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Application instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        */
        public Application(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Application instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created Application object</returns>
        */
        public static new Application GetInstance(IfyContext context) {
            EntityType entityType = EntityType.GetEntityType(typeof(Application));
            return (Application)entityType.GetEntityInstance(context); 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Application instance representing the application with the specified ID.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the application ID</param>
        /// <returns>the created Application object</returns>
        */
        public static Application FromId(IfyContext context, int id) {
            Application result = GetInstance(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Application instance representing the application with the specified unique name.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="name">the unique application name</param>
        /// <returns>the created Application object</returns>
        */
        public static Application FromIdentifier(IfyContext context, string identifier) {
            Application result = GetInstance(context);
            result.Identifier = identifier;
            result.Load();
            return result;
        }
        
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Application instance representing the application with the specified ID.</summary>
        public override void Load() {
            base.Load();
            
            if (!Available) context.ReturnError("The application is temporarily unavailable");
            
            ReadConfigurationFile(null);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Reads a the configuration file of the application.</summary>
        public TaskTemplate ReadConfigurationFile(string templateName) {
            if (ConfigurationFilename == null || !File.Exists(ConfigurationFilename)) context.ReturnError("Application configuration file not defined or not found");

            XmlDocument configDoc = new XmlDocument();
            XmlElement configElem = null;
            //bool success = false;
            try {
                configDoc.Load(ConfigurationFilename);
            } catch (Exception e) {
                context.ReturnError("Could not load process configurations" + (context.UserLevel == UserLevel.Administrator ? ": " + e.Message : String.Empty));
            }
            
            if (!configurationRead) {
                if ((configElem = configDoc.SelectSingleNode("/application/configuration") as XmlElement) != null) {
                    string filename;
                    int level = 0;
                    DateTime now = DateTime.UtcNow;
                    for (int i = 0; i < configElem.ChildNodes.Count; i++) {
                        XmlElement elem = configElem.ChildNodes[i] as XmlElement;
                        if (elem == null) continue;
                        switch (elem.Name) {
                            case "log" :
                                if (elem.HasAttribute("level")) Int32.TryParse(elem.Attributes["level"].Value, out level);
                                context.LogLevel = level;
                                if (level > 0 && elem.HasAttribute("file")) {
                                    filename = elem.Attributes["file"].Value;
                                    context.LogFilename = (filename == null ? null : filename.Replace("$(DATE)", now.ToString("yyyyMMdd")));
                                }
                                break;
                            case "debug" :
                                if (elem.HasAttribute("level")) Int32.TryParse(elem.Attributes["level"].Value, out level);
                                context.DebugLevel = level;
                                if (level > 0 && elem.HasAttribute("file")) {
                                    filename = elem.Attributes["file"].Value;
                                    context.DebugFilename = (filename == null ? null : filename.Replace("$(DATE)", now.ToString("yyyyMMdd")));
                                }
                                break;
                            case "alert" :
                                context.SetAlertInformation(Identifier, elem);
                                break;
                        }
                    }
                    //context.RefreshLoggingInformation();
                    configurationRead = true;

                } else if (context.UserLevel == UserLevel.Administrator) {
                    throw new Exception("Invalid application configuration");
                }
            }
                    
            if (templateName == null) return null;
            
            XmlElement templateElem = configDoc.SelectSingleNode("/application/template[@name='" + templateName.Replace("'", "''") + "']") as XmlElement;
            if (templateElem == null) context.ReturnError("The specified task template was not found");
            
            return TaskTemplate.FromXml(context, templateElem);
        }

    }
    
    
    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    
    
    /// <summary>Represents a task template configuration used by an external application, such as a web service.</summary>
    public class TaskTemplate {
        
        private Service service;
        protected IfyContext context;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the unique identifier of the task template.</summary>
        public string Identifier { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the version of the task template.</summary>
        public string Version { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the task template is available and accepts requests.</summary>
        public bool Available { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the name of the service on which the task template is based.</summary>
        public string ServiceIdentifier { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the service on which the task template is based.</summary>
        public Service Service {
            get {
                if (service == null && ServiceIdentifier != null) service = Service.FromIdentifier(context, ServiceIdentifier);
                return service;
            }
            protected set { service = value; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the task template caption or title.</summary>
        public string Name { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the task template description or abstract.</summary>
        public string Description { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the maximum priority for tasks defined by the task template.</summary>
        public double MaxPriority { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the submission of tasks defined by the template can be postponed if it is not possible immediately.</summary>
        /*!
            If the value is set to <i>true</i>, task submissions fail in case of insufficient credits or Computing Elelement unavailability.
        */
        public bool AllowPending { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool AllowInputFilesParameter { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool ForceInputFilesParameter { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the default parameter values for tasks defined by the template.</summary>
        public RequestParameterCollection DefaultParameters { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the unchangeable parameter values for tasks defined by the template.</summary>
        public RequestParameterCollection FixedParameters { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected TaskTemplate(IfyContext context) {
            this.context = context;
            DefaultParameters = new RequestParameterCollection();
            FixedParameters = new RequestParameterCollection();
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected void LoadFromXml(XmlElement templateElem) {
            AllowPending = true;
            if (!templateElem.HasAttribute("name")) context.ReturnError("Template without name found in configuration file");
            Identifier = templateElem.Attributes["name"].Value;
            Version = (templateElem.HasAttribute("version") ? templateElem.Attributes["version"].Value : "1.0.0"); 
            Available = templateElem.HasAttribute("available") && (templateElem.Attributes["available"].Value == "true" || templateElem.Attributes["available"].Value == "yes"); 
            ServiceIdentifier = (templateElem.HasAttribute("service") ? templateElem.Attributes["service"].Value : null);
            if (templateElem.HasAttribute("files")) {
                ForceInputFilesParameter = (templateElem.Attributes["files"].Value == "force");
                AllowInputFilesParameter = ForceInputFilesParameter || (templateElem.Attributes["files"].Value == "allow");
            }
        
            for (int i = 0; i < templateElem.ChildNodes.Count; i++) {
                XmlElement elem = templateElem.ChildNodes[i] as XmlElement;
                if (elem == null) continue;
                switch (elem.Name) {
                    case "caption" :
                    case "title" :
                        Name = elem.InnerXml;
                        break;
                    case "description" :
                    case "abstract" :
                        Description = elem.InnerXml;
                        break;
                    case "maxPriority" :
                        //Description = elem.InnerXml;
                        break;
                    case "allowPending" :
                        AllowPending = (elem.InnerXml == "true" || elem.InnerXml == "yes");
                        break;
                    case "default" :
                        for (int j = 0; j < elem.ChildNodes.Count; j++) {
                            XmlElement paramElem = elem.ChildNodes[j] as XmlElement;
                            if (paramElem == null) continue;
                            DefaultParameters.Add(new RequestParameter(context, null, paramElem.Name, null, null, paramElem.InnerXml));
                            if (paramElem == null) continue;
                        }
                        break;
                    case "fixed" :
                        for (int j = 0; j < elem.ChildNodes.Count; j++) {
                            XmlElement paramElem = elem.ChildNodes[j] as XmlElement;
                            if (paramElem == null) continue;
                            FixedParameters.Add(new RequestParameter(context, null, paramElem.Name, null, null, paramElem.InnerXml));
                            if (paramElem == null) continue;
                        }
                        break;
                }
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public void LoadNewFromXml(XmlElement templateElem) {
            Name = null;
            Description = null;
            // maxPriority !!!
            DefaultParameters.Clear();
            FixedParameters.Clear();
            LoadFromXml(templateElem);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public static TaskTemplate FromXml(IfyContext context, XmlElement templateElem) {
            TaskTemplate result = new TaskTemplate(context);
            result.LoadFromXml(templateElem);
            return result;
        }
    }

}

