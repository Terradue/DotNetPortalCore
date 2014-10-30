using System;
using System.Data;
using System.Text.RegularExpressions;
using Terradue.Util;


namespace Terradue.Portal {


    [EntityTable("schedulertaskconf", EntityTableConfiguration.Custom, HasAutomaticIds = false)]
    [EntityReferenceTable("service", SERVICE_TABLE)]
    public class SchedulerTaskConfiguration : Entity {
        private const int SERVICE_TABLE = 2;

        private Scheduler scheduler;

        private Service service;
        private ComputingResource computingResource;
        private PublishServer publishServer;
        private int totalTasks, emptyTasks, unsubmittedTasks, submittedTasks, failedTasks, finishedTasks;
        private bool gotTaskSummary;

        //---------------------------------------------------------------------------------------------------------------------

        public Scheduler Scheduler {
            get {
                if (scheduler == null && Id != 0) {
                    scheduler = Scheduler.FromId(context, Id);
                    scheduler.TaskConfiguration = this;
                }
                return scheduler;
            }
            set {
                scheduler = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the unique name of service that defines the derivate.</summary>
        [EntityForeignField("identifier", SERVICE_TABLE)]
        public string ServiceIdentifier { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the database ID of the service that defines the scheduler.</summary>
        [EntityDataField("id_service", IsForeignKey = true)]
        public int ServiceId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the service that defines the scheduler.</summary>
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

        /// <summary>Gets the database ID of the computing resource assigned to the scheduler.</summary>
        [EntityDataField("id_cr", IsForeignKey = true)]
        public int ComputingResourceId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the computing resource assigned to the scheduler.</summary>
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

        /// <summary>Gets or sets the maximum computing resource capacity that can be used at any time by the scheduler (in percent).</summary>
        [EntityDataField("max_cr_usage")]
        public int MaxComputingResourceUsage { get; set; }

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
        [EntityDataField("priority")]
        public double Priority { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the result compression value of the service derivate.</summary>
        [EntityDataField("compression")]
        public string Compression { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates that the scheduler has started and created at least one task.</summary>
        public bool Started { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of tasks created by the scheduler that have not yet been submitted by the user.</summary>
        /*
            A task is considered as <b>not yet submitted by the user</b> if it is in the <i>Created</i> state or if it has only been pre-created yet.
        */
        public int TotalTasks {
            get {
                if (!gotTaskSummary) GetTaskSummary();
                return totalTasks;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of tasks created by the scheduler that have no input data selected.</summary>
        /// <remarks>An empty task cannot be submitted as it is. It must be recreated performing another query for matching input data first.</remarks>
        public int EmptyTasks { 
            get {
                if (!gotTaskSummary) GetTaskSummary();
                return emptyTasks;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of tasks created by the scheduler that have not yet been submitted by the user.</summary>
        /*
            A task is considered as <b>not yet submitted by the user</b> if it is in the <i>Created</i> state or if it has only been pre-created yet.
        */
        public int UnsubmittedTasks { 
            get {
                if (!gotTaskSummary) GetTaskSummary();
                return unsubmittedTasks;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of tasks created by the scheduler that have been submitted by the user and not yet finished. </summary>
        /*
            A task is considered as <b>submitted by the user</b> if it is in the <i>Pending</i> or <i>Active</i> state.
        */
        public int SubmittedTasks {
            get {
                if (!gotTaskSummary) GetTaskSummary();
                return submittedTasks;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of tasks created by the scheduler that have not finished successfully. </summary>
        /*
            A task is considered as <b>not finished successfully</b> if it is in the <i>Failed</i> state.
        */
        public int FailedTasks {
            get {
                if (!gotTaskSummary) GetTaskSummary();
                return failedTasks;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of tasks created by the scheduler that have finished successfully. </summary>
        /*
            A task is considered as <b>finished successfully</b> if it is in the <i>Incomplete</i> or <i>Complete</i> state.
        */
        public int FinishedTasks {
            get {
                if (!gotTaskSummary) GetTaskSummary();
                return finishedTasks;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /*public virtual bool CanGetTask { 
            get { return false; } 
        }*/

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of nodes used by the scheduler on the configured computing resource.</summary>
        public int UsedCapacity {
            get {
                return context.GetQueryIntegerValue(
                    String.Format("SELECT COUNT(DISTINCT id_job, pid) FROM task AS t INNER JOIN job AS t1 ON t.id=t1.id_task INNER JOIN jobnode AS t2 ON t1.id=t2.id_job WHERE t.id_scheduler={0} AND t.id_cr={1} AND (t1.status>{2} AND t1.status<{3} OR t1.async_op={4} OR t1.async_op={5});",
                              Id,
                              ComputingResourceId,
                              ProcessingStatus.Pending,
                              ProcessingStatus.Failed,
                              TaskOperationType.Submit,
                              TaskOperationType.Retry
                              )
                    );
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string TasksRelativeUrl {
            get {
                string url;
                IfyWebContext webContext = context as IfyWebContext;
                if (context.AdminMode && webContext != null && webContext.AdminRootUrl != null) url = "{2}/{3}/{0}/tasks";  
                else if (context.AdminMode) url = "admin/task.aspx?scheduler={1}";
                else if (webContext != null && webContext.SchedulerWorkspaceRootUrl != null) url = "{4}/{0}/tasks"; 
                else url = "tasks/?scheduler={1}";
                return String.Format(url, Exists ? Identifier : String.Empty, Exists ? Id.ToString() : String.Empty, webContext.AdminRootUrl, EntityType.GetEntityType(this.GetType()).Keyword, webContext.SchedulerWorkspaceRootUrl);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string TasksRelativeUrlSqlExpression {
            get {
                IfyWebContext webContext = context as IfyWebContext;
                if (context.AdminMode && webContext != null && webContext.AdminRootUrl != null) return String.Format("CONCAT('{0}/{1}/', t.identifier, '/tasks')", webContext.AdminRootUrl, EntityType.GetEntityType(this.GetType()).Keyword);  
                else if (context.AdminMode) return "CONCAT('admin/task.aspx/scheduler=', t.id)";
                else if (webContext != null && webContext.SchedulerWorkspaceRootUrl != null) return String.Format("CONCAT('{0}/', t.identifier, '/tasks')", webContext.SchedulerWorkspaceRootUrl); 
                else return "CONCAT('tasks/?scheduler=', t.id)";
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string ControlRelativeUrl {
            get {
                string url;
                IfyWebContext webContext = context as IfyWebContext;
                /*if (context.AdminMode && webContext.AdminRootUrl != null) url = "{2}/{3}/{0}/control";
                else if (context.AdminMode) url = "admin/task.aspx/scheduler={1}";
                else */if (webContext != null && webContext.UsesUrlRewriting) url = "{2}/schedulers/{0}/control"; 
                else url = "{2}?_form=scheduler&id={1}";
                return String.Format(url, Exists ? Identifier : String.Empty, Exists ? Id.ToString() : String.Empty, Service.RelativeUrl);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string ControlRelativeUrlSqlExpression {
            get {
                string serviceRootSql = StringUtils.EscapeSql(Regex.Replace(context.ServiceWebRoot, "/$", String.Empty));
                /*if (webContext.SchedulerWorkspaceRootUrl != null) return String.Format("CONCAT(REPLACE(c.root, '$(SERVICEROOT)', " + serviceRootSql + "), '/schedulers/', t.identifier)", webContext.SchedulerWorkspaceRootUrl); 
                else */return "CONCAT(REPLACE(c.root, '$(SERVICEROOT)', " + serviceRootSql + "), '/?_form=scheduler&id=', t.id)";
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new TaskScheduler instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public SchedulerTaskConfiguration(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new TaskScheduler instance referring to the specified service.</summary>
        /// <remarks>Use this constructor to define a new task-processing scheduler based on the configuration of the specified service.</remarks>
        /// <param name="context">The execution environment context.</param>
        /// <param name="service">The service that is used for the scheduler definition.</param>
        public SchedulerTaskConfiguration(IfyContext context, Service service) : base(context) {
            this.Service = service;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new TaskScheduler instance representing the scheduler with the parameters of the specified task.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="taskIdentifier">The task on which the scheduler is based.</param>
        public SchedulerTaskConfiguration(IfyContext context, Task task) : base(context) {
            this.Service = task.Service;
            this.ComputingResourceId = task.ComputingResourceId;
            this.PublishServerId = task.PublishServerId;
            this.Name = String.Format("Scheduler from {0}", task.Name);
            this.Priority = task.Priority;
            this.Compression = task.Compression;
            //LoadTaskParameters(task);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new TaskScheduler instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created TaskScheduler object.</returns>
        public static new SchedulerTaskConfiguration GetInstance(IfyContext context) {
            return new SchedulerTaskConfiguration(context);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new TaskScheduler instance representing the scheduler with the specified ID and based on the specified service.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the scheduler ID</param>
        /// <param name="service">the service that created the scheduler</param>
        /// <returns>the created TaskScheduler object.</returns>
        public static SchedulerTaskConfiguration FromId(IfyContext context, int id) {
            EntityType entityType = EntityType.GetOrAddEntityType(typeof(SchedulerTaskConfiguration));
            SchedulerTaskConfiguration result = (SchedulerTaskConfiguration)entityType.GetEntityInstanceFromId(context, id);
            result.Id = id;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new TaskScheduler instance representing the scheduler with the specified identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="indentifier">the scheduler identifier</param>
        /// <param name="service">the processing service on which the scheduler is based</param>
        /// <returns>the created TaskScheduler object.</returns>
        public static SchedulerTaskConfiguration FromIdentifier(IfyContext context, string identifier, Service service) {
            EntityType entityType = EntityType.GetOrAddEntityType(typeof(SchedulerTaskConfiguration));
            SchedulerTaskConfiguration result = (SchedulerTaskConfiguration)entityType.GetEntityInstanceFromIdentifier(context, identifier);
            result.Identifier = identifier;
            result.Load();
            if (service != null && result.ServiceId != service.Id) throw new InvalidOperationException("The requested scheduler does not derive from this service");
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new TaskScheduler instance representing the scheduler with the specified ID and based on the specified service.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the scheduler ID</param>
        /// <param name="service">the service that created the scheduler</param>
        /// <returns>the created TaskScheduler object.</returns>
        public static SchedulerTaskConfiguration FromTask(IfyContext context, Task task, int typeId) {
            SchedulerTaskConfiguration result = GetInstance(context);

            result.Service = task.Service;
            result.ComputingResourceId = task.ComputingResourceId;
            result.PublishServerId = task.PublishServerId;
            result.Name = String.Format("Scheduler from {0}", task.Name);
            result.Priority = task.Priority;
            result.Compression = task.Compression;
            //result.LoadTaskParameters(task);

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static SchedulerTaskConfiguration ForService(IfyContext context, Service service) {
            SchedulerTaskConfiguration result = new SchedulerTaskConfiguration(context, service);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Inserts the scheduler and its parameters into the database.</summary>
        public override void Store() {
            if (Id == 0) Id = Scheduler.Id;
            if (Service == null || ComputingResource == null) throw new Exception("Service or computing resource has not been specified");
            base.Store();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void SetNextTaskParameters(RequestParameterCollection requestParameters) {

            /*RequestParameter seriesParam, param;

            for (int i = 0; i < requestParameters.Count; i++) {
                seriesParam = requestParameters[i];
                if (seriesParam.Type != "series") continue;

                param = new RequestParameter(context, Service, "_sort", null, null, String.Format("dct.modified,,{0} dc.identifier,,{0}", BackwardDirection ? "descending" : ""));
                param.SearchExtension = "sru:sortKeys";
                param.Optional = true;
                requestParameters.Add(param);

                string limitValue = (Started ? ReferenceTime : ValidityStart).ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\.fff\Z");
                param = new RequestParameter(context, Service, "_modified", null, null, "::" + limitValue);
                param.SearchExtension = "dct:modified";
                param.Optional = true;
                requestParameters.Add(param);

                if (Started && ReferenceFile != null) {
                    param = new RequestParameter(context, Service, "_lastFile", null, null, ":" + ReferenceFile);
                    param.SearchExtension = "geo:uid";
                    requestParameters.Add(param);
                }

                Series fileSeries = null;

                for (int j = 0; j < requestParameters.Count; j++) {
                    if (requestParameters[j].Source != "dataset" || requestParameters[j].OwnerName != seriesParam.Name) continue;

                    // The series parameter corresponding to a dataset parameter becomes mandatory if the dataset parameter is mandatory and has no files
                    fileSeries = Series.FromIdentifier(context, seriesParam.Value);
                    if (fileSeries == null) continue;

                    fileSeries.DataSetParameter = requestParameters[j];
                    //fileSeries.GetCatalogueResult(requestParameters);
                }
            }*/
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual Task GetNextTask() {
            Task task = null;//Task.ForScheduler(context, this);
            /*Service.GetParameters(); // !!! see TODO in RequestParamete.GetXmlInformation()
            
            IDataReader reader = context.GetQueryResult("SELECT name, type, value FROM schedulerparam WHERE id_scheduler=" + Id + ";");
            while (reader.Read()) {
                task.RequestParameters.GetParameter(context, Service, reader.GetString(0), reader.GetString(1), null, reader.GetString(2));
                //param.Level = RequestParameterLevel.Custom;
            }
            reader.Close();*/ // TODO-NEW-SERVICE

            return task;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the summary on the tasks created by the scheduler.</summary>
        protected void GetTaskSummary() {
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(
                String.Format("SELECT SUM(1), SUM(CASE WHEN t.empty THEN 1 ELSE 0 END), SUM(CASE WHEN t.status<={1} AND NOT t.empty THEN 1 ELSE 0 END), SUM(CASE WHEN t.status>{1} AND t.status<{2} THEN 1 ELSE 0 END), SUM(CASE WHEN t.status>={2} AND t.status<{3} THEN 1 ELSE 0 END), SUM(CASE WHEN t.status>={3} AND t.status<{4} THEN 1 ELSE 0 END) FROM task AS t WHERE id_scheduler={0};",
                          Id,
                          ProcessingStatus.Created,
                          ProcessingStatus.Failed,
                          ProcessingStatus.Completed,
                          ProcessingStatus.Deleted
                          ),
                dbConnection
                );
            if (reader.Read()) {
                totalTasks = reader.GetInt32(0);
                emptyTasks = reader.GetInt32(1);
                unsubmittedTasks = reader.GetInt32(2);
                submittedTasks = reader.GetInt32(3);
                failedTasks = reader.GetInt32(4);
                finishedTasks = reader.GetInt32(5);
            }

            context.CloseQueryResult(reader, dbConnection);
            gotTaskSummary = true;
        }

    }

}

