using System;
using System.Collections.Generic;
using System.Data;
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



    /// <summary>Represents a privilege that can be included in roles granted to users or groups globally or for specific domains.</summary>
    /// <remarks>
    ///     <para>Privileges are usually defined by an entity type and an operation. This means that a user with that privilege is allowed to perform the defined operation on items of the defined entity type within the scope of his role (domain or global). There are generic operations like create, change or delete that can apply to most entity types.</para>
    ///     <para>For more complex entity types, additional privileges may be defined where the operations are specific to that entity type.</para>
    ///     <para>Privileges may also refer to no specific entity type or operation at all and just be used as placeholders for application-specific authorisation policies.</para>
    /// </remarks>
    [EntityTable("priv", EntityTableConfiguration.Full)]
    public class Privilege : Entity {

        // Static list of privilege metadata; loaded at application startup and used as reference for lookup throughout the runtime
        private static Dictionary<int, Privilege> privileges = new Dictionary<int, Privilege>();

        private static Dictionary<EntityType, Dictionary<string, string>> entityTypePrivileges;// = new Dictionary<Type, EntityType>();

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

        public Privilege(int id, string identifier, string name, EntityType entityType, EntityOperationType operation, bool enableLog) : this(null) {
        }

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

        //---------------------------------------------------------------------------------------------------------------------

        public static void LoadPrivileges(IfyContext context) {
            privileges.Clear();
            IDataReader reader = context.GetQueryResult("SELECT t.id, t.identifier, t.name, t.id_type, t.operation, t.enable_log FROM priv AS t ORDER BY t.pos;");
            while (reader.Read()) {
                int id = context.GetIntegerValue(reader, 0);
                int entityTypeId = context.GetIntegerValue(reader, 3);
                EntityType entityType = entityTypeId == 0 ? null : EntityType.GetEntityTypeFromId(entityTypeId);
                EntityOperationType operation = GetOperationType(context.GetValue(reader, 4));

                Privilege privilege = new Privilege(
                    id,
                    context.GetValue(reader, 1),
                    context.GetValue(reader, 2),
                    entityType,
                    operation,
                    context.GetBooleanValue(reader, 5)
                );
                privileges[id] = privilege;
            }
            reader.Close();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static EntityOperationType GetOperationType(string value) {
            if (value == null || value.Length != 1) return EntityOperationType.Other;

            switch (value[0]) {
                case 'c':
                    return EntityOperationType.Create;
                case 's':
                    return EntityOperationType.Search;
                case 'v':
                    return EntityOperationType.View;
                case 'm':
                    return EntityOperationType.Change;
                case 'M':
                    return EntityOperationType.Manage;
                case 'd':
                    return EntityOperationType.Delete;
            }

            return EntityOperationType.Other;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Enumeration of generic operations regarding entities.</summary>
    public enum EntityOperationType {

        /// <summary>Create a new entity item.</summary>
        /// <remarks>This privilege allows a user to create new domain-owned or global entity items according to his grant.</remarks>
        Create = 'c',

        /// <summary>List and search entity items.</summary>
        /// <remarks>This privilege allows a user to see lists of entity items that are part of his grant and to search within these lists.</remarks>
        Search = 's',

        /// <summary>View an entity item.</summary>
        /// <remarks>This privilege allows a user to view the details of entity items that are part of his grant. The <c>View</c> privilege is implied by all other privileges except <c>Search</c>.</remarks>
        View = 'v',

        /// <summary>Change an existing entity item.</summary>
        /// <remarks>This privilege allows a user to make persistent modifications to entity items that are part of his grant.</remarks>
        Change = 'm',

        /// <summary>Use an entity item in the same way as its owner and manage or control it.</summary>
        /// <remarks>This privilege implies the Change privilege and, in addition, allows a user to influence what other users can do regarding entity items within his grant. Typical operations include changes to availability and the assignment of permissions to users or groups.</remarks>
        Manage = 'M',

        /// <summary>Make an entity item available to others.</summary>
        /// <remarks>This privilege allows a user to definitely remove entity items that are part of his grant from the database.</remarks>
        Delete = 'd',

        /// <summary>Any other operation.</summary>
        Other = '\0'
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

