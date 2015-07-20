using System;
using Terradue.Util;
using System.Collections.Generic;

namespace Terradue.Portal {

    /// <summary>
    /// Activity privilege class.
    /// </summary>
    public class ActivityPrivilege{

        public const string VIEW = "v";
        public const string CREATE = "c";
        public const string MODIFY = "m";
        public const string DELETE = "d";
        public const string MAKE_PUBLIC = "p";

    }

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
    public class Activity {
        
        private IfyContext context;

        /// <summary>Gets the Id</summary>
        [EntityDataField("id")]
        public int Id { get; set; }

        /// <summary>Gets the Entity Id</summary>
        [EntityDataField("id_entity")]
        public int EntityId { get; set; }

        /// <summary>Gets the User Id</summary>
        [EntityDataField("id_usr")]
        public int UserId { get; set; }

        /// <summary>Gets the Owner Id</summary>
        [EntityDataField("id_owner")]
        public int OwnerId { get; set; }

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
        public EntityType EntityType { 
            get { 
                return entityType;
            } 

            protected set {
                entityType = value;
                EntityTypeId = (entityType == null ? 0 : entityType.Id);
            }
        }

        /// <summary>Gets the description</summary>
        [EntityDataField("description")]
        public string Description { get; set; }

        /// <summary>Gets the UTC date and time of the activity's creation.</summary>
        [EntityDataField("creation_time")]
        public DateTime CreationTime { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Portal.Activity"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public Activity(IfyContext context) {
            this.context = context;
            this.CreationTime = DateTime.UtcNow;
            this.UserId = context.UserId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Portal.Activity"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="entity">Entity.</param>
        public Activity(IfyContext context, Entity entity, string operation) : this(context){
            if (entity != null) {
                try{
                    this.EntityId = entity.Id;
                    this.EntityType = EntityType.GetEntityType(entity.GetType());
                    this.Privilege = Privilege.FromTypeAndOperation(context, this.EntityTypeId, operation);
                    this.Description = string.Format("{0} (id = {1})",this.Privilege.Name, entity.Id);
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
            Activity result = null;
            string sql = String.Format("SELECT id, id_usr, id_priv, id_type, id_owner, description, creation_time FROM activity WHERE id={0};", id);
            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            if (reader.Read()) {
                result = new Activity(context);
                result.Id = reader.GetInt32(0);
                result.UserId = reader.GetInt32(1);
                result.PrivilegeId = reader.GetInt32(2);
                result.EntityTypeId = reader.GetInt32(3);
                result.OwnerId = reader.GetInt32(4);
                result.Description = reader.GetString(5);
                result.CreationTime = reader.GetDateTime(6);
            }
            reader.Close();

            return result;
        }

        /// <summary>
        /// Fors the user.
        /// </summary>
        /// <returns>The user.</returns>
        /// <param name="context">Context.</param>
        /// <param name="usrId">Usr identifier.</param>
        public static List<Activity> ForUser(IfyContext context, int usrId){
            List<Activity> results = new List<Activity>();
            string sql = String.Format("SELECT id, id_usr, id_priv, id_type, id_owner, description, creation_time FROM activity WHERE id_usr={0} OR id_owner={0};", usrId);
            System.Data.IDbConnection dbConnection = context.GetDbConnection();
            System.Data.IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                Activity result = new Activity(context);
                result.Id = reader.GetInt32(0);
                result.UserId = reader.GetInt32(1);
                result.PrivilegeId = reader.GetInt32(2);
                result.EntityTypeId = reader.GetInt32(3);
                result.OwnerId = reader.GetInt32(4);
                result.Description = reader.GetString(5);
                result.CreationTime = reader.GetDateTime(6);
                results.Add(result);
            }
            reader.Close();

            return results;
        }

        /// <summary>
        /// Store this instance.
        /// </summary>
        public void Store(){
            if (this.UserId == 0 || this.OwnerId == 0 || this.PrivilegeId == 0 || this.EntityTypeId == 0) return;

            string sql = String.Format("INSERT INTO activity (id_usr, id_priv, id_type, id_owner, description, creation_time) VALUES ({0},{1},{2},{3},{4},{5});",
                                       this.UserId, 
                                       this.PrivilegeId, 
                                       this.EntityTypeId, 
                                       this.OwnerId, 
                                       StringUtils.EscapeSql(this.Description), 
                                       StringUtils.EscapeSql(this.CreationTime.ToString("yyyy-MM-dd hh:mm:ss")));
            context.Execute(sql);
        }
    }
}

