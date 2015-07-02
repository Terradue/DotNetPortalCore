using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Terradue.Util;


/*!
\defgroup Persistence Persistence of Data
@{
The component provides generic interaction with data that is persistently stored in a relational database. The data location and structure are defined in the classes that implement an entity and which represent real-world entities.

\xrefitem mvc_m "Model" "Model components"

\ingroup "Core"

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
    ///         <li>Create various types of queries, such as for loading a particular item or a list of items that may be filtered by specific criteria. These queries are used by methods of <see cref="Terradue.Portal.Entity"/> or <see cref="Terradue.Portal.EntityCollection"/> that load data from the database.</li>/
    ///         <li>Create correctly typed instances of Entity subclasses</li>
    ///         <li>Delete items of a specific type, by database ID or unique identifier (if applicable). This has the advantage that the item does not have to be instantiated before deleting it.</li>
    ///     </ul>
    /// </remarks>
    /// \ingroup Persistence
    public class EntityType : Entity {
        
        private static Dictionary<Type, EntityType> entityTypes = new Dictionary<Type, EntityType>();
        private static Dictionary<PropertyInfo, EntityRelationshipType> entityRelationshipTypes = new Dictionary<PropertyInfo, EntityRelationshipType>();

        private string singularCaption, pluralCaption;

        //---------------------------------------------------------------------------------------------------------------------
        
        public int TopTypeId { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the class name.</summary>
        /// \ingroup Persistence
        public string ClassName {
            get; protected set;
        }
        
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
        
        public bool HasPrivilegeManagement {
            get { return PrivilegeSubjectTable != null; }
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

        public EntityTableAttribute PrivilegeSubjectTable { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public List<EntityTableAttribute> Tables { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public List<ForeignTableInfo> ForeignTables { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public List<FieldInfo> Fields;

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new EntityType instance.</summary>
        /// \ingroup Persistence
        /// <param name="context">The execution environment context.</param>
        public EntityType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public EntityType(int id, int superTypeId, string className, string genericClassName, string customClassName, string singularCaption, string pluralCaption, string keyword, bool hasExtensions) : base(null) {
            if (IfyContext.DefaultConsoleDebug) Console.WriteLine("NEW ENTITY TYPE {0}", className);
            this.Id = id;
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
            
            this.TopTypeId = this.Id;
            
            Type type = ClassType;
            while (type != null) {
                EntityType entityType = EntityType.GetEntityType(type);
                if (entityType != null) this.TopTypeId = entityType.Id;
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
            TopTypeId = source.TopTypeId;
            /*ClassName = source.ClassName;
            GenericClassType = source.GenericClassType;
            CustomClassType = source.CustomClassType;*/
            TopTable = source.TopTable;
            PrivilegeSubjectTable = source.PrivilegeSubjectTable;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new EntityType instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>The created EntityType object.</returns>
        public static new EntityType GetInstance(IfyContext context) {
            return new EntityType(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static void LoadEntityTypes(IfyContext context) {
            entityTypes.Clear();
            IDataReader reader = context.GetQueryResult("SELECT t.id, t.id_super, t.class, t.generic_class, t.custom_class, t.caption_sg, t.caption_pl, t.keyword, COUNT(t1.id)>0 FROM type AS t LEFT JOIN type AS t1 ON t.id=t1.id_super GROUP BY t.id ORDER BY t.id_module, t.id_super IS NOT NULL, t.pos;");
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
                if (entityType.TopTable.HasPrivilegeManagement) result.Add(entityType);
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual void GetEntityStructure(Type type) {
            if (type == null || type == typeof(Entity)) return;
            
            EntityType baseEntityType = GetEntityType(type.BaseType);
            if (baseEntityType == null) {
                GetEntityStructure(type.BaseType);
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

            if (IfyContext.DefaultConsoleDebug) Console.WriteLine("TABLE {0}", tableInfo.Name + " PRIV=" + tableInfo.PrivilegeTable);
            
            int tableIndex = 0;
            
            if (tableInfo.Storage == EntityTableStorage.Here && tableInfo.Name != null) {
                Tables.Add(tableInfo);
                if (tableInfo.HasPrivilegeManagement) PrivilegeSubjectTable = tableInfo;
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
                    if (IfyContext.DefaultConsoleDebug) Console.WriteLine("MULTIPLE FOREIGN TABLE {0} - {1}", attr.RelationshipName, attr.Name);
                } else if (attribute is EntityForeignTableAttribute) {
                    EntityForeignTableAttribute attr = attribute as EntityForeignTableAttribute;
                    ForeignTableInfo foreignTableInfo = new ForeignTableInfo(tableInfo, tableIndex, attr);
                    ForeignTables.Add(foreignTableInfo);
                    if (IfyContext.DefaultConsoleDebug) Console.WriteLine("FOREIGN TABLE {0}", foreignTableInfo.Name);
                }
            }

            AppendProperties(type, tableIndex);
            
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected void AppendProperties(Type type, int tableIndex) {
            foreach (PropertyInfo pi in type.GetProperties()) {
                if (pi.DeclaringType != type) continue;
                foreach (System.Attribute attribute in pi.GetCustomAttributes(true)) {
                    if (attribute is EntityPrivilegeFieldAttribute) {
                        if (IfyContext.DefaultConsoleDebug) Console.WriteLine("  - [P] {0,-20} {1}", (attribute as EntityPrivilegeFieldAttribute).Name, " (" + pi.DeclaringType.Name + "." + pi.Name + ")");
                        Fields.Add(new FieldInfo(pi, tableIndex, attribute as EntityPrivilegeFieldAttribute));
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
            return context.GetQueryIntegerValue(GetQuery(context, 0, null, String.Format("t.{1}={0}", id, TopTable.IdField), false, true)) != 0;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the item with the specified unique identifier exists in the database.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="identifier">The unique identifier of the item.</param>
        /// <returns><c>true</c>, if item exists, <c>false</c> otherwise.</returns>
        public bool DoesItemExist(IfyContext context, string identifier) {
            return context.GetQueryIntegerValue(GetQuery(context, 0, null, String.Format("t.{1}={0}", StringUtils.EscapeSql(identifier), TopTable.IdentifierField), false, true)) != 0;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the SQL query for selecting a single item on behalf of the user with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user on whose behalf the item is selected.</param>
        /// <param name="condition">Additional SQL condition.</param>
        /// <returns>The SQL query.</returns>
        public string GetItemQuery(IfyContext context, int userId, string condition) {
            return GetQuery(context, userId, null, condition, false, false);
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the full or IDs-only SQL query for selecting a single item on behalf of the user with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The database ID of the user on whose behalf the item is selected.</param>
        /// <param name="condition">Additional SQL condition.</param>
        /// <param name="idsOnly">Decides whether the returned query selects only the database IDs of matching item.</param>
        /// <returns>The SQL query.</returns>
        public string GetListQuery(IfyContext context, int userId, string condition, bool idsOnly) {
            return GetQuery(context, userId, null, condition, true, idsOnly);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetListQueryWithTemplate(IfyContext context, int userId, Entity template, bool idsOnly) {
            string condition = (template == null ? null : GetTemplateCondition(template, false));
            return GetQuery(context, userId, template, condition, true, idsOnly);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string GetListQueryOfRelationship(IfyContext context, int userId, Entity referringItem, bool idsOnly) {
            string condition = String.Format("t{0}.{1}={2}", TopStoreTableIndex == 0 ? String.Empty : TopStoreTableIndex.ToString(), TopStoreTable.ReferringItemField, referringItem.Id);
            return GetQuery(context, userId, null, condition, true, idsOnly);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetListQueryForOwnedItems(IfyContext context, int userId, bool idsOnly) {
            string condition = null;
            if (userId != 0 && TopTable.HasOwnerReference) {
                if (condition == null) condition = String.Empty; else condition += " AND ";
                condition += String.Format("t.{0}={1}", TopTable.OwnerReferenceField, userId);
            }

            return GetQuery(context, userId, null, condition, true, idsOnly);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetGroupQuery(IfyContext context, int[] groupIds, bool idsOnly, string condition) {
            // Build join
            int privilegeSubjectTableIndex = -1;
            
            string join = String.Format("{0} AS t", TopTable.Name);
            string aggregation = null;

            for (int i = 0; i < Tables.Count; i++) {
                EntityTableAttribute table = Tables[i];
                if (i != 0) {
                    join += String.Format(" INNER JOIN {0} AS t{1} ON t{2}.{4}=t{1}.{3}", 
                            table.Name,
                            i,
                            i == 1 ? String.Empty : (i - 1).ToString(),
                            table.IdField,
                            Tables[i - 1].IdField
                    );
                }
                if (table == PrivilegeSubjectTable) {
                    privilegeSubjectTableIndex = i;
                    join += String.Format(" INNER JOIN {0} AS p ON t{1}.id=p.id_{2}",
                            table.PrivilegeTable,
                            i == 0 ? String.Empty : i.ToString(),
                            table.Name
                    );
                }
                for (int j = 0; j < ForeignTables.Count; j++) {
                    if (ForeignTables[j].IsMultiple || ForeignTables[j].ReferringTable != table) continue;
                    join += String.Format(" {0} JOIN {1}", 
                            ForeignTables[j].IsRequired ? "INNER" : "LEFT",
                            ForeignTables[j].Join
                    );
                }
            }
            
            // Build WHERE clause
            if (TopTable.HasPrivilegeManagement && groupIds != null && groupIds.Length != 0) {
                if (condition == null) condition = String.Empty; else condition += " AND ";
                condition += String.Format("p.id_grp{0}", groupIds.Length == 1 ? String.Format("={0}", groupIds[0].ToString()) : String.Format(" IN ({0})", String.Join(", ", groupIds)));
            }
            if (idsOnly) return String.Format("SELECT DISTINCT(t.{2}) FROM {0}{1};", join, condition == null ? String.Empty : String.Format(" WHERE {0}", condition), TopTable.IdField);

            // Add GROUP BY aggregation if necessary
            if (TopTable.HasPrivilegeManagement) aggregation = " GROUP BY t.id";

            // Add generic identifying fields to SELECT clause
            string select = String.Format("t.{0}", TopTable.IdField);
            if (TopTable.HasIdentifierField) select += String.Format(", t.{0}", TopTable.IdentifierField);
            if (TopTable.HasNameField) select += String.Format(", t.{0}", TopTable.NameField);
            if (TopTable.HasDomainReference) select += String.Format(", t.{0}", TopTable.DomainReferenceField);
            if (TopTable.HasOwnerReference) select += String.Format(", t.{0}", TopTable.OwnerReferenceField);
            if (!context.AdminMode && TopTable.HasPrivilegeManagement) {
                select += String.Format(", MAX(CASE WHEN p.id_usr IS NULL THEN 0 ELSE 1 END) AS _usr_allow, MAX(CASE WHEN p.id_grp IS NULL THEN 0 ELSE 1 END) AS _grp_allow, MAX(CASE WHEN p.id_{0} IS NOT NULL AND p.id_usr IS NULL AND p.id_grp IS NULL THEN 1 ELSE 0 END) AS _global_allow", PrivilegeSubjectTable.Name);
            }
            
            // Add entity-specific fields to SELECT clause 
            foreach (FieldInfo field in Fields) {
                if (field.TableIndex == privilegeSubjectTableIndex && field.FieldType == EntityFieldType.PrivilegeField) {
                    select += String.Format(", MAX(p.{0}) AS _{0}", field.FieldName, PrivilegeSubjectTable.Name);
                } else if (field.FieldType == EntityFieldType.DataField) {
                    if (field.FieldName == null) select += String.Format(", {0}", field.Expression.Replace("$(TABLE).", String.Format("t{0}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString())));
                    else select += String.Format(", t{0}.{1}", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.FieldName);
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
                    if (foreignTable == null) select += ", NULL";
                    else if (field.FieldName == null) select += String.Format(", {0}", field.Expression.Replace(", $(TABLE).", String.Format("t{0}r{1}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.TableSubIndex)));
                    else select += String.Format(", t{0}r{1}.{2}", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.TableSubIndex, field.FieldName);
                }
            }
            
            return String.Format("SELECT {3} FROM {0} WHERE {1}{2};", join, condition, aggregation, select);

        }
        
        //---------------------------------------------------------------------------------------------------------------------

        private string GetQuery(IfyContext context, int userId, Entity template, string condition, bool list, bool idsOnly) {

            bool restrictedList = list && context.RestrictedMode;
            bool includePrivileges = !context.AdminMode && TopTable.HasPrivilegeManagement;
            bool excludeUnaccessibleItems = restrictedList && includePrivileges;
            bool distinct = false;
            
            // Build join
            int privilegeSubjectTableIndex = -1;
            
            string join = String.Format("{0} AS t", TopTable.Name);
            string aggregation = null;

            for (int i = 0; i < Tables.Count; i++) {
                EntityTableAttribute table = Tables[i];
                if (i != 0) {
                    join += String.Format(" INNER JOIN {0} AS t{1} ON t{2}.{4}=t{1}.{3}", 
                            table.Name,
                            i,
                            i == 1 ? String.Empty : (i - 1).ToString(),
                            table.IdField,
                            Tables[i - 1].IdField
                    );
                }
                if (includePrivileges && table == PrivilegeSubjectTable) {
                    privilegeSubjectTableIndex = i;
                    join += String.Format(" {0} JOIN {1} AS p ON t{2}.id=p.id_{3} AND (p.id_usr IS NULL{5} OR p.id_usr={4} OR p.id_grp IS NOT NULL) LEFT JOIN usr_grp AS ug ON p.id_grp=ug.id_grp AND ug.id_usr={4}",
                            restrictedList ? "INNER" : "LEFT",
                            table.PrivilegeTable,
                            i == 0 ? String.Empty : i.ToString(),
                            table.Name,
                            userId,
                            restrictedList ? String.Empty : " AND p.id_grp IS NULL"
                    );
                }
                for (int j = 0; j < ForeignTables.Count; j++) {
                    if (!list && ForeignTables[j].IsMultiple || ForeignTables[j].ReferringTable != table) continue;
                    if (ForeignTables[j].IsMultiple && (template == null || !HasTemplateValuesSet(ForeignTables[j], template))) continue;
                    join += String.Format(" {0} JOIN {1}", 
                            ForeignTables[j].IsRequired ? "INNER" : "LEFT",
                            ForeignTables[j].Join
                    );
                    distinct |= ForeignTables[j].IsMultiple;
                }
            }

            // Build WHERE clause
            if (list && TopTable.TypeReferenceField != null) {
                if (condition == null) condition = String.Empty; else condition += " AND ";
                //condition += String.Format("t.{1}={0}", Id, TopTable.TypeReferenceField);
                condition += EntityType.GetTypeFilterCondition(this, String.Format("t.{0}", TopTable.TypeReferenceField));
            }
            if (excludeUnaccessibleItems) {
                if (condition == null) condition = String.Empty; else condition += " AND ";
                condition += String.Format("(p.id_usr={0} OR ug.id_usr={0} OR p.id_usr IS NULL AND p.id_grp IS NULL)", userId);
            }

            if (idsOnly) return String.Format("SELECT DISTINCT(t.{2}) FROM {0}{1};", join, condition == null ? String.Empty : String.Format(" WHERE {0}", condition), TopTable.IdField);

            // Add GROUP BY aggregation if necessary
            if (includePrivileges) aggregation = " GROUP BY t.id";

            // Add generic identifying fields to SELECT clause
            string select = String.Format("t.{0}", TopTable.IdField);
            if (TopTable.HasIdentifierField) select += String.Format(", t.{0}", TopTable.IdentifierField);
            if (TopTable.HasNameField) select += String.Format(", t.{0}", TopTable.NameField);
            if (TopTable.HasDomainReference) select += String.Format(", t.{0}", TopTable.DomainReferenceField);
            if (TopTable.HasOwnerReference) select += String.Format(", t.{0}", TopTable.OwnerReferenceField);
            if (includePrivileges) {
                select += String.Format(", MAX(CASE WHEN p.id_usr IS NULL THEN 0 ELSE 1 END) AS _usr_allow, MAX(CASE WHEN ug.id_grp IS NULL THEN 0 ELSE 1 END) AS _grp_allow, MAX(CASE WHEN p.id_{0} IS NOT NULL AND p.id_usr IS NULL AND p.id_grp IS NULL THEN 1 ELSE 0 END) AS _global_allow", PrivilegeSubjectTable.Name);
            }
            
            // Add entity-specific fields to SELECT clause 
            foreach (FieldInfo field in Fields) {
                if (includePrivileges && field.TableIndex == privilegeSubjectTableIndex && field.FieldType == EntityFieldType.PrivilegeField) {
                    select += String.Format(", MAX(CASE WHEN p.id_usr IS NOT NULL OR ug.id_grp IS NOT NULL OR p.id_{1} AND p.id_usr IS NULL AND p.id_grp IS NULL THEN p.{0} ELSE NULL END) AS _{0}", field.FieldName, PrivilegeSubjectTable.Name);
                } else if (field.FieldType == EntityFieldType.DataField) {
                    if (field.FieldName == null) select += String.Format(", {0}", field.Expression.Replace("$(TABLE).", String.Format("t{0}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString())));
                    else select += String.Format(", t{0}.{1}", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.FieldName);
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
                    if (foreignTable == null) select += ", NULL";
                    else if (field.FieldName == null) select += String.Format(", {0}", field.Expression.Replace("$(TABLE).", String.Format("t{0}r{1}.", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.TableSubIndex)));
                    else select += String.Format(", t{0}r{1}.{2}", field.TableIndex == 0 ? String.Empty : field.TableIndex.ToString(), field.TableSubIndex, field.FieldName);
                }
            }

            string sort = (list ? String.Format(" ORDER BY t.{0}", TopTable.IdField) : String.Empty);

            return String.Format("SELECT {3}{4} FROM {0}{1}{2}{5};", join, condition == null ? String.Empty : String.Format(" WHERE {0}", condition), aggregation, select, distinct ? "DISTINCT " : String.Empty, sort);

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
        public string GetTemplateCondition(Entity template, bool forDelete) {
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

        /// <summary>Returns an entity instance of the represented type from its ID.</summary>
        /// <returns>The entity instance.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The entity item's ID.</param>
        /// \ingroup Persistence
        public Entity GetEntityInstanceFromId(IfyContext context, int id) {
            if (TopTable.HasExtensions) {
                Entity result = GetEntityInstance(context, String.Format("t.{0}={1}", TopTable.IdField, id));
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
                Entity result = GetEntityInstance(context, String.Format("t.{0}={1}", TopTable.IdentifierField, StringUtils.EscapeSql(identifier)));
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
            if (constructorInfo == null && GenericClassType != null) constructorInfo = GenericClassType.GetConstructor(new Type[]{typeof(IfyContext)});
            if (constructorInfo == null && ClassType != null) constructorInfo = ClassType.GetConstructor(new Type[]{typeof(IfyContext)});
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

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class EntityRelationshipType : EntityType {

        //---------------------------------------------------------------------------------------------------------------------

        public override EntityTableAttribute TopStoreTable {
            get { return Tables[Tables.Count - 1]; }
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

        /// <summary>Gets or sets the name of the table containing the privileges on the entity items for users and groups.</summary>
        /// <remarks>By default, it is assumed that the table's name is the main table's name appended by <c>_priv</c>.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string PrivilegeTable { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the .</summary>
        public bool HasPrivilegeManagement {
            get { return PrivilegeTable != null; }
            set { 
                if (value) {
                    if (PrivilegeTable == null) PrivilegeTable = Name + "_priv";
                } else {
                    PrivilegeTable = null;
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
        public object NullValue { get; protected set; }
        public object IgnoreValue { get; protected set; }
        public Type UnderlyingType { get; protected set; }
        public bool AutoLoad { get; protected set; }
        public bool AutoStore { get; protected set; }
        public bool IsList { get; protected set; }
        public bool IsDictionary { get; protected set; }
        public string ReferenceField { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected FieldInfo(PropertyInfo @property, int tableIndex, string fieldName) {
            this.Property = @property;
            this.TableIndex = tableIndex;
            this.FieldName = fieldName;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public FieldInfo(PropertyInfo @property, int tableIndex, EntityPrivilegeFieldAttribute attribute) : this(@property, tableIndex, attribute.Name) {
            this.FieldType = EntityFieldType.PrivilegeField;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public FieldInfo(PropertyInfo @property, int tableIndex, EntityDataFieldAttribute attribute) : this(@property, tableIndex, attribute.Name) {
            if (attribute.Name == null) this.Expression = attribute.Expression;
            this.FieldType = EntityFieldType.DataField;
            this.IsReadOnly = attribute.IsReadOnly;
            this.IsForeignKey = attribute.IsForeignKey;
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
        PrivilegeField,
        DataField,
        ForeignField,
        EntityField,
        //ComplexField,
        RelationshipField
    }
    

}

