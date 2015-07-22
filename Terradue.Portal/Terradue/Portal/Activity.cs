using System;
using Terradue.Util;
using System.Collections.Generic;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using System.Collections.Specialized;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Portal {

    /// <summary>
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
    /// </summary>
    [EntityTable("activity", EntityTableConfiguration.Custom, HasOwnerReference = true)]
    public class Activity : Entity, IAtomizable {

        /// <summary>Gets the Entity Id</summary>
        [EntityDataField("id_entity")]
        public int EntityId { get; set; }

        /// <summary>Gets the Entity Id</summary>
        [EntityDataField("identifier_entity")]
        public string EntityIdentifier { get; set; }

        /// <summary>Gets the Privilege Id</summary>
        [EntityDataField("id_priv")]
        public int PrivilegeId { get; set; }

        private Privilege priv;
        /// <summary>Gets or sets the privilege.</summary>
        public Privilege Privilege { 
            get { 
                if(priv == null && PrivilegeId != 0)
                    priv = Privilege.FromId(context, PrivilegeId);
                return priv;
            }
            set{
                priv = value;
                this.PrivilegeId = priv.Id;
            } 
        }

        /// <summary>Gets the Entity Type Id</summary>
        [EntityDataField("id_type")]
        public int EntityTypeId { get; protected set; }

        private EntityType entityType;
        public new EntityType ActivityEntityType { 
            get { 
                if (entityType == null && this.EntityTypeId != 0)
                    entityType = EntityType.GetEntityTypeFromId(this.EntityTypeId);
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

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Portal.Activity"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public Activity(IfyContext context) : base(context) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Portal.Activity"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="entity">Entity.</param>
        public Activity(IfyContext context, Entity entity, string operation) : base(context){
            if (entity != null) {
                try{
                    this.EntityId = entity.Id;
                    this.EntityIdentifier = entity.Identifier;
                    this.ActivityEntityType = EntityType.GetEntityType(entity.GetType());
                    this.EntityTypeId = (this.ActivityEntityType.Id != 0 ? this.ActivityEntityType.Id : this.ActivityEntityType.TopTypeId);
                    this.Privilege = Privilege.FromTypeAndOperation(context, this.EntityTypeId, operation);
                }catch(Exception e){
                }
            }
            this.OwnerId = entity.OwnerId;
        }

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static Activity FromId(IfyContext context, int id){
            Activity result = new Activity(context);
            result.Id = id;
            result.Load();
            return result;
        }

        /// <summary>
        /// Fors the user.
        /// </summary>
        /// <returns>The user.</returns>
        /// <param name="context">Context.</param>
        /// <param name="usrId">Usr identifier.</param>
        public static EntityList<Activity> ForUser(IfyContext context, int usrId){
            EntityList<Activity> results = new EntityList<Activity>(context);
            results.Template.UserId = usrId;
            results.Load();
            return results;
        }

        /// <summary>
        /// Store this instance.
        /// </summary>
        public override void Store(){
            
            if (this.context.UserId == 0 || this.PrivilegeId == 0 || this.EntityTypeId == 0 || !this.Privilege.EnableLog) return;

            this.UserId = context.UserId;
            this.CreationTime = DateTime.UtcNow;

            string sql = String.Format("INSERT INTO activity (id_usr, id_priv, id_type, id_owner, id_entity, log_time) VALUES ({0},{1},{2},{3},{4},{5});",
                                       this.UserId, 
                                       this.PrivilegeId, 
                                       this.EntityTypeId, 
                                       this.OwnerId, 
                                       this.EntityId,
                                       StringUtils.EscapeSql(this.CreationTime.ToString("yyyy-MM-dd hh:mm:ss")));
            context.Execute(sql);
        }

        #region IAtomizable implementation

        public new AtomItem ToAtomItem(NameValueCollection parameters) {


            if (this.EntityId == 0) return null;

            Entity entity = null;
            try{
                entity = this.ActivityEntityType.GetEntityInstanceFromId(context, this.EntityId);
                entity.Load(this.EntityId);
            }catch(Exception e){
                return null;
            }
            User owner = User.FromId(context, this.OwnerId);

            string identifier = null;
            string name = (entity.Name != null ? entity.Name : entity.Identifier);
            string description = null;
            string text = (this.TextContent != null ? this.TextContent : "");
            Uri id = new Uri(context.BaseUrl + "/" + this.ActivityEntityType.Keyword + "/search?id=" + entity.Identifier);

            if (!string.IsNullOrEmpty(parameters["q"])) {
                string q = parameters["q"].ToLower();
                if (!(name.ToLower().Contains(q) || entity.Identifier.ToLower().Contains(q) || text.ToLower().Contains(q)))
                    return null;
            }

            switch (this.Privilege.Operation) {
                case OperationPriv.CREATE:
                    description = string.Format("has created the {0} {1}",this.ActivityEntityType.Keyword, name);
                    break;
                case OperationPriv.MODIFY:
                    description = string.Format("has updated the {0} {1}",this.ActivityEntityType.Keyword, name);
                    break;
                case OperationPriv.DELETE:
                    description = string.Format("has deleted the {0} {1}",this.ActivityEntityType.Keyword, name);
                    break;
                case OperationPriv.MAKE_PUBLIC:
                    description = string.Format("has shared the {0} {1}",this.ActivityEntityType.Keyword, name);
                    break;
                default:
                    break;
            }

            AtomItem atomEntry = null;
            AtomItem result = new AtomItem();

            result.Id = id.ToString();
            result.Title = new TextSyndicationContent(entity.Identifier);
            result.Content = new TextSyndicationContent(name);

            result.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", Guid.NewGuid());
            result.Summary = new TextSyndicationContent(description);
            result.ReferenceData = this;
            result.PublishDate = new DateTimeOffset(this.CreationTime);
            result.Date = this.CreationTime;
            var basepath = new UriBuilder(context.BaseUrl);
            basepath.Path = "user";
            string usrUri = basepath.Uri.AbsoluteUri + "/" + owner.Username ;
            string usrName = (!String.IsNullOrEmpty(owner.FirstName) && !String.IsNullOrEmpty(owner.LastName) ? owner.FirstName + " " + owner.LastName : owner.Username);
            SyndicationPerson author = new SyndicationPerson(owner.Email, usrName, usrUri);
            author.ElementExtensions.Add(new SyndicationElementExtension("identifier", "http://purl.org/dc/elements/1.1/", owner.Username));
            result.Authors.Add(author);
            result.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));
            Uri share = new Uri(context.BaseUrl + "/share?url=" +id.AbsoluteUri);
            result.Links.Add(new SyndicationLink(share, "via", name, "application/atom+xml", 0));

            return result;
        }

        public System.Collections.Specialized.NameValueCollection GetOpenSearchParameters() {
            return OpenSearchFactory.GetBaseOpenSearchParameter();
        }

        #endregion
    }
}

