using System;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using Terradue.Util;
using Terradue.Portal;




//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Collections;





namespace Terradue.Gpod.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Provides properties for a references field (a foreign key field).</summary>
    public interface IReferenceField {
        string ReferenceTable { get; set; }
        string ReferenceTableAlias { get; set; }
        string ReferenceValueExpr { get; set; }
        string ReferenceIdField { get; set; }
        string ReferenceLinkField { get; set; }
        EntityData ReferenceEntity { get; set; }
        string NullCaption { get; set; }
        string SortExpression { get; set; }
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Provides properties for a multiple field.</summary>
    public interface IMultipleField {
        string MultipleTable { get; set; }
        string MultipleTableAlias { get; set; }
        string MultipleLinkField { get; set; }
        string MultipleIdField { get; set; }
        string Condition { get; set; }
        char ValueSeparator { get; set; }
        int[] DeleteIds { get; set; }
        int[] UpdateIds { get; set; }
        List<string[]> Rows { get; set; }
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Provides properties for an entity field.</summary>
    public interface IEntityField {
        EntityData ForeignEntity { get; set; }
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class FieldExpressionCollection : IEnumerable<FieldExpression> {

        private Dictionary<string, FieldExpression> dict = new Dictionary<string, FieldExpression>();

        //---------------------------------------------------------------------------------------------------------------------

        public int Count {
            get { return dict.Count; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void Add(FieldExpression item) {
            dict[item.Name] = item;
        }

        //---------------------------------------------------------------------------------------------------------------------

        IEnumerator<FieldExpression> IEnumerable<FieldExpression>.GetEnumerator() {
            return dict.Values.GetEnumerator();
        }

        //---------------------------------------------------------------------------------------------------------------------

        IEnumerator IEnumerable.GetEnumerator() {
            return dict.Values.GetEnumerator();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public FieldExpression this[string name] {
            get {
                if (!dict.ContainsKey(name)) return EntityData.EmptyField;
                return dict[name];
            }
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Abstract base class for fields of a data structure.</summary>
    public abstract class FieldExpression : TypedValue {
        protected FieldFlags flags; 
        protected string[] values;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the name of the field.</summary>
        public string Name { get; protected set; } 

        //---------------------------------------------------------------------------------------------------------------------

        public string ValueCaption { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the database field name of the field.</summary>
        public string Field { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the SQL expression for the selection of the field value.</summary>
        public string Expression { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets or sets the maximum length in characters the value can have.</summary>
        public string MaxLength { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the field is used for metadata or other reserved information.</summary>
        public bool Reserved { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the column index of the field in the query result set.</summary>
        public int ColumnIndex { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the OpenSearch extension name of the field.</summary>
        public string SearchExtension { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public TableInfo ExtensionTable { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the flags of the field.</summary>
        public FieldFlags Flags {
            get { return flags; }
            set { flags = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the values for a multiple field.</summary>
        public string[] Values {
            get { return values; }
            set { this.values = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string SqlValue { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the number of values for a multiple field.</summary>
        public virtual int ValueCount {
            get { return (Values == null ? (Value == null ? 0 : 1) : Values.Length); }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the value set for the field expression.</summary>
        public IValueSet ValueSet { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the field value is invalid.</summary>
        public virtual bool Invalid { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether a value respects its type format and transforms it to a value usable in SQL statements.</summary>
        /*!
        /// <param name="index">the index in the value list</param>
        /// <returns>true if the value is valid</returns>
        */
        public string ToSqlString(int index) {
            string newValue = null;
            bool valid = true;
            if (index > Values.Length) return null;
            
            switch (Type) {
                case "bool" :
                    newValue = Values[index].ToLower();
                    valid = (newValue == "true" || newValue == "false" || newValue == "on" || newValue == "yes" || newValue == "no");
                    if (valid) newValue = (newValue == "true" || newValue == "yes" || newValue == "on" ? "true" : "false");
                    break;
                case "int" :
                    int ip;
                    valid = Int32.TryParse(Values[index], out ip);
                    if (valid) newValue = ip.ToString();
                    break;
                case "float" :
                    double dp;
                    valid = Double.TryParse(Values[index], out dp);
                    if (valid) newValue = dp.ToString();
                    break;
                case "date" :
                case "datetime" :
                case "startdate" :
                case "enddate" :
                    DateTime dtp;
                    valid = DateTime.TryParse(Values[index], out dtp);
                    if (valid) newValue = "'" + dtp.ToString(Type == "date" ? @"yyyy\-MM\-dd" : @"yyyy\-MM\-dd\THH\:mm\:ss") + "'";
                    break;
                default :
                    newValue = (Values[index] == null ? "NULL" : "'" + Values[index].Replace("'", "''").Replace(@"\", @"\\") + "'");
                    break;
            }
            return newValue;
        }

    }



    public class EmptyField : FieldExpression {

        public override string Value {
            get { return null; }
            set {}
        }

        public override string SqlValue {
            get { return null; }
            set {}
        }

        public EmptyField() : base() {}

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a single-value expression.</summary>
    /*!
        Single-value expressions are attributes of an entity. The value of a single-value expression is calculated based on the values of one or more database fields.
        
        Example: the full name of a user is a concatenation of his first and last name.
    */
    public class SingleValueExpression : FieldExpression {

        //---------------------------------------------------------------------------------------------------------------------

        public SingleValueExpression() {}

        //---------------------------------------------------------------------------------------------------------------------

        public SingleValueExpression(string name, string expression, string caption, FieldFlags flags) {
            this.Name = name;
            this.Field = null;
            this.Expression = expression;
            this.Caption = caption;
            this.flags = flags;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public SingleValueExpression(string name, string expression, string type, string caption, string searchExtension, FieldFlags flags) {
            this.Name = name;
            this.Field = null;
            this.Expression = expression;
            this.Type = type;
            this.Caption = caption;
            this.SearchExtension = searchExtension;
            this.flags = flags;
        }
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a single-value field.</summary>
    /*!
        Single-value fields are attributes of an entity.
        A single-value field is a special case of a single value expression and contains its value in a single database fields.
        
        Example: the address of a computing resource is a fixed-value field.
    */
    public class SingleValueField : SingleValueExpression {

        //---------------------------------------------------------------------------------------------------------------------

        public SingleValueField() {}

        //---------------------------------------------------------------------------------------------------------------------

        public SingleValueField(string name, string type, string caption, FieldFlags flags) {
            this.Name = name;
            this.Type = type;
            this.Field = name;
            this.Expression = "t." + Field;
            this.Caption = caption;
            this.flags = flags;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public SingleValueField(string name, string field, string type, string caption, string searchExtension, FieldFlags flags) {
            this.Name = name;
            this.Type = type;
            this.Field = field;
            this.Expression = "t." + Field;
            this.Caption = caption;
            this.SearchExtension = searchExtension;
            this.flags = flags;
        }
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a fixed-value field.</summary>
    /*!
        Fixed-value fields are fields that have a value that cannot be modified.  
        
        Example: the user ID of the private publish server list of a user is a fixed-value field.
        The user may only see his own users (filtering criterion), and when he creates a new publish server, its user ID is set to his user ID.
    */
    public class FixedValueField : FieldExpression {
        
        public FixedValueField(string name, string field, string value) {
            this.Name = name;
            this.Field = field;
            this.Expression = "t." + Field;
            this.Type = "string";
            this.Value = value;
            this.flags = 0;
        }

        public FixedValueField(string name, string field, string type, string value, FieldFlags flags) {
            this.Name = name;
            this.Field = field;
            this.Expression = "t." + Field;
            this.Type = type;
            this.Value = value;
            this.flags = flags;
        }
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a single-reference field.</summary>
    /*!
        Single-reference fields contain a reference to a value that is defined independently within another entity.  
        
        Example: the computing resource reference of a task is a single reference. A task must refer to a computing resource, which is independent of the task.

        Single-reference fields can also refer to an entity in order to take into account a complex data structure rather than just one value. 
        
        Example: the service reference of a task is a single-reference with complex data structure. To be able to search tasks by service name and service caption, the service must be defined with as an entity within the data structure of the task. 
        
    */
    public class SingleReferenceField : FieldExpression, IReferenceField {
        public string ReferenceTable { get; set; }
        public string ReferenceTableAlias { get; set; }
        public string ReferenceValueExpr { get; set; }
        public string ReferenceIdField { get; set; }
        public string ReferenceLinkField { get; set; }
        public EntityData ReferenceEntity { get; set; }
        public string NullCaption { get; set; }
        public string SortExpression { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public SingleReferenceField(string name, string referenceTable, string referenceValueExpr, string referenceLinkField, string caption, string searchExtension, FieldFlags flags) {
            this.Name = name;
            this.Type = "select";
            this.Field = referenceLinkField;
            this.Expression = referenceValueExpr;
            this.ReferenceTable = referenceTable;
            this.ReferenceValueExpr = referenceValueExpr;
            this.ReferenceIdField = "id";
            this.ReferenceLinkField = Field;
            this.Caption = caption;
            this.SearchExtension = searchExtension;
            this.flags = flags;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public SingleReferenceField(string name, string referenceTable, string referenceValueExpr, string referenceLinkField, string caption, FieldFlags flags) : this(name, referenceTable, referenceValueExpr, referenceLinkField, caption, null, flags) {}

        //---------------------------------------------------------------------------------------------------------------------

        public SingleReferenceField(string name, EntityData referenceEntity, string referenceLinkField, string caption, string searchExtension, FieldFlags flags) : this(name, referenceEntity.Table, null, referenceLinkField, caption, searchExtension, flags) {
            this.ReferenceEntity = referenceEntity;
            this.ReferenceIdField = referenceEntity.IdField;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public SingleReferenceField(string name, EntityData referenceEntity, string caption, FieldFlags flags) : this(name, referenceEntity.Table, null, "id_" + referenceEntity.Table, caption, null, flags) {
            this.ReferenceEntity = referenceEntity;
            this.ReferenceIdField = referenceEntity.IdField;
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a multiple-value field.</summary>
    /*!
        Multiple-value fields contain <i>n</i> dependent values (<i>n</i> &gt;= 0 if the field is optional, <i>n</i> &gt;= 1 otherwise).
        
        Example (theoretical): alternative names of an entity.
        An entity could have alternative names, other than its main name. 
    */
    public class MultipleValueField : FieldExpression, IMultipleField {
        public string MultipleTable { get; set; }
        public string MultipleTableAlias { get; set; }
        public string MultipleLinkField { get; set; }
        public string MultipleIdField { get; set; }
        public string Condition { get; set; }
        public char ValueSeparator { get; set; }
        public int[] DeleteIds { get; set; }
        public int[] UpdateIds { get; set; }
        public List<string[]> Rows { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the number of values for a multiple field.</summary>
        public override int ValueCount {
            get { return (Values == null ? 0 : Values.Length); }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public MultipleValueField(string name, string multipleTable, string multipleTableValueExpr, string multipleLinkField, string caption, FieldFlags flags) {
            this.Name = name;
            this.Type = "values";
            this.Field = null;
            this.MultipleTable = multipleTable;
            this.Expression = multipleTableValueExpr;
            this.MultipleLinkField = multipleLinkField;
            this.MultipleIdField = "id";
            this.Caption = caption;
            this.flags = flags;
            this.ValueSeparator = ',';
        }
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    
    /// <summary>Represents a multiple-reference field.</summary>
    /*!
        Multiple-reference fields contain <i>n</i> values (<i>n</i> &gt;= 0 if the field is optional, <i>n</i> &gt;= 1 otherwise).
        
        Example: the user-group assignment is a multiple-reference. A given user can be member of different groups, and a given user group can contain different users. Users and groups are defined independently and refer to each other where the references can be multiple in either case.
        
        Multiple-reference fields can also be refer to an entity in order to take into account a complex data structure rather than just one value. 
        
        Example: the list of services that are compatible with a given series containing the caption and the link for each service is a multiple-reference with data structure. To be able to know both the service ID and the service URL, the service must be defined with as an entity within the data structure of the series.
    */
    public class MultipleReferenceField : FieldExpression, IReferenceField, IMultipleField {
        
        private bool singleValue;

        public string ReferenceTable { get; set; }
        public string ReferenceTableAlias { get; set; }
        public string ReferenceValueExpr { get; set; }
        public string ReferenceIdField { get; set; }
        public string ReferenceLinkField { get; set; }
        public EntityData ReferenceEntity { get; set; }
        public string NullCaption { get; set; }
        public string SortExpression { get; set; }
        
        public string MultipleTable { get; set; }
        public string MultipleTableAlias { get; set; }
        public string MultipleLinkField { get; set; }
        public string MultipleIdField { get; set; }
        public string Condition { get; set; }
        public char ValueSeparator { get; set; }
        public int[] DeleteIds { get; set; }
        public int[] UpdateIds { get; set; }
        public List<string[]> Rows { get; set; }
        
        public EntityData MultipleEntity { get; set; }

        /// <summary>Indicates or determines whether the field can accept only one value.</summary>
        /*! Setting this property to <i>true</i> is not the same as defining a SingleReferenceField. The reference is stored in the intermediate table, not in the entity's table */
        public bool SingleValue { 
            get { return singleValue; }
            set {
                singleValue = value;
                if (singleValue && Type == "multiple") Type = "select";
                else if (!singleValue && Type == "select") Type = "multiple";
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the number of values for a multiple field.</summary>
        public override int ValueCount {
            get { return (Values == null ? 0 : Values.Length); }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>...</summary>
/*        public override bool Invalid { !!! 
            get {
                if (invalid) return true;
                foreach (FieldExpression field in MultipleEntity.Fields) if (field.Invalid) return true; 
                return false;
            }
            set {
                invalid = value;
            }
        }*/

        //---------------------------------------------------------------------------------------------------------------------

        public MultipleReferenceField(string name, string referenceTable, string referenceValueExpr, string multipleTable, string referenceLinkField, string multipleLinkField, string caption, string searchExtension, FieldFlags flags) {
            this.Name = name;
            this.Type = "multiple";
            this.Expression = referenceValueExpr;
            this.ReferenceTable = referenceTable;
            this.ReferenceValueExpr = referenceValueExpr;
            this.ReferenceIdField = "id";
            this.MultipleTable = multipleTable;
            this.ReferenceLinkField = referenceLinkField;
            this.MultipleLinkField = multipleLinkField;
            this.MultipleIdField = "id";
            this.Caption = caption;
            this.SearchExtension = searchExtension;
            this.flags = flags;
            this.ValueSeparator = ',';
        }

        //---------------------------------------------------------------------------------------------------------------------

        public MultipleReferenceField(string name, string referenceTable, string referenceTableValueExpr, string multipleTable, string caption, FieldFlags flags) : this (name, referenceTable, referenceTableValueExpr, multipleTable, null, null, caption, null, flags) {}

        //---------------------------------------------------------------------------------------------------------------------

        public MultipleReferenceField(string name, EntityData referenceEntity, string multipleTable, string referenceLinkField, string multipleLinkField, string caption, string searchExtension, FieldFlags flags) : this(name, referenceEntity.Table, null, multipleTable, referenceLinkField, multipleLinkField, caption, searchExtension, flags) {
            this.ReferenceEntity = referenceEntity;
            this.ReferenceIdField = referenceEntity.IdField;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public MultipleReferenceField(string name, EntityData referenceEntity, string multipleTable, string caption, FieldFlags flags) : this(name, referenceEntity.Table, null, multipleTable, null, null, caption, null, flags) {
            this.ReferenceEntity = referenceEntity;
            this.ReferenceIdField = referenceEntity.IdField;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public MultipleReferenceField(string name, string referenceTable, string referenceTableValueExpr, EntityData multipleEntity, string referenceLinkField, string multipleLinkField, string caption, string searchExtension, FieldFlags flags) : this(name, referenceTable, referenceTableValueExpr, multipleEntity.Table, referenceLinkField, multipleLinkField, caption, searchExtension, flags) {
            this.MultipleEntity = multipleEntity;
            this.MultipleIdField = multipleEntity.IdField;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public MultipleReferenceField(string name, string referenceTable, string referenceTableValueExpr, EntityData multipleEntity, string caption, FieldFlags flags) : this(name, referenceTable, referenceTableValueExpr, multipleEntity.Table, null, null, caption, null, flags) {
            this.MultipleEntity = multipleEntity;
            this.MultipleIdField = multipleEntity.IdField;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public MultipleReferenceField(string name, EntityData referenceEntity, EntityData multipleEntity, string referenceLinkField, string multipleLinkField, string caption, string searchExtension, FieldFlags flags) : this(name, referenceEntity.Table, null, multipleEntity.Table, referenceLinkField, multipleLinkField, caption, searchExtension, flags) {
            this.ReferenceEntity = referenceEntity;
            this.ReferenceIdField = referenceEntity.IdField;
            this.MultipleEntity = multipleEntity;
            this.MultipleIdField = multipleEntity.IdField;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public MultipleReferenceField(string name, EntityData referenceEntity, EntityData multipleEntity, string caption, FieldFlags flags) : this(name, referenceEntity.Table, null, multipleEntity.Table, null, null, caption, null, flags) {
            this.ReferenceEntity = referenceEntity;
            this.ReferenceIdField = referenceEntity.IdField;
            this.MultipleEntity = multipleEntity;
            this.MultipleIdField = multipleEntity.IdField;
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a multiple-entity field.</summary>
    /*!
        Multiple-entity fields contain <i>n</i> dependent entities (<i>n</i> &gt;= 0 if the field is optional, <i>n</i> &gt;= 1 otherwise).
        Multiple-entity fields are similar to multiple-value fields with the difference that their content is not just a value, but has a complex data structure. 
        
        Example: the working and result directories of a computing resource are multiple-entities.
        Such a directory must be an entity on its own because it has more than one field (<i>available</i>, <i>path</i>) and belongs to exactly one computing resource, and a computing resource can have more than one working and result directories. 
    */
    public class MultipleEntityField : FieldExpression, IEntityField, IMultipleField {
        public EntityData ForeignEntity { get; set; }
        public string MultipleTable { get; set; }
        public string MultipleTableAlias { get; set; }
        public string MultipleLinkField { get; set; }
        public string MultipleIdField { get; set; }
        public string Condition { get; set; }
        public char ValueSeparator { get; set; }
        public int[] DeleteIds { get; set; }
        public int[] UpdateIds { get; set; }
        public List<string[]> Rows { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public override int ValueCount {
            get { 
                int result = 0;
                bool first = true;
                foreach (FieldExpression field in ForeignEntity.Fields) {
                    if (field is FixedValueField) continue;
                    int count = field.ValueCount;
                    if (first || count < result) result = count;
                    first = false;
                }
                return result;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public MultipleEntityField(string name, EntityData foreignEntity, string multipleLinkField, string caption, FieldFlags flags) {
            this.Name = name;
            this.Type = "entities";
            this.Field = null;
            this.ForeignEntity = foreignEntity;
            this.MultipleTable = foreignEntity.Table;
            this.MultipleLinkField = multipleLinkField;
            this.MultipleIdField = foreignEntity.IdField;
            this.Caption = caption;
            this.flags = flags;
            this.ValueSeparator = ',';
        }
    }
    

    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents the field for sorting the items in an item list.</summary>
    /*!
        There can be only one sort field per entity. 
    */
    public class SortField : FieldExpression {

        //---------------------------------------------------------------------------------------------------------------------

        public SortField() {}

        //---------------------------------------------------------------------------------------------------------------------

        public SortField(string name, string searchExtension) {
            this.Name = name;
            this.SearchExtension = searchExtension;
        }
    }
    

    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Flags for fields in an entity data structure.</summary>
    [Flags]
    public enum FieldFlags {
        
        /// <summary>Field is shown in the <b>single item</b> view</summary>
        Item = 0x0001,

        /// <summary>Field is shown in the <b>item list</b> view</summary>
        List = 0x0002,
        
        /// <summary>Field is shown in both <b>single item</b> and <b>item list</b> view</summary>
        Both = 0x0003,

        /// <summary>Field must have a unique value</summary>
        Unique = 0x0004,

        /// <summary>Field is a unique numeric identifier</summary>
        Id = 0x000c,

        /// <summary>Field is shown in the <b>single item</b> view</summary>
        //Define = 0x0004,

        /// <summary>Field does not require a value</summary>
        Optional = 0x0010, 
        AllButOptional = 0x1ffef,
        
        /// <summary>Field is hidden</summary>
        Hidden = 0x0020, 

        /// <summary>Field is ignored when writing !!! same as hidden ???</summary>
        Ignore = 0x0020, 

        /// <summary>Field value can not be changed</summary>
        ReadOnly = 0x0040,

        /// <summary>Field value is written to an XML attribute</summary>
        Attribute = 0x0080,
        //GroupBy = 0x0040, 
        
        /// <summary>Field is a sorting criterion (no sorting direction specified)</summary>
        Sort = 0x0100, 

        /// <summary>Field is a sorting criterion (ascending sorting)</summary>
        SortAsc = 0x0100, 

        /// <summary>Field is a sorting criterion (descending sorting)</summary>
        SortDesc = 0x0300, 

        /// <summary>Field is a sorting criterion (no sorting direction specified)</summary>
        Group = 0x0400,

        /// <summary>Field is an aggregation key field and a sorting criterion (ascending sorting)</summary>
        GroupAsc = 0x0500, 

        /// <summary>Field is an aggregation key field and a sorting criterion (descending sorting)</summary>
        GroupDesc = 0x0700, 

        /// <summary>Field has a lookup value set</summary>
        Lookup = 0x800,

        /// <summary>Field does not show its value list if not explicitly requested</summary>
        Reduced = 0x1000, 

        /// <summary>Field is searched using the value of the OpenSearch <i>searchTerms</i> parameter if provided</summary>
        Searchable = 0x2000,

        /// <summary>Field is searched using the value of the OpenSearch <i>searchTerms</i> parameter if provided</summary>
        TextSearch = 0x4000,
        /*Sortable = 0x0800,       
        Moveable = 0x1000,*/

        /// <summary>Field has specific characteristics regarding the filtering or the value visualization</summary>
        Custom = 0x8000,
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents information for a sorting criterion in a main query that uses paging.</summary>
    public struct SortInfo {
        
        /// <summary>Gets or sets the SQL field expression for the sorting criterion.</summary>
        public FieldExpression Field {get; set; }

        /// <summary>Gets or sets the flag whether the sorting is descending.</summary>
        public bool Descending {get; set; }

        /// <summary>Gets or sets the first value the sorting criterion has on the page.</summary>
        public string PageFirstValue {get; set; }

        /// <summary>Gets or sets the last value the sorting criterion has on the page.</summary>
        public string PageLastValue {get; set; }
    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    
    
    
    public class TableInfo {
        public int Index { get; protected set; }
        public string Name { get; protected set; }
        public string IdField { get; protected set; }
        public string Alias { get; protected set; }
        public TableInfo(int index, string name, string idField, string alias) {
            this.Index = index;
            this.Name = name;
            this.IdField = idField;
            this.Alias = alias;
        }
    }

}
