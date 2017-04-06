using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using Terradue.Util;



/*!
\defgroup ComputingResource Computing Resource
@{
The component represents an abstract Computing Resource available for processing \ref Terradue.Portal#DataSet and therefore enables the "remote" or "hosted" processing. Practically, a class that implements \ref Terradue.Portal#ComputingResource is assigned to each \ref Terradue.Portal#Task (\ref Task) and is in charge of achieving its job workflow until the completion and the output of the results. 
Diagram \ref task-cr depicts the role of the computing resource in a task processing.

\xrefitem mvc_c "Controller" "Controller components"

\ingroup "Core"

\xrefitem dep "Dependencies" "Dependencies" \ref processes Task

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence stores persistently the computing resource in the database

\xrefitem dep "Dependencies" "Dependencies" \ref Authorisation controls access on the resource

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



    /// <summary>Represents a computing resource that can be used for the processing of tasks.</summary>
    /// <remarks>
    ///     <para>This is an abstract class that defines properties and methods to provide a common interface for all types of computing resources.</para>
    ///     <para>The most important part of the defined members regard task- and job-related operations. Members of derived classes are usually not called directly by custom code, but by the core classes that the operation is performed on (e.g. <see cref="Terradue.Portal.Task"/> Task and <see cref="Terradue.Portal.Job"/>).</para>
    /// </remarks>
    /// \ingroup ComputingResource
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("cr", EntityTableConfiguration.Custom, IdentifierField = "identifier", NameField = "name", HasExtensions = true, HasDomainReference = true, HasPermissionManagement = true)]
    [EntityExtensionTable("crstate", STATE_TABLE, IdField = "id_cr")]
    public abstract class ComputingResource : Entity {

        private const int STATE_TABLE = 1;

        private int totalCapacity;
        private int freeCapacity;
        private bool forceRefresh = false;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the availability value.</summary>
        /// <remarks>
        /// 0 not available, ..., 3 for all users
        /// </remarks>
        [EntityDataField("availability")]
        public int Availability { get; set; } 
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether this computing resource is available to common users.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public bool IsAvailable { 
            get { return Availability == IfyContext.MaxUserLevel; } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the description of this computing resource.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("description")]
        public string Description { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the fully qualified DNS hostname for this computing resource.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("hostname")]
        public string Hostname { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets a value indicating whether real-time capacity status information has to be requested every time for this compthis computing resource.</summary>
        /// <remarks>If the value is <c>false</c>, capacity status information remains valid for as long as defined by <see cref="IfyContext.ComputingResourceStatusValidity"/> before an update is requested.</remarks>
        public bool ForceRefresh {
            get {
                return forceRefresh; 
            }
            set {
                forceRefresh = value;
                if (forceRefresh) Refreshed = false;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the URL of the icon used to represent this computing resource.</summary>
        [EntityDataField("icon_url")]
        public string IconUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the maximum processing capacity of this computing resource.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("capacity")]
        public int MaxCapacity { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the current total processing capacity of this computing resource.</summary>
        /// <remarks>The current capacity is usually the same as the total capacity</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityForeignField("total_nodes", STATE_TABLE)]
        public int TotalCapacity {
            get {
                if (!Refreshed) GetStatus();
                return totalCapacity;
            }
            set {
                totalCapacity = value;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the free processing capacity of this computing resource.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityForeignField("free_nodes", STATE_TABLE)]
        public int FreeCapacity { 
            get {
                if (!Refreshed) GetStatus();
                return freeCapacity;
            }
            set {
                freeCapacity = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the time when the capacity status information was last updated.</summary>
        [EntityForeignField("modified", STATE_TABLE)]
        public DateTime LastStatusUpdateTime { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the load of this computing resource in percent.</summary>
        public int LoadPercentage {
            get {
                if (!Refreshed) GetStatus();
                if (!Refreshed || totalCapacity == 0) return -1;
                return 100 - (100 * freeCapacity) / totalCapacity;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether this computing resource has a credit control where credits are configured for each user.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("credit_control")]
        public bool UserCreditControl { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the capacity status information has been refreshed.</summary>
        public bool Refreshed { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether this computing resource provides interfaces to monitor its capacity status.</summary>
        public abstract bool CanMonitorStatus { get; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the status of a task and all its jobs are received with one request at task level.</summary>
        /// <remarks>If not overriden in a derived class, the default value is <c>false</c>, which means that the status information of the task's jobs is not received with the task status information and that each job's status information must be requested individually.</remarks>
        public virtual bool ProvidesFullTaskStatus {
            get { return false; }
        }
       
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Constructor that allows derived classes of ComputingResource to inherit from its superclasses.</summary>
        /// <param name="context">The execution environment context.</param>
        public ComputingResource(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a new GenericComputingResource instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created GenericComputingResource object</returns>
        /// <remarks>Since ComputingResource is abstract, an instance of GenericComputingResource is returned. This subclass provides only the functionality inherited from the superclasses of ComputingResource (e.g. Entity) but no functionality of a real computing resource.</remarks>
        public static ComputingResource GetInstance(IfyContext context) {
            return new GenericComputingResource(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns an instance of a ComputingResource subclass representing the computing resource with the specified database ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The database ID of the computing resource.</param>
        /// <returns>The created ComputingResource subclass instance.</returns>
        public static ComputingResource FromId(IfyContext context, int id) {
            EntityType entityType = EntityType.GetEntityType(typeof(ComputingResource));
            ComputingResource result = (ComputingResource)entityType.GetEntityInstanceFromId(context, id); 
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns an instance of a ComputingResource subclass representing the computing resource with the specified identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="identifier">The unique identifier of the computing resource.</param>
        /// <returns>The created ComputingResource subclass instance.</returns>
        public static ComputingResource FromIdentifier(IfyContext context, string identifier) {
            EntityType entityType = EntityType.GetEntityType(typeof(ComputingResource));
            ComputingResource result = (ComputingResource)entityType.GetEntityInstanceFromIdentifier(context, identifier); 
            result.Identifier = identifier;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns an instance of a ComputingResource subclass representing the computing resource with the specified ID, address or caption.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="s">A search value that must match the database ID (preferred) of the computing resource, or its unique identifier.</param>
        /// <returns>The created ComputingResource subclass instance.</returns>
        public static ComputingResource FromString(IfyContext context, string s) {
            int id = 0;
            Int32.TryParse(s, out id);
            EntityType entityType = EntityType.GetEntityType(typeof(ComputingResource));
            ComputingResource result = (ComputingResource)entityType.GetEntityInstanceFromCondition(context, String.Format("t.name={0} OR t.id={1}", StringUtils.EscapeSql(s), id), String.Format("t.id!={0}", id)); 
            result.Id = id;
            result.Name = s;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Store this computing resource in the database.</summary>
        public override void Store() {
            if (Name == null) Name = Identifier;
            base.Store();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Stores the new computing resource load state in the database if it has changed.</summary>
        protected virtual void StoreState() {
            if (!Refreshed) return;
            
            context.Execute(
                    String.Format("INSERT INTO crstate (id_cr) SELECT id FROM cr AS t LEFT JOIN crstate AS t1 ON t.id=t1.id_cr WHERE t.id={0} AND t1.id_cr IS NULL;",
                            Id
                    )
            );
            context.Execute(
                String.Format("UPDATE crstate SET total_nodes={1}, free_nodes={2}, modified='{3}' WHERE id_cr={0};",
                            Id,
                            TotalCapacity,
                            FreeCapacity,
                            context.Now.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss")
                    )
            );
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the number of computing resource nodes used by the specified scheduler.</summary>
        /// <param name="scheduler">The scheduler for which the load is to be checked.</returns>
        /// <returns>The number of nodes occupied by the scheduler's active tasks.</returns>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public int GetUsedCapacity(Scheduler scheduler) {
            return context.GetQueryIntegerValue(
                    String.Format("SELECT COUNT(DISTINCT id_job, pid) FROM task AS t INNER JOIN job AS t1 ON t.id=t1.id_task INNER JOIN jobnode AS t2 ON t1.id=t2.id_job WHERE t.id_cr={0} AND t.id_scheduler={1} AND (t1.status>{2} AND t1.status<{3} OR t1.async_op={4} OR t1.async_op={5});",
                            Id,
                            scheduler.Id,
                            ProcessingStatus.Pending,
                            ProcessingStatus.Failed,
                            TaskOperationType.Submit,
                            TaskOperationType.Retry
                        )
            );
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, obtains the status information for this computing resource.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void GetStatus() {
            if (ForceRefresh) return; // this method only gets the latest status information from the database, with ForceRefresh = true. the derived class obtains real-time status information

            DateTime LastModifiedTime = DateTime.MinValue;
            
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT total_nodes, free_nodes, modified FROM crstate WHERE id_cr={0} AND total_nodes IS NOT NULL AND free_nodes IS NOT NULL;", Id), dbConnection);
            bool wasModified = false;
            if (reader.Read()) {
                TotalCapacity = context.GetIntegerValue(reader, 0);
                FreeCapacity = context.GetIntegerValue(reader, 1);
                LastModifiedTime = context.GetDateTimeValue(reader, 2);
                wasModified = true;
            }
            context.CloseQueryResult(reader, dbConnection);
            Refreshed = (wasModified && (context.Now - LastModifiedTime) <= context.ComputingResourceStatusValidity);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, creates a representation of the specified task on this computing resource.</summary>
        /// <param name="task">The task to be created.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public abstract bool CreateTask(Task task);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, checks whether it is possible to start the execution of the specified task on this computing resource.</summary>
        /// <param name="task">The task to be started.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        public virtual bool CanStartTask(Task task) {
            if (!UserCreditControl) return true;
            
            int totalUserCredits = context.GetQueryIntegerValue(String.Format("SELECT credits FROM cr_perm WHERE id_usr={0};", task.UserId));
            double consumedUserCredits = context.GetQueryDoubleValue(String.Format("SELECT SUM(resources * priority) FROM task AS t WHERE t.id_cr={0} AND t.id_usr={1} AND t.id_cr={1} AND t.status=20;", Id, task.UserId));
            
            if (task.Cost > totalUserCredits - consumedUserCredits) {
                context.AddError("The task cost exceeds the available credits on this computing resource");
                return false;
            }
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, requests the the execution of the specified task on this computing resource.</summary>
        /// <param name="task">The task to be started.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public abstract bool StartTask(Task task);
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, stops the execution of the specified task on this computing resource.</summary>
        /// <param name="task">The task to be stopped.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public abstract bool StopTask(Task task);
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, obtains status information of the specified job from this computing resource.</summary>
        /// <param name="task">The task to be queried.</param>
        /// <returns><c>true</c> if the status information was received correctly, <c>false</c> otherwise.</returns>
        /// <remarks>The status information must contain not only the status value, but also information such as results as soon as they are available.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public abstract bool GetTaskStatus(Task task);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, destroys the representation of the specified task on this computing resource.</summary>
        /// <param name="task">The task to be destroyed.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public abstract bool DestroyTask(Task task);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, creates a representation of the specified job on this computing resource.</summary>
        /// <param name="job">The job to be created.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        public abstract bool CreateJob(Job job);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, updates the information and parameters of the specified job on this computing resource.</summary>
        /// <param name="job">The job to be updated.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        public abstract bool UpdateJob(Job job);
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, starts or restarts the execution of the specified job on this computing resource.</summary>
        /// <param name="job">The job to be started.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        public abstract bool StartJob(Job job);
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, suspends the specified job on this computing resource.</summary>
        /// <param name="job">The job to be suspended.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        public abstract bool SuspendJob(Job job);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, resumes the specified job on this computing resource.</summary>
        /// <param name="job">The job to be resumed.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        public abstract bool ResumeJob(Job job);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, stops the specified job on this computing resource.</summary>
        /// <param name="job">The job to be stopped.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        public abstract bool StopJob(Job job);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, requests this computing resource to accept the specified job as completed.</summary>
        /// <param name="job">The job to be completed.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        public abstract bool CompleteJob(Job job);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, obtains status information of the specified job from this computing resource.</summary>
        /// <param name="job">The job to be queried.</param>
        /// <returns><c>true</c> if the status information was received correctly, <c>false</c> otherwise.</returns>
        public abstract bool GetJobStatus(Job job);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, obtains information on the results of the specified task from this computing resource.</summary>
        /// <param name="task">The task to be queried.</param>
        /// <returns><c>true</c> if the result information was received correctly, <c>false</c> otherwise.</returns>
        public abstract bool GetTaskResult(Task task);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, writes the RDF document containing information on the results of the specified task to the output stream.</summary>
        /// <param name="task">The task in question.</param>
        /// <param name="output">The XmlWriter to be written to.</param>
        public abstract void WriteTaskResultRdf(Task task, XmlWriter output);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether this computing resource is compatible with the specified service.</summary>
        /// <param name="serviceId">The database ID of the service.</param>
        /// <returns><c>true</c> if this computing resource is compatible with the service.</returns>
        public bool CanBeUsedWithService(int serviceId) {
            return (context.GetQueryIntegerValue("SELECT COUNT(*) FROM service_cr WHERE id_service=" + serviceId + " AND id_cr=" + Id + ";") != 0);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Executes the background agent action <b>Computing&nbsp;Element&nbsp;status&nbsp;refresh</b>.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <remarks>
        ///     <p>This method is called periodically if the action is active and the background agent is running according to the action's execution interval specified in the portal configuration.</p>
        ///     <p>The background agent action <b>Computing&nbsp;Element&nbsp;status&nbsp;refresh</b> refreshes task, job and node status information for active tasks.</p>
        /// </remarks>
        public static void ExecuteComputingResourceStatusRefresh(IfyContext context) {
            IDataReader reader;
            ComputingResource computingResource;
            List<int> computingResourceIds = new List<int>();
            int unknownCount = 0;
            
            // Add computing resources into status table if not yet there
            context.Execute("INSERT INTO crstate (id_cr) SELECT id FROM cr AS t LEFT JOIN crstate AS t1 ON t.id=t1.id_cr WHERE t1.id_cr IS NULL;");
            
            // Get computing resources with actual status Active (10)
            reader = null;
            string sql = "SELECT t.id FROM ce AS t WHERE status_method IS NOT NULL ORDER BY t.id;"; // WHERE status=" + ProcessingStatus.Active + " AND remote_id IS NOT NULL ORDER BY t.id;";
            IDbConnection dbConnection = context.GetDbConnection();
            reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) computingResourceIds.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            //context.WriteInfo(String.Format("Active tasks: {0}{1}", computingResourceIds.Count, computingResourceIds.Count == 0 ? String.Empty : " - status display format: created/active/failed/incomplete/completed"));
            // Get available computing resource information
            for (int i = 0; i < computingResourceIds.Count; i++) {
                computingResource = ComputingResource.FromId(context, computingResourceIds[i]);
                //computingResource.Restricted = false;
                computingResource.GetStatus();
                
                if (computingResource.Refreshed) {
                    context.WriteInfo(
                            String.Format("{0}: total: {1}, free: {2}",
                                    computingResource.Name,
                                    computingResource.TotalCapacity,
                                    computingResource.FreeCapacity
                            )
                    );
                } else {
                    unknownCount++;
                }
            }
            if (unknownCount != 0) context.WriteWarning(String.Format("Computing resources with unavailable status: {0} (out of {1})", unknownCount, computingResourceIds.Count));
            computingResourceIds.Clear();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes to the specified XmlWriter opening the RDF container element for RDF document containing the task results.</summary>
        /// <param name="output">The XmlWriter instance to write to.</param>
        /// <remarks>This method is called by derived classes that prepare result RDF documents.</remarks>
        protected void StartRdfContainer(XmlWriter output) {
            output.WriteStartElement("rdf:RDF");
            WriteNamespaceDefinition(output, "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            WriteNamespaceDefinition(output, "eop", "http://www.genesi-dr.eu/spec/opensearch/extensions/eop/1.0/");
            WriteNamespaceDefinition(output, "time", "http://a9.com/-/opensearch/extensions/time/1.0/");
            WriteNamespaceDefinition(output, "geo", "http://a9.com/-/opensearch/extensions/geo/1.0/");
            WriteNamespaceDefinition(output, "sar", "http://earth.esa.int/sar");
            WriteNamespaceDefinition(output, "dc", "http://purl.org/dc/elements/1.1/");
            WriteNamespaceDefinition(output, "dct", "http://purl.org/dc/terms/");
            WriteNamespaceDefinition(output, "dclite4g", "http://xmlns.com/2008/dclite4g#");
            WriteNamespaceDefinition(output, "ical", "http://www.w3.org/2002/12/cal/ical#");
            WriteNamespaceDefinition(output, "atom", "http://www.w3.org/2005/Atom");
            WriteNamespaceDefinition(output, "envisat", "http://www.example.com/schemas/envisat.rdf#");
            WriteNamespaceDefinition(output, "owl", "http://www.w3.org/2002/07/owl#");
            WriteNamespaceDefinition(output, "fp", "http://downlode.org/Code/RDF/file-properties/");
            WriteNamespaceDefinition(output, "ws", "http://dclite4g.xmlns.com/ws.rdf#");
            WriteNamespaceDefinition(output, "os", "http://a9.com/-/spec/opensearch/1.1/");
            WriteNamespaceDefinition(output, "jers", "http://www.eorc.jaxa.jp/JERS-1/en/");
            WriteNamespaceDefinition(output, "sru", "http://a9.com/-/opensearch/extensions/sru/2.0/");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes to the specified XmlWriter closing the RDF container element for RDF document containing the task results.</summary>
        /// <param name="output">The XmlWriter instance to write to.</param>
        /// <remarks>This method is called by derived classes that prepare result RDF documents.</remarks>
        protected void EndRdfContainer(XmlWriter output) {
            output.WriteEndElement();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes a namespace definition to the specified XmlWriter.</summary>
        /// <param name="output">The XmlWriter instance to write to.</param>
        /// <param name="prefix">The namespace prefix.</param>
        /// <param name="ns">The namespace URI.</param>
        protected void WriteNamespaceDefinition(XmlWriter output, string prefix, string ns) {
            output.WriteAttributeString("xmlns", prefix, null, ns);
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Empty implementation of ComputingResource.</summary>
    /// <remarks>
    ///     <p>This class is only used when a generic instance derived from the abstract ComputingResource class is needed in order to combine different types of computing resources (e.g. for producing an item list).</p>
    ///     <p>It provides only the functionality inherited from the superclasses of ComputingResource (e.g. Entity) but no functionality of a real computing resource.</p>
    /// </remarks>
    /// \ingroup ComputingResource
    public class GenericComputingResource : ComputingResource {
        
        /// <summary>Indicates that this computing resource provides no interface to monitor its capacity status.</summary>
        /// <value>Always <c>false</c> for GenericComputingResource instances.</value>
        public override bool CanMonitorStatus {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new GenericComputingResource instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public GenericComputingResource(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool CreateTask(Task task) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool CanStartTask(Task task) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool StartTask(Task task) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool StopTask(Task task) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool GetTaskStatus(Task task) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool DestroyTask(Task task) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool CreateJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool UpdateJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool StartJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool SuspendJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool ResumeJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool StopJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool CompleteJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool GetJobStatus(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool GetTaskResult(Task task) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        public override void WriteTaskResultRdf(Task task, XmlWriter output) {}
        
    }

}

