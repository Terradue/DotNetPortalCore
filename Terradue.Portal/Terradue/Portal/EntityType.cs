using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Terradue.Util;


/*!
\defgroup Persistence Persistence of Data
@{
The component provides generic interaction with data that is persistently stored in a relational database. 
The data location and structure are defined in the classes that implement an object and which represent real-world entities.

\ingroup Core

\xrefitem int "Interfaces" "Interfaces" connects to \ref SQLConnector

@}

\defgroup SQLConnector SQL interface to RDBMS
@{
This is the interface to the relational Database in SQL

\xrefitem cptype_int "Interfaces" "Interfaces"

@}
*/




//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {
    
    
    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Represents an entity type and contains information about its structure.</summary>
    /// <remarks>
    ///     <p>This class ist mostly used internally. Its main purpose is to link subclasses of <see cref="Terradue.Portal.Entity"/> and their properties to database tables and fields.</p>
    ///     <p>Main functionality includes:</p>
    ///     <ul>
    ///         <li>Check whether an item of a specific type exists, by database ID or unique identifier (if applicable). This is useful in some cases to avoid an EntityNotFoundException.</li>
    ///         <li>Create various types of queries, such as for loading a particular item or a list of items that may be filtered by specific criteria. These queries are used by methods of <see cref="Terradue.Portal.Entity"/> or <see cref="Terradue.Portal.EntityCollection"/> that load data from the database.</li>
    ///         <li>Create correctly typed instances of Entity subclasses</li>
    ///         <li>Delete items of a specific type, by database ID or unique identifier (if applicable). This has the advantage that the item does not have to be instantiated before deleting it.</li>
    ///     </ul>
    /// </remarks>
    /// \ingroup Persistence
    public class EntityType : Entity {

        private const string RolePrivilegeBaseQuery = "SELECT DISTINCT rp.id_role FROM priv AS p LEFT JOIN role_priv AS rp ON p.id=rp.id_priv";

        // Static list of entity type metadata; loaded at application startup and used as reference for lookup throughout the runtime
        private static Dictionary<Type, EntityType> entityTypes = new Dictionary<Type, EntityType>();

        private static Dictionary<PropertyInfo, EntityRelationshipType> entityRelationshipTypes = new Dictionary<PropertyInfo, EntityRelationshipType>();

        private string singularCaption, pluralCaption;

        private string restrictedJoinSql, unrestrictedJoinSql, adminJoinSql;

        private string coreSelectSql, permissionSelectSql, userPermissionSelectSqlTemplate, adminSelectSql, normalSelectSql;

        private bool isSqlPrepared = false;

        private Regex regexRegex = new Regex(@"[\[\]\(\)\{\}\?\*\+]");

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the direct subclass of Entity of which this entity type is derived./summary>
        public EntityType TopType { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets or sets the database ID of the direct subclass of Entity of which this entity type is derived./summary>
        public int TopTypeId { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the database ID of the nearest base class of this entity type that has a database record./summary>
        public int PersistentTypeId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the class name.</summary>
        /// \ingroup Persistence
        public string ClassName { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the class type reference.</summary>
        public Type ClassType { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the type reference of the class that should be used if the original class is abstract.</summary>
        public Type GenericClassType { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the type reference of the class that should be used instead of the original class.</summary>
        public Type CustomClassType { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the singular caption of the entity type.</summary>
        /// \ingroup Persistence
        public string SingularCaption {
            get {
                if (singularCaption == null) return ClassType.Name;
                return singularCaption;
            }
            protected set {
                singularCaption = value;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the plural caption of the entity type.</summary>
        /// \ingroup Persistence
        public string PluralCaption {
            get {
                if (pluralCaption == null) return String.Format("{0}*", ClassType.Name);
                return pluralCaption;
            }
            protected set {
                pluralCaption = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the keyword of the entity type.</summary>
        /// <remarks>The keyword is used in URLs, e.g. in the administration area where the different entity types can be addressed by a RESTful URL.</remarks>
        public string Keyword { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /*public bool HasIdentifierField {
            get { return TopTable.IdentifierField != null; } 
        }*/

        //---------------------------------------------------------------------------------------------------------------------
        
        /*public bool HasNameField {
            get { return TopTable.NameField != null; } 
        }*/

        //---------------------------------------------------------------------------------------------------------------------
        
        public bool HasExtensions {
            get { return TopTable.HasExtensions; } 
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /*public bool HasDomainReference { 
            get { return TopTable.DomainReferenceField != null; } 
        }*/

        //---------------------------------------------------------------------------------------------------------------------
        
        /*public bool HasOwnerReference {
            get { return TopTable.OwnerReferenceField != null; }
        }*/

        //---------------------------------------------------------------------------------------------------------------------
        
        public bool HasPermissionManagement {
            get { return PermissionSubjectTable != null; }
        }

        [Obsolete("Use HasPermissionManagement (changed for terminology consistency)")]
        public bool HasPrivilegeManagement {
            get { return PermissionSubjectTable != null; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool HasNestedData { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityTableAttribute TopTable { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual EntityTableAttribute TopStoreTable {
            get { return TopTable; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual int TopStoreTableIndex {
            get { return 0; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityTableAttribute PermissionSubjectTable { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public List<EntityTableAttribute> Tables { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public List<ForeignTableInfo> ForeignTables { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool HasMultipleTables { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public List<FieldInfo> Fields;

        //---------------------------------------------------------------------------------------------------------------------

        public bool AllowsKeywordSearch { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool AutoCheckIdentifiers { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool AutoCorrectDuplicateIdentifiers { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new EntityType instance.</summary>
        /// \ingroup Persistence
        /// <param name="context">The execution environment context.</param>
        public EntityType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public EntityType(int id, int superTypeId, string className, string genericClassName, string customClassName, string singularCaption, string pluralCaption, string keyword, bool hasExtensions) : this(null as IfyContext) {
            if (IfyContext.DefaultConsoleDebug) Console.WriteLine("NEW ENTITY TYPE {0}", className);
            this.Id = id;
            this.PersistentTypeId = id;
            this.ClassName = className;
            this.ClassType = Type.GetType(className, true);
            
            bool invalid = false;            
            if (ClassType == null) invalid = true;
            if (genericClassName != null) {
                this.GenericClassType = Type.GetType(genericClassName, true);
                if (this.GenericClassType == null) invalid = true;
            }
            if (customClassName != null) {
                this.CustomClassType = Type.GetType(customClassName, true);
                if (this.CustomClassType == null) invalid = true;
            }

            if (invalid) throw new Exception("Invalid entity type");
            
            this.SingularCaption = singularCaption;
            this.PluralCaption = pluralCaption;
            this.Keyword = keyword;
            
            this.TopType = this;
            this.TopTypeId = this.Id;
            
            Type type = ClassType;
            while (type != null) {
                EntityType entityType = EntityType.GetEntityType(type);
                if (entityType != null) {
                    this.TopType = entityType;
                    this.TopTypeId = entityType.Id;
                }
                type = type.BaseType;
            }
            
            Tables = new List<EntityTableAttribute>();
            ForeignTables = new List<ForeignTableInfo>();
            Fields = new List<FieldInfo>();

            if (ClassType != null) GetEntityStructure(ClassType);
            
            if (IfyContext.DefaultConsoleDebug) {
                Console.WriteLine("=======");
                Console.WriteLine("SUMMARY");
                for (int i = 0; i < Tables.Count; i++) Console.WriteLine("  - {0}", Tables[i].Name);
                Console.WriteLine();
                Console.WriteLine("=======");
                Console.WriteLine();
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public EntityType(Type type) : base(null) {
            this.ClassType = type;
            Tables = new List<EntityTableAttribute>();
            ForeignTables = new List<ForeignTableInfo>();
            Fields = new List<FieldInfo>();
            GetEntityStructure(ClassType);
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void CopyFrom(EntityType source) {
            foreach (EntityTableAttribute table in source.Tables) Tables.Add(table);
            foreach (ForeignTableInfo foreignTable in source.ForeignTables) ForeignTables.Add(foreignTable);
            foreach (FieldInfo field in source.Fields) Fields.Add(field);
            TopType = source.TopType;
            TopTypeId = source.TopTypeId;
            if (PersistentTypeId == 0) PersistentTypeId = source.PersistentTypeId;
            GenericClassType = source.GenericClassType;
            TopTable = source.TopTable;
            PermissionSubjectTable = source.PermissionSubjectTable;
            HasMultipleTables = source.HasMultipleTables;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new EntityType instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>The created EntityType object.</returns>
        public static EntityType GetInstance(IfyContext context) {
            return new EntityType(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static void LoadEntityTypes(IfyContext context) {
            entityTypes.Clear();
            IDataReader reader = context.GetQueryResult("SELECT t.id, t.id_super, t.class, t.generic_class, t.custom_class, t.caption_sg, t.caption_pl, t.keyword, COUNT(t1.id)>0 FROM type AS t LEFT JOIN type AS t1 ON t.id=t1.id_super GROUP BY t.id ORDER BY t.id_module, t.id_super IS NOT NULL, t.id_super, t.pos;");
            while (reader.Read()) {
                string typeStr = context.GetValue(reader, 2);
                Type type = Type.GetType(typeStr);
                if (type == null) {
                    continue;
                    /*reader.Close();
                    throw new Exception(String.Format("Entity type not found: {0}", typeStr));*/
                }
                entityTypes[type] = new EntityType(
                        context.GetIntegerValue(reader, 0),
                        context.GetIntegerValue(reader, 1),
                        context.GetValue(reader, 2),
                        context.GetValue(reader, 3),
                        context.GetValue(reader, 4),
                        context.GetValue(reader, 5),
                        context.GetValue(reader, 6),
                        context.GetValue(reader, 7),
                        context.GetBooleanValue(reader, 8)
                );
            }
            reader.Close();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns an SQL conditional expression to obtain only items of the specified entity type and its subtypes.</summary>
        /// <returns>The type filter condition.</returns>
        /// <param name="rootType">The entity type for which the items are to be obtained.</param>
        /// <param name="fieldExpression">The SQL expression for the type reference field.</param>
        public static string GetTypeFilterCondition(EntityType rootType, string fieldExpression) {
            string result = rootType.Id.ToString();
            bool multiple = false;
            foreach (EntityType entityType in entityTypes.Values) {
                if (entityType.Id == 0 || entityType == rootType || !rootType.ClassType.IsAssignableFrom(entityType.ClassType)) continue;
                result += String.Format(",{0}", entityType.Id);
                multiple = true;
            }
            return String.Format("{0}{2}{1}{3}", fieldExpression, result, multiple ? " IN (" : "=", multiple ? ")" : String.Empty);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static EntityType AddEntityType(Type type) {
            Type actualType = type;
            EntityType entityType = null;

            while (type != null && type != typeof(Entity)) {
                foreach (System.Attribute attribute in type.GetCustomAttributes(true)) {
                    if (attribute is EntityRelationshipTableAttribute) entityType = new EntityRelationshipType(actualType);
                    else if (attribute is EntityTableAttribute) entityType = new EntityType(actualType);
                    else {
                        continue;
                    }
                    entityTypes[actualType] = entityType;
                    return entityType;
                }
                type = type.BaseType;
            }
            throw new InvalidOperationException(String.Format("Entity information not available: {0}", actualType.FullName));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static EntityType GetEntityType(Type type) {
            // Return EntityType instance corresponding to type if exists
            return (entityTypes.ContainsKey(type) ? entityTypes[type] : null);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static EntityType GetOrAddEntityType(Type type) {
            EntityType result = (entityTypes.ContainsKey(type) ? entityTypes[type] : null);
            if (result == null) {
                result = AddEntityType(type);
                if (result == null || result.Tables.Count == 0) throw new InvalidOperationException(String.Format("Entity information not available: {0}", type.FullName));
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static EntityRelationshipType AddEntityRelationshipType(PropertyInfo referencingProperty, Type type) {
            EntityRelationshipType entityRelationshipType = new EntityRelationshipType(type, referencingProperty);
            entityRelationshipTypes[referencingProperty] = entityRelationshipType;
            return entityRelationshipType;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static EntityRelationshipType GetOrAddEntityRelationshipType(PropertyInfo propertyInfo) {
            Type actualType = (propertyInfo.PropertyType.GetGenericArguments().Length == 1 ? propertyInfo.PropertyType.GetGenericArguments()[0] : null);
            if (actualType == null) throw new InvalidOperationException("Entity information not available because property type has no type parameter");

            EntityRelationshipType result = (entityTypes.ContainsKey(actualType) ? entityTypes[actualType] as EntityRelationshipType : null);
            if (result != null) return result;

            result = (entityRelationshipTypes.ContainsKey(propertyInfo) ? entityRelationshipTypes[propertyInfo] : null);
            if (result == null) {
                result = AddEntityRelationshipType(propertyInfo, actualType);
                if (result == null || result.Tables.Count == 0) throw new InvalidOperationException(String.Format("Entity information not available: {0}", actualType.FullName));
                return result;
            }

            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static EntityType GetEntityTypeFromKeyword(string keyword) {
            foreach (EntityType entityType in entityTypes.Values) {
                if (entityType.Keyword == keyword) return entityType;
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static EntityType GetEntityTypeFromName(string name) {
            foreach (EntityType entityType in entityTypes.Values) {
                if (entityType.Name == name) return entityType;
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static EntityType GetEntityTypeFromId(int id) {
            foreach (EntityType entityType in entityTypes.Values) {
                if (entityType.Id == id) return entityType;
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static List<EntityType> GetDomainEntityTypes() {
            List<EntityType> result = new List<EntityType>();
            foreach (EntityType entityType in entityTypes.Values) {
                if (entityType.TopTable.HasDomainReference) result.Add(entityType);
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static List<EntityType> GetAssignedEntityTypes() {
            List<EntityType> result = new List<EntityType>();
            foreach (EntityType entityType in entityTypes.Values) {
                if (entityType.TopTable.HasPermissionManagement) result.Add(entityType);
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual void GetEntityStructure(Type type) {
            if (type == null || type == typeof(Entity)) return;

            EntityType baseEntityType = GetEntityType(type.BaseType);
            if (baseEntityType == null) {
                GetEntityStructure(type.BaseType);
                TopType = this;
                TopTypeId = this.Id;
            } else {
                CopyFrom(baseEntityType);
            }
            
            EntityTableAttribute tableInfo = null;
            
            foreach (System.Attribute attribute in type.GetCustomAttributes(true)) {
                if (attribute is EntityRelationshipTableAttribute) tableInfo = attribute as EntityRelationshipTableAttribute;
                else if (attribute is EntityTableAttribute) tableInfo = attribute as EntityTableAttribute;
                else continue;
                break;
            }
            
            if (tableInfo == null) return;

            if (IfyContext.DefaultConsoleDebug) Console.WriteLine("TABLE {0}", tableInfo.Name + " PRIV=" + tableInfo.PermissionTable);
            
            int tableIndex = 0;
            
            if (tableInfo.Storage == EntityTableStorage.Here && tableInfo.Name != null) {
                Tables.Add(tableInfo);
                if (tableInfo.HasPermissionManagement) PermissionSubjectTable = tableInfo;
                tableIndex = Tables.Count - 1;
            } else if (tableInfo.Storage == EntityTableStorage.Below) {
                tableIndex = Tables.Count;
            } else if (tableInfo.Storage == EntityTableStorage.Above) {
                tableIndex = Tables.Count - 1;
            } else {
                return;
            }

            HasNestedData |= tableInfo.HasNestedData;
            
            if (TopTable == null && Tables.Count != 0) TopTable = Tables[0];

            foreach (System.Attribute attribute in type.GetCustomAttributes(true)) {
                if (attribute is EntityMultipleForeignTableAttribute) {
                    EntityMultipleForeignTableAttribute attr = attribute as EntityMultipleForeignTableAttribute;
                    ForeignTableInfo foreignTableInfo = new ForeignTableInfo(tableInfo, tableIndex, attr);
                    ForeignTables.Add(foreignTableInfo);
                    HasMultipleTables = true;
                    if (IfyContext.DefaultConsoleDebug) Console.WriteLine("MULTIPLE FOREIGN TABLE {0} - {1}", attr.RelationshipName, attr.Name);
                } else if (attribute is EntityForeignTableAttribute) {
                    EntityForeignTableAttribute attr = attribute as EntityForeignTableAttribute;
                    ForeignTableInfo foreignTableInfo = new ForeignTableInfo(tableInfo, tableIndex, attr);
                    ForeignTables.Add(foreignTableInfo);
                    if (IfyContext.DefaultConsoleDebug) Console.WriteLine("FOREIGN TABLE {0}", foreignTableInfo.Name);
                }
            }

            AppendProperties(type, tableIndex);
            if (!AllowsKeywordSearch) {
                foreach (FieldInfo field in Fields) {
                    if (field.IsUsedInKeywordSearch) {
                        AllowsKeywordSearch = true;
                        break;
                    }
                }
            }

            foreach (EntityTableAttribute t in Tables) {
                AutoCheckIdentifiers |= t.AutoCheckIdentifiers;
                AutoCorrectDuplicateIdentifiers |= t.AutoCorrectDuplicateIdentifiers;
                AllowsKeywordSearch |= t.AllowsKeywordSearch;
            }

        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected void AppendProperties(Type type, int tableIndex) {
            foreach (PropertyInfo pi in type.GetProperties()) {
                if (pi.DeclaringType != type) continue;
                foreach (System.Attribute attribute in pi.GetCustomAttributes(true)) {
                    if (attribute is EntityPermissionFieldAttribute) {
                        if (IfyContext.DefaultConsoleDebug) Console.WriteLine("  - [P] {0,-20} {1}", (attribute as EntityPermissionFieldAttribute).Name, " (" + pi.DeclaringType.Name + "." + pi.Name + ")");
                        Fields.Add(new FieldInfo(pi, tableIndex, attribute as EntityPermissionFieldAttribute));
                    } else if (attribute is EntityDataFieldAttribute) {
                        if (IfyContext.DefaultConsoleDebug) Console.WriteLine("  - [D] {0,-20} {1}", (attribute as EntityDataFieldAttribute).Name, " (" + pi.DeclaringType.Name + "." + pi.Name + ")");
                        Fields.Add(new FieldInfo(pi, tableIndex, attribute as EntityDataFieldAttribute));
                    } else if (attribute is EntityForeignFieldAttribute) {
                        if (IfyContext.DefaultConsoleDebug) Console.WriteLine("  - [F] {0,-20} {1}", (attribute as EntityForeignFieldAttribute).Name, " (" + pi.DeclaringType.Name + "." + pi.Name + ")");
                        Fields.Add(new FieldInfo(pi, tableIndex, attribute as EntityForeignFieldAttribute));
                    } else if (attribute is EntityEntityFieldAttribute) {
                        if (IfyContext.DefaultConsoleDebug) Console.WriteLine("  - [E] {0,-20} {1}", (attribute as EntityEntityFieldAttribute).Name,  " (" + pi.DeclaringType.Name + "." + pi.Name + ")");
                        Fields.Add(new FieldInfo(pi, tableIndex, attribute as EntityEntityFieldAttribute));
                    } else if (attribute is EntityRelationshipAttribute) {
                        if (IfyContext.DefaultConsoleDebug) Console.WriteLine("  - [R] {0,-20} {1}", (attribute as EntityRelationshipAttribute).Name,  " (" + pi.DeclaringType.Name + "." + pi.Name + ")");
                        Fields.Add(new FieldInfo(pi, tableIndex, attribute as EntityRelationshipAttribute));
                    }/* else if (attribute is EntityComplexFieldAttribute) {
                        if (IfyContext.DefaultConsoleDebug) Console.WriteLine("  - [C] {0,-20} {1}", "[no name]",  " (" + pi.DeclaringType.Name + "." + pi.Name + ")");
                        Fields.Add(new FieldInfo(pi, tableIndex, attribute as EntityComplexFieldAttribute));
                    }*/
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the item with the specified database ID exists in the database.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The database ID of the item.</param>
        /// <returns><c>true</c>, if item exists, <c>false</c> otherwise.</returns>
        public bool DoesItemExist(IfyContext context, int id) {
            return context.GetQueryIntegerValue(GetQuery(context, true, 0, null, EntityItemVisibility.All, String.Format("t.{1}={0}", id, TopTable.IdField), null, true, -1, -1, EntityAccessLevel.Administrator)) != 0;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the item with the specified unique identifier exists in the database.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="identifier">The unique identifier of the item.</param>
        /// <returns><c>true</c>, if item exists, <c>false</c> otherwise.</returns>
        public bool DoesItemExist(IfyContext context, string identifier) {
            if (!TopTable.HasIdentifierField) return false;
            return context.GetQueryIntegerValue(GetQuery(context, true, 0, null, EntityItemVisibility.All, String.Format("t.{1}={0}", StringUtils.EscapeSql(identifier), TopTable.IdentifierField), null, true, -1, -1, EntityAccessLevel.Administrator)) != 0;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the SQL query for selecting a single item on behalf of the specified user.</summary>
        /// <returns>The SQL query.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="item">The <see cref="Entity"/> for which the query is built (currently ignored).</param>
        /// <param name="userId">The database ID of the user on whose behalf the item is selected.</param>
        /// <param name="condition">Additional SQL condition.</param>
        /// <param name="accessLevel">The <see cref="EntityAccessLevel"/> to be taken into account for the query. It affects the resulting query and thus whether the entity can actually be loaded. If set to <c>None</c>, the context's default access level is applied.</param>
        public string GetItemQuery(IfyContext context, Entity item, int userId, string condition, EntityAccessLevel accessLevel) {
            return GetQuery(context, false, userId, null, EntityItemVisibility.All, condition, null, false, -1, -1, accessLevel);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the full or IDs-only SQL query for selecting an item list on behalf of the specified user.</summary>
        /// <returns>The SQL query.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="items">The <see cref="EntityCollection"/> for which the query is built.</param>
        /// <param name="userId">The database ID of the user on whose behalf the item is selected.</param>
        /// <param name="groupIds">An optional array of IDs of the groups to take into account for the permission filtering.</param>
        /// <param name="condition">Additional SQL condition.</param>
        /// <param name="idsOnly">Decides whether the returned query selects only the database IDs of matching item.</param>
        public string GetListQuery(IfyContext context, EntityCollection items, int userId, int[] groupIds, string condition, bool idsOnly) {
            object[] parts = GetListQueryParts(context, items, userId, groupIds, condition);
            return idsOnly ? GetIdQuery(parts) : GetQuery(parts);
        }

        /// <summary>Gets the parts to build the full or IDs-only SQL query for selecting an item list on behalf of the specified user.</summary>
        /// <returns>An array of objects containing the components of the SQL query in a defined order.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="items">The <see cref="EntityCollection"/> for which the query is built.</param>
        /// <param name="userId">The database ID of the user on whose behalf the item is selected.</param>
        /// <param name="groupIds">An optional array of IDs of the groups to take into account for the permission filtering.</param>
        /// <param name="condition">Additional SQL conditaion.</param>
        /// <param name="idsOnly">Decides whether the returned query selects only the database IDs of matching item.</param>
        public object[] GetListQueryParts(IfyContext context, EntityCollection items, int userId, int[] groupIds, string condition) {
            if (items.Template != null) {
                string templateCondition = GetTemplateConditionSql(items.Template, false);
                if (templateCondition != null) {
                    if (condition == null) condition = String.Empty;
                    else condition += " AND ";
                    condition += GetTemplateConditionSql(items.Template, false);
                }
            }
            if (items.FilterValues != null) {
                string filterCondition = GetFilterSql(items.FilterValues);
                if (filterCondition != null) {
                    if (condition == null) condition = String.Empty;
                    else condition += " AND ";
                    condition += filterCondition;
                }
            }
            if (AllowsKeywordSearch && items.SearchKeyword != null) {
                string keywordCondition = GetKeywordFilterSql(items.SearchKeyword, items.FindWholeWords);
                if (keywordCondition != null) {
                    if (condition == null) condition = String.Empty;
                    else condition += " AND ";
                    condition += keywordCondition;
                }
            }

            if ((items.ItemVisibility & EntityItemVisibility.OwnedOnly) == EntityItemVisibility.OwnedOnly && userId != 0 && TopTable.HasOwnerReference) {
                if (condition == null) condition = String.Empty; else condition += " AND ";
                condition += String.Format("t.{0}={1}", TopTable.OwnerReferenceField, userId);
            }

            if (this is EntityRelationshipType && items.ReferringItem != null) {
                if (condition == null) condition = String.Empty; else condition += " AND ";
                condition += String.Format("t{0}.{1}={2}", TopStoreTableIndex == 0 ? String.Empty : TopStoreTableIndex.ToString(), TopStoreTable.ReferringItemField, items.ReferringItem.Id);
            }

            string sort = (items.SortCriteria == null ? null : GetSortSql(items.SortCriteria));

            int limit, offset;
            if (items.UsePaging) {
                limit = items.ItemsPerPage;
                offset = items.SelectByPage ? (items.Page - EntityCollection.DefaultPageOffset) * items.ItemsPerPage : items.StartIndex - EntityCollection.DefaultItemOffset;
            } else {
                limit = -1;
                offset = 0;
            }

            return GetQueryParts(context, true, userId, groupIds, items.ItemVisibility, condition, sort, limit, offset, items.AccessLevel);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the full or IDs-only SQL query for selecting an item list on behalf of the specified user.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user on whose behalf the items are selected.</param>
        /// <param name="condition">Additional SQL condition.</param>
        /// <param name="idsOnly">Decides whether the returned query selects only the database IDs of matching item.</param>
        /// <param name="limit">Number of records to be retrieved (optional, defaults to -1, i.e. no limit)</param>
        /// <param name="offset">Starting offset of records to be retrieved (optional, defaults to 0, i.e. no offset)</param>
        /// <returns>The SQL query.</returns>
        [Obsolete("Use GetListQuery with EntityCollection argument")]
        public string GetListQuery(IfyContext context, int userId, string condition, bool idsOnly, int limit = -1, int offset = 0) {
            return GetQuery(context, true, userId, null, EntityItemVisibility.All, condition, null, idsOnly, limit, offset, EntityAccessLevel.None);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the full or IDs-only SQL query for selecting an item list filtered according to the specified template on behalf of the specified user.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user on whose behalf the items are selected.</param>
        /// <param name="template">A template object of the same type as the list item entity type with properties set to filtering values.</param>
        /// <param name="idsOnly">Decides whether the returned query selects only the database IDs of matching item.</param>
        /// <param name="limit">Number of records to be retrieved (optional, defaults to -1, i.e. no limit)</param>
        /// <param name="offset">Starting offset of records to be retrieved (optional, defaults to 0, i.e. no offset)</param>
        /// <returns>The SQL query.</returns>
        [Obsolete("Use GetListQuery with EntityCollection argument")]
        public string GetListQuery(IfyContext context, int userId, Entity template, Dictionary<FieldInfo, string> filterValues, bool idsOnly, int limit = -1, int offset = 0) {
            string condition = null;
            if (template != null) {
                condition = GetTemplateConditionSql(template, false);
            } else if (filterValues != null) {
                string filterCondition = GetFilterSql(filterValues);
                if (filterCondition != null) {
                    if (condition == null) condition = String.Empty;
                    else condition += " AND ";
                    condition += filterCondition;
                }
            }

            return GetQuery(context, true, userId, null, EntityItemVisibility.All, condition, null, idsOnly, limit, offset, EntityAccessLevel.None);
        }
        
        [Obsolete("Use GetListQuery with EntityCollection argument")]
        public string GetListQueryWithTemplate(IfyContext context, int userId, Entity template, bool idsOnly, int limit = -1, int offset = 0) {
            return GetListQuery(context, userId, template, null, idsOnly, limit, offset);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets full or IDs-only the SQL query for selecting an item list containing items referred to by another item on behalf of the specified user.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user on whose behalf the items are selected.</param>
        /// <param name="referringItem">A template object of the same type as the list item entity type with properties set to filtering values.</param>
        /// <param name="idsOnly">Decides whether the returned query selects only the database IDs of matching item.</param>
        /// <param name="limit">Number of records to be retrieved (optional, defaults to -1, i.e. no limit)</param>
        /// <param name="offset">Starting offset of records to be retrieved (optional, defaults to 0, i.e. no offset)</param>
        /// <returns>The SQL query.</returns>
        [Obsolete("Use GetListQuery with EntityCollection argument")]
        public string GetListQueryOfRelationship(IfyContext context, int userId, Entity referringItem, bool idsOnly, int limit = -1, int offset = 0) {
            string condition = String.Format("t{0}.{1}={2}", TopStoreTableIndex == 0 ? String.Empty : TopStoreTableIndex.ToString(), TopStoreTable.ReferringItemField, referringItem.Id);
            return GetQuery(context, true, userId, null, EntityItemVisibility.All, condition, null, idsOnly, limit, offset, EntityAccessLevel.None);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the full or IDs-only SQL query for selecting an item list containing items referred to by another item on behalf of the specified user.</summary>
        /// <returns>The list query for owned items.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user on whose behalf the items are selected.</param>
        /// <param name="idsOnly">If set to <c>true</c> identifiers only.</param>
        /// <param name="limit">Number of records to be retrieved (optional, defaults to -1, i.e. no limit)</param>
        /// <param name="offset">Starting offset of records to be retrieved (optional, defaults to 0, i.e. no offset)</param>
        [Obsolete("Use GetListQuery with EntityCollection argument")]
        public string GetListQueryForOwnedItems(IfyContext context, int userId, bool idsOnly, int limit = -1, int offset =0) {
            string condition = null;
            if (userId != 0 && TopTable.HasOwnerReference) {
                if (condition == null) condition = String.Empty; else condition += " AND ";
                condition += String.Format("t.{0}={1}", TopTable.OwnerReferenceField, userId);
            }

            return GetQuery(context, true, userId, null, EntityItemVisibility.All, condition, null, idsOnly, limit, offset, EntityAccessLevel.None);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the full or IDs-only SQL query for selecting an item list containing items for which any of the specified groups have access permission.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="groupIds">An array of IDs of the groups to take into account for the permission filtering.</param>
        /// <param name="condition">An initial SQL conditional expression. The main database table is addressed with the alias <c>t</c>.</param>
        /// <param name="idsOnly">Decides whether the returned query is to return only a list of matching database IDs.</param>
        /// <param name="limit">Number of records to be retrieved (optional, defaults to -1, i.e. no limit)</param>
        /// <param name="offset">Starting offset of records to be retrieved (optional, defaults to 0, i.e. no offset)</param>
        public string GetGroupQuery(IfyContext context, int[] groupIds, string condition = null, bool idsOnly = false, int limit = -1, int offset = 0) {
            return GetQuery(context, true, 0, groupIds, EntityItemVisibility.All, condition, null, idsOnly, limit, offset, EntityAccessLevel.None);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Builds the SQL query necessary to load an entity item or a list of entity items according to the specified restrictions.</summary>
        /// <returns>The SQL <c>SELECT</c> statement that selects the entity item or the list of entity items.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="list">The entity item that has already been instantiated but still needs to be loaded. Can be <c>null</c>.</param>
        /// <param name="userId">ID of the user on whose behalf the item or list is to be selected.</param>
        /// <param name="groupIds">An array of IDs of the groups to take into account for the permission filtering.</param>
        /// <param name="visibility">The visibility flags for items in the collection.</param>
        /// <param name="condition">An initial SQL conditional expression. The main database table is addressed with the alias <c>t</c>.</param>
        /// <param name="idsOnly">Decides whether the returned query is to return only a list of matching database IDs.</param>
        /// <param name="accessLevel">The entity access mode according to which restrictions are applied to the resulting query.</param>
        public string GetQuery(IfyContext context, bool list, int userId, int[] groupIds, EntityItemVisibility visibility, string condition, string sort = null, bool idsOnly = false, int limit = -1, int offset = 0, EntityAccessLevel accessLevel = EntityAccessLevel.None) {

            object[] parts = GetQueryParts(context, list, userId, groupIds, visibility, condition, sort, limit, offset, accessLevel);

            return GetQuery(parts);

        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetQuery(object[] parts) {
            return String.Format("SELECT {4}{5} FROM {0}{1}{2}{6}{7};", parts);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetCountQuery(object[] parts) {
            return String.Format("SELECT COUNT(*) FROM (SELECT {3} FROM {0}{1}{2}) AS a;", parts);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetIdQuery(object[] parts) {
            return String.Format("SELECT {3} FROM {0}{1}{2}{6}{7};", parts);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public object[] GetQueryParts(IfyContext context, bool list, int userId, int[] groupIds, EntityItemVisibility visibility, string condition, string sort = null, int limit = -1, int offset = 0, EntityAccessLevel accessLevel = EntityAccessLevel.None) {

            if ((visibility & EntityItemVisibility.All) == 0) visibility |= EntityItemVisibility.All;

            string groupIdsStr = null;

            if (accessLevel == EntityAccessLevel.None) {
                accessLevel = context.AccessLevel;
                //queryMode = context.AdminMode ? EntityQueryMode.Administrator : context.RestrictedMode ? EntityQueryMode.Restricted : EntityQueryMode.Unrestricted;
            }

            if (!isSqlPrepared) PrepareSql();

            // Build FROM clause
            string joinSql = (accessLevel == EntityAccessLevel.Administrator ? adminJoinSql : userId != 0 && accessLevel == EntityAccessLevel.Permission && list ? restrictedJoinSql : unrestrictedJoinSql);

            // Add GROUP BY aggregation if necessary
            string aggregationSql = null;
            if (HasPermissionManagement) {
                aggregationSql = String.Format(" GROUP BY t.id{0}", GetVisibilityAggregationClause(visibility, accessLevel, list));
            } else {
                aggregationSql = String.Empty;
            }

            string grantSelectSql = null;

            if (accessLevel == EntityAccessLevel.Privilege) {
                // For lists, get all roles with search privilege on items of the entity type (inverse = false); 
                // for items, get all roles with any other privilege than search on items of the entity type (inverse = true)
                int[] roleIds = GetRolesForPrivilege(context, new EntityOperationType[] { EntityOperationType.Create, EntityOperationType.Search }, !list); // in case of a list, look for search privilege, otherwise for anything else
                if (context.ConsoleDebug) Console.WriteLine("ROLES: {0}", roleIds == null ? "PRIVILEGE NOT DEFINED" : String.Join(",", roleIds));

                // Do not perform domain-restriction check if view (or similar) privilege is not defined (roleIds == null)
                if (roleIds != null) {

                    // If list is empty, the view (or similar) privilege is defined, but there is no role that includes that privilege
                    // Therefore, the item cannot be loaded (by nobody except an administrator).
                    if (roleIds.Length == 0) roleIds = new int[] { 0 };
                    //TODO: resolve the roleIds.Length == 0 situation elsewhere
                    //if (roleIds.Length == 0) throw new EntityUnauthorizedException(String.Format("Not authorized to view {0} (no role exists)", item == null ? PluralCaption : GetItemTerm(item)), this, null, userId);

                    // Get the list of domains for which the user has the view privilege on items of the entity type
                    // The domain restriction check needs to be performed only if the privilege is defined (otherwise everybody has the privilege)
                    int[] domainIds = Domain.GetGrantScope(context, userId, groupIds, roleIds);
                    if (context.ConsoleDebug) Console.WriteLine("DOMAINS: {0}", domainIds == null ? "GLOBAL GRANT" : String.Join(",", domainIds));

                    // no domain (unauthorised) -> select "not granted" for all items
                    // some domains (partly authorised) -> if entity can be assigned to domains: filter result by domains in domainIds, otherwise: select grant value according to items' domains (or true in case of list, because list already filters)
                    // NULL domain (globally authorised) -> no domain filtering in query

                    string domainMatchSql = TopTable.HasDomainReference && domainIds != null && domainIds.Length != 0 ? String.Format("t.{0} IN ({1})", TopTable.DomainReferenceField, String.Join(",", domainIds)) : null; 

                    if (domainIds != null) {
                        if (domainIds.Length == 0 || !TopTable.HasDomainReference) grantSelectSql = ", false";
                        else if (TopTable.HasDomainReference) grantSelectSql = String.Format(", {0}", domainMatchSql);
                    }
                }
            }
            if (accessLevel != EntityAccessLevel.Administrator) {
                if (grantSelectSql == null) grantSelectSql = accessLevel == EntityAccessLevel.Permission ? ", false" : ", true";
                grantSelectSql += " AS _grant";
            }

            // Build WHERE clause
            if (list && TopTable.TypeReferenceField != null) {
                if (condition == null) condition = String.Empty; else condition += " AND ";
                condition += EntityType.GetTypeFilterCondition(this, String.Format("t.{0}", TopTable.TypeReferenceField));
            }

            string dataSelectSql = null;
            string idSelectSql = null;

            if (accessLevel == EntityAccessLevel.Administrator) {
                dataSelectSql = String.Format("{0}{1}{2}", coreSelectSql, permissionSelectSql, adminSelectSql);
                idSelectSql = String.Format("t.{0}{1}", TopTable.IdField, permissionSelectSql);
            } else {
                string userPermissionSelectSql;
                if (HasPermissionManagement) {
                    if (groupIdsStr == null) groupIdsStr = GetGroupIdsString(context, userId, groupIds);
                    userPermissionSelectSql = String.Format(userPermissionSelectSqlTemplate, userId, groupIdsStr);
                } else {
                    userPermissionSelectSql = String.Empty;
                }
                dataSelectSql = String.Format("{0}{1}{2}{3}{4}", coreSelectSql, grantSelectSql, permissionSelectSql, userPermissionSelectSql, normalSelectSql);
                idSelectSql = String.Format("t.{0}{1}{2}{3}", TopTable.IdField, grantSelectSql, permissionSelectSql, userPermissionSelectSql);
            }


            if (list) {
                if (sort == null) sort = String.Empty;
                else sort += ", ";
                sort += String.Format("t.{0}", TopTable.IdField);
                sort = String.Format(" ORDER BY {0}", sort);
            } else {
                sort = String.Empty;
            }

            string subset = (!list || limit == -1 ? String.Empty : String.Format(" LIMIT {0} OFFSET {1}", limit, offset));

            //Console.WriteLine(String.Format("QUERY SQL: SELECT {3}{4} FROM {0}{1}{2}{5};", joinSql, condition == null ? String.Empty : String.Format(" WHERE {0}", condition), aggregation, selectSql, HasMultipleTables ? "DISTINCT " : String.Empty, sort));
            return new object[] {
                    joinSql,
                    condition == null ? String.Empty : String.Format(" WHERE {0}", condition),
                    aggregationSql,
                    idSelectSql,
                    dataSelectSql,
                    HasMultipleTables ? "DISTINCT " : String.Empty,
                    sort,
                    subset
            };

        }

        /// <summary>Gets values to complete the aggregate condition based on the specified item visibility.</summary>
        /// <returns>A string array of two elements: the database ID of a user to be matched; the database IDs of groups to be matched.</returns>
        /// <param name="visibility">Visibility.</param>
        private string GetVisibilityAggregationClause(EntityItemVisibility visibility, EntityAccessLevel accessLevel, bool list) {

            // no result-limiting HAVING clause with administrator access or when single item is to be loaded (if unauthorized, exception is raised later)
            if (accessLevel == EntityAccessLevel.Administrator || !list) return String.Empty;

            string result = " HAVING ";
            if ((visibility & EntityItemVisibility.All) == EntityItemVisibility.All) {
                result += "_grant OR _usr_allow OR _grp_allow OR _global_allow";
            } else if ((visibility & EntityItemVisibility.Public) == EntityItemVisibility.Public) {
                result += "_global_allow";
                if ((visibility & EntityItemVisibility.Restricted) == EntityItemVisibility.Restricted) result += " OR (_usr_allow OR _grp_allow) AND (!_usr_allow OR _any_usr_allow+_any_grp_allow+_global_allow>1)";
                if ((visibility & EntityItemVisibility.Private) == EntityItemVisibility.Private) result += " OR _usr_allow AND _any_usr_allow+_any_grp_allow+_global_allow=1";
            } else if ((visibility & EntityItemVisibility.Restricted) == EntityItemVisibility.Restricted) {
                result += "!_global_allow AND (_usr_allow OR _grp_allow)";
                if ((visibility & EntityItemVisibility.Private) == 0) result += " AND (!_usr_allow OR _any_usr_allow+_any_grp_allow+_global_allow>1)";
            } else {
                result += "_usr_allow AND _any_usr_allow+_any_grp_allow+_global_allow=1";
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets values to complete the aggregate selection fields for the database query based on the specified user and groups.</summary>
        /// <returns>A string array of two elements: the database ID of a user to be matched; the database IDs of groups to be matched.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">Database ID of a user.</param>
        /// <param name="groupIds">Database IDs of groups. If the value is <c>null</c>, the user's group are selected if necessary.</param>
        private string GetGroupIdsString(IfyContext context, int userId, int[] groupIds) {
            if (HasPermissionManagement && groupIds == null) groupIds = context.GetQueryIntegerValues(String.Format("SELECT id_grp FROM usr_grp WHERE id_usr={0}", userId));
            if (groupIds.Length == 0) return "0";
            return String.Join(",", groupIds);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Lists all roles that include the specified privilege for this entity type or, alternatively, any privilege for this entity type other than the specified one.</summary>
        /// <returns>A list that contains the database IDs of all matching roles. If the list is empty, there is no matching role. If it is <c>null</c>, the privilege is not defined, in which case it would not make sense to deny authorisation.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="operation">The operation that, in combination with the entity type represented by this instance, defines the privilege.</param>
        /// <param name="inverse">If <c>false</c>, roles are selected if they contain the privilege; if <c>true</c>, roles are selected if they contain any of the other privileges related to this entity type.</param>
        public int[] GetRolesForPrivilege(IfyContext context, EntityOperationType operation, bool inverse) {
            string condition = String.Format("p.id_type={0} AND ", TopTypeId);
            condition += String.Format("p.operation{1}='{0}';", (char)operation, inverse ? "!" : String.Empty);
            string sql = String.Format("{0} WHERE {1}", RolePrivilegeBaseQuery, condition);
            return GetRolesForPrivilege(context, sql);
        }

        /// <summary>Lists all roles that include the specified privileges for this entity type or, alternatively, any privilege for this entity type other than the specified ones.</summary>
        /// <returns>A list that contains the database IDs of all matching roles. If the list is empty, there is no matching role. If it is <c>null</c>, the privilege is not defined, in which case it would not make sense to deny authorisation.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="operations">The operations that, in combination with the entity type represented by this instance, defines the privileges.</param>
        /// <param name="inverse">If <c>false</c>, roles are selected if they contain the privileges; if <c>true</c>, roles are selected if they contain any of the other privileges related to this entity type.</param>
        public int[] GetRolesForPrivilege(IfyContext context, EntityOperationType[] operations, bool inverse) {
            string condition = String.Format("p.id_type={0} AND ", TopTypeId);
            string ops = null;
            foreach (EntityOperationType operation in operations) {
                if (ops == null) ops = String.Empty;
                else ops += ",";
                ops += String.Format("'{0}'", (char)operation);
            }
            condition += String.Format("p.operation{1} IN ({0});", ops, inverse ? " NOT" : String.Empty);
            string sql = String.Format("{0} WHERE {1}", RolePrivilegeBaseQuery, condition);
            return GetRolesForPrivilege(context, sql);
        }

        protected int[] GetRolesForPrivilege(IfyContext context, string sql) {
            List<int> result = new List<int>();
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            bool privilegeExists = false;
            while (reader.Read()) {
                privilegeExists = true;
                if (reader.GetValue(0) == DBNull.Value) continue;
                result.Add(reader.GetInt32(0));
            }
            context.CloseQueryResult(reader, dbConnection);
            if (!privilegeExists) return null; // privilege does not exist at all -> calling code has to drop privilege-based authorisation check in this case
            return result.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetAdministratorOperationsQuery(int userId, int itemId) {

            string join = "priv AS t INNER JOIN role_priv AS t1 ON t.id=t1.id_priv INNER JOIN usr_role AS t2 ON t1.id_role=t2.id_role";
            string condition = String.Format("t.id_type={0} AND t2.id_usr={1}", TopTypeId, userId);

            if (itemId != 0 && TopTable.HasDomainReference) {
                join += String.Format(" INNER JOIN role AS t3 ON t1.id_role=t3.id INNER JOIN {0} AS t4 ON t4.conf_deleg OR t3.id_domain=t4.id_domain", TopTable.Name);
                condition += String.Format(" AND t4.id={0}", itemId);
            }

            return String.Format("SELECT DISTINCT(t.operation) FROM {0} WHERE {1};",
                    join,
                    condition
            );
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the SQL conditional expression to obtain a list of entities with the filtering criteria expressed in the specified template.</summary>
        /// <remarks>
        ///     This method is used to load correctly filtered lists for instances of <see cref="Terradue.Portal.EntityCollection"/>.
        ///     The template's properties that are not configured to be ignored for filtering and that have a value different from the IgnoreValue are included in the condition.
        ///     For the concept of templates, see <see cref="Terradue.Portal.EntityCollection"/>.
        /// </remarks>
        /// <param name="template">An instance of the represented Entity subclass for which the filtering properties are set to the desired filter values.</param>
        /// <param name="forDelete">Determines whether the condition will affect only the entity's top table so that it can be used in a <c>DELETE</c> command to delete multiple items.</param>
        /// <returns>The SQL conditional expression to be used in the <c>WHERE</c> clause of the filtering query.</returns>
        public string GetTemplateConditionSql(Entity template, bool forDelete) {
            string result = null;

            foreach (FieldInfo field in Fields) {
                if (field.FieldType != EntityFieldType.DataField && field.FieldType != EntityFieldType.ForeignField || forDelete && field.TableIndex != 0) continue;

                PropertyInfo propertyInfo = field.Property;
                Type type = propertyInfo.PropertyType;
                object value = propertyInfo.GetValue(template, null);

                if (value == null || Object.Equals(value, field.IgnoreValue) || type.IsEnum && Convert.ToInt32(value) == 0) continue;

                string alias = null;
                if (forDelete) {
                    alias = String.Empty;
                } else if (field.FieldType == EntityFieldType.ForeignField) {
                    ForeignTableInfo foreignTableInfo = null;
                    foreach (ForeignTableInfo fti in ForeignTables) {
                        if (fti.ReferringTable == Tables[field.TableIndex] && fti.SubIndex == field.TableSubIndex) {
                            foreignTableInfo = fti;
                            break;
                        }
                    }
                    if (foreignTableInfo == null) continue;
                    if (foreignTableInfo.IsMultiple) alias = String.Format("t{0}r{1}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.TableSubIndex.ToString());
                }
                if (alias == null) alias = String.Format("t{0}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString());
                string fieldExpression = (field.FieldName == null ? field.Expression.Replace("$(TABLE).", alias) : String.Format("{0}{1}", alias, field.FieldName));
                string term;
                if (type == typeof(bool)) term = String.Format("{1}{0}", fieldExpression, (bool)value ? String.Empty : "NOT ");
                else term = String.Format("{0}={1}", fieldExpression, StringUtils.ToSqlString(value));

                if (result == null) result = String.Empty; else result += " AND ";
                result += term;
            }
           
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Determines whether the specified foreign table needs to be considered in a filtering query according to the criteria of the specified template.</summary>
        /// <param name="foreignTableInfo">Foreign table info.</param>
        /// <param name="template">An instance of the represented Entity subclass for which the filtering properties are set to the desired filter values.</param>
        /// <returns><c>true</c> if there are filtering criteria related to the foreign table that therefore needs to be considered, <c>false</c> otherwise.</returns>
        private bool HasTemplateValuesSet(ForeignTableInfo foreignTableInfo, Entity template) {
            int index = -1;
            for (int i = 0; i < ForeignTables.Count; i++) {
                if (ForeignTables[i] == foreignTableInfo) index = i;
            }

            if (index == -1) return false;

            foreach (FieldInfo field in Fields) {
                if (field.TableSubIndex != foreignTableInfo.SubIndex || field.FieldType != EntityFieldType.DataField && field.FieldType != EntityFieldType.ForeignField) continue;

                PropertyInfo propertyInfo = field.Property;
                Type type = propertyInfo.PropertyType;
                object value = propertyInfo.GetValue(template, null);

                if (value == null || Object.Equals(value, field.IgnoreValue) || type.IsEnum && Convert.ToInt32(value) == 0) continue;

                return true;

            }

            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetKeywordFilterSql(string searchTerm, bool findWholeWords) {
            string result = null;
            List<string> fieldNames = new List<string>();

            if (TopTable.HasIdentifierField) fieldNames.Add(TopTable.IdentifierField);
            if (TopTable.HasNameField) fieldNames.Add(TopTable.NameField);
            foreach (FieldInfo field in Fields) {
                if (!field.IsUsedInKeywordSearch) continue;
                string alias = null;
                if (field.FieldType == EntityFieldType.ForeignField) {
                    ForeignTableInfo foreignTableInfo = null;
                    foreach (ForeignTableInfo fti in ForeignTables) {
                        if (fti.ReferringTable == Tables[field.TableIndex] && fti.SubIndex == field.TableSubIndex) {
                            foreignTableInfo = fti;
                            break;
                        }
                    }
                    if (foreignTableInfo == null) continue;
                    if (foreignTableInfo.IsMultiple) alias = String.Format("t{0}r{1}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.TableSubIndex.ToString());
                }
                if (alias == null) alias = String.Format("t{0}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString());

                fieldNames.Add(field.FieldName == null ? field.Expression.Replace("$(TABLE).", alias) : String.Format("{0}{1}", alias, field.FieldName));
            }
            if (fieldNames.Count == 0) return null;


            return String.Format("CONCAT_WS('\t', {0}) REGEXP '{2}{1}{3}'",
                    String.Join(",", fieldNames),
                    regexRegex.Replace(searchTerm.Replace("'", "''").Replace(@"\", @"\\"), @"\\$0"), 
                    findWholeWords ? "[[:<:]]" : String.Empty, 
                    findWholeWords ? "[[:>:]]" : String.Empty
            );
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetFilterSql(Dictionary<FieldInfo, string> filterValues) {
            if (filterValues == null) return null;

            string result = null;
            bool hasCondition = false;

            foreach (FieldInfo field in filterValues.Keys) {
                string condition = null;
                string searchTerm = filterValues[field];

                string alias = null;
                if (field.FieldType == EntityFieldType.ForeignField) {
                    ForeignTableInfo foreignTableInfo = null;
                    foreach (ForeignTableInfo fti in ForeignTables) {
                        if (fti.ReferringTable == Tables[field.TableIndex] && fti.SubIndex == field.TableSubIndex) {
                            foreignTableInfo = fti;
                            break;
                        }
                    }
                    if (foreignTableInfo == null) continue;
                    if (foreignTableInfo.IsMultiple) alias = String.Format("t{0}r{1}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.TableSubIndex.ToString());
                }
                if (alias == null) alias = String.Format("t{0}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString());
                string fieldExpression = (field.FieldName == null ? field.Expression.Replace("$(TABLE).", alias) : String.Format("{0}{1}", alias, field.FieldName));

                if (field.Property.PropertyType == typeof(string)) {
                    condition = GetStringConditionSql(fieldExpression, searchTerm);
                } else if (field.Property.PropertyType == typeof(bool)) {
                    if (String.IsNullOrEmpty(searchTerm)) continue;
                    searchTerm = searchTerm.ToLower();

                    condition = String.Format("{1}{0}", fieldExpression, searchTerm == "true" || searchTerm == "yes" ? String.Empty : "!");
                } else if (field.Property.PropertyType == typeof(int) || field.Property.PropertyType == typeof(long) || field.Property.PropertyType == typeof(double)) {
                    condition = GetNumericConditionSql(fieldExpression, searchTerm);
                } else if (field.Property.PropertyType == typeof(DateTime)) { 
                    condition = GetDateTimeConditionSql(fieldExpression, searchTerm);
                } else if (field.Property.PropertyType.IsEnum) {
                    condition = GetNumericConditionSql(fieldExpression, searchTerm);
                }

                if (condition == null) continue;

                if (hasCondition) result += " AND ";
                else result = String.Empty;

                result += condition;

                hasCondition = true;
            }

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetSortSql(Dictionary<FieldInfo, SortDirection> sortCriteria) {
            if (sortCriteria == null) return null;

            string result = null;
            bool hasSort = false;

            foreach (FieldInfo field in sortCriteria.Keys) {
                string alias = null;
                if (field.FieldType == EntityFieldType.ForeignField) {
                    ForeignTableInfo foreignTableInfo = null;
                    foreach (ForeignTableInfo fti in ForeignTables) {
                        if (fti.ReferringTable == Tables [field.TableIndex] && fti.SubIndex == field.TableSubIndex) {
                            foreignTableInfo = fti;
                            break;
                        }
                    }
                    if (foreignTableInfo == null) continue;
                    if (foreignTableInfo.IsMultiple) alias = String.Format("t{0}r{1}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.TableSubIndex.ToString());
                }
                if (alias == null) alias = String.Format("t{0}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString());
                string fieldExpression = (field.FieldName == null ? field.Expression.Replace("$(TABLE).", alias) : String.Format("{0}{1}", alias, field.FieldName));
                string sortTerm = String.Format("{0} {1}", fieldExpression, sortCriteria[field] == SortDirection.Ascending ? "ASC" : "DESC");

                if (hasSort) result += ", ";
                else result = String.Empty;

                result += sortTerm;

                hasSort = true;
            }

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public FieldInfo GetField(string propertyName) {
            if (propertyName == "Id") return new FieldInfo(ClassType.GetProperty("Id"), 0, TopTable.IdField);
            if (propertyName == "Identifier") {
                if (TopTable.HasIdentifierField) return new FieldInfo (ClassType.GetProperty ("Identifier"), 0, TopTable.IdentifierField);
            } else if (propertyName == "Name") {
                if (TopTable.HasNameField) return new FieldInfo (ClassType.GetProperty ("Name"), 0, TopTable.NameField);
            } else if (propertyName == "DomainId") {
                if (TopTable.HasDomainReference) return new FieldInfo (ClassType.GetProperty ("DomainId"), 0, TopTable.DomainReferenceField);
            } else if (propertyName == "OwnerId") {
                if (TopTable.HasOwnerReference) return new FieldInfo (ClassType.GetProperty ("OwnerId"), 0, TopTable.OwnerReferenceField);
            } else {
                foreach (FieldInfo field in Fields) {
                    if (field.Property.Name == propertyName) return field;
                }
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Translates human-readable search values for this entity type from a parameter collection into a single value containing IDs of items.</summary>
        /// <returns>A comma-separated list of database IDs of matching items. This string value can be used as a search filter with an EntityCollection (by using SetFilter on an *Id property with this value).</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="parameters">A NameValueCollection containing the parameters (e.g. the query string).</param>
        /// <param name="parameterName">The name of the parameter containing the search values (identifiers or names of the items to be found).</param>
        /// <param name="searchIdentifiers">Decides whether the search values should be present among the items' identifiers.</param>
        /// <param name="searchNames">Decides whether the search values should be present among the items' names.</param>
        public string GetIdFilterString(IfyContext context, NameValueCollection parameters, string parameterName, bool searchIdentifiers = true, bool searchNames = false) {
            List<string> values = new List<string>();
            if (parameters == null || parameters[parameterName] == null) return null;

            int[] ids = GetIds(context, parameters.GetValues(parameterName), searchIdentifiers, searchNames);
            if (ids == null || ids.Length == 0) return null;

            return String.Join(",", ids);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Translates human-readable search values for this entity type to the corresponding database IDs.</summary>
        /// <returns>An array of database IDs of the matching items.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="values">Search values (identifiers or names of the items to be found).</param>
        /// <param name="searchIdentifiers">Decides whether the search values should be present among the items' identifiers.</param>
        /// <param name="searchNames">Decides whether the search values should be present among the items' names.</param>
        public int[] GetIds(IfyContext context, string[] values, bool searchIdentifiers = true, bool searchNames = false) {
            string valueList = null;
            int count = 0;
            foreach (string value in values) {
                if (valueList == null) valueList = String.Empty;
                else valueList += ",";
                valueList += StringUtils.EscapeSql(value);
                count++;
            }
            if (count == 0) return new int[0];
            string term = String.Format("{1}{2}{0}{3}", valueList, count == 1 ? "=" : " IN ", count == 1 ? String.Empty : "(", count == 1 ? String.Empty : ")");
            string condition = null;
            if (searchIdentifiers && TopTable.HasIdentifierField) {
                condition = String.Format("t.{0}{1}", TopTable.IdentifierField, term);
            }
            if (searchNames && TopTable.HasNameField) {
                if (condition == null) condition = String.Empty;
                else condition = condition + " OR ";
                condition += String.Format("t.{0}{1}", TopTable.NameField, term);
            }

            object[] parts = GetQueryParts(context, true, 0, null, EntityItemVisibility.All, String.Format("({0})", condition), null, -1, 0, EntityAccessLevel.Administrator);
            string sql = GetQuery(parts);
            return context.GetQueryIntegerValues(sql);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns an entity instance of the represented type from its ID.</summary>
        /// <returns>The entity instance, that has to be fully loaded by the calling code.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The entity item's ID.</param>
        /// \ingroup Persistence
        public Entity GetEntityInstanceFromId(IfyContext context, int id) {
            if (TopTable.HasExtensions) {
                Entity result;
                if (TopTable.TypeReferenceField != null) {
                    int entityTypeId = context.GetQueryIntegerValue(String.Format("SELECT {3} FROM {0} WHERE {1}={2};", TopTable.Name, TopTable.IdField, id, TopTable.TypeReferenceField));
                    if (entityTypeId != 0) {
                        EntityType entityType = null;
                        foreach (EntityType et in entityTypes.Values) {
                            if (et.Id != entityTypeId) continue;
                            entityType = et;
                            break;
                        }
                        if (entityType != null) {
                            result = entityType.GetEntityInstance(context);
                            return result;
                        }
                    }
                }

                result = GetEntityInstance(context, String.Format("t.{0}={1}", TopTable.IdField, id));
                if (result != null) return result;
                if (DoesItemExist(context, id)) throw new Exception("Invalid entity extension type");
                throw new EntityNotFoundException(String.Format("{0} item [{1}] not found", SingularCaption == null ? ClassName : SingularCaption, id), this, id.ToString());
            }
            return GetEntityInstance(context);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns an entity instance of the represented type from its unique identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="identifier">The entity item's unique identifier.</param>
        /// <returns>The entity instance.</returns>
        /// \ingroup Persistence
        public Entity GetEntityInstanceFromIdentifier(IfyContext context, string identifier) {
            if (TopTable.HasExtensions) {
                Entity result;
                if (TopTable.TypeReferenceField != null) {
                    int entityTypeId = context.GetQueryIntegerValue(String.Format("SELECT {3} FROM {0} WHERE {1}={2};", TopTable.Name, TopTable.IdentifierField, StringUtils.EscapeSql(identifier), TopTable.TypeReferenceField));
                    if (entityTypeId != 0) {
                        EntityType entityType = null;
                        foreach (EntityType et in entityTypes.Values) {
                            if (et.Id != entityTypeId) continue;
                            entityType = et;
                            break;
                        }
                        if (entityType != null) {
                            result = entityType.GetEntityInstance(context);
                            return result;
                        }
                    }
                }

                result = GetEntityInstance(context, String.Format("t.{0}={1}", TopTable.IdentifierField, StringUtils.EscapeSql(identifier)));
                if (result != null) return result;
                if (DoesItemExist(context, identifier)) throw new Exception("Invalid entity extension type");
                throw new EntityNotFoundException(String.Format("{0} item \"{1}\" not found", SingularCaption == null ? ClassName : SingularCaption, identifier), this, identifier);
            }
            return GetEntityInstance(context);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns an entity instance of the represented type from an SQL conditional expression.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="condition">An SQL conditional expression (without the <c>WHERE</c> keyword identifying the desired item.</param>
        /// <param name="sort">An optional SQL sort expression (without the <c>ORDER BY</c> keyword) to make the preferred item appear first in case of multiple matches.</param>
        /// <returns>The entity instance.</returns>
        /// \ingroup Persistence
        public Entity GetEntityInstanceFromCondition(IfyContext context, string condition, string sort) {
            if (TopTable.HasExtensions) {
                Entity result = GetEntityInstance(context, String.Format("{0}{1}", condition, sort == null ? String.Empty : " ORDER BY " + sort));
                if (result != null) return result;
                throw new Exception("Item not found or invalid entity extension type");
            }
            return GetEntityInstance(context);
        }

        //---------------------------------------------------------------------------------------------------------------------

        private Entity GetEntityInstance(IfyContext context, string condition) {
            string className;
            if (TopTable.TypeReferenceField != null) className = context.GetQueryStringValue(String.Format("SELECT CASE WHEN t1.custom_class IS NULL THEN t1.class ELSE t1.custom_class END FROM {0} AS t INNER JOIN type AS t1 ON t.{1}=t1.id WHERE {2};", TopTable.Name, TopTable.TypeReferenceField, condition));
            else className = context.GetQueryStringValue(String.Format("SELECT t.{2} FROM {0} AS t WHERE {1};", TopTable.Name, condition, TopTable.TypeField));
            if (className != null) {
                Type type = Type.GetType(className, true);
                if (type != null) {
                    System.Reflection.ConstructorInfo constructorInfo = type.GetConstructor(new Type[]{typeof(IfyContext)});
                    if (constructorInfo != null) return (Entity)constructorInfo.Invoke(new object[] {context});
                }
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary></summary>
        public Entity GetEntityExtensionInstance(IfyContext context, int extensionTypeId) {
            string className = context.GetQueryStringValue(String.Format("SELECT e.class FROM type AS t WHERE t.id={0};", extensionTypeId));
            if (className != null) {
                Type type = Type.GetType(className, true);
                if (type != null) {
                    System.Reflection.ConstructorInfo constructorInfo = type.GetConstructor(new Type[]{typeof(IfyContext)});
                    if (constructorInfo != null) return (Entity)constructorInfo.Invoke(new object[] {context}); 
                }

            }
            throw new Exception("Invalid entity extension type");
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets an empty entity instance of the represented type.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>The entity instance.</returns>
        /// \ingroup Persistence
        public Entity GetEntityInstance(IfyContext context) {
            System.Reflection.ConstructorInfo constructorInfo = null;
            if (CustomClassType != null) constructorInfo = CustomClassType.GetConstructor(new Type[]{typeof(IfyContext)});
            if (constructorInfo == null && ClassType != null && !ClassType.IsAbstract) constructorInfo = ClassType.GetConstructor(new Type[]{typeof(IfyContext)});
            if (constructorInfo == null && GenericClassType != null) constructorInfo = GenericClassType.GetConstructor(new Type[]{typeof(IfyContext)});
            if (constructorInfo == null) throw new NullReferenceException(String.Format("No suitable constructor found for {0}", ClassType.FullName));
            return (Entity)constructorInfo.Invoke(new object[]{context});
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Deletes the item with the specified database ID exists from the database.</summary>
        /// <remarks>
        ///     This method should be used if there is no instance of an Entity subclass representing the item to be deleted.
        ///     Using this method avoids creating an instance (which includes database access for loading its data) that would be for the only purpose to call its <see cref="Terradue.Portal.Entity.Delete"/> method.
        /// </remarks>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The database ID of the item.</param>
        public void DeleteItem(IfyContext context, int id) {
            context.Execute(String.Format("DELETE FROM {1} WHERE {2}={0};", id, TopTable.Name, TopTable.IdField));
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Deletes the item with the specified unique identifier exists from the database.</summary>
        /// <remarks>
        ///     This method should be used if there is no instance of an Entity subclass representing the item to be deleted.
        ///     Using this method avoids creating an instance (which includes database access for loading its data) that would be for the only purpose to call its <see cref="Terradue.Portal.Entity.Delete"/> method.
        /// </remarks>
        /// <param name="context">The execution environment context.</param>
        /// <param name="identifier">The unique identifier of the item.</param>
        public void DeleteItem(IfyContext context, string identifier) {
            context.Execute(String.Format("DELETE FROM {1} WHERE {2}={0};", StringUtils.EscapeSql(identifier), TopTable.Name, TopTable.IdentifierField));
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a string representing the item for exception messages or similar.</summary>
        /// <param name="item">The item for which the term is to be created.</param>
        public string GetItemTerm(Entity item) {
            return TopTable.HasIdentifierField && item.Identifier != null ? GetItemTerm(item.Identifier) : GetItemTerm(item.Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a string representing the item for exception messages or similar.</summary>
        /// <param name="id">The item's database ID.</param>
        public string GetItemTerm(int id) {
            return String.Format("{0} [{1}]", SingularCaption == null ? ClassType.Name : SingularCaption, id.ToString());
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a string representing the item for exception messages or similar.</summary>
        /// <param name="identifier">The item's unique identifier.</param>
        public string GetItemTerm(string identifier) {
            return String.Format("{0} \"{1}\"", SingularCaption == null ? ClassType.Name : SingularCaption, identifier);
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void PrepareSql() {
            //bool restrictedList = list && context.RestrictedMode;
            //bool includePrivileges = !context.AdminMode && TopTable.HasPermissionManagement;
            //bool excludeUnaccessibleItems = restrictedList && includePrivileges;
            bool distinct = false;

            // Build join
            int permissionSubjectTableIndex = -1;

            string s = String.Format("{0} AS t", TopTable.Name);
            restrictedJoinSql = s;
            unrestrictedJoinSql = s;
            adminJoinSql = s;


            for (int i = 0; i < Tables.Count; i++) {
                EntityTableAttribute table = Tables[i];
                if (i != 0) {
                    s = String.Format(" INNER JOIN {0} AS t{1} ON t{2}.{4}=t{1}.{3}", 
                        table.Name,
                        i,
                        i == 1 ? String.Empty : (i - 1).ToString(),
                        table.IdField,
                        Tables[i - 1].IdField
                    );
                    restrictedJoinSql += s;
                    unrestrictedJoinSql += s;
                    adminJoinSql += s;
                }

                if (table == PermissionSubjectTable) {
                    permissionSubjectTableIndex = i;
                    s = String.Format("JOIN {0} AS p ON t{1}.id=p.id_{2}",
                        table.PermissionTable,
                        i == 0 ? String.Empty : i.ToString(),
                        table.Name
                    );
                    restrictedJoinSql += String.Format(" INNER {0}", s);
                    unrestrictedJoinSql += String.Format(" LEFT {0}", s);
                    adminJoinSql += String.Format(" LEFT {0}", s);
                }
                for (int j = 0; j < ForeignTables.Count; j++) {
                    if (ForeignTables[j].ReferringTable != table) continue;
                    s = String.Format(" {0} JOIN {1}", 
                        ForeignTables[j].IsRequired ? "INNER" : "LEFT",
                        ForeignTables[j].Join
                    );
                    distinct |= ForeignTables[j].IsMultiple;
                    restrictedJoinSql += s;
                    unrestrictedJoinSql += s;
                    adminJoinSql += s;
                }
            }

            s = String.Format("t.{0}", TopTable.IdField);
            coreSelectSql = s;

            // Add generic identifying fields to SELECT clause
            s = String.Empty;
            if (TopTable.HasIdentifierField) s += String.Format(", t.{0}", TopTable.IdentifierField);
            if (TopTable.HasNameField) s += String.Format(", t.{0}", TopTable.NameField);
            if (TopTable.HasDomainReference) s += String.Format(", t.{0}", TopTable.DomainReferenceField);
            if (TopTable.HasOwnerReference) s += String.Format(", t.{0}", TopTable.OwnerReferenceField);

            coreSelectSql += s;

            if (HasPermissionManagement) {
                permissionSelectSql = String.Format(", SUM(p.id_usr IS NOT NULL) AS _any_usr_allow, SUM(p.id_grp IS NOT NULL) AS _any_grp_allow, MAX(p.id_{0} IS NOT NULL AND p.id_usr IS NULL AND p.id_grp IS NULL) AS _global_allow", PermissionSubjectTable.Name);
                userPermissionSelectSqlTemplate = ", MAX(p.id_usr IS NOT NULL AND p.id_usr={0}) AS _usr_allow, MAX(p.id_grp IS NOT NULL AND p.id_grp IN ({1})) AS _grp_allow";
            }

            normalSelectSql = String.Empty;
            adminSelectSql = String.Empty;

            // Add entity-specific fields to SELECT clause 
            foreach (FieldInfo field in Fields) {
                if (field.TableIndex == permissionSubjectTableIndex && field.FieldType == EntityFieldType.PermissionField) {
                    s = String.Format(", MAX(CASE WHEN p.id_{1} IS NULL THEN NULL ELSE p.{0} END) AS _{0}", field.FieldName, PermissionSubjectTable.Name);
                    normalSelectSql += s;

                } else if (field.FieldType == EntityFieldType.DataField) {
                    if (field.FieldName == null) s = String.Format(", {0}", field.Expression.Replace("$(TABLE).", String.Format("t{0}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString())));
                    else s = String.Format(", t{0}.{1}", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.FieldName);
                    normalSelectSql += s;
                    adminSelectSql += s;

                } else if (field.FieldType == EntityFieldType.ForeignField) {
                    ForeignTableInfo foreignTable = null;
                    for (int j = 0; j < ForeignTables.Count; j++) {
                        foreignTable = ForeignTables[j];
                        if (foreignTable.IsMultiple || foreignTable.ReferringTable != Tables[field.TableIndex] || foreignTable.SubIndex != field.TableSubIndex) {
                            foreignTable = null;
                            continue;
                        }
                        break;
                    }
                    if (foreignTable == null) s = ", NULL";
                    else if (field.FieldName == null) s = String.Format(", {0}", field.Expression.Replace("$(TABLE).", String.Format("t{0}r{1}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.TableSubIndex)));
                    else s = String.Format(", t{0}r{1}.{2}", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.TableSubIndex, field.FieldName);
                    normalSelectSql += s;
                    adminSelectSql += s;
                }
            }

            isSqlPrepared = true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the SQL conditional expression for a search string that can consist of several string values and takes also into account wildcard characters.</summary>
        /// <returns>The SQL expression that, if applied to a list query, yields <c>true</c> for item records that match the given condition.</returns>
        /// <param name="name">The expression against which the search term is compared, usually the qualified name of a table field.</param>
        /// <param name="searchTerm">The search term. It can contain multiple values separated by comma. The values may contain wildcards such as <em>*</em> or <em>?</em>.</param>
        public static string GetStringConditionSql(string name, string searchTerm) {
            if (searchTerm == null) return null;

            string result = String.Empty, exclude = String.Empty;

            Match match = Regex.Match(searchTerm, @"^\{([^\}]+)\}$");
            if (match.Success) searchTerm = match.Groups[1].Value;

            string[] terms = StringUtils.SplitSimply(searchTerm, ',');
            bool interval = false, like;
            string cl = null, cr = null, cn = null;
            for (int i = 0; i < terms.Length; i++) {
                like = false;

                cn = terms[i];//StringUtils.EscapeSql(terms[i]).Replace("\\\\\\\\", "\\\\");
                if (Regex.Match(cn, @"[\*\?]").Success) {
                    int shift = 0, shift2 = 0;
                    string cn2 = cn;
                    MatchCollection matches = Regex.Matches(cn, @"(\\.|[\*\?_%])");
                    int index;
                    for (int j = 0; j < matches.Count; j++) {
                        switch (matches[j].Value) {
                            case "\\*" :
                            case "\\?" :
                                index = matches[j].Index + shift--;
                                cn = String.Format("{0}{1}", cn.Substring(0, index), cn.Substring(index + 1));
                                index = matches[j].Index + shift2--;
                                cn2 = String.Format("{0}{1}", cn2.Substring(0, index), cn2.Substring(index + 1));
                                break;
                            case "*" :
                            case "?" :
                                like = true;
                                index = matches[j].Index + shift; 
                                cn = String.Format("{0}{1}{2}", cn.Substring(0, index), matches[j].Value == "*" ? "%" : "_", cn.Substring(index + 1));
                                break;
                            case "%" :
                            case "_" :
                                index = matches[j].Index + shift++; 
                                cn = String.Format("{0}\\{1}", cn.Substring(0, index), cn.Substring(index));
                                break;
                            default:
                                if (matches[j].Value == "\\\\") break;
                                index = matches[j].Index + shift--; 
                                cn = String.Format("{0}{1}", cn.Substring(0, index), cn.Substring(index + 1));
                                break;
                        }
                    }
                    if (!like) cn = cn2;
                }

                int tl = cn.Length;
                cl = cn[0].ToString();
                cr = cn[tl - 1].ToString();
                if (tl > 1 && (cl == "[" || cl == "]" || cl == "(")) cn = cn.Substring(1, --tl); else cl = String.Empty;
                if (tl > 1 && cn[tl - 2] != '\\' && (cr == "]" || cr == "[" || cr == ")")) cn = cn.Substring(0, --tl); else cr = String.Empty;

                if (like && (cl == String.Empty || cr == String.Empty)) {
                    cn = String.Format("{0}{1}{2}", cl, cn, cr);
                    cl = String.Empty;
                    cr = String.Empty;
                }

                if (cl != String.Empty) cl = (cl == "[" ? ">=" : ">");
                if (cr != String.Empty) cr = (cr == "]" ? "<=" : "<");

                cn = String.Format("'{0}'", cn.Replace("'", "''"));

                if (!like && cl != String.Empty && cr == String.Empty) result += String.Format("{0}{1}{2}{3}", result == String.Empty ? String.Empty : " OR ", name, cl, cn);
                else if (!like && cl == String.Empty && cr != String.Empty) result += String.Format("{0}{1}{2}{3}", interval ? " AND " : result == String.Empty ? String.Empty : " OR ", name, cr, cn);
                else if (cl == ">" && cr == "<") exclude += String.Format("{0}{1}{2}{3}", exclude == String.Empty ? String.Empty : " AND ", name, like ? " NOT LIKE " : "!=", cn);
                else result += String.Format("{0}{1}{2}{3}", result == String.Empty ? String.Empty : " OR ", name, like ? " LIKE " : "=", cn);

                interval = (!like && cl != String.Empty && cr == String.Empty);
            }
            if (result != String.Empty) result = String.Format("({0})", result);
            if (exclude != String.Empty) result += String.Format("{0}{1}", result == String.Empty ? String.Empty : " AND ", exclude);
            if (result == String.Empty) result = null;
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the SQL conditional expression for a search string related to a numeric search.</summary>
        /// <returns>The SQL expression that, if applied to a list query, yields <c>true</c> for item records that match the given condition.</returns>
        /// <param name="name">The expression against which the search term is compared, usually the qualified name of a table field.</param>
        /// <param name="searchTerm">The search term. It can contain multiple values separated by comma. The values may contain interval the delimiter characters <em>[</em>, <em>]</em>, <em>(</em> and <em>)</em>.</param>
        public static string GetNumericConditionSql(string name, string searchTerm) {
            if (searchTerm == null) return null;

            string result = String.Empty, exclude = String.Empty;

            Match match = Regex.Match(searchTerm, @"^\{([^\}]+)\}$");
            if (match.Success) searchTerm = match.Groups[1].Value;

            string[] terms = searchTerm.Split(',');
            bool interval = false;
            string cl = null, cr = null, cn = null;
            for (int i = 0; i < terms.Length; i++) {
                match = Regex.Match(terms[i], @"^([\[\]\(])?(-?([0-9]+)|([0-9]*\.[0-9]+))([\[\]\)])?$");
                if (!match.Success) {
                    interval = false;
                    continue;
                }

                cl = match.Groups[1].Value;
                cr = match.Groups[5].Value;
                cn = match.Groups[2].Value;
                if (cl != String.Empty) cl = (cl == "[" ? ">=" : ">");
                if (cr != String.Empty) cr = (cr == "]" ? "<=" : "<");

                if (cl != String.Empty && cr == String.Empty) result += String.Format("{0}{1}{2}{3}", result == String.Empty ? String.Empty : " OR ", name, cl, cn);
                else if (cl == String.Empty && cr != String.Empty) result += String.Format("{0}{1}{2}{3}", interval ? " AND " : result == String.Empty ? String.Empty : " OR ", name, cr, cn);
                else if (cl == ">" && cr == "<") exclude += String.Format("{0}{1}!={2}", exclude == String.Empty ? String.Empty : " AND ", name, cn);
                else result += String.Format("{0}{1}={2}", result == String.Empty ? String.Empty : " OR ", name, cn);

                interval = (cl != String.Empty && cr == String.Empty);
            }
            if (result != String.Empty) result = String.Format("({0})", result);
            if (exclude != String.Empty) result += String.Format("{0}{1}", result == String.Empty ? String.Empty : " AND ", exclude);
            if (result == String.Empty) result = null;
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the SQL conditional expression for a search string related to a temporal search.</summary>
        /// <returns>The SQL expression that, if applied to a list query, yields <c>true</c> for item records that match the given condition.</returns>
        /// <param name="name">The expression against which the search term is compared, usually the qualified name of a table field.</param>
        /// <param name="searchTerm">The search term. It can contain multiple numeric values separated by comma. The values may contain interval the delimiter characters <em>[</em>, <em>]</em>, <em>(</em> and <em>)</em>.</param>
        public static string GetDateTimeConditionSql(string expression, string value) {
            return GetDateTimeConditionSql(expression, value, TimeZoneInfo.Utc);
        }

        public static string GetDateTimeConditionSql(string name, string searchTerm, TimeZoneInfo timeZoneInfo) {
            if (searchTerm == null) return null;

            string result = String.Empty, exclude = String.Empty;

            Match match = Regex.Match(searchTerm, @"^\{([^\}]+)\}$");
            if (match.Success) searchTerm = match.Groups[1].Value;

            string[] terms = searchTerm.Split(',');
            bool interval = false;
            string cl = null, cr = null, cn = null, ln = null;
            for (int i = 0; i < terms.Length; i++) {
                match = Regex.Match(terms[i], @"^([\[\]\(])?([0-9PTZYMDHShms\/\-\:\.]+)?([\[\]\)])?$");
                if (!match.Success) continue;

                // Check for ISO 8601 interval slash syntax (1) or mathematcial interval syntax (2)
                if (match.Groups[2].Value.IndexOf('/') != -1) { // (1)
                    string[] parts = StringUtils.Split(match.Groups[2].Value, '/');
                    if (parts.Length != 2) continue;

                    DateTime dt;
                    if (!DateTime.TryParse(parts[0], out dt)) continue;
                    try {
                        dt = TimeZoneInfo.ConvertTimeToUtc(dt, timeZoneInfo);
                    } catch (Exception) {}
                    cn = String.Format(@"'{0:yyyy\-MM\-dd\THH\:mm\:ss'", dt);

                    DateTime ldt = dt;
                    ln = cn;

                    if (!DateTime.TryParse(parts[1], out dt)) {
                        try {
                            dt = TimeZoneInfo.ConvertTimeToUtc(dt, timeZoneInfo);
                        } catch (Exception) {}

                        match = Regex.Match(parts[1], @"^P((\d+)Y)?((\d+)M)?((\d+)D)?((\d+)H)?((\d+)S)?$");
                        if (!match.Success) continue;

                        int[] times = new int[6];
                        for (int j = 0; j < 6; j++) Int32.TryParse(match.Groups[2 * j + 2].Value, out times[j]);

                        dt = ldt.AddYears(times[0]);
                        dt = dt.AddMonths(times[1]);
                        dt = dt.AddDays(times[2]);
                        dt = dt.AddHours(times[3]);
                        dt = dt.AddMinutes(times[4]);
                        dt = dt.AddSeconds(times[5]);
                    }

                    //if (dt.TimeOfDay.TotalSeconds == 0) dt = dt.Add(new TimeSpan(1, 0, 0, 0));
                    cn = String.Format(@"'{0:yyyy\-MM\-dd\THH\:mm\:ss'", dt);
                    result += String.Format("{0}({1}>={2} AND {1}<{3})", result == String.Empty ? String.Empty : " OR ", name, ln, cn);

                    interval = false;

                } else { // (2)
                    cl = match.Groups[1].Value;
                    cr = match.Groups[3].Value;
                    cn = match.Groups[2].Value;

                    DateTime dt;

                    if (!DateTime.TryParse(cn, out dt)) {
                        interval = false;
                        continue;
                    }
                    try {
                        dt = TimeZoneInfo.ConvertTimeToUtc(dt, timeZoneInfo);
                    } catch (Exception) {}

                    if (cl != String.Empty) cl = (cl == "[" ? ">=" : ">");
                    if (cr != String.Empty) cr = (cr == "]" ? "<=" : "<");

                    ln = "'" + dt.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\.fff") + "'";

                    if ((cr == "<=" || cl != String.Empty && cr != String.Empty || cl == String.Empty && cr == String.Empty) && !Regex.Match(cn, "T.+").Success) {
                        dt = dt.AddDays(1);
                        if (cr == "<=") cr = "<";
                    }
                    cn = "'" + dt.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\.fff") + "'";

                    if (cl != String.Empty && cr == String.Empty) result += String.Format("{0}{1}{2}{3}", result == String.Empty ? String.Empty : " OR ", name, cl, cn);
                    else if (cl == String.Empty && cr != String.Empty) result += String.Format("{0}{1}{2}{3}", interval ? " AND " : result == String.Empty ? String.Empty : " OR ", name, cr, cn);
                    else if (cl == ">" && cr == "<") exclude += String.Format("{0}{1}<{2} AND {1}>={3}", exclude == String.Empty ? String.Empty : " AND ", name, ln, cn);
                    else if (cn == ln) result += String.Format("{0}{1}={2}", result == String.Empty ? String.Empty : " OR ", name, cn);
                    else result += String.Format("{0}{1}>={2} AND {1}<{3}", result == String.Empty ? String.Empty : " OR ", name, ln, cn);

                    interval = (cl != String.Empty && cr == String.Empty);
                }
            }
            if (result != String.Empty) String.Format("({0})", result);
            if (exclude != String.Empty) result += String.Format("{0}{1}", result == String.Empty ? String.Empty : " AND ", exclude);
            if (result == String.Empty) result = null;
            return result;
        }
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class EntityRelationshipType : EntityType {

        //---------------------------------------------------------------------------------------------------------------------

        public override EntityTableAttribute TopStoreTable {
            get { return Tables[TopStoreTableIndex]; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override int TopStoreTableIndex {
            get { return Tables.Count - 1; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityRelationshipType(Type type) : base(type) {
        }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityRelationshipType(Type type, PropertyInfo referencingProperty) : base(type) {
            AddRelationship(referencingProperty);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void AddRelationship(PropertyInfo referencingProperty) {
            string tableName = null;
            string referringItemField = null;
            string referencedItemField = null;

            foreach (System.Attribute attribute in referencingProperty.GetCustomAttributes(true)) {
                if (attribute is EntityRelationshipAttribute) {
                    tableName = (attribute as EntityRelationshipAttribute).Name;
                    referringItemField = (attribute as EntityRelationshipAttribute).ReferringItemField;
                    referencedItemField = (attribute as EntityRelationshipAttribute).ReferencedItemField;
                }
            }

            if (referringItemField == null) referringItemField = String.Format("id_{0}", GetOrAddEntityType(referencingProperty.DeclaringType).TopTable.Name);
            if (referencedItemField == null) referencedItemField = String.Format("id_{0}", TopTable.Name);

            EntityTableAttribute table;
            if (Tables.Count != 0 && (tableName == null || Tables[Tables.Count - 1].Name == tableName)) {
                table = Tables[Tables.Count - 1];
            } else {
                table = new EntityTableAttribute(tableName, EntityTableConfiguration.Custom);
                table.ReferringItemField = referringItemField;
                table.IdField = referencedItemField;
                Tables.Add(table);
            }
        
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class TableInfo {

        private bool autoCheckIdentifiers = true;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the database table that holds the items of the entity.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string Name { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the table's primary key field.</summary>
        /// <remarks>By default, it is assumed that the primary key field is named <c>id</c> and of numeric type.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string IdField { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the primary key values are generated automatically by the underlying database management system.</summary>
        /// <remarks>
        ///     By default, the value is <c>true</c>, which normally means that the primary key value increases automatically with every new inserted record (through a sequence or auto_increment flag).
        ///     Otherwise, an item's Id (<see cref="Terradue.Portal.Entity.Id"/>) must be set to a value different from 0 before the item is stored.
        /// </remarks>
        public bool HasAutomaticIds { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of an alternative unique key field for the table.</summary>
        /// <remarks>By default, it is assumed that the alternative key field is named <c>identifier</c> and of a character type.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string IdentifierField { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the table has an alternative unique key.</summary>
        public bool HasIdentifierField {
            get { return IdentifierField != null; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the identifier of an item is checked for uniqueness among the items of the entity type.</summary>
        public bool AutoCheckIdentifiers {
            get {
                return autoCheckIdentifiers || AutoCorrectDuplicateIdentifiers;
            }
            set {
                autoCheckIdentifiers = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the identifier of a new item is automatically made unique if the chosen identifier already exists for an existing item.</summary>
        /// <remarks>
        ///     If this is false (the default case), a <see cref="DuplicateEntityIdentifierException"/> is thrown in case of a duplicate. 
        ///     The property value applies only to new items. If an item is changed and its identifier conflicts with another one, the exception is always thrown.
        /// </remarks>
        public bool AutoCorrectDuplicateIdentifiers { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the table field containing the human-readable name of an item.</summary>
        /// <remarks>By default, it is assumed that this field is named <c>name</c> and of a character type.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string NameField { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the table has a field containing human-readable names.</summary>
        public bool HasNameField {
            get { return NameField != null; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the field containing the fully qualified extension type name.</summary>
        /// <remarks>By default, it is assumed that the field is named <c>type</c>.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string TypeField { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the field referencing the extension type.</summary>
        /// <remarks>By default, it is assumed that the field is named <c>id_type</c>.</remarks>
        public string TypeReferenceField { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the entity is designed to have specialized extensions.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public bool HasExtensions {
            get { return TypeReferenceField != null || TypeField != null; }
            set { 
                if (value) {
                    if (TypeReferenceField == null) TypeReferenceField = "id_type";
                } else {
                    TypeReferenceField = null;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the field referencing the domain of an item.</summary>
        /// <remarks>By default, it is assumed that the field is named <c>id_domain</c>.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string DomainReferenceField { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the .</summary>
        public bool HasDomainReference {
            get { return DomainReferenceField != null; }  
            set { 
                if (value) {
                    if (DomainReferenceField == null) DomainReferenceField = "id_domain";
                } else {
                    DomainReferenceField = null;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the field referencing the user owning an item.</summary>
        /// <remarks>By default, it is assumed that the field is named <c>id_usr</c>.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string OwnerReferenceField { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the .</summary>
        public bool HasOwnerReference {
            get { return OwnerReferenceField != null; }
            set { 
                if (value) {
                    if (OwnerReferenceField == null) OwnerReferenceField = "id_usr";
                } else {
                    OwnerReferenceField = null;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the table containing the permissions on the entity items for users and groups.</summary>
        /// <remarks>By default, it is assumed that the table's name is the main table's name appended by <c>_perm</c> (previously it was <c>_priv</c>, but this was changed to make the distinction between privileges and permissions clearer).</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string PermissionTable { get; set; }

        [Obsolete("Use PermissionTable (changed for terminology consistency)")]
        public string PrivilegeTable { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the .</summary>
        public bool HasPermissionManagement {
            get { return PermissionTable != null; }
            set { 
                if (value) {
                    if (PermissionTable == null) PermissionTable = Name + "_perm";
                } else {
                    PermissionTable = null;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool HasNestedData { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the information in the database table is required for the entity.</summary>
        /// <remarks>If the value is <c>true</c> (the default value) the selecting SQL query uses an <c>INNER JOIN</c>; otherweise a <c>LEFT JOIN</c>. This setting has only effect on tables that come after the first (or top) table in the join.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public bool IsRequired { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityTableStorage Storage { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string ReferringItemField { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public TableInfo(EntityTableAttribute attribute) {
            /*this.Name = name;
            this.IdField = DefaultIdFieldName;
            this.HasAutomaticIds = true;
            if (config == EntityTableConfiguration.Full) {
                this.IdentifierField = DefaultIdentifierFieldName;
                this.NameField = DefaultNameFieldName;
            }
            this.Storage = EntityTableStorage.Here;
            this.IsRequired = true;*/
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ForeignTableInfo {
        public string Name { get; protected set; }
        public string Join { get; protected set; }
        public int SubIndex { get; protected set; }
        public bool IsRequired { get; protected set; }
        public bool IsMultiple { get; protected set; }

        /// <summary>Gets or sets the name of the database table that holds the items of the entity.</summary>
        public EntityTableAttribute ReferringTable { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public ForeignTableInfo(EntityTableAttribute tableInfo, int tableIndex, EntityForeignTableAttribute attribute) {
            this.Name = attribute.Name;
            this.Join = String.Format("{0} AS t{1}r{2} ON t{1}.{4}=t{1}r{2}.{3}", 
                    attribute.Name,
                    tableIndex == 0 ? String.Empty : tableIndex.ToString(),
                    attribute.SubIndex,
                    attribute.IdField,
                    attribute.ReferenceField
            );
            this.SubIndex = attribute.SubIndex;
            this.IsRequired = attribute.IsRequired;
            this.ReferringTable = tableInfo;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public ForeignTableInfo(EntityTableAttribute tableInfo, int tableIndex, EntityMultipleForeignTableAttribute attribute) {
            this.Name = attribute.RelationshipName;
            this.Join = String.Format("{0} AS t{1}r{2} ON t{1}.{3}=t{1}r{2}.{4}", 
                    attribute.RelationshipName,
                    tableIndex == 0 ? String.Empty : tableIndex.ToString(),
                    attribute.SubIndex,
                    tableInfo.IdField,
                    attribute.ReferenceField
            );
            this.SubIndex = attribute.SubIndex;
            this.IsRequired = attribute.IsRequired;
            this.IsMultiple = true;
            this.ReferringTable = tableInfo;
        }


        public ForeignTableInfo(string name, string join, int subIndex, bool isMultiple) {
            this.Name = name;
            this.Join = join;
            this.SubIndex = subIndex;
            this.IsMultiple = isMultiple;
        }
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class FieldInfo {
        public PropertyInfo Property { get; protected set; }
        public EntityFieldType FieldType { get; protected set; }
        public int TableIndex { get; protected set; }
        public int TableSubIndex { get; protected set; }
        public string FieldName { get; protected set; }
        public string Expression { get; protected set; }
        public bool IsReadOnly { get; protected set; }
        public bool IsForeignKey { get; protected set; }
        public bool IsUsedInKeywordSearch { get; protected set; }
        public object NullValue { get; protected set; }
        public object IgnoreValue { get; protected set; }
        public Type UnderlyingType { get; protected set; }
        public bool AutoLoad { get; protected set; }
        public bool AutoStore { get; protected set; }
        public bool IsList { get; protected set; }
        public bool IsDictionary { get; protected set; }
        public string ReferenceField { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public FieldInfo(PropertyInfo @property, int tableIndex, string fieldName) {
            this.Property = @property;
            this.TableIndex = tableIndex;
            this.FieldName = fieldName;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public FieldInfo(PropertyInfo @property, int tableIndex, EntityPermissionFieldAttribute attribute) : this(@property, tableIndex, attribute.Name) {
            this.FieldType = EntityFieldType.PermissionField;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public FieldInfo(PropertyInfo @property, int tableIndex, EntityDataFieldAttribute attribute) : this(@property, tableIndex, attribute.Name) {
            if (attribute.Name == null) this.Expression = attribute.Expression;
            this.FieldType = EntityFieldType.DataField;
            this.IsReadOnly = attribute.IsReadOnly;
            this.IsForeignKey = attribute.IsForeignKey;
            this.IsUsedInKeywordSearch = attribute.IsUsedInKeywordSearch;
            Type type = @property.PropertyType;
            if (attribute.NullValue != null) this.NullValue = attribute.NullValue;
            else if (this.IsForeignKey) this.NullValue = 0;
            else if (type == typeof(DateTime)) this.NullValue = DateTime.MinValue;
            if (attribute.IgnoreValue != null) this.IgnoreValue = attribute.IgnoreValue;
            else SetIgnoreValue(type);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public FieldInfo(PropertyInfo @property, int tableIndex, EntityForeignFieldAttribute attribute) : this(@property, tableIndex, attribute.Name) {
            this.FieldType = EntityFieldType.ForeignField;
            this.TableSubIndex = attribute.TableSubIndex;
            if (attribute.IgnoreValue != null) this.IgnoreValue = attribute.IgnoreValue;
            else SetIgnoreValue(@property.PropertyType);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public FieldInfo(PropertyInfo @property, int tableIndex, EntityEntityFieldAttribute attribute) : this(@property, tableIndex, attribute.Name) {
            this.FieldType = EntityFieldType.EntityField;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public FieldInfo(PropertyInfo @property, int tableIndex, EntityRelationshipAttribute attribute) : this(@property, tableIndex, attribute.Name) {
            this.FieldType = EntityFieldType.RelationshipField;
        }

        //---------------------------------------------------------------------------------------------------------------------
/*
        public FieldInfo(PropertyInfo @property, int tableIndex, EntityComplexFieldAttribute attribute) : this(@property, tableIndex, null as string) {
            this.FieldType = EntityFieldType.ComplexField;
            this.ReferenceField = attribute.ReferenceField;
            this.AutoLoad = attribute.AutoLoad;
            this.AutoStore = attribute.AutoStore;
            if (Property.PropertyType.IsGenericType) {
                if (Property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) {
                    this.IsList = true;
                    this.UnderlyingType = Property.PropertyType.GetGenericArguments()[0];
                } else if (Property.PropertyType.GetGenericTypeDefinition() == typeof(EntityDictionary<>)) {
                    this.IsDictionary = true;
                    this.UnderlyingType = Property.PropertyType.GetGenericArguments()[0];
                }
            } else {
                this.UnderlyingType = Property.PropertyType;
            }
        }
*/
        //---------------------------------------------------------------------------------------------------------------------

        public void SetIgnoreValue(Type type) {
            if (this.IsForeignKey) {
                IgnoreValue = 0;
            } else if (type.IsValueType && Nullable.GetUnderlyingType(type) == null) {
                IgnoreValue = Activator.CreateInstance(type);
            }
        }        
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    
    
    public enum EntityFieldType {
        PermissionField,
        DataField,
        ForeignField,
        EntityField,
        //ComplexField,
        RelationshipField
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Levels of access to entity items used for restricting item selections.</summary>
    /// <remarks>
    ///     <para>The meaning of these values depend on the situation: if used before loading an entity item, the value applies the corresponding filtering rules. After the item has been loaded, the property Entity.AccessLevel contains the maximum access level applicable for the loading user (Entity.UserId), which can be the same as the one used for the selection or lower.</para>
    ///     <para><c>None</c> if used before selecting entity items, the value of the current IfyContext.AccessLevel applies. After the item has been loaded, the value <c>None</c> means that the access is denied.</para>
    ///     <para><c>Privilege</c> and <c>Permission</c> limit the access to items, but follow different principles. Based on the application's authorisation policies and the way the authorisation framework is used, access checks using these modes yield different results. <c>Privilege</c> is considered a higher level because a single grant of a role with a privilege can apply to a large number of entity items.</para>
    ///     <para><c>Privilege</c> is based on roles granted to a user group or a single user directly either for a specific domain or globally. It is suitable for applications that make use of roles and privileges where users are considered small administrators with a limited scope.</para>
    ///     <para><c>Permission</c>, on the other hand, is based on direct assignments of items to user groups or users without considering roles, privileges and domains. This is suitable when there is less need for a complex privilege management.</para>
    ///     <para>The <c>Administrator</c> level does not limit the access to items in any way.</para>
    /// </remarks>
    public enum EntityAccessLevel {

        /// <summary>Use the context's access mode; otherwise indicator for denied access.</summary>
        None,

        /// <summary>Allow access only for items for which the user has specific permissions.</summary>
        Permission,

        /// <summary>Allow access only for items for which the user has been granted search or view privileges and enforce other privilege-based as well.</summary>
        Privilege,

        /// <summary>Apply no restrictions at all and ignore permission or grant information for items.</summary>
        Administrator

    }


    /// <summary>Enumaration for the visibility of an item and for restricting the items within a collection.</summary>
    [Flags]
    public enum EntityItemVisibility {

        /// <summary>All items to which a user has access are included in collection.</summary>
        All = 0x07,

        /// <summary>Public items to which a user has access are included in collection.</summary>
        Public = 0x01,

        /// <summary>Items to which a user has access via user or group permissions are included in collection, except public and private ones.</summary>
        Restricted = 0x02,

        /// <summary>Items to which a user has exclusive access via user permissions are included in collection.</summary>
        Private = 0x04,

        /// <summary>Limits the items in the collection to those owned by the user.</summary>
        OwnedOnly = 0x08

    }


}

