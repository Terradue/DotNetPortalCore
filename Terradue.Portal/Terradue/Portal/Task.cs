using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;
using Terradue.Util;



/*!
\defgroup Task Task
@{
This component is quite generic and represent any kind of data access requests from 
its most simple form (simple download) to the most complex processing workflow ("hosted" or "remote" processing).
A task is always created by a \ref Service so there is always a service that is associated to the task.
A task is manageable individually in the user’s space. A task is always submitted to a 
\ref ComputingResource to process it and then when completed, returns results to user.
The Diagram \ref task-cr details the interactions between the involved entities.

\xrefitem mvc_c "Controller" "Controller components"

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence stores persistently the task information in the database

\xrefitem dep "Dependencies" "Dependencies" \ref Authorisation controls access

\xrefitem dep "Dependencies" "Dependencies" \ref ComputingResource processes task

\anchor task-cr

@startuml

!define DIAG_NAME Service - Task - Computing Resource sequence Diagram

skinparam backgroundColor #FFFFFF

participant "Service" as S
participant "Task" as T
participant "Computing Resources" as C

autonumber

== Data Request Creation ==

S -> T: Create task\n(parameters, computing resource)
activate T

T -> T: Create and store Task

T --> S: Return Task ID
deactivate T

== Data Request Submission ==

S -> T: Submit Task\n(Task ID)
activate T

T -> C: Start Task
activate C

C -> C: Process Task
activate C #DarkSalmon

C --> T: Task started\n(Process id)

T --> S: Task submitted
deactivate T

== Data Request Status ==

S -> T: Get Task status\n(Task id)
activate T

alt case status 'in progress'

T -> C: Get Task status\n(Process id)

C --> T: Task status

T --> S: Task status
deactivate T

else case status 'paused', 'completed', 'cancelled'

deactivate C
deactivate C

T -> C: Get Task status\n(Process id)
activate T
activate C
C --> T: Task status

deactivate C
T --> S: Task status
deactivate T

end

footer 
DIAG_NAME
(c) Terradue Srl
endfooter

@enduml



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



    public delegate void TaskOperationCallbackType(Task task);



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Represents an Ify processing task.</summary>
    /// <remarks>
    ///     <para>
    ///         This class provides information on an Ify processing task including properties for execution and status details and methods for the execution control.
    ///         An instance of the Task class maintains a list of all its processing jobs. 
    ///         A Task object sends requests to the Grid engine via the wsServer interface (on the Grid engine side).
    ///  </para>
    ///  <para>
    ///         <b>Data storage:</b> The tasks are stored in the database table <c>task</c>.
    ///     </para>
    /// </remarks>
    /// \ingroup Task
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("task", EntityTableConfiguration.Full, HasOwnerReference = true)]
    [EntityReferenceTable("usr", USER_TABLE)]
    [EntityReferenceTable("service", SERVICE_TABLE)]
    public class Task : Entity {
        private const int USER_TABLE = 1;
        private const int SERVICE_TABLE = 2;

        private Service service;
        private ComputingResource computingResource;
        private PublishServer publishServer;
        public List<string> resultMetadataFiles;
        public List<DataSetInfo> outputFiles;

        private Scheduler scheduler;
        private bool canChangeOwner, canChangeAttributes, canChangePublishServer;
        private bool submitChecked, canSubmitNow, canSubmitDelayed;
        private bool newTask;

        // private TaskGroup taskGroup;
        private int oldPublishServerId;
        private string oldCompression;
        private bool hasFailedJobs = false;
        private string metadataUrl;
        
        private ExecutionParameterSet executionParameters = new ExecutionParameterSet();
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the privilege level of the service derivate owner.</summary>
        [EntityForeignField("level", USER_TABLE)]
        public int OwnerUserLevel { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the username of the service derivate owner.</summary>
        [EntityForeignField("username", USER_TABLE)]
        public string Username { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the user caption of the service derivate owner.</summary>
        [EntityForeignField("username", USER_TABLE)]
        public string UserCaption { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the ID of the service that defines the derivate.</summary>
        [EntityDataField("id_service", IsForeignKey = true)]
        public int ServiceId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the service that defines the derivate</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public Service Service {
            get {
                if (service == null && ServiceId != 0) service = Service.FromId(context, ServiceId);
                return service;
            }
            protected set { 
                service = value; 
                ServiceId = (value == null ? 0 : value.Id);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the unique name of service that defines the derivate.</summary>
        [EntityForeignField("identifier", SERVICE_TABLE)]
        public string ServiceIdentifier { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the ID of the computing resource assigned to the derivate.</summary>
        [EntityDataField("id_cr", IsForeignKey = true)]
        public int ComputingResourceId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the computing resource assigned to the derivate.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual ComputingResource ComputingResource {
            get {
                if (Service.FixedComputingResource != null) computingResource = Service.FixedComputingResource;
                else if (ComputingResourceId == 0) computingResource = null;
                else if (computingResource == null || ComputingResourceId != computingResource.Id) computingResource = ComputingResource.FromId(context, ComputingResourceId);
                return computingResource; 
            }
            set { 
                if (Service.FixedComputingResource != null) return;
                computingResource = value;
                ComputingResourceId = (computingResource == null ? 0 : computingResource.Id);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the ID of the publish server assigned to the derivate.</summary>
        [EntityDataField("id_pubserver", IsForeignKey = true)]
        public int PublishServerId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Get or sets the publish server assigned to the derivate.</summary>
        public virtual PublishServer PublishServer {
            get {
                if (publishServer == null && PublishServerId != 0) publishServer = PublishServer.FromId(context, PublishServerId);
                return publishServer; 
            }
            set { 
                publishServer = value;
                PublishServerId = (value == null ? 0 : value.Id);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the processing priority value of the service derivate.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("priority")]
        public double Priority { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the result compression value of the service derivate.</summary>
        [EntityDataField("compression")]
        public string Compression { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether deleted tasks are to be shown or to be hidden.</summary>
        protected bool ShowDeleted { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Get or sets the scheduler that produced this task.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public Scheduler Scheduler {
            get {
                if (scheduler == null && SchedulerId != 0) scheduler = Terradue.Portal.Scheduler.FromId(context, SchedulerId);
                return scheduler; 
            }
            set { 
                scheduler = value;
                SchedulerId = (value == null ? 0 : value.Id);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets an array containing information on each task input file.</summary>
        public CatalogueDataSetCollection InputFiles { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Obsolete, please use Identifier instead.")]
        public string Uid { 
            get { return Identifier; } 
            set { Identifier = value; } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the ID of the scheduler that created the task if it was created by a scheduler.</summary>
        [EntityDataField("id_scheduler", IsForeignKey = true)]
        public int SchedulerId { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the ID of the task group that to which the task belongs if it belongs to a task group.</summary>
        [EntityDataField("id_taskgroup", IsForeignKey = true)]
        public int TaskGroupId { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the task submission may be retried if the first submission attempt fails.</summary>
        protected bool SubmitDelayed { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the resources consumed by the task processing.</summary>
        [EntityDataField("resources")]
        public double Resources { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the cost caused by the task, i.e. the product of priority and consumed resources.</summary>
        public double Cost { 
            get { return Priority * Resources; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the current actual task status.</summary>
        /// <remarks>
        ///     <para>The actual status differs from the logical status if a task operation is pending.</para>
        ///     <para>For example the actual status of a task may still be <i>Active</i> but its logical status is <i>Created</i> if the user aborted it.</para>
        /// </remarks>
        [EntityDataField("status")]
        public int ActualStatus { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the pending asynchronous operation.</summary>
        [EntityDataField("async_op", NullValue = 0)]
        public int AsynchronousOperation { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the current logical task status that is presented to the user.</summary>
        /// <remarks>
        ///     <para>The logical status differs from the actual status if an asynchronous task operation is pending.</para>
        ///     <para>For example an aborted task is presented as <i>Created</i> to the user even if the actual abortion has not yet taken place on the Grid engine.</para>
        /// </remarks>
        /// \ingroup Task
        public int Status { 
            get {
                switch (AsynchronousOperation) {
                    case 0 :
                        return ActualStatus;
                    case TaskOperationType.ConfirmDelay :
                        return ProcessingStatus.Created;
                    case TaskOperationType.Restart :
                    case TaskOperationType.Submit :
                        return ProcessingStatus.Pending;
                        // return (ActualStatus == ProcessingStatus.Created ? ProcessingStatus.Pending : ProcessingStatus.Active);
                    case TaskOperationType.Abort :
                        return ProcessingStatus.Created;
                    case TaskOperationType.Retry :
                        return ProcessingStatus.Active;
                    case TaskOperationType.Delete :
                        return ProcessingStatus.Deleted;
                    default :
                        return ActualStatus;
                }
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the numeric code of the message regarding the state of a task.</summary>
        /// <remarks>For example, the message code may contain an error code that indicating the reason for a failed submission.</remarks>
        [EntityDataField("message_code")]
        public int MessageCode { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        [EntityDataField("message_text")]
        public string MessageText { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates whether the task has been created without input files.</summary>
        /// <remarks>This is useful for automatically created logical sequences of tasks, such as time-driven schedulers.</remarks>
        [EntityDataField("empty")]
        public bool Empty { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates whether the task may be created even if there are no matching input products.</summary>
        /// <remarks>This is useful for automatically created logical sequences of tasks, such as time-driven schedulers.</remarks>
        public bool AllowEmpty { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets or sets the unique identifier of the task by which it is addressed on the computing resource where it is running.</summary>
        [EntityDataField("remote_id")]
        public string RemoteIdentifier { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the URL providing the status of the task.</summary>
        /// <remarks>
        ///     Whether or not this value is set depends on the computing resource on which the task is running.
        ///     Some types of computing resources, e.g. GlobusComputingResource have predictable status URLs and the corresponding rules are configured in the portal database (they use a placeholder for the remote identifier). For those computing resources, the property has the value <c>null</c>.
        ///     Other types of computing resources (e.g. WpsProvider) return specific status URLs within the response to the task creation request. For these the value of this property contains that URL.
        /// </remarks>
        [EntityDataField("status_url")]
        public string StatusUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time of the task's creation.</summary>
        [EntityDataField("creation_time")]
        public DateTime CreationTime { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time for a scheduled task execution.</summary>
        [EntityDataField("scheduled_time")]
        public DateTime ScheduledTime { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time of the task submission.</summary>
        /// \ingroup Task
        [EntityDataField("start_time")]
        public DateTime StartTime { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time of the task completion or failure.</summary>
        /// \ingroup Task
        [EntityDataField("end_time")]
        public DateTime EndTime { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time of the last access to the task.</summary>
        [EntityDataField("access_time")]
        public DateTime LastAccessTime { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets a collection of the jobs that constitute the task.</summary>
        public JobCollection Jobs { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /*/// <summary>Indicates whether the task has started on the Grid engine.</summary>
        public bool Unsubmitted {
            get { return Status < ProcessingStatus.Pending; }
        }*/
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates or determines whether the incoming request is processed as a definition request.</summary>
        /// <remarks>Definition request means that the .</remarks>
        public bool DefinitionMode { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public bool ImmediateSubmission { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates whether the task can be submitted immediately.</summary>
        public bool CanSubmitNow { 
            get { 
                if (!submitChecked) CheckCanSubmit(false);
                return canSubmitNow;
            } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the task can be submitted at a later time.</summary>
        public bool CanSubmitDelayed {
            get { 
                if (!submitChecked) CheckCanSubmit(false);
                return canSubmitDelayed;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the computing resource can be used by the owner of the derivate.</summary>
        public bool CanUseComputingResource { 
            get { return (Service.FixedComputingResource != null || ComputingResource == null || ComputingResource.Availability + OwnerUserLevel > IfyContext.MaxUserLevel); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates !!! whether the task has been set into submission retrying mode.</summary>
        protected bool HasSubmissionRetryingPeriod {
            get { return SubmissionRetryingPeriod != 0; } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the period (in minutes) during which the submission of the task is retried in case of failed submissions.</summary>
        [EntityDataField("retry_period", NullValue = 0)]
        protected int SubmissionRetryingPeriod { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether from the user's point of view the task has been submitted.</summary>
        public bool Submitted {
            get { return Status >= ProcessingStatus.Pending; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the task has been submitted to the Grid engine.</summary>
        public bool Started {
            get { return Status >= ProcessingStatus.Active && Status < ProcessingStatus.Deleted; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the task has started, but not yet ended on the Grid engine.</summary>
        public bool Active {
            get { return Status >= ProcessingStatus.Active && Status < ProcessingStatus.Failed; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the task has finished successfully or failed.</summary>
        public bool Ended { 
            get { return Status >= ProcessingStatus.Failed && Status < ProcessingStatus.Deleted; } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the task has finished sucessfully.</summary>
        public bool Finished {
            get { return Status >= ProcessingStatus.Completed && Status < ProcessingStatus.Deleted; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the latest task status message.</summary>
        public string StatusMessage { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the time stamp of the latest task status message.</summary>
        public DateTime StatusMessageTime { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the message type of the latest task status message.</summary>
        public MessageType StatusMessageType { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string RelativeUrl {
            get {
                string url;
                IfyWebContext webContext = context as IfyWebContext;
                if (context.AdminMode && webContext != null && webContext.AdminRootUrl != null) url = "{1}/{2}/{0}";  
                else if (webContext != null && webContext.TaskWorkspaceRootUrl != null) url = "{3}/{0}"; 
                else url = "/tasks?uid={0}";
                return String.Format(url, Exists ? Identifier : String.Empty, webContext.AdminRootUrl, EntityType.GetEntityType(this.GetType()).Keyword, webContext.TaskWorkspaceRootUrl);
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the base URL for the publishing of the task result information files, such as image previews.</summary>
        public string MetadataUrl {
            get {
                if (metadataUrl != null) return metadataUrl;
                
                metadataUrl = context.GetQueryStringValue(
                        String.Format("SELECT CASE WHEN t.download_url IS NOT NULL THEN t.download_url WHEN t.upload_url IS NOT NULL THEN t.upload_url ELSE CONCAT(t.protocol, '://', CASE WHEN t.username IS NULL THEN '' ELSE CONCAT(t.username, CASE WHEN t.password IS NULL THEN '' ELSE CONCAT(':', t.password) END, '@') END, t.hostname, CASE WHEN t.port IS NULL THEN '' ELSE CONCAT(':', CAST(t.port AS char)) END, CASE WHEN t.path IS NULL THEN '' ELSE CONCAT('/', t.path) END) END FROM pubserver AS t WHERE t.metadata OR t.id={0} ORDER BY t.metadata DESC;", 
                                PublishServerId
                        )
                );
                return (metadataUrl == null ? null : metadataUrl.Replace("$(UID)", Identifier));
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public ExecutionParameterSet ExecutionParameters {
            get { return executionParameters; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the base URL of the task results.</summary>
        public string ResultUrl { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public List<string> ResultMetadataFiles {
            get {
                if (resultMetadataFiles == null) resultMetadataFiles = new List<string>();
                return resultMetadataFiles;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public List<DataSetInfo> OutputFiles {
            get {
                if (outputFiles == null) outputFiles = new List<DataSetInfo>();
                return outputFiles;
            }
        }


        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the base URL of the task results.</summary>
        public string ResultFileRootDir {
            get {
                if (PublishServer == null) return null;
                string dir = PublishServer.FileRootDir;
                return (dir == null ? null : dir.Replace("$(UID)", Identifier));
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the callback method that is executed after a successful task creation.</summary>
        public TaskOperationCallbackType OnCreated { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the callback method that is executed after a successful task submission.</summary>
        public TaskOperationCallbackType OnSubmitted { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates whether temporal parameters were used to create the task.</summary>
        public bool IsTemporal { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string DownloadProviderUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Task instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Task(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Task instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created Task object</returns>
        public static new Task GetInstance(IfyContext context) {
            EntityType entityType = EntityType.GetEntityType(typeof(Task));
            return (Task)entityType.GetEntityInstance(context); 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static Task ForService(IfyContext context, Service service) {
            Task result = GetInstance(context);
            result.Service = service;
            result.Jobs = new JobCollection(context, result);
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static Task ForScheduler(IfyContext context, Scheduler scheduler) {
            Task result = GetInstance(context);
            result.scheduler = scheduler;
            result.SchedulerId = scheduler.Id;
            result.GetAttributesFromScheduler();
            result.Jobs = new JobCollection(context, result);
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Task instance representing the task with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the task ID</param>
        /// <returns>the created Task object</returns>
        public static Task FromId(IfyContext context, int id) {
            return FromId(context, id, null, true);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Task instance representing the task with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the task ID</param>
        /// <param name="service">the processing service on which the task is based</param>
        /// <param name="complete">determines whether also the job information is loaded</param>
        /// <returns>the created Task object</returns>
        public static Task FromId(IfyContext context, int id, Service service, bool complete) {
            Task result = GetInstance(context);
            result.Id = id;
            result.Load();
            if (service != null) {
                if (result.ServiceId != service.Id) context.ReturnError("The requested task does not derive from this service");
                result.Service = service;
            }
            if (complete) result.LoadJobs();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Task instance representing the task with the specified UID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="identifier">the task UID</param>
        /// <returns>the created Task object</returns>
        public static Task FromIdentifier(IfyContext context, string identifier) {
            return FromIdentifier(context, identifier, null, true);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Task instance representing the task with the specified UID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="identifier">the task UID</param>
        /// <param name="service">the processing service on which the task is based</param>
        /// <param name="complete">determines whether also the job information is loaded</param>
        /// <returns>the created Task object</returns>
        public static Task FromIdentifier(IfyContext context, string identifier, Service service, bool complete) {
            Task result = GetInstance(context);
            result.Identifier = identifier;
            result.Load();
            if (service != null) {
                if (result.ServiceId != service.Id) context.ReturnError("The requested task does not derive from this service");
                result.Service = service;
            }
            if (complete) result.LoadJobs();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Task instance representing the task with the specified UID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="uid">the task UID</param>
        /// <returns>the created Task object</returns>
        public static Task FromUid(IfyContext context, string uid) {
            return FromIdentifier(context, uid, null, true);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the information about a specific task from the database.</summary>
        /// <param name="condition">the SQL conditional clause identifying the desired task (without <c>WHERE</c> keyword)</param>
        public override void Load() {
            base.Load();
            oldPublishServerId = PublishServerId;
            oldCompression = Compression;
            Jobs = new JobCollection(context, this);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the task parameters from the database.</summary>
        public void LoadParameters() {
/*            Service service = this.Service;
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT name, type, value FROM taskparam WHERE id_task={0} AND id_job IS NULL AND metadata IS NOT NULL;", Id), dbConnection);
            while (reader.Read()) RequestParameters.GetParameter(context, service, reader.GetString(0), reader.GetString(1), null, reader.GetString(2));
            context.CloseQueryResult(reader, dbConnection);*/

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT name, type, value FROM taskparam WHERE id_task={0} AND id_job IS NULL;", Id), dbConnection);
            while (reader.Read()) this.ExecutionParameters.Add(reader.GetString(0), reader.GetString(2));
            context.CloseQueryResult(reader, dbConnection);

        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the jobs of the task from the database and gets their status from a defined source.</summary>
        /// <remarks>
        ///     The status request method is specified in the configuration variable <i>StatusRequestMethod</i>. According to that value, the job status may be
        ///     <list type="bullet">
        ///         <item>not requested at all (the status value in the database is used in this case)</item>
        ///         <item>requested from the Grid engine via the <i>wsServer</i> web service</item>
        ///         <item>requested from the Grid engine via the job XML file</item>
        ///     </list>
        /// </remarks>
        public void LoadJobs() {
            // Load jobs
            //string sql = String.Format("SELECT t.id, t.name, t.job_type, t.grid_type, t.status, t.start_time, t.end_time FROM job AS t WHERE t.id_task={0} ORDER BY t.id;", Id);
            //Jobs.Clear();
            
            string sql = Job.GetSqlQuery(String.Format("t.id_task={0}", Id), "t.id");
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                Job job = Job.OfTask(context, this);
                job.Load(reader);
                Jobs.Add(job);
                    //reader.GetInt32(0), context.GetValue(reader, 1), context.GetValue(reader, 2), Status >= ProcessingStatus.Active ? reader.GetInt32(3) : ProcessingStatus.Created); // !!! find a better way of dealing with the jobs' statuses (generally)
            }
            reader.Close();

            // Load job dependencies
            sql = String.Format("SELECT j1.name, j2.name FROM jobdependency jd INNER JOIN job AS j1 ON jd.id_job=j1.id INNER JOIN job AS j2 ON jd.id_job_input=j2.id WHERE j1.id_task={0} AND j2.id_task={0};", Id);
            reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) Jobs[reader.GetValue(0).ToString()].AddDependency(Jobs[reader.GetValue(1).ToString()]);
            reader.Close();
            context.CloseDbConnection(dbConnection);

            int oldStatus = Status;
            //bool allCompletedOrIncomplete = true, allCompleted = true, oneFailed = false, oneActiveOrPending = false; // initial settings
            
            if (Started) { // if task has started (from user-point of view)
                GetStatus(); // TODO: GetStatus not ideal here, when ended, no more status requests are needed
                if (!Ended) {// (StatusRequestMethod == RequestMethodType.StatusFile) {
                    for (int i = 0; i < Jobs.Count; i++) {
                        if (Jobs[i].Status == ProcessingStatus.Failed) {
                            hasFailedJobs = true;
                            break;
                        }
                    }
                /*} else {
                    for (int i = 0; i < Jobs.Count; i++) { // !!! STATUS PROPERTIES !!!
                        if (StatusRequestMethod > RequestMethodType.NoRefresh) Jobs[i].GetStatus();
                        allCompletedOrIncomplete &= (Jobs[i].Status == ProcessingStatus.Completed || Jobs[i].Status == ProcessingStatus.Incomplete);
                        allCompleted &= (Jobs[i].Status == ProcessingStatus.Completed);
                        oneFailed |= (Jobs[i].Status == ProcessingStatus.Failed);
                        oneActiveOrPending |= (Jobs[i].Status == ProcessingStatus.Active || Jobs[i].Status == ProcessingStatus.Pending);
                    }
                    
                    if (oneFailed) {
                        hasFailedJobs = true;
                        ActualStatus = ProcessingStatus.Failed;
                    } else if (oneActiveOrPending) {
                        ActualStatus = ProcessingStatus.Active;
                    } else if (allCompleted) {
                        ActualStatus = ProcessingStatus.Completed;
                    } else if (allCompletedOrIncomplete) {
                        ActualStatus = ProcessingStatus.Incomplete;
                    } else {
                        //status = ProcessingStatus.Created;
                    }*/
                }
            }

            if (Status != oldStatus) {
                if (Ended) {
                    for (int i = 0; i < Jobs.Count; i++) if (Jobs[i].EndTime > EndTime) EndTime = Jobs[i].EndTime;
                } else {
                    EndTime = DateTime.MinValue;
                }
                Store();
                ProcessNewStatus(); 
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>When overridden in a derived class, performs additional operations when the status has changed.</summary>
        protected virtual void ProcessNewStatus() {} 

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Inserts the task and its parameters and jobs into the database.</summary>
        /// <returns>the ID of the new task</returns>
        public override void Store() {
            bool isNew = !Exists;

            if (isNew && ActualStatus == ProcessingStatus.None) ActualStatus = (Empty ? ProcessingStatus.Precreated : ProcessingStatus.Created);

            if (Name == null) {
                Name = "Unnamed \"" + Service.Identifier + "\" task";
            } else if (Name.Contains("$(")) {
                /*for (int i = 0; i < RequestParameters.Count; i++) {
                    RequestParameter param = RequestParameters[i]; 
                    if (param.Type == "caption" || param.Value == null) continue;
                    Name = Name.Replace("$(" + param.Name + ")", param.Value); 
                }*/
            }
            if (Resources == 0) Resources = 1;

            if (isNew && Identifier == null) {
                Identifier = Guid.NewGuid().ToString();
                CreationTime = context.Now;
            }

            LastAccessTime = context.Now; 

            // if (context.DebugLevel >= 3) context.AddDebug(3, (isNew ? "Insert" : "Update") + " task in database: " + sql); // TODO debug this
            base.Store();

            if (isNew) {
                if (context.DebugLevel >= 2) context.AddDebug(2, String.Format("Task {0} ({1}) inserted in database", Identifier, Id));
            } else {
                context.Execute(String.Format("DELETE FROM taskparam WHERE id_task={0};", Id));
                context.Execute(String.Format("DELETE FROM job WHERE id_task={0};", Id));
                if (context.DebugLevel >= 2) context.AddDebug(2, String.Format("Task {0} ({1}) updated in database", Identifier, Id));
            }

            /*for (int i = 0; i < RequestParameters.Count; i++) {
                RequestParameter param = RequestParameters[i]; 
                //context.AddInfo(param.Name + " : " param.Type);
                if (param.Ignore || !param.IsRow || param.Value == null) continue;
                switch (param.Type) {
                    case "starttime" :
                        case "startdate" :
                        hasStartTime = true;
                        DateTime.TryParse(param.Value, out startTime);
                        IsTemporal = true;
                        break;
                        case "endtime" :
                        case "enddate" :
                        hasEndTime = true;
                        DateTime.TryParse(param.Value, out endTime);
                        IsTemporal = true;
                        break;
                }
                string sql = String.Format("INSERT INTO taskparam (id_task, name, type, value, metadata) VALUES ({0}, {1}, {2}, {3}, true);", 
                                           Id,
                                           StringUtils.EscapeSql(param.Name),
                                           StringUtils.EscapeSql(param.Type),
                                           StringUtils.EscapeSql(param.Value)
                                           );
                context.Execute(sql);
            }
            if (context.DebugLevel >= 3) context.AddDebug(2, "Task parameters inserted in database");

            if (IsTemporal) {
                string sql = String.Format(isNew ? "INSERT INTO temporaltask (id_task, start_time, end_time) VALUES ({0}, {1}, {2});" : "UPDATE temporaltask SET start_time={1}, end_time={2} WHERE id_task={0};",
                                           Id,
                                           (hasStartTime ? "'" + startTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss") + "'" : "NULL"),
                                           (hasEndTime ? "'" + endTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss") + "'" : "NULL")
                                           );
                try {
                    context.Execute(sql);
                } catch (Exception e) {
                    context.ReturnError("Could not create temporal data for task " + Identifier + ": " + e.Message);
                }
            }*/

            // Insert jobs and job parameters into database    
            if (isNew && Jobs != null) {
                for (int i = 0; i < Jobs.Count; i++) Jobs[i].Store();
                if (context.DebugLevel >= 2) context.AddDebug(2, "Jobs inserted in database");
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void SetExecutionParameters(ServiceParameterArray serviceParameters) {
            if (serviceParameters == null) return;
            foreach (ServiceParameter parameter in serviceParameters) ExecutionParameters.Add(parameter.Name, parameter.Value);
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void GetAttributesFromScheduler() {
            UserId = scheduler.UserId;
            /*if (Service == null) Service = scheduler.Service;
            ComputingResourceId = scheduler.ComputingResourceId;
            PublishServerId = scheduler.PublishServerId;
            Priority = scheduler.Priority;
            Compression = scheduler.Compression;*/
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the task status and job details according from a defined source.</summary>
        /// <returns>the job status</returns>
        /// <remarks>
        ///     The status request method is specified in the configuration variable <i>StatusRequestMethod</i>. According to that value, the job status may be
        ///     <list type="bullet">
        ///         <item>not requested at all (the status value in the database is used in this case)</item>
        ///         <item>requested from the Grid engine via the <i>wsServer</i> web service</item>
        ///         <item>requested from the Grid engine via the job XML file</item>
        ///     </list>
        /// </remarks>
        public bool GetStatus() {
            // Change the task status in the database
            int oldStatus = Status;

            bool result = ComputingResource.GetTaskStatus(this);

            if (result) {
                if (StatusMessageType == MessageType.Error) throw new Exception(StatusMessage);

                
                if (Status != oldStatus) {
                    if (oldStatus == ProcessingStatus.Created) StartTime = DateTime.UtcNow;
                    if (!Started) StartTime = DateTime.MinValue;
                    if (Ended) EndTime = DateTime.UtcNow; // !!! STATUS PROPERTIES !!!
                    Store();
                    context.AddDebug(3, "Task updated in database (status)");
                }
            }

            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates the task using the task's service access point.</summary>
        /// <param name="sendMessages">determines whether returning messages are sent to the client</param>
        /// <param name="throwOnError">determines whether in case of error, an exception is thrown</param>
        /// <remarks>
        ///     The method sends a local HTTP request to the service access point (index.aspx) requesting the task creation and evaluates the response.
        ///     Thus, the actual task creation takes place in a different process, but this is transparent to the caller, as any information returning in the response (such as error messages) can be included in the current Task instance.
        /// </remarks>
        public virtual void Build() {
            /*if (Status != ProcessingStatus.None) {
                context.AddError("The task has already been created");
                return;// true;
            }*/

            Service.BuildTask(this);
            
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void StoreParameters(ServiceParameterSet parameters) {
            // Insert custom task parameters (not task attributes) that have a value into database
            bool hasStartTime = false, hasEndTime = false;
            DateTime startTime = DateTime.MinValue, endTime = DateTime.MinValue;
            foreach (ServiceParameter param in parameters) {
                //context.AddInfo(param.Name + " : " param.Type);
                if (param.Ignore || /*!param.IsRow || */param.Value == null) continue;
                switch (param.Type) {
                    case "starttime" :
                    case "startdate" :
                        hasStartTime = true;
                        DateTime.TryParse(param.Value, out startTime);
                        IsTemporal = true;
                        break;
                    case "endtime" :
                    case "enddate" :
                        hasEndTime = true;
                        DateTime.TryParse(param.Value, out endTime);
                        IsTemporal = true;
                        break;
                }
                string sql = String.Format("INSERT INTO taskparam (id_task, name, type, value, metadata) VALUES ({0}, {1}, {2}, {3}, true);", 
                        Id,
                        StringUtils.EscapeSql(param.Name),
                        StringUtils.EscapeSql(param.Type),
                        StringUtils.EscapeSql(param.Value)
                );
                context.Execute(sql);
            }
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Updates the task attributes in the database.</summary>
        /// <remarks>
        ///     The task parameters cannot be modified, since the job parameters depend on the task parameters and the task would have to be rebuilt completely.
        ///     A user who wants to modify the task parameters has to clone the task and create a new one.
        /// </remarks>
        public virtual void Modify() {
            // Determine the attributes to be updated (user, priority, computing resource, publish server and result compression value) 
            // depending on the user privileges and the task status
            
            if (CanModify) {
                Store();
                if (context.DebugLevel >= 2) context.AddDebug(2, "Task attributes updated in database");
            } else {
                context.AddInfo("The task attributes have not been changed");
            }
            
            // If a publishing-related attribute has changed (publish server or compression), modify the job parameters on the Grid engine.
            if (Started && canChangePublishServer && (PublishServerId != oldPublishServerId || Compression != oldCompression)) {
                for (int i = 0; i < Jobs.Count; i++) if (Jobs[i].Publishes) Jobs[i].UpdateRemote();  
            }
            
            //warnOnlyIfInvalid = true;
        }    
            
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates an exact copy of the task in the <i>Created</i> state.</summary>
        /// <returns>The copy of the task</returns>
        public virtual Task Copy() {
            Task task = Task.FromId(context, Id);
            task.CopyOver();
            return task;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates the current task again in the database and transforms the current instance in a representation of that new task.</summary>
        /// <returns><c>true</c> if the creation was successful</returns>
        /// <remarks>The new task contains the same jobs and parameters as the original task. The status of the new task is <i>Created</i>.</remarks> 
        public virtual bool CopyOver() {
            int oldId = Id;
            string oldIdentifier = Identifier;
            //LoadParameters(); -> COMMENTED BY ENGUE (09/09/2014)
            /*for (int i = 0; i < RequestParameters.Count; i++) RequestParameters[i].IsRow = true;
            for (int i = 0; i < Jobs.Count; i++) {
                Jobs[i].LoadParameters();
                Jobs[i].Copy();
            }
            */
            // Reset the key fields and foreign key fields referring to task aggregators
            Id = 0;
            Identifier = null;
            SchedulerId = 0;
            TaskGroupId = 0;
            
            if (context.DebugLevel >= 3) context.AddDebug(3, "Copy task " + oldIdentifier);
            // Save the task 
            CanModify = false;
            Store();
            if (Id == oldId) {
                context.AddError("Could not copy task " + oldIdentifier + " (" + oldId + ")");
                return false;
            }
            if (context.DebugLevel >= 2) context.AddDebug(2, "Task " + oldIdentifier + " (" + oldId + ") copied to " + Identifier + " (" + Id + ")");
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Submits the task to the Grid engine or marks it for been submitted by the background agent.</summary>
        /// <returns><c>true</c> if the submission was successful</returns>
        /// <remarks>
        ///     The web portal configuration setting <i>SynchronousTaskOperations</i> determines the submission mode:
        ///     <list type="bullet">
        ///         <item>If set to <c>true</c>, the task is submitted immediately to the Grid engine (with longer response time for the client).</item> 
        ///         <item>If set to <c>false</c>, the task's next status is set to <i>Pending</i>, which causes the background agent on its next run to actually submit the task to the Grid engine.</item>  
        ///     </list>
        ///     If the task is not in the <i>Created</i> or <i>Pending</i> state, it is aborted first.
        /// </remarks>
        public virtual bool Submit() {
            bool asyncSubmit = false;
            
            if (!submitChecked) CheckCanSubmit(true);
            
            if (ImmediateSubmission) {
                if (CanSubmitNow) {
                    return DoSubmit(false);
                } else {
                    context.AddError(MessageText, GetMessageClass(MessageCode));
                    return false;
                }
                
            } else if (context.SynchronousTaskOperations) {
                if (CanSubmitNow) {
                    return DoSubmit(false);
                    
                } else if (CanSubmitDelayed) {
                    
                    //context.AddWarning(GetMessageText(MessageCode), GetMessageClass(MessageCode));
                    /*if (AsynchronousOperation == TaskOperationType.Submit) {
                        context.AddInfo("Task remains in pending state");
                        asyncSubmit = (MessageCode != oldMessageCode);
                    } else {*/
                        
                        //context.AddInfo("Task is set to pending state");
                        asyncSubmit = true;
                        
                    /*}*/
                } else {
                    //context.AddError(GetMessageText(MessageCode), GetMessageClass(MessageCode));
                    return false;
                }
                
            } else {
                asyncSubmit = true;
            }
            
            if (asyncSubmit) {
                StartTime = context.Now;
                AsynchronousOperation = (context.TaskSubmissionRetrying == SubmissionRetryingType.AskUser && !SubmitDelayed && !CanSubmitNow ? TaskOperationType.ConfirmDelay : TaskOperationType.Submit);
                //context.ReturnError(context.TaskSubmissionRetrying + " " + SubmissionRetryingType.AskUser + " " + SubmitDelayed + " " + AsynchronousOperation); 
                if (MessageCode != 0) {
                    context.AddWarning(
                            MessageText + "; " + (AsynchronousOperation == TaskOperationType.ConfirmDelay ? "for automatic task submission as soon as possible press \"Submit When Possible\"" : "task will be submitted automatically as soon as possible"), 
                            GetMessageClass(MessageCode)
                    );
                }
                EndTime = DateTime.MinValue;
                Store();
                return true;
            }
            
            return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Resubmits the failed jobs of the task to the Grid engine or marks the task for resubmission of its failed jobs to be performed by the background agent.</summary>
        /// <remarks>
        ///     The web portal configuration variable <i>SynchronousTaskOperations</i> determines the submission mode:
        ///     <list type="bullet">
        ///         <item>If set to <c>true</c>, the failed jobs are resubmitted immediately to the Grid engine (with longer response time for the client).</item>
        ///         <item>If set to <c>false</c>, the task's next status is set to <i>Active</i>, which causes the background agent on its next run to actually resubmit the failed jobs to the Grid engine.</item>  
        ///     </list>
        ///     If the task is in the <i>Failed</i> state, all its jobs in the <i>Failed</i> state are resubmitted.
        /// </remarks>
        public virtual void Retry() {
            if (!hasFailedJobs) {
                context.AddError("The task has no failed jobs");
                AsynchronousOperation = TaskOperationType.None;
                Store();
                return;
            }
            
            if (context.SynchronousTaskOperations) {
                DoRetry();
                
            } else {
                AsynchronousOperation = TaskOperationType.Retry;
                Store();
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Aborts the task on the Grid engine or marks it for been aborted by the background agent.</summary>
        /// <remarks>
        ///     The web portal configuration setting <i>SynchronousTaskOperations</i> determines the abortion mode:
        ///     <list type="bullet">
        ///         <item>If set to <c>true</c>, the task is aborted immediately on the Grid engine (with longer response time for the client).</item> 
        ///         <item>If set to <c>false</c>, the task's next status is set to <i>Created</i>, which causes the background agent on its next run to actually abort the task on the Grid engine.</item>  
        ///     </list>
        ///     The task is aborted only if it is not in the <i>Created</i> or <i>Pending</i> state and if it has a Grid session ID.
        /// </remarks>
        public virtual void Abort() {
            if (Status == ProcessingStatus.Created && ActualStatus != Status) {
                context.AddWarning("The task is already being aborted");
                return;
            } else if (!Submitted && AsynchronousOperation != TaskOperationType.ConfirmDelay) {
                context.AddError("The task has not been submitted");
                return;
            }

            if (context.SynchronousTaskOperations) {
                DoAbort(false);

            } else {
                RemoteIdentifier = null;
                AsynchronousOperation = (AsynchronousOperation == TaskOperationType.ConfirmDelay ? TaskOperationType.None : TaskOperationType.Abort);
                Store();
            }
            CheckCanModify();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Deletes a task or marks the task for deletion to be performed by the background agent.</summary>
        /// <remarks>
        ///     The web portal configuration setting <i>SynchronousTaskOperations</i> determines the deletion mode:
        ///     <list type="bullet">
        ///         <item>If set to <c>true</c>, the jobs is aborted on the Grid engine (if it is not in <i>Created</i> or <i>Pending</i> state) immediately and deleted.</item>
        ///         <item>If set to <c>false</c>, the task's next status is set to <i>Deleted</i>, which causes the background agent on its next run to abort it on the Grid engine (if it is not in <i>Created</i> or <i>Pending</i> state) and actually delete it.</item>  
        ///     </list>
        /// </remarks>
        public override void Delete() {
            if (context.SynchronousTaskOperations || Status == ProcessingStatus.None) {
                DoDelete();
                
            } else {
                AsynchronousOperation = TaskOperationType.Delete;
                Store();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Submits or resubmits the task with all its jobs to the Grid engine.</summary>
        /// <returns><c>true</c> if the submission was successful</returns>
        /// <remarks>
        ///     If the task is not in the <i>Created</i> or <i>Pending</i> state, it is aborted first. 
        /// </remarks>
        protected virtual bool DoSubmit(bool check) {
            int oldMessageCode = MessageCode;

            if (Started || RemoteIdentifier != null) DoAbort(false);

            if (check) {
                if (!submitChecked) CheckCanSubmit(true);
                if (!CanSubmitNow) {
                    if (CanSubmitDelayed) {
                        context.AddWarning(MessageText, GetMessageClass(MessageCode));
                        if (AsynchronousOperation != TaskOperationType.Submit || !HasSubmissionRetryingPeriod) {
                            context.AddInfo("Task is set to pending state");
                            StartTime = context.Now;
                            ActualStatus = ProcessingStatus.Created;
                            AsynchronousOperation = TaskOperationType.Submit;
                            SubmissionRetryingPeriod = GetSubmissionRetryingPeriodMinutes();
                            Store();
                        } else {
                            context.AddInfo("Task remains in pending state");
                            if (MessageCode != oldMessageCode) Store();
                        }
                        return false;
                            
                    } else {
                        // !!! SET TASK TO FAILED
                        EndTime = context.Now; 
                        ActualStatus = ProcessingStatus.Failed;
                        AsynchronousOperation = TaskOperationType.None;
                        SubmissionRetryingPeriod = 0;
                        Store();
                        if (MessageCode == 0) context.AddError("Task failed before submission");
                        else context.AddError("Task failed before submission: " + MessageText, GetMessageClass(MessageCode));
                        return false;
                    }
                }
            }
            
            // Instantiate task on computing resource (specific for computing resources)
            bool result = ComputingResource.CreateTask(this);
            
            if (result) {
                Store();
            
                context.AddDebug(1, "Ready to start task execution");
                try {    
                    result &= ComputingResource.StartTask(this);
                } catch (Exception e) {
                    context.AddError("Could not submit task " + Identifier + ": " + (e.InnerException == null ? e.Message : e.InnerException.Message));
                    result = false;
                }
                
                if (result) {
                    if (result) context.AddDebug(1, "Task submitted");
                    if (!Started) ActualStatus = ProcessingStatus.Active;
                    StartTime = context.Now;
                    if (Ended) EndTime = context.Now;
                    Store();
                    context.AddDebug(2, "Task updated in database (status)");
    
                    // Call the callback method for the submission
                    if (OnSubmitted != null) OnSubmitted(this);
                    context.AddDebug(2, "Custom callback method called");
    
                } else {
                    context.AddError("Could not submit task " + Identifier); // !!!
                }
            }
                
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Resubmits the failed jobs of the task to the Grid engine.</summary>
        /// <returns>the number of resubmitted jobs</returns>
        /// <remarks>
        ///     If the task is in the <i>Failed</i> state, all its jobs in the <i>Failed</i> state are resubmitted and the task status is set to <i>Active</i>.
        /// </remarks>
        protected int DoRetry() {
            if (!hasFailedJobs) {
                context.AddError("The task has no failed jobs");
                return 0;
            }
            int result = 0;
            bool success = true;
            for (int i = 0; i < Jobs.Count && success; i++) {
                if (Jobs[i].Status == ProcessingStatus.Failed) {
                    success &= Jobs[i].Resubmit();
                    if (success) result++;
                }
            }
            //if (success) { !!!
            AsynchronousOperation = TaskOperationType.None;
            MessageCode = 0;
            MessageText = null;
            //}
            
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Aborts the task on the Grid engine.</summary>
        /// <param name="cleanup">determines whether the folder containing the task results on the publish server is deleted</param>
        /// <returns><c>true</c> if the abortion was successful</returns>
        /// <remarks>
        ///     The task is aborted only if it is not in the <i>Created</i> or <i>Pending</i> state and if it has a Grid session ID. The task status is set to <i>Created</i>.
        /// </remarks>
        protected virtual bool DoAbort(bool cleanup) {
            return DoAbort(cleanup, false);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Aborts the task on the Grid engine.</summary>
        /// <param name="cleanup">determines whether the folder containing the task results on the publish server is deleted</param>
        /// <param name="ignoreCertificateError">determines whether the task is considered successfully aborted even if the was a problem with the proxy certificate</param>
        /// <returns><c>true</c> if the abortion was successful</returns>
        /// <remarks>
        ///     The task is aborted only if it is not in the <i>Created</i> or <i>Pending</i> state and if it has a Grid session ID. The task status is set to <i>Created</i>.
        /// </remarks>
        protected virtual bool DoAbort(bool cleanup, bool ignoreCertificateError) {
            bool success = ComputingResource.StopTask(this);
            
            if (success) {
                context.Execute(
                        String.Format("DELETE FROM jobnode WHERE id_job IN (SELECT id FROM job WHERE id_task={0});",
                                Id
                        )
                );
                context.Execute(
                        String.Format("UPDATE job SET status={1}, start_time=NULL, end_time=NULL WHERE id_task={0};",
                                Id,
                                ProcessingStatus.Created
                        )
                );
                //if (Status != ProcessingStatus.Deleted) {
                //}
                SubmissionRetryingPeriod = 0;
                ActualStatus = ProcessingStatus.Created;
                if (AsynchronousOperation != TaskOperationType.Delete) AsynchronousOperation = TaskOperationType.None;
                Store();

                context.AddDebug(2, "Task updated in database (jobs reset, status)");

                if (cleanup && ResultFileRootDir != null) {
                    try {
                        System.IO.Directory.Delete(ResultFileRootDir, true);
                        context.AddDebug(1, "Task result directory deleted");
                    } catch (Exception e) {
                        context.AddWarning(String.Format("Could not delete task result directory: {0}", e.Message));
                    }
                }

            } else {
                if (ignoreCertificateError) MessageCode = 0;
                else AsynchronousOperation = TaskOperationType.None;
                if (ActualStatus != ProcessingStatus.Created || AsynchronousOperation == TaskOperationType.Abort) AsynchronousOperation = TaskOperationType.None;
                Store();
            }
            
            return success;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Deletes the job from the Grid engine.</summary>
        /// <remarks>
        ///     If the task is not in the <i>Created</i> or <i>Pending</i> state, it is aborted first. 
        /// </remarks>
        protected bool DoDelete() {
            if (Submitted && !DoAbort(true)) return false;
            
            context.Execute(String.Format("DELETE FROM task WHERE id={0};", Id));
            ActualStatus = ProcessingStatus.Deleted;
            
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected int GetSubmissionRetryingPeriodMinutes() {
            int result = (HasSubmissionRetryingPeriod ? SubmissionRetryingPeriod : (int)context.DefaultTaskSubmissionRetryingPeriod.TotalMinutes);
            if (result <= 0) result = 1;
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual string GetMessageClass(int code) {
            switch (code) {
                case TaskMessageCode.UserCreditsExceeded :
                    return "userCreditsExceeded";
                case TaskMessageCode.ComputingResourceNotAvailable :
                    return "computingResourceNotAvailable";
                case TaskMessageCode.ComputingResourceBusy :
                    return "computingResourceBusy";
                case TaskMessageCode.ComputingResourceSchedulerLimit :
                    return "computingResourceSchedulerLimit";
                default :
                    return null;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        protected virtual void CheckCanSubmit(bool showMessage) {
            canSubmitNow = false;
            canSubmitDelayed = false;
            submitChecked = true;
            
            if (Empty) {
                if (showMessage) context.AddError("The task has no input data");
                return;
            }
            
            if (ComputingResource == null) {
                if (showMessage) context.AddError("No computing resource selected");
                return;
            }
            
            MessageCode = 0;
            bool pendingRequired = false;

            // Check task cost (skip if task is already submitted because then the cost check has already been done)
            if (!pendingRequired && Cost > context.GetAvailableCredits(UserId) + (Started ? Cost : 0)) {
                MessageCode = TaskMessageCode.UserCreditsExceeded;
                MessageText = "The task cost exceeds the available credits";
                pendingRequired = true;
            }
            
            if (!CanUseComputingResource) {
                MessageCode = TaskMessageCode.ComputingResourceNotAvailable;
                MessageText = "The selected Computing Element is currently not available";
                pendingRequired = true;
            }

            // Check the task weight on the computing resource
            ComputingResource.GetStatus();
            if (!pendingRequired && Priority > ComputingResource.FreeCapacity + (Started ? Priority : 0)) {
                MessageCode = TaskMessageCode.ComputingResourceBusy;
                MessageText = "The selected Computing Element has no free capacity to process the task";
                pendingRequired = true;
            }

/*            if (!pendingRequired && SchedulerId != 0 && 100 * (ComputingResource.GetUsedCapacity(Scheduler) / ComputingResource.TotalCapacity) > Scheduler.MaxComputingResourceUsage) {
                MessageCode = TaskMessageCode.ComputingResourceSchedulerLimit;
                MessageText = String.Format("The Computing Element has no free capacity to process the task ({0}% capacity limit for this scheduler)", Scheduler.MaxComputingResourceUsage);
                pendingRequired = true;
            }*/
            
            if (pendingRequired) {
                if (context.TaskSubmissionRetrying == SubmissionRetryingType.Never && showMessage) context.AddError(MessageText, GetMessageClass(MessageCode));
                //context.AddInfo(context.TaskSubmissionRetrying + " " + SubmissionRetryingType.Never + " " + AsynchronousOperation + " " + context.Now + " " + StartTime + " " + GetSubmissionRetryingPeriodMinutes());
                canSubmitDelayed = (!ImmediateSubmission && context.TaskSubmissionRetrying != SubmissionRetryingType.Never && (AsynchronousOperation != TaskOperationType.Submit || context.Now < StartTime.AddMinutes(GetSubmissionRetryingPeriodMinutes())));
                //context.AddInfo("CSD " + canSubmitDelayed + " ");
                return;
            }
            
            canSubmitNow = ComputingResource.CanStartTask(this);
            
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Determines which of the task attributes can be modified.</summary>
        /// </remarks>
        ///     The possibility to change a task attribute depends on the privileges of the user and of the task and job status:
        ///     <list type="bullet">
        ///         <item>The <b>computing resource</b> and the <b>priority value</b> can be changed only if the task is in the <i>Created</i> state.</item>
        ///         <item>The <b>publish server</b> and the <b>result compression value</b> can be changed only if no publishing job has started its execution yet.</item>
        ///         <item>The <b>owner</b> can be changed only by administrators.</item>
        ///     </list>
        /// </remarks>
        protected virtual void CheckCanModify() {
            //AcceptsInitialization = ((OperationType & (ServiceOperationType.Modify | ServiceOperationType.Submit)) != 0);
            
            // Only administrators can change the owner of a task
            canChangeOwner = (context.UserLevel >= UserLevel.Administrator && SchedulerId == 0);
            
            // Priority, computing resource etc. can only be changed if the task has not been submitted yet (even if it is still pending)
            canChangeAttributes = !Submitted;
            
            // The publish server can be changed only when none of the Publish jobs has been started yet
            canChangePublishServer = true;
            for (int i = 0; i < Jobs.Count && canChangePublishServer; i++) {
                canChangePublishServer &= (!Jobs[i].Publishes || Jobs[i].Status == ProcessingStatus.Created || Jobs[i].Status == ProcessingStatus.Failed);
            }

            // So the task itself can be changed only if any of the above can be changed
            CanModify = (canChangeOwner || canChangeAttributes || canChangePublishServer);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        //!
        public void GetResult() {
            bool result = ComputingResource.GetTaskResult(this);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string[] GetResultFiles() {
            GetResult();
            if (OutputFiles == null) return new string[0];
            
            string[] files = new string[OutputFiles.Count];
            for (int i = 0; i < OutputFiles.Count; i++) {
                files[i] = OutputFiles[i].Resource;
            }
            return files;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public XmlDocument GetResultDocument(string xslFile) {
            XmlDocument result = new XmlDocument();
            XmlTextReader reader;
            using (MemoryStream memStream = new MemoryStream()) {
                XmlTextWriter rdfOutput = new XmlTextWriter(memStream, Encoding.UTF8);
                ComputingResource.WriteTaskResultRdf(this, rdfOutput);
                rdfOutput.Flush();
                memStream.Seek(0, SeekOrigin.Begin);
                reader = new XmlTextReader(memStream);
                reader.MoveToContent();
                if (xslFile == null) {
                    result.Load(reader);
                    reader.Close();
                } else {
                    XslCompiledTransform xslt = new XslCompiledTransform();
                    xslt.Load(xslFile);
                    using (MemoryStream tempMemStream = new MemoryStream()) {
                        XmlWriter tempWriter = new XmlTextWriter(tempMemStream, Encoding.UTF8); 
                        xslt.Transform(reader, null, tempWriter);
                        tempWriter.Flush();
                        reader.Close();
                        tempMemStream.Seek(0, SeekOrigin.Begin);
                        reader = new XmlTextReader(tempMemStream);
                        reader.MoveToContent();
                        result.Load(reader);
                        reader.Close();
                    }
                }
            }
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual void ProvideDownload(string url) {
            IfyWebContext webContext = context as IfyWebContext;
            webContext.Redirect(url, false, false);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the URL for the graphical job flow and status representation.</summary>
        /// <param name="format">the format (e.g. <c>"svg"</c> or <c>"png"</c>)</param>
        /// <returns>the flow URL</returns>
        public virtual string GetFlowUrl(string format) {
            string flow = String.Empty, wait = String.Empty, prep = String.Empty, run = String.Empty, fail = String.Empty, done = String.Empty;
            for (int i = 0; i < Jobs.Count; i++) {
                flow += (i == 0 ? String.Empty : ";") + Jobs[i].Name;
                for (int j = 0; j < Jobs[i].Dependencies.Count; j++) flow += "|" + Jobs[i].Dependencies[j].Name;
                if (Jobs[i].Status == ProcessingStatus.Pending) wait += (wait == String.Empty ? String.Empty : ";") + Jobs[i].Name;
                else if (Jobs[i].Status == ProcessingStatus.Preparing) prep += (prep == String.Empty ? String.Empty : ";") + Jobs[i].Name;
                else if (Jobs[i].Status == ProcessingStatus.Active) run += (run == String.Empty ? String.Empty : ";") + Jobs[i].Name;
                else if (Jobs[i].Status == ProcessingStatus.Failed) fail += (fail == String.Empty ? String.Empty : ";") + Jobs[i].Name;
                else if (Jobs[i].Status == ProcessingStatus.Completed) done += (done == String.Empty ? String.Empty : ";") + Jobs[i].Name;
            }
            IfyWebContext webContext = context as IfyWebContext;
            return String.Format("{0}?format={1}&flow={2}&wait={3}&prep={4}&run={5}&fail={6}&done={7}", webContext.TaskFlowUrl, format, flow, wait, prep, run, fail, done);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Resets the value of the Grid engine session ID of the task in the database.</summary>
        public virtual void RemoveRemoteIdentifier() {
            if (RemoteIdentifier == null) return;
            RemoteIdentifier = null;
            Store();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string GetJobList() {
            string result = String.Empty;
            for (int i = 0; i < Jobs.Count; i++) result += (i == 0 ? String.Empty : ",") + Jobs[i].Name;
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a job and adds it to the task</summary>
        /// <param name="name">the name of the job</param>
        /// <param name="jobType">the job type (must be known by the Grid engine)</param>
        /// <returns>the created job</returns>
        public virtual Job AddJob(string name, string jobType) {
            return Jobs.Add(name, jobType);
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a job that publishes results and adds it to the task</summary>
        /// <param name="name">the name of the job</param>
        /// <param name="clean">determines ???</param>
        /// <returns>the created job</returns>
        public virtual Job AddPublishJob(string name, bool clean) {
            //if (id != 0) context.ReturnError("Job cannot be added to existing task");
            return AddPublishJob(name, Job.PublishJobType, null as string, clean);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a job that publishes results and adds it to the task</summary>
        /// <param name="name">the name of the job</param>
        /// <param name="inputFile">a pipe-separated list of input file identifiers</param>
        /// <param name="clean">determines ???</param>
        /// <returns>the created job</returns>
        public virtual Job AddPublishJob(string name, string inputFile, bool clean) {
            //if (id != 0) context.ReturnError("Job cannot be added to existing task");
            return AddPublishJob(name, Job.PublishJobType, inputFile, clean);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a job that publishes results and adds it to the task</summary>
        /// <param name="name">the name of the job</param>
        /// <param name="inputFiles">a string array containing the input file identifiers</param>
        /// <param name="clean">determines ???</param>
        /// <returns>the created job</returns>
        public virtual Job AddPublishJob(string name, string[] inputFiles, bool clean) {
            //if (id != 0) context.ReturnError("Job cannot be added to existing task");
            Job publishJob = Jobs.Add(name, Job.PublishJobType);
            if (inputFiles != null) publishJob.AddInputFiles(inputFiles);
            publishJob.AddParameter("clean", clean.ToString().ToLower());
            return publishJob;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a job that publishes results and adds it to the task</summary>
        /// <param name="name">the name of the job</param>
        /// <param name="jobType">the job type (must be known by the Grid engine)</param>
        /// <param name="inputFile">a pipe-separated list of input file identifiers</param>
        /// <param name="clean">determines ???</param>
        /// <returns>the created job</returns>
        public virtual Job AddPublishJob(string name, string jobType, string inputFile, bool clean) {
            //if (id != 0) context.ReturnError("Job cannot be added to existing task");
            Job publishJob = Jobs.Add(name, jobType);
            if (inputFile != null) publishJob.AddInputFile(inputFile);
            publishJob.AddParameter("clean", clean.ToString().ToLower());
            publishJob.Publishes = true;
            return publishJob;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a job that publishes results and adds it to the task</summary>
        /// <param name="name">the name of the job</param>
        /// <param name="jobType">the job type (must be known by the Grid engine)</param>
        /// <param name="inputFiles">a string array containing the input file identifiers</param>
        /// <param name="clean">determines ???</param>
        /// <returns>the created job</returns>
        public virtual Job AddPublishJob(string name, string jobType, string[] inputFiles, bool clean) {
            //if (id != 0) context.ReturnError("Job cannot be added to existing task");
            Job publishJob = Jobs.Add(name, jobType);
            if (inputFiles != null) publishJob.AddInputFiles(inputFiles);
            publishJob.AddParameter("clean", clean.ToString().ToLower());
            publishJob.Publishes = true;
            return publishJob;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public static List<Task> GetTaskList(IfyContext context, int userId) {
            List<int> taskIds = new List<int>();
            string sql = String.Format("SELECT t.id FROM task AS t WHERE id_usr={0};", userId);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) taskIds.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            List<Task> result = new List<Task>();
            foreach (int taskId in taskIds) result.Add(Task.FromId(context, taskId, null, true));
            
            return result;
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Executes the background agent action <b>Grid&nbsp;engine&nbsp;pending&nbsp;operations</b>.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <remarks>
        ///     <para>This method is called periodically if the action is active and the background agent is running according to the action's execution interval specified in the portal configuration.</para>
        ///     <para>
        ///         The background agent action <b>Grid&nbsp;engine&nbsp;pending&nbsp;operations</b> performs the pending <b>submit</b>, <b>retry</b>, <b>abort</b> and <b>delete</b> operations on tasks and jobs.
        ///         %Task operations are pending if they were requested while the portal configuration variable <i>SynchronousTaskOperations</i> was set to <c>false</c>.
        ///     </para>
        ///     <para>The following table lists the <b>task operations</b> that are performed according to the requested and actual <b>task</b> status, which are kept in different database fields.</para>
        ///     <table border="1" cellpadding="4" style="border-collapse:collapse; border:2px solid #c0c0c0; empty-cells:show">
        ///         <tr>
        ///             <th rowspan="2">Original task operation</th>
        ///             <th colspan="3">%Task status before operation (during the pending phase)</th>
        ///             <th rowspan="2">Operations performed on task or jobs</th>
        ///             <th colspan="2">%Task status after successful operation</th>
        ///         </tr>
        ///         <tr>
        ///             <th>Requested operation<br/>(DB field <c>task.async_op</c>)</th>
        ///             <th>Actual status<br/>(DB field <c>task.status</c>)</th>
        ///             <th>Logical status<br/>presented to the user</th>
        ///             <th>Actual status<br/>(DB field <c>task.status</c>)</th>
        ///             <th>Logical status<br/>presented to the user</th>
        ///         </tr>
        ///         <tr>
        ///             <th>submit</th>
        ///             <td><i>Submit</i></td>
        ///             <td><i>Created</i></td>
        ///             <td><i><b>Pending</b></i></td>
        ///             <td>Submits the task to the Grid engine.</td>
        ///             <td><i>Active</i></td>
        ///             <td><i><b>Active</b></i></td>
        ///         </tr>
        ///         <tr>
        ///             <th>submit</th>
        ///             <td><i>Submit</i></td>
        ///             <td><i>Active</i> or <br/><i>Failed</i> or <br/><i>Incomplete</i> or <br/><i>Completed</i></td>
        ///             <td><i><b>Pending</b></i></td>
        ///             <td>Aborts the task on the Grid engine if it has a Grid session ID assigned.<br/>Submits the task to the Grid engine.</td>
        ///             <td><i>Active</i></td>
        ///             <td><i><b>Active</b></i></td>
        ///         </tr>
        ///         <tr>
        ///             <th>retry</th>
        ///             <td><i>Retry</i></td>
        ///             <td><i>Failed</i></td>
        ///             <td><i><b>Active</b></i></td>
        ///             <td>Resubmits the failed jobs of the task to the Grid engine.</td>
        ///             <td><i>Active</i></td>
        ///             <td><i><b>Active</b></i></td>
        ///         </tr>
        ///         <tr>
        ///             <th>abort</th>
        ///             <td><i>Abort</i></td>
        ///             <td><i>Active</i> or <br/><i>Failed</i> or <br/><i>Incomplete</i> or <br/><i>Completed</i></td>
        ///             <td><i><b>Created</b></i></td>
        ///             <td>Aborts the task on the Grid engine if it has a Grid session ID assigned.</td>
        ///             <td><i>Created</i></td>
        ///             <td><i><b>Created</b></i></td>
        ///         </tr>
        ///         <tr>
        ///             <th>delete</th>
        ///             <td><i>Delete</i></td>
        ///             <td><i>Active</i> or <br/><i>Failed</i> or <br/><i>Incomplete</i> or <br/><i>Completed</i></td>
        ///             <td>N/A <sup>1</sup></td>
        ///             <td>Aborts the task on the Grid engine if it has a Grid session ID assigned.<br/>Does not delete the task from the database; this is done by the <b>%Task&nbsp;cleanup</b> action, see #ExecuteTaskCleanup().</td>
        ///             <td><i>Deleted</i></td>
        ///             <td>N/A <sup>1</sup></td>
        ///         </tr>
        ///     </table>
        ///     <para>
        ///         <sup>1</sup> task is considered as deleted and completely invisible to the user.
        ///     </para>
        ///     <para>The following table lists the <b>job operations</b> that are performed according to the requested and actual <b>job</b> status, which are kept in different database fields.</para>
        ///     <table border="1" cellpadding="4" style="border-collapse:collapse; border:2px solid #c0c0c0; empty-cells:show">
        ///         <tr>
        ///             <th rowspan="2">Original job operation</th>
        ///             <th colspan="3">%Job status before operation (during the pending phase)</th>
        ///             <th rowspan="2">Operations performed on job</th>
        ///             <th colspan="2">%Job status after successful operation</th>
        ///         </tr>
        ///         <tr>
        ///             <th>Requested operation<br/>(DB field <c>job.async_op</c>)</th>
        ///             <th>Actual status<br/>(DB field <c>job.status</c>)</th>
        ///             <th>Logical status<br/>presented to the user</th>
        ///             <th>Actual status<br/>(DB field <c>job.status</c>)</th>
        ///             <th>Logical status<br/>presented to the user</th>
        ///         </tr>
        ///         <tr>
        ///             <th>submit</th>
        ///             <td><i>Active</i></td>
        ///             <td><i>Created</i></td>
        ///             <td><i><b>Preparing</b></i></td>
        ///             <td>Submits the job to the Grid engine if the owning task has a Grid session ID assigned.</td>
        ///             <td><i>Preparing</i></td>
        ///             <td><i><b>Preparing</b></i></td>
        ///         </tr>
        ///         <tr>
        ///             <th>submit</th>
        ///             <td><i>Active</i></td>
        ///             <td><i>Active</i> or <br/><i>Failed</i> or <br/><i>Completed</i></td>
        ///             <td><i><b>Preparing</b></i></td>
        ///             <td>Resubmits the job to the Grid engine if the owning task has a Grid session ID assigned.</td>
        ///             <td><i>Preparing</i></td>
        ///             <td><i><b>Preparing</b></i></td>
        ///         </tr>
        ///         <tr>
        ///             <th>abort</th>
        ///             <td><i>Created</i></td>
        ///             <td><i>Active</i> or <br/><i>Failed</i> or <br/><i>Completed</i></td>
        ///             <td><i><b>Created</b></i></td>
        ///             <td>Aborts the job on the Grid engine if the owning task has a Grid session ID assigned.</td>
        ///             <td><i>Created</i></td>
        ///             <td><i><b>Created</b></i></td>
        ///         </tr>
        ///         <tr>
        ///             <th>conclude</th>
        ///             <td><i>Completed</i></td>
        ///             <td><i>Active</i> or <br/><i>Failed</i> or <br/><i>Completed</i></td>
        ///             <td><i><b>Completed</b></i></td>
        ///             <td>Sets the job status to completed on the Grid engine if the owning task has a Grid session ID assigned.</td>
        ///             <td><i>Completed</i></td>
        ///             <td><i><b>Completed</b></i></td>
        ///         </tr>
        ///     </table>
        /// </remarks>
        public static void ExecuteTaskPendingOperations(IfyContext context) {
            IDataReader reader;
            Task task;
            List<int> taskIds = new List<int>();
            string sql;
            
            // Abort tasks (next_status = Created)
            sql = String.Format("SELECT t.id FROM task AS t WHERE t.async_op IN ({0},{1}) AND t.status>={2} AND t.remote_id IS NOT NULL ORDER BY t.id;", TaskOperationType.Abort, TaskOperationType.Delete, ProcessingStatus.Active);
            reader = context.GetQueryResult(sql);
            while (reader.Read()) taskIds.Add(reader.GetInt32(0));
            reader.Close();
            
            for (int i = 0; i < taskIds.Count; i++) {
                try {
                    task = Task.FromId(context, taskIds[i]);
                    context.WriteInfo("Aborting task " + task.Identifier + " ... \\");
                    
                    if (task.DoAbort(false)) context.WriteInfo("Task aborted");
                } catch (Exception) {}
            }
            taskIds.Clear();

            // Resubmit failed jobs (next_status = ProcessingStatus.Active)
            sql = String.Format("SELECT t.id FROM task AS t WHERE t.async_op={0} AND t.status={1} ORDER BY t.id;", TaskOperationType.Retry, ProcessingStatus.Failed);
            reader = context.GetQueryResult(sql);
            while (reader.Read()) taskIds.Add(reader.GetInt32(0));
            reader.Close();

            for (int i = 0; i < taskIds.Count; i++) {
                try {
                    task = Task.FromId(context, taskIds[i]); // job statuses required (third parameter implicitly true)
                    context.WriteInfo("Resubmitting failed job(s) of task " + task.Identifier);
                    int count = task.DoRetry();
                    if (count != 0) {
                        string s = String.Empty;
                        for (int j = 0; j < task.Jobs.Count; j++) if (task.Jobs[j].Resubmitted) s += (s == String.Empty ? String.Empty : ", ") + task.Jobs[j].Name;
                        context.WriteInfo("Jobs resubmitted: " + count + " (" + s + ")");
                    }
                } catch (Exception) {}
            }
            taskIds.Clear();

            // Submit pending tasks (next_status = ProcessingStatus.Pending)
            sql = String.Format("SELECT t.id FROM task AS t WHERE t.async_op={0} ORDER BY t.id;", TaskOperationType.Submit);
            reader = context.GetQueryResult(sql);
            while (reader.Read()) taskIds.Add(reader.GetInt32(0));
            reader.Close();
            
            for (int i = 0; i < taskIds.Count; i++) {
                try {
                    task = Task.FromId(context, taskIds[i]);
                    context.WriteInfo("Submitting task " + task.Identifier);
                    if (task.DoSubmit(true)) {
                        context.WriteInfo("Task submitted");
                        context.WriteInfo("Remote ID is " + task.RemoteIdentifier);
                    }
                } catch (Exception e) {
                    context.AddError(e.Message + " " + e.StackTrace);
                }
            }
            taskIds.Clear();

            // Rebuild and submit pending tasks (next_status = ProcessingStatus.Pending)
            sql = String.Format("SELECT t.id FROM task AS t WHERE t.async_op={0} ORDER BY t.id;", TaskOperationType.Restart);
            reader = context.GetQueryResult(sql);
            while (reader.Read()) taskIds.Add(reader.GetInt32(0));
            reader.Close();
            
            for (int i = 0; i < taskIds.Count; i++) {
                try {
                    task = Task.FromId(context, taskIds[i]);
                    context.WriteInfo("Rebuilding task " + task.Identifier);
                    task.Build();
                    if (task.Empty) {
                        context.WriteError("Task " + task.Identifier + " has no input data");
                        task.AsynchronousOperation = TaskOperationType.None;
                        context.Execute(String.Format("UPDATE task SET async_op=NULL WHERE id={0}", task.Id));
                    } else/* if (!task.Error)*/ {
                        context.WriteInfo("Submitting task " + task.Identifier);
                        if (task.DoSubmit(true)) {
                            context.WriteInfo("Task submitted");
                            context.WriteInfo("Remote ID is " + task.RemoteIdentifier);
                        }
                    }
                } catch (Exception e) {
                    context.AddError(e.Message + " " + e.StackTrace);
                }
            }
            taskIds.Clear();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Executes the background agent action <b>%Task&nbsp;status&nbsp;refresh</b>.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <remarks>
        ///     <para>This method is called periodically if the action is active and the background agent is running according to the action's execution interval specified in the portal configuration.</para>
        ///     <para>The background agent action <b>%Task&nbsp;status&nbsp;refresh</b> refreshes task, job and node status information for active tasks.</para>
        /// </remarks>
        public static void ExecuteTaskStatusRefresh(IfyContext context) {
            IDataReader reader;
            Task task;
            List<int> taskIds = new List<int>();
            string sql;
            
            // Get tasks with actual status Active (10)
            reader = null;
            sql = "SELECT t.id FROM task AS t WHERE status=" + ProcessingStatus.Active + " AND remote_id IS NOT NULL ORDER BY t.id;";
            reader = context.GetQueryResult(sql);
            while (reader.Read()) taskIds.Add(reader.GetInt32(0));
            reader.Close();
            
            context.WriteInfo(String.Format("Active tasks: {0}{1}", taskIds.Count, taskIds.Count == 0 ? String.Empty : " - status display format: created/active/failed/incomplete/completed"));
            // Get available task, job and node status information
            for (int i = 0; i < taskIds.Count; i++) {
                try {
                    task = Task.FromId(context, taskIds[i]);
                    int createdJobsBefore = 0, activeJobsBefore = 0, failedJobsBefore = 0, incompleteJobsBefore = 0, completedJobsBefore = 0;
                    int createdJobs = 0, activeJobs = 0, failedJobs = 0, incompleteJobs = 0, completedJobs = 0;
                    int createdNodes = 0, totalNodes = 0, activeNodes = 0, failedNodes = 0, incompleteNodes = 0, completedNodes = 0;
                    for (int j = 0; j < task.Jobs.Count; j++) {
                        task.Jobs[j].ShowNodes = true;
                        switch (task.Jobs[j].Status) {
                            case ProcessingStatus.Created : createdJobsBefore++; break;
                            case ProcessingStatus.Active : activeJobsBefore++; break;
                            case ProcessingStatus.Failed : failedJobsBefore++; break;
                            case ProcessingStatus.Incomplete : incompleteJobsBefore++; break;
                            case ProcessingStatus.Completed : completedJobsBefore++; break;
                        }
                    }
                    for (int j = 0; j < task.Jobs.Count; j++) {
                        Job job = task.Jobs[j];
                        switch (task.Jobs[j].Status) {
                            case ProcessingStatus.Created : createdJobs++; break;
                            case ProcessingStatus.Active : activeJobs++; break;
                            case ProcessingStatus.Failed : failedJobs++; break;
                            case ProcessingStatus.Incomplete : incompleteJobs++; break;
                            case ProcessingStatus.Completed : completedJobs++; break;
                        }
                        for (int k = 0; k < job.Nodes.Count; k++) {
                            totalNodes++;
                            switch (task.Jobs[j].Nodes[k].Status) {
                                case ProcessingStatus.Created : createdNodes++; break;
                                case ProcessingStatus.Active : activeNodes++; break;
                                case ProcessingStatus.Failed : failedNodes++; break;
                                case ProcessingStatus.Incomplete : incompleteNodes++; break;
                                case ProcessingStatus.Completed : completedNodes++; break;
                            }
                        }
                    }
                    //bool unchanged = (createdJobs == createdJobsBefore && activeJobs == activeJobsBefore && failedJobs == failedJobsBefore && incompleteJobs == incompleteJobsBefore && completedJobs == completedJobsBefore);
                    //if (unchanged) continue;
                    
                    string message = String.Format("{0} ({1}), user: {2}, jobs: {3}/{4}/{5}/{6}/{7}, nodes: {8}/{9}/{10}/{11}/{12}",
                            task.Identifier,
                            task.ServiceIdentifier,
                            task.Username,
                            createdJobs,
                            activeJobs,
                            failedJobs,
                            incompleteJobs,
                            completedJobs,
                            createdNodes,
                            activeNodes,
                            failedNodes,
                            incompleteNodes,
                            completedNodes
                    );
                    if (failedJobs + failedNodes != 0) context.WriteError(message);
                    else if (incompleteJobs + incompleteNodes != 0) context.WriteWarning(message);
                    else context.WriteInfo(message);
                    switch (task.Status) { // !!! STATUS PROPERTIES !!!
                        case ProcessingStatus.Failed : context.WriteError(String.Format("Task {0} failed", task.Identifier)); break;
                        case ProcessingStatus.Incomplete : context.WriteWarning(String.Format("Task {0} finished as incomplete", task.Identifier)); break;
                        case ProcessingStatus.Completed : context.WriteInfo(String.Format("Task {0} completed successfully", task.Identifier)); break;
                    }
                } catch (Exception) {}
            }
            taskIds.Clear();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Executes the task-related part of the background agent action <b>%Task&nbsp;and&nbsp;scheduler&nbsp;cleanup</b>.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <remarks>
        ///     <para>This method is called periodically if the action is active and the background agent is running according to the action's execution interval specified in the portal configuration.
        ///     <para>
        ///         The background agent action <b>%Task&nbsp;cleanup</b> deletes the tasks that have been marked for deletion and no longer consume Grid engine resources, together with all data related to these tasks (jobs, node information etc.), from the database.
        ///         This is the case for tasks that have the value <i>Deleted</i> in the database field <c>task.status</c>.
        ///         %Tasks deletions are pending if they were requested while the portal configuration variable <i>SynchronousTaskOperations</i> was set to <c>false</c>.
        ///     </para>
        /// </remarks>
        public static void ExecuteCleanup(IfyContext context) {
            IDataReader reader;
            Task task;
            List<int> taskIds = new List<int>();
            
            // Abort tasks (next_status = Deleted)
            reader = context.GetQueryResult(String.Format("SELECT t.id FROM task AS t WHERE t.async_op={0} AND (t.message_code IS NULL OR t.message_code!={1}) ORDER BY t.id;", TaskOperationType.Delete, TaskMessageCode.CertificateExpired));
            while (reader.Read()) taskIds.Add(reader.GetInt32(0));
            reader.Close();
            for (int i = 0; i < taskIds.Count; i++) {
                try {
                    task = Task.FromId(context, taskIds[i]);
                    context.WriteInfo("Aborting task " + task.Identifier + " ... \\");
                    
                    if (task.DoAbort(true, true)) {
                        context.WriteInfo("Task aborted");
                        context.Execute(String.Format("UPDATE task SET retry_period=NULL, status={1}, async_op=NULL, message_code=NULL WHERE id={0};", task.Id, ProcessingStatus.Deleted));
                    }
                } catch (Exception) {}
            }
            taskIds.Clear();

            int count = context.Execute(String.Format("DELETE FROM task WHERE status={0};", ProcessingStatus.Deleted));
            context.WriteInfo("Deleted tasks: " + (count <= 0 ? "0" : count.ToString()));
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    
    
    
    public class ProcessingStatus {
        
        public const int None       =   0;
        public const int Precreated =   1;
        public const int Created    =  10; // Unsubmitted                               Created
        public const int Pending    =  11; // Submitted                                 Pending
        public const int Active     =  20; // Submitted   Started                       Active
        public const int Preparing  =  21; // Submitted   Started                       Preparing
        public const int Queued     =  22; // Submitted   Started                       Queued
        public const int Paused     =  23; // Submitted   Started                       Paused
        public const int Failed     =  30; // Submitted   Started   Ended               Failed
        public const int Completed  =  40; // Submitted   Started   Ended    Finished   Completed
        public const int Incomplete =  41; // Submitted   Started   Ended    Finished   Incomplete
        public const int Deleted    = 255;
        
        public static string ToString(int i) {
            switch (i) {
                case Created :
                    return "Created";
                case Pending :
                    return "Pending";
                case Active :
                    return "Active";
                case Paused :
                    return "Paused";
                case Failed :
                    return "Failed";
                case Completed :
                    return "Completed";
                case Incomplete :
                    return "Incomplete";
                default :
                    return "Undefined";
            }
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    /// <summary>Request method types for task and job status values or similar</summary>
    public class RequestMethodType {

        // Undefined request method
        public const int Unknown = 0;

        /// <summary>Value is not requested</summary>
        public const int NoRefresh = 1;

        /// <summary>Value is requested via a web service (e.g. wsServer)</summary>
        public const int WebService = 2;

        /// <summary>Value is requested via a file (e.g. job status XML file)</summary>
        public const int StatusFile = 3;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a valid request method type value.</summary>
        /// <param name="value">the original request method type</param>
        /// <returns>the original or corrected (if invalid) request method type value</returns>
        public static int FromInt(int value) {
            return (value < 1 || value > 3 ? 1 : value);
        }
    }
    
    
    
    public class TaskMessageCode {
        public const int UserCreditsExceeded = 1;
        public const int ComputingResourceNotAvailable = 2;
        public const int ComputingResourceBusy = 3;
        public const int ComputingResourceSchedulerLimit = 4;
        public const int NoConnection = 5;
        public const int CertificateExpired = 6;
        public const int Other = 128;
    }
    

}

