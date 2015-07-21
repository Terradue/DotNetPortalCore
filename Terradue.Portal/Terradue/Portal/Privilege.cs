using System;
using Terradue.Util;

namespace Terradue.Portal {

    /// <summary>
    /// Activity privilege class.
    /// </summary>
    public class PrivilegeOperation{

        public const string VIEW = "v";
        public const string CREATE = "c";
        public const string MODIFY = "m";
        public const string DELETE = "d";
        public const string MAKE_PUBLIC = "p";

    }

    /// <summary>
    /// Privilege class
    /// </summary>
    [EntityTable("priv", EntityTableConfiguration.Custom)]
    public class Privilege : Entity {

        /// <summary>Gets the Id</summary>
        [EntityDataField("id")]
        public int Id { get; set;}

        /// <summary>Gets the EntityType Id</summary>
        [EntityDataField("id_type", IsForeignKey = true)]
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

        /// <summary>Gets the position</summary>
        [EntityDataField("pos")]
        public int Position { get; set; }

        /// <summary>Gets the name</summary>
        [EntityDataField("name")]
        public string Name { get; set;}

        /// <summary>Gets the operation</summary>
        [EntityDataField("operation")]
        public string Operation { get; set;}

        /// <summary>Gets the boolean to enable log or not</summary>
        [EntityDataField("enable_log")]
        public bool EnableLog { get; set;}

        /// <summary>
        /// Gets the alternative identifying condition.
        /// </summary>
        /// <value>The alternative identifying condition.</value>
        public override string AlternativeIdentifyingCondition {
            get {
                if (this.Id == 0 && EntityTypeId != 0 && !string.IsNullOrEmpty(Operation))
                    return String.Format("t.id_type={0} AND t.operation={1}", EntityTypeId, StringUtils.EscapeSql(Operation));
                else
                    return null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Portal.Privilege"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        public Privilege(IfyContext context) : base(context){}

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public static Privilege FromId(IfyContext context, int id){
            Privilege result = new Privilege(context);
            result.Id = id;
            result.Load();
            return result;
        }

        /// <summary>
        /// Froms the type and operation.
        /// </summary>
        /// <returns>The type and operation.</returns>
        /// <param name="context">Context.</param>
        /// <param name="type_id">Type identifier.</param>
        /// <param name="operation">Operation.</param>
        public static Privilege FromTypeAndOperation(IfyContext context, int type_id, string operation){
            Privilege result = new Privilege(context);
            result.EntityTypeId = type_id;
            result.Operation = operation;
            result.Load();
            return result;
        }
    }
}

