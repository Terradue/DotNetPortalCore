using System;
using Terradue.Util;
using Terradue.Portal;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using System.Web;
using System.IO;

namespace Terradue.Gpod.Portal {
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class EntityTypeStructure : EntityData {

        public override string EntityName {
            get { return "Control Panel"; }
        }

        public EntityTypeStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.EntityType), optionList) {
            if (context.UserLevel < UserLevel.Administrator) {
                Join += " INNER JOIN priv AS t1 ON t.id=t1.id_basetype AND t1.operation!='A' INNER JOIN role_priv AS t2 ON t1.id=t2.id_priv INNER JOIN usr_role AS t3 ON t2.id_role=t3.id_role";
                FilterCondition = "t3.id_usr=" + context.UserId;
            }
            Fields.Add(new SingleValueExpression("link", String.Format("CONCAT('{0}/', keyword)", context.AdminRootUrl), "url", null, null, FieldFlags.List | FieldFlags.Attribute));
            Fields.Add(new SingleValueField("caption", "caption", "string", "Caption", null, FieldFlags.List | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("icon", "icon_url", "url", "Icon URL", null, FieldFlags.List | FieldFlags.Optional));
            Paging = false;
            CustomSorting = "t.id_module, t.pos";
            ShowItemLinks = false;
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ArticleStructure : EntityData {

        public override string EntityName {
            get { return "News Articles"; }
        }

        /*EntityData commentsData = new EntityData(
                    "Comments", 
                    "articlecomment",
                    new FieldExpression[] {
                        new SingleValueField("author", "string", "Author", FieldFlags.Both | FieldFlags.Searchable),
                        new SingleValueField("email", "email", "E-mail", FieldFlags.Both),
                        new SingleValueField("country", "countries", "Country", FieldFlags.Both),
                        new SingleValueField("time", "datetime", "Date", FieldFlags.Both),
                        new SingleValueField("ip", "ip", "IP Address", FieldFlags.Both),
                        new SingleValueField("comments", "string", "Comment", FieldFlags.Both | FieldFlags.Searchable),
                    }
            );

            Entity comments = Entity.GetInstance(context, commentsData);*/

        public ArticleStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Article), optionList) {
            Fields.Add(new SingleValueField("title", "string", "Title", FieldFlags.Both | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("abstract", "text", "Abstract", FieldFlags.Both | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("content", "text", "Content", FieldFlags.Item | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("time", "datetime", "Date and time", FieldFlags.Both | FieldFlags.Optional));
            Fields.Add(new SingleValueField("url", "url", "Article URL", FieldFlags.Item | FieldFlags.Optional));
            Fields.Add(new SingleValueField("author", "string", "Author", FieldFlags.Item | FieldFlags.Optional));
            Fields.Add(new SingleValueField("tags", "string", "Tags", FieldFlags.Item | FieldFlags.Optional));
            //Fields.Add(new MultipleEntityField("comments", comments, "id_article", "Comments", FieldFlags.Item));
            CustomSorting = "time DESC";
        }
    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class FaqStructure : EntityData {

        public FaqStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Faq), optionList) {
            Fields.Add(new SingleValueField("caption", "question", "string", "Question", null, FieldFlags.Both | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("answer", "answer", "text", "Answer", null, FieldFlags.Item | FieldFlags.Searchable));
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ImageStructure : EntityData {

        public ImageStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Image), optionList) {
            Fields.Add(new SingleValueField("caption", "string", "Caption", FieldFlags.Both | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("description", "text", "Description", FieldFlags.Item | FieldFlags.Searchable | FieldFlags.Optional));
            Fields.Add(new SingleValueField("url", "url", "URL", FieldFlags.Item | FieldFlags.Optional));
            Fields.Add(new SingleValueField("previewUrl", "small_url", "url", "Preview URL", null, FieldFlags.Both | FieldFlags.Optional));
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ProjectStructure : EntityData {

        public ProjectStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Project), optionList) {
            Fields.Add(new SingleValueField("title", "string", "Title", FieldFlags.Both | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("short_description", "text", "Abstract", FieldFlags.Item | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("long_description", "text", "Description", FieldFlags.Item | FieldFlags.Optional));
            Fields.Add(new SingleValueField("contact", "string", "Contacts", FieldFlags.Item | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("status", "projectStatus", "Status", FieldFlags.Item | FieldFlags.Optional));
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class CatalogueStructure : EntityData {

        public CatalogueStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Catalogue), optionList) {
            if (OptionList) {
                Fields.Add(new SingleValueField("value", "id", "int", "ID", null, FieldFlags.Both));
                Fields.Add(new SingleValueExpression("caption", "name", "string", "Name", null, FieldFlags.Both | FieldFlags.SortAsc));
            } else {
                Fields.Add(new SingleValueField("caption", "name", "identifier", "Name", null, FieldFlags.Both | FieldFlags.Searchable));
                Fields.Add(new SingleValueField("description", "text", "Description", FieldFlags.Item | FieldFlags.Searchable | FieldFlags.Optional));
                Fields.Add(new SingleReferenceField("domain", "domain", "@.name", "id_domain", "Domain", null, FieldFlags.Both | FieldFlags.Optional));
                //Fields.Add(new SingleValueField("configDelegation", "conf_deleg", "bool", "Delegate Configuration", null, FieldFlags.List));
                Fields.Add(new SingleValueField("osDescriptionUrl", "osd_url", "url", "OpenSearch Description URL", null, FieldFlags.Both));
                Fields.Add(new SingleValueField("baseUrl", "base_url", "url", "Base URL (prefix for relative URLs)", null, FieldFlags.Item));
                Fields.Add(new SingleValueField("seriesRelUrl", "series_rel_url", "url", "Relative URL for series list/ingestion", null, FieldFlags.Item | FieldFlags.Optional));
                Fields.Add(new SingleValueField("dataSetRelUrl", "dataset_rel_url", "url", "Relative URL for data set list/ingestion", null, FieldFlags.Item | FieldFlags.Optional));
            }
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class LightGridEngineStructure : EntityData {

        public LightGridEngineStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.LightGridEngine), optionList) {
            Fields.Add(new SingleValueField("caption", "name", "string", "Caption", null, FieldFlags.Both | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("wsUrl", "ws_url", "url", "wsServer access point", null, FieldFlags.Item | FieldFlags.Optional));
            Fields.Add(new SingleValueField("statusMethod", "status_method", "taskStatusRequest", "Status request method", null, FieldFlags.Item | FieldFlags.Lookup | FieldFlags.Custom));
            Fields.Add(new SingleValueField("myproxyAddress", "myproxy_address", "url", "Myproxy address", null, FieldFlags.Both | FieldFlags.Optional));
            Fields.Add(new SingleValueField("taskStatusUrl", "task_status_url", "url", "Task status URL", null, FieldFlags.Item | FieldFlags.Optional));
            Fields.Add(new SingleValueField("jobStatusUrl", "job_status_url", "url", "Job status URL", null, FieldFlags.Item | FieldFlags.Optional));
            //Fields.Add(new SingleValueField("ceStatusUrl", "ce_status_url", "Computing Element status URL", FieldFlags.Item | FieldFlags.Optional));
            Fields.Add(new SingleValueField("confFile", "conf_file", "string", "Configuration file name", null, FieldFlags.Item | FieldFlags.Optional));
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ActionStructure : EntityData {

        bool invalidInterval;

        public ActionStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Action), optionList) {
            if (OptionList) {
                Fields.Add(new SingleValueField("value", "id", "int", "ID", null, FieldFlags.List));
                Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.List));

            } else {
                Fields.Add(new SingleValueField("pos", "int", null, FieldFlags.Hidden | FieldFlags.SortAsc));
                Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.Both | FieldFlags.ReadOnly));
                Fields.Add(new SingleValueField("description", "string", "Description", FieldFlags.Item | FieldFlags.ReadOnly));
                Fields.Add(new SingleValueField("enabled", "bool", "Enabled", FieldFlags.Both));
                Fields.Add(new SingleValueField("interval", "time_interval", "timespan", "Execution Interval", null, FieldFlags.Both | FieldFlags.Optional | FieldFlags.Custom));
                Fields.Add(new SingleValueField("next", "next_execution_time", "datetime", "Next Execution Time", null, FieldFlags.Both | FieldFlags.Optional));
                Fields.Add(new SingleValueField("immediate", "bool", "Execute immediately (at next cycle)", FieldFlags.Item));
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Processes a request on an Action entity.</summary>
        /// 
        /// This method executes the action specified in the request.
        /// This is obtained using the Control Panel action URL with the query string ?_request=execute&id=<i><b>id</b></i>.
        /// Consider that the operation may involve large amounts of data and time out.
        public void ProcessRequest() {
            if (context.GetParamValue("_request") != "execute") return;

            switch (Identifier) {
                case "task" :
                    Task.ExecuteTaskPendingOperations(context);
                    break;
                    case "scheduler" :
                    //Scheduler.SubmitNewTasks();
                    break;
                    case "series" :
                    Series.ExecuteCatalogueSeriesRefresh(context);
                    break;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets default values to fields when writing into the database.</summary>
        protected override void OnAfterRead() {
            FieldExpression field = Fields["interval"];
            if (Fields["enabled"].AsBoolean) field.Flags &= FieldFlags.AllButOptional;
            if (field.Value == null) return;
            int interval = StringUtils.StringToSeconds(field.Value);
            if (interval == 0) {
                invalidInterval = true;
                field.Invalid = true;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnItemNotProcessed(OperationType type, int itemId) {
            if (invalidInterval) {
                context.AddError("The execution interval has an incorrect format; use a number with one of the time units \"YMWDhms\", e.g. \"2h\"");
                return true;
            }
            return false;
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ApplicationStructure : EntityData {

        public ApplicationStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Application), optionList) {
            if (OptionList) {
                Fields.Add(new SingleValueField("value", "id", "int", "ID", null, FieldFlags.List));
                Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.List));
            } else {
                Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.Both));
                Fields.Add(new SingleValueField("name", "identifier", "identifier", "Identifier", null, FieldFlags.Item));
                Fields.Add(new SingleValueField("available", "bool", "Available", FieldFlags.Both));
                Fields.Add(new SingleValueField("configFile", "config_file", "file", "Configuration file", null, FieldFlags.Item));
            }
        }


    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ManagerRoleStructure : EntityData {

        public ManagerRoleStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.ManagerRole), optionList) {
            if (OptionList) {
                Fields.Add(new SingleValueField("value", "id", "int", "ID", null, FieldFlags.Both));
                Fields.Add(new SingleValueExpression("caption", "t.name", "string", "Name", null, FieldFlags.Both | FieldFlags.SortAsc));
            } else {
                MultipleReferenceField privileges = new MultipleReferenceField("privileges", "priv", "@.name", "role_priv", "Privileges", FieldFlags.Item | FieldFlags.Optional);
                privileges.SortExpression = "pos";
                Fields.Add(new SingleValueField("caption", "name", "identifier", "Name", null, FieldFlags.Both | FieldFlags.Searchable));
                Fields.Add(new SingleValueField("description", "text", "Description", FieldFlags.Item | FieldFlags.Searchable | FieldFlags.Optional));
                Fields.Add(new SingleReferenceField("domain", "domain", "@.name", "id_domain", "Domain", null, FieldFlags.Both | FieldFlags.Optional));
                Fields.Add(privileges);
                Fields.Add(new MultipleReferenceField("users", "usr", "CONCAT(@.firstname, ' ', @.lastname)", "usr_role", "Users", FieldFlags.Item));
            }
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ProductTypeStructure : EntityData {

        public ProductTypeStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.ProductType), optionList) {
            MultipleReferenceField groups = new MultipleReferenceField("groups", "grp", "@.name", "producttype_priv", "Groups", FieldFlags.Item | FieldFlags.Optional);

            Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.Both | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("name", "identifier", "identifier", "Identifier", null, FieldFlags.Item | FieldFlags.Searchable));
            Fields.Add(new SingleValueField("description", "text", "Description", FieldFlags.Item | FieldFlags.Searchable | FieldFlags.Optional));
            Fields.Add(new SingleValueField("catDescription", "cat_description", "url", "Catalogue Description URL", null, FieldFlags.Item));
            Fields.Add(new SingleValueField("catTemplate", "cat_template", "url", "Catalogue URL Template", null, FieldFlags.Item | FieldFlags.Optional));
            Fields.Add(new SingleValueField("iconUrl", "icon_url", "url", "Icon/logo URL", null, FieldFlags.Item | FieldFlags.Optional));
            Fields.Add(new SingleValueField("legend", "string", "Legend", FieldFlags.Item | FieldFlags.Searchable | FieldFlags.Optional));
            Fields.Add(new SingleValueField("rolling", "bool", "Rolling product", FieldFlags.Item));
            Fields.Add(new SingleValueField("public", "bool", "Available to public", FieldFlags.Item));
            Fields.Add(groups);
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ServiceCategoryStructure : EntityData {

        public ServiceCategoryStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.ServiceCategory), optionList) {
            if (OptionList) {
                Fields.Add(new SingleValueField("value", "id", "int", "ID", null, FieldFlags.Both));
                Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.Both));
            } else {
                Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.Both | FieldFlags.Searchable));
                Fields.Add(new SingleValueField("name", "identifier", "identifier", "Identifier", null, FieldFlags.Item | FieldFlags.Searchable));
            }
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ServiceClassStructure : EntityData {

        public ServiceClassStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.ServiceClass), optionList) {
            if (OptionList) {
                Fields.Add(new SingleValueField("value", "id", "int", "ID", null, FieldFlags.Both));
                Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.Both));
            } else {
                Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.Both | FieldFlags.Searchable));
                Fields.Add(new SingleValueField("name", "identifier", "identifier", "Identifier", null, FieldFlags.Item | FieldFlags.Searchable));
            }
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class DomainStructure : EntityData {

        public DomainStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Domain), optionList) {
            if (OptionList) {
                Fields.Add(new SingleValueField("value", "id", "int", "ID", null, FieldFlags.Both));
                Fields.Add(new SingleValueExpression("name", "t.name", "string", "Name", null, FieldFlags.Both | FieldFlags.SortAsc));
            } else {
                Fields.Add(new SingleValueField("caption", "name", "identifier", "Name", null, FieldFlags.Both | FieldFlags.Searchable | FieldFlags.Unique));
                Fields.Add(new SingleValueField("description", "text", "Description", FieldFlags.Item | FieldFlags.Searchable | FieldFlags.Optional));
                AppendFields(null, null, GetDomainMultipleFields());
            }
        }

        protected List<FieldExpression> GetDomainMultipleFields() {
            List<EntityType> entityTypes = EntityType.GetDomainEntityTypes();
            List<FieldExpression> result = new List<FieldExpression>();

            foreach (EntityType entityType in entityTypes) {
                string expr;
                switch (entityType.Name) {
                    case "usr" :
                        expr = "CONCAT(@.firstname, ' ', @.lastname)";
                        break;
                    case "cloudprov" :
                    case "laboratory" :
                        expr = "@.caption";
                        break;
                    default :
                        expr = "@.name";
                        break;
                }
                result.Add(new MultipleReferenceField(entityType.Name, entityType.Name, expr, null as string, null, "id_domain", entityType.PluralCaption + " in this Domain", null, FieldFlags.Item | FieldFlags.Optional));
            }
            return result;
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class GroupStructure : EntityData {

        public GroupStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Group), optionList) {
            if (OptionList) {
                Fields.Add(new SingleValueField("value", "id", "int", "ID", null, FieldFlags.Both));
                Fields.Add(new SingleValueExpression("caption", "t.name", "string", "Name", null, FieldFlags.Both | FieldFlags.SortAsc));
            } else {
                Fields.Add(new SingleValueField("caption", "name", "identifier", "Name", null, FieldFlags.Both | FieldFlags.Searchable));
                Fields.Add(new SingleValueField("description", "text", "Description", FieldFlags.Item | FieldFlags.Searchable | FieldFlags.Optional));
                Fields.Add(new SingleReferenceField("domain", "domain", "@.name", "id_domain", "Domain", null, FieldFlags.Both | FieldFlags.Optional));
                Fields.Add(new SingleValueField("default", "is_default", "bool", "Automatically selected for new users", null, FieldFlags.Item | FieldFlags.Optional));
                Fields.Add(new MultipleReferenceField("users", "usr", "CONCAT(@.firstname, ' ', @.lastname)", "usr_grp", "Users", FieldFlags.Item | FieldFlags.Optional));
                AppendFields(null, null, GetAssignedMultipleFields());
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected List<FieldExpression> GetAssignedMultipleFields() {
            List<EntityType> entityTypes = EntityType.GetAssignedEntityTypes();
            List<FieldExpression> result = new List<FieldExpression>();

            foreach (EntityType entityType in entityTypes) {
                string expr;
                switch (entityType.Name) {
                    case "usr" :
                        expr = "CONCAT(@.firstname, ' ', @.lastname)";
                        break;
                    case "cloudprov" :
                    case "laboratory" :
                        expr = "@.caption";
                        break;
                    default :
                        expr = "@.name";
                        break;
                }
                result.Add(new MultipleReferenceField(entityType.Name, entityType.Name, expr, entityType.Name + "_priv", "id_" + entityType.Name, "id_grp", entityType.PluralCaption + " for this Group", null, FieldFlags.Item | FieldFlags.Optional));
            }
            return result;
        }


    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public abstract class ComputingResourceStructure : EntityData {

        private int[] defaultServiceIds;
        private ComputingResource computingResource;

        //---------------------------------------------------------------------------------------------------------------------

        public abstract ComputingResource ComputingResource {
            get;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the ID of the service that must be compatible with the computing resource.</summary>
        public int ServiceId { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public ComputingResourceStructure(IfyWebContext context, Type type, bool optionList) : base(context, type, optionList) {
            if (context.AdminMode) {
                string loadStr = "''";

                if (Id != 0) {
                    computingResource = ComputingResource.FromId(context, Id);
                    computingResource.Load();
                    computingResource.ForceRefresh = true;
                    int load = computingResource.LoadPercentage;
                    loadStr = (load == -1 ? "Status unavailable" : String.Format("{0}%", load));
                }

                // TODO: re-enable
                /*EntityData defaultServicesEntity = new EntityData.GetInstance(context, new EntityData(
                    null, 
                    "service_cr",
                    new FieldExpression[] {
                    new FixedValueField("default", "is_default", "bool", "true", 0)
                }
                ));
                MultipleReferenceField defaultServicesField = new MultipleReferenceField("defaultServices", "service", "@.name", defaultServicesEntity, "Services using this computing resource as default", FieldFlags.Item | FieldFlags.Optional | FieldFlags.Custom);*/

                // Prepare multi-reference field for groups allowed to use this computing resource
                MultipleReferenceField groups = new MultipleReferenceField("groups", "grp", "@.name", "cr_priv", "Groups", FieldFlags.Item | FieldFlags.Optional);

                // Finally, set all fields for computing resource entity
                Fields.Add(new SingleValueField("availability", "resourceAvailability", "Availability", FieldFlags.Item | FieldFlags.Lookup | FieldFlags.Custom));
                Fields.Add(Id == 0 ? EntityData.EmptyField : new SingleValueExpression("load", "'" + loadStr + "'", "string", "Current usage", null, FieldFlags.Item | FieldFlags.ReadOnly));
                Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.Both | FieldFlags.Searchable | FieldFlags.SortAsc));
                Fields.Add(new SingleValueField("description", "text", "Description", FieldFlags.Item | FieldFlags.Searchable | FieldFlags.Optional));
                Fields.Add(new SingleReferenceField("domain", "domain", "@.name", "id_domain", "Domain", null, FieldFlags.Both | FieldFlags.Optional));
                Fields.Add(new SingleValueField("hostname", "string", "Hostname", FieldFlags.Item | FieldFlags.Searchable));
                Fields.Add(new SingleValueField("capacity", "capacity", "int", "Maximum capacity", null, FieldFlags.Item));
                Fields.Add(new SingleValueField("iconUrl", "icon_url", "url", "Icon/logo URL", null, FieldFlags.Item | FieldFlags.Optional));
                Fields.Add(new SingleValueField("creditControl", "credit_control", "bool", "Credit management at user level", null, FieldFlags.Item));
                // TODO: re-enable
                //                Fields.Add(defaultServicesField);
                Fields.Add(new MultipleReferenceField("services", "service", "@.name", "service_cr", "Compatible services", FieldFlags.Item | FieldFlags.Custom | FieldFlags.Optional));
                Fields.Add(groups);
                if (Id != 0 && CanModify && context.AdminRootUrl != null) AddOperation(OperationType.Other, "config", "Configure User Credits ...", false, "GET", String.Format("{0}/users", context.ScriptUrl), false, null);

            } else {

                Join = String.Format("cr AS t{0}", ServiceId == 0 ? String.Empty : " INNER JOIN service_cr AS c ON t.id=c.id_cr AND c.id_service=" + ServiceId);

                if (OptionList) {
                    Fields.Add(new SingleValueField("value", "id", null, null, null, FieldFlags.Both | FieldFlags.Id));
                    Fields.Add(new SingleValueField("caption", "name", null, null, null, FieldFlags.List | FieldFlags.Unique | FieldFlags.SortAsc));
                    Fields.Add(new SingleValueExpression("default", (ServiceId == 0 ? "0" : "CASE WHEN c.is_default THEN 1 ELSE 0 END"), null, null, null, FieldFlags.List));
                    Fields.Add(new SingleValueExpression("disabled", String.Format("t.availability+{0}<={1}", context.UserLevel, IfyContext.MaxUserLevel), "bool", null, null, FieldFlags.List | FieldFlags.Attribute));
                }

            }

        }

        //---------------------------------------------------------------------------------------------------------------------

        public ComputingResourceStructure(IfyWebContext context, bool optionList) : this(context, typeof(Terradue.Portal.ComputingResource), optionList) {
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Processes a request on the service.</summary>
        public override void ProcessAdministrationRequest() {
            context.AdminMode = true;
            if (context.ResourcePathParts.Length >= 3 && context.ResourcePathParts[2] == "users") {
                GetAllowedAdministratorOperations();
                computingResource = ComputingResource.FromId(context, Id);
                computingResource.Load();
                if (!CanViewItem) context.ReturnError("You are not authorized to view this information");

                UserStructure user = new UserStructure(context, false);
                user.RelatedComputingResource = computingResource;
                user.SetOpenSearchDescription("Users", "Search users", "Search users by keyword or any of the specific fields defined in the OpenSearch URL.");
                //user.ShowItemLinks = true;

                if (context.ResourcePathParts.Length == 3) {
                    user.ProcessGenericRequest();                    
                } else {
                    int userId;
                    Int32.TryParse(context.ResourcePathParts[3], out userId);
                    user.ProcessGenericRequest(userId);
                }

            } else {
                base.ProcessAdministrationRequest();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override void OnBeforeDefine() {
            Fields["availability"].Value = "3";
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets default values to fields when writing into the database.</summary>
        protected override void OnAfterRead() {
            FieldExpression field;
            field = Fields["defaultServices"];
            if (field.ValueCount != 0) {
                defaultServiceIds = new int[field.ValueCount];
                for (int i = 0; i < field.ValueCount; i++) Int32.TryParse(field.Values[i], out defaultServiceIds[i]);
            } else {
                defaultServiceIds = null;
            }
            field.Flags |= FieldFlags.Ignore;

            field = Fields["services"];
            if (defaultServiceIds != null) {
                if (field.ValueCount == 0) {
                    field.Values = new string[0];
                } else {
                    for (int i = 0; i < defaultServiceIds.Length; i++) {
                        for (int j = 0; j < field.ValueCount; j++) if (field.Values[j] == defaultServiceIds[i].ToString()) field.Values[j] = null;
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnLoad() {
            computingResource = this.ComputingResource;

            Fields["availability"].AsInteger = computingResource.Availability;
            Fields["caption"].Value = computingResource.Name;
            Fields["description"].Value = computingResource.Description;
            Fields["domain"].AsInteger = computingResource.DomainId;
            Fields["hostname"].Value = computingResource.Hostname;
            Fields["capacity"].AsInteger = computingResource.TotalCapacity;
            Fields["iconUrl"].Value = computingResource.IconUrl;
            Fields["creditControl"].AsBoolean = computingResource.UserCreditControl;
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override Entity OnStore() {
            computingResource = this.ComputingResource;

            computingResource.Availability = Fields["availability"].AsInteger;
            computingResource.Name = Fields["caption"].Value;
            computingResource.Description = Fields["description"].Value;
            computingResource.DomainId = Fields["domain"].AsInteger;
            computingResource.Hostname = Fields["hostname"].Value;
            computingResource.TotalCapacity = Fields["capacity"].AsInteger;
            computingResource.IconUrl = Fields["iconUrl"].Value;
            computingResource.UserCreditControl = Fields["creditControl"].AsBoolean;

            return computingResource;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnItemProcessed(OperationType type, int itemId) {
            if (type != OperationType.Create && type != OperationType.Modify) return false;

            if (defaultServiceIds != null) {
                string rows = null;
                for (int i = 0; i < defaultServiceIds.Length; i++) {
                    if (defaultServiceIds[i] == 0) continue;
                    if (rows == null) rows = String.Empty; else rows += ", ";
                    rows += String.Format("({0}, {1}, true)", defaultServiceIds[i], itemId);
                }
                if (rows != null) context.Execute(String.Format("INSERT INTO service_cr (id_service, id_cr, is_default) VALUES {0};", rows));
            }
            return false;
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class GlobusComputingElementStructure : ComputingResourceStructure {

        private bool statusUrlRequired = false;
        private GlobusComputingElement computingResource;

        //---------------------------------------------------------------------------------------------------------------------

        public override ComputingResource ComputingResource {
            get { 
                if (computingResource == null) computingResource = (Id == 0 ? GlobusComputingElement.GetInstance(context) : ComputingResource.FromId(context, Id)) as GlobusComputingElement;
                return computingResource;
            }
        }

        public GlobusComputingElementStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.GlobusComputingElement), optionList) {
            if (OptionList) return;

            // TODO: re-enable wdirs, rdirs

            // Prepare multi-entity field for working directories
/*            MultipleEntityField wdirs = new MultipleEntityField(
                "wdirs", 
                Entity.GetInstance(context, new EntityData(
                "Working directories", 
                "cedir",
                new FieldExpression[] {
                new SingleValueField("path", "string", "Path", FieldFlags.Both | FieldFlags.Searchable),
                new SingleValueField("available", "bool", "Available", FieldFlags.Both),
                new FixedValueField("type", "dir_type", "W")
            }
            )),
                "id_ce",
                "Working directories", 
                FieldFlags.Item
                ); 
            wdirs.Condition = "t.dir_type='W'";

            // Prepare multi-entity field for result directories
            MultipleEntityField rdirs = new MultipleEntityField(
                "rdirs", 
                Entity.GetInstance(context, new EntityData(
                "Result directories", 
                "cedir",
                new FieldExpression[] {
                new SingleValueField("path", "string", "Path", FieldFlags.Both | FieldFlags.Searchable),
                new SingleValueField("available", "bool", "Available", FieldFlags.Both),
                new FixedValueField("type", "dir_type", "R")
            }
            )),
                "id_ce",
                "Result directories", 
                FieldFlags.Item
                ); 
            rdirs.Condition = "t.dir_type='R'";
*/

            FieldExpression wdirs = EmptyField;
            FieldExpression rdirs = EmptyField;
            List<FieldExpression> additionalFields = new List<FieldExpression>();
            additionalFields.Add(new SingleValueField("cePort", "ce_port", "int", "Port", null, FieldFlags.Item));
            additionalFields.Add(new SingleValueField("gsiPort", "gsi_port", "int", "GSI Port", null, FieldFlags.Item));
            additionalFields.Add(new SingleValueField("jobManager", "job_manager", "string", "Job manager", null, FieldFlags.Item));
            additionalFields.Add(new SingleValueField("flags", "flags", "string", "Flags", null, FieldFlags.Item | FieldFlags.Optional));
            additionalFields.Add(new SingleValueField("gridType", "grid_type", "string", "Grid type", null, FieldFlags.Item));
            additionalFields.Add(new SingleValueField("jobQueue", "job_queue", "string", "Job queue", null, FieldFlags.Item));
            additionalFields.Add(new SingleValueField("statusMethod", "status_method", "ceStatusRequest", "Status request method", null, FieldFlags.Item | FieldFlags.Lookup | FieldFlags.Custom));
            additionalFields.Add(new SingleValueField("statusUrl", "status_url", "url", "Status request URL", null, FieldFlags.Item | FieldFlags.Optional | FieldFlags.Custom));
            additionalFields.Add(wdirs);
            additionalFields.Add(rdirs);
            additionalFields.Add(new SingleReferenceField("lge", "lge", "@.name", "id_lge", "Managing LGE instance", null, FieldFlags.Item));
            AppendFields("ce", "id", additionalFields);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets default values to fields when writing into the database.</summary>
        /// <param name="field">the field in question</param>
        protected override void OnAfterRead() {
            base.OnAfterRead();
            statusUrlRequired = (Fields["statusMethod"].Value == "2");
            if (statusUrlRequired) {
                FieldExpression field = Fields["statusUrl"];
                field.Flags = field.Flags & FieldFlags.AllButOptional;
                if (field.Value == null) field.Invalid = true;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnLoad() {
            Fields["cePort"].AsInteger = computingResource.Port;
            Fields["gsiPort"].AsInteger = computingResource.GsiPort;
            Fields["jobManager"].Value = computingResource.JobManager;
            Fields["flags"].Value = computingResource.Flags;
            Fields["gridType"].Value = computingResource.GridType;
            Fields["jobQueue"].Value = computingResource.JobQueue;
            Fields["statusMethod"].AsInteger = computingResource.StatusMethod;
            Fields["statusUrl"].Value = computingResource.StatusUrl;
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override Entity OnStore() {
            computingResource.Port = Fields["cePort"].AsInteger;
            computingResource.GsiPort = Fields["gsiPort"].AsInteger;
            computingResource.JobManager = Fields["jobManager"].Value;
            computingResource.Flags = Fields["flags"].Value;
            computingResource.GridType = Fields["gridType"].Value;
            computingResource.JobQueue = Fields["jobQueue"].Value;
            computingResource.StatusMethod = Fields["statusMethod"].AsInteger;
            computingResource.StatusUrl = Fields["statusUrl"].Value;

            //wdir, rdir

            computingResource.LightGridEngineId = Fields["lge"].AsInteger;

            return computingResource;
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class UserStructure : EntityData {

        protected string affiliation;
        private string password;
        protected int enabledUsers = AccountStatusType.Enabled;
        private bool allowPassword, isWeakPassword, passwordSet, passwordUnset;
        private bool sendMail;
        private int computingResourceCredits;
        FieldExpression certField;

        //---------------------------------------------------------------------------------------------------------------------

        public bool Impersonation { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public ComputingResource RelatedComputingResource { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public UserStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.User), optionList) {
            if (ViewingState == ViewingState.ShowingList) {
                //if (multiple && xmlWriter.WriteState == WriteState.Element) xmlWriter.WriteAttributeString("multiple", "true");
                if (context.AdminMode) {
                    //if (CanCreate) AddOperation(Operation.Other, "define", null, "Create New", "_request=define", "GET");
                    if (CanModify) {
                        if (enabledUsers == AccountStatusType.Enabled) AddOperation(OperationType.Modify, "disable", "Disable", "POST", "_request=disable", null);
                        else AddOperation(OperationType.Modify, "enable", "Enable", "POST", "_request=enable", null);
                    }
                    //if (CanDelete) AddOperation("delete", null, "Delete", "_request=delete", "POST"); // multiple delete, must be POST
                }

            } else if (ViewingState == ViewingState.ShowingItem) {
                enabledUsers = context.GetQueryIntegerValue(String.Format("SELECT status FROM usr WHERE id={0};", Id));
                if (RelatedComputingResource != null) {
                    CanModify = false;
                    AddOperation(OperationType.Other, "set", "Set Credits", "POST", "_request=set", null);
                    AddOperation(OperationType.Other, "unset", "Remove Credits", "GET", "_request=unset", null);
                } else {
                    if (context.AdminMode) {
                        if (enabledUsers == AccountStatusType.Enabled) AddOperation(OperationType.Modify, "disable", "Disable", "GET", "_request=disable", null);
                        else AddOperation(OperationType.Modify, "enable", "Enable", "GET", "_request=enable", null);
                        AddOperation(OperationType.Modify, "impersonate", "Impersonate", false, "GET", String.Format(context.AdminRootUrl == null ? "/admin/impersonate.aspx?_request=start&id={0}" : "{0}/{1}/{2}/impersonate?_request=start", context.AdminRootUrl, EntityType.GetEntityType(typeof(Terradue.Portal.User)).Keyword, Id), false, null);
                        //if (enabledUsers != AccountStatusType.Enabled && CanDelete) AddOperation("delete", null, "Delete", "_request=delete", "GET");
                    }
                }
            }


            if (OptionList) {
                Fields.Add(new SingleValueField("value", "id", "int", "ID", null, FieldFlags.Both));
                Fields.Add(new SingleValueExpression("caption", "CONCAT(firstname, ' ', lastname)", "string", "Full Name", null, FieldFlags.Both | FieldFlags.SortAsc));

            } else if (Impersonation) {
                context.Privileges = PagePrivileges.AdminOnly;
                Fields.Add(new SingleValueExpression("link", String.Format("CONCAT('{0}?id=', id, '&{1}=start', {2})", context.ScriptName, IfyWebContext.OperationParameterName, (context.Format == null ? "''" : "'&" + IfyWebContext.FormatParameterName + "=" + context.Format.Replace("'", "''") + "'")), "Link", FieldFlags.Both | FieldFlags.Attribute));
                Fields.Add(new SingleValueField("caption", "username", "identifier", "Username", null, FieldFlags.Both | FieldFlags.Searchable));
                Fields.Add(new SingleValueExpression("fullname", "CONCAT(firstname, ' ', lastname)", "Last Name", FieldFlags.List | FieldFlags.Searchable));
                ShowItemLinks = false;
                CanCreate = false;
                CanModify = false;
                CanDelete = false;
                FilterCondition = (context.UserLevel == UserLevel.Administrator ? "t.id!=" + context.UserId : "false");

            } else if (RelatedComputingResource != null) { 
                //"User Credits for " + RelatedComputingResource.Name,
                Join = "usr AS t LEFT JOIN cr_priv AS t1 ON t.id=t1.id_usr AND t1.id_cr=" + RelatedComputingResource.Id;
                //Fields.Add(new SingleValueExpression("link", String.Format("CONCAT('{0}?id=', id, '&{1}=start', {2})", context.ScriptName, IfyWebContext.OperationParameterName, (context.Format == null ? "''" : "'&" + IfyWebContext.FormatParameterName + "=" + context.Format.Replace("'", "''") + "'")), "Link", FieldFlags.Both | FieldFlags.Attribute));
                Fields.Add(new SingleValueExpression("caption", "t.username", "identifier", "Username", null, FieldFlags.Both | FieldFlags.Searchable | FieldFlags.ReadOnly | FieldFlags.SortAsc));
                Fields.Add(new SingleValueExpression("fullname", "CONCAT(t.firstname, ' ', t.lastname)", "Full Name", FieldFlags.Both | FieldFlags.Searchable | FieldFlags.ReadOnly));
                Fields.Add(new SingleValueExpression("credits", "t1.credits", "int", "Credits", null, FieldFlags.Both | FieldFlags.Custom));
                CanCreate = false;
                CanDelete = false;

            } else {
                FieldExpression mailField;
                if (ViewingState == ViewingState.DefiningItem) {
                    mailField = new SingleValueExpression("sendMail", "true", "bool", "Send registration mail to user", null, FieldFlags.Item | FieldFlags.Custom);
                } else if (ViewingState == ViewingState.ShowingItem) {
                    mailField = new SingleValueExpression("sendMail", "false", "bool", "Send mail to user if password was changed", null, FieldFlags.Item | FieldFlags.Custom);
                } else {
                    mailField = new SingleValueExpression("sendMail", null, null, 0);
                }

                if (context.UserCertificateServerVariable == IfyContext.CertSubjectVariable) {
                    certField = new SingleValueField("certSubject", "cert_subject", "string", "Certificate Subject", null, FieldFlags.Item | FieldFlags.Optional);
                } else if (context.UserCertificateServerVariable != null) {
                    string caption = (context.UserCertificateServerVariable == IfyWebContext.CertPemContentVariable ? "Certificate Content in PEM Format" : "Certificate Content (" + context.UserCertificateServerVariable + ")");
                    certField = new SingleValueField("certContent", "cert_content", "text", caption, null, FieldFlags.Item | FieldFlags.Optional);
                } else {
                    certField = EntityData.EmptyField;
                }

                Fields.Add(new SingleValueExpression("caption", "CONCAT(firstname, ' ', lastname)", "string", "Full Name", null, FieldFlags.List | FieldFlags.SortAsc));
                Fields.Add(new SingleValueField("username", "identifier", "Username", FieldFlags.Both | FieldFlags.Id | FieldFlags.Searchable));
                Fields.Add(new SingleValueField("accountStatus", "status", "accountStatus", "Account Status", "ify:accountStatus", FieldFlags.Item | FieldFlags.Custom | FieldFlags.Lookup));
                Fields.Add(new SingleValueField("normal", "normal_account", "bool", "Normal Account (Apply General Account Rules)", null, FieldFlags.Item | FieldFlags.Custom));
                Fields.Add(new SingleValueField("level", "userLevel", "Level", FieldFlags.Item | FieldFlags.Lookup));
                Fields.Add(new SingleValueField("allowPassword", "allow_password", "bool", "Allow password authentication", null, FieldFlags.Item | FieldFlags.Custom));
                //Fields.Add(new SingleValueField("extAuth", "allow_ext_login", "bool", "Allow external authentication", null, FieldFlags.Item | FieldFlags.Optional));
                //Fields.Add(new SingleValueField("forceTrusted", "force_trusted", "bool", "Allow connections from trusted hosts only", null, FieldFlags.Item));
                Fields.Add(new SingleValueField("forceSsl", "force_ssl", "bool", "Require Client Certificate", null, FieldFlags.Item));
                Fields.Add(new SingleValueField("allowSessionless", "allow_sessionless", "bool", "Allow task scheduling and external sessionless requests for this user account", null, FieldFlags.Item | FieldFlags.Optional));
                Fields.Add(new SingleValueField("password", "password", "Password", FieldFlags.Item | FieldFlags.Custom));
                Fields.Add(new SingleValueField("lastname", "string", "Last Name", FieldFlags.Item | FieldFlags.Searchable));
                Fields.Add(new SingleValueField("firstname", "string", "First Name", FieldFlags.Item | FieldFlags.Searchable));
                Fields.Add(new SingleValueField("email", "email", "E-mail", FieldFlags.Item));
                Fields.Add(new SingleValueField("affiliation", "string", "Affiliation", FieldFlags.Item | FieldFlags.Optional | FieldFlags.Searchable));
                Fields.Add(new SingleValueField("country", "countries", "Country", FieldFlags.Item | FieldFlags.Optional | FieldFlags.Searchable));
                /*Fields.Add(new SingleValueField("language", "language", "language", "Language", null, FieldFlags.Item | FieldFlags.Optional | FieldFlags.Lookup));*/
                Fields.Add(new SingleValueField("timeZone", "time_zone", "timeZone", "Time zone", null, FieldFlags.Item | FieldFlags.Optional | FieldFlags.Lookup));
                Fields.Add(new SingleValueField("credits", "int", "Credits", FieldFlags.Item));
                Fields.Add(new SingleValueField("proxy_username", "string", "Proxy Username", FieldFlags.Item));
                Fields.Add(new SingleValueField("proxy_password", "password", "Proxy Password", FieldFlags.Item));
                Fields.Add(certField);
                Fields.Add(new SingleValueField("task_storage_period", "int", "Maximum Task Storage Period (days)", FieldFlags.Item | FieldFlags.Optional));
                Fields.Add(new SingleValueField("publish_folder_size", "int", "Publish Folder Size", FieldFlags.Item | FieldFlags.Optional));
                Fields.Add(new MultipleReferenceField("groups", "grp", "name", "usr_grp", "Groups", FieldFlags.Item | FieldFlags.Custom));
                Fields.Add(mailField);
                Fields.Add(new SortField("sort", "ify:sort"));
                //filterCondition = "t.status=" + enabledUsers.ToString().ToLower();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Processes a request on the configuration.</summary>
        public override void ProcessAdministrationRequest() {
            context.AdminMode = true;
            if (context.ResourcePathParts.Length >= 3 && context.ResourcePathParts[2] == "impersonate") {
                switch (context.RequestedOperation) {
                    case "start" :
                        context.StartImpersonation(Id);
                        context.WriteInfo("This is the user account of " + context.UserCaption + " (" + context.Username + ")");
                        break;
                    case "end" :
                        context.EndImpersonation();
                        context.WriteInfo("This is your own user account (" + context.Username + ")");
                        break;
                        default :
                        context.ReturnError("Invalid request");
                        break;
                }
            } else {
                base.ProcessAdministrationRequest();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool ProcessSpecificRequest(string operationName) {
            if (Impersonation) {
                switch (operationName) {
                    case "start" :
                        context.Privileges = PagePrivileges.AdminOnly;
                        context.StartImpersonation(Id);
                        context.WriteInfo("This is the user account of " + context.UserCaption + " (" + context.Username + ")");
                        return true;
                    case "end" :
                        context.EndImpersonation();
                        context.WriteInfo("This is your own user account (" + context.Username + ")");
                        return true;
                        default :
                        Id = 0;
                        break;
                }
            } else if (RelatedComputingResource != null) {
                switch (operationName) {
                    case "set" :
                        GetValuesFromRequest(null, null);
                        context.Execute(String.Format("DELETE FROM cr_priv WHERE id_cr={1} AND id_usr={0};", Id, RelatedComputingResource.Id));
                        context.Execute(String.Format("INSERT INTO cr_priv (id_cr, id_usr, credits) VALUES ({1}, {0}, {2});", Id, RelatedComputingResource.Id, computingResourceCredits));
                        return false;
                    case "unset" :
                        context.Execute(String.Format("DELETE FROM cr_priv WHERE id_cr={1} AND id_usr={0};", Id, RelatedComputingResource.Id));
                        return false;
                }
            } else {
                switch (operationName) {
                    case "disable" :
                    case "enable" :
                        if (Id == 0) GetIdsFromRequest("id");
                        else Ids = new int[] {Id};
                        if (Ids != null && Ids.Length != 0) User.SetAccountStatus(context, Ids, operationName == "enable" ? AccountStatusType.Enabled : AccountStatusType.Disabled);
                        //if (!newEnabledState) Id = 0;        
                        return false;
                }
            }
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override string GetSpecificCondition(FieldExpression field, string tableAlias, string value) {
            switch (field.SearchExtension) {
                case "ify:accountStatus" :
                    if (Id != 0) break;
                    if (value == null) return null; // value = AccountStatusType.Enabled.ToString();
                    Int32.TryParse(value, out enabledUsers);
                    return field.Expression.Replace("t.", tableAlias + ".") + "=" + enabledUsers;
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override void OnBeforeDefine() {
            allowPassword = (context.AllowPassword != RuleApplicationType.Never);

            Fields["status"].Value = AccountStatusType.Enabled.ToString();
            Fields["normal"].Value = "true";
            Fields["allowPassword"].Value = allowPassword.ToString().ToLower();

            List<string> values = new List<string>();
            MultipleReferenceField field1;
            if ((field1 = Fields["groups"] as MultipleReferenceField) != null && field1.ReferenceTable == "grp") {
                string sql = String.Format("SELECT {0}, {1} AS _caption FROM {2} WHERE is_default=true ORDER BY _caption;", field1.ReferenceIdField, field1.ReferenceValueExpr, field1.ReferenceTable);
                IDataReader reader = context.GetQueryResult(sql);
                while (reader.Read()) values.Add(reader.GetValue(1).ToString());
                reader.Close();
                field1.Values = values.ToArray();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override void OnAfterRead() {
            FieldExpression field;
            allowPassword = Fields["allowPassword"].AsBoolean;
            field = Fields["password"];
            if (!allowPassword) field.Flags |= FieldFlags.Optional;
            password = field.Value;
            if (allowPassword) {
                if (password == null) {
                    if (Id == 0 || context.GetQueryBooleanValue(String.Format("SELECT password IS NULL FROM usr WHERE id={0};", Id))) field.Invalid = true;
                    else field.Flags |= FieldFlags.Ignore;
                } else if (!IfyWebContext.passwordAuthenticationType.ForceStrongPasswords || IfyWebContext.IsStrongPassword(password)) {
                    field.SqlValue = (password == null ? null : "PASSWORD(" + StringUtils.EscapeSql(password) + ")");
                    passwordSet = true;
                } else {
                    isWeakPassword = true;
                    field.Invalid = true;
                }
            } else {
                field.SqlValue = "NULL";
                passwordUnset = true;
            }
            field = Fields["certContent"];
            field.SqlValue = (field.Value == null ? "NULL" : StringUtils.EscapeSql(Regex.Replace(field.Value, @"[\r\n]", " ")));
            sendMail = Fields["sendMail"].AsBoolean;
            if (Fields["credits"].Value == null) computingResourceCredits = 0; else Int32.TryParse(field.Value, out computingResourceCredits);
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnItemProcessed(OperationType type, int itemId) {
            context.RefreshUser(itemId);

            User user = User.FromId(context, itemId);

            if (type == OperationType.Delete) {
                if (itemId == 0) context.AddInfo("The user account(s) have been deleted");
                else context.AddInfo("The user account has been deleted");
                return true;
            } 
            if (Fields["accountStatus"].AsInteger == AccountStatusType.Enabled) user.EnableAccount();
            bool result = false;

            if (sendMail) {
                if (type == OperationType.Create) {
                    user.SendMail(UserMailType.Registration, false);
                    user.AccountStatus = AccountStatusType.Enabled;
                    context.AddInfo("The user account has been created and a registration mail has been sent to " + user.Email);
                    result = true;
                } else if (type == OperationType.Modify) {
                    if (passwordSet) {
                        user.SendMail(UserMailType.PasswordReset, false);
                        user.AccountStatus = AccountStatusType.PasswordReset;
                        context.AddInfo("The user account has been modified and a password reset mail has been sent to " + user.Email);
                    } else { 
                        context.AddWarning("The user account has been modified, but no mail was sent because the password has not been changed");
                    }
                    result = true;
                }
            }
            user.Store();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnItemNotProcessed(OperationType type, int itemId) {
            if (isWeakPassword) {
                context.AddError("The password must contain at least 8 characters, of which at least one upper-case character, one lower-case character, one digit and one non-alphanumeric character");
                return true;
            }

            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool HandleConstraintError() {
            string username = null;
            username = Fields["username"].Value;
            context.ReturnError(String.Format("The user account \"{0}\" already exists", username), "uniqueConstraint");
            return false;
        }
    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class UserProfileStructure : EntityData {

        //---------------------------------------------------------------------------------------------------------------------

        bool signInForm, allowPassword, passwordSet, passwordUnset, isWeakPassword, sendMail;
        string password;
        User user;

        //---------------------------------------------------------------------------------------------------------------------

        public UserProfileStructure(IfyWebContext context, bool optionList) : this(context, optionList, false) {
        }

        //---------------------------------------------------------------------------------------------------------------------

        public UserProfileStructure(IfyWebContext context, bool optionList, bool signInForm) : base(context, typeof(Terradue.Portal.User), optionList) {
            this.signInForm = signInForm;

            if (signInForm) AddOperation(OperationType.Other, "signin", "Sign in", "POST", "_request=signin", null);
            allowPassword = (context.AllowPassword != RuleApplicationType.Never);
            if (allowPassword && context.IsUserAuthenticated) allowPassword = context.GetQueryBooleanValue(String.Format("SELECT allow_password FROM usr WHERE id={0};", Id));

            //context.IsUserAuthenticated ? "My Profile" : "Registration",
            Fields.Add(GetUsernameFieldExpression());
            Fields.Add(GetUserFieldExpression("lastname", "string", "Last Name", 0));
            Fields.Add(GetUserFieldExpression("firstname", "string", "First Name", 0));
            if (allowPassword) Fields.Add(new SingleValueField("password", "password", "Password", FieldFlags.Item | FieldFlags.Custom));
            Fields.Add(GetUserFieldExpression("email", "email", "E-mail", 0));
            Fields.Add(GetUserFieldExpression("affiliation", "string", "Affiliation", FieldFlags.Optional));
            Fields.Add(GetUserFieldExpression("country", "countries", "Country", FieldFlags.Optional));
            //Fields.Add(new SingleValueField("language", "language", "language", "Language", null, FieldFlags.Item | FieldFlags.Optional | FieldFlags.Lookup));
            Fields.Add(new SingleValueField("timeZone", "time_zone", "timeZone", "Time zone", null, FieldFlags.Item | FieldFlags.Lookup | (Array.IndexOf(context.DisabledProfileFields, "timeZone") == -1 ? 0 : FieldFlags.ReadOnly)));
            Fields.Add(new SingleValueField("debugLevel", "debug_level", "debugLevel", "Debug Information", null, (context.UserLevel >= UserLevel.Developer ? FieldFlags.Item | FieldFlags.Optional | FieldFlags.Lookup : 0)));
            //Fields.Add(new SingleValueField("simpleGui", "simple_gui", "bool", "Use simplified user interface", null, FieldFlags.Item));
            if (!context.IsUserAuthenticated) {
                if (context.ExternalUsername != null) {
                    Fields.Add(new FixedValueField("username", "username", context.ExternalUsername));
                }
                foreach (string disabledField in context.DisabledProfileFields) {
                    if (context.GetUserField(disabledField) != null) {
                        Fields.Add(new FixedValueField(disabledField, disabledField, context.GetUserField(disabledField)));
                    }
                }
            }
            CanDelete = false;
            ShowItemIds = false;

            CanViewList = false;
            CanModify = context.IsUserAuthenticated;
            CanCreate = !context.IsUserAuthenticated && context.AllowSelfRegistration || context.IsUserIdentified;
            CanDelete = false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool ProcessSpecificRequest(string operationName) {
            TokenAuthenticationType tokenAuth;
            switch (operationName) {
                case "signin" :
                case "login" :
                    if (context.GetParamValue("username") != null && context.GetParamValue("password") != null) { // (3)
                        if (IfyWebContext.passwordAuthenticationType.AuthenticateUser(context, context.GetParamValue("username"), context.GetParamValue("password")) != null) {
                            context.CheckAvailability();
                            context.WriteInfo("You are now logged in");
                        }
                    } else { // (4)
                        //UseOpenId = context.GetParamValue("form") == "openid";
                        ShowSignInForm();
                    }

                    return true;

                case "signout" :
                case "logout" :
                    context.LogoutUser();
                    context.WriteInfo("You are now logged out", "userLogOut");
                    return true;

                case "activate":
                    tokenAuth = IfyWebContext.GetAuthenticationType(typeof(TokenAuthenticationType)) as TokenAuthenticationType;
                    if (tokenAuth == null) throw new UnauthorizedAccessException("Cannot authenticate with activation key");
                    tokenAuth.AuthenticateUser(context, context.GetParamValue("key"));
                    context.Redirect(context.AccountRootUrl == null ? "/account/" : context.AccountRootUrl);
                    return false;

                case "recover" :
                    string token = context.GetParamValue("key");
                    if (token == null) {
                        RecoverPassword();
                        return true;
                    } else {
                        tokenAuth = IfyWebContext.GetAuthenticationType(typeof(TokenAuthenticationType)) as TokenAuthenticationType;
                        if (tokenAuth == null) throw new UnauthorizedAccessException("Cannot authenticate with activation key");
                        tokenAuth.AuthenticateUser(context, token);
                        context.AddInfo("Please enter a new password now");
                        return false;
                    }
                    default :
                    if (context.IsUserAuthenticated) {
                        if (context.UserInformation != null && context.UserInformation.PasswordExpired && operationName != "modify") context.AddInfo("Your password has expired, please enter a new password now");
                    } else {
                        RequestedOperation = new EntityOperation(context, OperationType.Define, null, null, false, null, null);
                    }
                    break;
            }
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void ShowSignInForm() {
            // "Sign in"
            Fields.Add(new SingleValueField("username", "string", "Username", FieldFlags.Item));
            Fields.Add(new SingleValueField("password", "password", "Password", FieldFlags.Item));

            CanCreate = false;
            CanModify = false;
            CanDelete = false;

            WriteSingleItem();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void RecoverPassword() {
            if (context.IsUserAuthenticated) {
                context.AddWarning("You are already logged in");
                WriteSingleItem();
                return;
            }
            //"Password Reset"
            Fields.Add(new SingleValueField("username", "identifier", "Username", FieldFlags.Item | FieldFlags.Optional));
            Fields.Add(new SingleValueField("email", "email", "E-mail", FieldFlags.Item | FieldFlags.Optional));
            AddOperation(OperationType.Other, "recover", "Recover My Account", "POST", "_request=recover&send=true", null);

            bool formReceived = (context.GetParamValue("send") == "true");
            if (formReceived) {
                GetValuesFromRequest(null, null);
                User user = User.GetInstance(context);
                user.Username = Fields["username"].Value;
                user.Email = Fields["email"].Value;
                xmlWriter = context.StartXmlResponse();
                if (user.Username == null && user.Email == null) {
                    context.AddError(String.Format("You have to specify either a username or an e-mail address", user.Username));
                    WriteSingleItem();
                    return;
                }

                string condition = String.Empty;
                if (user.Username != null) condition = "username=" + StringUtils.EscapeSql(user.Username);
                if (user.Email != null) condition += (condition == String.Empty ? String.Empty : " OR ") + "email=" + StringUtils.EscapeSql(user.Email);

                IDataReader reader = context.GetQueryResult(String.Format("SELECT id, email FROM usr WHERE {0};", condition));

                List<int> userIds = new List<int>();
                while (reader.Read()) {
                    userIds.Add(reader.GetInt32(0));
                    if (user.Email == null) user.Email = context.GetValue(reader, 1);
                }
                reader.Close();

                if (user.Email == null) context.ReturnError("No matching e-mail address found");

                foreach (int userId in userIds) {
                    Id = userId;
                    user.SendMail(UserMailType.PasswordReset, false);
                    user.AccountStatus = AccountStatusType.PasswordReset;
                    user.Store();
                }
                context.AddInfo("We received your request and will soon send you an e-mail with details on how to recover access to your account");
                Id = 0;
                userIds.Clear();

                xmlWriter.WriteStartElement("singleItem");
                xmlWriter.WriteAttributeString("link", "/");
                xmlWriter.WriteEndElement();

            } else {
                WriteSingleItem();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        private FieldExpression GetUsernameFieldExpression() {
            if (!context.IsUserAuthenticated && context.ExternalUsername != null) return new SingleValueExpression("username_disabled", StringUtils.EscapeSql(context.ExternalUsername), "identifier", "Username for your account", null, FieldFlags.Item | FieldFlags.ReadOnly | FieldFlags.Unique | FieldFlags.Custom);
            return new SingleValueField("username", "identifier", context.IsUserAuthenticated ? "Username" : "Username for your account", context.IsUserAuthenticated ? FieldFlags.Item | FieldFlags.ReadOnly : FieldFlags.Item | FieldFlags.Unique | FieldFlags.Custom); 
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override void OnBeforeDefine() {
            if (context.IsUserAuthenticated) return;
            foreach (FieldExpression field in Fields) {
                if (field.Name == "username_disabled") {
                    field.Value = context.ExternalUsername;
                } else if (field.Name.EndsWith("_disabled")) {
                    string disabledField = field.Name.Substring(0, field.Name.Length - 9);
                    field.Value = context.GetUserField(disabledField);
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override void OnAfterRead() {
            FieldExpression field;
            field = Fields["password"];
            if (!allowPassword) field.Flags |= FieldFlags.Optional;
            password = field.Value;
            if (allowPassword) {
                if (password == null) {
                    if (Id == 0 || context.GetQueryBooleanValue(String.Format("SELECT password IS NULL FROM usr WHERE id={0};", Id))) field.Invalid = true;
                    else field.Flags |= FieldFlags.Ignore;
                } else if (!IfyWebContext.passwordAuthenticationType.ForceStrongPasswords || IfyWebContext.IsStrongPassword(password)) {
                    field.SqlValue = (password == null ? null : "password(" + StringUtils.EscapeSql(password) + ")");
                    passwordSet = true;
                } else {
                    isWeakPassword = true;
                    field.Invalid = true;
                }
            } else {
                field.SqlValue = "NULL";
                passwordUnset = true;
            }
            sendMail = Fields["sendMail"].AsBoolean;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnItemProcessed(OperationType type, int itemId) {
            context.RefreshUser(itemId);

            user = User.FromId(context, itemId);

            if (passwordSet) {
                user.LastPasswordChangeTime = context.Now;
                context.RefreshUser(itemId);
                //if (context.PasswordExpired) context.PasswordExpired = false;
            } else if (passwordUnset) {
                user.LastPasswordChangeTime = DateTime.MaxValue;
            }
            bool result = false;
            if (type == OperationType.Create) {
                user.AccountStatus = AccountStatusType.Disabled;
                context.Execute(String.Format("INSERT INTO usr_grp (id_usr, id_grp) SELECT {0}, t.id FROM grp AS t WHERE is_default=true;", itemId));
                if (context.IsUserIdentified || context.AccountActivationRule == AccountActivationRuleType.ActiveImmediately) {
                    user.AccountStatus = AccountStatusType.Enabled;
                    user.SendMail(UserMailType.Registration, true);
                    context.AddInfo("Your user account has been created and you can start using it now. A confirmation e-mail with account details has been sent to " + user.Email);
                } else if (context.AccountActivationRule == AccountActivationRuleType.ActiveAfterMail) {
                    user.AccountStatus = AccountStatusType.PendingActivation;
                    user.SendMail(UserMailType.Registration, false);
                    context.AddInfo("Your user account has been created and an e-mail with account activation details has been sent to " + user.Email);
                } else {
                    user.AccountStatus = AccountStatusType.PendingActivation;
                    context.AddInfo("We received your request for the creation of a user account and will soon send you an e-mail with activation details");
                }
                result = true;
            }
            user.Store();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnItemNotProcessed(OperationType type, int itemId) {
            if (isWeakPassword) {
                context.AddError("The password must contain at least 8 characters, of which at least one upper-case character, one lower-case character, one digit and one non-alphanumeric character");
                return true;
            }

            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        private FieldExpression GetUserFieldExpression(string name, string type, string caption, FieldFlags flags) {
            flags |= FieldFlags.Item;
            if (Array.IndexOf(context.DisabledProfileFields, name) != -1) {
                flags |= FieldFlags.ReadOnly;
                if (!context.IsUserAuthenticated && context.GetUserField(name) != null) return new SingleValueExpression(name + "_disabled", StringUtils.EscapeSql(context.GetUserField(name)), type, caption, null, flags | FieldFlags.Custom); 
            }
            return new SingleValueField(name, type, caption, flags);
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class FilterStructure : EntityData {

        private string token;
        private string url;
        private EntityType entityType;
        public int EntityTypeId { get; set; }

        public FilterStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Filter), optionList) {
            Fields.Add(new FixedValueField("user", "id_usr", "int", context.UserId.ToString(), FieldFlags.Item | FieldFlags.Unique));
            Fields.Add(new FixedValueField("entity", "id_type", EntityTypeId.ToString()));
            Fields.Add(new SingleValueField("token", "token", null, null, null, FieldFlags.Both | FieldFlags.Attribute | FieldFlags.Optional | FieldFlags.Custom));
            Fields.Add(new SingleValueField("caption", "name", null, null, null, EntityTypeId == 0 ? FieldFlags.Item | FieldFlags.Optional : FieldFlags.Item | FieldFlags.Unique));
            Fields.Add(new SingleValueField("link", "definition", null, null, null, FieldFlags.Both | FieldFlags.Attribute | FieldFlags.Custom));
            Fields.Add(new SingleValueField("url", "url", null, null, null, FieldFlags.Both | FieldFlags.Custom | (Id == 0 ? FieldFlags.Hidden : 0)));
            Fields.Add(new SingleValueField("text", "name", null, null, null, FieldFlags.List));
            ShowItemIds = false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ProcessRequest() {
            token = context.GetParamValue("token");
            url = context.GetParamGetValue("url");

            if (entityType == null) {
                string codeStr = context.GetParamGetValue("e");
                int code = 0;
                if (codeStr != null) Int32.TryParse(codeStr, out code); 
                EntityTypeId = code;
            }


            OptionList = true;

            bool list = false;
            xmlWriter = context.StartXmlResponse();

            string operation = context.RequestedOperation; 
            switch (operation) {
                case "create" :
                    xmlWriter.WriteStartElement("filter");
                    token = null;
                    try {
                        context.StartTransaction();
                        Id = CreateItem();
                        context.Commit();
                    } catch (Exception) {
                        return;
                    }
                    break;
                case "modify" :
                    xmlWriter.WriteStartElement("filter");
                    GetId(true, EntityTypeId != 0);
                    try {
                        context.StartTransaction();
                        ModifyItem();
                        context.Commit();
                    } catch (Exception) {
                        return;
                    }
                    break;
                case "delete" :
                    list = true;
                    xmlWriter.WriteStartElement("filters");
                    GetId(true, EntityTypeId != 0);
                    if (Id == 0) {
                        GetIdsFromRequest("id");
                        if (Ids.Length != 0) DeleteItems(Ids);
                    } else {
                        DeleteItem(Id);
                    }
                    break;
                default :
                    GetId(false, true);
                    list = (Id == 0);
                    xmlWriter.WriteStartElement(list ? "filters" : "filter");
                    break;

            }

            //Data = null;

            if (list) {
                if (EntityTypeId == 0) WriteItemList();
            } else {
                Filter filter = Filter.FromId(context, Id);
                filter.Load();
                if (xmlWriter.WriteState == WriteState.Element) {
                    xmlWriter.WriteAttributeString("token", token);
                    xmlWriter.WriteAttributeString("link", EntityTypeId == 0 ? filter.Url : (filter.Url == null ? String.Empty : filter.Url + "?") + (filter.Definition == null ? String.Empty : filter.Definition.Replace("%", "%25").Replace("&", "%26").Replace("\t", "&")));
                }
                xmlWriter.WriteString(filter.Name);
                //WriteSingleItem();
            }

            xmlWriter.WriteEndElement(); // </filters> or </filter>
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>!!!</summary>
        protected override void OnBeforeWrite() {
            Fields["link"].Value = (Fields["link"].Value == null ? String.Empty : Fields["link"].Value.Replace("%", "%25").Replace("&", "%26").Replace("\t", "&"));
            if (EntityTypeId != 0) {
                if (String.IsNullOrEmpty(url)) url = Fields["url"].Value;
                if (!String.IsNullOrEmpty(url)) Fields["link"].Value = url + "?" + Fields["link"].Value; 
                Fields["url"].Value = null;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets default values to fields when writing into the database.</summary>
        protected override void OnAfterRead() {
            FieldExpression field;
            if (token == null) {
                field = Fields["token"];
                token = Guid.NewGuid().ToString();
                field.SqlValue = StringUtils.EscapeSql(token);
            }
            field = Fields["link"];
            if (field.Value != null) {
                if (EntityTypeId == 0) {
                    url = field.Value;
                    field.SqlValue = "NULL";
                    field.Flags |= FieldFlags.Ignore;
                } else {
                    int pos = field.Value.IndexOf('?');
                    url = (pos == -1 ? field.Value : field.Value.Substring(0, pos));
                    field.SqlValue = (pos == -1 ? "NULL" : StringUtils.EscapeSql(HttpUtility.UrlDecode(field.Value.Substring(pos + 1)).Replace("&", "\t")));
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool HandleConstraintError() {
            string name = Fields["caption"].Value;
            context.ReturnError(String.Format("A filter named \"{0}\" already exists", name), "uniqueConstraint");
            return false;
        }


        //---------------------------------------------------------------------------------------------------------------------

        public void GetId(bool restrict, bool useCode) {
            if (token == null) {
                if (restrict) context.ReturnError("No token specified", "invalidToken");
                return;
            }
            Id = context.GetQueryIntegerValue(
                String.Format("SELECT id FROM filter WHERE token={0}{1}{2};",
                          StringUtils.EscapeSql(token),
                          restrict ? " AND id_usr=" + context.UserId : String.Empty,
                          useCode ? " AND id_basetype=" + EntityTypeId : String.Empty
                          )
                );
            if (Id == 0) context.ReturnError("Invalid token", "invalidToken");
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class LookupListStructure : EntityData {

        LookupList lookupList;

        protected bool Defining { get; set; }

        protected bool Error { get; set; }

        RequestParameter nameParameter, maxLengthParameter, sortParameter, valuesParameter;

        public LookupListStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.LookupList), optionList) {
            if (OptionList) {
                Fields.Add(new SingleValueField("value", "id", "int", "ID", null, FieldFlags.Both));
                Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.Both));
            } else {
                Fields.Add(new SingleValueField("caption", "name", "caption", "Name", null, FieldFlags.List | FieldFlags.Searchable | FieldFlags.Unique));
            }
            FilterCondition = "NOT t.system";
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Processes a request on the configuration.</summary>
        public override void ProcessAdministrationRequest() {
            context.AdminMode = true;
            ProcessRequest();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public void ProcessRequest() {
            string operationStr = context.RequestedOperation;

            if (Id == 0 && operationStr == null || operationStr == "delete") {
                ProcessGenericRequest();
                return;
            }
            
            if (operationStr == "delete") {
                DeleteItem(Id);
                return;
            }
            
            GetOperation();
            Defining = (RequestedOperation != null && RequestedOperation.Type == OperationType.Define);

            xmlWriter = context.StartXmlResponse();
            xmlWriter.WriteStartElement("singleItem");
            xmlWriter.WriteAttributeString("entity", "LookupList");
            if (Id != 0) xmlWriter.WriteAttributeString("link", String.Format(context.AdminRootUrl == null ? "/admin/lookuplist.aspx?id={0}" : "{1}/{2}/{0}", Id, context.AdminRootUrl, EntityType.GetEntityType(typeof(Terradue.Portal.User)).Keyword));
            
            CheckValidity();
    
            if (!Error && (operationStr == "create" || operationStr == "modify")) {
                lookupList.Store();
                context.Redirect(String.Format("{0}?id={1}&_state=created", context.ScriptUrl, Id), true, true);
                //Load("t.id=" + Id);
            }

            xmlWriter.WriteStartElement("item");
            
            // Call overridden method in subclass to add specific attributes to item
            if (Id != 0) xmlWriter.WriteAttributeString("id", Id.ToString());
            
            xmlWriter.WriteStartElement(nameParameter.Name);
            if (!nameParameter.AllValid) xmlWriter.WriteAttributeString("valid", "false");
            xmlWriter.WriteString(nameParameter.Value);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement(maxLengthParameter.Name);
            if (!maxLengthParameter.AllValid) xmlWriter.WriteAttributeString("valid", "false");
            if (!maxLengthParameter.AllValid || maxLengthParameter.AsInteger != 0) xmlWriter.WriteString(maxLengthParameter.Value);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement(sortParameter.Name);
            if (!sortParameter.AllValid) xmlWriter.WriteAttributeString("valid", "false");
            xmlWriter.WriteString(sortParameter.Value);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement(valuesParameter.Name);
            if (!valuesParameter.AllValid) xmlWriter.WriteAttributeString("valid", "false");
            xmlWriter.WriteString(valuesParameter.Value);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement(); // </item>
            
            xmlWriter.WriteEndElement(); // </singleItem>
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected void CheckValidity() {
            Error = false;
            
            xmlWriter.WriteStartElement("operations");
            WriteOperations();
            xmlWriter.WriteEndElement(); // </operations>                  

            xmlWriter.WriteStartElement("fields");

            if (Id == 0) lookupList = new LookupList(context);
            else lookupList = LookupList.FromId(context, Id);

            nameParameter = new RequestParameter(context, null, "name", "identifier", "List name", lookupList.Name);
            nameParameter.ReadOnly = (Id != 0);
            //nameParameter.Level = RequestParameterLevel.Custom;
            if (Id == 0) nameParameter.GetValues(ValueUpdateMethod.NoNullReplace); // !!! correct RequestParameter class, property ReadOnly must be checked in GetValues()
            nameParameter.Check(xmlWriter, true);
            if (nameParameter.AllValid) lookupList.Name = nameParameter.Value; else Error = true;
            
            maxLengthParameter = new RequestParameter(context, null, "maxLength", "int", "Maximum length of values in list", lookupList.MaxLength.ToString());
            //maxLengthParameter.Level = RequestParameterLevel.Custom;
            maxLengthParameter.AllowEmpty = true;
            maxLengthParameter.GetValues(ValueUpdateMethod.NoNullReplace);
            maxLengthParameter.Check(xmlWriter, true);
            if (maxLengthParameter.AllValid) lookupList.MaxLength = maxLengthParameter.AsInteger; else Error = true;

            sortParameter = new RequestParameter(context, null, "sort", "bool", "Order by caption alphabetically", lookupList.Sort.ToString().ToLower());
            //sortParameter.Level = RequestParameterLevel.Custom;
            sortParameter.GetValues(ValueUpdateMethod.NoNullReplace);
            sortParameter.Check(xmlWriter, true);
            if (sortParameter.AllValid) lookupList.Sort = sortParameter.AsBoolean; else Error = true;

            valuesParameter = new RequestParameter(context, null, "values", "text", "List values (caption = value)", lookupList.Values);
            //valuesParameter.Level = RequestParameterLevel.Custom;
            valuesParameter.AllowEmpty = true;
            valuesParameter.GetValues(ValueUpdateMethod.NoNullReplace);
            valuesParameter.Check(xmlWriter, true);
            if (valuesParameter.Value != null) {
                StringReader valueReader = new StringReader(valuesParameter.Value);
    
                string pair;
                int line = 0, errorCount = 0;
                
                while ((pair = valueReader.ReadLine()) != null) {
                    line++;
                    int pos = pair.IndexOf('=');
                    
                    if (pos == -1) {
                        valuesParameter.Invalidate(String.Format("Invalid data in line {0}: no \"=\" character", line));  
                        errorCount++;
                        continue;
                    }
                    
                    string caption = pair.Substring(0, pos).Trim();
                    string value = pair.Substring(pos + 1).Trim();
                    
                    if (caption.Length == 0) {
                        if (errorCount == 0) valuesParameter.Invalidate(String.Format("Invalid data in line {0}: missing caption", line));
                        errorCount++;
                    } else if (value.Length == 0) {
                        if (errorCount == 0) valuesParameter.Invalidate(String.Format("Invalid data in line {0}: missing value after \"=\"", line));
                        errorCount++;
                    } else if (caption.Length > 50) { // !!! fixed value
                        if (errorCount == 0) valuesParameter.Invalidate(String.Format("Invalid data in line {0}: caption too long", line));  
                        errorCount++;
                    } else if (lookupList.MaxLength != 0 && value.Length > lookupList.MaxLength) {
                        if (errorCount == 0) valuesParameter.Invalidate(String.Format("Invalid data in line {0}: value too long", line));  
                        errorCount++;
                    } 
                }
            }
            
            if (valuesParameter.AllValid) {
                lookupList.Values = valuesParameter.Value;
                if (Error && !Defining) context.AddError("Not all fields have correct values");
            } else {
                Error = true;
                context.AddError(valuesParameter.Message);
            }

            xmlWriter.WriteEndElement(); // </fields>
        }
        
    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class PublishServerStructure : EntityData {

        private string protocol;
        private bool usePassword, hasPassword;
        private bool usernameRequired, missingPasswordOrPublicKey;

        public PublishServerStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.PublishServer), optionList) {
            if (OptionList) {
                Fields.Add(new SingleValueField("value", "id", "int", "ID", null, FieldFlags.List | FieldFlags.Id));
                Fields.Add(new SingleValueField("caption", "name", "caption", "Caption", null, FieldFlags.List | FieldFlags.Unique | FieldFlags.SortAsc));
                Fields.Add(new SingleValueExpression("default", "CASE WHEN t.is_default THEN CASE WHEN t.id_usr IS NULL THEN 1 ELSE 2 END ELSE 0 END", "bool", "ID", null, FieldFlags.List));

            } else {
                if (context.AdminMode) {
                    SingleReferenceField userReferenceField = new SingleReferenceField("user", "usr", "CONCAT(@.firstname, ' ', @.lastname)", "id_usr", "User", "ify:user", FieldFlags.Both | FieldFlags.Optional | FieldFlags.SortAsc);
                    userReferenceField.NullCaption = "[shared among all users]";
                    Fields.Add(userReferenceField);
                }
                Fields.Add(new SingleValueField("caption", "name", "caption", "Name", null, FieldFlags.Both | FieldFlags.Searchable | FieldFlags.SortAsc));
                Fields.Add(new SingleValueField("protocol", "protocol", "Protocol", FieldFlags.Item | FieldFlags.Lookup | FieldFlags.Custom));
                Fields.Add(new SingleValueField("hostname", "string", "Hostname", FieldFlags.Item));
                Fields.Add(new SingleValueField("port", "int", "Port", FieldFlags.Item | FieldFlags.Optional));
                Fields.Add(new SingleValueField("path", "string", "Path", FieldFlags.Item));
                Fields.Add(new SingleValueField("usernamex", "username", "identifier", "Connection username", null, FieldFlags.Item | FieldFlags.Optional | FieldFlags.Custom));
                Fields.Add(new SingleValueExpression("usePassword", "t.password IS NOT NULL", "bool", "Use password", null, FieldFlags.Item | FieldFlags.Custom));
                Fields.Add(new SingleValueField("passwordx", "password", "password", "Connection password", null, FieldFlags.Item | FieldFlags.Optional | FieldFlags.Custom));
                Fields.Add(new SingleValueField("publicKey", "public_key", "string", "Public key subject", null, FieldFlags.Item | FieldFlags.Optional | FieldFlags.Custom));
                Fields.Add(new SingleValueField("options", "string", "Options", FieldFlags.Item | FieldFlags.Optional));
                //Fields.Add(new SingleValueField("uploadUrl", "upload_url", "url", "Upload URL", null, FieldFlags.Item | FieldFlags.ReadOnly));
                Fields.Add(new SingleValueField("downloadUrl", "download_url", "url", "Download URL", null, FieldFlags.Item | FieldFlags.Optional));
                Fields.Add(new SingleValueField("fileRoot", "file_root", "string", "Local file system folder", null, FieldFlags.Item | FieldFlags.Optional));
                Fields.Add(new SingleValueField("default", "is_default", "bool", "Selected by default", null, FieldFlags.Item | FieldFlags.Custom));
                if (context.AdminMode) Fields.Add(new SingleValueField("metadata", "metadata", "bool", "Use for task result metadata", null, FieldFlags.Item | FieldFlags.Custom));
                Fields.Add(new SingleValueField("deleteFiles", "delete_files", "bool", "Delete task result files when task is deleted", null, FieldFlags.Item));
                if (context.AdminMode) Fields.Add(new MultipleReferenceField("groups", "grp", "@.name", "pubserver_priv", "id_grp", "id_pubserver", "Groups", null, FieldFlags.Item | FieldFlags.Optional));
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override void OnBeforeDefine() {
            Fields["usePassword"].Value = "true";
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets default values to fields when writing into the database.</summary>
        protected override void OnAfterRead() {
            FieldExpression field;
            int userId = Fields["user"].AsInteger;
            protocol = Fields["protocol"].Value;
            usernameRequired = (protocol != "gsiftp");
            if (usernameRequired) {
                Fields["usernamex"].Flags = Fields["usernamex"].Flags & FieldFlags.AllButOptional;
                if (Fields["usernamex"].Value == null) Fields["usernamex"].Invalid = true;
            }
            //username = Fields["usernamex"].Value;
            usePassword = Fields["usePassword"].AsBoolean;

            field = Fields["passwordx"];
            if (!usePassword) {
                field.Value = null;
                field.SqlValue = "NULL";
            }
            if (protocol == "ftp") field.Flags = field.Flags & FieldFlags.AllButOptional;
            hasPassword = (field.Value != null || Id != 0 && context.GetQueryBooleanValue(String.Format("SELECT password IS NOT NULL FROM pubserver WHERE id={0};", Id)));
            field.Invalid = (!hasPassword && (usePassword || (field.Flags & FieldFlags.Optional) == 0));
            Fields["usePassword"].Invalid = field.Invalid;

            if (usePassword && field.Value == null && !field.Invalid) field.Flags |= FieldFlags.Ignore;

            if (!hasPassword && protocol == "scp") {
                if (Fields["publicKey"].Value == null) {
                    missingPasswordOrPublicKey = true;
                    Fields["publicKey"].Invalid = true;
                }
            }
            //isDefault = Fields["default"].AsBoolean;
            if (Fields["metadata"].AsBoolean && userId != 0) Fields["metadata"].Invalid = true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Performs an additional operation on the item that was just processed.</summary>
        /*!
        /// <param name="type">the operation type that has been applied to the item</param>
        /// <param name="itemId">the ID of the item</param>

            Used here for assembling the upload URL of the publish server after a creation or modification request.
        */
        protected override bool OnItemProcessed(OperationType type, int itemId) {
            switch (type) {
                case OperationType.Create:
                case OperationType.Modify:

                    PublishServer publishServer = PublishServer.FromId(context, itemId);
                    publishServer.Store(); // TODO: improve (Store updates other fields and records)
                    break;
            }
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnItemNotProcessed(OperationType type, int itemId) {
            if (missingPasswordOrPublicKey) {
                context.AddError("With the selected protocol you must specify either a connection password or a public key subject");
                return true;
            } else if (Fields["metadata"].Invalid) {
                context.AddError("Only shared publish servers can be used for task result metadata");
                return true;
            }
            return false;
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class SeriesStructure : EntityData {

        private int[] defaultServiceIds;
        private Series series;

        protected virtual Series Series {
            get {
                if (series == null) series = new Series(context);
                return series;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the ID of the service that must be compatible with the computing resource.</summary>
        public int ServiceId { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the identifier of the default series in value set option list.</summary>
        public string DefaultIdentifier { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the regular expression pattern to be matched by series that are included in the value set option list.</summary>
        public string RegexPattern { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public SeriesStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Series), optionList) {
            
            if (context.AdminMode) {
                /*Entity defaultServicesEntity = Entity.GetInstance(context, new EntityData(
                    null,
                    "service_series",
                    new FieldExpression[] {
                    new FixedValueField("default", "is_default", "bool", "true", 0)
                }
                ));
                MultipleReferenceField defaultServicesField = new MultipleReferenceField("defaultServices", "service", "@.name", defaultServicesEntity, "Services using this dataset series as default", FieldFlags.Item | FieldFlags.Optional | FieldFlags.Custom);*/
                FieldExpression defaultServicesField = EntityData.EmptyField;

                Fields.Add(new SingleValueField("caption", "name", "string", "Name", null, FieldFlags.Both | FieldFlags.Searchable | FieldFlags.SortAsc));
                Fields.Add(new SingleValueField("identifier", "identifier", "identifier", "Identifier", null, FieldFlags.Both | FieldFlags.Searchable | FieldFlags.Custom));
                Fields.Add(new SingleValueField("description", "text", "Description", FieldFlags.Item | FieldFlags.Searchable | FieldFlags.Optional));
                Fields.Add(new SingleReferenceField("domain", "domain", "@.name", "id_domain", "Domain", null, FieldFlags.Both | FieldFlags.Optional));
                Fields.Add(new SingleReferenceField("catalogue", "catalogue", "@.name", "id_catalogue", "Catalogue", null, FieldFlags.Item | FieldFlags.Optional | FieldFlags.Custom));
                Fields.Add(new SingleValueField("catDescription", "cat_description", "url", "OpenSearch Description URL", null, FieldFlags.Item | FieldFlags.Custom));
                Fields.Add(new SingleValueField("refreshWithAgent", "auto_refresh", "bool", "Refresh URL template periodically (background agent)", null, FieldFlags.Item | FieldFlags.Custom));
                Fields.Add(new SingleValueExpression("refreshWhenSaving", "cat_template IS NULL", "bool", "Refresh URL template now (once)", null, FieldFlags.Item | FieldFlags.Custom));
                Fields.Add(new SingleValueField("catTemplate", "cat_template", "url", "OpenSearch URL template", null, FieldFlags.Item | FieldFlags.Optional | FieldFlags.Custom));
                Fields.Add(new SingleValueField("iconUrl", "icon_url", "url", "Icon/logo URL", null, FieldFlags.Item | FieldFlags.Optional));
                Fields.Add(defaultServicesField);
                Fields.Add(new MultipleReferenceField("services", "service", "@.name", "service_series", "Services accepting this dataset series", FieldFlags.Item | FieldFlags.Custom | FieldFlags.Optional));
                Fields.Add(new MultipleReferenceField("groups", "grp", "@.name", "series_priv", "Groups", FieldFlags.Item | FieldFlags.Optional));

            } else {
                Join = String.Format("cr AS t{0}", ServiceId == 0 ? String.Empty : " INNER JOIN service_series AS c ON t.id=c.id_service AND c.id_service=" + ServiceId);

                if (OptionList) {
                    Fields.Add(new SingleValueField("value", "identifier", null, null, null, FieldFlags.Both));
                    Fields.Add(new SingleValueExpression("description", "REPLACE(cat_description, '$(CATALOGUE)', t1.base_url)", null, null, null, FieldFlags.List));
                    Fields.Add(new SingleValueExpression("template", "REPLACE(cat_template, '$(CATALOGUE)', t1.base_url)", null, null, null, FieldFlags.List));
                    Fields.Add(new SingleValueField("caption", "name", null, null, null, FieldFlags.List | FieldFlags.Unique | FieldFlags.SortAsc));
                    Fields.Add(new SingleValueExpression("default", (ServiceId == 0 ? "0" : "CASE WHEN c.is_default THEN 1 ELSE 0 END"), null, null, null, FieldFlags.List));
                }

            }

            if (DefaultIdentifier != null || RegexPattern != null) {
                if (FilterCondition == null) FilterCondition = String.Empty;
                else FilterCondition += " AND ";
                bool both = DefaultIdentifier != null && RegexPattern != null;
                FilterCondition += String.Format("{0}{1}{2}{3}{4}",
                        both ? "(" : String.Empty,
                        DefaultIdentifier == null ? String.Empty : "t.identifier=" + StringUtils.EscapeSql(DefaultIdentifier),
                        both ? " OR " : String.Empty,
                        RegexPattern == null ? String.Empty : "t.identifier REGEXP " + StringUtils.EscapeSql(RegexPattern),
                        both ? ")" : String.Empty
                ); 
            }

        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override void OnBeforeDefine() {
            Fields["refreshWithAgent"].Value = "true";
            Fields["refreshWhenSaving"].Value = "true";
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets default values to fields when writing into the database.</summary>
        protected override void OnAfterRead() {

            FieldExpression field = Fields["defaultServices"];

            if (field.ValueCount != 0) {
                defaultServiceIds = new int[field.ValueCount];
                for (int i = 0; i < field.ValueCount; i++) Int32.TryParse(field.Values[i], out defaultServiceIds[i]);
            } else {
                defaultServiceIds = null;
            }
            field.Flags |= FieldFlags.Ignore;

            field = Fields["services"];
            if (defaultServiceIds != null) {
                if (field.ValueCount == 0) {
                    field.Values = new string[0];
                } else {
                    for (int i = 0; i < defaultServiceIds.Length; i++) {
                        for (int j = 0; j < field.ValueCount; j++) if (field.Values[j] == defaultServiceIds[i].ToString()) field.Values[j] = null;
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnLoad() {

            Series result = this.Series;
            /*result.LoadCompatibleServices();

            Fields["identifier"].Value = result.Identifier;
            Fields["catalogue"].AsInteger = result.CatalogueId;
            Fields["catDescription"].Value = result.RawCatalogueDescriptionUrl;
            Fields["auto_refresh"].Value = result.RefreshWithAgent;
            Fields["refreshWhenSaving"].AsBoolean = true;
            Fields["catTemplate"].Value = result.RawCatalogueUrlTemplate;
            Fields["iconUrl"] = result.IconUrl;
            MultipleReferenceField defaultServicesField = Fields["defaultServices"] as MultipleReferenceField;
            MultipleReferenceField servicesField = Fields["services"] as MultipleReferenceField;
            servicesField.Values = new string[result.CompatibleServices.Count];
            foreach (SeriesServiceCompatibility item in result.CompatibleServices) {
                if (item.IsDefault) defaultServicesField[defaultIndex++] = item.Id;
                servicesField.Values[index++] = item.Id;
            }

            Fields.Add(defaultServicesField);
            Fields.Add(new MultipleReferenceField("services", "service", "@.name", "service_series", "Services accepting this dataset series", FieldFlags.Item | FieldFlags.Custom | FieldFlags.Optional));
            Fields.Add(new MultipleReferenceField("groups", "grp", "@.name", "series_priv", "Groups", FieldFlags.Item | FieldFlags.Optional));*/

            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override Entity OnStore() {

            Series result = this.Series;

            result.Identifier = Fields["identifier"].Value;
            result.CatalogueId = Fields["catalogue"].AsInteger;
            result.RawCatalogueDescriptionUrl = Fields["catDescription"].Value;
            //result.RefreshWithAgent = Fields["auto_refresh"].Value;
            if (Fields["refreshWhenSaving"].AsBoolean) result.GetCatalogueUrlTemplate();
            else result.RawCatalogueUrlTemplate = Fields["catTemplate"].Value;

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override bool OnItemProcessed(OperationType type, int itemId) {
            if (type != OperationType.Create && type != OperationType.Modify) return false;
            if (defaultServiceIds != null) {
                string rows = null;
                for (int i = 0; i < defaultServiceIds.Length; i++) {
                    if (defaultServiceIds[i] == 0) continue;
                    if (rows == null) rows = String.Empty; else rows += ", ";
                    rows += String.Format("({0}, {1}, true)", defaultServiceIds[i], itemId);
                }
                if (rows != null) context.Execute(String.Format("INSERT INTO service_series (id_service, id_series, is_default) VALUES {0};", rows));
            }
            return false;
        }




    }


/*
    public class XxxStructure : EntityData {

        public XxxStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Xxx), optionList) {
        }

    }

    public class XxxStructure : EntityData {

        public XxxStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Xxx), optionList) {
        }

    }

    public class XxxStructure : EntityData {

        public XxxStructure(IfyWebContext context, bool optionList) : base(context, typeof(Terradue.Portal.Xxx), optionList) {
        }

    }

*/






}

