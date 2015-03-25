using System;
using System.Data;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Util;


/*!
\defgroup Scheduler Scheduler
@{
This class is quite generic and represent any kind of scheduled task from its most simple form (simple download) to the most complex processing workflow ("hosted" or "remote" processing). As per \ref core_Task, a scheduler is always created by a \ref core_Service so there is always a service that creates the scheduled task. The schedulers may be simply managed in the simple user’s space. Practically, a scheduler is linked to a reference task that contains all the reference parameters for all the future tasks that will be created in data-driven or time-driven mode. In fact, in each loop the scheduler is triggered and there is at least a new product available, the system creates a new task to manage this request.

\xrefitem mvc_c "Controller" "Controller components"

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence stores persistently the scheduler information in the database

\xrefitem dep "Dependencies" "Dependencies" \ref Authorisation controls owner and access

\xrefitem dep "Dependencies" "Dependencies" creates new \ref Task



@}
 */



//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Abstract class that represents an operation that is repeated at regular intervals.</summary>
    /// \ingroup Scheduler
    /// A task scheduler creates tasks according to variable and fixed parameters and the scheduling mode.
    ///    
    /// There are two scheduling modes:
    /// <ul>
    ///     <li>
    ///         <b>Time-driven</b>: The scheduler definition contains a validity start and stop date and a time interval and a time interval.
    ///         The first execution time of the scheduler is the validity start time, the following execution time is the current execution time incremented by the time interval (may be negative). New tasks are created and submitted until the validity end time is reached or exceeded.
    ///         The selection period for the task input files (task start and end time) must be variable, i.e. a function of the execution time.
    ///     </li>
    ///     <li>
    ///         <b>Data-driven</b>: The scheduler definition contains a validity start and stop date and a time interval and a time interval.
    ///         The first execution time of the scheduler is the validity start time, the following execution time is the current execution time incremented by the time interval (may be negative). New tasks are created and submitted until the validity end time is reached or exceeded.
    ///         The selection period for the task input files (task start and end time) must be variable, i.e. a function of the execution time.
    ///     </li>
    /// </ul>
    /// 
    /// In order to work automatically, the background agent must be running and the Scheduler action must be enabled.
    /// 
    /// Alternatively, for debug purposes, the scheduler can be advanced manually and the resulting tasks can be submitted manually.
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("scheduler", EntityTableConfiguration.Full, HasExtensions = true, HasOwnerReference = true)]
    [EntityReferenceTable("usr", USER_TABLE)]
    public abstract class Scheduler : Entity {

        private const int USER_TABLE = 1;
        private SchedulerRunConfiguration schedulerRunConfiguration;
        private SchedulerTaskConfiguration taskConfiguration;

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("has_tasks")]
        public bool HasTaskConfiguration { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public SchedulerTaskConfiguration TaskConfiguration {
            get {
                if (HasTaskConfiguration && taskConfiguration == null) taskConfiguration = SchedulerTaskConfiguration.FromId(context, Id);
                return taskConfiguration;
            }
            set {
                taskConfiguration = value;
                HasTaskConfiguration = (taskConfiguration != null);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the username of the scheduler's owner.</summary>
        [EntityForeignField("username", USER_TABLE)]
        public string Username { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the ID of the scheduler class.</summary>
        [EntityDataField("id_class", IsForeignKey = true)]
        public int SchedulerClassId { get; set; } //TODO:modif engue set was protected

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("status")]
        public SchedulingStatus Status { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the run configuration.
        /// </summary>
        /// <value>The run configuration.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public SchedulerRunConfiguration RunConfiguration {
            get {
                if (schedulerRunConfiguration == null && Id != 0) {
                    schedulerRunConfiguration = SchedulerRunConfiguration.FromId(context, Id);
                    schedulerRunConfiguration.Scheduler = this;
                }
                return schedulerRunConfiguration;
            }
            set {
                schedulerRunConfiguration = value;
                if (schedulerRunConfiguration != null) schedulerRunConfiguration.Scheduler = this;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the maximum number of runs per cycle of the scheduler daemon.</summary>
        /// <remarks>If the value is 0 it means that the scheduler tries to run until the end at the first cycle.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("max_runs")]
        public int MaxSubmissions { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /*/// <summary>In a derived class, gets the OpenSearch URL to be.</summary>
        /// <remarks>The array contains usually only one element.</remarks>
        public abstract string[] OpenSearchUrls { get; }*/

        //---------------------------------------------------------------------------------------------------------------------

        public virtual bool UsesExternalProcessing {
            get { return true; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the scheduler is enabled.</summary>
        public bool Enabled {
            get { return Status == SchedulingStatus.Running; }
            set { Status = (value ? SchedulingStatus.Running : SchedulingStatus.Paused); }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates whether the scheduler can create tasks with no input data.</summary>
        public virtual bool CanCreateEmptyTasks {
            get { return false; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates or determines whether the scheduler execution is interrupted when one of its tasks has failed.</summary>
        [EntityDataField("no_fail")]
        public bool NoFailedRuns { get; set; } // !!! make changeable in

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the inputs.
        /// </summary>
        /// <value>The inputs.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public List<ProcessingInputSet> Inputs { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Scheduler instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Scheduler(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Scheduler instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created Scheduler object</returns>
        public static new Scheduler GetInstance(IfyContext context) {
            return new CustomScheduler(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Scheduler instance representing the scheduler with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the scheduler ID</param>
        /// <returns>the created Scheduler object</returns>
        public static Scheduler FromId(IfyContext context, int id) {
            EntityType entityType = EntityType.GetEntityType(typeof(Scheduler));
            Scheduler result = (Scheduler)entityType.GetEntityInstanceFromId(context, id);
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Scheduler instance representing the scheduler with the specified identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="indentifier">the scheduler identifier</param>
        /// <returns>the created Scheduler object</returns>
        public static Scheduler FromIdentifier(IfyContext context, string identifier) {
            EntityType entityType = EntityType.GetEntityType(typeof(Scheduler));
            Scheduler result = (Scheduler)entityType.GetEntityInstanceFromIdentifier(context, identifier);
            result.Identifier = identifier;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static Scheduler ForService(IfyContext context, Service service, int typeId) {
            EntityType entityType = EntityType.GetEntityType(typeof(Scheduler));
            Scheduler result = (Scheduler)entityType.GetEntityExtensionInstance(context, typeId);
            
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the scheduler parameters from the database.</summary>
        public NameValueCollection LoadParametersTemp() {
            NameValueCollection result = new NameValueCollection();
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT name, type, value FROM schedulerparam WHERE id_scheduler={0};", Id), dbConnection);
            while (reader.Read()) result.Add(reader.GetString(0), reader.GetString(2));
            context.CloseQueryResult(reader, dbConnection);
            return result;
        }
              
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the scheduler parameters from the database.</summary>
        public void StoreParametersTemp(NameValueCollection parameters) {
            IDbConnection dbConnection = context.GetDbConnection();
            foreach (string name in parameters.Keys) {
                foreach (string value in parameters.GetValues(name)) {
                    context.Execute(String.Format("INSERT INTO schedulerparam (id_scheduler, name, value) VALUES ({0}, {1}, {2});", Id, StringUtils.EscapeSql(name), StringUtils.EscapeSql(value)), dbConnection);
                }
            }
            context.CloseDbConnection(dbConnection);

        }


        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads parameters from the specified task.</summary>
        public void LoadTaskParameters(Task task) {
/*            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT name, type, value FROM taskparam WHERE id_task={0} AND id_job IS NULL AND metadata IS NOT NULL;", task.Id), dbConnection);
            while (reader.Read()) {
                RequestParameter param = new RequestParameter(context, Service, reader.GetString(0), reader.GetString(1), null, reader.GetString(2));
                //param.Level = RequestParameterLevel.Custom;
                RequestParameters.Add(param);
            }
            context.CloseQueryResult(reader, dbConnection);*/
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override void Load() {
            base.Load();
            if (HasTaskConfiguration) TaskConfiguration.Load();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Inserts the scheduler and its parameters into the database.</summary>
        public override void Store() {
            bool existedBefore = Exists;
            if (Identifier == null) Identifier = Guid.NewGuid().ToString();
            if (Name == null) Name = Identifier;

            HasTaskConfiguration = (taskConfiguration != null);
            base.Store();
            if (RunConfiguration != null) RunConfiguration.Store();
            if (HasTaskConfiguration) TaskConfiguration.Store();

            //if (existedBefore) context.Execute(String.Format("DELETE FROM schedulerparam WHERE id_scheduler={0};", Id));
            CreateParameters();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Deletes a scheduler and deletes its tasks or marks them for deletion to be performed by the background agent.</summary>
        /*!
            The web portal configuration setting <i>SynchronousTaskOperations</i> determines the deletion mode:
            <ul>
                <li>If set to <i>true</i>, the jobs is aborted on the Grid engine (if it is not in <i>Created</i> or <i>Pending</i> state) immediately and deleted.</li> 
                <li>If set to <i>false</i>, the task's next status is set to <i>Deleted</i>, which causes the background agent on its next run to abort it on the Grid engine (if it is not in <i>Created</i> or <i>Pending</i> state) and actually delete it.  
            </ul>
        */
        public override void Delete() {
            Status = SchedulingStatus.Deleted;
            if (context.SynchronousTaskOperations) { // (1)
                DoAbortAllTasks();
                context.Execute(String.Format("DELETE FROM scheduler WHERE id={0};", Id));
            } else { // (2)
                context.Execute(String.Format("UPDATE scheduler SET status={1} WHERE id={0};", Id, Status));
            }

            
            /*Task task = Task.GetInstance(context);
            
            task.GetIdsFromScheduler();
            task.AbortMultiple();*/
            
            /*context.Execute(String.Format("UPDATE task scheduler WHERE id={0};", Id));
            context.Execute(String.Format("DELETE FROM scheduler WHERE id={0};", Id));*/
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        protected int DoAbortAllTasks() {
            // return Task.GetInstance(context).DoAbortMultiple(this); TODO: Reorganised

            return 0;

            /* REPLACED WOULD BE:
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(
                String.Format("SELECT t.id FROM task AS t WHERE t.id_scheduler={0} AND t.status>={1};",
                          scheduler.Id,
                          ProcessingStatus.Active
                          ),
                dbConnection
            );
            List<int> taskIds = new List<int>();
            while (reader.Read()) taskIds.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            Ids = new int[taskIds.Count];
            for (int i = 0; i < taskIds.Count; i++) Ids[i] = taskIds[i];
            taskIds.Clear();
            return DoAbortMultiple(true);*/
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Inserts the scheduler parameters into the database.</summary>
        protected void CreateParameters() {
/*            for (int i = 0; i < RequestParameters.Count; i++) {
                RequestParameter param = RequestParameters[i];
                if (!param.IsRow || param.Value == null) continue;
                string sql = String.Format("INSERT INTO schedulerparam (id_scheduler, name, type, value) VALUES ({0}, {1}, {2}, {3});",
                        Id,
                        StringUtils.EscapeSql(param.Name),
                        StringUtils.EscapeSql(param.Type),
                        StringUtils.EscapeSql(param.Value)
                );
                context.Execute(sql);
            }*/
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, runs the main action of the scheduler the specified number of times.</summary>
        /// <param name="count">The number of times for the scheduler's main action to be run.</param>
        /// <returns>The status of the scheduler after the execution of the run.</returns>
        public abstract SchedulingStatus Iterate(int count);

        //---------------------------------------------------------------------------------------------------------------------

        public virtual CatalogueResult GetNextCatalogueResult() {
/*            if (EndReached) {
                string message = "Cannot create a new task, the scheduler execution time has exceeded the validity end time";
                if (UsedByAgent) context.AddInfo(message);
                else Invalidate(message);
                return null;
            }

            RequestParameterCollection requestParameters = new RequestParameterCollection();
            
            Service.GetParameters();
            
            IDataReader reader = context.GetQueryResult("SELECT name, type, value FROM schedulerparam WHERE id_scheduler=" + Id + ";");
            while (reader.Read()) {
                requestParameters.GetParameter(context, Service, reader.GetString(0), reader.GetString(1), null, reader.GetString(2));
            }
            reader.Close();

            if (CheckSchedulingState()) SetNextTaskParameters(requestParameters);
            
            for (int i = 0; i < requestParameters.Count; i++) {
                RequestParameter param = requestParameters[i];
                if (param.Type != "series") continue;
                
                Series series = Series.FromIdentifier(context, param.Value);

                series.DataSetParameter = requestParameters["files"];
                return series.GetCatalogueResult(requestParameters);
            }*/ // TODO-NEW-SERVICE
            
            return null;
            
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Resets the scheduler attributes to their initial values and marks the task created so far by the scheduler for deletion.</summary>
        public virtual void Reset() {
            /*context.Execute( // !!! query?
                    String.Format("UPDATE task SET id_scheduler=NULL, status=async_op=CASE WHEN status>={1} THEN status ELSE {2} END, async_op=CASE WHEN status>={1} THEN {3} ELSE NULL END WHERE id_scheduler={0};",
                            Id,
                            ProcessingStatus.Pending,
                            ProcessingStatus.Deleted,
                            TaskOperationType.Delete
                    )
            );
            context.Execute(String.Format("UPDATE scheduler SET status={1} WHERE id={0};", Id, ProcessingStatus.Active));
            Started = false;*/
        }
        
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Executes the scheduler-related part of the background agent action <b>%Task&nbsp;and&nbsp;scheduler&nbsp;cleanup</b>.</summary>
        /*
            This method is called periodically if the action is active and the background agent is running according to the action's execution interval specified in the portal configuration.

            The background agent action <b>%Task&nbsp;cleanup</b> deletes the tasks that have been marked for deletion and no longer consume Grid engine resources, together with all data related to these tasks (jobs, node information etc.), from the database.
            This is the case for tasks that have the value <i>Deleted</i> in the database field <i>task.status</i>.
            %Tasks deletions are pending if they were requested while the portal configuration variable <i>SynchronousTaskOperations</i> was set to <i>false</i>.
            
        /// <param name="context">The execution environment context.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        */
        public static void ExecuteCleanup(IfyContext context) {
            IDataReader reader;
            List<int> schedulerIds = new List<int>();
            
            // Abort tasks (next_status = Deleted)
            reader = context.GetQueryResult(String.Format("SELECT t.id FROM scheduler AS t WHERE status={0} ORDER BY t.id;", ProcessingStatus.Deleted));
            while (reader.Read()) schedulerIds.Add(reader.GetInt32(0));
            reader.Close();
            
            int count = 0, taskCount = 0;
            foreach (int schedulerId in schedulerIds) {
                Scheduler scheduler = FromId(context, schedulerId);
                taskCount += scheduler.DoAbortAllTasks();
                context.Execute(String.Format("DELETE FROM scheduler WHERE id={0};", schedulerId));
                count++;
            }
            schedulerIds.Clear();

            if (count > 0) context.WriteInfo(String.Format("Deleted schedulers {0}: (aborted tasks: {1})", count, taskCount));
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual bool GetNextInputData(Task task) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static void ExecuteTaskScheduler(IfyContext context) {
            /*IDataReader reader;
            List<int> schedulerIds = new List<int>();
            string sql;
            
            // Abort tasks (next_status = 10)
            reader = null;
            sql = String.Format("SELECT t.id FROM scheduler AS t WHERE t.status={0} ORDER BY t.id;", ProcessingStatus.Active);
            reader = context.GetQueryResult(sql);
            while (reader.Read()) schedulerIds.Add(reader.GetInt32(0));
            reader.Close();
            
            for (int i = 0; i < schedulerIds.Count; i++) {
                try {
                    Scheduler scheduler = Scheduler.FromId(context, schedulerIds[i]);
                    scheduler.UsedByAgent = true;
                    
                    // Write log information on scheduler
                    context.WriteInfo(
                            String.Format("Scheduler \"{0}\" ({1} {2}), user: {3}",
                                    scheduler.Name,
                                    scheduler.ServiceIdentifier,
                                    scheduler.ModeCaption,
                                    scheduler.Username
                            )
                    );
                    
                    // Write log information on scheduler tasks
                    if (scheduler.UsesExternalProcessing) {
                        scheduler.GetTaskSummary();
                        context.WriteInfo(
                                String.Format("Task summary: {0}{1} unsubmitted, {2} submitted, {3} failed, {4} finished",
                                        scheduler.CanCreateEmptyTasks ? scheduler.EmptyTasks + " empty, " : String.Empty,
                                        scheduler.UnsubmittedTasks,
                                        scheduler.SubmittedTasks,
                                        scheduler.FailedTasks, 
                                        scheduler.FinishedTasks
                                )
                        );
                    }
                    
                    if (scheduler.FailedTasks != 0 && scheduler.NoFailedRuns) {
                        context.WriteWarning("Scheduler does not allow failed tasks, skipping");
                        continue;
                    }
                    
                    // Submit as many new tasks for the scheduler as possible (defined in MaxSubmissions, limited by user credits and)
                    int unended = scheduler.SubmittedTasks;// + scheduler.UnsubmittedTasks;
                    bool canSubmitMore = true;
                    int submittedCount = 0;
                    while (canSubmitMore) {

                        // If no more created tasks are there, advance the scheduler, otherwise use that task
                        int nextTaskId = context.GetQueryIntegerValue(String.Format("SELECT id FROM task WHERE id_scheduler={0} AND status={1} AND async_op IS NULL AND NOT empty ORDER BY id LIMIT 1;", scheduler.Id, ProcessingStatus.Created));
                        Task task = null;
                        if (nextTaskId == 0) {
                            if (scheduler.MaxSubmissions != 0 && unended >= scheduler.MaxSubmissions) {
                                context.WriteInfo(String.Format("Cannot create a new task now, the scheduler has currently {0} task{1} in running state", unended, unended == 1 ? String.Empty : "s"));
                            } else {
                                context.WriteInfo("Preparing next task ... \\");

                                task = scheduler.GetNextTask();
                                if (task != null) {
                                    context.WriteInfo("Done");
                                    context.WriteInfo(String.Format("Scheduling attributes for this task: {0}", scheduler.GetExecutionLogMessage()));
                                }
                            }
                            
                        } else {
                            task = Task.FromId(context, nextTaskId);
                        }
                        
                        if (task == null) break;
                        
                        // Create it using the service's script
                        if (task.Status == ProcessingStatus.None) {
                            context.WriteInfo("Building task ... \\");
                            task.AllowEmpty = scheduler.CanCreateEmptyTasks;
                            task.Build();
                            //if (task.Error) break;
                            scheduler.Load(); // IMPORTANT refresh scheduler attributes
                            context.WriteInfo("Task " + task.Identifier + " built");
                        }
                        //task.Scheduler = scheduler;
                        //context.Execute(String.Format("UPDATE task SET id_scheduler={0} WHERE id={1};", scheduler.Id, task.Id));
                        
                        // If task is created state, submit it directly to the Grid engine
                        if (task.Status == ProcessingStatus.Created) {
                            double cost = task.Cost;
                            double credits = context.GetAvailableCredits(task.UserId);
                            int usagePercentage;
                            
                            if (task.Empty) {
                                context.WriteInfo(String.Format("Cannot submit the next task, because it has no input data", unended, unended == 1 ? String.Empty : "s"));
                            } else if (scheduler.MaxSubmissions != 0 && unended > scheduler.MaxSubmissions) {
                                canSubmitMore = false;
                                context.WriteInfo(String.Format("Cannot submit the next task, the scheduler has currently {0} task{1} in created or running state", unended, unended == 1 ? String.Empty : "s"));
                            } else if (cost > credits) {
                                canSubmitMore = false;
                                context.WriteInfo(String.Format("The cost of the next task {0} ({1}) exceeds the available credits for user {2} ({3})", task.Identifier, cost, task.Username, credits));
                            } else {
                                scheduler.ComputingResource.ForceRefresh = true;
                                usagePercentage = (scheduler.ComputingResource.CanMonitorStatus ? scheduler.ComputingResource.LoadPercentage : 0);
                                if (usagePercentage > scheduler.MaxComputingResourceUsage) {
                                    canSubmitMore = false;
                                    context.WriteInfo(String.Format("Cannot submit the next task, the computing resource is too busy (usage: {0}%, allowed maxium: {1}%)", usagePercentage, scheduler.MaxComputingResourceUsage));
                                } else if (usagePercentage == -1) {
                                    canSubmitMore = false;
                                    context.WriteInfo("Cannot submit the next task, the computing resource status is unavailable.");
                                } else {
                                    context.WriteInfo("Submitting task " + task.Identifier + " ... \\");
                                    task.ImmediateSubmission = true;
                                    canSubmitMore = task.Submit() && task.Active; // scheduler tasks are submitted to the computing resource immediately (no pending)
                                    if (canSubmitMore) {
                                        unended++;
                                        submittedCount++;
                                        context.WriteInfo("Remote identifier is " + task.RemoteIdentifier);
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception e) {
                    context.AddError("Could not execute scheduler operation: " + e.Message);
                }
            }
            schedulerIds.Clear();*/
            
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the task list.
        /// </summary>
        /// <returns>The task list.</returns>
        /// <param name="context">Context.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public List<Task> GetTaskList(IfyContext context) {
            List<int> taskIds = new List<int>();
            IDbConnection dbConnection = context.GetDbConnection();
            string sql = String.Format("SELECT t.id FROM task AS t WHERE id_scheduler={0};", this.Id);
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) taskIds.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            List<Task> result = new List<Task>();
            foreach (int taskId in taskIds) result.Add(Task.FromId(context, taskId, null, true));

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public List<int> GetTaskIdList(IfyContext context) {
            List<int> taskIds = new List<int>();
            string sql = String.Format("SELECT t.id FROM task AS t WHERE id_scheduler={0};", this.Id);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) taskIds.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            return taskIds;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public int GetActiveTaskCount() {
            return context.GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM task WHERE id_scheduler={0} AND status>={1} AND status<{2};", this.Id, ProcessingStatus.Active, ProcessingStatus.Failed));
        }

        //---------------------------------------------------------------------------------------------------------------------


        /// <summary>
        /// Gets the active tasks.
        /// </summary>
        /// <returns>The active tasks.</returns>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public EntityList<Task> GetActiveTasks() {
            List<int> taskIds = new List<int>();
            string sql = String.Format("SELECT id FROM task WHERE id_scheduler={0} AND status>={1} AND status<{2};", this.Id, ProcessingStatus.Active, ProcessingStatus.Failed);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) taskIds.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            EntityList<Task> result = new EntityList<Task>(context);
            foreach (int taskId in taskIds) result.Include(Task.FromId(context, taskId, null, false));

            return result;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    /// <summary>
    /// Scheduler run action.
    /// </summary>
    public delegate void SchedulerRunAction(string result);
    

    /// <summary>Represents a customisable scheduler that does not operate tasks.</summary>
    public class CustomScheduler : Scheduler {

        public SchedulerRunAction OnRun { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public CustomScheduler(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------
        
        public bool CheckSchedulingState() {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void SetNewSchedulingState() {}

        //---------------------------------------------------------------------------------------------------------------------

        public override SchedulingStatus Iterate(int count) {

            // Advance parameters
            //RunConfiguration.GetNextParameters();
            // Make query
            string result = null;//GetOpenSearchResult();

            // If result is acceptable (i.e. not empty or too small or similar), call callback function
            if (/*RunConfiguration.CheckResult()*/true) {
                // call function
                if (OnRun == null) throw new InvalidOperationException(String.Format("No run action method defined for scheduler {0}", Identifier));
                OnRun.Invoke(result);
                //RunConfiguration.Store();
                return SchedulingStatus.Running;

            } else {
                return SchedulingStatus.None;
            } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /*public virtual void Apply(NameValueCollection coll) {

            // If result is acceptable (i.e. not empty or too small or similar),


        }*/

    }



    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public enum SchedulingStatus {

        None = 0,

        Ready = 10,

        Running = 20,

        Paused = 25,

        Failed = 30,

        Completed = 40,

        Deleted = 255

    }
    
}

