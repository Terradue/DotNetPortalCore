using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
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



    public delegate void JobOperationCallbackType(Job job);



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a task's collection of jobs</summary>
    public class JobCollection {
        private IfyContext context;
        private Task task;
        private Dictionary<string, Job> dict = new Dictionary<string, Job>();
        private Job[] items = new Job[0];

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of jobs in the collection.</summary>
        public int Count {
            get { return items.Length; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the job with the specified name.</summary>
        public Job this[string name] {
            get { 
                if (dict.ContainsKey(name)) return dict[name];
                else return null;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the job at the specified position in the list.</summary>
        public Job this[int index] { 
            get { return items[index]; } 
            set { items[index] = value; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new JobCollection instance referring to the specified task.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="task">the task to which the job colletion belongs</param>
        */
        public JobCollection(IfyContext context, Task task) {
            this.context = context;
            this.task = task;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a new job to the job collection.</summary>
        /*!
        /// <param name="name">the name of the job</param>
        /// <param name="jobType">the job type (must be known by the Grid engine)</param>
        /// <returns>the created Job object.</returns>
        */
        public Job Add(string name, string jobType) {
            Job job = Job.ForTask(context, task, name, jobType);
            if (dict.ContainsKey(name)) {
                throw new Exception("Duplicate job name \"" + name + "\"");
                return job;
            }
            dict.Add(name, job);
            Array.Resize(ref items, items.Length + 1);
            items[items.Length - 1] = job;
            return job;
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        public Job Add(Job job) {
            dict.Add(job.Name, job);
            Array.Resize(ref items, items.Length + 1);
            items[items.Length - 1] = job;
            return job;
        }


        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the job collection contains a job with the specified name.</summary>
        /*!
        /// <param name="key">the name of the job</param>
        /// <returns>true if the job collection contains such a job</returns>
        */
        public bool ContainsKey(string key) {
            return dict.ContainsKey(key);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the job collection contains the specified Job instance.</summary>
        /*!
        /// <param name="value">the job reference</param>
        /// <returns>true if the job collection contains the job.</returns>
        */
        public bool ContainsValue(Job value) {
            return dict.ContainsValue(value);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the position of the specified Job instance in the job collection.</summary>
        /*!
        /// <param name="value">the job reference</param>
        /// <returns>the position of the job if it is contained in the list, otherwise -1</returns>
        */
        public int IndexOf(Job job) {
            for (int i = 0; i < items.Length; i++) if (items[i] == job) return i;
            return -1;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sorts the contained jobs so that all input jobs of a job are defined before it.</summary>
        public void Sort() {
            Dictionary<string, bool> jobs = new Dictionary<string, bool>(); // maps each job to a boolean value indicating that the job has been successfully positioned.
            
            foreach (KeyValuePair<string, Job> kvp in dict) jobs[kvp.Key] = false;
            for (int i = 0; i < items.Length; i++) {
                Job job = items[i];
                bool allInputJobsBefore = true;
                
                // If job at the current position has no input jobs or all input jobs defined before the current position,
                // leave it at its position and continue with next job
                for (int j = 0; j < job.Dependencies.Count; j++) if (!(allInputJobsBefore = jobs[job.Dependencies[j].Name])) break;
                if (allInputJobsBefore) {
                    jobs[job.Name] = true;
                    continue;
                }

                // If the job at the current position has an input job defined after the current position,
                // try place here the next available job with no input jobs or all its input jobs defined before the current 
                for (int j = i + 1; j < items.Length; j++) {
                    job = items[j];
                    allInputJobsBefore = true;
                    for (int k = 0; k < job.Dependencies.Count; k++) if (!(allInputJobsBefore = jobs[job.Dependencies[k].Name])) break;
                    if (allInputJobsBefore) {
                        for (int k = j; k > i; k--) items[k] = items[k - 1];
                        items[i] = job;
                        jobs[job.Name] = true;
                        break;
                    }
                }

                // If there is no job left that has all its input jobs defined before the current position, there are cyclic dependencies.
                // This indicates a wrong job creation routine and is a fatal error for the creation of the task.  
                if (!allInputJobsBefore) {
                    throw new Exception("There are cyclic job dependencies involving " + (items.Length - i) + " jobs.");
                    break;
                }
            }
            jobs.Clear();
        }
        
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    /// <summary>Represents an Ify processing job.</summary>
    /*!
      This class provides information on an Ify processing job including properties for execution and status details and methods for the execution control.
      An instance of the Job class refers to a Task object, its owner, and maintains list of the jobs on which it depends and of processing nodes on which the job execution is distributed. 
      A Job object sends requests to the Grid engine via the wsServer interface on the Grid engine.
    */
    [EntityTable("job", EntityTableConfiguration.Custom, NameField = "name")]
    public class Job : Entity, IFlowNode {
        
        public static string PublishJobType = "Publish";
        private ServiceOperationType operationType;
        
        private bool canChangeParameters;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the ID of the task to which the job belongs.</summary>
        [EntityDataField("id_task", IsForeignKey = true)]
        public int TaskId { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public Task Task { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the job type</summary>
        [EntityDataField("job_type")]
        public string JobType { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the maximum number of Grid nodes on which the job is split for its execution.</summary>
        [EntityDataField("max_nodes")]
        public int MaxNodes { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the minimum number of input arguments each Grid node in case of split execution.</summary>
        [EntityDataField("min_args")]
        public int MinArgumentsPerNode { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the job publishes its result on a server outside the Grid.</summary>
        [EntityDataField("publish")]
        public bool Publishes { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the job execution is interrupted after the cration of its enivironment on the processing node.</summary>
        [EntityDataField("forced_exit")]
        public bool ForcedExit { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string StatusUrl { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the current logical task status that is presented to the user.</summary>
        /*! 
            The logical status differs from the actual status if a task operation is pending.
            
            For example an aborted task is presented as <i>Created</i> to the user even if the actual abortion has not yet taken place on the Grid engine.
        */
        [EntityDataField("status")]
        public int Status { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets a value that allows to distinguish different executions of the same job</summary>
        [EntityDataField("seq_exec_id")]
        public string SequentialExecutionIdentifier { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time of the job submission. </summary>
        [EntityDataField("start_time")]
        public DateTime StartTime { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time of the job completion or failure. </summary>
        [EntityDataField("end_time")]
        public DateTime EndTime { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public void GetNodePosition(out IFlowNode[] nodesBefore, out IFlowNode[] nodesAfter) {
            nodesBefore = Dependencies.ToArray();
            nodesAfter = null;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected bool Loaded { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UID of the task to which the job belongs.</summary>
        protected string TaskIdentifier { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the Grid sesion ID of the task to which the job belongs.</summary>
        protected string TaskRemoteIdentifier { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the Grid sesion ID of the task to which the job belongs.</summary>
        protected bool TaskIsRunning { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the current actual job status. </summary>
        /*! 
            The actual status differs from the logical status if a task operation is pending.

            For example the actual status of a task may still be <i>Active</i> but its logical status is <i>Created</i> if the user aborted it.
        */
        //public int ActualStatus { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the job has started, but not yet ended on the Grid Engine.</summary>
        public bool Running {
            get { return Status >= ProcessingStatus.Active && Status < ProcessingStatus.Failed; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the job has finished sucessfully.</summary>
        public bool Finished {
            get { return Status >= ProcessingStatus.Completed && Status < ProcessingStatus.Deleted; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the job has finished sucessfully.</summary>
        public bool Failed {
            get { return Status >= ProcessingStatus.Failed && Status < ProcessingStatus.Completed; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the job is in paused state on the Grid engine. </summary>
        public bool Paused { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the request method for the status of the job </summary>
        public int StatusRequestMethod { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the collection of the job parameters.</summary>
        public ExecutionParameterSet ExecutionParameters { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the list of the predecessor jobs on which the job depends.</summary>
        public List<Job>/*Job[]*/ Dependencies { get; protected set; }
            
        // get { return dependencies/*.ToArray()*/; } // !!! if Dependencies.Add is called directly, dependencies are not ordered

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the list of the predecessor jobs on which the job depends.</summary>
        public string[] DependenciesAsStringArray {
            get { 
                string[] result = new string[Dependencies.Count];
                for (int i = 0; i < Dependencies.Count; i++) result[i] = Dependencies[i].Name;
                return result;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the list of the processing nodes on which the job is running.</summary>
        public List<NodeProcess> Nodes { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string InputFiles { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /*/// <summary>Gets the list of the predecessor jobs on which the job depends.</summary>
        public JobProcessing[] Nodes {
            get {
                JobProcessing[] result = new JobProcessing[Processings.Count];
                for (int i = 0; i < Processings.Count; i++) result[i] = Processings[i];
                return result;
            }
        }*/
        
        //---------------------------------------------------------------------------------------------------------------------

        // ! Gets the actual number of input arguments of the job.
        //protected int ArgumentCount { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>???</summary>
        public bool ShowParameters { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>???</summary>
        public bool ShowNodes { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the job has just been submitted or resubmitted.</summary>
        public bool Resubmitted { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        protected bool Ended { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the latest job status message.</summary>
        public string StatusMessage { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the time stamp of the latest job status message. </summary>
        public DateTime StatusMessageTime { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the message type of the latest job status message. </summary>
        public MessageType StatusMessageType { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the latest job debug message.</summary>
        public string DebugMessage { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the time stamp of the latest job debug message. </summary>
        public DateTime DebugMessageTime { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the message type of the latest job debug message. </summary>
        public MessageType DebugMessageType { get; set; }
        
        public int TotalArguments { get; set; }
        public int DoneArguments { get; set; }
        public int NodeCount { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string RelativeUrl {
            get {
                string url;
                IfyWebContext webContext = context as IfyWebContext;
                if (AccessLevel == EntityAccessLevel.Administrator && webContext != null && webContext.AdminRootUrl != null) url = "{2}/{3}/{5}/{6}/{1}";  
                else if (webContext != null && webContext.TaskWorkspaceRootUrl != null) url = "{4}/{5}/{6}/{1}";
                else url = "/tasks/jobs?id={0}";
                return String.Format(url, Id, Name, webContext.AdminRootUrl, EntityType.GetEntityType(typeof(Task)).Keyword, webContext.TaskWorkspaceRootUrl, TaskIdentifier, webContext.TaskWorkspaceJobDir);
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string DetailsRelativeUrl {
            get {
                string url;
                IfyWebContext webContext = context as IfyWebContext;
                if (AccessLevel == EntityAccessLevel.Administrator && webContext.AdminRootUrl != null) url = "{2}/{3}/{5}/{6}/{1}/details";  
                else if (webContext != null && webContext.TaskWorkspaceRootUrl != null) url = "{4}/{5}/{6}/{1}/details";
                else url = "/tasks/jobs/details?id={0}";
                return String.Format(url, Id, Name, webContext.AdminRootUrl, EntityType.GetEntityType(typeof(Task)).Keyword, webContext.TaskWorkspaceRootUrl, TaskIdentifier, webContext.TaskWorkspaceJobDir);
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string ParametersRelativeUrl {
            get {
                string url;
                IfyWebContext webContext = context as IfyWebContext;
                if (AccessLevel == EntityAccessLevel.Administrator && webContext.AdminRootUrl != null) url = "{2}/{3}/{5}/{6}/{1}/parameters";  
                else if (webContext != null && webContext.TaskWorkspaceRootUrl != null) url = "{4}/{5}/{6}/{1}/parameters";
                else url = "/tasks/jobs/parameters?id={0}";
                return String.Format(url, Id, Name, webContext.AdminRootUrl, EntityType.GetEntityType(typeof(Task)).Keyword, webContext.TaskWorkspaceRootUrl, TaskIdentifier, webContext.TaskWorkspaceJobDir);
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Job instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        */
        public Job(IfyContext context) : base(context) {
            Initialize();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        private void Initialize() {
            Status = ProcessingStatus.Created;
            Dependencies = new List<Job>();
            Nodes = new List<NodeProcess>(); 
            ExecutionParameters = new ExecutionParameterSet();
            MaxNodes = 4;
            MinArgumentsPerNode = 8;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Job instance.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created Job object</returns>
        */
        public static Job GetInstance(IfyContext context) {
            return new Job(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Job instance representing the job with the specified ID.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the job ID</param>
        /// <returns>the created Job object</returns>
        */
        public static Job FromId(IfyContext context, int id) {
            Job result = new Job(context);
            // result.StatusRequestMethod = context.StatusRequestMethod;
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Job instance representing the job with the specified attributes.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="task">the task to which the job belongs</param>
        /// <returns>the created Job object</returns>
        */
        public static Job OfTask(IfyContext context, Task task) {
            Job result = new Job(context);
            result.Task = task;
            return result;
            /*computingElement = task.ComputingElement;
            publishServer = task.PublishServer;
            compression = task.Compression;*/
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Job instance representing the job with the specified attributes.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="task">the task to which the job belongs</param>
        /// <returns>the created Job object</returns>
        */
        public static Job OfTask(IfyContext context, Task task, int id) {
            Job result = new Job(context);
            result.Task = task;
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Job instance representing the job with the specified attributes.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="task">the task to which the job belongs</param>
        /// <returns>the created Job object</returns>
        */
        public static Job OfTask(IfyContext context, Task task, string name) {
            Job result = new Job(context);
            result.Task = task;
            result.Name = name;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static Job ForTask(IfyContext context, Task task, string name, string jobType) {
            Job result = new Job(context);
            result.Task = task;
            result.Name = name;
            result.JobType = jobType;
            return result;
            /*computingElement = task.ComputingElement;
            publishServer = task.PublishServer;
            compression = task.Compression;*/
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override string AlternativeIdentifyingCondition {
            get {
                if (Task == null) return null;
                return String.Format("t.id_task={0} AND t.name={1}", Task.Id, StringUtils.EscapeSql(Name));
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the job information from the database.</summary>
        /*!
        /// <param name="condition">SQL conditional expression without WHERE keyword</param>
        */
        public override void Load() {
            base.Load();
            Exists = true;

            if (Task != null && TaskId != Task.Id) context.ReturnError("The requested job does not belong to the task", "invalidJob");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void Load(IDataReader reader) {
            Id = reader.GetInt32(0);
            TaskId = reader.GetInt32(1);
            TaskIdentifier = reader.GetString(2);
            TaskIsRunning = reader.GetBoolean(3);
            TaskRemoteIdentifier = context.GetValue(reader, 5);
            Name = context.GetValue(reader, 6);
            JobType = context.GetValue(reader, 7);
            MaxNodes = context.GetIntegerValue(reader, 8);
            MinArgumentsPerNode = context.GetIntegerValue(reader, 9);
            Publishes = context.GetBooleanValue(reader, 10);
            ForcedExit = context.GetBooleanValue(reader, 11);
            Status = reader.GetInt32(12);
            SequentialExecutionIdentifier = context.GetValue(reader, 13);
            StartTime = context.GetDateTimeValue(reader, 14);
            EndTime = context.GetDateTimeValue(reader, 15);
            Exists = true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static string GetSqlQuery(string condition, string sorting) {
            return String.Format("SELECT t.id, t1.id, t1.identifier, t1.start_time IS NOT NULL AND t1.remote_id IS NOT NULL, t1.id_usr, t1.remote_id, t.name, t.job_type, t.max_nodes, t.min_args, t.publish, t.forced_exit, t.status, t.seq_exec_id, t.start_time, t.end_time FROM job AS t INNER JOIN task AS t1 ON t.id_task=t1.id WHERE {0}{1};",
                    condition,
                    sorting == null ? String.Empty : " ORDER BY " + sorting
            );
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the job parameters from the database.</summary>
        public void LoadParameters() {
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT name, value FROM taskparam WHERE id_job={0} AND metadata IS NOT NULL;", Id), dbConnection);
            while (reader.Read()) {
                AddParameter(reader.GetString(0), reader.GetString(1));
            }
            context.CloseQueryResult(reader, dbConnection);

            Loaded = true;
            if (context.DebugLevel >= 2) context.AddDebug(2, "Data of job " + Name + " loaded");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the job dependencies from the database.</summary>
        public void LoadDependencies() {
            List<int> ids = new List<int>();
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT t.id FROM job AS t INNER JOIN jobdependency AS t1 ON t.id=t1.id_job_input WHERE t1.id_job={0};", Id), dbConnection);
            while (reader.Read()) ids.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);
            foreach (int id in ids) {
                Job job = Job.FromId(context, id);
                job.StatusRequestMethod = RequestMethodType.NoRefresh;
                AddDependency(job);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the job status according from a defined source.</summary>
        /*
            The status request method is specified in the configuration variable <i>StatusRequestMethod</i>. According to that value, the job status may be
            <ul>
                <li>not requested at all (the status value in the database is used in this case)</li>
                <li>requested from the Grid engine via the <i>wsServer</i> web service</li>
                <li>requested from the Grid engine via the job XML file</li>
            </ul>
        */
        public virtual void GetStatus() {
            GetStatus(false);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the job status and other details from the job XML file.</summary>
        /// <param name="url">the URL of the job details XML file</param>
        /// <param name="modifyNodes">determines whether the job's node information is updated in the database</param>
        /// <returns><c>true</c> if the details were retrieved.</returns>
        public bool GetStatus(bool modifyNodes) {
            GetTask();

            int oldStatus = Status;
            
            bool result = Task.ComputingResource.GetJobStatus(this);
            
            // Change the job status in the database
            if (result) {
                if (Status != oldStatus) {
                    string additionalFields = String.Empty;
                    if (oldStatus == ProcessingStatus.Created) additionalFields += String.Format(", seq_exec_id={0}, start_time='{1}'", StringUtils.EscapeSql(SequentialExecutionIdentifier), StartTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss"));
                    if (Ended) additionalFields += String.Format(", end_time='{0}'", EndTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss"));
                    context.Execute(
                            String.Format(
                                    "UPDATE job SET status={1}{2} WHERE id={0};",
                                    Id,
                                    Status,
                                    additionalFields
                            )
                    );
                }
    
                if (modifyNodes && Nodes != null) {
                    context.Execute("START TRANSACTION;");
                    try {
                        IDbConnection dbConnection = context.GetDbConnection();
                        IDataReader reader = context.GetQueryResult(String.Format("SELECT pid FROM jobnode WHERE id_job={0};", Id), dbConnection);
                        string deleteList = null;
                        NodeProcess node;
                        while (reader.Read()) {
                            int pid = reader.GetInt32(0);
                            node = null;
                            for (int i = 0; i < Nodes.Count; i++) {
                                if (Nodes[i].Pid == pid) {
                                    node = Nodes[i];
                                    node.Update = true;
                                    break;
                                }
                            }
                            if (node == null) {
                                if (deleteList == null) deleteList = ""; else deleteList += ", ";
                                deleteList += pid.ToString();
                            }
                        }
                        context.CloseQueryResult(reader, dbConnection);
                        if (deleteList != null) {
                            context.Execute(
                                    String.Format(
                                            "DELETE FROM jobnode WHERE id_job={0} AND pid IN ({1});",
                                            Id,
                                            deleteList
                                    )
                            );
                        }
                        for (int i = 0; i < Nodes.Count; i++) {
                            node = Nodes[i];
                            string sql;
                            if (node.Update) {
                                sql = "UPDATE jobnode SET status={2}, result_size={3}, result_size_unit={4}, arg_total={5}, arg_done={6}, hostname={7}, start_time={8}, end_time={9} WHERE id_job={0} AND pid={1};"; 
                            } else {
                                sql = "INSERT INTO jobnode (id_job, pid, status, result_size, result_size_unit, arg_total, arg_done, hostname, start_time, end_time) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9});"; 
                            }
                            context.Execute(
                                    String.Format(
                                            sql,
                                            Id,
                                            node.Pid,
                                            node.Status,
                                            node.ResultSize,
                                            StringUtils.EscapeSql(node.ResultSizeUnit),
                                            node.TotalArguments,
                                            node.DoneArguments,
                                            StringUtils.EscapeSql(node.Hostname),
                                            (node.StartTime > DateTime.MinValue ? "'" + node.StartTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss") + "'" : "NULL"),
                                            (node.EndTime > DateTime.MinValue ? "'" + node.EndTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss") + "'" : "NULL")
                                    )
                            );
                        }
                        context.Execute("COMMIT;");
            
                    } catch (Exception e) {
                        context.WriteError(e.Message + " " + e.StackTrace);
                        context.Execute("ROLLBACK;");
                    }
                }
            }
            
            //context.AddInfo("job " + Name + ": status " + Status + " " + url);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Inserts the job and its parameters and dependencies into the database.</summary>
        /*!
        /// <returns>the ID of the new job</returns>
        */
        public new int Store() {
            TaskId = Task.Id;

            CorrectSplittingValues(false);
            if (Status == ProcessingStatus.None) Status = ProcessingStatus.Created;
            
            bool isNew = (Id == 0);
            
            if (isNew) {
                // Insert job record into database
                string sql = String.Format(
                    "INSERT INTO job (id_task, name, job_type, max_nodes, min_args, publish, forced_exit, status) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", 
                        TaskId,
                        StringUtils.EscapeSql(Name),
                        StringUtils.EscapeSql(JobType),
                        MaxNodes,
                        MinArgumentsPerNode,
                        Publishes.ToString().ToLower(),
                        ForcedExit.ToString().ToLower(),
                        Status
                );
                IDbConnection dbConnection = context.GetDbConnection();
                context.Execute(sql, dbConnection);
                Id = context.GetInsertId(dbConnection);
                Exists = true;
                context.CloseDbConnection(dbConnection);
            } else {
                string sql = String.Format(
                    "UPDATE job SET name={1}, job_type={2}, max_nodes={3}, min_args={4}, publish={5}, forced_exit={6}, status={7} WHERE id={0};", 
                        Id,
                        StringUtils.EscapeSql(Name),
                        StringUtils.EscapeSql(JobType),
                        MaxNodes,
                        MinArgumentsPerNode,
                        Publishes.ToString().ToLower(),
                        ForcedExit.ToString().ToLower(),
                        Status
                );
                context.Execute(sql);
            }
            
            if (!isNew) {
                context.Execute(String.Format("DELETE FROM jobdependency WHERE id_job={0};", Id));
                context.Execute(String.Format("DELETE FROM taskparam WHERE id_job={0};", Id));
            }
            
            // Insert job dependencies
            if (Dependencies.Count != 0) {
                string dependencyStr = "";
                for (int i = 0; i < Dependencies.Count; i++) dependencyStr += (dependencyStr == "" ? "" : ", ") + "(" + Id + ", " + Dependencies[i].Id + ")";
                string sql = String.Format("INSERT INTO jobdependency (id_job, id_job_input) VALUES {0};", dependencyStr); 
                context.Execute(sql);
            }
            
            // Append the input files as a job parameter
            if (InputFiles != null) AddParameter("inputProduct", InputFiles, true);

            // Store the job parameters in the database
            foreach (ExecutionParameter parameter in ExecutionParameters) {
                string sql = String.Format("INSERT INTO taskparam (id_task, id_job, name, type, value, metadata) VALUES ({0}, {1}, {2}, {3}, {4}, {5});",
                        TaskId,
                        Id,
                        StringUtils.EscapeSql(parameter.Name),
                        StringUtils.EscapeSql(parameter.Type),
                        StringUtils.EscapeSql(parameter.Value),
                        parameter.Metadata ? "1" : "0"
                );
                context.Execute(sql);
            }
            
            Loaded = true;
            return Id;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the logical job status to <i>Created</i>.</summary>
        public void Reset() {
            Status = ProcessingStatus.Created;
            Nodes.Clear();
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the logical job status to <i>Created</i>.</summary>
        public void Copy() {
            Id = 0;
            Reset();
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Corrects the splitting values MaxNodes and MinArgumentsPerNode if necessary.</summary>
        public bool CorrectSplittingValues(bool update) {
            bool changed = false;
            if (MaxNodes <= 0) {
                MaxNodes = context.DefaultMaxNodesPerJob;
                if (MaxNodes <= 0) MaxNodes = 1;
                changed = true;
            }
            if (MinArgumentsPerNode <= 0) {
                MinArgumentsPerNode = context.DefaultMinArgumentsPerNode;
                if (MinArgumentsPerNode <= 0) MinArgumentsPerNode = 1;
                changed = true;
            }
            
            if (changed && update) context.Execute(String.Format("UPDATE job SET max_nodes={1}, min_args={2} WHERE id={0}", MaxNodes, MinArgumentsPerNode, Id));
            
            return changed;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads an instance of the task to which the job belongs.</summary>
        public Task GetTask() {
            if (TaskId != 0 && Task == null) Task = Task.FromId(context, TaskId, null as Service, false); 
            TaskId = Task.Id;
            TaskIdentifier = Task.Identifier;
            return Task;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Suspends the job if it is in the <i>Created</i> state.</summary>
        public virtual bool Suspend() {
            GetTask();
            Task.ComputingResource.SuspendJob(this);
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Activates the job if it is suspended.</summary>
        public virtual bool Resume() {
            GetTask();
            Task.ComputingResource.ResumeJob(this);
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Submits the job to the Grid engine.</summary>
        public virtual bool CreateRemote() {
            GetTask();
            if (!Loaded) LoadParameters();
            CorrectSplittingValues(true);
            bool result = Task.ComputingResource.CreateJob(this);
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Submits the job to the Grid engine.</summary>
        public virtual bool UpdateRemote() {
            GetTask();
            if (!Loaded) LoadParameters();
            CorrectSplittingValues(true);
            return Task.ComputingResource.UpdateJob(this);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Aborts the job on the Grid engine.</summary>
        public virtual bool Abort() {
            GetTask();
            return Task.ComputingResource.StopJob(this);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Resubmits the job to the Grid engine.</summary>
        public virtual bool Resubmit() {
            //string s;
            //s = ;
            //context.AddInfo("ws jobResubmit: " + s);
            if (Status < ProcessingStatus.Active) {
                context.ReturnError("The job is not running");
                return false;
            }
            
            if (!Loaded) LoadParameters();
            
            bool result = Task.ComputingResource.StartJob(this);
            
            if (result) {
                Resubmitted = true;
                context.Execute(String.Format("UPDATE job SET status={1} WHERE id={0};", Id, ProcessingStatus.Active));
                Reset();
            }
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the job's state to completed on the Grid Engine.</summary>
        public virtual bool Complete() {
            //string s;
            //s = ;
            //context.AddInfo("ws jobResubmit: " + s);
            if (Status == ProcessingStatus.Completed) {
                context.ReturnError("The job is already completed");
                return false;
            }

            bool result = Task.ComputingResource.CompleteJob(this);
            
            if (result) {
                Status = ProcessingStatus.Completed;
                context.Execute(String.Format("UPDATE job SET status={1} WHERE id={0};", Id, Status));
                if (context.DebugLevel >= 2) context.AddDebug(2, "Job " + Name + " set to completed");
                return true;
            } else {
                context.AddError("Could not complete job \"" + Name + "\" of task " + Task.Identifier + " to completed");
                return false;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Resubmits the job to the Grid engine.</summary>
        /*public bool DoResubmit() {
            //string s;
            //s = ;
            //context.AddInfo("ws jobResubmit: " + s);
            if (!Loaded) LoadParameters();
            RenewSubmissionParameters();
            if (context.GridEngineWebService.jobResubmit(task.RemoteIdentifier, Name) == task.RemoteIdentifier) {
                Resubmitted = true;
                return true;
            }
            return false;
        }*/
        
        //---------------------------------------------------------------------------------------------------------------------

        /*/// <summary>Restarts the job.</summary>
        public bool Restart() {
            return (context.GridEngineWebService.jobRestart(task.RemoteIdentifier, Name) == Name);
        }*/

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds dependencies to the job</summary>
        /*!
        /// <param name="jobs">an array name of the predecessor jobs on which the job depends</param>
        */
        public void AddDependencies(Job[] jobs) {
            for (int i = 0; i < jobs.Length; i++) AddDependency(jobs[i]);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds dependencies to the job</summary>
        /*!
        /// <param name="s">the name of a predecessor job on which the job depends</param>
        */
        public void AddDependencies(string s) {
            string[] names = StringUtils.Split(s, ';'); 
            for (int i = 0; i < names.Length; i++) {
                if (Task.Jobs.ContainsKey(names[i])) AddDependency(Task.Jobs[names[i]]);
                else context.AddError("Input job \"" + names[i] + "\" does not belong to current task");
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a single dependency to the job</summary>
        /*!
        /// <param name="job">a predecessor job on which the job depends</param>
        */
        public void AddDependency(Job job) {
            /*if (task == null) {
                context.AddError("No task defined");
                return;
            }*/
            int jobIndex = Task.Jobs.IndexOf(this);
            int inputJobIndex = Task.Jobs.IndexOf(job);
            bool dependencyCheck = (Id == 0);
            
            if (dependencyCheck && jobIndex == -1) {
                throw new Exception("Job \"" + Name + "\" does not belong to current task");
            } else if (dependencyCheck && inputJobIndex == -1) {
                throw new Exception("Input job \"" + job.Name + "\" does not belong to current task");
            } else {
                if (job == this) throw new Exception("Cyclic dependency of job \"" + Name + "\" on itself");
                for (int i = 0; i < Dependencies.Count; i++) {
                    if (Dependencies[i] == job) throw new Exception("Duplicate dependency of job \"" + Name + "\" on job \"" + job.Name + "\"");
                }
                Dependencies.Add(job);
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds input files to the job</summary>
        /*!
        /// <param name="items">an array of data sets</param>
        */
        public void AddInputFiles(DataSetCollection items) {
            for (int i = 0; i < items.Count; i++) {
                InputFiles += (i == 0 && InputFiles == null ? "" : "|") + items[i].Resource;
                //items[i].Used = true;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds input files to the job</summary>
        /*!
        /// <param name="items">a string array containing the input file identifiers</param>
        */
        public void AddInputFiles(string[] items) {
            for (int i = 0; i < items.Length; i++) InputFiles += (i == 0 && InputFiles == null ? "" : "|") + items[i];
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds input files to the job</summary>
        /*!
        /// <param name="s">a pipe-separated list of input file identifiers</param>
        */
        public void AddInputFiles(string s) {
            InputFiles += (InputFiles == null ? "" : "|") + s;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a single input file to the job</summary>
        /*!
        /// <param name="item">a data sets</param>
        */
        public void AddInputFiles(DataSetInfo item) {
            InputFiles += (InputFiles == null ? "" : "|") + item.Resource;
            //item.Used = true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a single input file to the job</summary>
        /*!
        /// <param name="s">an input file identifier</param>
        */
        public void AddInputFile(string s) {
            InputFiles += (InputFiles == null ? "" : "|") + s;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a parameter to the job</summary>
        /*!
            Job parameters added by this method may still be modified using the job parameter modification interface.
        /// <param name="name">the parameter name</param>
        /// <param name="name">the parameter value</param>
        */
        public void AddParameter(string name, object value) {
            ExecutionParameters.Add(name, null, value, true);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a debug parameter to the job</summary>
        /*!
            Job parameters added by this method may still be modified using the job parameter modification interface.
        /// <param name="name">the parameter name</param>
        /// <param name="name">the parameter value</param>
        */
        public void AddDebugParameter(string name, object value) {
            ExecutionParameters.Add(name, "debug", value, true);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a parameter to the job</summary>
        /*!
        /// <param name="name">the parameter name</param>
        /// <param name="name">the parameter value</param>
        /// <param name="metadata">a flag indicating whether the job parameter may still be modified using the job parameter modification interface. </param>
        */
        public void AddParameter(string name, object value, bool metadata) {
            ExecutionParameters.Add(name, null, value, metadata);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a parameter to the job</summary>
        /*!
        /// <param name="name">the parameter name</param>
        /// <param name="type">the parameter type</param>
        /// <param name="name">the parameter value</param>
        /// <param name="metadata">determines whether the job parameter may still be modified using the job parameter modification interface. </param>
        */
        public void AddParameter(string name, string type, object value, bool metadata) {
            ExecutionParameters.Add(name, type, value, metadata);
        }
    }

    


    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    
    
    
/*    public class JobProcessingInformation {
        
        //---------------------------------------------------------------------------------------------------------------------

        public Job Job { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string Url { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        //public int ActualStatus { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public int Status { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string SequentialExecutionIdentifier { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public DateTime StartTime { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public DateTime EndTime { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string StatusMessage { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public MessageType StatusMessageType { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public DateTime StatusMessageTime { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string DebugMessage { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public MessageType DebugMessageType { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public DateTime DebugMessageTime { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public int TotalArguments { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public int DoneArguments { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public int NodeCount {
            get { return Nodes == null ? 0 : Nodes.Count; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public List<NodeProcess> Nodes { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public JobProcessingInformation(Job job) {
            this.Job = job;
            Refresh(job);
            //this.ActualStatus = job.ActualStatus;
            this.Status = job.Status;
            this.StartTime = job.StartTime;
            this.EndTime = job.EndTime;
            this.StatusMessageType = job.StatusMessageType;
            this.StatusMessage = job.StatusMessage;
        }
            
        public virtual void Refresh(Job job) {
            this.Status = job.Status;
            this.SequentialExecutionIdentifier = job.SequentialExecutionIdentifier;
            this.StartTime = job.StartTime;
            this.EndTime = job.EndTime;
            this.StatusMessage = job.StatusMessage;
            this.StatusMessageType = job.StatusMessageType;
            this.StatusMessageTime = job.StatusMessageTime;
            this.DebugMessage = job.DebugMessage;
            this.DebugMessageType = job.DebugMessageType;
            this.DebugMessageTime = job.DebugMessageTime;
            this.TotalArguments = job.TotalArguments;
            this.DoneArguments = job.DoneArguments;
            this.Nodes = job.Nodes;
        }
    }

*/    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    /// <summary>Represents a job execution on an individual processing node.</summary>
    public class NodeProcess {
        public int Pid;
        public int Status;
        public int ResultSize;
        public string ResultSizeUnit;
        public int TotalArguments, DoneArguments;
        public string Hostname;
        public DateTime StartTime, EndTime;
        public DateTime NotificationTime;
        public string NotificationMessage;
        public string ResultXml;
        public bool Update;
        public string GridEngineFolderUrl;
        public string Stdout, Stderr;
        public List<string> Logs;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new NodeProcess instance based on the information of an XML element.</summary>
        /*!
        /// <param name="element">the XML element in the job information XML file containing the processing node information</param>
        */
        public NodeProcess(XmlElement element, string gridEngineFolderUrl) {
            int pid;
            if (!Int32.TryParse(element.Attributes["pid"].Value, out pid)) pid = 0;
            this.Pid = pid;
            if (element.HasAttribute("status")) this.Status = GlobusComputingElement.StringToStatus(element.Attributes["status"].Value); // !!! Change this, GlobusComputingElement should not be used here
            for (int i = 0; i < element.ChildNodes.Count; i++) {
                XmlElement elem = element.ChildNodes[i] as XmlElement;
                if (elem == null) continue;
                
                switch (elem.Name) {
                    case "resultSize" :
                        Int32.TryParse(elem.InnerText, out this.ResultSize);
                        if (elem.HasAttribute("unit") && elem.Attributes["unit"].Value.Length > 0) this.ResultSizeUnit = elem.Attributes["unit"].Value[0].ToString().ToUpper();
                        break;
                    case "hostname" :
                        this.Hostname = elem.InnerText;
                        break;
                    case "startDate" :
                        DateTime.TryParse(elem.InnerText, out this.StartTime);
                        this.StartTime = StartTime.ToUniversalTime();
                        break;
                    case "endDate" :
                        DateTime.TryParse(elem.InnerText, out this.EndTime);
                        this.EndTime = EndTime.ToUniversalTime();
                        break;
                    case "notification" :
                        for (int j = 0; j < elem.ChildNodes.Count; j++) {
                            if (!(elem.ChildNodes[j] is XmlElement)) continue;
                            XmlElement notificationElem = elem.ChildNodes[j] as XmlElement;
                            switch (notificationElem.Name) {
                                case "date" :
                                    DateTime.TryParse(notificationElem.InnerText, out this.NotificationTime);
                                    this.NotificationTime = NotificationTime.ToUniversalTime();
                                    break;
                                case "action" :
                                    this.NotificationMessage = notificationElem.InnerText;
                                    break;
                                case "parameter" :
                                    if (notificationElem.HasAttribute("total")) Int32.TryParse(notificationElem.Attributes["total"].Value, out this.TotalArguments);
                                    if (notificationElem.HasAttribute("done")) Int32.TryParse(notificationElem.Attributes["done"].Value, out this.DoneArguments);
                                    break;
                            }
                        }
                        break;
                    case "PublishedResults" :
                        ResultXml = elem.OuterXml; 
                        break;
                    case "log" :
                        if (Logs == null) Logs = new List<string>();
                        Logs.Add(String.Format("{0}/{1}", gridEngineFolderUrl, elem.InnerXml));
                        break;
                    case "stdout" :
                        Stdout = String.Format("{0}/{1}", gridEngineFolderUrl, elem.InnerXml);
                        break;
                    case "stderr" :
                        Stderr = String.Format("{0}/{1}", gridEngineFolderUrl, elem.InnerXml);
                        break;
                }
            }
            this.GridEngineFolderUrl = gridEngineFolderUrl;        
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static void GetArguments(XmlElement element, ref int total, ref int done) {
            for (int i = 0; i < element.ChildNodes.Count; i++) {
                XmlElement elem = element.ChildNodes[i] as XmlElement;
                if (elem == null) continue;
                
                switch (elem.Name) {
                    case "notification" :
                        for (int j = 0; j < elem.ChildNodes.Count; j++) {
                            if (!(elem.ChildNodes[j] is XmlElement)) continue;
                            XmlElement notificationElem = elem.ChildNodes[j] as XmlElement;
                            switch (notificationElem.Name) {
                                case "parameter" :
                                    if (notificationElem.HasAttribute("total")) Int32.TryParse(notificationElem.Attributes["total"].Value, out total);
                                    if (notificationElem.HasAttribute("done")) Int32.TryParse(notificationElem.Attributes["done"].Value, out done);
                                    break;
                            }
                        }
                        break;
                }
            }
        }
        
    }

}



/*
ok
[06/10/2010 16:06:43] Frank Loeschau: ci sono

jobClean
jobResubmit
jobRestart

[06/10/2010 16:08:15] Frank Loeschau: perch� come operazioni abbiamo
Pause o Resume
Resubmit
e forse Abort
che chiamata devo fare a wsServer per ciascuna di queste operazioni?

[06/10/2010 16:21:50] fabio�: allora per il pause dovrebbe usare i setJobStatus
[06/10/2010 16:23:28] Frank Loeschau: e questo prima della partenza del job, vero?
[06/10/2010 16:23:47] fabio�: no questo anche durante
[06/10/2010 16:23:52] fabio�: alla fine gli passi il 600
[06/10/2010 16:23:58] fabio�: e se ne occupa poi lui

[06/10/2010 17:06:13] fabio�: si fin'ora ti so dire che la pausa corrisponde a setJobStatus
[06/10/2010 17:06:30] Frank Loeschau: con quale numero?
[06/10/2010 17:06:32] Frank Loeschau: 600?
[06/10/2010 17:06:37] fabio�: e il pulsante restart sul portale (quando un task � finito) corrisponde a taskAbort
[06/10/2010 17:06:40] fabio�: si 600
[06/10/2010 17:07:09] Frank Loeschau: ok
[06/10/2010 17:07:42] Frank Loeschau: e per fare ripartire un job in pausa? si deve sapere lo status che aveva prima?
[06/10/2010 17:55:38] fabio�: allora su un job andato male quando fai il resubmit chiama il metodo jobResubmit di wsServer
[06/10/2010 17:56:28] Frank Loeschau: ok
[06/10/2010 17:56:30] fabio�: quando fai partire un job che era paused, chiama il metodo jobRestart
[06/10/2010 17:56:40] Frank Loeschau: :) cos� facile :)
[06/10/2010 17:57:01] fabio�: che manca?
[06/10/2010 17:57:07] fabio�: delle cose che mi hai chiesto?
[06/10/2010 17:57:54] Frank Loeschau: c'� tutto:
pause -> setJobStatus 600
resume -> jobRestart
resubmit -> jobResubmit
[06/10/2010 17:58:00] Frank Loeschau: giusto?
[06/10/2010 17:58:38] Frank Loeschau: l'abort di un job non ha senso, mi sembra
[06/10/2010 18:02:14] fabio�: non esiste l'abort di un job manuale
[06/10/2010 18:02:19] fabio�: nel senso esiste il force failure
[06/10/2010 18:02:31] fabio�: ma lavora su un altro livello
[06/10/2010 18:02:58] Frank Loeschau: ok, non lo faccio
[06/10/2010 18:03:14] Frank Loeschau: grazie :)
*/
