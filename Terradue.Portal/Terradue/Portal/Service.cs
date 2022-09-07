using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Util;
using System.Linq;

/*!
\defgroup Service Service
@{

This component is a wrapper for all users’ application level functionalities. It implements an open framework to plug services that then creates \ref Task or \ref Scheduler. 
This framework controls common service functionalities:
- \ref Persistence on the service
- Service definition parsing and template build
- \ref Task creation
Practically, every service implements \ref IService "IService" that defines all the parameters the service must or may include in the task or scheduler and several functions that
- prepares the data access request view template for the web client;
- reviews the parameters’ correctness;
- builds the task with parameterized jobs composition.
Once the task created by the service, the assigned \ref ComputingResource handles it for submission.

\xrefitem mvc_c "Controller" "Controller components"

\ingroup "Core"

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence stores the service reference in the database

\xrefitem dep "Dependencies" "Dependencies" \ref Authorisation controls access on the services

\xrefitem dep "Dependencies" "Dependencies" creates new \ref Task

\xrefitem dep "Dependencies" "Dependencies" is available on \ref ComputingResource

@}
*/



//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using System.Collections.Specialized;
using Terradue.OpenSearch.Schema;
using System.Collections;
using Terradue.OpenSearch.Request;
using System.Web;
using Terradue.OpenSearch.Engine;
using Terradue.ServiceModel.Syndication;
using Terradue.Portal.OpenSearch;

namespace Terradue.Portal {



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public delegate XmlDocument GetDefinitionCallbackType(RequestParameterCollection request);
    public delegate void CheckParameterCallbackType(RequestParameter param);
    public delegate void ReviewParametersCallbackType(RequestParameterCollection request);
    public delegate void BuildTaskCallbackType(Task task);



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Processing Service</summary>
    /// <description>Abstract base object for processing services.</description>
    /// \ingroup Service
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("service", EntityTableConfiguration.Full, HasExtensions = true, HasDomainReference = true, HasPermissionManagement = true, AllowsKeywordSearch = true, HasOwnerReference = true)]
    [EntityReferenceTable("serviceclass", CLASS_TABLE, ReferenceField = "id_class")]
    public abstract class Service : EntitySearchable, IAtomizable {
        private const int CLASS_TABLE = 1;

        //private double minPriority = 0, maxPriority = 0;
        private bool serviceChecked, validService;

        //---------------------------------------------------------------------------------------------------------------------

        [EntityPermissionField("allow_scheduling")]
        public bool CanSchedule { get; private set; }
        // TODO if (context.GetQueryBooleanValue(String.Format("SELECT allow_sessionless FROM usr WHERE id={0};", UserId))) return true;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the service is available to common users.</summary>
        [EntityDataField("available")]
        public bool Available { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Terradue.Portal.WpsProcessOffering"/> is quotable.
        /// </summary>
        /// <value><c>true</c> if is quotable; otherwise, <c>false</c>.</value>
        [EntityDataFieldAttribute("quotable")]
        public bool Quotable { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Terradue.Portal.Service"/> is commercial.
        /// </summary>
        /// <value><c>true</c> if commercial; otherwise, <c>false</c>.</value>
        [EntityDataField("commercial")]
        public bool Commercial { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// The tags used to describe and filter the service.
        /// </summary>
        [EntityDataField ("tags")]
        public string Tags { get; set; }

        /// <summary>
        /// Adds a tag to the tags list.
        /// </summary>
        /// <param name="tag">Tag.</param>
        public void AddTag (string tag) {
            Tags += string.IsNullOrEmpty (Tags) ? tag : "," + tag;
        }

        /// <summary>
        /// Gets the tags as list.
        /// </summary>
        /// <returns>The tags as list.</returns>
        public List<string> GetTagsAsList () {
            if (Tags != null)
                return Tags.Split (",".ToCharArray ()).ToList ();
            else return new List<string> ();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Description text of the service</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("description", IsUsedInKeywordSearch = true)]
        public string Description { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Version of the service</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("version")]
        public string Version { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// URL pinting to the service
        /// </summary>
        /// <value>The URL.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("url")]
        public string Url { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Icon URL of the logo representing the service.
        /// </summary>
        /// <value>The icon URL.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("icon_url")]
        public string IconUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Service validation URL.
        /// </summary>
        /// <value>The validation URL.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("validation_url")]
        public string ValidationUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Service tutorial URL.
        /// </summary>
        /// <value>The tutorial URL.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("tutorial_url")]
        public string TutorialUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Service spec URL.
        /// </summary>
        /// <value>The spec URL.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("spec_url")]
        public string SpecUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Service Terms and Conditions URL.
        /// </summary>
        /// <value>The Terms and Conditions URL.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("termsconditions_url")]
        public string TermsConditionsUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Service Terms and Conditions URL.
        /// </summary>
        /// <value>The Terms and Conditions URL.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("termsconditions_text")]
        public string TermsConditionsText { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("view_url")]
        public string ViewUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Rating of the service</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("rating")]
        public int Rating { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Geometry defining the AOI of the service</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("geometry")]
        public string Geometry { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Class identifier. Can be used to classify the maturity level.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityForeignField("id", CLASS_TABLE)]
        public int ClassId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the service class.</summary>
        [EntityForeignField("name", CLASS_TABLE)]
        public string Class { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the collection of the request parameters (alias for RequestParameters).</summary>
        public ServiceParameterArray Constants { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the service is available considering that administrators can always use the service.</summary>
        public bool CanUse {
            get { return Available || context.UserLevel >= UserLevel.Administrator; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the service categories.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public Category[] Categories { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the filtering condition for listing or viewing only available services.</summary>
        public bool OnlyAvailable { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the callback method that is called to obtain a dynamic service configuration file.</summary>
        public GetDefinitionCallbackType OnGetDefinition { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the callback method that is called for each service parameter when its value has been received.</summary>
        public CheckParameterCallbackType OnCheckParameter { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the callback method that is called once all service parameter values have been received.</summary>
        public ReviewParametersCallbackType OnReviewParameters { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the callback method that is called for building a task from the given parameter values, which should include the creation of jobs and the calculation of required resources.</summary>
        public BuildTaskCallbackType OnBuildTask { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the computing resource that must be used by the service.</summary>
        /// <remarks>This property is overridden in derived classes that represent services that are tightly linked to a sole computing resource.</remarks>
        public virtual ComputingResource FixedComputingResource {
            get { return null; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the ID of the series that must be compatible with the service.</summary>
        /// <remarks>This value is used for listing only services that are compatible with the specific series.</remarks>
        public int SeriesId { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the ID of the computing resource that must be compatible with the service.</summary>
        /// <remarks>This value is used for listing only services that are compatible with the specific computing resource.</remarks>
        public EntityList<ComputingResource> CompatibleComputingResources { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the service definition document (<i>service.xml</i>) is required for the processing.</summary>
        public double[] Priorities { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the file system root directory of the service.</summary>
        public virtual string FileRootDir {
            get { return null; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the root directory of the service for web access.</summary>
        public virtual string RelativeUrl {
            get { return null; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the root directory of the service for web access.</summary>
        public virtual string AbsoluteUrl {
            get { return null; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the root directory of the service for web access.</summary>
        public virtual string SchedulerRelativeUrl {
            get { 
                return String.Format("{0}{1}", RelativeUrl, "?_form=scheduler"); 
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        private User owner;
        public User Owner {
            get {
                if (owner == null) {
                    if (OwnerId != 0) owner = User.FromId(context, OwnerId);
                }
                return owner;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Service instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Service(IfyContext context) : base(context) {
            this.Available = false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a new Service instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public static Service GetInstance(IfyContext context) {
            EntityType entityType = EntityType.GetEntityType(typeof(Service));
            return (Service)entityType.GetEntityInstance(context); 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a Service instance representing the service with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the service ID</param>
        public static Service FromId(IfyContext context, int id) {
            EntityType entityType = EntityType.GetEntityType(typeof(Service));
            Service result = (Service)entityType.GetEntityInstanceFromId(context, id); 
            result.Id = id;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Obsolete, please use FromIdentifier instead")]
        public static Service FromName(IfyContext context, string name) {
            return FromIdentifier(context, name);
        }

        /// <summary>Returns a Service instance representing the service with the specified identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="name">The unique service identifier.</param>
        public static Service FromIdentifier(IfyContext context, string identifier) {
            EntityType entityType = EntityType.GetEntityType(typeof(Service));
            Service result = (Service)entityType.GetEntityInstanceFromIdentifier(context, identifier); 
            result.Identifier = identifier;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a Service instance representing the service with the specified ID or identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="s">A search value that must match the service ID (preferred) or identifier.</param>
        public static Service FromString(IfyContext context, string s) {
            int id = 0;
            Int32.TryParse(s, out id);
            EntityType entityType = EntityType.GetEntityType(typeof(Service));
            Service result = (Service)entityType.GetEntityInstanceFromCondition(context, String.Format("t.identifier={0} OR t.id={1}", StringUtils.EscapeSql(s), id), String.Format("t.id!={0}", id)); 
            result.Id = id;
            result.Identifier = s;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the service information from the database.</summary>
        /// <param name="condition">SQL conditional clause without <c>WHERE</c> keyword</param>
        public override void Load() {
            base.Load();

            Category[] categories = null;

            string sql = String.Format("SELECT t.id, t.name FROM servicecategory AS t INNER JOIN service_category AS t1 ON t.id=t1.id_category WHERE t1.id_service={0};", Id);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                if (categories == null) categories = new Category[1];
                else Array.Resize(ref categories, categories.Length + 1);
                categories[categories.Length - 1] = new Category(context.GetIntegerValue(reader, 0), context.GetValue(reader, 1));
            }
            reader.Close();
            Categories = (categories == null ? new Category[0] : categories);

            context.CloseQueryResult(reader, dbConnection);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Processes a request on an Entity that derives from a service definition file and the service-specific programming logic.</summary>
        /// <remarks>
        ///     This method responds to requests sent to a service's access point and operates on both Task and Scheduler objects.
        ///     
        ///     The table below shows the request types (identified by operation names and key parameters) that are accepted, besides by the Service, also by the Task, Job and Scheduler access point URLs. These classes have homonymous methods for their request processing.
        ///     
        ///     The <b>operation name</b> is the value of the operation parameter, which is normally named <i>_request</i>. The <b>key parameters</b> are the parameters that are needed, besides the operation name, to process the request correctly.
        ///
        ///     The <b>additional request parameters</b> marked with <b>X</b> are are mandatory, those marked with <b>(X)</b> are optional. Parameters that are neither mandatory nor optional for a type of request are ignored for that request. The four groups of additional request parameters are:
        ///     <list type="bullet">
        ///         <item><b>%Service parameters</b> include all parameters defined in the service definition file <i>service.xml</i>. Some of the service parameters are translated into <b>task attributes</b> or <b>scheduler attributes</b> (e.g. the Computing Elemenent or the priority), others into <b>task parameters</b> or <b>scheduler parameters</b> (the additional service-specific parameters).</item>
        ///         <item><b>%Scheduler attributes</b> are the fields common to all schedulers, such as the scheduler class, mode or validity dates.</item>
        ///         <item><b>%Task attributes</b> are the fields common to all tasks, such as the computing resource, the priority or the publish server of a task.</item>
        ///         <item><b>%Job parameters</b> are the parameters that are added to a job during the job creation by the service-specific method assigned to the #OnBuildTask property. They usually depend on the <b>task parameters</b>.</item>
        ///     </list>
        /// </remarks>
        public virtual void ProcessRequest() {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a collection of configurable parameters and constants.</summary>
        public virtual void GetConfigurableParameters(RequestParameterCollection parameters) {
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the service can be used by the user and prepares the service parameter validity checks.</summary>
        /// <returns><c>true</c> if the service can be used and the validity checks can be performed</returns>
        public virtual bool Check(int userId) {
            if (serviceChecked) return validService;

            //minPriority = 0;
            //maxPriority = 1;

            UserId = userId;
            validService = true;

            if (!CanView) {
                if (UserId == context.UserId) {
                    validService = (context.UserLevel >= UserLevel.Administrator);
                    if (validService) context.AddWarning("You are not authorized to use this service", "notAllowedService"); // !!! use exception and set class also for other entities
                    else throw new EntityUnauthorizedException("You are not authorized to use this service", EntityType, this, UserId); // !!! use exception and set class also for other entities
                } else {
                    throw new EntityUnauthorizedException("The owner is not authorized to use this service", EntityType, this, UserId); // !!! use exception and set class also for other entities
                }
            }

            serviceChecked = true;
            return validService;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <returns>The parameters.</returns>
        public abstract ServiceParameterSet GetParameters();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Checks the parameters.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void CheckParameters(ServiceParameterSet parameters) {
            parameters.Check();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Reads the constants from the service definition file. </summary>
        /*!
        /// <param name="writeUnknownConstants">indicates whether also unrecognized <i>&lt;const&gt;</i> elements are to be written to the single item XML output </param>
        */
        public virtual void WriteConstants(XmlWriter output, bool writeUnknownConstants) {
            if (Constants == null) return;
            
            // Read constants for the task definition
            
            foreach (ServiceParameter constant in Constants) {
                output.WriteStartElement("const");
                output.WriteAttributeString("name", constant.Name);
                output.WriteAttributeString("type", constant.Type);
                output.WriteAttributeString("value", constant.Value);
                output.WriteEndElement(); // </const>
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool GetConfigBooleanValue(IDataReader reader, bool max, bool defaultValue) {
            bool result = defaultValue, value;
            int level = 0;

            while (reader.Read()) {
                if (reader.GetValue(1) == DBNull.Value || !Boolean.TryParse(reader.GetString(1), out value)) continue;
                if (level == 0) {
                    level = reader.GetInt32(0);
                    result = value;
                } else if (reader.GetInt32(0) == level) {
                    if (max && value && !result || !max && !value && result) result = value;
                } else {
                    break;
                }
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the the integer numeric value for a configurable service constant that applies for a service considering the requesting user and his group assignments. </summary>
        /// <param name="paramName">the name of the configurable service constant</param>
        /// <param name="max">determines whether in case of alternative values at the same level the maximum value is chosen (otherwise the minimum value is chosen)</param>
        /// <param name="defaultValue">the default result value if no other value can be retrieved</param>
        /// <remarks>This can be used to obtain a single value that configures a service attribute.</remarks>
        public int GetConfigIntegerValue(IDataReader reader, bool max, int defaultValue) {
            int result = defaultValue, value;
            int level = 0;

            while (reader.Read()) {
                if (reader.GetValue(1) == DBNull.Value || !Int32.TryParse(reader.GetString(1), out value)) continue;
                if (level == 0) {
                    level = reader.GetInt32(0);
                    result = value;
                } else if (reader.GetInt32(0) == level) {
                    if (max && value > result || !max && value < result) result = value;
                } else {
                    break;
                }
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the the real numeric value for a configurable service constant that applies for a service considering the requesting user and his group assignments. </summary>
        /// <param name="paramName">the name of the configurable service constant</param>
        /// <param name="max">determines whether in case of alternative values at the same level the maximum value is chosen (otherwise the minimum value is chosen)</param>
        /// <param name="defaultValue">the default result value if no other value can be retrieved</param>
        /// <remarks>
        ///     This can be used to obtain a single value that configures a service attribute.
        ///     Example: the maximum task priority can be defined at service level, but may be overridden by group-specific or user-specific value.
        ///     This function returns the value that applies for the user (if defined), otherwise for the group(s) (if defined), otherwise for the service (if defined), or the specified default value if none of the other applies.
        /// </remarks>
        public double GetConfigDoubleValue(IDataReader reader, bool max, double defaultValue) {
            double result = defaultValue, value;
            int level = 0;

            while (reader.Read()) {
                if (reader.GetValue(1) == DBNull.Value || !Double.TryParse(reader.GetString(1), out value)) continue;
                if (level == 0) {
                    level = reader.GetInt32(0);
                    result = value;
                } else if (reader.GetInt32(0) == level) {
                    if (max && value > result || !max && value < result) result = value;
                } else {
                    break;
                }
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public DateTime GetConfigDateTimeValue(IDataReader reader, bool max, DateTime defaultValue) {
            DateTime result = defaultValue = context.Now, value;
            int level = 0;

            while (reader.Read()) {
                if (reader.GetValue(1) == DBNull.Value || !DateTime.TryParse(reader.GetString(1), out value)) continue;
                if (level == 0) {
                    level = reader.GetInt32(0);
                    result = value;
                } else if (reader.GetInt32(0) == level) {
                    if (max && value > result || !max && value < result) result = value;
                } else {
                    break;
                }
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetConfigValue(IDataReader reader, bool max, string defaultValue) {
            string result = defaultValue, value;
            int level = 0;

            while (reader.Read()) {
                if (reader.GetValue(1) == DBNull.Value) continue;
                value = reader.GetString(1);
                if (level == 0) {
                    level = reader.GetInt32(0);
                    result = value;
                } else if (reader.GetInt32(0) == level) {
                    if (max && String.Compare(value, result) > 0 || String.Compare(value, result) < 0) result = value;
                } else {
                    break;
                }
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the query for retrieving the value for a configurable service constant that applies for a service considering the requesting user and his group assignments. </summary>
        /// <param name="paramName">the name of the configurable service constant</param>
        /// <returns>the query SQL</returns>
        /// <remarks>
        ///     This is used to obtain a single value that configures a service attribute.
        ///     Example: the maximum task priority can be defined at service level, but may be overridden by group-specific or user-specific value.
        ///     This function returns the value that applies for the user (if defined), otherwise for the group(s) (if defined), otherwise for the service (if defined), or the specified default value if none of the other applies.
        ///     In case of different values for the same level the specified aggregate function is used (a user could belong to different groups which have different maximum task priorities set for the service).
        /// </remarks>
        protected string GetConfigQuery(string paramName) {
            return String.Format(
                "SELECT DISTINCT CASE WHEN (sc.id_service IS NULL AND sc.id_grp IS NULL AND sc.id_usr IS NULL) OR sc.id_service IS NULL THEN 4 WHEN (sc.id_grp IS NULL AND sc.id_usr IS NULL) AND sc.id_service IS NOT NULL THEN 3 WHEN sc.id_grp IS NOT NULL AND sc.id_service IS NOT NULL THEN 2 WHEN sc.id_usr IS NOT NULL AND sc.id_service IS NOT NULL THEN 1 END AS level, sc.value FROM serviceconfig AS sc LEFT JOIN usr_grp AS ug ON ug.id_usr={0} AND (sc.id_usr={0} OR (sc.id_usr IS NULL AND sc.id_grp IS NOT NULL AND ug.id_grp=sc.id_grp)) WHERE (sc.id_service={1} OR sc.id_service IS NULL) AND (sc.id_usr={0} OR sc.id_usr IS NULL AND ug.id_grp IS NOT NULL) AND sc.name={2} ORDER BY level;",
                UserId,
                Id,
                StringUtils.EscapeSql(paramName)
                );
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds the task.
        /// </summary>
        /// <param name="task">Task.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public abstract void BuildTask(Task task);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the requested service operation type from the given name or a default type if no type matches.</summary>
        /// <param name="name">the name of the requested operation</param>
        /// <param name="defaultOperation">the default operation type if none of the defined types matches</param>
        public static ServiceOperationType GetServiceOperation(string name, ServiceOperationType defaultOperation) {
            switch (name) {
                case null : 
                    return defaultOperation;
                case "view" :
                    return ServiceOperationType.ViewItem;
                case "define" :
                case "clone" :
                    return ServiceOperationType.Define;
                case "create" :
                case "recreate" :
                    return ServiceOperationType.Create;
                case "modify" :
                    return ServiceOperationType.Modify;
                case "submit" :
                    return ServiceOperationType.Submit;
                case "restart" :
                    return ServiceOperationType.Create | ServiceOperationType.Submit;
                case "abort" :
                    return ServiceOperationType.Abort;
                case "retry" : 
                    return ServiceOperationType.Retry;
                case "copy" : 
                    return ServiceOperationType.Copy;
                case "delete" : 
                    return ServiceOperationType.Delete;
                case "restore" : 
                    return ServiceOperationType.Restore;
                case "schedule" :
                    return ServiceOperationType.Schedule;
                case "advance" :
                    return ServiceOperationType.Advance;
                case "reset" :
                    return ServiceOperationType.Reset;
                case "set" :
                    return ServiceOperationType.SetParameter;
                case "accept" :
                    return ServiceOperationType.Accept;
                    default :
                    return defaultOperation;
            }
        }

        public struct Category {
            public int Id;
            public string Caption;

            public Category(int id, string caption) {
                this.Id = id;
                this.Caption = caption;
            }
        }

        #region IEntitySearchable implementation
        public override object GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
            case "tag":
                    if (string.IsNullOrEmpty(value)) return base.GetFilterForParameter(parameter, value);
                    var tags = value.Split(",".ToArray());
                    if (tags.Length == 1) {
                        var result = new string[] { value, value + "\\,*", "*\\," + value, "*\\," + value + "\\,*" };
                        return new KeyValuePair<string, string[]>("Tags", result);
                    } else if (tags.Length > 1) {

                        var tags2 = tags.ToList();
                        var p = GetPermutations(tags2, tags.Count());
                        var allPermutations = p.Select(subset => string.Join("\\,", subset.Select(t => t).ToArray())).ToList();
                        for (int i=0; i < tags.Length; i++){
                            tags2.Add("_TMP_" + i);
                            p = GetPermutations(tags2, tags2.Count());
                            var p1 = p.Select(subset => string.Join("\\,", subset.Select(t => t).ToArray())).ToList();
                            allPermutations.AddRange(p1);
                        }

                        var finalAllPermutations = new List<string>();
                        foreach (var ap in allPermutations) {
                            var tmp = ap;
                            for (int i = 0; i < tags.Length; i++) tmp = tmp.Replace("_TMP_" + i, "*");
                            while (tmp.Contains("*\\,*")) tmp = tmp.Replace("*\\,*", "*");
                            if (!finalAllPermutations.Contains(tmp)) finalAllPermutations.Add(tmp);
                        }

                        return new KeyValuePair<string, string[]>("Tags", finalAllPermutations.ToArray());
                    }
                    return base.GetFilterForParameter(parameter, value);
                default:
                return base.GetFilterForParameter(parameter, value);
            }
        }

		private IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length) {
			if (length == 1) return list.Select(t => new T[] { t });

			return GetPermutations(list, length - 1)
				.SelectMany(t => list.Where(e => !t.Contains(e)),
					(t1, t2) => t1.Concat(new T[] { t2 }));
		}


        #endregion

        #region IAtomizable implementation

        public bool IsSearchable (System.Collections.Specialized.NameValueCollection parameters)
        {
            string identifier = (this.Identifier != null ? this.Identifier : "service" + this.Id);
            string name = (this.Name != null ? this.Name : identifier);
            string text = (this.TextContent != null ? this.TextContent : "");

            if (parameters ["q"] != null) {
                string q = parameters ["q"];
                if (!(name.Contains (q) || identifier.Contains (q) || text.Contains (q))) return false;
            }
            return true;
        }

        public override AtomItem ToAtomItem(NameValueCollection parameters) {

            string identifier = (this.Identifier != null ? this.Identifier : "service" + this.Id);
            string name = (this.Name != null ? this.Name : identifier);

            if (!IsSearchable(parameters)) return null;

            Uri alternate = new Uri(this.Url);
                
            AtomItem entry = new AtomItem(identifier, name, alternate, this.Id.ToString(), DateTime.UtcNow);
            entry.Categories.Add(new SyndicationCategory("service"));

            entry.Summary = new TextSyndicationContent(name);
            entry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", identifier);

            return entry;
        }

        public new NameValueCollection GetOpenSearchParameters() {
            var parameters = OpenSearchFactory.GetBaseOpenSearchParameter();
            parameters.Set("grouped", "{t2:grouped?}");
            parameters.Set("tag", "{t2:tag?}");
            return parameters;
        }

        #endregion

        /// <summary>
        /// Is the service shared to user.
        /// </summary>
        /// <returns><c>true</c>, if shared to user, <c>false</c> otherwise.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="policy">Policy of sharing (direct = permission directly given to the user, role = permission only given via role and privilege, none = one of both previous cases ).</param>
        public bool IsSharedToUser(int id, string policy = "none") {
            bool permissionOnly = false;
            bool privilegeOnly = false;
            switch(policy){
                case "permission":
                    permissionOnly = true;
                    break;
                case "privilege":
                    privilegeOnly = true;
                    break;
                default:
                    break;
            }
            var sharedUsersIds = this.GetAuthorizedUserIds(permissionOnly, privilegeOnly);
            return sharedUsersIds != null && (sharedUsersIds.Contains(id));
        }

        public bool IsSharedWithUsers(){
            string sql = String.Format("SELECT COUNT(*) FROM service_perm WHERE id_service={0} AND ((id_usr IS NOT NULL AND id_usr != {1}) OR id_grp IS NOT NULL);", this.Id, this.UserId);
            return context.GetQueryIntegerValue(sql) > 0;
        }

        public bool IsSharedToCommunity() {
            return (this.Owner != null && this.DomainId != this.Owner.DomainId);
        }

        public bool IsRestricted(){
            return (IsSharedWithUsers() || IsSharedToCommunity());
        }
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class GenericService : Service {
        
        public GenericService(IfyContext context) : base(context) {}
        
        public override ServiceParameterSet GetParameters() {
            return null;
        }
        
        public override void BuildTask(Task task) {}
        
    }
    
    
    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>%Service derivate operation types</summary>
    [Flags]
    public enum ServiceOperationType {

        /// <summary>Unknown operation type.</summary>
        None            = 0x0000,

        /// <summary>View a derivate single item.</summary>
        ViewItem        = 0x0001,

        /// <summary>View a derivate item list.</summary>
        ViewList        = 0x0002,

        /// <summary>Define a new derivate item or (for tasks only) a clone of an existing task.</summary>
        Define          = 0x0004,

        /// <summary>Create a new derivate item or (for tasks only) recreate a task based on an existing one.</summary>
        Create          = 0x0008,

        /// <summary>Modify a derivate item.</summary>
        Modify          = 0x0010,

        /// <summary>Delete a derivate item.</summary>
        Delete          = 0x0020,

        /// <summary>Restore a derivate item.</summary>
        Restore         = 0x0040,

        /// <summary>Submit a task.</summary>
        Submit          = 0x0080,

        // /// <summary>Requery input data.</summary>
        //Requery         = 0x0100,

        /// <summary>Abort a task.</summary>
        Abort           = 0x0200,

        /// <summary>Resubmit the failed jobs of a task.</summary>
        Retry           = 0x0400,

        /// <summary>Create an identical copy of a task.</summary>
        Copy            = 0x0800,

        /// <summary>Use the task parameters for the definition of a scheduler.</summary>
        Schedule        = 0x1000,

        /// <summary>Create the next task of a scheduler. </summary>
        Advance         = 0x2000,

        /// <summary>Reset a scheduler to its initial state.</summary>
        Reset           = 0x4000,

        /// <summary>Set a task or job parameter.</summary>
        SetParameter    = 0x8000,

        /// <summary>Accept a job as completed.</summary>
        Accept          = 0x10000
    }


    public class TaskOperationType {
        public const int None = 0;
        public const int Create = 10;
        public const int ConfirmDelay = 11;
        public const int Restart = 12;
        public const int Submit = 20;
        public const int Retry = 21;
        public const int Abort = 15;
        public const int Delete = 255;
    }


}
