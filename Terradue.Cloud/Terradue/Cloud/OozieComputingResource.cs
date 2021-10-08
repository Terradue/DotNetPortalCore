using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Portal;
using Terradue.Util;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Runtime.Serialization;





namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    //! Represents a Globus computing resource that is accessed through an LGE interface.
	[Serializable]
	[DataContract]
    [EntityTable("ooziecr", EntityTableConfiguration.Custom)]
    public class OozieComputingResource : CloudComputingResource {
        
        private Dictionary<string, string> environmentVariables; 
        
        private bool statusUrlRequired = false;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates that the status of a task and all its jobs are received with one request at task level.</summary>
		[IgnoreDataMember]
        public override bool ProvidesFullTaskStatus {
            get { return true; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        //! Gets the address or hostname of the Globus computing resource.
        [DataMember]
        [EntityDataField("oozie_address")]
        public string OozieBaseAddress { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
		[DataMember]
        [EntityDataField("job_tracker")]
        public string JobTracker { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
		[DataMember]
        [EntityDataField("env_vars")]
        public string EnvironmentVariablesString { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
		[DataMember]
        public Dictionary<string, string> EnvironmentVariables {
            get {
                if (environmentVariables != null) return environmentVariables;
                environmentVariables = new Dictionary<string, string>();
                if (EnvironmentVariablesString == null) return environmentVariables;
                string[] vars = EnvironmentVariablesString.Split(';');
                foreach (string var in vars) {
                    string[] nameValue = var.Split(new char[]{'='}, 2);
                    if (nameValue.Length != 2) continue;
                    environmentVariables[nameValue[0].Trim()] = nameValue[1].Trim();
                }
                return environmentVariables;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
		[DataMember]
        [EntityDataField("queue_name")]
        public string QueueName { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

		[DataMember]
        [EntityDataField("proc_user")]
        public string ProcessingUser { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
		[DataMember]
        [EntityDataField("hdfs_name_node")]
        public string HdfsNameNode { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
		[DataMember]
        [EntityDataField("hdfs_ws_address")]
        public string HdfsAccessPoint { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
		[DataMember]
        [EntityDataField("task_base_dir")]
        public string TaskBaseDirectory { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
		[DataMember]
        [EntityDataField("task_proc_dir")]
        public string TaskProcessingDirectory { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
		[DataMember]
        [EntityDataField("app_doc_url")]
        public string JobApplicationDocumentUrl { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the computing resource is able to monitor its own status.</summary>
        [IgnoreDataMember]
        public override bool CanMonitorStatus { 
            get {
                return false;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

		[DataMember]
        public XmlDocument JobApplicationDocument { get; protected set; }

		#region implemented abstract members of CloudComputingResource

       

		[DataMember]
		public override List<CloudComputingService> Services {
			get ;
			protected set ;
		}

		#endregion

        #region implemented abstract members of ComputingResource

        public override bool CreateTask(Task task) {
            throw new NotImplementedException();
        }

        public override void WriteTaskResultRdf(Task task, XmlWriter output) {
            throw new NotImplementedException();
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------------

        //! Creates a new ComputingResource instance.
        /*!
            \param context the execution environment context
        */
        public OozieComputingResource(IfyContext context) : base(context) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="Terradue.Cloud.OozieComputingResource"/> class.
		/// </summary>
		/// <param name='context'>
		/// Context.
		/// </param>
		/// <param name='cloudAppliance'>
		/// Cloud appliance to load info from
		/// </param>
		public OozieComputingResource (IfyContext context, CloudAppliance appliance, bool local) : base(context)
		{
			this.Appliance = appliance;

			if ( local == true )
				LoadLocalOozieComputingResource(appliance);

		}
        
        //---------------------------------------------------------------------------------------------------------------------

        //! Creates a new ComputingResource instance.
        /*!
            \param context the execution environment context
            \return the created ComputingResource object
        */
        public static new OozieComputingResource GetInstance(IfyContext context) {
            return new OozieComputingResource(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static OozieComputingResource OnAppliance(IfyContext context, CloudAppliance appliance, bool local) {
			OozieComputingResource result = new OozieComputingResource(context, appliance, local);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override void Store() {
            if (Name == null) Name = Appliance.Name;
            if (Hostname == null) Hostname = "unknown";
            if (OozieBaseAddress == null) OozieBaseAddress = String.Format("http://{0}:11000/oozie", Hostname);
            if (JobTracker == null) JobTracker = String.Format("{0}:8021", Hostname);
            if (QueueName == null) QueueName = "default";
            if (ProcessingUser == null) ProcessingUser = "unknown";
            if (HdfsNameNode == null) HdfsNameNode = "hdfs://localhost:8020";
            if (HdfsAccessPoint == null) HdfsAccessPoint = String.Format("http://{0}:14000", Hostname);            
            if (TaskBaseDirectory == null) TaskBaseDirectory = "/tmp/portal/$(UID)";
            if (TaskProcessingDirectory == null) TaskProcessingDirectory = "/tmp/portal/run/${wf:id()}";
            if (JobApplicationDocumentUrl == null) JobApplicationDocumentUrl = String.Format("http://{0}/sbws/application.xml", Hostname);

            base.Store();
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        //---------------------------------------------------------------------------------------------------------------------
        
        //! Calculates the status of a Globus computing resource.
        /*!
            \param refresh determines whether the is refreshed by a new request.
        */
        public override void GetStatus() {
/*            DateTime LastModifiedTime = DateTime.MinValue;
        
            IDataReader reader = context.GetQueryResult(String.Format("SELECT total_nodes, free_nodes, modified FROM crstate WHERE id_cr={0} AND total_nodes IS NOT NULL AND free_nodes IS NOT NULL;", Id));
            bool wasModified = false;
            if (reader.Read()) {
                TotalCapacity = context.GetIntegerValue(reader, 0);
                FreeCapacity = context.GetIntegerValue(reader, 1);
                wasModified = true;
                LastModifiedTime = context.GetDateTimeValue(reader, 2);
            }
            reader.Close();
            Refreshed = (wasModified && (context.Now - LastModifiedTime) <= context.ComputingResourceStatusValidity);
            
            if (Refreshed) return;
            
            switch (StatusMethod) {
                case GridStatusRequestMethod.Ganglia :
                    GetStatusFromGangliaUrl();
                    break;
            }*/
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /*public override bool CreateTask(Task task) {
            bool result = false;
            
            for (int i = 0; i < task.Jobs.Count; i++) task.Jobs[i].CreateRemote();

            GetJobApplicationDocument();
            
            // Check task weight
            // ...
            
            // Store workflow XML in HDFS file system
            // "http://sb-10-10-14-12.lab14.sandbox.ciop.int:14000/tmp/portal/testdir?op=list&user.name=floeschau"            
            
            // ...
            

            // Step 1: Build workflow XML ------------------------------------------------------------------------------
            
            string taskBasePath = GetTaskBasePath(task);
            string url;
            
            string processingUserForTask = (ProcessingUser == null ? task.Username : ProcessingUser.Replace("$(USER)", task.Username));
            
            context.AddDebug(1, String.Format("Create Oozie workflow document at {0}", taskBasePath));
            try {
                result = (SendFileToHdfs(String.Format("{0}/workflow.xml", taskBasePath), processingUserForTask, BuildWorkflowXml(task)) == HttpStatusCode.Created);
            } catch (Exception e) {
                context.AddError(String.Format("Could not create Oozie workflow for task {0}: {1}", task.Identifier, e.InnerException == null ? e.Message : e.InnerException.Message));
                return false;
            }
            
            if (result) {
                context.AddDebug(1, String.Format("Create Oozie workflow parameter document at {0}", taskBasePath));
                try {
                    result = (SendFileToHdfs(String.Format("{0}/workflow-params.xml", taskBasePath), processingUserForTask, BuildWorkflowParamsXml(task)) == HttpStatusCode.Created);
                } catch (Exception e) {
                    context.AddError(String.Format("Could not create Oozie workflow parameters for task {0}: {1}", task.Identifier, e.InnerException == null ? e.Message + " " + e.StackTrace : e.InnerException.Message));
                    return false;
                }
            }

            SendFileToHdfs(String.Format("{0}/start", TaskBaseDirectory.Replace("$(UID)", task.Identifier)), processingUserForTask, (new UTF8Encoding()).GetBytes(String.Format("{0}/workflow.xml", taskBasePath)));
            
            if (result) {
                context.AddDebug(1, String.Format("Create Oozie input files {0}", taskBasePath));
                try {
                    for (int i = 0; i < task.Jobs.Count; i++) {
                        Job job = task.Jobs[i];
                        if (job.Dependencies.Count != 0) continue;
                        result = (SendFileToHdfs(String.Format("{0}/jobinputfiles-{1}", taskBasePath, job.Name), processingUserForTask, BuildJobInputFilesFile(job)) == HttpStatusCode.Created);
                    }
                } catch (Exception e) {
                    context.AddError(String.Format("Could not create Oozie input files for task {0}: {1}", task.Identifier, e.InnerException == null ? e.Message : e.InnerException.Message));
                    return false;
                }
            }

            //context.AddDebug(3, "TASK-1 Workflow: " + stringWriter.ToString());
            
            if (!result) {
                context.AddError(String.Format("Could not create Oozie workflow for task {0}: wrong response status code", task.Identifier));
                return false;
            }

            
            // Step 3: Build the Oozie job configuration document ------------------------------------------------------
            
            url = String.Format("{0}/v1/jobs", OozieBaseAddress);
            context.AddDebug(1, String.Format("Create Oozie job at {0} with base directory {1}{2}", url, HdfsNameNode, taskBasePath));

            //request.Credentials = new NetworkCredential("floeschau", "floeschau");
            
            string s = null;
            try {
                HttpWebResponse response = SendRequestToOozie(url, "POST", "application/xml; charset=UTF-8", BuildOozieConfigurationXml(task, null));
                
                s = new StreamReader(response.GetResponseStream()).ReadToEnd();
                context.AddDebug(3, String.Format("Server returned: {0}", s));
            } catch (Exception e) {
                context.AddError(String.Format("Could not send Oozie process configuration for task {0}: {1}", task.Identifier, e.InnerException == null ? e.Message : e.InnerException.Message));
                string errorMessage = "Could not create Oozie process for task " + task.Identifier;
                string errorDetail = null;
                
                XmlDocument errorDoc = new XmlDocument();
                errorDoc.Load(e.Response.GetResponseStream());
                XmlElement detailElem = errorDoc.SelectSingleNode("//detail") as XmlElement;
                if (detailElem != null) errorDetail = detailElem.InnerXml;
                
                if (errorDetail != null) errorMessage += ": " + errorDetail;

                task.ProcessingInformation.MessageCode = TaskMessageCode.Other;
                task.ProcessingInformation.MessageText = errorMessage;

                context.AddError(errorMessage, task.GetMessageClass(task.ProcessingInformation.MessageCode));
                return false;
            }

            // response looks like: {"id":"0000010-120606024604984-oozie-oozi-W"}                
            Match match = Regex.Match(s, "^\\s*^\\{[\\s]*([\"'])([^\"']+)\\1\\s*:\\s*([\"'])([^\"']*)\\3\\s*\\}\\s*$");
            
            string remoteId = null;
            if (match.Success && match.Groups[2].Value == "id") {
                remoteId = match.Groups[4].Value;
                context.AddDebug(1, String.Format("Oozie job identifier is {0}", remoteId));
                task.RemoteIdentifier = remoteId;
            }

            if (!result || remoteId == null) {
                context.AddError("Could not understand server response");
                return false;
            }
            
            //RemoteId = GetJson
            
            return result;
        }*/
        
        //---------------------------------------------------------------------------------------------------------------------

        public override bool CanStartTask(Task task) {
            GetJobApplicationDocument();

            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override bool StartTask(Task task) {
            bool result = false;
            
            try {
    
                if (task.RemoteIdentifier == null) {
                    context.AddError("Task not not yet created");
                    return false;
                } else {
                    context.AddWarning(String.Format("Start Oozie job"));
                    string url = String.Format("{0}/v1/job/{1}?action=start", OozieBaseAddress, task.RemoteIdentifier);
                    context.AddDebug(1, String.Format("Start Oozie job at {0}", url));
                    HttpWebResponse response = SendRequestToOozie(url, "PUT", null, null);
                    context.AddDebug(3, "Repsonse status: " + response.StatusCode);
                    result = (response.StatusCode == HttpStatusCode.OK);
                    
                }
            } catch (Exception e) {
                context.AddError("Could not submit task " + task.Identifier + "["+ task.RemoteIdentifier +"] on Cloud Appliance [" + OozieBaseAddress + "]: " + (e.InnerException == null ? e.Message : e.InnerException.Message));
                return false;
            }
            
            return result;

        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override bool StopTask(Task task) {
            bool result = false;
            try {
                if (task.RemoteIdentifier == null) {
                    result = true;
                    context.AddDebug(3, "Task not aborted (no Oozie process associated)");
                } else {
                    string url = String.Format("{0}/v1/job/{1}?action=kill", OozieBaseAddress, task.RemoteIdentifier);
                    context.AddDebug(1, String.Format("Abort Oozie job at {0}", url));
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "PUT";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    context.AddDebug(3, "Repsonse status: " + response.StatusCode);
                    result = (response.StatusCode == HttpStatusCode.OK);
                    
                }
            } catch (Exception e) {
                context.AddError("Could not submit task " + task.Identifier + " on Grid engine: " + (e.InnerException == null ? e.Message : e.InnerException.Message));
                return false;
            }
            
            return result;

        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override bool GetTaskStatus(Task task) {
            /*
            task.ProcessingInformation.Status = newStatus;
            task.ProcessingInformation.EndTime = dt.ToUniversalTime();
            task.ProcessingInformation.StatusMessage = elem.InnerXml;
            task.ProcessingInformation.StatusMessageTime = dt.ToUniversalTime();
            task.ProcessingInformation.StatusMessageType = MessageType.Error;
            */
            //BuildWorkflowXml(task); // just here for test            
            bool result = false;
            try {
                if (task.RemoteIdentifier == null) {
                    result = true;
                    context.AddDebug(3, "Task not active (no Oozie process associated)");
                } else {
                    string url = String.Format("{0}/v1/job/{1}?show=info", OozieBaseAddress, task.RemoteIdentifier);
                    context.AddDebug(1, String.Format("Get Oozie job status at {0}", url));
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    context.AddDebug(3, "Repsonse status: " + response.StatusCode);
                    result = (response.StatusCode == HttpStatusCode.OK);
                    
                    string s = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    context.AddDebug(3, String.Format("Server returned: {0}", s));
                    
                    // !!! TODO: parse JSON more generically:
                    result = true;
                    string statusStr = null;
                    Match match = Regex.Match(s, "\"status\" *: *\"([^\"]+)\"");
                    if (match.Success) {
                        statusStr = match.Groups[1].Value;
                        task.ActualStatus = StringToStatus(statusStr);
                        context.AddDebug(3, "task status = " + task.ActualStatus); 
                    } else {
                        result = false;
                    }
                    match = Regex.Match(s, "\"endTime\" *: *\"([^\"]+)\"");
                    DateTime dt = DateTime.MinValue;
                    if (match.Success) {
                        result &= DateTime.TryParse(match.Groups[1].Value, out dt);
                        task.EndTime = dt.ToUniversalTime();
                        context.AddDebug(3, "STATUS: " + statusStr + " " + dt.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\Z"));
                    } else {
                        result = false;
                    }
                    
                    int pos = s.IndexOf("\"actions\"");
                    string jobsStr = s.Substring(pos);
                    
                    context.AddDebug(3, "RESULT = " + result);

                    MatchCollection matches = Regex.Matches(jobsStr, "\\{[^\\}]+\\}");
                    foreach (Match jobMatch in matches) {
                        context.AddDebug(3, "job Match: " + jobMatch.Value); 
                        string name = null;
                        match = Regex.Match(jobMatch.Value, "\"name\" *: *\"([^\"]+)\"");
                        if (match.Success) {
                            name = match.Groups[1].Value;
                        } else {
                            result = false;
                        }
                        context.AddDebug(3, "RESULT(1) = " + result);
                        Job job = task.Jobs[name];
                        if (job == null) continue;
                        
                        statusStr = null;
                        match = Regex.Match(jobMatch.Value, "\"status\" *: *\"([^\"]+)\"");
                        if (match.Success) {
                            statusStr = match.Groups[1].Value;
                            job.Status = StringToStatus(statusStr);
                            context.AddDebug(3, "job " + name + " status = " + job.Status); 
                        } else {
                            result = false;
                        }
                        context.AddDebug(3, "RESULT(2) = " + result);
                        match = Regex.Match(jobMatch.Value, "\"startTime\" *: *\"([^\"]+)\"");
                        if (match.Success) {
                            result &= DateTime.TryParse(match.Groups[1].Value, out dt);
                            job.StartTime = dt.ToUniversalTime();
                        } else {
                            result = false;
                        }
                        context.AddDebug(3, "RESULT(3) = " + result);
                        match = Regex.Match(jobMatch.Value, "\"endTime\" *: *\"([^\"]+)\"");
                        if (match.Success) {
                            result &= DateTime.TryParse(match.Groups[1].Value, out dt);
                            job.EndTime = dt.ToUniversalTime();
                        }
                        context.AddDebug(3, "RESULT(4) = " + result);
                        match = Regex.Match(jobMatch.Value, "\"errorMessage\" *: *\"([^\"]+)\"");
                        if (match.Success) {
                            job.StatusMessageType = MessageType.Error;
                            job.StatusMessage = match.Groups[1].Value;
                        }
                        context.AddDebug(3, "RESULT(5) = " + result);
                        context.AddDebug(3, "JOB-" + name + " " + statusStr + " " + result);
                        job.GetStatus();
                    }
                    
                }

            } catch (Exception e) {
                context.AddError("Could not get details of task " + task.Identifier + " from cloud controller: " + (e.InnerException == null ? e.Message : e.InnerException.Message));
                return false;
            }
            
            return result;

        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override bool DestroyTask(Task task) {
            return StopTask(task);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override bool CreateJob(Job job) {
            // Job not created individually. Method must return true.
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override bool UpdateJob(Job job) {
            return false;
       }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override bool StartJob(Job job) {
            Task task = job.GetTask();
            
            bool result = false;
            
            try {
                if (task.RemoteIdentifier == null) {
                    context.AddError("Task not not yet created");
                    return false;
                }
                
                if (job.Status != ProcessingStatus.Completed && job.Status != ProcessingStatus.Failed) {
                    context.AddError("Only completed or failed jobs can be restarted");
                    return false;
                }
                
                string url = String.Format("{0}/v1/job/{1}?action=rerun", OozieBaseAddress, task.RemoteIdentifier);
                context.AddDebug(1, String.Format("Restart Oozie job ({0}) at {1}", job.Name, url));
                HttpWebResponse response = SendRequestToOozie(url, "PUT", "application/xml; charset=UTF-8", BuildOozieConfigurationXml(task, job));
                string s = new StreamReader(response.GetResponseStream()).ReadToEnd();
                context.AddDebug(3, String.Format("Server returned: {0}", s));
                context.AddDebug(3, "Repsonse status: " + response.StatusCode);
                result = (response.StatusCode == HttpStatusCode.OK);
                
            } catch (Exception e) {
                context.AddError(String.Format("Could not restart job \"{0}\" of task {1}: {2}", job.Name, task.Identifier, e.InnerException == null ? e.Message : e.InnerException.Message));
                return false;
            }
            
            return result;

        }

        //---------------------------------------------------------------------------------------------------------------------

        public override bool SuspendJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override bool ResumeJob(Job job) {
            return false;
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        public override bool StopJob(Job job) {
            return false;
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        public override bool CompleteJob(Job job) {
            return false;
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        public override bool GetJobStatus(Job job) {
            // Job status is received in GetTaskStatus. Method must return true so that Job properties are updated correctly.
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override bool GetTaskResult(Task task) {
            /*List<DataSetInfo> outputFiles = new List<DataSetInfo>();

            XmlDocument resultDoc = new XmlDocument();
            DownloadUrl = String.Format("{0}{1}/$(REMOTEID)/", HdfsAccessPoint, 
            // http://sb-10-16-10-50.dev2.terradue.int:50070/webhdfs/v1/tmp/sandbox/run/0000006-130917091642732-oozie-oozi-W/results.xml?op=OPEN
            resultDoc.Load(ReceiveXmlFromHdfs(DownloadUrl.Replace("$(UID)", task.Identifier).Replace("$()", "XXX"))); // TODO replace?
            
            XmlNodeList fileNodes = resultDoc.SelectNodes("/metalink/files/file");
            foreach (XmlNode fileNode in fileNodes) {
                string identifier = (fileNode.HasAttribute("name") ? fileNode.Attributes["name"].Value : null);
                XmlElement sizeElem = fileNode.SelectSingleNode("size") as XmlElement;
                int size = -1;
                if (sizeElem != null && !Int32.TryParse(sizeStr, out size)) size = -1;
                DataSetInfo dsi = new DataSetInfo(resource, identifier, size, task.EndTime);
                outputFiles.Add(dsi);
            }
            
            task.ProcessingInformation.OutputFiles = outputFiles.ToArray();*/
            
            return true;
        }

        
        //---------------------------------------------------------------------------------------------------------------------
        
        //! Set Oozie parameter according to appliance attributes.
        public void SetDefaultFromAppliance() {
            if ( Appliance != null && Appliance.Hostname != null ){
               Hostname = Appliance.Hostname;
               ProcessingUser = Appliance.Username;
               OozieBaseAddress = String.Format("http://{0}:11000/oozie", Hostname);
               JobTracker = String.Format("{0}:8021", Hostname);
               HdfsNameNode = String.Format("hdfs://{0}:8020", Hostname);
               HdfsAccessPoint = String.Format("http://{0}:14000", Hostname);            
               JobApplicationDocumentUrl = String.Format("http://{0}:88/application.xml", Hostname);
            }
            return;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
                
        protected byte[] BuildWorkflowXml(Task task) {
            
            // Build workflow graph including fork and join nodes
            OozieNode[] nodes = new OozieNode[task.Jobs.Count];
            for (int i = 0; i < task.Jobs.Count; i++) {
                Job job = task.Jobs[i];
                OozieNode node = new OozieNode();
                nodes[i] = node;
                node.Name = job.Name;
            }
            for (int i = 0; i < task.Jobs.Count; i++) {
                OozieNode node = nodes[i];
                Job job = task.Jobs[i];
                node.Predecessors = new OozieNode[job.Dependencies.Count];
                for (int j = 0; j < job.Dependencies.Count; j++) {
                    for (int k = 0; k < i; k++) if (job.Dependencies[j] == task.Jobs[k]) node.Predecessors[j] = nodes[k];
                }
                int count = 0;
                for (int j = i + 1; j < task.Jobs.Count; j++) {
                    for (int k = 0; k < task.Jobs[j].Dependencies.Count; k++) if (task.Jobs[j].Dependencies[k] == job) count++;
                }
                node.Successors = new OozieNode[count];
                count = 0;
                for (int j = i + 1; j < task.Jobs.Count; j++) {
                    for (int k = 0; k < task.Jobs[j].Dependencies.Count; k++) if (task.Jobs[j].Dependencies[k] == job) node.Successors[count++] = nodes[j];
                }
            }
            
            int forkCount = 0, joinCount = 0;
            int startCount = 0, endCount = 0;
            for (int i = 0; i < nodes.Length; i++) {
                OozieNode node = nodes[i];
                if (node.Predecessors.Length == 0) {
                    node.AtStart = true;
                    startCount++;
                } else if (node.Predecessors.Length > 1) {
                    OozieNode joinNode = new OozieNode();
                    joinNode.Name = "join-" + (++joinCount);
                    joinNode.Join = true;
                    joinNode.Predecessors = node.Predecessors;
                    for (int j = 0; j < joinNode.Predecessors.Length; j++) for (int k = 0; k < joinNode.Predecessors[j].Successors.Length; k++) if (joinNode.Predecessors[j].Successors[k] == node) joinNode.Predecessors[j].Successors[k] = joinNode;
                    joinNode.Successors = new OozieNode[] { node };
                    node.Predecessors = new OozieNode[] { joinNode };
                }
                if (node.Successors.Length == 0) {
                    node.AtEnd = true;
                    endCount++;
                } else if (node.Successors.Length > 1) {
                    OozieNode forkNode = new OozieNode();
                    forkNode.Name = "fork-" + (++forkCount);
                    forkNode.Fork = true;
                    forkNode.Predecessors = new OozieNode[] { node };
                    forkNode.Successors = node.Successors;
                    for (int j = 0; j < forkNode.Successors.Length; j++) for (int k = 0; k < forkNode.Successors[j].Predecessors.Length; k++) if (forkNode.Successors[j].Predecessors[k] == node) forkNode.Successors[j].Predecessors[k] = forkNode;
                    node.Successors = new OozieNode[] { forkNode };
                }
            }
            
            /*for (int i = 0; i < nodes.Length; i++) {
                OozieNode node = nodes[i];
                string a = "JOB " + node.Name; 
                a += ", PREDECESSORS:";
                for (int j = 0; j < node.Predecessors.Length; j++) a += " " + node.Predecessors[j].Name;
                a += ", SUCCESSORS:";
                for (int j = 0; j < node.Successors.Length; j++) a += " " + node.Successors[j].Name;
                context.AddDebug(3, a); 
            }*/
            
            
            string taskBasePath = GetTaskBasePath(task);

            StringWriter stringWriter = new StringWriter();
            MonoXmlWriter writer = MonoXmlWriter.Create(stringWriter);
            //writer.WriteStartDocument();
            writer.WriteStartElement("workflow-app");
            writer.WriteAttributeString("xmlns", "uri:oozie:workflow:0.1");
            writer.WriteAttributeString("name", String.Format("{0}-{1}", task.Service.Identifier, task.Identifier));
            
            writer.WriteStartElement("start");
            writer.WriteAttributeString("to", "prepare");
            writer.WriteEndElement(); // </start>

            // prepare job
            writer.WriteStartElement("action");
            writer.WriteAttributeString("name", "prepare");
            writer.WriteStartElement("map-reduce");
            writer.WriteElementString("job-tracker", "${jobTracker}");
            writer.WriteElementString("name-node", "${nameNode}");
            writer.WriteStartElement("prepare");
            writer.WriteStartElement("mkdir");
            writer.WriteAttributeString("path", String.Format("${{nameNode}}{0}", TaskProcessingDirectory));
            writer.WriteEndElement(); // </mkdir>
            writer.WriteEndElement(); // </prepare>
            writer.WriteStartElement("streaming");
            writer.WriteElementString("mapper", "ciop-wf-prepare");
            writer.WriteElementString("env", String.Format("ciop_wf_run_root={0}", TaskProcessingDirectory));
            writer.WriteElementString("env", String.Format("ciop_wf_run_id=${{wf:id()}}"));
            writer.WriteElementString("env", String.Format("ciop_wf_jobs_params=${{nameNode}}{0}/workflow-params.xml", TaskBaseDirectory.Replace("$(UID)", task.Identifier)));
            foreach (KeyValuePair<string, string> env in EnvironmentVariables) {
                writer.WriteElementString("env", String.Format("{0}={1}", env.Key, env.Value.Replace("$(JOB)", "prepare")));
            }
            writer.WriteEndElement(); // </streaming>
            writer.WriteStartElement("configuration");
            AddProperty(writer, "mapred.input.dir", String.Format("{0}/start", TaskBaseDirectory.Replace("$(UID)", task.Identifier)));
            AddProperty(writer, "mapred.output.dir", String.Format("{0}/prepare.out", TaskProcessingDirectory.Replace("$(UID)", task.Identifier)));
            AddProperty(writer, "mapred.reduce.tasks", "0");
            AddProperty(writer, "ciop.wf.jobs.params", String.Format("${{nameNode}}{0}/workflow-params.xml", TaskBaseDirectory.Replace("$(UID)", task.Identifier)));
            writer.WriteEndElement(); // </configuration>
            writer.WriteEndElement(); // </map-reduce>
            writer.WriteStartElement("ok");
            if (startCount == 1) {
                for (int i = 0; i < nodes.Length; i++) if (nodes[i].AtStart) writer.WriteAttributeString("to", nodes[i].Name);
            } else {
                writer.WriteAttributeString("to", "start-fork");
            }
            writer.WriteEndElement(); // </ok>
            writer.WriteStartElement("error");
            writer.WriteAttributeString("to", "fail");
            writer.WriteEndElement(); // </error>
            writer.WriteEndElement(); // </action>
            
            if (startCount != 1) {
                writer.WriteStartElement("fork");
                writer.WriteAttributeString("name", "start-fork");
                for (int i = 0; i < nodes.Length; i++) {
                    if (nodes[i].AtStart) {
                        writer.WriteStartElement("path");
                        writer.WriteAttributeString("start", nodes[i].Name);
                        writer.WriteEndElement(); // </path>
                    }
                }
                writer.WriteEndElement(); // </fork>
            }

            for (int i = 0; i < nodes.Length; i++) {
                OozieNode node = nodes[i];
                Job job = task.Jobs[i];
                
                OozieNode predecessor = node;
                while (predecessor.Predecessors.Length == 1 && predecessor.Predecessors[0].Join) predecessor = predecessor.Predecessors[0];
                while (predecessor.Join) {
                    writer.WriteStartElement("join");
                    writer.WriteAttributeString("name", predecessor.Name);
                    predecessor = predecessor.Successors[0];
                    writer.WriteAttributeString("to", predecessor.Name);
                    writer.WriteEndElement(); // </join>
                }
                
                writer.WriteStartElement("action");
                writer.WriteAttributeString("name", node.Name);
                writer.WriteStartElement("map-reduce");
                writer.WriteElementString("job-tracker", "${jobTracker}");
                writer.WriteElementString("name-node", "${nameNode}");
                writer.WriteStartElement("streaming");
                RefreshMaxNodes(job);
                if (job.MaxNodes == 1) {
                    writer.WriteElementString("mapper", "/bin/cat");
                    writer.WriteElementString("reducer", GetJobApplication(job));
                } else {
                    writer.WriteElementString("mapper", GetJobApplication(job));
                    writer.WriteElementString("reducer", "/bin/cat");
                }
                writer.WriteElementString("env", String.Format("ciop_job_nodeid={0}", job.JobType));
                writer.WriteElementString("env", String.Format("ciop_wf_run_root={0}", TaskBaseDirectory.Replace("$(UID)", task.Identifier)));
                writer.WriteElementString("env", String.Format("ciop_job_data_dir={0}/{1}/data", TaskProcessingDirectory, job.Name));
                foreach (KeyValuePair<string, string> env in EnvironmentVariables) {
                    writer.WriteElementString("env", String.Format("{0}={1}", env.Key, env.Value.Replace("$(JOB)", job.Name)));
                }

                if (job.Publishes) {
                    string urlStr;
                    string portalPublishUrl = context.ResultMetadataUploadUrl;
                    if (portalPublishUrl != null) portalPublishUrl = portalPublishUrl.Replace("$(UID)", task.Identifier);
                    if (task.PublishServer == null) {
                        if /*(portalPublishUrl == null)*/ (false) {
                            //context.AddError("No publish server provided");
                            //return false;
                        } else {
                            urlStr = portalPublishUrl/* + ",[" + (task.Compression == null ? String.Empty : task.Compression) + "],*"*/;
                        }
    
                    } else {
                        urlStr = task.PublishServer.UploadUrl.Replace("$(UID)", task.Identifier)/* + ",[" + (task.Compression == null ? String.Empty : task.Compression) + "],*"*/;
                        if (portalPublishUrl != null) urlStr += portalPublishUrl;// urlStr += String.Format(";{0},[],*_browse.*;{0},[],*.rdf", portalPublishUrl);
                    }
                    writer.WriteElementString("env", String.Format("ciop_publish_location={0}", urlStr));
                }
                writer.WriteEndElement(); // </streaming>

                writer.WriteStartElement("configuration");
                AddProperty(writer, "mapred.input.dir", node.AtStart ? String.Format("{0}/jobinputfiles-{1}", taskBasePath, job.Name) : GetJobPredecessorOutputDirectories(job));
                AddProperty(writer, "mapred.output.dir", GetJobOutputDirectory(job));
                
                // Add default properties from application.xml
                AddJobDefaultProperties(job, writer);
                
                if (job.Publishes) {
                    AddProperty(writer, "mapred.reduce.tasks", "0");
                } else if (job.MaxNodes == 1) {
                    AddProperty(writer, "mapred.reduce.tasks", "1");
                } else {
                    AddProperty(writer, "mapred.map.max.attempts", "1");
                    AddProperty(writer, "mapred.reduce.max.attempts", "1");
                }
                                
                writer.WriteEndElement(); // </configuration>
                writer.WriteEndElement(); // </map-reduce>
                writer.WriteStartElement("ok");
                if (node.AtEnd) writer.WriteAttributeString("to", "clean");
                else writer.WriteAttributeString("to", node.Successors[0].Name);
                writer.WriteEndElement(); // </ok>
                writer.WriteStartElement("error");
                writer.WriteAttributeString("to", "fail");
                writer.WriteEndElement(); // </error>
                writer.WriteEndElement(); // </action>
                
                if (!node.AtEnd) {
                    OozieNode successor = node.Successors[0];
                    if (successor.Fork) {
                        writer.WriteStartElement("fork");
                        writer.WriteAttributeString("name", successor.Name);
                        for (int j = 0; j < successor.Successors.Length; j++) {
                            writer.WriteStartElement("path");
                            writer.WriteAttributeString("start", successor.Successors[j].Name);
                            writer.WriteEndElement(); // </path>
                        }
                        writer.WriteEndElement(); // </fork>
                    }
                }
            }
            
            writer.WriteStartElement("action");
            writer.WriteAttributeString("name", "clean");
            writer.WriteElementString("fs", null);
            writer.WriteStartElement("ok");
            writer.WriteAttributeString("to", "end");
            writer.WriteEndElement(); // </ok>
            writer.WriteStartElement("error");
            writer.WriteAttributeString("to", "fail");
            writer.WriteEndElement(); // </error>
            writer.WriteEndElement(); // </action>
            
            writer.WriteStartElement("kill");
            writer.WriteAttributeString("name", "fail");
            writer.WriteElementString("message", "Workflow Streaming Map/Reduce failed, error message[${wf:errorMessage(wf:lastErrorNode())}]");
            writer.WriteEndElement(); // </kill>
            
            writer.WriteStartElement("end");
            writer.WriteAttributeString("name", "end");
            writer.WriteEndElement(); // </end>

            writer.WriteEndElement(); // </workflow-app>
            

            context.AddDebug(3, "TASK-1 Workflow: " + stringWriter.ToString());

            byte[] data = (new UTF8Encoding()).GetBytes(stringWriter.ToString());
            writer.Close();
            
            return data;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /*protected byte[] BuildWorkflowParamsXml(Task task) {

            StringWriter stringWriter = new StringWriter();
            MonoXmlWriter writer = MonoXmlWriter.Create(stringWriter);
            //writer.WriteStartDocument();
            writer.WriteStartElement("workflow");
            
            for (int i = 0; i < task.Jobs.Count; i++) {
                Job job = task.Jobs[i];
                AddJobDefaultParameters(job);
                
                writer.WriteStartElement("node");
                writer.WriteAttributeString("id", job.Name);
                writer.WriteStartElement("parameters");
                for (int j = 0; j < job.Parameters.Count; j++) {
                    TaskParameter param = job.Parameters[j];
                    writer.WriteStartElement("parameter");
                    writer.WriteAttributeString("id", param.Name);
                    writer.WriteString(param.Value);
                    writer.WriteEndElement(); // </parameter>
                }
                writer.WriteEndElement(); // </parameters>
                writer.WriteEndElement(); // </node>
            }

            writer.WriteEndElement(); // </workflow>
            

            context.AddDebug(3, "TASK-2 Workflow Parameters: " + stringWriter.ToString());

            byte[] data = (new UTF8Encoding()).GetBytes(stringWriter.ToString());
            writer.Close();
            
            return data;
        }*/

        //---------------------------------------------------------------------------------------------------------------------
        
        protected byte[] BuildOozieConfigurationXml(Task task, Job job) {
            string taskBasePath = GetTaskBasePath(task);

            StringWriter stringWriter = new StringWriter();
            MonoXmlWriter writer = MonoXmlWriter.Create(stringWriter);
            //writer.WriteStartDocument();
            writer.WriteStartElement("configuration");
            AddProperty(writer, "user.name", ProcessingUser == null ? task.Username : ProcessingUser.Replace("$(USER)", task.Username));
            AddProperty(writer, "oozie.wf.application.path", String.Format("{0}{1}", HdfsNameNode, taskBasePath));
            // Proparty to indicate the library path
            AddProperty(writer, "oozie.libpath", "/ciop/lib");
            AddProperty(writer, "oozie.use.system.libpath", "true");
            AddProperty(writer, "jobTracker", JobTracker);
            AddProperty(writer, "queueName", QueueName);
            AddProperty(writer, "nameNode", HdfsNameNode);
            if (job != null) {
                string skip = null;
                for (int i = 0; i < task.Jobs.Count; i++) {
                    if (task.Jobs[i] != job && task.Jobs[i].Status == ProcessingStatus.Completed) {
                        if (skip == null) skip = String.Empty; else skip += ",";
                        skip += job.Name;
                    }
                }
                if (skip != null) AddProperty(writer, "oozie.wf.rerun.skip.nodes", skip);
            }
            writer.WriteEndElement(); // </configuration>

            context.AddDebug(3, "TASK-3 Configuration: " + stringWriter.ToString());

            byte[] data = (new UTF8Encoding()).GetBytes(stringWriter.ToString());
            writer.Close();
            
            return data;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected void AddProperty(XmlWriter writer, string name, string value) {
            writer.WriteStartElement("property");
            writer.WriteElementString("name", name);
            writer.WriteElementString("value", value);
            writer.WriteEndElement(); // </property>
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /*protected byte[] BuildJobInputFilesFile(Job job) {
            context.AddDebug(3, "BJIFF-0");
            string input = job.Parameters["inputProduct"].Value;
            context.AddDebug(3, "BJIFF-3");
            if (input == null) input = String.Empty; else input = input.Replace("|", "\n");
            
            context.AddDebug(3, "INPUT FILES: " + input);
            
            byte[] data = (new UTF8Encoding()).GetBytes(input);
            
            return data;
        }*/

        //---------------------------------------------------------------------------------------------------------------------
        
        protected HttpStatusCode SendFileToHdfsHoop(string path, string user, byte[] content) {
            string url = String.Format("{0}{1}{2}?op=create&user.name={3}", HdfsAccessPoint, path.StartsWith("/") ? String.Empty : "/", path, user);
            context.AddDebug(3, url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/xml; charset=UTF-8";
            request.ContentLength = content.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(content, 0, content.Length);
            requestStream.Close();
            
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.StatusCode;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected HttpWebResponse SendRequestToOozie(string url, string method, string contentType, byte[] content) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            if ((method == "POST" || method == "PUT") && content != null) {
                if (contentType != null) request.ContentType = contentType;
                request.ContentLength = content.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(content, 0, content.Length);
                requestStream.Close();
            }
            context.AddDebug(3, "SRTO " + url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected HttpStatusCode SendFileToHdfs(string path, string user, byte[] content) {
            try {
            string url = String.Format("{0}{1}{2}?op=create&user.name={3}", HdfsAccessPoint, path.StartsWith("/") ? String.Empty : "/", path, user);
            context.AddDebug(3, url);
 
            // First request (empty)
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "PUT";
            request.AllowAutoRedirect = false;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            url = response.Headers["Location"]; // Location header value is URL for second request
            context.AddDebug(3, url);
            
            // Second request (containing file)
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "PUT";
            request.ContentType = "application/xml; charset=UTF-8";
            request.ContentLength = content.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(content, 0, content.Length);
            requestStream.Close();
            response = (HttpWebResponse)request.GetResponse();
            url = response.Headers["Location"];
            
            return response.StatusCode;
            } catch (Exception e) {
                context.AddError("HDFS exception " + e.Message);
                return HttpStatusCode.Created;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected string GetTaskBasePath(Task task) {
            return String.Format("{1}{0}", TaskBaseDirectory.Replace("$(UID)", task.Identifier), TaskBaseDirectory.StartsWith("/") ? String.Empty : "/");
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected void GetJobApplicationDocument() {
            if (JobApplicationDocument != null) return;
            
            JobApplicationDocument = new XmlDocument();
            JobApplicationDocument.Load(JobApplicationDocumentUrl);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        protected string GetJobApplication(Job job) {
            if (JobApplicationDocument == null) return null;
            XmlElement executableElem = JobApplicationDocument.SelectSingleNode(String.Format("/application/jobTemplates/jobTemplate[@id='{0}']/streamingExecutable", job.JobType)) as XmlElement;
            if (executableElem == null) return null;
            return executableElem.InnerXml;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void AddJobDefaultProperties(Job job, XmlWriter writer) {
            if (JobApplicationDocument == null) return;
            foreach (XmlNode node in JobApplicationDocument.SelectNodes(String.Format("/application/jobTemplates/jobTemplate[@id='{0}']/defaultJobconf/property", job.JobType))) {
                XmlElement paramElem = node as XmlElement;
                if (paramElem == null || !paramElem.HasAttribute("id")) continue;
                AddProperty(writer, paramElem.Attributes["id"].Value, paramElem.InnerXml);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void RefreshMaxNodes(Job job) {
            if (JobApplicationDocument == null) return;
            XmlElement elem = JobApplicationDocument.SelectSingleNode(String.Format("/application/jobTemplates/jobTemplate[@id='{0}']/defaultJobconf/property[@id='ciop.job.max.tasks']", job.JobType)) as XmlElement;
            if (elem != null) {
                int value;
                if (Int32.TryParse(elem.InnerText, out value)) job.MaxNodes = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /*protected void AddJobDefaultParameters(Job job) {
            if (JobApplicationDocument == null) return;
            foreach (XmlNode node in JobApplicationDocument.SelectNodes(String.Format("/application/jobTemplates/jobTemplate[@id='{0}']/defaultParameters/parameter", job.JobType))) {
                XmlElement paramElem = node as XmlElement;
                if (paramElem == null || !paramElem.HasAttribute("id")) continue;
                string name = paramElem.Attributes["id"].Value;
                string value = paramElem.InnerXml;
                if (!job.Parameters.Contains(name)) job.Parameters.Add(name, value);
                else if (job.Parameters["name"].Value == null || job.Parameters["name"].Value == String.Empty) job.Parameters["name"].Value = value;
            }
        }*/

        //---------------------------------------------------------------------------------------------------------------------

        protected string GetJobOutputDirectory(Job job) {
            return String.Format("{0}/{1}/output", TaskProcessingDirectory, job.Name);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected string GetJobPredecessorOutputDirectories(Job job) {
            string result = String.Empty;
            for (int i = 0; i < job.Dependencies.Count; i++) {
                if (i != 0) result += ",";
                result += String.Format("{0}/{1}/output", TaskProcessingDirectory, job.Dependencies[i].Name);
            }
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static int StringToStatus(string s) {
            if (s == null) return 0;
            switch (s.ToLower()) {
                case "running" :
                    return ProcessingStatus.Active;
                case "failed" :
                case "killed" :
                case "error" :
                    return ProcessingStatus.Failed;
                case "succeeded" :
                case "ok" :
                    return ProcessingStatus.Completed;
            }
            return 0;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override void Delete() {
            base.Delete();
            context.Execute(String.Format("DELETE FROM ooziecr WHERE id={0};", Id));
        }
        
		void LoadLocalOozieComputingResource (CloudAppliance appliance)
		{
			this.Hostname = Dns.GetHostName();


			// Hadoop

		}

		#region implemented abstract members of CloudComputingResource
		[DataMember]
		public override List<CloudComputingDriveInfo> DrivesInfo {
			get {
				List<CloudComputingDriveInfo> disk = new List<CloudComputingDriveInfo>();
                disk.Add(new CloudComputingDriveInfo("Home", "/home/"+this.Appliance.Username));
				disk.Add(new CloudComputingDriveInfo("Application", "/application"));
                disk.Add(new HDFSCloudComputingDriveInfo(this.Appliance.Hostname,this.Appliance.Username));
				return disk;
			}
		}
		#endregion
    }

    public class OozieNode {
        public string Name;
        public bool Fork, Join;
        public OozieNode[] Predecessors;
        public OozieNode[] Successors;
        public bool Marked, Written, AtStart, AtEnd;
        
        public void InsertForkAfter(OozieNode node) {
        }
        
        public void InsertJoinBefore(OozieNode node) {
        }
        
    }

}

