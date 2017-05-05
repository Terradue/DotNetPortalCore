using System;

namespace Terradue.Portal {


    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Attribute that allows to link a subclass of Entity to a database table.</summary>
    /// <remarks>This attribute is used in combination with the EntityDataFieldAttribute attributes at property level.</remarks>
    /// \ingroup Persistence
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class EntityTableAttribute : System.Attribute {
        
        /// <summary>The default name of the database ID field: <c>id</c>.</summary>
        public const string DefaultIdFieldName = "id";

        /// <summary>The default name of the unique identifier field: <c>identifier</c>.</summary>
        public const string DefaultIdentifierFieldName = "identifier";

        /// <summary>The default name of the human-readable name field: <c>name</c>.</summary>
        public const string DefaultNameFieldName = "name";

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

        /// <summary>Indicates or decides whether it is possible assign direct permissions on items of this entity type to users and groups.</summary>
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

        [Obsolete("Use HasPermissionManagement (changed for terminology consistency)")]
        public bool HasPrivilegeManagement {
            get { return HasPermissionManagement; }
            set { 
                HasPermissionManagement = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether entity items of the related type can have nested data.</summary>
        public bool HasNestedData { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the information in the database table is required for the entity.</summary>
        /// <remarks>If the value is <c>true</c> (the default value) the selecting SQL query uses an <c>INNER JOIN</c>; otherweise a <c>LEFT JOIN</c>. This setting has only effect on tables that come after the first (or top) table in the join.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public bool IsRequired { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the relative position of the class that is responsible for the storage of the property values.</summary>
        public EntityTableStorage Storage { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the relative position of the class that is responsible for the storage of the property values.</summary>
        public string ReferringItemField { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether this entity provides a keyword search parameter even if there is no keyword-searchable property.</summary>
        public bool AllowsKeywordSearch { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityTableAttribute(string name, EntityTableConfiguration config) {
            this.Name = name;
            this.IdField = DefaultIdFieldName;
            this.HasAutomaticIds = true;
            if (config == EntityTableConfiguration.Full) {
                this.IdentifierField = DefaultIdentifierFieldName;
                this.NameField = DefaultNameFieldName;
            }
            this.Storage = EntityTableStorage.Here;
            this.IsRequired = true;
        }
        
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Attribute that allows to link a subclass of Entity to a database table.</summary>
    /// <remarks>This attribute is used in combination with the EntityDataFieldAttribute attributes at property level.</remarks>
    /// \ingroup Persistence
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class EntityRelationshipTableAttribute : EntityTableAttribute {

        //---------------------------------------------------------------------------------------------------------------------

        public EntityRelationshipTableAttribute(string name, string referringItemField, string referencedItemField) : base(name, EntityTableConfiguration.Custom) {
            this.Name = name;
            this.ReferringItemField = referringItemField;
            this.IdField = referencedItemField;
            this.HasAutomaticIds = false;
            this.Storage = EntityTableStorage.Here;
            this.IsRequired = true;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Attribute that allows to link a referenced table to the entity's main table or a subtable.</summary>
    /// <remarks>This attribute is used in combination with the EntityForeignFieldAttribute attributes at property level.</remarks>
    //[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public abstract class EntityForeignTableAttribute : System.Attribute {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the database table that holds the items of the entity.</summary>
        public string Name { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the database table that holds the items of the entity.</summary>
        public int SubIndex { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the table's primary key field.</summary>
        /// <remarks>By default, it is assumed that the primary key field is named <c>id</c> and of numeric type.</remarks>
        public string IdField { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the foreign key field linking from the main table to the foreign table.</summary>
        /// <remarks>By default, it is assumed that the primary key field is named <c>id_</c>+<i>table name</i> and of numeric type.</remarks>
        public string ReferenceField { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the information in the database table is required for the entity.</summary>
        /// <remarks>If the value is <c>true</c>, the selecting SQL query uses an <c>INNER JOIN</c>; otherweise a <c>LEFT JOIN</c>.</remarks>
        public bool IsRequired { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected EntityForeignTableAttribute() {}

        //---------------------------------------------------------------------------------------------------------------------

        public EntityForeignTableAttribute(string name, int subIndex) {
            this.Name = name;
            this.SubIndex = subIndex;
        }
        
    }

    
    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Attribute that allows to link a referenced table to the entity's main table or a subtable.</summary>
    /// <remarks>This attribute is used in combination with the EntityForeignFieldAttribute attributes at property level.</remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [Obsolete("No longer supported")]
    public class EntityExtensionTableAttribute : EntityForeignTableAttribute {

        //---------------------------------------------------------------------------------------------------------------------

        protected EntityExtensionTableAttribute() {}

        //---------------------------------------------------------------------------------------------------------------------

        public EntityExtensionTableAttribute(string name, int subIndex) : base(name, subIndex)  {
            this.Name = name;
            this.SubIndex = subIndex;
            this.IdField = EntityTableAttribute.DefaultIdFieldName;
            this.ReferenceField = EntityTableAttribute.DefaultIdFieldName;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Attribute that represents a relationships.</summary>
    /// <remarks>This attribute is used on properties that are  combination with the EntityDataFieldAttribute attributes at property level.</remarks>
    /// \ingroup Persistence
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    [Obsolete("No longer supported")]
    public class EntityRelationshipAttribute : System.Attribute {

        public string Name { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the table's primary key field.</summary>
        /// <remarks>By default, it is assumed that the primary key field is named <c>id</c> and of numeric type.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string ReferringItemField { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the table's primary key field.</summary>
        /// <remarks>By default, it is assumed that the primary key field is named <c>id</c> and of numeric type.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string ReferencedItemField { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityRelationshipAttribute(string name) {
            this.Name = name;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Attribute that allows to link a referenced table to the entity's main table or a subtable.</summary>
    /// <remarks>This attribute is used in combination with the EntityForeignFieldAttribute attributes at property level.</remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class EntityReferenceTableAttribute : EntityForeignTableAttribute {

        //---------------------------------------------------------------------------------------------------------------------

        protected EntityReferenceTableAttribute() {}

        //---------------------------------------------------------------------------------------------------------------------

        public EntityReferenceTableAttribute(string name, int subIndex) : base(name, subIndex) {
            this.Name = name;
            this.SubIndex = subIndex;
            this.IdField = EntityTableAttribute.DefaultIdFieldName;
            this.ReferenceField = EntityTableAttribute.DefaultIdFieldName + "_" + name;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Attribute that allows relationship tables to link to the entity's main table.</summary>
    /// <remarks>This attribute is used in combination with the EntityForeignFieldAttribute attributes at property level.</remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class EntityMultipleForeignTableAttribute : EntityReferenceTableAttribute {

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the relationship table.</summary>
        public string RelationshipName { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityMultipleForeignTableAttribute(string name, string relationshipName, int subIndex) {
            this.Name = name;
            this.RelationshipName = relationshipName;
            this.SubIndex = subIndex;
            this.ReferenceField = EntityTableAttribute.DefaultIdFieldName + "_" + name;
            this.IdField = EntityTableAttribute.DefaultIdFieldName;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    [AttributeUsage(AttributeTargets.Property)]
    public class EntityDataFieldAttribute : System.Attribute {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the database table field where the property's value is stored.</summary>
        public string Name { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets a short caption describing what kind of information the field contains.</summary>
        public string Caption { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the SQL expression to obtain the property's value.</summary>
        public string Expression { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the field is read-only.</summary>
        /// <remarks>For example, a field that is calculated using an SQL expression, must be read-only.</remarks>
        public bool IsReadOnly { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the maximum length in bytes for string-type values.</summary>
        public int MaxLength { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the value that represents the database <c>NULL</c> value.</summary>
        public object NullValue { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the value for which no filtering condition is added to a list query.</summary>
        public object IgnoreValue { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the field is a foreign key field.</summary>
        public bool IsForeignKey { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the field is included in keyword searches.</summary>
        public bool IsUsedInKeywordSearch { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityDataFieldAttribute(string name) {
            this.Name = name;
            if (this.Name == null) this.IsReadOnly = true;
        }
        
    }

    
    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    [AttributeUsage(AttributeTargets.Property)]
    public class EntityPermissionFieldAttribute : System.Attribute {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the database table field where the property's value is stored.</summary>
        public string Name { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets a short caption describing what kind of information the field contains.</summary>
        public string Caption { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityPermissionFieldAttribute(string name) {
            this.Name = name;
        }
        
    }



    [Obsolete("Use EntityPermissionFieldAttribute (changed for terminology consistency)")]
    public class EntityPrivilegeFieldAttribute : EntityPermissionFieldAttribute {

        //---------------------------------------------------------------------------------------------------------------------

        public EntityPrivilegeFieldAttribute(string name) : base(name) {}

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Attribute that allows to link a field in a foreign table to an instance property.</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EntityForeignFieldAttribute : System.Attribute {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the database table field where the property's value is stored.</summary>
        public string Name { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets a short caption describing what kind of information the field contains.</summary>
        public string Caption { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the database table that holds the items of the entity.</summary>
        public int TableSubIndex { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the SQL expression to obtain the property's value.</summary>
        public string Expression { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the value for which no filtering condition is added to a list query.</summary>
        public object IgnoreValue { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityForeignFieldAttribute(string name, int tableSubIndex) {
            this.Name = name;
            this.TableSubIndex = tableSubIndex;
        }
        
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    [AttributeUsage(AttributeTargets.Property)]
    public class EntityEntityFieldAttribute : System.Attribute {

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the database table field where the property's value is stored.</summary>
        public string Name { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets a short caption describing what kind of information the field contains.</summary>
        public string Caption { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityEntityFieldAttribute(string name) {
            this.Name = name;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    
/*
    /// <summary>Attribute that allows to link a multiple field in a foreign table to an instance property.</summary>
    /// <remarks>The property on which the attribute is applied should be of a type using the EntityTable attribute, or a List&lt;T&gt; (with T being such a type).</remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class EntityComplexFieldAttribute : System.Attribute {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets a short caption describing what kind of information the field contains.</summary>
        public string Caption { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the foreign key field linking from the multiple table to the main table.</summary>
        /// <remarks>By default, it is assumed that the primary key field is named <c>id_</c>+<i>table name</i> and of numeric type.</remarks>
        public string ReferenceField { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the property value is loadad automatically when the parent item is loaded.</summary>
        public bool AutoLoad { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the property value is loadad automatically when the parent item is loaded.</summary>
        public bool AutoStore { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public EntityComplexFieldAttribute() {
        }
        
    }
*/


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public enum EntityTableConfiguration {
        Full,
        Custom
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Possible locations of the database table information for the database fields defined in a class in relation to that class.</summary>
    public enum EntityTableStorage {
        
        /// <summary>The database tab belong to</summary>
        /// <remarks>This is the most common and default case.</remarks>
        Here,
        
        /// <summary>The database fields defined in a class are part of the same database table that contains the fields of the superclass.</summary>
        /// <remarks>This is used for subclasses that have their database information in the same table than their superclass, example: the sublcasses of Scheduler.</remarks>
        Above,

        /// <summary>The database fields defined in the class are part of the database in the next subclass that has table information.</summary>
        /// <remarks>This is used for abstract classes that define properties for which database fields are defined in all subclasses, example: ServiceDerivate.</remarks>
        Below
        
    }

}

