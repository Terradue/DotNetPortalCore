using System;
using Terradue.Util;
using System.Collections.Generic;
using Terradue.OpenSearch;
using Terradue.Portal.OpenSearch;
using Terradue.OpenSearch.Result;
using System.Collections.Specialized;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Portal {




    /// <summary>Represents an activity related to or performed on an item of an Entity subclass.</summary>
    /// <description>
    /// Activity class
    /// -> log activities made by users
    /// -> associated to a privilege and an entity type (e.g series: create)
    /// -> NOT directly associated to an entity (allow persistence if entity is deleted)
    /// -> stores current user
    /// -> stores entity owner
    /// -> stores type of the entity
    /// 
    /// Linked to Entity
    /// -> Entity Store function creates an activity (CREATE or MODIFY)
    /// -> Entity Delete function creates an activity (DELETE)
    /// -> Entity StoreGlobalPrivileges creates an activity (MAKE_PUBLIC)
    /// 
    /// Specific actions at SubClass level
    /// -> Actions as View, Share, ... should not be done at Entity level but at subclass level (so we better control what we log)
    /// </description>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("activity", EntityTableConfiguration.Custom, HasDomainReference = true)]
    public class Activity : EntitySearchable, IComparable<Activity> {

        /// <summary>Gets or sets the database ID of the entity item.</summary>
        [EntityDataField("id_entity")]
        public int EntityId { get; set; }

        /// <summary>Gets or sets the database ID of the entity item's owner.</summary>
        [EntityDataField("id_owner")]
        public new int OwnerId { get; set; }

        /// <summary>Gets or sets the database ID of the user performing this activity.</summary>
        [EntityDataField("id_usr")]
        public new int UserId { get; set; }

        private Entity pentity;
        protected Entity Entity {
            get {
                if (pentity == null) {
                    try {
                        pentity = this.ActivityEntityType.GetEntityInstanceFromId(context, this.EntityId);
                        pentity.Load(this.EntityId);
                    } catch (Exception) { return null; }
                }
                return pentity;
            }
            set {
                pentity = value;
            }
        }

        /// <summary>Gets or sets the unique identifier of the Entity item.</summary>
        [EntityDataField("identifier_entity")]
        public string EntityIdentifier { get; set; }

        /// <summary>Gets or sets the database ID of the privilege that applies to this activity.</summary>
        [EntityDataField("id_priv")]
        public int PrivilegeId { get; set; }

        private Privilege priv;

        /// <summary>Gets or sets the privilege that applies to this activity.</summary>
        public Privilege Privilege {
            get {
                if (priv == null && PrivilegeId != 0) {
                    try {
                        priv = Privilege.FromId(context, PrivilegeId);
                    } catch (Exception) { priv = null; }
                }
                return priv;
            }
            set {
                priv = value;
                this.PrivilegeId = priv.Id;
            }
        }

        /// <summary>Gets or sets the database ID of the entity type of the item used by activity.</summary>
        [EntityDataField("id_type")]
        public int EntityTypeId { get; protected set; }

        private EntityType entityType;

        /// <summary>Gets or sets the entity type of the item used by activity.</summary>
        public EntityType ActivityEntityType {
            get {
                if (entityType == null && this.EntityTypeId != 0) {
                    try {
                        entityType = EntityType.GetEntityTypeFromId(this.EntityTypeId);
                    } catch (Exception) { entityType = null; }
                }
                return entityType;
            }

            protected set {
                entityType = value;
                EntityTypeId = (entityType == null ? 0 : entityType.Id);
            }
        }

        /// <summary>Gets the UTC date and time of the activity's log creation.</summary>
        [EntityDataField("log_time")]
        public DateTime CreationTime { get; protected set; }

        /// <summary>Creates a new Activity instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Activity(IfyContext context) : base(context) { }

        /// <summary>Creates a new Activity instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="entity">The entity to which the activity relates.</param>
        /// <param name="operation">The operation performed on the entity.</param>
        public Activity(IfyContext context, Entity entity, EntityOperationType operation) : this(context, entity, ((char)operation).ToString()) {
        }

        /// <summary>Creates a new Activity instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="entity">Entity.</param>
        /// <param name="operation">The name of the operation performed on the entity.</param>
        public Activity(IfyContext context, Entity entity, string operation) : base(context) {
            if (entity != null) {
                try {
                    this.EntityId = entity.Id;
                    this.Entity = entity;
                    this.EntityIdentifier = entity.Identifier;
                    this.ActivityEntityType = EntityType.GetEntityType(entity.GetType());
                    this.EntityTypeId = (this.ActivityEntityType.Id != 0 ? this.ActivityEntityType.Id : this.ActivityEntityType.TopTypeId);
                    this.DomainId = entity.DomainId;
                    this.Privilege = Privilege.Get(EntityType.GetEntityTypeFromId(this.EntityTypeId), Privilege.GetOperationType(operation));
                } catch (Exception e) {
                    var t = e;
                }
            }
            this.OwnerId = entity.OwnerId;
        }

        /// <summary>Creates a new Activity instance representing the activity with the specified database ID.</summary>
        /// <returns>The created Activity object.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The database ID of the activity.</param>
        public static Activity FromId(IfyContext context, int id) {
            Activity result = new Activity(context);
            result.Id = id;
            result.Load();
            return result;
        }

        /// <summary>Creates a new Activity instance representing an existing activity with the specified entity and operation.</summary>
        /// <returns>The created Activity object.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="entity">The entity to which the activity relates.</param>
        /// <param name="operation">The operation performed on the entity.</param>
        public static Activity FromEntityAndPrivilege(IfyContext context, Entity entity, EntityOperationType operation) {
            var etype = EntityType.GetEntityType(entity.GetType());
            var priv = Privilege.Get(EntityType.GetEntityTypeFromId(etype.Id), Privilege.GetOperationType(((char)operation).ToString()));
            Activity result = new Activity(context);
            result.Entity = entity;
            result.EntityTypeId = etype.Id;
            result.Privilege = priv;
            result.Load();
            return result;
        }

        /// <summary>Returns an SQL conditional expression to select an activity item based on property values of this activity.</summary>
        /// <returns>The SQL conditional expression corresponding to the property values.</returns>
        public override string GetIdentifyingConditionSql() {
            if (Entity != null && Privilege != null) {
                if (EntityTypeId == 0) EntityTypeId = EntityType.GetEntityType(Entity.GetType()).Id;
                return String.Format("t.id_entity='{0}' AND t.id_type='{1}' AND t.id_priv='{2}'", Entity.Id, EntityTypeId, Privilege.Id);
            }
            return null;
        }

        /// <summary>Loads the activities for the specified user.</summary>
        /// <returns>An EntityList<Activity> object containing the activities of the user.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="usrId">The database ID of the user.</param>
        public static EntityList<Activity> ForUser(IfyContext context, int usrId) {
            EntityList<Activity> results = new EntityList<Activity>(context);
            results.Template.UserId = usrId;
            results.Load();
            return results;
        }

        /// <summary>Store this activity in the database.</summary>
        public override void Store() {

            context.LogDebug(this, string.Format("Storing activity: {0},{1},{2},{3}", this.context.UserId, this.PrivilegeId, this.EntityTypeId, (this.Privilege != null ? this.Privilege.EnableLog.ToString() : "null")));
            if (this.context.UserId == 0 || this.PrivilegeId == 0 || this.EntityTypeId == 0 || !this.Privilege.EnableLog) return;

            this.UserId = context.UserId;
            this.CreationTime = DateTime.UtcNow;

            base.Store();
        }

        #region IEntitySearchable implementation
        public override KeyValuePair<string, string> GetFilterForParameter(string parameter, string value) {
            switch (parameter) {
            case "entitytype":
                var t = EntityType.GetEntityTypeFromKeyword(value);
                return new KeyValuePair<string, string>("EntityTypeId", t.Id.ToString());
            case "operation":
                var privList = new EntityList<Privilege>(context);
                privList.SetFilter("OperationChar", value);
                privList.Load();
                var items = privList.GetItemsAsList();
                var ids = new List<int>();
                foreach (var item in items) ids.Add(item.Id);
                return new KeyValuePair<string, string>("PrivilegeId", string.Join(",", ids));
            default:
                return base.GetFilterForParameter(parameter, value);
            }
        }

        #endregion

        #region IAtomizable implementation
        public bool IsSearchable(NameValueCollection parameters) {
            if (this.EntityId == 0 || Entity == null) return false;
            if (this.Privilege == null) return false;
            if (this.ActivityEntityType == null) return false;

            return true;
        }


        public override AtomItem ToAtomItem(NameValueCollection parameters) {


            if (!IsSearchable(parameters)) return null;

            User owner = User.ForceFromId(context, this.OwnerId);

            //string identifier = null;
            string name = (Entity.Name != null ? Entity.Name : Entity.Identifier);
            string description = null;
            Uri id = new Uri(context.BaseUrl + "/" + this.ActivityEntityType.Keyword + "/search?id=" + Entity.Identifier);

            switch (this.Privilege.Operation) {
            case EntityOperationType.Create:
                description = string.Format("created the {0} {1}", this.ActivityEntityType.SingularCaption, name);
                break;
            case EntityOperationType.Change:
                description = string.Format("updated the {0} {1}", this.ActivityEntityType.SingularCaption, name);
                break;
            case EntityOperationType.Delete:
                description = string.Format("deleted the {0} {1}", this.ActivityEntityType.SingularCaption, name);
                break;
            case EntityOperationType.Share:
                description = string.Format("shared the {0} {1}", this.ActivityEntityType.SingularCaption, name);
                break;
            default:
                break;
            }

            //AtomItem atomEntry = null;
            AtomItem result = new AtomItem();

            result.Id = id.ToString();
            result.Title = new TextSyndicationContent(Entity.Identifier);
            result.Content = new TextSyndicationContent(name);

            result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", Guid.NewGuid());
            result.Summary = new TextSyndicationContent(description);
            result.ReferenceData = this;
            result.PublishDate = this.CreationTime;
            result.LastUpdatedTime = this.CreationTime;
            var basepath = new UriBuilder(context.BaseUrl);
            basepath.Path = "user";
            string usrUri = basepath.Uri.AbsoluteUri + "/" + owner.Username;
            string usrName = (!String.IsNullOrEmpty(owner.FirstName) && !String.IsNullOrEmpty(owner.LastName) ? owner.FirstName + " " + owner.LastName : owner.Username);
            SyndicationPerson author = new SyndicationPerson(owner.Email, usrName, usrUri);
            author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", owner.Username));
            result.Authors.Add(author);
            result.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));
            Uri share = new Uri(context.BaseUrl + "/share?url=" + id.AbsoluteUri);
            result.Links.Add(new SyndicationLink(share, "via", "share", "application/atom+xml", 0));

            return result;
        }

        public new NameValueCollection GetOpenSearchParameters() {
            NameValueCollection nvc = base.GetOpenSearchParameters();
            nvc.Add("entitytype", "{t2:entityType?}");
            nvc.Add("operation", "{t2:operation?}");
            return nvc;
        }

        #endregion

        #region IComparable implementation

        public int CompareTo(Activity other) {
            if (other == null)
                return 1;
            else
                return this.CreationTime.CompareTo(other.CreationTime);
        }

        #endregion
    }
}

