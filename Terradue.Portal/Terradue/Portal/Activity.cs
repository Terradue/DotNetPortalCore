﻿using System;
using Terradue.Util;
using System.Collections.Generic;
using Terradue.OpenSearch;
using Terradue.Portal.OpenSearch;
using Terradue.OpenSearch.Result;
using System.Collections.Specialized;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Portal {




    /// <summary>Activity</summary>
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
    [EntityTable("activity", EntityTableConfiguration.Custom, HasOwnerReference = true)]
    public class Activity : Entity, IEntitySearchable, IComparable<Activity> {

        /// <summary>Gets the Entity Id</summary>
        [EntityDataField("id_entity")]
        public int EntityId { get; set; }

        private Entity pentity;
        private Entity Entity {
            get {
                if (pentity == null) {
                    try {
                        pentity = this.ActivityEntityType.GetEntityInstanceFromId (context, this.EntityId);
                        pentity.Load (this.EntityId);
                    } catch (Exception) { return null; }
                }
                return pentity;
            }
            set {
                pentity = value;
            }
        }

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
                if (priv == null && PrivilegeId != 0) {
                    try {
                        priv = Privilege.FromId (context, PrivilegeId);
                    } catch (Exception) { priv = null; }
                }
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
        public EntityType ActivityEntityType { 
            get {
                if (entityType == null && this.EntityTypeId != 0) {
                    try {
                        entityType = EntityType.GetEntityTypeFromId (this.EntityTypeId);
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

        private Domain domain;
        public override Domain Domain { 
            get {
                if(domain == null && this.Entity != null) domain = this.Entity.Domain;
                return domain;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Portal.Activity"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public Activity(IfyContext context) : base(context) {}

        public Activity(IfyContext context, Entity entity, EntityOperationType operation) : this(context, entity, ((char)operation).ToString()) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Portal.Activity"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="entity">Entity.</param>
        public Activity(IfyContext context, Entity entity, string operation) : base(context) {
            if (entity != null) {
                try {
                    this.EntityId = entity.Id;
                    this.Entity = entity;
                    this.EntityIdentifier = entity.Identifier;
                    this.ActivityEntityType = EntityType.GetEntityType(entity.GetType());
                    this.EntityTypeId = (this.ActivityEntityType.Id != 0 ? this.ActivityEntityType.Id : this.ActivityEntityType.TopTypeId);
                    this.Privilege = Privilege.Get(EntityType.GetEntityTypeFromId(this.EntityTypeId), Privilege.GetOperationType(operation));
                } catch (Exception) {
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

            context.LogDebug(this, string.Format("Storing activity: {0},{1},{2},{3}", this.context.UserId, this.PrivilegeId, this.EntityTypeId, (this.Privilege != null ? this.Privilege.EnableLog.ToString() : "null")));
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
        public string GetSqlCondition(System.Collections.Specialized.NameValueCollection parameters) {
            string sql = "";
            if (!string.IsNullOrEmpty(parameters["author"])) {
                var owner = User.ForceFromUsername(context, parameters["author"]);
                sql += string.Format(" AND id_usr={0}", owner.Id);
            }
            if (!string.IsNullOrEmpty(parameters["domain"])) {
                var d = Domain.FromIdentifier(context, parameters["domain"]);
                sql += string.Format(" AND id_domain={0}", d.Id);
            }
            return sql;
        }

        public bool IsSearchable (NameValueCollection parameters) {
            if (this.EntityId == 0 || Entity == null) return false;
            if (this.Privilege == null) return false;
            if (this.ActivityEntityType == null) return false;

            string name = (Entity.Name != null ? Entity.Name : Entity.Identifier);
            string text = (this.TextContent != null ? this.TextContent : "");

            if (!string.IsNullOrEmpty (parameters ["q"])) {
                string q = parameters ["q"].ToLower ();
                if (!(name.ToLower ().Contains (q) || Entity.Identifier.ToLower ().Contains (q) || text.ToLower ().Contains (q)))
                    return false;
            }

            return true;
        }


        public AtomItem ToAtomItem(NameValueCollection parameters) {


            if (!IsSearchable (parameters)) return null;

            User owner = User.ForceFromId(context, this.OwnerId);

            //string identifier = null;
            string name = (Entity.Name != null ? Entity.Name : Entity.Identifier);
            string description = null;
            Uri id = new Uri(context.BaseUrl + "/" + this.ActivityEntityType.Keyword + "/search?id=" + Entity.Identifier);

            switch (this.Privilege.Operation) {
                case EntityOperationType.Create:
                    description = string.Format("has created the {0} {1}",this.ActivityEntityType.Keyword, name);
                    break;
                case EntityOperationType.Change:
                    description = string.Format("has updated the {0} {1}",this.ActivityEntityType.Keyword, name);
                    break;
                case EntityOperationType.Delete:
                    description = string.Format("has deleted the {0} {1}",this.ActivityEntityType.Keyword, name);
                    break;
                case EntityOperationType.Share:
                    description = string.Format("has shared the {0} {1}",this.ActivityEntityType.Keyword, name);
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

