using System;
using Terradue.Util;
using System.Collections.Generic;

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
    public class Activity : Entity {

        /// <summary>Gets the Entity Id</summary>
        [EntityDataField("id_entity")]
        public int EntityId { get; set; }

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

//        private EntityType entityType;
//        public EntityType EntityType { 
//            get { 
//                return entityType;
//            } 
//
//            protected set {
//                entityType = value;
//                EntityTypeId = (entityType == null ? 0 : entityType.Id);
//            }
//        }

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
                    this.EntityType = EntityType.GetEntityType(entity.GetType());
                    this.EntityTypeId = this.EntityType.Id;
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
    }
}

