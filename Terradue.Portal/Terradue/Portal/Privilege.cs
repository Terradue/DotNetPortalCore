using System;
using Terradue.Util;

namespace Terradue.Portal {



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>
    /// Activity privilege class.
    /// </summary>
    public class OperationPriv{

        public const string VIEW = "v";
        public const string CREATE = "c";
        public const string MODIFY = "m";
        public const string DELETE = "d";
        public const string MAKE_PUBLIC = "p";
        public const string LOGIN = "l";

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Represents a privilege that can be included in roles that are granted to users or groups globally or for specific domains.</summary>
    /// <remarks>
    ///     <para>Privileges are usually defined by an entity type and an operation. This means that a user with that privilege is allowed to perform the defined operation on items of the defined entity type within the scope of his role (domain or global). There are generic operations like create, change or delete that can apply to most entity types.</para>
    ///     <para>For more complex entity types, additional privileges may be defined where the operations are specific to that entity type.</para>
    ///     <para>Privileges may also refer to no specific entity type or operation at all and just be used as placeholders for application-specific authorisation policies.</para>
    /// </remarks>
    [EntityTable("priv", EntityTableConfiguration.Full)]
    public class Privilege : Entity {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the database ID of the entity type to which this privilege refers.</summary>
        [EntityDataField("id_type", IsForeignKey = true)]
        public int EntityTypeId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the default position of this privilege in lists.</summary>
        [EntityDataField("pos")]
        public int Position { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the operation on items of the entity type defined by this privilege.</summary>
        [EntityDataField("operation")]
        public string Operation { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the every usage of this privilege is logged.</summary>
        [EntityDataField("enable_log")]
        public bool EnableLog { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the alternative identifying condition.</summary>
        public override string AlternativeIdentifyingCondition {
            get {
                if (Id == 0 && EntityTypeId != 0 && !String.IsNullOrEmpty(Operation)) return String.Format("t.id_type={0} AND t.operation={1}", EntityTypeId, StringUtils.EscapeSql(Operation));
                return null;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Privilege instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>The created Privilege object.</returns>
        public Privilege(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Privilege instance representing the privilege with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The database ID of the privilege.</param>
        /// <returns>The created Privilege object.</returns>
        public static Privilege FromId(IfyContext context, int id) {
            Privilege result = new Privilege(context);
            result.Id = id;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Privilege instance representing the privilege with the specified identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="identifier">The unique identifier of the privilege.</param>
        /// <returns>The created Privilege object.</returns>
        public static Privilege FromIdentifier(IfyContext context, string identifier) {
            Privilege result = new Privilege(context);
            result.Identifier = identifier;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Privilege instance representing the privilege with the specified identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="entityTypeId">The database ID of the entity type to which the privilege refers.</param>
        /// <param name="operation">The operation to be performed on items of the entity type.</param>
        /// <returns>The created Privilege object.</returns>
        public static Privilege FromTypeAndOperation(IfyContext context, int entityTypeId, string operation) {
            Privilege result = new Privilege(context);
            result.EntityTypeId = entityTypeId;
            result.Operation = operation;
            result.Load();
            return result;
        }
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class PrivilegeSet {

        Privilege[] Privileges { get; set; }

        public PrivilegeSet() {
        }

    }

}

