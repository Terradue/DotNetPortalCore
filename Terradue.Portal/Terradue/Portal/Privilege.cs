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
    public class OperationPriv {

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

        //private static Dictionary<EntityType, Dictionary<string, string>> entityTypePrivileges;// = new Dictionary<Type, EntityType>();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the database ID of the entity type to which this privilege refers.</summary>
        [EntityDataField("id_type", IsForeignKey = true)]
        public int TargetEntityTypeId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the entity type to which this privilege refers (different from Entity.EntityType property that is the EntityType of the item itself).</summary>
        public EntityType TargetEntityType { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the default position of this privilege in lists.</summary>
        [EntityDataField("pos")]
        public int Position { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the operation on items of the entity type defined by this privilege.</summary>
        [EntityDataField("operation")]
        public string OperationChar { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the operation on items of the entity type defined by this privilege.</summary>
        public EntityOperationType Operation { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the every usage of this privilege is logged.</summary>
        [EntityDataField("enable_log")]
        public bool EnableLog { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Privilege instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>The created Privilege object.</returns>
        public Privilege(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public Privilege(int id, string identifier, string name, EntityType entityType, EntityOperationType operation, bool enableLog) : this(null) {
            this.Id = id;
            this.Identifier = identifier;
            this.Name = name;
            this.TargetEntityType = entityType;
            this.Operation = operation;
            this.EnableLog = enableLog;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Load() {
            base.Load();
            TargetEntityType = EntityType;
            Operation = GetOperationType(OperationChar.ToString());
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the preloaded privilege instance representing the privilege with the specified ID.</summary>
        /// <param name="id">The database ID of the privilege.</param>
        /// <returns>The matching Privilege instance.</returns>
        public static Privilege Get(int id) {
            Privilege result = privileges[id];
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns all privileges related to the specified entity type.</summary>
        /// <param name="entityType">The entity type to which the privileges apply.</param>
        /// <returns>An array of matching Privilege instances.</returns>
        public static Privilege[] Get(EntityType entityType) {
            List<Privilege> result = new List<Privilege>();
            entityType = entityType.TopType; // privileges defined for subtypes are not supported
            foreach (Privilege privilege in privileges.Values) { // method parameter "privileges" 
                if (privilege.TargetEntityType == entityType) result.Add(privilege);
            }
            return result.ToArray();
            //entityType = entityType.TopType; // privileges defined for subtypes are not supported
            //return Get(entityType, operation, privileges.Values); // static member "privileges"
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns all privileges related to the specified entity type that match the specified operations.</summary>
        /// <param name="entityType">The entity type to which the privileges apply.</param>
        /// <param name="operations">The operations on the entity type.</param>
        /// <returns>An array of matching Privilege instances.</returns>
        public static Privilege[] Get(EntityType entityType, IEnumerable<EntityOperationType> operations) {
            List<Privilege> result = new List<Privilege>();
            List<EntityOperationType> operationsList = new List<EntityOperationType>(operations);
            entityType = entityType.TopType; // privileges defined for subtypes are not supported
            foreach (Privilege privilege in privileges.Values) { // method parameter "privileges" 
                if (privilege.TargetEntityType == entityType && operationsList.Contains(privilege.Operation)) result.Add(privilege);
            }
            return result.ToArray();
            //entityType = entityType.TopType; // privileges defined for subtypes are not supported
            //return Get(entityType, operation, privileges.Values); // static member "privileges"
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the privilege defined by the specified entity type and operation.</summary>
        /// <param name="entityType">The entity type to which the privilege applies.</param>
        /// <param name="operation">The operation on the entity type.</param>
        /// <returns>The matching Privilege instance.</returns>
        public static Privilege Get(EntityType entityType, EntityOperationType operation) {
            entityType = entityType.TopType; // privileges defined for subtypes are not supported
            return Get(entityType, operation, privileges.Values); // static member "privileges"
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the privilege defined by the specified entity type and operation from a list.</summary>
        /// <param name="entityType">The entity type to which the privilege applies.</param>
        /// <param name="operation">The operation on the entity type.</param>
        /// <param name="privileges">An IEnumerable containing privileges to choose from.</param>
        /// <returns>The matching Privilege instance.</returns>
        public static Privilege Get(EntityType entityType, EntityOperationType operation, IEnumerable<Privilege> privileges) {
            entityType = entityType.TopType; // privileges defined for subtypes are not supported
            foreach (Privilege privilege in privileges) { // method parameter "privileges" 
                if (privilege.TargetEntityType == entityType && privilege.Operation == operation) return privilege;
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the privilege defined by the specified entity type and operation from a list.</summary>
        /// <param name="entityType">The entity type to which the privilege applies.</param>
        /// <param name="operation">The operation on the entity type.</param>
        /// <param name="privileges">An IEnumerable containing privileges to choose from. It should contain only privileges applicable to the same type with only one occurence of the requested operation.</param>
        /// <returns>The matching Privilege instance.</returns>
        public static Privilege Get(EntityOperationType operation, IEnumerable<Privilege> privileges) {
            foreach (Privilege privilege in privileges) { // method parameter "privileges" 
                if (privilege.Operation == operation) return privilege;
            }
            return null;
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
                case 'u':
                    return EntityOperationType.Use;
                case 'm':
                    return EntityOperationType.Change;
                case 'M':
                    return EntityOperationType.Manage;
                case 'd':
                    return EntityOperationType.Delete;
                case 'p':
                    return EntityOperationType.Share;
            }

            return EntityOperationType.Other;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Enumeration of generic operations that, in combination with entity types, are used to define privileges.</summary>
    public enum EntityOperationType {

        /// <summary>Create a new entity item.</summary>
        /// <remarks>This operation creates new domain-owned or global entity items according to his grant.</remarks>
        Create = 'c',

        /// <summary>List and search entity items.</summary>
        /// <remarks>This operation selects lists of entity items that are part of his grant and to search within these lists.</remarks>
        Search = 's',

        /// <summary>View an entity item.</summary>
        /// <remarks>This operation selects the details of entity items that are part of a user's grant. The <c>View</c> privilege on an entity type is implied by all other privileges on that entity type except <c>Search</c>.</remarks>
        View = 'v',

        /// <summary>Use an entity item for its main purpose.</summary>
        /// <remarks>This operation is applicable to entity types that represent resources that can be consumed by users (e.g. processing services or computing resources).</remarks>
        Use = 'u',

        /// <summary>Change an existing entity item.</summary>
        /// <remarks>This operation makes persistent modifications to entity items that are part a user's grant.</remarks>
        Change = 'm',

        /// <summary>Use an entity item in the same way as its owner and manage or control it.</summary>
        /// <remarks>This operation is a for all sorts of entity-type-specific operations. The <c>Manage</c> privilege on an entity type implies the Change privilege on that entity type and, in addition, allows a user to influence what other users can do regarding entity items within his grant. Typical operations include changes to availability and the assignment of permissions to users or groups.</remarks>
        Manage = 'M',

        /// <summary>Make an entity item available to others.</summary>
        /// <remarks>This operation definitely removes entity items that are part of a user's grant from the database.</remarks>
        Delete = 'd',

        /// <summary>Share an entity by making it accessible to other users.</summary>
        /// <remarks>This operation makes an entity item accessible to other users. Unlike the other operations it is ignored if it is used in a role privilege, i.e. a user's <em>share</em> privileges are not checked.</remarks>
        Share = 'p', // "p" for "part" or "publish"

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

