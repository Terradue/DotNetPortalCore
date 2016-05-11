using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Terradue.Metadata.OpenSearch;
using Terradue.Util;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Entity</summary>
    /// <description>
    /// Generic object of all entity types that usually correspond to real-world entities.
    /// The object provides generic interaction with data that is persistently stored in a relational database.
    /// The data location and structure are defined in the extended object which represent real-world entities.
    /// </description>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    public abstract partial class Entity {

        protected IfyContext context;
        private int domainId;
        private Domain domain;
        private int userId;
        private Privilege[] itemPrivileges;
        private EntityCollection collection;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the reference to the entity type of this entity item.</summary>
        public EntityType EntityType { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the EntityAccessLevel by which this entity item was created or loaded.</summary>
        public EntityAccessLevel AccessLevel { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>The database ID, i.e. the numeric primary key value of this entity item.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public int Id { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides (protected) whether a database record exists for this item.</summary>
        public bool Exists { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Unique identifier of this entity item.</summary>
        /// <remarks>
        ///     The identifier must be unique among all items of an entity type. It can be a meaningful string describing the item, similar to variable identifiers in programming languages, or a machine-generated Universally Unique Identifier (UUID).
        ///     It should be short and usable in RESTful URLs. Therefore it should not contain spaces or special characters, except those often found in URLs, such as hyphens or underscores.
        ///     Not all entity types require string identifiers, in some cases the numeric <see cref="Id"/> is sufficient. If the corresponding <see cref="EntityTableAttribute.IdentifierField"/> of the Entity subclass is unset, the property value is ignored when an item is stored and <c>null</c> when it is loaded.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string Identifier { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Human-readable name of this entity item item.</summary>
        /// <remarks>
        ///     The value of this property a short text corresponding to a title or caption of the item, according to the nature of the entity type. It should be of a length that fits without line break in a table cell so that it can be displayed easily in lists.
        ///     If subclass refer to the human-readable name as something different (e.g. <c>Title</c>, <c>Caption</c>, <c>HumanReadableName</c> or similar), it can be helpful for users of those classes to define such a property as a proxy property for <c>Name></c>, i.e. reading from and writing to <c>Name</c>.
        ///     Not all entity types require human-readable names. If the corresponding <see cref="EntityTableAttribute.NameField"/> of the Entity subclass is unset, the property value is ignored when an item is stored and <c>null</c> when it is loaded.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual string Name { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the ID of the domain to which the item belongs.</summary>
        /// <remarks>
        ///     The value is only meaningful if the related entity type has a domain reference. The value <c>0</c> means that this item belongs to no domain and is considered "global", requiring appropriate privileges.
        ///     If the corresponding <see cref="EntityTableAttribute.DomainReferenceField"/> of the Entity subclass is unset, the property value is ignored when an item is stored and <c>0</c> when it is loaded.
        ///     Otherwise, the value <c>0</c> means that this item belongs to no domain and is "global"
        /// </remarks>
        public virtual int DomainId {
            get {
                return (domain == null ? domainId : domain.Id); 
            }
            set {
                if (value != domainId) domain = null;
                domainId = value;
            }
        }

        /// <summary>Gets or sets the domain to which the item belongs.</summary>
        /// <remarks>
        ///     The value is only meaningful if the related entity type has a domain reference. The <c>null</c> value means that this item belongs to no domain and is considered "global", requiring appropriate privileges.
        ///     If the corresponding <see cref="EntityTableAttribute.DomainReferenceField"/> of the Entity subclass is unset, the property value is ignored when an item is stored and <c>null</c> when it is loaded.
        /// </remarks>
        /// <description>Domain to which the item belongs.</description>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        public virtual Domain Domain {
            get {
                if (domain == null && domainId != 0) domain = Domain.FromId(context, domainId);
                return domain;
            }
            set {
                domain = value;
                if (value == null) domainId = 0;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the ID of the owner of the item.</summary>
        /// <remarks>
        ///     The owner is the user who owns the item, this information is stored in the database, usually in a column named <c>id_usr</c>. Note that some entity types do not foresee item ownership. In these cases the value of this property has no meaning.
        ///     Do not confuse with UserId.
        /// </remarks>
        public virtual int OwnerId { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the ID of the user who is the subject to restrictions.</summary>
        /// <remarks>The value of this property is used for obtaining privilege information. By default this is the requesting user (e.g. currently logged in on the website). Do not confuse with OwnerId.</remarks>
        public virtual int UserId {
            get {
                return userId;
            }
            set {
                userId = value;
                itemPrivileges = null; // force reload of user's item privileges the next time a privilege value is requested
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the privileges the user has on this item.</summary>
        public Privilege[] ItemPrivileges {
            get {
                if (itemPrivileges == null) itemPrivileges = Role.GetUserPrivileges(context, UserId, this);
                return itemPrivileges;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (only once) the EntityCollection instance that was used to create this item, if applicable.</summary>
        public EntityCollection Collection { 
            get {
                return collection;
            }
            set {
                if (collection != null) throw new InvalidOperationException("The initial collection of an entity item cannot be changed");
                collection = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether an item contained in an entity collection is still part of its original collection.</summary>
        /// <remarks>This property has an effect only if the Entity instance was created by an EntityCollection.</remarks>
        public bool IsInCollection { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the current user is authorized to view this item.</summary>
        /// <remarks>The current user is the one with UserId as its database ID.</remarks>
        public virtual bool CanView {
            get {
                if (AccessLevel == EntityAccessLevel.Administrator) return true;
                if (AccessLevel == EntityAccessLevel.Privilege) {
                    if (Privilege.Get(EntityType, EntityOperationType.View) == null) return true;
                    return Privilege.Get(EntityOperationType.View, ItemPrivileges) != null;
                }
                return false;
            } 
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates whether the current user is authorized to persistently store this item as a new record in the database.</summary>
        /// <remarks>
        ///     CanCreate and CanChange are complementary, CanCreate is only meaningful if the item does not exist yet in the database. 
        ///     The property CanStore determines whether the current user is authorized to either create the record or change it, depending on whether the record already exists. The current user is the one with UserId as its database ID.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual bool CanCreate {
            get {
                if (AccessLevel == EntityAccessLevel.Administrator) return true;
                if (AccessLevel == EntityAccessLevel.Privilege) {
                    if (Privilege.Get(EntityType, EntityOperationType.Create) == null) return true;
                    return Privilege.Get(EntityOperationType.Create, ItemPrivileges) != null;
                }
                return false;
            } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the current user is authorized to persistently store the modifications applied to this item's database record.</summary>
        /// <remarks>
        ///     CanCreate and CanChange are complementary, CanChange is only meaningful if the item already exists yet in the database. 
        ///     The property CanStore determines whether the current user is authorized to either create the record or change it, depending on whether the record already exists. The current user is the one with UserId as its database ID.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual bool CanChange { 
            get {
                if (AccessLevel == EntityAccessLevel.Administrator) return true;
                if (AccessLevel == EntityAccessLevel.Privilege) {
                    if (Privilege.Get(EntityType, EntityOperationType.Change) == null) return true;
                    return Privilege.Get(EntityOperationType.Change, ItemPrivileges) != null;
                }
                return false;
            }
        }

        [Obsolete("Obsolete, please use CanChange instead")]
        public bool CanModify { 
            get { return CanChange; }
            set { }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the current user is authorized to either create a new database record for this item or change it, depending on whether it already exists.</summary>
        public virtual bool CanStore {
            get { return !Exists && CanCreate || Exists && CanChange; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the current user is authorized to perform operations such as assinging permissions on this item.</summary>
        /// <remarks>The current user is the one with UserId as its database ID.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual bool CanManage { 
            get {
                if (AccessLevel == EntityAccessLevel.Administrator) return true;
                if (AccessLevel == EntityAccessLevel.Privilege) {
                    if (Privilege.Get(EntityType, EntityOperationType.Manage) == null) return true;
                    return Privilege.Get(EntityOperationType.Manage, ItemPrivileges) != null;
                }
                return false;
            } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the current user is authorized to permanently delete this item from the database.</summary>
        /// <remarks>The current user is the one with UserId as its database ID.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual bool CanDelete { 
            get {
                if (AccessLevel == EntityAccessLevel.Administrator) return true;
                if (AccessLevel == EntityAccessLevel.Privilege) {
                    if (Privilege.Get(EntityType, EntityOperationType.Delete) == null) return true;
                    return Privilege.Get(EntityOperationType.Delete, ItemPrivileges) != null;
                }
                return false;
            } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string AlternativeIdentifyingCondition { 
            get { return null; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string TextContent { 
            get { return null; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new Entity instance.</summary>
        /// <param name="context">The execution environment context.</param>
        protected Entity(IfyContext context) {
            this.context = context;
            if (!(this is EntityType)) this.EntityType = EntityType.GetOrAddEntityType(this.GetType());
            if (context != null) {
                this.AccessLevel = context.AccessLevel;
                this.UserId = context.UserId;
                this.OwnerId = UserId;
                InitializeRelationships(context);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void InitializeRelationships(IfyContext context) {
            EntityType entityType = this.EntityType;
            foreach (FieldInfo field in entityType.Fields) {
                if (field.FieldType == EntityFieldType.RelationshipField) {
                    ConstructorInfo ci = field.Property.PropertyType.GetConstructor(new Type[] {
                        typeof(IfyContext),
                        typeof(EntityType),
                        typeof(Entity)
                    });
                    EntityRelationshipType entityRelationshipType = EntityRelationshipType.GetOrAddEntityRelationshipType(field.Property);
                    object o = ci.Invoke(new object[] { context, entityRelationshipType, this });
                    field.Property.SetValue(this, o, null);
                }
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Reads the information of the item with the specified ID from the database.</summary>
        public virtual void Load(int id) {
            Id = id;
            Identifier = null;
            Name = null;
            Domain = null;
            Load();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Reads the information of the item with the specified ID from the database.</summary>
        public virtual void Load(string identifier) {
            Id = 0;
            Identifier = identifier;
            Name = null;
            Domain = null;
            Load();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Reads the information of an item from the database based on the permissions of the current user.</summary>
        /// </remarks>
        ///     The method performs the necessary <c>SELECT</c> command to obtain an item of a derived class of Entity from the database.
        ///     The database table(s) and fields to be used must be linked to the corresponding class(es) and its/their properties via the appropriate attributes.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"   
        public virtual void Load() {
            Load(context.AccessLevel);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Reads the information of an item from the database based on the permissions of the current user and the specified access level.</summary>
        /// <param name="accessLevel">The strictness of permission and privilege checks to be applied.</param>
        public virtual void Load(EntityAccessLevel accessLevel) {
            EntityType entityType = this.EntityType;

            // Build WHERE clause
            string condition = null;
            if (Id != 0) condition = String.Format("t.{0}={1}", entityType.TopTable.IdField, Id);
            else if (entityType.TopTable.HasIdentifierField && Identifier != null) condition = String.Format("t.{0}={1}", entityType.TopTable.IdentifierField, StringUtils.EscapeSql(Identifier));
            //else if (!entityType.TopTable.HasAutomaticIds) condition = String.Format("t.{0}={1}", entityType.TopTable.IdField, Id);
            else condition = AlternativeIdentifyingCondition;
            if (condition == null) throw new EntityNotAvailableException("No identifying attribute specified for item");

            // Do not restrict query (a single item is requested)
            //EntityQueryMode queryMode = context.AdminMode ? EntityQueryMode.Administrator : EntityQueryMode.Unrestricted;
            string sql = entityType.GetItemQuery(context, this, UserId, condition, accessLevel);
            
            if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader;
            try {
                reader = context.GetQueryResult(sql, dbConnection);
            } catch (Exception e) {
                Console.WriteLine("FAILING SQL: {0} - {1}", sql, e.Message);
                throw;
            }

            if (!reader.Read()) {
                context.CloseQueryResult(reader, dbConnection);
                string itemTerm = GetItemTerm();
                throw new EntityNotFoundException(String.Format("{0} not found", itemTerm), entityType, itemTerm);
            }

            bool authorized = Load(entityType, reader, accessLevel);

            context.CloseQueryResult(reader, dbConnection);

            if (context.ConsoleDebug) Console.WriteLine("AFTER LOAD");
            
            if (!authorized) {
                string message = String.Format("{0} is not authorized to use {1} ({2})",
                    UserId == 0 ? "Unauthenticated user" : UserId == context.UserId ? String.Format("User \"{0}\"", context.Username) : String.Format("User [{0}]", UserId),
                    GetItemTerm(),
                    context.AccessLevel == EntityAccessLevel.Permission ? "no permission" : "no permission or grant"
                );

                throw new EntityUnauthorizedException(message, entityType, this, UserId);

                //if (context.RestrictedMode && !CanView) throw new EntityUnauthorizedException(String.Format("Not authorized to view {0} (no permission)", GetItemTerm()), entityType, this, UserId);
            }

            //canChange = context.AdminMode || UserId != 0 && UserId == OwnerId;
            //canDelete = context.AdminMode || UserId != 0 && UserId == OwnerId;
            
            /*if (context.ConsoleDebug) Console.WriteLine("BEFORE LCF");
            if (hasAutoLoadFields) LoadComplexFields(false);
            if (context.ConsoleDebug) Console.WriteLine("AFTER LCF");*/
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Reads the information of an item using the specified IDataReader.</summary>
        /// <param name="entityType">The entity type of this entity item.</param>
        /// <param name="reader">The IDataReader from which the entity item is loaded.</param>
        /// <param name="accessLevel">The access level that was used to create the query for the reader. Depending on the query, the selected fields differ.</param>
        /// <returns><c>true</c> if the user is allowed to access the item or <c>false</c> otherwise.</returns>
        /// <remarks>
        ///     The method is called from other core methods that build the appropriate query. It should not be called directly by other code unless the query is correctly built using one of the methods in EntityType class that provide queries.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public bool Load(EntityType entityType, IDataReader reader, EntityAccessLevel accessLevel) {
            //if (queryMode == EntityQueryMode.Default) queryMode = context.AdminMode ? EntityQueryMode.Administrator : context.RestrictedMode ? EntityQueryMode.Restricted : EntityQueryMode.Unrestricted;
            if (accessLevel == EntityAccessLevel.None) accessLevel = context.AccessLevel;
            bool includePermissions = accessLevel != EntityAccessLevel.Administrator && entityType.HasPermissionManagement;
            int index = 0;

            Id = reader.GetInt32(index++);
            Exists = true;

            if (entityType.TopTable.HasIdentifierField) Identifier = context.GetValue(reader, index++);
            if (entityType.TopTable.HasNameField) Name = context.GetValue(reader, index++);
            if (entityType.TopTable.HasDomainReference) DomainId = context.GetIntegerValue(reader, index++);
            if (entityType.TopTable.HasOwnerReference) OwnerId = context.GetIntegerValue(reader, index++);
            else OwnerId = 0;
            bool hasPrivilege = (accessLevel == EntityAccessLevel.Administrator || context.GetBooleanValue(reader, index++));
            bool hasPermission = true;
            if (entityType.HasPermissionManagement) {
                bool hasUserPermission, hasGroupPermission = false, hasGlobalPermission = false;
                if (accessLevel != EntityAccessLevel.Administrator) {
                    hasUserPermission = context.GetBooleanValue(reader, index++);
                    hasGroupPermission = context.GetBooleanValue(reader, index++);
                    hasGlobalPermission = context.GetBooleanValue(reader, index++);
                    hasPermission = hasUserPermission || hasGroupPermission || hasGlobalPermission;
                }
            }
            int firstCustomFieldIndex = index;

            try {
                foreach (FieldInfo field in entityType.Fields) {
                    // Skip permission fields in admin mode
                    if (field.FieldType != EntityFieldType.PermissionField && field.FieldType != EntityFieldType.DataField && field.FieldType != EntityFieldType.ForeignField) continue;
                    if (!includePermissions && field.FieldType == EntityFieldType.PermissionField) continue;

                    SetPropertyValue(field.Property, reader, index++);
                }
            } catch (Exception) {
                index = firstCustomFieldIndex;
                foreach (FieldInfo field in entityType.Fields) {
                    // Skip permission fields in admin mode
                    if (field.FieldType != EntityFieldType.PermissionField && field.FieldType != EntityFieldType.DataField && field.FieldType != EntityFieldType.ForeignField) continue;
                    if (!includePermissions && field.FieldType == EntityFieldType.PermissionField) continue;

                    try {
                        SetPropertyValue(field.Property, reader, index++);
                    } catch (Exception) {}
                }
            }

            index = 0;
            if (context.ConsoleDebug) {
                Console.WriteLine("+ VALUE: {0,-25} = {1}", "Id", reader.GetInt32(index++));
                if (entityType.TopTable.HasIdentifierField) Console.WriteLine("- VALUE: {0,-25} = {1}", "Identifier", context.GetValue(reader, index++));
                if (entityType.TopTable.HasNameField) Console.WriteLine("- VALUE: {0,-25} = {1}", "Name", context.GetValue(reader, index++));
                if (entityType.TopTable.HasDomainReference) Console.WriteLine("- VALUE: {0,-25} = {1}", "DomainId", context.GetIntegerValue(reader, index++));
                if (entityType.TopTable.HasOwnerReference) Console.WriteLine("- VALUE: {0,-25} = {1}", "OwnerId", context.GetIntegerValue(reader, index++));
                if (accessLevel != EntityAccessLevel.Administrator) Console.WriteLine("- VALUE: {0,-25} = {1}", "HasPrivilege", context.GetBooleanValue(reader, index++));
                if (accessLevel != EntityAccessLevel.Administrator && entityType.HasPermissionManagement/* && Restricted*/) { // TODO
                    Console.WriteLine("- VALUE: {0,-25} = {1}", "UserAllow", context.GetBooleanValue(reader, index++));
                    Console.WriteLine("- VALUE: {0,-25} = {1}", "GroupAllow", context.GetBooleanValue(reader, index++));
                    Console.WriteLine("- VALUE: {0,-25} = {1}", "GlobalAllow", context.GetBooleanValue(reader, index++));
                }
                foreach (FieldInfo field in entityType.Fields) {
                    if (field.FieldType != EntityFieldType.PermissionField && field.FieldType != EntityFieldType.DataField && field.FieldType != EntityFieldType.ForeignField) continue;
                    if (!includePermissions && field.FieldType == EntityFieldType.PermissionField) continue;
                    Console.WriteLine("- VALUE: {0,-25} = {1}", field.Property.Name, context.GetValue(reader, index++));
                }
            }

            if (accessLevel == EntityAccessLevel.Administrator) AccessLevel = EntityAccessLevel.Administrator;
            else if (accessLevel == EntityAccessLevel.Privilege) AccessLevel = hasPrivilege ? EntityAccessLevel.Privilege : hasPermission ? EntityAccessLevel.Permission : EntityAccessLevel.None;
            else if (accessLevel == EntityAccessLevel.Permission) AccessLevel = hasPermission ? EntityAccessLevel.Permission : EntityAccessLevel.None;
            else AccessLevel = EntityAccessLevel.None;

            itemPrivileges = null;

            return hasPermission || hasPrivilege;
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void SetPropertyValue(PropertyInfo propertyInfo, IDataReader reader, int index) {
            if (propertyInfo.PropertyType == typeof(string)) propertyInfo.SetValue(this, context.GetValue(reader, index), null);
            else if (propertyInfo.PropertyType == typeof(bool)) propertyInfo.SetValue(this, context.GetBooleanValue(reader, index), null);
            else if (propertyInfo.PropertyType == typeof(int)) propertyInfo.SetValue(this, context.GetIntegerValue(reader, index), null);
            else if (propertyInfo.PropertyType == typeof(long)) propertyInfo.SetValue(this, context.GetLongIntegerValue(reader, index), null);
            else if (propertyInfo.PropertyType == typeof(double)) propertyInfo.SetValue(this, context.GetDoubleValue(reader, index), null);
            else if (propertyInfo.PropertyType == typeof(DateTime)) propertyInfo.SetValue(this, context.GetDateTimeValue(reader, index), null);
            else if (propertyInfo.PropertyType.IsEnum) propertyInfo.SetValue(this, Enum.ToObject(propertyInfo.PropertyType, context.GetIntegerValue(reader, index)), null);
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the item to the database.</summary>
        /// <remarks>
        ///     The method performs the necessary <c>INSERT</c> or <c>UPDATE</c> command(s) to represent the item in the database.
        ///     The database tables and fields to be used must be linked to the derived class of Entity and its properties via the appropriate attributes.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void Store() {
            Store(null, null);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Store the specified entityRelationshipType and referringItem.</summary>
        /// <param name="entityRelationshipType">Entity relationship type.</param>
        /// <param name="referringItem">Referring item.</param>
        public virtual void Store(EntityRelationshipType entityRelationshipType, Entity referringItem) {
            EntityType entityType = (entityRelationshipType == null ? this.EntityType : entityRelationshipType);
            bool hasAutoStoreFields = false;
            
            if (!CanStore) throw new EntityUnauthorizedException(String.Format("Not authorized to {0} {1}", Exists ? "change" : "create", GetItemTerm()), EntityType, this, UserId);

            // Check whether identifier or name already exists
            if (entityType.TopTable == entityType.TopStoreTable && entityType.TopTable.HasIdentifierField && entityType.AutoCheckIdentifiers) {
                if (!Exists && entityType.AutoCorrectDuplicateIdentifiers) {
                    bool finding = true;
                    int suffix = 0;
                    while (finding) {
                        if (context.GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM {0} WHERE {1}={2};", entityType.TopTable.Name, entityType.TopTable.IdentifierField, StringUtils.EscapeSql(String.Format("{0}{1}", Identifier, suffix == 0 ? String.Empty : suffix.ToString())))) == 0) {
                            finding = false;
                        } else {
                            suffix++;
                        }
                    }
                    if (suffix != 0) Identifier = String.Format("{0}{1}", Identifier, suffix);
                } else {
                    if (context.GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM {0} WHERE {1}!={2} AND {3}={4};", entityType.TopTable.Name, entityType.TopTable.IdField, Id, entityType.TopTable.IdentifierField, StringUtils.EscapeSql(Identifier))) != 0) {
                        throw new DuplicateEntityIdentifierException(String.Format("A different {0} named '{1}' already exists", entityType.SingularCaption, Identifier), null);
                    }
                }
            }

            //Log activity
            Activity activity = null;
            
            // Do the INSERT if the item does not yet exist (1), or an UPDATE if it exists (2)
            // Note: the domain is only stored when the item is created
            if (!Exists || entityRelationshipType != null && referringItem != null) { // (1) - INSERT
                for (int i = entityType.TopStoreTableIndex; i < entityType.Tables.Count; i++) {
                    string names = null;
                    string values = null;
                    if (i == 0) {
                        if (!entityType.TopTable.HasAutomaticIds) {
                            if (names == null) {
                                names = String.Empty;
                                values = String.Empty;
                            } else {
                                names += ", ";
                                values += ", ";
                            }
                            names += entityType.TopTable.IdField;
                            values += Id.ToString();
                        }
                        if (entityType.TopTable.HasExtensions) {
                            if (names == null) {
                                names = String.Empty;
                                values = String.Empty;
                            } else {
                                names += ", ";
                                values += ", ";
                            }
                            if (entityType.TopTable.TypeReferenceField != null) {
                                names += entityType.TopTable.TypeReferenceField;
                                values += entityType.PersistentTypeId.ToString();
                            } else {
                                names += entityType.TopTable.TypeField;
                                values += entityType.ClassName;
                            }
                        }
                        if (entityType.TopTable.HasIdentifierField) {
                            if (names == null) {
                                names = String.Empty;
                                values = String.Empty;
                            } else {
                                names += ", ";
                                values += ", ";
                            }
                            names += entityType.TopTable.IdentifierField;
                            values += StringUtils.EscapeSql(Identifier);
                        }
                        if (entityType.TopTable.HasNameField) {
                            if (names == null) {
                                names = String.Empty;
                                values = String.Empty;
                            } else {
                                names += ", ";
                                values += ", ";
                            }
                            names += entityType.TopTable.NameField;
                            values += StringUtils.EscapeSql(Name);
                        }
                        if (entityType.TopTable.HasDomainReference) {
                            if (names == null) {
                                names = String.Empty;
                                values = String.Empty;
                            } else {
                                names += ", ";
                                values += ", ";
                            }
                            names += entityType.TopTable.DomainReferenceField;
                            values += (DomainId == 0 ? "NULL" : DomainId.ToString());
                        }

                        if (context.OwnerId != 0) OwnerId = context.OwnerId;
                        if (entityType.TopTable.HasOwnerReference) {
                            if (names == null) {
                                names = String.Empty;
                                values = String.Empty;
                            } else {
                                names += ", ";
                                values += ", ";
                            }
                            names += entityType.TopTable.OwnerReferenceField;
                            values += (OwnerId == 0 ? "NULL" : OwnerId.ToString());
                        }

                    } else {
                        names = entityType.Tables[i].IdField;
                        values = Id.ToString();
                    }
                    
                    foreach (FieldInfo field in entityType.Fields) {
                        /*if (field.FieldType == EntityFieldType.ComplexField && field.AutoStore) {
                            hasAutoStoreFields = true;
                            continue;
                        }*/
                        if (field.TableIndex != i || field.FieldType != EntityFieldType.DataField || field.IsReadOnly) continue;

                        object value = field.Property.GetValue(this, null);
                        if (value == null) continue;

                        if (names == null) {
                            names = String.Empty;
                            values = String.Empty;
                        } else {
                            names += ", ";
                            values += ", ";
                        }

                        names += field.FieldName;
                        value = (field.IsForeignKey && value != null && value.Equals(0) || field.NullValue != null && field.NullValue.Equals(value) ? "NULL" : StringUtils.ToSqlString(value));
                        values += value;
                    }

                    if (entityRelationshipType != null && referringItem != null && i == entityRelationshipType.TopStoreTableIndex) {
                        names = String.Format("{0}, {1}", entityRelationshipType.TopStoreTable.ReferringItemField, names);
                        values = String.Format("{0}, {1}", referringItem.Id, values);
                    }

                    string sql = String.Format("INSERT INTO {0} ({1}) VALUES ({2});", entityType.Tables[i].Name, names, values);
                    if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);
                    IDbConnection dbConnection = context.GetDbConnection();
                    context.Execute(sql, dbConnection);
                    
                    // Get the database ID of the created record
                    if (Id == 0 && i == 0) {
                        Id = context.GetInsertId(dbConnection);
                        if (context.ConsoleDebug) Console.WriteLine("  -> ID = " + Id);
                    }
                    context.CloseDbConnection(dbConnection);
                }
                
                // Add view privilege to owner (if it was not created by an administrator)
                if (OwnerId != 0 && entityType.HasPermissionManagement && (AccessLevel != EntityAccessLevel.Administrator || context.IsImpersonating || OwnerId != UserId)) {
                    context.Execute(String.Format("INSERT INTO {3} (id_{2}, id_usr) VALUES ({0}, {1});", Id, OwnerId, entityType.PermissionSubjectTable.Name, entityType.PermissionSubjectTable.PermissionTable));
                }
                Exists = true;

                //activity
                activity = new Activity(context, this, EntityOperationType.Create);
                activity.Store();
                
            } else { // (2) - UPDATE
                for (int i = entityType.TopStoreTableIndex; i < entityType.Tables.Count; i++) {
                    string assignments = null;
                    if (i == 0) {
                        if (entityType.TopTable.HasIdentifierField) {
                            if (assignments == null) assignments = String.Empty;
                            else assignments += ", ";
                            assignments += String.Format("{0}={1}", entityType.TopTable.IdentifierField, StringUtils.EscapeSql(Identifier));
                        }
                        if (entityType.TopTable.HasNameField) {
                            if (assignments == null) assignments = String.Empty;
                            else assignments += ", ";
                            assignments += String.Format("{0}={1}", entityType.TopTable.NameField, StringUtils.EscapeSql(Name));
                        }
                        if (entityType.TopTable.HasOwnerReference) {
                            if (assignments == null) assignments = String.Empty;
                            else assignments += ", ";
                            assignments += String.Format("{0}={1}", entityType.TopTable.OwnerReferenceField, OwnerId == 0 ? "NULL" : OwnerId.ToString());
                        }
                    }
                    
                    foreach (FieldInfo field in entityType.Fields) {
                        if (field.TableIndex != i || field.FieldType != EntityFieldType.DataField || field.IsReadOnly) continue;
                        if (assignments == null) assignments = String.Empty;
                        else assignments += ", ";
                        object value = field.Property.GetValue(this, null);
                        value = (field.IsForeignKey && value != null && value.Equals(0) || field.NullValue != null && field.NullValue.Equals(value) ? "NULL" : StringUtils.ToSqlString(value)); 
                        assignments += String.Format("{0}={1}", field.FieldName, value); 
                    }

                    if (assignments != null) {
                        string sql = String.Format("UPDATE {0} SET {3} WHERE {1}={2};", entityType.Tables[i].Name, entityType.Tables[i].IdField, Id, assignments);
                        if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);
                        context.Execute(sql);
                    }
                }
                //activity
                activity = new Activity(context, this, EntityOperationType.Change);
                activity.Store();
            }

            if (hasAutoStoreFields) StoreComplexFields(false);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the permissions for the user specified in the item's UserId property according to the permission properties.</summary>
        public void GrantPermissions() {
            GrantPermissions(false, UserId, null, false);
        }

        [Obsolete("Use GrantPermissions (changed for terminology consistency)")]
        public void StorePrivileges() {
            GrantPermissions();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the permissions on the resource represented by this instance for the specified users according to the permission properties.</summary>
        /// <remarks>This method allows managing permissions from the resource's point of view: one resource grants permissions to several users.</remarks>
        /// <param name="users">An array of users to which the permission setting applies.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void GrantPermissionsToUsers(IEnumerable<User> users) {
            List<int> userIds = new List<int>();
            foreach (User user in users) userIds.Add(user.Id);
            GrantPermissions(false, 0, userIds, false);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the permissions on the resource represented by this instance for the specified users according to the permission properties.</summary>
        /// <remarks>This method allows managing permissions from the resource's point of view: one resource grants permissions to several users.</remarks>
        /// <param name="userIds">An array of IDs of the users to which the permission setting applies.</param>
        public void GrantPermissionsToUsers(int[] userIds) {
            GrantPermissions(false, 0, userIds, false);
        }

        [Obsolete("Use GrantPermissionsToUsers (changed for terminology consistency)")]
        public void StorePrivilegesForUsers(int[] userIds) {
            GrantPermissionsToUsers(userIds);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the permissions on the resource represented by this instance for the specified users according to the permission properties.</summary>
        /// <remarks>This method allows managing permissions from the resource's point of view: one resource grants permissions to several users.</remarks>
        /// <param name="users">An array of users to which the permission setting applies.</param>
        /// <param name="removeOthers">Determines whether permission settings for other users not contained in <c>users</c> are removed.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void GrantPermissionsToUsers(IEnumerable<User> users, bool removeOthers) {
            List<int> userIds = new List<int>();
            foreach (User user in users) userIds.Add(user.Id);
            GrantPermissions(false, 0, userIds, removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the permissions on the resource represented by this instance for the specified users according to the permission properties.</summary>
        /// <remarks>This method allows managing permissions from the resource's point of view: one resource grants permissions to several users.</remarks>
        /// <param name="userIds">An array of IDs of the users to which the permission setting applies.</param>
        /// <param name="removeOthers">Determines whether permission settings for other users not contained in <c>userIds</c> are removed.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void GrantPermissionsToUsers(int[] userIds, bool removeOthers) {
            GrantPermissions(false, 0, userIds, removeOthers);
        }

        [Obsolete("Use GrantPermissionsToUsers (changed for terminology consistency)")]
        public void StorePrivilegesForUsers(int[] userIds, bool removeOthers) {
            GrantPermissionsToUsers(userIds, removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the permissions on the resource represented by this instance for the specified user groups according to the permission properties.</summary>
        /// <remarks>This method allows managing permissions from the resource's point of view: one resource grants permissions to several groups.</remarks>
        /// <param name="groups">An array of groups to which the permission setting applies.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void GrantPermissionsToGroups(IEnumerable<Group> groups) {
            List<int> groupIds = new List<int>();
            foreach (Group group in groups) groupIds.Add(group.Id);
            GrantPermissions(true, 0, groupIds, false);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the permissions on the resource represented by this instance for the specified user groups according to the permission properties.</summary>
        /// <remarks>This method allows managing permissions from the resource's point of view: one resource grants permissions to several groups.</remarks>
        /// <param name="groupIds">An array of IDs of the groups to which the permission setting applies.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void GrantPermissionsToGroups(int[] groupIds) {
            GrantPermissions(true, 0, groupIds, false);
        }

        [Obsolete("Use GrantPermissionsToGroups (changed for terminology consistency)")]
        public void StorePrivilegesForGroups(int[] groupIds) {
            GrantPermissionsToGroups(groupIds);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the permissions on the resource represented by this instance for the specified user groups according to the permission properties.</summary>
        /// <remarks>This method allows managing permissions from the resource's point of view: one resource grants permissions to several groups.</remarks>
        /// <param name="groups">An array of groups to which the permission setting applies.</param>
        /// <param name="removeOthers">Determines whether permission settings for other groups not contained in <c>groups</c> are removed.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void GrantPermissionsToGroups(IEnumerable<Group> groups, bool removeOthers) {
            List<int> groupIds = new List<int>();
            foreach (Group group in groups) groupIds.Add(group.Id);
            GrantPermissions(true, 0, groupIds, removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the permissions on the resource represented by this instance for the specified user groups according to the permission properties.</summary>
        /// <remarks>This method allows managing permissions from the resource's point of view: one resource grants permissions to several groups.</remarks>
        /// <param name="groupIds">An array of IDs of the groups to which the permission setting applies.</param>
        /// <param name="removeOthers">Determines whether permission settings for other groups not contained in <c>groupIds</c> are removed.</param>
        public void GrantPermissionsToGroups(int[] groupIds, bool removeOthers) {
            GrantPermissions(true, 0, groupIds, removeOthers);
        }

        [Obsolete("Use GrantPermissionsToGroups (changed for terminology consistency)")]
        public void StorePrivilegesForGroups(int[] groupIds, bool removeOthers) {
            GrantPermissionsToGroups(groupIds, removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the permissions on the resource represented by this instance that will apply to all users according to the permission properties.</summary>
        /// <remarks>
        ///     This method allows managing permissions from the resource's point of view: one resource grants permissions to everybody.
        ///     The permissions previously defined for users and groups are kept but, since the global permission settings override all finer grained settings, those have only effect for the permissions that are not allowed by the global permission.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void GrantGlobalPermissions() {
            GrantPermissions(false, 0, null, false);

            //activity
            Activity activity = new Activity(context, this, EntityOperationType.Share);
            activity.Store();
        }
        
        [Obsolete("Use GrantGlobalPermissions (changed for terminology consistency)")]
        public void StoreGlobalPrivileges() {
            GrantGlobalPermissions();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets global permissions on the resource represented by this instance that will apply to all users according to the permission properties.</summary>
        /// <remarks>
        ///     This method allows managing permissions from the resource's point of view: one resource grants permissions to everybody.
        ///     The permissions previously defined for users and groups are kept but, since the global permission settings override all finer grained settings, those have only effect for the permissions that are not allowed by the global permission.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void RevokeGlobalPermission() {
            context.Execute(String.Format("DELETE FROM {1} WHERE id_{2}={0} AND id_usr IS NULL AND id_grp IS NULL;", Id, EntityType.PermissionSubjectTable.PermissionTable, EntityType.PermissionSubjectTable.Name));
        }

        [Obsolete("Use RevokeGlobalPermission (changed for terminology consistency)")]
        public void RemoveGlobalPrivileges() {
            RevokeGlobalPermission();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Determines whether global permissions are granted for this resource.</summary>
        public bool DoesGrantGlobalPermission() {
            return context.GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM {1} WHERE id_{2}={0} AND id_usr IS NULL AND id_grp IS NULL;", Id, EntityType.PermissionSubjectTable.PermissionTable, EntityType.PermissionSubjectTable.Name)) > 0;
        }

        [Obsolete("Use DoesGrantGlobalPermission (changed for terminology consistency)")]
        public bool HasGlobalPrivilege() {
            return context.GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM {1} WHERE id_{2}={0} AND id_usr IS NULL AND id_grp IS NULL;", Id, EntityType.PermissionSubjectTable.PermissionTable, EntityType.PermissionSubjectTable.Name)) > 0;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets global permissions on the resource represented by this instance that will apply to all users according to the permission properties.</summary>
        /// <remarks>This method allows managing permissions from the resource's point of view: one resource grants permissions to everybody.</remarks>
        /// <param name="removeOthers">Determines whether permission settings at user and group level are removed.</param>
        public void GrantGlobalPermissions(bool removeOthers) {
            GrantPermissions(false, 0, null, removeOthers);
        }

        [Obsolete("Use AssignPermissionsGlobally (changed for terminology consistency)")]
        public void StoreGlobalPrivileges(bool removeOthers) {
            GrantGlobalPermissions(removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the permissions on the resource represented by this instance for the specified beneficiaries according to the permission properties.</summary>
        /// <param name="forGroup">If <c>true</c>, the methods considers the given IDs as group IDs, otherwise as user IDs.</param>
        /// <param name="singleId">The database ID of a single user or group, used alternatively with multipleIds.</param>
        /// <param name="multipleIds">An array of database IDs of multiple users or groups, used alternatively with singleId.</param>
        /// <param name="removeOthers">Determines whether permission settings at user and group level are removed.</param>
        protected void GrantPermissions(bool forGroup, int singleId, IEnumerable<int> multipleIds, bool removeOthers) {
            if (!CanManage) throw new EntityUnauthorizedException(String.Format("Not authorized to change permissions on {0}", GetItemTerm()), EntityType, this, UserId);

            EntityType entityType = this.EntityType;
            int permissionSubjectTableIndex = -1;

            for (int i = 0; i < entityType.Tables.Count; i++) {
                EntityTableAttribute table = entityType.Tables[i];
                if (table == entityType.PermissionSubjectTable) {
                    permissionSubjectTableIndex = i;
                    break;
                }
            }
            
            // Add entity-specific fields to SELECT clause
            string names = String.Format("id_{0}, id_usr, id_grp", entityType.PermissionSubjectTable.Name);
            string values = String.Empty;
            string totalValues = String.Empty;
            string deleteCondition = String.Empty;
            foreach (FieldInfo field in entityType.Fields) {
                if (field.TableIndex != permissionSubjectTableIndex || field.FieldType != EntityFieldType.PermissionField) continue;
                names += String.Format(", {0}", field.FieldName);
                object value = field.Property.GetValue(this, null);
                value = (field.NullValue != null && field.NullValue.Equals(value)) ? "NULL" : StringUtils.ToSqlString(value);
                values += String.Format(", {0}", value);
            }
            if (singleId != 0) {
                totalValues = String.Format("({0}, {1}, {2}{3})", Id, forGroup ? "NULL" : singleId.ToString(), forGroup ? singleId.ToString() : "NULL", values);
                if (removeOthers) deleteCondition = String.Format("{0} IS NOT NULL", forGroup ? "id_grp" : "id_usr");
                else deleteCondition = String.Format("{0}={1}", forGroup ? "id_grp" : "id_usr", singleId);
            } else if (multipleIds != null) {
                bool hasIds = false;
                foreach (int id in multipleIds) {
                    totalValues += String.Format("{4}({0}, {1}, {2}{3})", Id, forGroup ? "NULL" : id.ToString(), forGroup ? id.ToString() : "NULL", values, hasIds ? ", " : String.Empty);
                    if (!removeOthers) deleteCondition = String.Format("{2}{0}={1}", forGroup ? "id_grp" : "id_usr", singleId, hasIds ? ", " : String.Empty);
                    hasIds = true;
                }
                if (removeOthers) deleteCondition = String.Format("{0} IS NOT NULL", forGroup ? "id_grp" : "id_usr");
                else if (hasIds) deleteCondition = String.Format("{0} IN ({1})", forGroup ? "id_grp" : "id_usr", deleteCondition);
                else deleteCondition = "false";
            } else {
                totalValues = String.Format("({0}, NULL, NULL{1})", Id, values);
                if (removeOthers) deleteCondition = "true";
                else deleteCondition = "id_usr IS NULL AND id_grp IS NULL";
            }
            
            context.Execute(String.Format("DELETE FROM {1} WHERE id_{2}={0} AND ({3});", Id, entityType.PermissionSubjectTable.PermissionTable, entityType.PermissionSubjectTable.Name, deleteCondition));
            context.Execute(String.Format("INSERT INTO {0} ({1}) VALUES {2};", entityType.PermissionSubjectTable.PermissionTable, names, totalValues));
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets permissions on resources for a single user.</summary>
        /// <remarks>This method allows managing permissions from a single user's point of view: one user has permissions on several resources.</remarks>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The ID of the user for which permissions on entity items are provided.</param>
        /// <param name="items">An array containing the Entity instances with the permission values set according to the user's actual permissions.</param>
        /// <param name="removeOthers">Determines whether permission settings for other resources not contained in <c>items</c> are removed.</param>
        public static void GrantPermissionsToUser(IfyContext context, int userId, Entity[] items, bool removeOthers) {
            GrantPermissions(context, false, userId, items, removeOthers);
        }

        [Obsolete("Use GrantPermissionsToUser (changed for terminology consistency)")]
        public static void StorePrivilegesForUser(IfyContext context, int userId, Entity[] items, bool removeOthers) {
            GrantPermissionsToUser(context, userId, items, removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets permissions on resources for a single group.</summary>
        /// <remarks>This method allows managing permissions from a single group's point of view: one group has permissions on several resources.</remarks>
        /// <param name="context">The execution environment context.</param>
        /// <param name="groupId">The ID of the group for which permissions on entity items are provided.</param>
        /// <param name="items">An array containing the Entity instances with the permission values set according to the group's actual permissions.</param>
        /// <param name="removeOthers">Determines whether permission settings for other resources not contained in <c>items</c> are removed.</param>
        public static void GrantPermissionsToGroup(IfyContext context, int groupId, Entity[] items, bool removeOthers) {
            GrantPermissions(context, true, groupId, items, removeOthers);
        }

        [Obsolete("Use GrantPermissionsToGroup (changed for terminology consistency)")]
        public static void StorePrivilegesForGroup(IfyContext context, int groupId, Entity[] items, bool removeOthers) {
            GrantPermissionsToGroup(context, groupId, items, removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary></summary>
        /// <remarks></remarks>
        protected static void GrantPermissions(IfyContext context, bool forGroup, int id, Entity[] items, bool removeOthers) {
            if (removeOthers && items.Length != 0) {
                context.Execute(String.Format("DELETE FROM {0} WHERE {1}={2};", items[0].EntityType.PermissionSubjectTable.PermissionTable, forGroup ? "id_grp" : "id_usr", id));
            }
            foreach (Entity item in items) {
                if (context.ConsoleDebug) Console.WriteLine(String.Format("{0} {1} -> {2}", forGroup ? "grp" : "usr", id, item.Identifier));
                item.GrantPermissions(forGroup, id, null, false);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the database IDs of users who are authorized to view this entity.</summary>
        /// <returns>A list of database IDs of the authorized users.</returns>
        public int[] GetAuthorizedUserIds() {
            return GetAuthorizedSubjects(false);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the users who are authorized to view this entity.</summary>
        /// <returns>The authorized users.</returns>
        public List<User> GetAuthorizedUsers() {
            List<User> result = new List<User>();
            int[] userIds = GetAuthorizedSubjects(false);
            foreach (int id in userIds) result.Add(User.FromId(context, id));
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the database IDs of users who are authorized to view this entity.</summary>
        /// <returns>A list of database IDs of the authorized users.</returns>
        public int[] GetAuthorizedGroupIds() {
            return GetAuthorizedSubjects(true);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the groups who are authorized to view this entity.</summary>
        /// <returns>The authorized groups.</returns>
        public List<Group> GetAuthorizedGroups() {
            List<Group> result = new List<Group>();
            int[] groupIds = GetAuthorizedSubjects(true);
            foreach (int id in groupIds) result.Add(Group.FromId(context, id));
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the users or groups who are authorized to view this entity via role privilege or item-specific permission.</summary>
        /// <returns>The database IDs of the users or groups with view permission or privilege on this item. If <c>null</c> is returned, the item provides global permissions, i.e. all users are authorized to view the item.</returns>
        /// <param name="forGroups">Decides whether a list of group or user IDs is returned. If <c>true</c>, group IDs are returned, otherwise user IDs.</param>
        protected int[] GetAuthorizedSubjects(bool forGroups) {
            HashSet<int> ids = new HashSet<int>();

            if (EntityType.HasPermissionManagement) {
                string sql = String.Format("SELECT NULL FROM {1} WHERE id_{2}={0} AND id_usr IS NULL AND id_grp IS NULL;",
                    Id,
                    EntityType.PermissionSubjectTable.PermissionTable,
                    EntityType.PermissionSubjectTable.Name
                );
                IDbConnection dbConnection = context.GetDbConnection();
                IDataReader reader = context.GetQueryResult(sql, dbConnection);
                bool globalPermission = reader.Read();
                context.CloseQueryResult(reader);
                if (!globalPermission) {
                    sql = String.Format("SELECT {5} FROM {1} AS p{2} WHERE id_{3}={0} AND {4}",
                        Id,
                        EntityType.PermissionSubjectTable.PermissionTable,
                        forGroups ? String.Empty : " LEFT JOIN usr_grp AS ug ON p.id_grp=ug.id_grp",
                        EntityType.PermissionSubjectTable.Name,
                        forGroups ? "p.id_grp IS NOT NULL" : "(p.id_usr IS NOT NULL OR ug.id_usr IS NOT NULL)",
                        forGroups ? "p.id_grp" : "CASE WHEN p.id_usr IS NULL THEN ug.id_usr ELSE p.id_usr END"
                    );
                    reader = context.GetQueryResult(sql, dbConnection);
                    while (reader.Read()) ids.Add(reader.GetInt32(0));
                    context.CloseQueryResult(reader);
                }
                context.CloseDbConnection(dbConnection);
                if (globalPermission) return null;
            }

            // Get roles that have view access on item's domain (i.e. any other operation than Search)
            int[] roleIds = EntityType.GetRolesForPrivilege(context, EntityOperationType.Search, true);
            if (roleIds != null) {
                string domainCondition = EntityType.TopTable.HasDomainReference ? "true" : DomainId == 0 ? "rg.id_domain IS NULL" : String.Format("id_domain={0}", DomainId);
                if (roleIds.Length != 0) {
                    string sql = String.Format("SELECT DISTINCT {4} FROM rolegrant AS rg{3} WHERE rg.id_role IN ({0}) AND {1} AND {2} ",
                        String.Join(",", roleIds),
                        forGroups ? "rg.id_grp IS NOT NULL" : "(rg.id_usr IS NOT NULL OR ug.id_usr IS NOT NULL)",
                        domainCondition,
                        forGroups ? String.Empty : " LEFT JOIN usr_grp AS ug ON rg.id_grp=ug.id_grp",
                        forGroups ? "rg.id_grp" : "CASE WHEN rg.id_usr IS NULL THEN ug.id_usr ELSE rg.id_usr END"
                    );
                    IDbConnection dbConnection = context.GetDbConnection();
                    IDataReader reader = context.GetQueryResult(sql, dbConnection);
                    while (reader.Read()) ids.Add(reader.GetInt32(0));
                    context.CloseQueryResult(reader, dbConnection);
                }

            }

            int[] result = new int[ids.Count];
            ids.CopyTo(result);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the list of groups that have any permissions on this entity item.</summary>
        /// <returns>The database IDs of the groups with privileges.</returns>
        [Obsolete("Use GetAuthorizedGroupIds")]
        public List<int> GetGroupsWithPermissions() {
            List<int> ids = new List<int>();
            string sql = String.Format("SELECT id_grp FROM {1} WHERE id_{2}={0};", Id, EntityType.PermissionSubjectTable.PermissionTable, EntityType.PermissionSubjectTable.Name);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);

            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value) ids.Add(reader.GetInt32(0));
            }
            reader.Close();
            return ids;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the list of users that have any permissions on this entity item.</summary>
        /// <returns>The database IDs of the users with privileges.</returns>
        [Obsolete("Use GetAuthorizedUserIds")]
        public List<int> GetUsersWithPermissions() {
            List<int> ids = new List<int>();
            string sql = String.Format("SELECT id_usr FROM {1} WHERE id_{2}={0};", Id, EntityType.PermissionSubjectTable.PermissionTable, EntityType.PermissionSubjectTable.Name);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);

            while (reader.Read()) {
                if (reader.GetValue(0) != DBNull.Value) ids.Add(reader.GetInt32(0));
            }
            reader.Close();
            return ids;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Remove the entity from the database</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void Delete() {
            if (!Exists) throw new InvalidOperationException("Cannot delete, no item loaded");

            if (!CanDelete) throw new EntityUnauthorizedException(String.Format("Not authorized to delete {0}", GetItemTerm()), EntityType, this, UserId);

            //activity
            Activity activity = new Activity(context, this, EntityOperationType.Delete);
            activity.Store();

            EntityType entityType = this.EntityType;

            context.Execute(String.Format("DELETE FROM {0} WHERE {1}={2};", entityType.TopTable.Name, entityType.TopTable.IdField, Id));
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void LoadComplexFields(bool all) {
/*            if (context.ConsoleDebug) Console.WriteLine("LCF " + all);
            foreach (FieldInfo field in EntityType.Fields) {
                if (field.FieldType != EntityFieldType.ComplexField || !field.AutoLoad && !all) continue;
                if (field.IsList) {
                    ConstructorInfo constructorInfo = field.Property.PropertyType.GetConstructor(new Type[]{});
                    if (constructorInfo == null) {
                        constructorInfo = field.Property.PropertyType.GetConstructor(new Type[]{typeof(IfyContext)}); 
                        if (constructorInfo == null) continue;
                        field.Property.SetValue(this, constructorInfo.Invoke(new object[]{context}), null);
                    } else {
                        field.Property.SetValue(this, constructorInfo.Invoke(new object[]{}), null);
                    }
                    Type genericListType = typeof(EntityList<>);
                    Type concreteListType = genericListType.MakeGenericType(new Type[]{field.UnderlyingType});
                    constructorInfo = concreteListType.GetConstructor(new Type[]{typeof(IfyContext), typeof(Entity)});
                    object entityList = constructorInfo.Invoke(new object[]{context, this});
                    MethodInfo loadMethod = concreteListType.GetMethod("Load", new Type[]{});
                    loadMethod.Invoke(entityList, new object[]{});
                } else if (field.IsDictionary) {
                    ConstructorInfo constructorInfo = field.Property.PropertyType.GetConstructor(new Type[]{});
                    if (constructorInfo == null) {
                        constructorInfo = field.Property.PropertyType.GetConstructor(new Type[]{typeof(IfyContext)}); 
                        if (constructorInfo == null) continue;
                        field.Property.SetValue(this, constructorInfo.Invoke(new object[]{context}), null);
                    } else {
                        field.Property.SetValue(this, constructorInfo.Invoke(new object[]{}), null);
                    }
                    Type genericDictionaryType = typeof(EntityDictionary<>);
                    Type concreteDictionaryType = genericDictionaryType.MakeGenericType(new Type[]{typeof(string), field.UnderlyingType});
                    constructorInfo = concreteDictionaryType.GetConstructor(new Type[]{typeof(IfyContext), typeof(Entity)});
                    object entityDictionary = constructorInfo.Invoke(new object[]{context, this});
                    MethodInfo loadMethod = concreteDictionaryType.GetMethod("Load", new Type[]{});
                    loadMethod.Invoke(entityDictionary, new object[]{});
                } else {
                    //constructorInfo = field.Property.PropertyType.GetConstructor(new Type[]{typeof(IfyContext)}); 
                    //if (constructorInfo == null) continue;
                }
            }*/
        }
                
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void StoreComplexFields(bool all) {
/*            if (context.ConsoleDebug) Console.WriteLine("SCF " + all);
            foreach (FieldInfo field in EntityType.Fields) {
                if (field.FieldType != EntityFieldType.ComplexField || !field.AutoLoad && !all) continue;
                if (field.IsList) {
                    ConstructorInfo constructorInfo = field.Property.PropertyType.GetConstructor(new Type[]{});
                    if (constructorInfo == null) {
                        constructorInfo = field.Property.PropertyType.GetConstructor(new Type[]{typeof(IfyContext)}); 
                        if (constructorInfo == null) continue;
                        field.Property.SetValue(this, constructorInfo.Invoke(new object[]{context}), null);
                    } else {
                        field.Property.SetValue(this, constructorInfo.Invoke(new object[]{}), null);
                    }
                    Type genericListType = typeof(EntityList<>);
                    Type concreteListType = genericListType.MakeGenericType(new Type[]{field.UnderlyingType});
                    constructorInfo = concreteListType.GetConstructor(new Type[]{typeof(IfyContext), typeof(Entity)});
                    object entityList = constructorInfo.Invoke(new object[]{context, this});
                    if (context.ConsoleDebug) Console.WriteLine(concreteListType.Name);
                    MethodInfo storeMethod = concreteListType.GetMethod("Store", new Type[]{});
                    storeMethod.Invoke(entityList, new object[]{});
                } else if (field.IsDictionary) {
                    ConstructorInfo constructorInfo = field.Property.PropertyType.GetConstructor(new Type[]{});
                    if (constructorInfo == null) {
                        constructorInfo = field.Property.PropertyType.GetConstructor(new Type[]{typeof(IfyContext)}); 
                        if (constructorInfo == null) continue;
                        field.Property.SetValue(this, constructorInfo.Invoke(new object[]{context}), null);
                    } else {
                        field.Property.SetValue(this, constructorInfo.Invoke(new object[]{}), null);
                    }
                    Type genericDictionaryType = typeof(EntityDictionary<>);
                    Type concreteDictionaryType = genericDictionaryType.MakeGenericType(new Type[]{field.UnderlyingType});
                    constructorInfo = concreteDictionaryType.GetConstructor(new Type[]{typeof(IfyContext), typeof(Entity)});
                    object entityDictionary = constructorInfo.Invoke(new object[]{context, this});
                    if (context.ConsoleDebug) Console.WriteLine(concreteDictionaryType.Name);
                    MethodInfo storeMethod = concreteDictionaryType.GetMethod("Store", new Type[]{});
                    storeMethod.Invoke(entityDictionary, new object[]{});
                } else {
                    //constructorInfo = field.Property.PropertyType.GetConstructor(new Type[]{typeof(IfyContext)}); 
                    //if (constructorInfo == null) continue;
                }
            }*/
        }
                
        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Use Can* properties directly")]
        public virtual void GetAllowedAdministratorOperations() {
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a string representing the item for exception messages or similar.</summary>
        public string GetItemTerm() {
            EntityType entityType = this.EntityType;
            bool useIdentifier = entityType.TopTable.HasIdentifierField && Identifier != null;
            return String.Format("{0} {2}{1}{3}",
                entityType.SingularCaption == null ? entityType.ClassType.Name : entityType.SingularCaption,
                entityType.TopTable.HasIdentifierField ? Identifier : Id.ToString(),
                useIdentifier ? "\"" : "[",
                useIdentifier ? "\"" : "]"
            );
        }

    }


    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    
    

    public class EntityNotFoundException : Exception {

        public EntityType EntityType { get; protected set; }

        public string LoadTerm { get; protected set; }

        public EntityNotFoundException(string message) : base(message) {

        }

        public EntityNotFoundException(string message, EntityType entityType, string loadTerm) : base(message) {
            this.EntityType = entityType;
            this.LoadTerm = loadTerm;
        }

        public EntityNotFoundException(string message, Exception e) : base(message, e) {
        }
    }


    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class EntityUnauthorizedException : UnauthorizedAccessException {

        public EntityType EntityType { get; protected set; }

        public Entity Entity { get; protected set; }

        public int UserId { get; protected set; }

        public EntityUnauthorizedException(string message, EntityType entityType, Entity entity, int userId) : base(message) {
            this.EntityType = entityType;
            this.Entity = entity;
            this.UserId = userId;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class EntityUnavailableException : Exception {

        public EntityType EntityType { get; protected set; }

        public Entity Entity { get; protected set; }

        public EntityUnavailableException(string message, EntityType entityType, Entity entity) : base(message) {
            this.EntityType = entityType;
            this.Entity = entity;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class DuplicateEntityIdentifierException : Exception {

        public EntityType EntityType { get; protected set; }

        public Entity Entity { get; protected set; }

        public string Identifier { get; protected set; }

        public DuplicateEntityIdentifierException(string message, EntityType entityType, string identifier) : base(message) {
            this.EntityType = entityType;
            this.Identifier = identifier;
        }

        public DuplicateEntityIdentifierException(string message, Exception e) : base(message, e) {
        }
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class EntityNotAvailableException : Exception {
        public EntityNotAvailableException(string message) : base(message) {
        }
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class EntityNotOwnedException : Exception {
        public EntityNotOwnedException(string message) : base(message) {
        }
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    // IICD-WC-WS exception
    public class ResourceNotFoundException : Exception {
        public ResourceNotFoundException(string message) : base(message) {
        }
    }

    /*    public class EntityNotAccessibleException : Exception { 
        public EntityNotAccessibleException(string message) : base(message) {}
    }

    public class PermissionDeniedException : Exception { 
        public PermissionDeniedException(string message) : base(message) {}
    }

    public class BadValueException : Exception { 
        public BadValueException(string message) : base(message) {}
    }

    public class MissingParameterException : Exception { 
        public MissingParameterException(string message) : base(message) {}
    }

    public class IncorrectRequestFormatException : Exception { 
        public IncorrectRequestFormatException(string message) : base(message) {}
    }
*/



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Output types</summary>
    public enum OutputType {
        
        /// <summary><b>item list</b> output type</summary>
        List,

        /// <summary><b>single item</b> output type</summary>
        Item,

        /// <summary><b>composite item</b>, a special case of the <b>single item</b> output type</summary>
        Composite
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Generic operation types for entities</summary>
    public enum OperationType {
        /// <summary>View the item list</summary>
        ViewList,

        /// <summary>View a single item</summary>
        ViewItem,

        /// <summary>Define a new item</summary>
        Define,

        /// <summary>Create a new item</summary>
        Create,

        /// <summary>Modify an existing item</summary>
        Modify,

        /// <summary>Define an existing item</summary>
        Delete,

        /// <summary>Other operation</summary>
        Other
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public enum ViewingState {
        Unknown,
        ShowingList,
        DefiningItem,
        ShowingItem
    }

}
