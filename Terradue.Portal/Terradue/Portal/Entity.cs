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
using MySql.Data.MySqlClient;
using Terradue.Metadata.OpenSearch;
using Terradue.Util;

//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------

/*!

\defgroup Core Core
@{

The Core component is a set of library developed as the base implementation of a Content Management System (CMS) for EO world entities.
It implements basic subcomponent to deal with basic EO business objects such as dataset series, WPS service, user context, jobs...
It also implement the low level functions to store and read data persistently on the database or to apply a configuration.

@}

*/

/*!

\defgroup Security Security
@{

The Security component is a set of library in charge with all the authorisation or authentication functions and also with the privileges management 
between users, groups and other business objects.The security scheme is open and offers many possibilities to plug other component to
implement specific authentication mechanism or authorization scheme.

@}

*/

/*!
\defgroup Authorisation Authorisation
@{
It provides with the functions to define privileges for users or groups on entities for which restrictions are useful, such as entities that represent resources (computing resources or processing services etc.).

\ingroup Security

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence reads/writes the privileges persistently

\xrefitem dep "Dependencies" "Dependencies" uses \ref Context to identify the user and the session

The authorisation consists of two phases:
- a generic phase where the current user's access privileges are compared to the necessary privileges for the accessed resource
- an optional specific phase where the same check is performed for the requested operation. This phase is specific to the entity subclass in question as the possible operations are entity-specific.

If IfyContext.RestrictedMode is <em>true</em> (the default value) and the user has insufficient privileges to access an item, the item is not loaded and an exception is thrown immediately.
Otherwise, if IfyContext.RestrictedMode is <em>false</em>, the authorisation check needs to be done by the code that loaded the entity item. This code should check the CanView property of the loaded item and if its value is <em>false</em>, it may either continue or throw another, more appropriate, exception.
The latter procedure is also followed for the second phase that checks operation authorisations. The authorisation for a specific operation must be ensured by the code of the entity subclass. The central authorisation model supports this task by initialising the properties corresponding to the operation privilege that are applicable to the entity subclass.


\startuml "Authorisation mechanism Activity Diagram

start
:Load entity item considering access policies and user/group privileges;
if (Are view privileges for current user sufficient?) then (yes)
    :Access granted;
else (no)
    if (Is current context set to restricted mode?) then (yes)
        :Access denied (throw exception);
        stop
    else (no)
        :Item flagged as unaccessible for current user (no exception);
    endif
    :Access granted;
endif
:Generic authorisation check completed;
:Speficic authorisation checks for operation (performed by entity subclass);
if (Is specific privilege required for requested operation) then (yes)
    if (Does user have this privilege?) then (no)
        :Operation rejected (throw exception);
        stop
    else (yes)
    endif
else (no)
endif
:Operation allowed;
stop

\enduml

@}
 */
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
    public abstract class Entity : IValueSet {

        private string identifier;
        private int ownerId;

        protected IfyContext context;
        private bool canCreate = false, canChange = false, canDelete = false;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>The database ID, i.e. the numeric key value of an entity item.</summary>
        /// <remarks>0 is not (yet) persistently stored in the database.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public int Id { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides (protected) whether the item exists in the database.</summary>
        public bool Exists { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Unique identifier of an entity item.</summary>
        /// <remarks>
        ///     It must be unique among all items of an entity type. It can be a meaningful string describing the item, similar to variable identifiers in programming languages, or a machine-generated Universally Unique Identifier (UUID).
        ///     The identifier should be short and usable in RESTful URLs. Therefore it should not contain spaces or special characters, except those often found in URLs, such as hyphens or underscores.
        ///     Not all entity types require string identifiers, in some cases the numeric <see cref="Id"/> is sufficient. If the corresponding <see cref="EntityTableAttribute.IdentifierField"/> of the Entity subclass is unset, the property value is ignored when an item is stored and <c>null</c> when it is loaded.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string Identifier { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Human-readable name of an entity item item.</summary>
        /// <remarks>
        ///     The value of this property a short text corresponding to a title or caption of the item, according to the nature of the entity type. It should be of a length that fits without line break in a table cell so that it can be displayed easily in lists.
        ///     If subclass refer to the human-readable name as something different (e.g. <c>Title</c>, <c>Caption</c>, <c>HumanReadableName</c> or similar), it can be helpful for users of those classes to define such a property as a proxy property for <c>Name></c>, i.e. reading from and writing to <c>Name</c>.
        ///     Not all entity types require human-readable names. If the corresponding <see cref="EntityTableAttribute.NameField"/> of the Entity subclass is unset, the property value is ignored when an item is stored and <c>null</c> when it is loaded.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual string Name { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the ID of the domain to which the item belongs.</summary>
        public virtual int DomainId { get; set; } 
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the ID of the owner of the item.</summary>
        /// <remarks>
        ///     The owner is the user who owns the item, this information is stored in the database. Note that not all entity types allow ownership. In this case the value of this property has no meaning.
        ///     Do not confuse with UserId.
        /// </remarks>
        public virtual int OwnerId {
            get {
                return this.ownerId;
            }
            set {
                this.ownerId = value;
                if (EntityType == null) return;
                CanChange = context.AdminMode || UserId != 0 && UserId == OwnerId;
                CanDelete = context.AdminMode || UserId != 0 && UserId == OwnerId;
           }
        } 
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the ID of the user who is the subject to restrictions.</summary>
        /// <summary>The value of this property is used for obtaining privilege information. By default this is the requesting user (e.g. currently logged in on the website). Do not confuse with OwnerId.</summary>
        public virtual int UserId { get; set; } 
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates or determines whether the user is authorized to use the item representad by the entity instance.</summary>
        public virtual bool CanView { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether an item contained in an entity collection is still supposed to be part of the collection.</summary>
        /// <remarks>This property has an effect only if the Entity instance is part of an IEntityCollection.</remarks>
        public bool IsInCollection { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates or determines whether a new item can be created.</summary>
        public virtual bool CanCreate {
            get {
                if (canCreate || (context != null && context.AdminMode)) return true;
                return canCreate;
            } 
            set {
                canCreate = value;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual bool CanStore {
            get { return !Exists && CanCreate || Exists && CanChange; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether an existing item can be modified.</summary>
        public virtual bool CanChange { 
            get {
                if (canChange || (context != null && context.AdminMode)) return true;
                return canChange;
            } 
            set {
                canChange = value;
            }
        }

        [Obsolete("Obsolete, please use CanChange instead")]
        public bool CanModify { 
            get { return CanChange; } 
            set { CanChange = value; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether an existing item can be deleted.</summary>
        public virtual bool CanDelete { 
            get {
                if (canDelete || (context != null && context.AdminMode)) return true;
                return canDelete;
            } 
            set {
                canDelete = value;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public EntityType EntityType { get; protected set; }

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
                    ConstructorInfo ci = field.Property.PropertyType.GetConstructor(new Type[]{ typeof(IfyContext), typeof(EntityType), typeof(Entity) });
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
            Load();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Reads the information of the item with the specified ID from the database.</summary>
        public virtual void Load(string identifier) {
            Identifier = identifier;
            Load();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Reads the information of an item from the database.</sum\xrefitem rmodp "RM-ODP" "RM-ODP Documentation"   ///     The method performs the necessary <c>SELECT</c> command to obtain an item of a derived class of Entity from the database.
        ///     The database table(s) and fields to be used must be linked to the corresponding class(es) and its/their properties via the appropriate attributes.
        /// </remarks>
        public virtual void Load() {
            EntityType entityType = this.EntityType;
            bool hasAutoLoadFields = false;

            // Build WHERE clause
            string condition = null;
            if (Id != 0) condition = String.Format("t.{0}={1}", entityType.TopTable.IdField, Id);
            else if (entityType.TopTable.HasIdentifierField && Identifier != null) condition = String.Format("t.{0}={1}", entityType.TopTable.IdentifierField, StringUtils.EscapeSql(Identifier));
            //else if (!entityType.TopTable.HasAutomaticIds) condition = String.Format("t.{0}={1}", entityType.TopTable.IdField, Id);
            else condition = AlternativeIdentifyingCondition;
            if (condition == null) throw new EntityNotAvailableException("No identifying attribute specified for item");

            string sql = entityType.GetItemQuery(context, UserId, condition);
            
            //string sql = String.Format("SELECT {3} FROM {0} WHERE {1}{2};", join, condition, aggregation, select);
            if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);

            IDataReader reader;
            IDbConnection dbConnection = context.GetDbConnection();
            try {
                reader = context.GetQueryResult(sql, dbConnection);
            } catch (Exception e) {
                Console.WriteLine("FAILING SQL: {0}", sql);
                throw;
            }
            
            if (!reader.Read()) {
                context.CloseQueryResult(reader, dbConnection);
                string loadTerm = entityType.TopTable.HasIdentifierField ? Identifier : Id.ToString();
                string message = String.Format("{0} {2}{1}{3} not found",
                        entityType.SingularCaption == null ? entityType.ClassType.Name : entityType.SingularCaption,
                        loadTerm,
                        entityType.TopTable.HasIdentifierField ? "\"" : "[",
                        entityType.TopTable.HasIdentifierField ? "\"" : "]"
                );
                throw new EntityNotFoundException(message, entityType, loadTerm);
            }

            Load(entityType, reader);

            if (context.ConsoleDebug) Console.WriteLine("AFTER LOAD");
            
            CanChange = context.AdminMode || UserId != 0 && UserId == OwnerId;
            CanDelete = context.AdminMode || UserId != 0 && UserId == OwnerId;
            
            context.CloseQueryResult(reader, dbConnection);

            /*if (context.ConsoleDebug) Console.WriteLine("BEFORE LCF");
            if (hasAutoLoadFields) LoadComplexFields(false);
            if (context.ConsoleDebug) Console.WriteLine("AFTER LCF");*/
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Reads the information of an item using the specified IDataReader.</summary>
        /// <remarks>
        ///     The method is called from other core methods that build the appropriate query. It should not be called directly by other code unless the query is correctly built according to the Entity.Load() overload.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void Load(EntityType entityType, IDataReader reader) {
            bool includePrivileges = !context.AdminMode && entityType.TopTable.HasPrivilegeManagement;
            int index = 0;

            Id = reader.GetInt32(index++);
            Exists = true;
            if (entityType.TopTable.HasIdentifierField) Identifier = context.GetValue(reader, index++);
            if (entityType.TopTable.HasNameField) Name = context.GetValue(reader, index++);
            if (entityType.TopTable.HasDomainReference) DomainId = context.GetIntegerValue(reader, index++);
            if (entityType.TopTable.HasOwnerReference) OwnerId = context.GetIntegerValue(reader, index++);
            else OwnerId = 0;
            if (entityType.TopTable.HasPrivilegeManagement) {
                bool isUserAuthorized = false, isUserAuthorizedViaGroup = false, isUserAuthorizedViaGlobal = false;
                if (context.AdminMode) {
                    isUserAuthorized = true;
                } else {
                    isUserAuthorized = context.GetBooleanValue(reader, index++);
                    isUserAuthorizedViaGroup = context.GetBooleanValue(reader, index++);
                    isUserAuthorizedViaGlobal = context.GetBooleanValue(reader, index++);
                }
                CanView = isUserAuthorized || isUserAuthorizedViaGroup || isUserAuthorizedViaGlobal;
            }
            int firstCustomFieldIndex = index;

            if (includePrivileges && context.RestrictedMode && !CanView) {
                reader.Close();
                string message = String.Format("{0} is not authorized to use {1} {2}",
                        UserId == 0 ? "An unauthenticated user" : UserId == context.UserId ? String.Format("User \"{0}\"", context.Username) : String.Format("User [{0}]", UserId),
                        entityType.SingularCaption == null ? entityType.ClassType.Name : entityType.SingularCaption,
                        entityType.TopTable.HasIdentifierField ? String.Format("\"{0}\"", Identifier) : String.Format("[{0}]", Id)
                );

                throw new EntityUnauthorizedException(message, entityType, this, UserId);
            }

            try {
                foreach (FieldInfo field in entityType.Fields) {
                    // Skip privilege fields in admin mode
                    if (field.FieldType != EntityFieldType.PrivilegeField && field.FieldType != EntityFieldType.DataField && field.FieldType != EntityFieldType.ForeignField) continue;
                    if (!includePrivileges && field.FieldType == EntityFieldType.PrivilegeField) continue;

                    SetPropertyValue(field.Property, reader, index++);
                }
            } catch (Exception) {
                index = firstCustomFieldIndex;
                foreach (FieldInfo field in entityType.Fields) {
                    // Skip privilege fields in admin mode
                    if (field.FieldType != EntityFieldType.PrivilegeField && field.FieldType != EntityFieldType.DataField && field.FieldType != EntityFieldType.ForeignField) continue;
                    if (!includePrivileges && field.FieldType == EntityFieldType.PrivilegeField) continue;

                    try {
                        SetPropertyValue(field.Property, reader, index++);
                    } catch (Exception) {
                    }
                }
            }

            index = 0;
            if (context.ConsoleDebug) {
                Console.WriteLine("+ VALUE: {0,-25} = {1}", "Id", reader.GetInt32(index++));
                if (entityType.TopTable.HasIdentifierField) Console.WriteLine("- VALUE: {0,-25} = {1}", "Identifier", context.GetValue(reader, index++));
                if (entityType.TopTable.HasNameField) Console.WriteLine("- VALUE: {0,-25} = {1}", "Name", context.GetValue(reader, index++));
                if (entityType.TopTable.HasDomainReference) Console.WriteLine("- VALUE: {0,-25} = {1}", "DomainId", context.GetIntegerValue(reader, index++));
                if (entityType.TopTable.HasOwnerReference) Console.WriteLine("- VALUE: {0,-25} = {1}", "OwnerId", context.GetIntegerValue(reader, index++));
                if (!context.AdminMode && entityType.TopTable.HasPrivilegeManagement/* && Restricted*/) { // TODO
                    Console.WriteLine("- VALUE: {0,-25} = {1}", "UserAllow", context.GetBooleanValue(reader, index++));
                    Console.WriteLine("- VALUE: {0,-25} = {1}", "GroupAllow", context.GetBooleanValue(reader, index++));
                    Console.WriteLine("- VALUE: {0,-25} = {1}", "GlobalAllow", context.GetBooleanValue(reader, index++));
                }
                foreach (FieldInfo field in entityType.Fields) {
                    if (field.FieldType != EntityFieldType.PrivilegeField && field.FieldType != EntityFieldType.DataField && field.FieldType != EntityFieldType.ForeignField) continue;
                    if (!includePrivileges && field.FieldType == EntityFieldType.PrivilegeField) continue;
                    Console.WriteLine("- VALUE: {0,-25} = {1}", field.Property.Name, context.GetValue(reader, index++));
                }
            }
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

        public virtual void Store(EntityRelationshipType entityRelationshipType, Entity referringItem) {
            EntityType entityType = (entityRelationshipType == null ? this.EntityType : entityRelationshipType);
            bool hasAutoStoreFields = false;
            
            // Check whether identifier or name already exists
            if (entityType.TopTable == entityType.TopStoreTable && entityType.TopTable.HasIdentifierField && entityType.AutoCheckIdentifiers) {
                if (!Exists && entityType.AutoCorrectDuplicateIdentifiers) {
                    bool finding = true;
                    int suffix = 0;
                    while (finding) {
                        if (context.GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM {0} WHERE {1}={2};", entityType.TopTable.Name, entityType.TopTable.IdentifierField, StringUtils.EscapeSql(String.Format("{0}{1}", Identifier, suffix == 0 ? String.Empty : suffix.ToString())))) == 0) finding = false;
                        else suffix++;
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
                                values += entityType.Id.ToString();
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
                
                // Add view privilege to owner
                if (OwnerId != 0 && entityType.TopTable.HasPrivilegeManagement) {
                    context.Execute(String.Format("INSERT INTO {3} (id_{2}, id_usr) VALUES ({0}, {1});", Id, OwnerId, entityType.PrivilegeSubjectTable.Name, entityType.PrivilegeSubjectTable.PrivilegeTable));
                }
                Exists = true;

                //activity
                activity = new Activity(context,this, OperationPriv.CREATE);
                activity.Store();
                
            } else { // (2) - UPDATE
                for (int i = entityType.TopStoreTableIndex; i < entityType.Tables.Count; i++) {
                    string assignments = null;
                    if (i == 0) {
                        if (entityType.TopTable.HasIdentifierField) {
                            if (assignments == null) assignments = String.Empty; else assignments += ", ";
                            assignments += String.Format("{0}={1}", entityType.TopTable.IdentifierField, StringUtils.EscapeSql(Identifier));
                        }
                        if (entityType.TopTable.HasNameField) {
                            if (assignments == null) assignments = String.Empty; else assignments += ", ";
                            assignments += String.Format("{0}={1}", entityType.TopTable.NameField, StringUtils.EscapeSql(Name));
                        }
                        if (entityType.TopTable.HasOwnerReference) {
                            if (assignments == null) assignments = String.Empty; else assignments += ", ";
                            assignments += String.Format("{0}={1}", entityType.TopTable.OwnerReferenceField, OwnerId == 0 ? "NULL" : OwnerId.ToString());
                        }
                    }
                    
                    foreach (FieldInfo field in entityType.Fields) {
                        if (field.TableIndex != i || field.FieldType != EntityFieldType.DataField || field.IsReadOnly) continue;
                        if (assignments == null) assignments = String.Empty; else assignments += ", ";
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
                activity = new Activity(context,this, OperationPriv.MODIFY);
                activity.Store();
            }

            if (hasAutoStoreFields) StoreComplexFields(false);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the privileges for the user specified in the item's UserId property according to the privilege properties.</summary>
        public void StorePrivileges() {
            StorePrivileges(false, UserId, null, false);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the privileges on the resource represented by the instance for the specified users according to the privilege properties.</summary>
        /// <remarks>This method allows managing privileges from the resource's point of view: one resource grants privileges to several users.</remarks>
        /// <param name="userIds">An array of IDs of the users to which the privilege setting applies.</param>
        public void StorePrivilegesForUsers(int[] userIds) {
            StorePrivileges(false, 0, userIds, false);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the privileges on the resource represented by the instance for the specified users according to the privilege properties.</summary>
        /// <remarks>This method allows managing privileges from the resource's point of view: one resource grants privileges to several users.</remarks>
        /// <param name="userIds">An array of IDs of the users to which the privilege setting applies.</param>
        /// <param name="removeOthers">Determines whether privilege settings for other users not contained in <c>userIds</c> are removed.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void StorePrivilegesForUsers(int[] userIds, bool removeOthers) {
            StorePrivileges(false, 0, userIds, removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the privileges on the resource represented by the instance for the specified user groups according to the privilege properties.</summary>
        /// <remarks>This method allows managing privileges from the resource's point of view: one resource grants privileges to several groups.</remarks>
        /// <param name="groupIds">An array of IDs of the groups to which the privilege setting applies.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void StorePrivilegesForGroups(int[] groupIds) {
            StorePrivileges(true, 0, groupIds, false);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets the privileges on the resource represented by the instance for the specified user groups according to the privilege properties.</summary>
        /// <remarks>This method allows managing privileges from the resource's point of view: one resource grants privileges to several groups.</remarks>
        /// <param name="groupIds">An array of IDs of the groups to which the privilege setting applies.</param>
        /// <param name="removeOthers">Determines whether privilege settings for other groups not contained in <c>groupIds</c> are removed.</param>
        public void StorePrivilegesForGroups(int[] groupIds, bool removeOthers) {
            StorePrivileges(true, 0, groupIds, removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets global privileges on the resource represented by the instance that will apply to all users according to the privilege properties.</summary>
        /// <remarks>
        ///     This method allows managing privileges from the resource's point of view: one resource grants privileges to everybody.
        ///     The privileges previously defined for users and groups are kept but, since the global privilege settings override all finer grained settings, those have only effect for the privileges that are not allowed by the global privilege.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void StoreGlobalPrivileges() {
            StorePrivileges(false, 0, null, false);

            //activity
            Activity activity = new Activity(context,this, OperationPriv.MAKE_PUBLIC);
            activity.Store();
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets global privileges on the resource represented by the instance that will apply to all users according to the privilege properties.</summary>
        /// <remarks>This method allows managing privileges from the resource's point of view: one resource grants privileges to everybody.</remarks>
        /// <param name="removeOthers">Determines whether privilege settings at user and group level are removed.</param>
        public void StoreGlobalPrivileges(bool removeOthers) {
            StorePrivileges(false, 0, null, removeOthers);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        private void StorePrivileges(bool forGroup, int singleId, int[] multipleIds, bool removeOthers) {
            EntityType entityType = this.EntityType;
            int privilegeSubjectTableIndex = -1;

            for (int i = 0; i < entityType.Tables.Count; i++) {
                EntityTableAttribute table = entityType.Tables[i];
                if (table == entityType.PrivilegeSubjectTable) {
                    privilegeSubjectTableIndex = i;
                    break;
                }
            }
            
            // Add entity-specific fields to SELECT clause
            string names = String.Format("id_{0}, id_usr, id_grp", entityType.PrivilegeSubjectTable.Name);
            string values = String.Empty;
            string totalValues = null;
            string deleteCondition = null;
            foreach (FieldInfo field in entityType.Fields) {
                if (field.TableIndex != privilegeSubjectTableIndex || field.FieldType != EntityFieldType.PrivilegeField) continue;
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
                for (int i = 0; i < multipleIds.Length; i++) {
                    if (i == 0) totalValues = String.Empty; else totalValues += ", ";
                    if (i == 0) deleteCondition = String.Empty; else deleteCondition += ", ";
                    totalValues += String.Format("({0}, {1}, {2}{3})", Id, forGroup ? "NULL" : multipleIds[i].ToString(), forGroup ? multipleIds[i].ToString() : "NULL", values);
                    if (!removeOthers) deleteCondition = String.Format("{0}={1}", forGroup ? "id_grp" : "id_usr", singleId);
                }
                if (removeOthers) deleteCondition = String.Format("{0} IS NOT NULL", forGroup ? "id_grp" : "id_usr");
                else if (multipleIds.Length != 0) deleteCondition = String.Format("{0} IN ({1})", forGroup ? "id_grp" : "id_usr", deleteCondition);
                else deleteCondition = "false";
            } else {
                totalValues = String.Format("({0}, NULL, NULL{1})", Id, values);
                if (removeOthers) deleteCondition = "true";
                else deleteCondition = "id_usr IS NULL AND id_grp IS NULL";
            }
            
            context.Execute(String.Format("DELETE FROM {1} WHERE id_{2}={0} AND ({3});", Id, entityType.PrivilegeSubjectTable.PrivilegeTable, entityType.PrivilegeSubjectTable.Name, deleteCondition));
            context.Execute(String.Format("INSERT INTO {0} ({1}) VALUES {2};", entityType.PrivilegeSubjectTable.PrivilegeTable, names, totalValues));
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets privileges on resources for a single user.</summary>
        /// <remarks>This method allows managing privileges from a single user's point of view: one user has privileges on several resources.</remarks>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The ID of the user for which privileges on entity items are provided.</param>
        /// <param name="items">An array containing the Entity instances with the privilege values set according to the user's actual privileges.</param>
        /// <param name="removeOthers">Determines whether privilege settings for other resources not contained in <c>items</c> are removed.</param>
        public static void StorePrivilegesForUser(IfyContext context, int userId, Entity[] items, bool removeOthers) {
            StorePrivileges(context, false, userId, items, removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sets privileges on resources for a single group.</summary>
        /// <remarks>This method allows managing privileges from a single group's point of view: one group has privileges on several resources.</remarks>
        /// <param name="context">The execution environment context.</param>
        /// <param name="groupId">The ID of the group for which privileges on entity items are provided.</param>
        /// <param name="items">An array containing the Entity instances with the privilege values set according to the group's actual privileges.</param>
        /// <param name="removeOthers">Determines whether privilege settings for other resources not contained in <c>items</c> are removed.</param>
        public static void StorePrivilegesForGroup(IfyContext context, int groupId, Entity[] items, bool removeOthers) {
            StorePrivileges(context, true, groupId, items, removeOthers);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary></summary>
        /// <remarks></remarks>
        protected static void StorePrivileges(IfyContext context, bool forGroup, int id, Entity[] items, bool removeOthers) {
            if (removeOthers && items.Length != 0) {
                context.Execute(String.Format("DELETE FROM {0} WHERE {1}={2};", items[0].EntityType.PrivilegeSubjectTable.PrivilegeTable, forGroup ? "id_grp" : "id_usr", id));
            }
            foreach (Entity item in items) {
                if (context.ConsoleDebug) Console.WriteLine(String.Format("{0} {1} -> {2}", forGroup ? "grp" : "usr", id, item.Identifier));
                item.StorePrivileges(forGroup, id, null, false);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets global privileges on the resource represented by the instance that will apply to all users according to the privilege properties.</summary>
        /// <remarks>
        ///     This method allows managing privileges from the resource's point of view: one resource grants privileges to everybody.
        ///     The privileges previously defined for users and groups are kept but, since the global privilege settings override all finer grained settings, those have only effect for the privileges that are not allowed by the global privilege.
        /// </remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void RemoveGlobalPrivileges() {
            context.Execute(String.Format("DELETE FROM {1} WHERE id_{2}={0} AND id_usr IS NULL AND id_grp IS NULL;", Id, EntityType.PrivilegeSubjectTable.PrivilegeTable, EntityType.PrivilegeSubjectTable.Name));
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Determines whether this instance has global privilege.
        /// </summary>
        /// <returns><c>true</c> if this instance has global privilege; otherwise, <c>false</c>.</returns>
        public bool HasGlobalPrivilege(){
            return context.GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM {1} WHERE id_{2}={0} AND id_usr IS NULL AND id_grp IS NULL;", Id, EntityType.PrivilegeSubjectTable.PrivilegeTable, EntityType.PrivilegeSubjectTable.Name)) > 0;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the list of groups with privileges.
        /// </summary>
        /// <returns>The groups with privileges.</returns>
        public List<int> GetGroupsWithPrivileges(){
            List<int> ids = new List<int>();
            string sql = String.Format("SELECT id_grp FROM {1} WHERE id_{2}={0};",Id, EntityType.PrivilegeSubjectTable.PrivilegeTable, EntityType.PrivilegeSubjectTable.Name);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);

            while (reader.Read()) {
                if(reader.GetValue(0) != DBNull.Value) ids.Add(reader.GetInt32(0));
            }
            reader.Close();
            return ids;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the list of users with privileges.
        /// </summary>
        /// <returns>The users with privileges.</returns>
        public List<int> GetUsersWithPrivileges(){
            List<int> ids = new List<int>();
            string sql = String.Format("SELECT id_usr FROM {1} WHERE id_{2}={0};",Id, EntityType.PrivilegeSubjectTable.PrivilegeTable, EntityType.PrivilegeSubjectTable.Name);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);

            while (reader.Read()) {
                if(reader.GetValue(0) != DBNull.Value) ids.Add(reader.GetInt32(0));
            }
            reader.Close();
            return ids;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Remove the entity from the database</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void Delete() {
            if (!Exists) throw new InvalidOperationException("Cannot delete, no item loaded");
            //if (CanDelete) // TODO check privileges 

            //activity
            Activity activity = new Activity(context,this, OperationPriv.DELETE);
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

        public virtual void GetAllowedAdministratorOperations() {
            if (context.UserLevel == UserLevel.Administrator) return;

            CanCreate = false;
            CanChange = false;
            CanDelete = false;
            CanView = false;


            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(EntityType.GetAdministratorOperationsQuery(context.UserId, Id), dbConnection);

            while (reader.Read()) {
                switch (reader.GetChar(0)) {
                    case 'v' :
                    case 'V' :
                        CanView = true;
                        break;
                    case 'c' :
                        CanCreate = true;
                        break;
                    case 'm' :
                        CanChange = true;
                        break;
                    case 'p' :
                        //CanMakePublic = true;
                        break;
                    case 'd' :
                        CanDelete = true;
                        break;
                }
            }
            context.CloseQueryResult(reader, dbConnection);
        }

        public string[] GetValues() {
            return null;
        }

        public string GetExplanation() {
            return null;
        }

        public bool CheckValue(string value, out string defaultValue, out string selectedCaption) {
            defaultValue = null;
            selectedCaption = null;
            return false;
        }

        public bool[] CheckValues(string[] values, out string[] defaultValues) {
            defaultValues = null;
            return null;
        }

        public int WriteValues(XmlWriter output) {
            return 0;
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

        public EntityNotFoundException(string message, Exception e) : base(message, e) {}
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



    public class DuplicateEntityIdentifierException : Exception {

        public EntityType EntityType { get; protected set; }
        public Entity Entity { get; protected set; }
        public string Identifier { get; protected set; }

        public DuplicateEntityIdentifierException(string message, EntityType entityType, string identifier) : base(message) {
            this.EntityType = entityType;
            this.Identifier = identifier;
        }

        public DuplicateEntityIdentifierException(string message, Exception e) : base(message, e) {}
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class EntityNotAvailableException : Exception { 
        public EntityNotAvailableException(string message) : base(message) {}
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class EntityNotOwnedException : Exception { 
        public EntityNotOwnedException(string message) : base(message) {}
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    // IICD-WC-WS exception
    public class ResourceNotFoundException : Exception { 
        public ResourceNotFoundException(string message) : base(message) {}
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
        Unknown, ShowingList, DefiningItem, ShowingItem
    }

}
