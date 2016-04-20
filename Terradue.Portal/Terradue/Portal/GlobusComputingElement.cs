using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
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

    

    /// <summary>Represents a Globus computing element that is accessed through an LGE interface.</summary>
    /// <remarks>
    ///     <p>These interfaces are:</p>
    ///     <list type="bullet">
    ///         <item>the LGE web service <b>wsServer</b> for task and job operations, and</item>
    ///         <item><b>XML files</b> accessed through HTTP for task and job status information.</item>
    ///     </list>
    /// </remarks>
    [EntityTable("ce", EntityTableConfiguration.Custom)]
    [EntityReferenceTable("lge", LGE_TABLE)]
    public class GlobusComputingElement : ComputingResource {
        
        private const int LGE_TABLE = 1;
        
        private List<ComputingGridDirectory> workingDirs;
        private List<ComputingGridDirectory> resultDirs;
        private string workingDir;
        private string resultDir;
        private wsServerClient gridEngineWebService;
        /*private string taskStatusUrl;
        private string jobStatusUrl;
        private string gridEngineConfigFile;*/

        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool CanMonitorStatus {
            get { return StatusMethod != GlobusStatusMethod.Unknown && StatusMethod != GlobusStatusMethod.NoRefresh; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates that the status of a task and all its jobs are received with one request at task level.</summary>
        public override bool ProvidesFullTaskStatus {
            get { return true; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets a collection containing the task parameters, i.e. the parameters that were used to define the task.</summary>
        protected ExecutionParameterSet ExecutionParameters { get; set; } // the final parameters that are saved to the database

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the CE port of the Globus computing element.</summary>
        [EntityDataField("ce_port")]
        public int Port { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the GSI port of the Globus computing element.</summary>
        [EntityDataField("gsi_port")]
        public int GsiPort { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the job manager of the Globus computing element.</summary>
        [EntityDataField("job_manager")]
        public string JobManager { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the flags of the Globus computing element.</summary>
        [EntityDataField("flags")]
        public string Flags { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the grid type of the Globus computing element.</summary>
        [EntityDataField("grid_type")]
        public string GridType { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the job queue of the Globus computing element.</summary>
        [EntityDataField("job_queue")]
        public string JobQueue { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the method for requesting the computing resource status information.</summary>
        [EntityDataField("status_method")]
        public int StatusMethod { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the job queue of the computing resource.</summary>
        [EntityDataField("status_url")]
        public string StatusUrl { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the access point of the Grid engine web service (wsServer).</summary>
        [EntityForeignField("ws_url", LGE_TABLE)]
        public string GridEngineAccessPoint { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the address or hostname of the MyProxy server.</summary>
        [EntityForeignField("myproxy_address", LGE_TABLE)]
        public string MyProxyAddress { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the method for requesting the task and job status information.</summary>
        [EntityForeignField("status_method", LGE_TABLE)]
        public int TaskStatusMethod { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets an instance of the client proxy class for the the Grid engine web service (wsServer).</summary>
        public wsServerClient GridEngineWebService {
            get { 
                if (gridEngineWebService == null) gridEngineWebService = new wsServerClient(GridEngineAccessPoint);
                return gridEngineWebService;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the URL of the task details XML file created by the Grid engine.</summary>
        [EntityForeignField("task_status_url", LGE_TABLE)]
        public string TaskStatusUrl { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the URL of the job details XML file created by the Grid engine.</summary>
        [EntityForeignField("job_status_url", LGE_TABLE)]
        public string JobStatusUrl { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets a random available working directory on the Globus computing element.</summary>
        public string WorkingDir {
            get { 
                if (workingDir == null) workingDir = GetRandomDir(workingDirs);
                return workingDir; 
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets a random available result directory on the Globus computing element.</summary>
        public string ResultDir {
            get { 
                if (resultDir == null) resultDir = GetRandomDir(resultDirs);
                return resultDir;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("id_lge")]
        public int LightGridEngineId { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityForeignField("conf_file", LGE_TABLE)]
        public string GridEngineConfigFile { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new GlobusComputingElement instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public GlobusComputingElement(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a new GlobusComputingElement instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created GlobusComputingElement object</returns>
        public static new GlobusComputingElement GetInstance(IfyContext context) {
            return new GlobusComputingElement(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the Globus computing element information from the database.</summary>
        /// <param name="condition">SQL conditional expression without WHERE keyword</param>
        public override void Load() {
            base.Load();
            
            // Set status URL to default values if unset
            if (TaskStatusUrl == null) {
                TaskStatusUrl = GridEngineAccessPoint;
                if (TaskStatusUrl != null) {
                    Match match = Regex.Match(TaskStatusUrl, @"^[a-z]+\://[^/]+");
                    if (match.Success) TaskStatusUrl = match.Value + "/gridify-logs/$(SESSION)/$(SESSION).xml";
                    else TaskStatusUrl = String.Empty;
                }
            }
            if (JobStatusUrl == null) {
                JobStatusUrl = GridEngineAccessPoint;
                if (JobStatusUrl != null) {
                    Match match = Regex.Match(JobStatusUrl, @"^[a-z]+\://[^/]+");
                    if (match.Success) JobStatusUrl = match.Value + "/gridify-logs/$(SESSION)/$(JOB)/$(JOB).xml";
                    else JobStatusUrl = null;
                }
            }
            
            // Get working and result directory lists
            workingDirs = new List<ComputingGridDirectory>();
            resultDirs = new List<ComputingGridDirectory>();

            string sql = "SELECT available, path, dir_type FROM cedir WHERE id_ce=" + Id + " AND dir_type IN ('W', 'R');";
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                ComputingGridDirectory cedir = new ComputingGridDirectory(this, context.GetBooleanValue(reader, 0), context.GetValue(reader, 1));
                switch (reader.GetChar(2)) {
                    case 'W':
                        workingDirs.Add(cedir);
                        break;
                    case 'R':
                        resultDirs.Add(cedir);
                        break;
                }
            }
            context.CloseQueryResult(reader, dbConnection);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Calculates the status of a Globus computing element.</summary>
        /// <param name="refresh">determines whether the is refreshed by a new request.</param>
        public override void GetStatus() {
            base.GetStatus();
            
            if (Refreshed) return;
            
            switch (StatusMethod) {
                case GlobusStatusMethod.Ganglia :
                    GetStatusFromGangliaUrl();
                    break;
                case GlobusStatusMethod.GlobusMds :
                    GetStatusFromGlobusMds();
                    break;
            }

            if (Refreshed) StoreState();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the status of a Globus computing element.</summary>
        // /// <param name="refresh">determines whether the is refreshed by a new request.</param>
        protected void GetStatusFromGangliaUrl() {
            if (StatusUrl == null || JobQueue == null) return;
            
            XmlDocument statusDoc = null;
            try {
                statusDoc = context.LoadXmlFile(StatusUrl);
            } catch (Exception e) {
                context.AddDebug(1, String.Format("Could not get computing resource status from {0}: {1}", StatusUrl, e.Message));
                return;
            }

            XmlNamespaceManager nsm = new XmlNamespaceManager(statusDoc.NameTable);
            nsm.AddNamespace("ns", "http://mds.globus.org/batchproviders/2004/09");
            
            XmlElement queueElem = statusDoc.SelectSingleNode("//ns:Queue[@name='" + JobQueue.Replace("'", "''") + "']", nsm) as XmlElement;

            if (queueElem == null) return;
            
            foreach (XmlNode xmlNode in queueElem.ChildNodes) {
                XmlElement elem = xmlNode as XmlElement;
                if (elem == null) continue;
                
                int intValue = 0;
                
                switch (elem.Name) {
                    case "totalnodes" :
                        Int32.TryParse(elem.InnerXml, out intValue);
                        TotalCapacity = intValue;
                        break;
                    case "freenodes" :
                        Int32.TryParse(elem.InnerXml, out intValue);
                        FreeCapacity = intValue;
                        Refreshed = true;
                        break;
                }
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected void GetStatusFromGlobusMds() {
            if (StatusUrl == null || JobQueue == null || JobQueue.Contains("'")) return;
            try {
                System.Diagnostics.Process process = context.GetShellCommandProcess("/usr/local/globus/bin/wsrf-query", String.Format("-s {0} -a -z none", StatusUrl));
                XmlDocument doc = new XmlDocument();
                doc.Load(process.StandardOutput);
                process.StandardOutput.Close();
                XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);
                nsm.AddNamespace("glue", "http://mds.globus.org/glue/ce/1.1");
                XmlElement elem = doc.SelectSingleNode(String.Format("//glue:GLUECE/glue:ComputingElement[@glue:Name='{0}']", JobQueue.Replace("'", "''")), nsm) as XmlElement;
                if (elem != null) {
                    int total, used;
                    XmlAttribute attr;
                    attr = elem.SelectSingleNode("glue:Info/@glue:TotalCPUs", nsm) as XmlAttribute;
                    if (attr == null || !Int32.TryParse(attr.Value, out total)) total = -1;
                    attr = elem.SelectSingleNode("glue:State/@glue:RunningJobs", nsm) as XmlAttribute;
                    if (attr == null || !Int32.TryParse(attr.Value, out used)) used = -1;  
                    Refreshed = (total != -1 && used != -1);
                    if (Refreshed) {
                        TotalCapacity = total;
                        FreeCapacity = total - used;
                    }
                }

            } catch (Exception e) {
                context.AddError(e.Message);
            }
            return;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets a random available directory from a directory list.</summary>
        /// <param name="dirs">the directory list from which to choose</param>
        /// <returns>the path of the random directory</returns>
        protected string GetRandomDir(List<ComputingGridDirectory> dirs) {
            if (dirs == null) return null;
            
            int count = 0;
            for (int i = 0; i < dirs.Count; i++) if (dirs[i].Available) count++;
            if (count == 0) return null;
            Random random = new Random(DateTime.Now.Millisecond);
            int availableIndex = random.Next(count);
            int index = 0;
            for (int i = 0; i < dirs.Count; i++) {
                if (dirs[i].Available) {
                    if (index == availableIndex) return dirs[i].Path;
                    else index++;
                }
            }
            return null;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a representation of the specified task on the Grid engine.</summary>
        /// <param name="task">the task to be created</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method calls the <c>createTask</c> method of the LGE's <i>wsServer</i> SOAP web service.</remarks>
        public override bool CreateTask(Task task) {
            // Check task weight
            context.GetUserProxyInformation(task.OwnerId);
            //context.AddDebug(2, "Proxy information checked");
            
            // Add task parameters
            ExecutionParameterSet parameters = new ExecutionParameterSet();
            foreach (ExecutionParameter parameter in task.ExecutionParameters) parameters.Add(parameter.Name, parameter.Value);

            // Add generic task parameters
            parameters.AddSubmissionParameter("SUID", task.Identifier);
            parameters.AddSubmissionParameter("lgeContactPoint", GridEngineAccessPoint);
            parameters.AddSubmissionParameter("gridType", "Local");
            parameters.AddSubmissionParameter("CE", Hostname);
            parameters.AddSubmissionParameter("CEPORT", Port.ToString());
            parameters.AddSubmissionParameter("GSIPORT", ":" + GsiPort.ToString());
            parameters.AddSubmissionParameter("WDIR", WorkingDir);
            parameters.AddSubmissionParameter("RDIR", ResultDir);
            parameters.AddSubmissionParameter("jobManager", JobManager);
            parameters.AddSubmissionParameter("SITECONFIG", GridEngineConfigFile);
            parameters.AddSubmissionParameter("jobqueue", JobQueue);
            parameters.AddSubmissionParameter("JOBLIST", task.GetJobList());
            parameters.AddSubmissionParameter("VO", context.GetConfigValue("VirtualOrganization")); // !!!
            parameters.AddSubmissionParameter("MIDDLEWARE", GridType);

            context.AddDebug(1, "Ready to create task session on Grid engine (" + GridEngineAccessPoint + ")");
            
            // Create session on Grid engine and save session ID
            try {
                task.RemoteIdentifier = GridEngineWebService.createTask(
                        context.ProxyUsername,
                        context.ProxyPassword,
                        MyProxyAddress,
                        task.Name,
                        parameters.ToArray()
                );
                
                context.AddDebug(1, "Task created on Grid engine, session ID is " + task.RemoteIdentifier);
                
                context.AddDebug(1, "Ready to create jobs on Grid engine");
                
                bool result = true;
                for (int i = 0; i < task.Jobs.Count && result; i++) result &= task.Jobs[i].CreateRemote();
                
//                SaveGridEngineParameters(task);
//                context.AddDebug(2, "Specific parameters for Grid engine saved");
    
                return result;
    
            } catch (WebException e) {
                string errorMessage = "Could not create Grid engine session for task " + task.Identifier + " on Grid engine";
                
                string errorDetail = wsServerClient.GetErrorDetail(e);
                
                if (errorDetail != null) {
                    if (errorDetail.Contains("ConnectFailure")) {
                        errorMessage += ": Could not connect to Grid engine";
                        task.MessageCode = TaskMessageCode.NoConnection;
                        task.MessageText = "No connection to Grid engine";
                    } else {
                        Match match = Regex.Match(errorDetail, @"ERROR from myproxy-server \(([^\)]+)\):([^:]+): (.+)$");
                        if (match.Success) {
                            // ERROR from myproxy-server (ify-ce03.terradue.com):X509_verify_cert() failed: certificate has expired
                            switch (match.Groups[3].Value) {
                                case "certificate has expired" :
                                    task.MessageCode = TaskMessageCode.CertificateExpired;
                                    task.MessageText = "Proxy certificate has expired";
                                    errorMessage = task.MessageText + " (received from " + match.Groups[1].Value + ")";
                                    break;
                                default :
                                    errorMessage += ": " + errorDetail;
                                    task.MessageCode = TaskMessageCode.Other;
                                    task.MessageText = errorMessage;
                                    break;
                            }
                        } else {
                            errorMessage += ": " + errorDetail;
                            task.MessageCode = TaskMessageCode.Other;
                            task.MessageText = errorMessage;
                        }
                    }
                    
                }
                
                context.AddError(errorMessage, task.GetMessageClass(task.MessageCode));
                return false;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether it is possible to start the execution of the specified task on the Grid engine.</summary>
        /// <param name="task">the task to be started</param>
        /// <returns><c>true</c> if the task can be started, <c>false</c> otherwise</returns>
        /// <remarks>The method checks whether there are working and result directories available.</remarks>
        public override bool CanStartTask(Task task) {
            if (!base.CanStartTask(task)) return false;
            
            // Check whether computing resource and working/result directories are available
            /* !!! if (!computingResource.Available && context.UserLevel >= UserLevel.Administrator) throw new EntityUnvailableException("The selected computing resource is currently not available");*/
            if (context.UserLevel >= UserLevel.Administrator && WorkingDir == null) {
                context.AddError("No working directory available on selected computing resource");
                return false;
            }
            
            if (context.UserLevel >= UserLevel.Administrator && ResultDir == null) {
                context.AddError("No result directory available on selected computing resource");
                return false;
            }
            
            // Generic error message for normal users
            if (WorkingDir == null || ResultDir == null) {
                context.AddError("The selected computing resource (or one of its resources) is currently not available", "computingResourceNotAvailable");
                return false;
            }

            if (GridEngineWebService == null) {
                throw new ProcessingException(context.UserLevel >= UserLevel.Administrator ? "Bad Grid engine web service configuration" : "Submission not possible, please retry later", this, task);
            }
            
            return true;
            
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Starts or restarts the execution of the specified task on the Grid engine.</summary>
        /// <param name="task">the task to be started</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method calls the <c>submit</c> method of the LGE's <i>wsServer</i> SOAP web service.</remarks>
        public override bool StartTask(Task task) {
            bool result = false;
            try {
                string wsValue = null;
                if (task.RemoteIdentifier == null) {
                    context.AddError("Task not not yet created");
                    return false;
                } else {
                    wsValue = GridEngineWebService.submit(task.RemoteIdentifier);
                    result = (wsValue == task.RemoteIdentifier);
                    if (context.DebugLevel >= 3) context.AddDebug(3, "wsServer::submit() returned \"" + wsValue + "\" " + (result ? "(correct)" : "(wrong)"));
                }
            } catch (Exception e) {
                context.AddError("Could not submit task " + task.Identifier + " on Grid engine: " + (e.InnerException == null ? e.Message : e.InnerException.Message));
                return false;
            }
            
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Stops the execution of the specified task on the Grid engine.</summary>
        /// <param name="task">the task to be stopped</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method calls the <c>taskAbort</c> method of the LGE's <i>wsServer</i> SOAP web service, which aborts the task and removes it from the LGE (database and file system) at the same time.</remarks>
        public override bool StopTask(Task task) {
            bool result = false;
            try {
                string wsValue = null;
                if (task.RemoteIdentifier == null) {
                    result = true;
                    context.AddDebug(3, "Task not aborted on Grid engine (was not started)");
                } else {
                    wsValue = GridEngineWebService.taskAbort(task.RemoteIdentifier);
                    result = (wsValue == task.RemoteIdentifier);
                    if (context.DebugLevel >= 3) context.AddDebug(3, "wsServer::taskAbort() returned \"" + wsValue + "\" " + (result ? "(correct)" : "(wrong)"));
                    if (result) context.AddDebug(2, "Task aborted on Grid engine");
                    else context.AddError("Could not abort task " + task.Identifier + " on Grid engine");
                }
                //context.WriteInfo("ABORT SUCCESSFUL = " + success);
            
            } catch (System.Web.Services.Protocols.SoapException e) {
                string detail = (e.Detail == null ? null : e.Detail.InnerXml);
                if (detail.Contains("Task does not exist in the db")) {
                    context.AddWarning("Task session " + task.RemoteIdentifier + " does not exist on Grid engine, reference will be removed");
                    result = true;
                } else {
                    if (detail.Contains("Proxy expired")) {
                        task.MessageCode = TaskMessageCode.CertificateExpired;
                    } else {
                        task.MessageCode = TaskMessageCode.Other;
                    }
                    task.MessageText = "Task could not be aborted correctly: " + (detail == null ? e.Message : detail);
                    context.AddError("Could not abort task " + task.Identifier + " on Grid engine: " + (detail == null ? e.Message : detail));
                }

            } catch (Exception e) {
                context.AddError("Could not abort task " + task.Identifier + " on Grid engine: " + (e.InnerException == null ? e.Message : e.InnerException.Message));
            }

            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Obtains status information of the specified job from the Grid engine.</summary>
        /// <param name="task">the task to be queried</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method receives from the LGE the XML document containing the task status information.</remarks>
        public override bool GetTaskStatus(Task task) {
            string url, baseUrl = null;

            XmlDocument taskDetailsDoc = null;
            try {
                url = TaskStatusUrl.Replace("$(SESSION)", task.RemoteIdentifier).Replace("$(UID)", task.Identifier);
                context.AddDebug(3, "Get details of task " + task.Identifier + ": " + url);
                baseUrl = url.Substring(0, url.LastIndexOf('/'));
                taskDetailsDoc = new XmlDocument();
                taskDetailsDoc.Load(url);
            } catch (Exception e) {
                if (context.DebugLevel >= 1) context.AddDebug(1, "Could not get details of task " + task.Identifier + ": " + e.Message);
                return false;
            }
            
            if (taskDetailsDoc.DocumentElement.HasAttribute("status")) {
                int newStatus = StringToStatus(taskDetailsDoc.DocumentElement.Attributes["status"].Value);
                if (newStatus != ProcessingStatus.None) task.ActualStatus = newStatus;
            }
            DateTime dt;
            
            for (int i = 0; i < taskDetailsDoc.DocumentElement.ChildNodes.Count; i++) {
                XmlElement elem = taskDetailsDoc.DocumentElement.ChildNodes[i] as XmlElement;
                if (elem == null) continue;
                
                switch (elem.Name) {
                    case "startDate" :
                        DateTime.TryParse(elem.InnerText, out dt);
                        //task.StartTime = dt.ToUniversalTime(); // commented for PBI-4667
                        break;
                    case "endDate" :
                        DateTime.TryParse(elem.InnerText, out dt);
                        task.EndTime = dt.ToUniversalTime();
                        break;
                    case "message" :
                        task.StatusMessage = elem.InnerXml;
                        if (elem.HasAttribute("date")) {
                            DateTime.TryParse(elem.Attributes["date"].Value, out dt);
                            task.StatusMessageTime = dt.ToUniversalTime();
                        }
                        if (elem.HasAttribute("type")) {
                            switch (elem.Attributes["type"].Value) {
                                case "ERROR" :
                                    task.StatusMessageType = MessageType.Error;
                                    break;
                                case "WARNING" : 
                                    task.StatusMessageType = MessageType.Warning;
                                    break;
                                default : 
                                    task.StatusMessageType = MessageType.Info;
                                    break;
                            }
                        }
                        break;
                    case "jobs" :
                        for (int j = 0; j < elem.ChildNodes.Count; j++) {
                            XmlElement jobElem = elem.ChildNodes[j] as XmlElement;
                            if (jobElem == null || jobElem.Name != "job" || !jobElem.HasAttribute("name")) continue;
                            
                            Job job = task.Jobs[jobElem.Attributes["name"].Value];
                            if (job == null) continue;
                            
                            if (jobElem.HasAttribute("resource")) {
                                url = jobElem.Attributes["resource"].Value; 
                                if (!url.Contains("://")) url = baseUrl + "/" + url;
                                job.StatusUrl = url;
                                job.GetStatus(true);
                            }
                        }
                        break;
                }
            }
            
            return true;

        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Destroys the representation of the specified task on the computing resource.</summary>
        /// <param name="task">the task to be destroyed</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method calls StopTask, which aborts the task and removes it from the LGE.</remarks>
        public override bool DestroyTask(Task task) {
            return StopTask(task);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a representation of the specified job on the Grid engine.</summary>
        /// <param name="job">the job to be created</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method calls the <c>insertJob</c> method of the LGE's <i>wsServer</i> SOAP web service.</remarks>
        public override bool CreateJob(Job job) {
            Task task = job.GetTask();

            // Add generic job parameters
            ExecutionParameterSet parameters = RenewJobSubmissionParameters(task, job);

            // Add job to session
            bool result = false;
            try {
                string wsValue = GridEngineWebService.insertJob(
                        task.RemoteIdentifier,
                        job.JobType,
                        job.Name, 
                        job.DependenciesAsStringArray, 
                        parameters.ToArray()
                );
                result = (wsValue == job.Name);
                if (context.DebugLevel >= 3) context.AddDebug(3, "wsServer::insertJob() returned \"" + wsValue + "\" " + (result ? "(correct)" : "(wrong)"));
            } catch (Exception e) {
                //context.WriteInfo("ABORT ERROR");
                string errorMessage = "Could not create job \"" + job.Name + "\" of task " + task.Identifier + "on Grid engine";
                // innerErrorMessage = (e.InnerException == null ? e.Message : e.InnerException.Message);
                
                string errorDetail = wsServerClient.GetErrorDetail(e);
                
                if (errorDetail != null) {
                    if (errorDetail.Contains("ConnectFailure")) {
                        errorMessage += ": Could not connect to Grid engine";
                    } else {
                        errorMessage += ": " + errorDetail;
                    }
                    
                }
                
                context.AddError(errorMessage);
                return false;
            }
            if (result) {
                //SaveGridEngineParameters(job);
                if (context.DebugLevel >= 2) context.AddDebug(2, "Job " + job.Name + " created on Grid engine");
                return true;
            } else {
                context.AddError("Could not create job \"" + job.Name + "\" of task " + task.Identifier + "on Grid engine");
                return false;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Updates the information and parameters of the specified job on the Grid engine.</summary>
        /// <param name="job">the job to be updated</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method calls the <c>modifyJob</c> method of the LGE's <i>wsServer</i> SOAP web service.</remarks>
        public override bool UpdateJob(Job job) {
            Task task = job.GetTask();

            ExecutionParameterSet parameters = RenewJobSubmissionParameters(task, job);
            
            string s = GridEngineWebService.modifyJob(
                    task.RemoteIdentifier, 
                    job.Name, 
                    parameters.ToArray()
            );

            return (s == job.Name);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Starts or restarts the execution of the specified job on the Grid engine.</summary>
        /// <param name="job">the job to be started</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method calls the <c>jobResubmit</c> method of the LGE's <i>wsServer</i> SOAP web service.</remarks>
        public override bool StartJob(Job job) {
            Task task = job.GetTask();

            //if (!RenewJobSubmissionParameters(task, job)) return false;
            
            bool result = false;
            try {
                string wsValue = GridEngineWebService.jobResubmit(task.RemoteIdentifier, job.Name); 
                result = (wsValue == task.RemoteIdentifier);
                if (context.DebugLevel >= 3) context.AddDebug(3, "wsServer::jobResubmit() returned \"" + wsValue + "\" " + (result ? "(correct)" : "(wrong)"));
            } catch (Exception e) {
                string errorMessage = "Could not resubmit job \"" + job.Name + "\" of task " + task.Identifier + " on Grid engine";
                // innerErrorMessage = (e.InnerException == null ? e.Message : e.InnerException.Message);
                
                string errorDetail = wsServerClient.GetErrorDetail(e);
                
                if (errorDetail != null) {
                    if (errorDetail.Contains("ConnectFailure")) {
                        errorMessage += ": Could not connect to Grid engine";
                    } else {
                        Match match = Regex.Match(errorDetail, @"ERROR from myproxy-server \(([^\)]+)\):([^:]+): (.+)$");
                        if (match.Success) {
                            switch (match.Groups[3].Value) {
                                case "certificate has expired" :
                                    errorMessage = "Proxy certificate has expired (received from " + match.Groups[1].Value + ")";
                                    break;
                            }
                        } else {
                            errorMessage += ": " + errorDetail;
                        }
                    }
                    
                }
                
                context.AddError(errorMessage);
                return false;
            }
            if (result) {
                if (context.DebugLevel >= 2) context.AddDebug(2, "Job " + job.Name + " resubmitted on Grid engine");
                return true;
            } else {
                context.AddError("Could not resubmit job \"" + job.Name + "\" of task " + task.Identifier + " on Grid engine");
                return false;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Suspends the specified job on the Grid engine.</summary>
        /// <param name="job">the job to be suspended</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method calls the <c>setJobStatus</c> method of the LGE's <i>wsServer</i> SOAP web service with the status value <c>600</c>.</remarks>
        public override bool SuspendJob(Job job) {
            Task task = job.GetTask();
            GridEngineWebService.setJobStatus(task.RemoteIdentifier, job.Name, "600");
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Resumes the specified job on the Grid engine.</summary>
        /// <param name="job">the job to be resumed</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method calls the <c>setJobStatus</c> method of the LGE's <i>wsServer</i> SOAP web service with the status value <c>100</c>.</remarks>
        public override bool ResumeJob(Job job) {
            Task task = job.GetTask();
            GridEngineWebService.setJobStatus(task.RemoteIdentifier, job.Name, "100");
            return true;
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Stops the specified job on the Grid engine.</summary>
        /// <param name="job">the job to be stopped</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method calls the <c>jobClean</c> method of the LGE's <i>wsServer</i> SOAP web service.</remarks>
        public override bool StopJob(Job job) {
            Task task = job.GetTask();
            return (GridEngineWebService.jobClean(task.RemoteIdentifier, job.Name) == job.Name);
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Requests the Grid engine to accept the specified job as completed.</summary>
        /// <param name="job">the job to be completed</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        /// <remarks>The method calls the <c>setJobStatus</c> method of the LGE's <i>wsServer</i> SOAP web service with the status value <c>400</c>.</remarks>
        public override bool CompleteJob(Job job) {
            Task task = job.GetTask();
            string wsValue = GridEngineWebService.setJobStatus(task.RemoteIdentifier, job.Name, StatusToGridEngineCode(ProcessingStatus.Completed)); 
            bool result = (wsValue != null && wsValue.StartsWith("INFO::" + StatusToGridEngineCode(ProcessingStatus.Completed)));
            if (context.DebugLevel >= 3) context.AddDebug(3, "wsServer::setJobStatus() returned \"" + wsValue + "\" " + (result ? "(correct)" : "(wrong)"));
            return result;
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Obtains status information of the specified job from the Grid engine.</summary>
        /// <param name="job">the job to be queried</param>
        /// <returns><c>true</c> if the status information was received correctly, <c>false</c> otherwise</returns>
        /// <remarks>The method receives from the LGE the XML document containing the job status information.</remarks>
        public override bool GetJobStatus(Job job) {
            Task task = job.GetTask();

            XmlDocument jobDetailsDoc = null;
            bool wasFinished = job.Finished;
            //context.AddDebug(3, Name + " " + wasFinished);
            
            if (job.StatusUrl == null) job.StatusUrl = JobStatusUrl.Replace("$(SESSION)", task.RemoteIdentifier).Replace("$(UID)", task.Identifier).Replace("$(JOB)", job.Name); // !!! use different URL if given TODO
                                                                                       
            try {
                jobDetailsDoc = context.LoadXmlFile(job.StatusUrl);
            } catch (Exception) {
                if (wasFinished) return true;
                return false;
            }
            
            string gridEngineFolderUrl = job.StatusUrl.Substring(0, job.StatusUrl.LastIndexOf('/'));
            int oldStatus = job.Status;

            // Read job status (overwrite only if it was not finished)
            if (!wasFinished && jobDetailsDoc.DocumentElement.HasAttribute("status")) job.Status = StringToStatus(jobDetailsDoc.DocumentElement.Attributes["status"].Value);
            DateTime dt;
            
            for (int i = 0; i < jobDetailsDoc.DocumentElement.ChildNodes.Count; i++) {
                XmlElement elem = jobDetailsDoc.DocumentElement.ChildNodes[i] as XmlElement;
                if (elem == null) continue;
                
                switch (elem.Name) {
                    case "startDate" :
                        string startDateText = elem.InnerText;
                        
                        // If the job has been aborted/resubmitted and we see still the status information of the previous execution,
                        // do not consider the whole status information at all.
                        if (oldStatus == ProcessingStatus.Created && startDateText == job.SequentialExecutionIdentifier) break;

                        job.SequentialExecutionIdentifier = startDateText;
                        DateTime.TryParse(startDateText, out dt);
                        job.StartTime = dt.ToUniversalTime();
                        break;
                    case "endDate" :
                        DateTime.TryParse(elem.InnerText, out dt);
                        job.EndTime = dt.ToUniversalTime();
                        break;
                    case "message" :
                        job.StatusMessage = elem.InnerXml;
                        if (elem.HasAttribute("date")) {
                            DateTime.TryParse(elem.Attributes["date"].Value, out dt);
                            job.StatusMessageTime = dt.ToUniversalTime();
                        }
                        if (elem.HasAttribute("type")) {
                            switch (elem.Attributes["type"].Value) {
                                case "ERROR" :
                                    job.StatusMessageType = MessageType.Error;
                                    break;
                                case "WARNING" : 
                                    job.StatusMessageType = MessageType.Warning;
                                    break;
                                default : 
                                    job.StatusMessageType = MessageType.Info;
                                    break;
                            }
                        }
                        break;
                    case "debug" :
                        job.DebugMessage = elem.InnerXml;
                        if (elem.HasAttribute("date")) {
                            DateTime.TryParse(elem.Attributes["date"].Value, out dt);
                            job.DebugMessageTime = dt.ToUniversalTime();
                        }
                        if (elem.HasAttribute("type")) {
                            switch (elem.Attributes["type"].Value) {
                                case "ERROR" :
                                    job.DebugMessageType = MessageType.Error;
                                    break;
                                case "WARNING" : 
                                    job.DebugMessageType = MessageType.Warning;
                                    break;
                                default : 
                                    job.DebugMessageType = MessageType.Info;
                                    break;
                            }
                        }
                        break;
                    case "numberOfParams" :
                        //int count;
                        //Int32.TryParse(elem.InnerXml, out count);
                        //job.ArgumentCount = count;
                        break;
                    case "nodes" :
                        for (int j = 0; j < elem.ChildNodes.Count; j++) {
                            XmlElement nodeElem = elem.ChildNodes[j] as XmlElement;
                            if (nodeElem == null || nodeElem.Name != "node" || !nodeElem.HasAttribute("pid")) continue;
                            if (job.ShowNodes) {
                                NodeProcess node = new NodeProcess(nodeElem, gridEngineFolderUrl);
                                job.TotalArguments += node.TotalArguments;
                                job.DoneArguments += node.DoneArguments;
                                job.Nodes.Add(node);
                            } else {
                                int total = 0, done = 0;
                                NodeProcess.GetArguments(nodeElem, ref total, ref done);
                                job.TotalArguments += total;
                                job.DoneArguments += done;
                            }
                        }
                        break;
                }
            }

            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Obtains information on the results of the specified task from the Grid Engine.</summary>
        /// <param name="task">the task in question</param>
        /// <returns><c>true</c> if the result information was received correctly, <c>false</c> otherwise</returns>
        /// <remarks>The method receives from the LGE for each publishing job the XML document containing the corresponding status and result information.</remarks>
        public override bool GetTaskResult(Task task) {
            bool isMetadataFile;
            
            for (int i = 0; i < task.Jobs.Count; i++) {
                Job job = task.Jobs[i];
                if (!job.Publishes) continue;
                
                int metadataCount = 0, fileCount = 0;
            
                XmlDocument jobDoc = new XmlDocument();
                try {
                    jobDoc.Load(JobStatusUrl.Replace("$(SESSION)", task.RemoteIdentifier).Replace("$(UID)", task.Identifier).Replace("$(JOB)", job.Name));
                } catch (Exception) {
                    continue;
                }

                foreach (string path in new string[] {"/job/PublishedResults/Location", "/job/nodes/node/PublishedResults/Location"}) {
                    foreach (XmlNode locationNode in jobDoc.SelectNodes(path)) {
                        XmlElement elem = locationNode as XmlElement;
                        string baseUrl = null;
                        isMetadataFile = (elem.HasAttribute("type") && elem.Attributes["type"].Value == "metadata");
                        if (isMetadataFile) {
                            if (metadataCount != 0) continue;
                            if (elem.HasAttribute("url")) baseUrl = elem.Attributes["url"].Value;
                            else baseUrl = task.MetadataUrl;
                            if (baseUrl == null) continue;
                            metadataCount++;

                        } else {
                            if (task.PublishServerId != 0) baseUrl = task.PublishServer.DownloadUrl;
                            else if (elem.HasAttribute("url")) baseUrl = elem.Attributes["url"].Value;
                            if (baseUrl == null) continue;
                            
                            //if (task.ResultBaseUrl == null) task.ResultBaseUrl = baseUrl;
                            
                        }
                        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
                        baseUrl = baseUrl.Replace("$(UID)", task.Identifier);

                        XmlNodeList nodes = locationNode.SelectNodes("File");
                    
                        foreach (XmlNode node in nodes) {
                            XmlElement fileElem = node as XmlElement;
                            if (fileElem == null) continue;
                            string url = fileElem.InnerXml;
                            if (url.StartsWith("./")) url = url.Substring(2);
                            else if (url.StartsWith("/")) url = url.Substring(1);
                            url = baseUrl + "/" + url;
                            if (isMetadataFile) {
                                task.ResultMetadataFiles.Add(url);
                            } else {
                                long size = 0;
                                DateTime creationTime = DateTime.UtcNow;
                                if (fileElem.HasAttribute("size")) Int64.TryParse(fileElem.Attributes["size"].Value, out size);
                                if (fileElem.HasAttribute("endtime") && DateTime.TryParse(fileElem.Attributes["endtime"].Value, out creationTime)) creationTime = creationTime.ToUniversalTime();
                                task.OutputFiles.Add(new DataSetInfo(url, null, size, creationTime));
                            }
                            fileCount++;
                        }
                    }
                    if (fileCount != 0) break; 
                }
            }
            
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static void WriteTaskResult(Task task, string xslFile, XmlWriter output) {
            XmlDocument resultDoc = task.GetResultDocument(xslFile);
            output.WriteRaw(resultDoc.DocumentElement.OuterXml);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the RDF document containing information on the results of the specified task to the output stream.</summary>
        /// <param name="task">the task in question</param>
        public override void WriteTaskResultRdf(Task task, XmlWriter output) {
            if (task.OutputFiles == null) task.GetResult();

            // !!! TODO Remove namespace declarations from here !!!

            List<string> series = new List<string>();

            int rdfCount = 0;

            string downloadProviderPrefix = task.DownloadProviderUrl;
            if (downloadProviderPrefix != null) downloadProviderPrefix += (downloadProviderPrefix.Contains("?") ? "&" : "?") + "url=";

            foreach (string url in task.ResultMetadataFiles) {
                if (!url.EndsWith(".rdf")) continue;

                try {
                    XmlDocument rdfDoc = new XmlDocument();
                    rdfDoc.Load(url);

                    if (rdfCount == 0) StartRdfContainer(output);

                    rdfCount++;

                    XmlNamespaceManager nsm = new XmlNamespaceManager(rdfDoc.NameTable);
                    nsm.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                    nsm.AddNamespace("dclite4g", "http://xmlns.com/2008/dclite4g#");
                    XmlNodeList seriesNodes = rdfDoc.SelectNodes("/rdf:RDF/dclite4g:Series", nsm);
                    foreach (XmlNode seriesNode in seriesNodes) {
                        XmlElement seriesElem = seriesNode as XmlElement;
                        if (seriesElem == null || !seriesElem.HasAttribute("rdf:about")) continue;
                        if (series.Contains(seriesElem.Attributes["rdf:about"].Value)) continue;

                        series.Add(seriesElem.Attributes["rdf:about"].Value);
                        seriesElem.WriteTo(output);
                    }

                    XmlNodeList dataSetNodes = rdfDoc.SelectNodes("/rdf:RDF/dclite4g:DataSet", nsm);
                    foreach (XmlNode dataSetNode in dataSetNodes) {
                        XmlElement dataSetElem = dataSetNode as XmlElement;
                        if (dataSetElem == null) continue;

                        XmlNodeList rdfNodes = dataSetElem.SelectNodes(".//@rdf:about | .//@rdf:resource", nsm);
                        foreach (XmlNode rdfNode in rdfNodes) {
                            XmlAttribute rdfAttr = rdfNode as XmlAttribute;
                            if (rdfAttr == null) continue;

                            rdfAttr.Value = rdfAttr.Value.Replace("$(PUBLISHROOT)", task.ResultUrl);
                        }

                        if (downloadProviderPrefix != null) { 
                            rdfNodes = dataSetElem.SelectNodes("dclite4g:onlineResource/*/@rdf:about | dclite4g:quicklook/@rdf:resource", nsm);
                            foreach (XmlNode rdfNode in rdfNodes) {
                                XmlAttribute rdfAttr = rdfNode as XmlAttribute;
                                if (rdfAttr == null) continue;

                                rdfAttr.Value = String.Format("{0}{1}", downloadProviderPrefix, HttpUtility.UrlEncode(rdfAttr.Value));
                            }
                        }

                        dataSetElem.WriteTo(output);
                    }

                    if (rdfCount != 0) break;
                } catch (Exception e) {
                    context.AddError(e.Message);
                }
            }

            if (rdfCount == 0) WriteTaskDefaultResultRdf(task, output);
            else EndRdfContainer(output); // </rdf:RDF>
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes a fallback RDF document containing information on the results of the specified task to the output stream.</summary>
        /// <param name="task">the task in question</param>
        /// <remarks>This method is called automatically if no genuine RDF of the task results is available.</remarks>
        public void WriteTaskDefaultResultRdf(Task task, XmlWriter output) {
            IfyWebContext webContext = context as IfyWebContext; // TODO: Replace

            Series series = null;
            try {
                string seriesIdentifier = task.Service.Identifier;
                if (seriesIdentifier == null) series = Series.FromIdentifier(context, seriesIdentifier);
            } catch (Exception e) {
                context.WriteError(e.Message + " " + e.StackTrace);
            }

            string seriesResource = (series == null ? "unknownResource" : webContext.HostUrl + "/series/xml?id=" + series.Id); 

            // Get startdate, stopdate and bounding box

            string downloadProviderPrefix = task.DownloadProviderUrl;
            if (downloadProviderPrefix != null) downloadProviderPrefix += (downloadProviderPrefix.Contains("?") ? "&" : "?") + "url=";

            string startDateStr = null, endDateStr = null, spatialStr = null;
            /*for (int i = 0; i < task.RequestParameters.Count; i++) {
                switch (task.RequestParameters[i].Type) {
                    case "startdate" :
                        startDateStr = task.RequestParameters[i].Value;
                        break;
                    case "enddate" :
                        endDateStr = task.RequestParameters[i].Value;
                        break;
                    case "bbox" :
                        spatialStr = new BoundingBox(task.RequestParameters[i].Value).ToPolygonWkt();
                        break;
                }
            }*/

            StartRdfContainer(output);

            output.WriteStartElement("dclite4g:Series");
            output.WriteAttributeString("rdf:about", seriesResource);
            output.WriteStartElement("dc:description");
            output.WriteAttributeString("rdf:resource", (series == null ? "unknownDescription" : webContext.HostUrl + "/series/description?id=" + series.Id));
            output.WriteEndElement(); // </dc:description>
            output.WriteEndElement(); // </dclite4g:Series>

            output.WriteStartElement("dclite4g:DataSet");
            output.WriteAttributeString("rdf:about", webContext.HostUrl + "/" + task.Identifier);

            output.WriteElementString("dc:identifier", webContext.HostUrl + "/" + task.Identifier); // task caption
            output.WriteElementString("dc:title", task.Name + " (" + task.Identifier + ")"); // task caption
            output.WriteElementString("dc:subject", task.Service.Identifier);
            output.WriteElementString("eop:processorVersion", task.Service.Identifier + (task.Service.Version == null ? String.Empty : "/" + task.Service.Version));
            output.WriteElementString("eop:orbitNumber", String.Empty);
            output.WriteElementString("eop:acquisitionStation", String.Empty);
            output.WriteElementString("eop:processingCenter", String.Empty);
            output.WriteElementString("eop:processorVersion", String.Empty);
            output.WriteElementString("eop:processingDate", String.Empty); // use context.ShortName or something similar !!!

            if (startDateStr != null) output.WriteElementString("ical:dtstart", startDateStr);
            if (endDateStr != null) output.WriteElementString("ical:dtend", endDateStr);
            if (spatialStr != null) output.WriteElementString("dct:spatial", spatialStr);
            output.WriteElementString("dct:created", task.EndTime.ToUniversalTime().ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\Z"));
            output.WriteElementString("dct:modified", task.EndTime.ToUniversalTime().ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\Z"));
            output.WriteElementString("dct:audience", "Public");
            output.WriteStartElement("dc:source");
            output.WriteAttributeString("rdf:resource", webContext.HostUrl + task.RelativeUrl);
            output.WriteEndElement();
            output.WriteStartElement("dc:publisher");
            output.WriteAttributeString("rdf:resource", task.Service.AbsoluteUrl);  // !!! use context.BaseUrl
            output.WriteEndElement();
            output.WriteStartElement("dc:creator");
            output.WriteAttributeString("rdf:resource", String.Format(webContext.AdminRootUrl == null ? "{1}/admin/user.aspx?id={0}" : "{1}{2}/{3}/{0}", task.OwnerId, webContext.HostUrl, webContext.AdminRootUrl, EntityType.GetEntityType(typeof(Terradue.Portal.User)).Keyword));
            output.WriteEndElement();
            output.WriteElementString("dc:title", task.Name);
            output.WriteElementString("dc:abstract", String.Empty);

            foreach (DataSetInfo dataSet in task.OutputFiles) {
                string url = dataSet.Resource;
                if (downloadProviderPrefix != null) url = String.Format("{0}{1}", downloadProviderPrefix, url);
                if (Regex.Match(url, @"_browse\..*$").Success) continue;

                try {
                    output.WriteStartElement("dclite4g:onlineResource");
                    output.WriteStartElement("ws:HTTP");
                    output.WriteAttributeString("rdf:about", url);
                    output.WriteEndElement(); // </ws:HTTP>

                    output.WriteEndElement(); // </dclite4g:onlineResource>

                } catch (Exception e) {
                    output.WriteElementString("EXCEPTION", e.Message);
                }
            }

            // Then show quicklooks
            foreach (DataSetInfo dataSet in task.OutputFiles) {
                string url = dataSet.Resource;
                if (!Regex.Match(url, @"_browse\..*$").Success) continue;

                output.WriteStartElement("dclite4g:quicklook");
                output.WriteAttributeString("rdf:resource", url);

                output.WriteEndElement(); // </dclite4g:onlineResource>
            }

            output.WriteEndElement(); // </dclite4g:DataSet>
            EndRdfContainer(output);
        }

        //---------------------------------------------------------------------------------------------------------------------
        /*
        /// <summary>Inserts the Grid engine parameters for the task into the database.</summary>
        public void SaveGridEngineParameters(Task task) {
            string sql = String.Format("DELETE FROM taskparam WHERE id_task={0} AND id_job IS NULL AND metadata IS NULL;", task.Id); 
            context.Execute(sql);
            for (int i = 0; i < TaskParameters.Count; i++) {
                TaskParameter param = TaskParameters[i];
                if (param.Metadata) continue;
                
                sql = String.Format("INSERT INTO taskparam (id_task, id_job, name, value, metadata) VALUES ({0}, NULL, {1}, {2}, NULL);",
                        task.Id,
                        StringUtils.EscapeSql(param.Name),
                        StringUtils.EscapeSql(param.Value)
                );
                context.Execute(sql);
            }
        }*/
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Inserts the Grid engine parameters for the task into the database.</summary>
/*        public void SaveGridEngineParameters(Job job) {
            Task task = job.GetTask();
            string sql = String.Format("DELETE FROM taskparam WHERE id_task={0} AND id_job={1} AND metadata IS NULL;", task.Id, job.Id); 
            context.Execute(sql);
            for (int i = 0; i < job.Parameters.Count; i++) {
                if (!job.Parameters[i].Submission) continue;
                sql = String.Format("INSERT INTO taskparam (id_task, id_job, name, value, metadata) VALUES ({0}, {1}, {2}, {3}, NULL);",
                        task.Id,
                        job.Id,
                        StringUtils.EscapeSql(job.Parameters[i].Name),
                        StringUtils.EscapeSql(job.Parameters[i].Value)
                );
                context.Execute(sql);
            }

            if (context.DebugLevel >= 2) context.AddDebug(2, "Submission parameters renewed");
        }
*/        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates or recreates the Grid engine parameters for the job and updates them in the database.</summary>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise</returns>
        protected virtual ExecutionParameterSet RenewJobSubmissionParameters(Task task, Job job) {
            ExecutionParameterSet result = new ExecutionParameterSet();
            foreach (ExecutionParameter parameter in job.ExecutionParameters) result.Add(parameter.Name, parameter.Value);

            // Correct MaxNodes and MinArgumentsPerNode if necessary
            result.AddSubmissionParameter("SUID", task.Identifier);
            result.AddSubmissionParameter("lgeContactPoint", GridEngineAccessPoint);
            result.AddSubmissionParameter("statusService", GridEngineAccessPoint);
            result.AddSubmissionParameter("statusPool", "30");
            result.AddSubmissionParameter("CE", Hostname);
            result.AddSubmissionParameter("CEPORT", Port.ToString());
            result.AddSubmissionParameter("GSIPORT", ":" + GsiPort.ToString());
            result.AddSubmissionParameter("WDIR", WorkingDir);
            result.AddSubmissionParameter("RDIR", ResultDir);
            result.AddSubmissionParameter("jobManager", JobManager);
            result.AddSubmissionParameter("jobqueue", JobQueue);
            result.AddSubmissionParameter("MIDDLEWARE", GridType);
            result.AddSubmissionParameter("LIMIT_MAX_NUMBER_OF_JOBS", job.MaxNodes.ToString());
            result.AddSubmissionParameter("LIMIT_MIN_NUMBER_OF_PARAMS", job.MinArgumentsPerNode.ToString());
            result.AddSubmissionParameter("EXTERNALCATALOGUEHOST", context.GetConfigValue("ExternalCatalogueHost"));
            if (job.ForcedExit) result.AddSubmissionParameter("options", "EXIT1;");
            // /*!!!*/result.AddSubmissionParameter("WSJOBORDER", "http://gpod-dev.terradue.com/tasks/_job.orders.asp");
            
            string portalPublishUrl = context.ResultMetadataUploadUrl;
            
            if (job.Publishes) {
                string urlStr;
                if (portalPublishUrl != null) portalPublishUrl = portalPublishUrl.Replace("$(UID)", task.Identifier);
                if (task.PublishServer == null) {
                    if (portalPublishUrl == null) {
                        throw new InvalidOperationException("No publish server provided");
                    } else {
                        urlStr = portalPublishUrl + ",[" + (task.Compression == null ? String.Empty : task.Compression) + "],*";
                    }

                } else {
                    urlStr = task.PublishServer.UploadUrl.Replace("$(UID)", task.Identifier) + ",[" + (task.Compression == null ? String.Empty : task.Compression) + "],*";
                    if (portalPublishUrl != null) urlStr += String.Format(";{0},[],*_browse.*;{0},[],*.rdf", portalPublishUrl);
                }
                result.AddSubmissionParameter("urls", urlStr);
            }
            
            return result;
        }

        public static int StringToStatus(string s) {
            if (s == null) return 0;
            switch (s.ToLower()) {
                case "preparation" : 
                    return ProcessingStatus.Preparing;
                case "pending" : 
                    return ProcessingStatus.Queued; // (it is "pending" only from the Grid engine point of view)
                case "active" :
                    return ProcessingStatus.Active;
                case "failed" :
                    return ProcessingStatus.Failed;
                case "finalization" :
                case "done" :
                    return ProcessingStatus.Completed;
                case "paused" :
                    return ProcessingStatus.Paused;
            }
            return 0;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static int GridEngineCodeToStatus(string s) {
            if (s == null) return 0;
            int code;
            Int32.TryParse(s, out code);
            if (code == 400) return ProcessingStatus.Completed;
            else if (code >= 300) return ProcessingStatus.Failed;
            else if (code >= 200) return ProcessingStatus.Active;
            else if (code > 100) return ProcessingStatus.Active; // previously: Pending;
            else if (code == 100) return ProcessingStatus.Created;
            return 0;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static string StatusToGridEngineCode(int i) {
            if (i == 0) return null;
            if (i == ProcessingStatus.Completed) return "400";
            if (i >= ProcessingStatus.Failed) return "300";
            if (i > ProcessingStatus.Created) return "200";
            if (i == ProcessingStatus.Created) return "100";
            return null;
        }
        
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    /// <summary>Represents a directory on a computing resource (working or result directory)</summary>
    public class ComputingGridDirectory {
        protected GlobusComputingElement computingResource;
        protected bool available; 
        protected string path;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the absolute path of the directory.</summary>
        public string Path {
            get { return path; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the directory is available.</summary>
        public bool Available {
            get { return available; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a ComputingGridDirectory instance for the specified computing resource.</summary>
        /// <param name="computingResource">the computing resource to which the directory belongs</param>
        /// <param name="available">indicates whether the directory is available</param>
        /// <param name="path">the absolute path of the directory</param>
        public ComputingGridDirectory(GlobusComputingElement computingResource, bool available, string path) {
            this.computingResource = computingResource;
            this.available = available;
            //this.dirType = dirType;
            this.path = path;
        }
    }
    
    
    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    /// <summary>Request method types for computing resource status values or similar.</summary>
    public class GlobusStatusMethod {

        /// <summary>Undefined request method</summary>
        public const int Unknown = 0;

        /// <summary>Value is not requested</summary>
        public const int NoRefresh = 1;

        /// <summary>Values are requested via Ganglia</summary>
        public const int Ganglia = 2;

        /// <summary>Values are requested via Globus MDS web service</summary>
        public const int GlobusMds = 3;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a valid request method type value.</summary>
        /// <param name="value">the original request method type</param>
        /// <returns>the original or corrected (if invalid) request method type value</returns>
        public static int FromInt(int value) {
            return (value < 1 || value > 2 ? 1 : value);
        }
    }
    
}

