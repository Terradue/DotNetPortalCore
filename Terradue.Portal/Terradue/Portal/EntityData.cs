using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Util;
using Terradue.Metadata.OpenSearch;
using Terradue.Portal;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Gpod.Portal {

    /// <summary>Represents the relational data structure of an Entity.</summary>
    /// <remarks>
    ///     <p>
    ///         A REST interface allows the manipulation of the stored data via the appropriate HTTP methods, according to the REST design principles. The manipulations include
    ///         <list type="bullet">
    ///             <item><b>create</b>: creating new items (HTTP <c>POST</c>),</item>
    ///             <item><b>modify</b>: modifying existing items (HTTP <c>PUT</c>), and</item>
    ///             <item><b>delete</b>: deleting items (HTTP <c>DELETE</c>).</item>
    ///         </list>
    ///         Alternatively, these operations can be requested using a query string parameter for the operation type and if necessary other <c>GET</c> or <c>POST</c> parameters for the item data. The advantage of the REST interface is, however, the possibility of manipulating also complex entities.
    ///     </p>
    ///     <p>Besides the data structure, Entity subclasses may add other class members for specific semantics and programming logic to an entity. For instance, the Task class (representing a processing task) has properties for the creation time and the status and methods for the submission and abortion of a task.</p>
    /// </remarks>
    public abstract class EntityData : IValueSet {



        public string Identifier { get; set; }




        private int extensionCount = 0;
        protected IfyWebContext context;
        private EntityType entityType;
        protected const string openSearchNamespacePrefix = "os";
        protected const string searchTermsParamName = "q";

        private bool fromRequest = false; // values have been received from request
        private string listUrl;

        private IInputSource inputSource;

        private static string searchTermsValue;
        private RequestParameterCollection compositeFields; // for entity fields that are represented by multiple lines in a table (virtual)
        private SortInfo[] sorts;
        private string sortSelect;
        private int sortSelectCount;

        protected OpenSearchDescription openSearchDescription;
        protected MonoXmlWriter xmlWriter;

        private bool optionList = false;

        public bool CanCreate { get; set; }
        public bool CanModify { get; set; }
        public bool CanDelete { get; set; }


        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the entity.</summary>
        public virtual string EntityName {
            get { return entityType.SingularCaption; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public int Id { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public int[] Ids { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets a caption that can be displayed in the GUI as a heading above the form representing the item.</summary>
        public string OperationCaption { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the table containing the data of the entity.</summary>
        public string Table { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the complete join (in SQL syntax) containing the data of the entity.</summary>
        public string Join { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public TableInfo[] ExtensionTables;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the fields of the entity.</summary>
        public FieldExpressionCollection Fields { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the ID field of the table containing the data of the entity (default: <i>id</i>).</summary>
        public string IdField { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public static FieldExpression EmptyField = new EmptyField();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates an EntityData instance.</summary>
        /*!
        /// <param name="entityName">the name of the entity</param>
        /// <param name="join">the table or join (in SQL syntax) containing the data of the entity</param>
        /// <param name="fields">the fields of the entity</param>
        */
        public EntityData(IfyWebContext context, Type type, bool optionList) {

            this.context = context;
            this.OptionList = optionList;
            this.entityType = GetEntityStructure(type);
            Join = String.Format("{0} AS t", entityType.TopTable.Name);

            this.Fields = new FieldExpressionCollection();
            this.IdField = "id";

            /*foreach (FieldExpression field in fields) {
                if (field is MultipleReferenceField) {
                    MultipleReferenceField field1 = field as MultipleReferenceField;
                    if (field1.ReferenceLinkField == null) field1.ReferenceLinkField = "id_" + field1.ReferenceTable;
                    if (field1.MultipleLinkField == null) field1.MultipleLinkField = "id_" + this.Table;
                }
            }*/

            ShowItemIds = true;
            ShowItemLinks = true;

            this.inputSource = context;
            this.ViewingState = ViewingState.Unknown;

            if (context != null) {
                CanViewList = (context.UserLevel >= context.Privileges.MinUserLevelList);
                CanViewItem = (context.UserLevel >= context.Privileges.MinUserLevelView);
                CanCreate = (context.UserLevel >= context.Privileges.MinUserLevelCreate); // TODO: re-enable
                CanModify = (context.UserLevel >= context.Privileges.MinUserLevelModify);
                CanDelete = (context.UserLevel >= context.Privileges.MinUserLevelDelete);
            }
            this.Paging = true;
            this.FieldNamePrefix = String.Empty;
            if (context != null) {
                this.ListUrl = context.ScriptName;
                this.ItemBaseUrl = ListUrl;
                this.ItemUrl = String.Format(context.UsesUrlRewriting ? "{0}" : "{0}{1}", ItemBaseUrl, Id == 0 ? String.Empty : "?id=" + Id);
            }

            CanViewList = false;
            CanViewItem = false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected EntityType GetEntityStructure(Type type) {
            EntityType result = EntityType.GetEntityType(type);
            if (result == null ) {
                result = EntityType.AddEntityType(this.GetType());
                if (result == null || result.Tables.Count == 0) throw new InvalidOperationException(String.Format("Entity information not available: {0}", type.FullName));
            }

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void AppendFields(string extensionTableName, string extensionIdField, List<FieldExpression> fields) {
            if (extensionTableName != null) {
                TableInfo extensionTable = new TableInfo(extensionCount, extensionTableName, extensionIdField, "e" + (++extensionCount));
                Array.Resize(ref ExtensionTables, extensionCount);
                ExtensionTables[extensionCount - 1] = extensionTable;
                foreach (FieldExpression f in fields) f.ExtensionTable = extensionTable;
                Join += String.Format(" INNER JOIN {0} AS {1} ON t.id={1}.{2}", extensionTable.Name, extensionTable.Alias, extensionIdField);
            }
            foreach (FieldExpression item in fields) Fields.Add(item);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Resets the aliases assigned to the tables that are foreign entities.</summary>
        public void ResetTableAliases() {
            foreach (FieldExpression field in Fields) {
                if (field is IReferenceField) {
                    (field as IReferenceField).ReferenceTableAlias = null;
                    if (field is IEntityField) (field as IEntityField).ForeignEntity.ResetTableAliases();
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the field with the specified name and flags.</summary>
        /*!
        /// <param name="name">the field name</param>
        /// <param name="flags">the flags the field must match</param>
        /// <returns>the matching field, or <i>null</i> if no field matches</returns>
        */
        public FieldExpression GetField(string name, FieldFlags flags) {
            FieldExpression result = Fields[name];
            if ((result.Flags & flags) != 0) return result;
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /*protected void AssignValueSets(bool define) {
            foreach (FieldExpression field in Fields) {
                if (field is IReferenceField) {
                    IReferenceField field1 = field as IReferenceField;

                    if (field1.ReferenceEntity != null) {
                        field.ValueSet = field1.ReferenceEntity;
                        continue;
                    }
                    
                    field.ValueSet = new ReferenceValueSet(
                            context,
                            field1.ReferenceTable,
                            field1.ReferenceTableIdField, 
                            (field1.ReferenceTableValueExpr == null ? field1.ReferenceTableIdField : field1.ReferenceTableValueExpr.Replace("@.", String.Empty)),
                            define
                    );
                    if ((field.Flags & FieldFlags.Optional) != 0) {
                        SingleReferenceField field2 = field as SingleReferenceField;
                        if (field2 == null) {
                            MultipleReferenceField field3 = field as MultipleReferenceField;
                            if (field3 != null && field3.SingleValue) field.ValueSet.NullCaption = field3.NullCaption;
                        } else {
                            field.ValueSet.NullCaption = field2.NullCaption;
                        }
                    }
                    
                } else if (!(field is IEntityField) && (field.Flags & FieldFlags.Lookup) != 0) {
                    field.ValueSet = new LookupValueSet(
                            context,
                            field.Type,
                            true,
                            define
                    );
                }
            }
        }*/

        //---------------------------------------------------------------------------------------------------------------------

        public virtual bool HasExtensionTypes {
            get {
                EntityType entityType = EntityType.GetEntityType(this.GetType());
                if (entityType == null) return false;
                return entityType.TopTable.HasExtensions;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected string ExtraParameter { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool NoRedirect { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets a list of key values to be excluded from the item list.</summary>
        public int[] ExcludeIds { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether user-defined filters can be used.</summary>
        /// <remarks>Filters can only be used if the derived class overrides the EntityCode property.</remarks>
        public bool PersistentFilters { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the SQL conditional expression for the default filtering of items.</summary>
        public string FilterCondition { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the item list is split into different pages.</summary>
        public bool Paging { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the maximum number of items that are displayed in a page of the item list.</summary>
        public int ItemsPerPage { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the page of the item list that is displayed.</summary>
        public int Page { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the index of the first item that is displayed in the item list.</summary>
        public int StartIndex { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the last sorting criterium (key field) is in descending order.</summary>
        protected bool KeyDescending { get; set; } 

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the item list sorting criteria that apply for the inner records that belong to the same item.</summary>
        /// <remarks>This property can be used for an additional aggregation because only the values of the first record of an item are considered.</remarks>
        protected string InnerSorting { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the item list sorting criteria that apply for the inner records that belong to the same item.</summary>
        protected bool UseUserTimeZone { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the item list is formatted as an option list (i.e. <i>&lt;element&gt;</i> elements with <i>value</i> attributes).</summary>
        public bool OptionList {
            get { return optionList; }
            set { 
                optionList = value;
                Paging = (!optionList);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string OptionListElementName { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the IDs of the items are shown in <i>&lt;item&gt;</i> elements.</summary>
        public bool ShowItemIds { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the links to the single item representations are shown in <i>&lt;item&gt;</i> elements.</summary>
        public bool ShowItemLinks { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the item is a "virtual" composite, i.e. composed of a variable number of database records.</summary>
        /*!
            The fields of composite items correspond to rows of a database table that match a specific criteria defined by the item
            (unlike the fields of single items, which correspond to the columns of a database table).
            
            The fields of a composite item cannot be pre-configured in a class and the fields usually differ between composite items.

            Composite items can be modified like "real" items, but they cannot be
            <ul>
                <li>listed in an item list: virtual items have usually different fields, unlike an item list which is a set of identically structured items.</li>
                <li>defined, created or deleted: a virtual item depends on something else (e.g. a "real" item) by which its fields are defined, created and deleted.</li> 
            </ul>
            An example of a composite item are the parameters of a processing job. They are stored different records of the same database table.
            Each of these records contains, among other data, a value for the name, for the actual parameter value and a reference to the job they belong to.
            The job parameters are created and deleted together with the job, which is the "real" item they depend on.
        */
        //---------------------------------------------------------------------------------------------------------------------

        public bool CompositeItem {
            get { return IsComposite; }
            set { IsComposite = value; } 
        }

        public bool IsComposite { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the prefix for the names of fields of composite items.</summary>
        public string FieldNamePrefix { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityOperation RequestedOperation { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether items can be viewed as item list.</summary>
        public bool CanViewList { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether an existing item can be viewed as single item.</summary>
        public bool CanViewItem { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the generic item interface accepts the initialization of field values.</summary>
        /// <remarks>
        ///     <p>When this property is <c>true</c>, the request for the generic item interface accepts item field values that are different from the stored values and initializes the interface accordingly. This is usually the case for items before their creation (since they are not stored yet).</p>
        ///     <p>If set to <c>false</c> for an item, the generic item interface ignores different values and is initialized with the current representation of the item (if it exists) or with the empty/default values (if the item does not exist yet). 
        /// </remarks>
        public bool AcceptsInitialization { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public List<EntityOperation> Operations { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the list URL shown in the <i>&lt;itemList&gt;</i> element if it is different from the requested script URL.</summary>
        public string ListUrl { 
            get { return listUrl; } 
            set {
                listUrl = value;
                if (openSearchDescription == null) return;
                openSearchDescription.UrlTemplate = (value == null ? context.ScriptUrl + "?" : value + (value.Contains("?") ? "&" : "?")) + "{format}&{searchfields}";
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string ItemBaseUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string ItemUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the item list output is transformed into an RSS feed.</summary>
        /*!
            The transformation is done by an XSL file. Derived classes can use this property to define a different entity structure in case of an RSS feed.
        */
        public bool Feed { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the SQL aggregation fields expression to be used in the item list query.</summary>
        /*!
            The value of this property must not contain the <b>GROUP BY</b> keyword.
        */
        public string CustomAggregation { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the SQL sorting fields expression to be used in the item list query.</summary>
        /*!
            The value of this property must not contain the <b>ORDER BY</b> keyword.
        */
        public string CustomSorting { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        // !!! PROPERTY VALUE MUST NOT BE APPENDED

        /// <summary>Gets or sets key/value pairs of additional URL parameters.</summary>
        /*!
            The value must be in encoded URL syntax, separating key/value pairs by <i>&amp;</i> and keys and values by <i>=</i>
        */
        public string FieldsTemplateStr { get; set; }        

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string GetExplanation() {
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool AcceptCaptionsAsValues { get; set; }

        public ViewingState ViewingState { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets an array of the values contained in the entity option list.</summary>
        public virtual string[] GetValues() {
            OptionList = true;

            int aliasIndex = 0;
            string join = Join;

            string condition = FilterCondition;
            ExpandQuery(ref join, ref condition, ref aliasIndex);

            string groupBy = CustomAggregation;
            string orderBy = CustomSorting;
            string countFields = null;
            ExpandSortingClause(ref countFields, ref groupBy, ref orderBy);

            string select = "t." + IdField;
            int columnIndex = 1;
            ExpandSelectClause(ref columnIndex, ref select, "t", FieldFlags.List);

            // Get index of column that has the flag for the default value(s)
            int nameIndex = -1, captionIndex = -1;
            foreach (FieldExpression field in Fields) {
                if ((field.Flags & FieldFlags.List) == 0) continue;
                if (field.Type == "name") nameIndex = field.ColumnIndex;
                switch (field.Name) {
                    case "value" :
                    case "name" :
                    case "identifier" :
                        nameIndex = field.ColumnIndex;
                        //context.AddInfo("captionIndex = " + field.ColumnIndex);
                        break;
                    case "caption" :
                        if (AcceptCaptionsAsValues) captionIndex = field.ColumnIndex;
                        //context.AddInfo("captionIndex = " + field.ColumnIndex);
                        break;
                }
            }

            if (nameIndex == -1 && captionIndex == -1) return new string[0];

            string sql = String.Format("SELECT {0} FROM {1}{2}{3}{4};",
                                       select,
                                       join,
                                       (condition == null ? "" : " WHERE " + condition),
                                       (groupBy == null ? "" : " GROUP BY " + groupBy),
                                       (orderBy == null ? "" : " ORDER BY " + orderBy)
                                       );

            List<string> nameValues = null, captionValues = null; 
            if (nameIndex != -1) nameValues = new List<string>();
            if (captionIndex != -1) captionValues = new List<string>(); 

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                if (nameIndex != -1) nameValues.Add(reader.GetValue(nameIndex).ToString());
                if (captionIndex != -1) captionValues.Add(reader.GetValue(captionIndex).ToString());
            }
            context.CloseQueryResult(reader, dbConnection);
            if (nameIndex == -1) {
                return captionValues.ToArray();
            } else if (captionIndex == -1) {
                return nameValues.ToArray();
            } else {
                foreach (string s in nameValues) captionValues.Add(s); 
                return captionValues.ToArray();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ProcessAdministrationRequest(int id) {
            this.Id = id;
            ProcessAdministrationRequest();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ProcessAdministrationRequest(string identifier) {
            //this.Id = id;
            ProcessAdministrationRequest();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void ProcessAdministrationRequest() {
            context.AdminMode = true;
            GetAllowedAdministratorOperations();
            ProcessGenericRequest();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Evaluates the parameters sent by the client and performs the requested operation.</summary>
        /*! The standard operations are
            <ul>
                <li><b>define</b>: return the entity's field information to the client, allowing the definition of a new item</li>
                <li><b>create</b>: create a new item in the database and return its single item information to the client</li>
                <li><b>modify</b>: modify an existing item in the database and return the updated single item information to the client</li>
                <li><b>delete</b>: delete one or more items from the database and return the updated item list to the client</li>
                <li><b>describe</b>: return the entity's OpenSearch description to the client (if defined)</li>
            </ul>
           If no operation is requested, the returned output depends on whether the URL parameter for the item key value is provided
            <ul>
                <li>if no key field value is provided: send the item list data to the client considering the the paging and, if an OpenSearch description is defined, the filter parameters</li>
                <li>if a key field value is provided: send the requested single item data to the client</li>
            </ul>    
           Other entity-specific operations can be performed by overriding the ProcessSpecificRequest method in a derived class
        */
        public void ProcessGenericRequest() {
            ProcessGenericRequest(Id);
        }

        public void ProcessGenericRequest(int id) {
            this.Id = id;
            if (Id == 0 && !context.UsesUrlRewriting) GetIdFromRequest();
            if (ViewingState == ViewingState.Unknown) GetViewingState();

            AddBasicOperations();

            //bool createError = false;
            bool noOutput = true;
            string operationName = context.RequestedOperation;

            GetOperation();

            if (RequestedOperation == null) RequestedOperation = new EntityOperation(context, OperationType.ViewItem, null, null, false, null, null);
            ListUrl = context.ScriptName;
            ItemBaseUrl = ListUrl;

            if (ViewingState == ViewingState.DefiningItem) fromRequest = true;
            //context.AddInfo("ot: " + RequestedOperation.Type + ", vs: " + ViewingState);

            switch (RequestedOperation.Type) {
                case OperationType.Create :
                    try {
                        //context.KeepOpen = true;
                        context.StartTransaction();
                        fromRequest = true;
                        Id = CreateItem();
                        ItemUrl = String.Format(context.UsesUrlRewriting ? "{0}/{1}" : "{0}?id={1}", ItemBaseUrl, Id);
                        context.Commit();
                        if (!NoRedirect && Id != 0) context.Redirect(String.Format("{0}{1}_state=created", ItemUrl, ItemUrl.Contains("?") ? "&" : "?", Id), true, true);
                    } catch (Exception e) {
                        context.Rollback();
                        Id = 0;
                        //createError = true;
                    }
                    //xmlWriter.WriteElementString("TEST", "1");
                    break;
                case OperationType.Modify :
                    try {
                        //context.KeepOpen = true;
                        context.StartTransaction();
                        fromRequest = true;
                        ModifyItem();
                        context.Commit();
                    } catch (Exception e) {
                        context.Rollback();
                    }
                    break;
                case OperationType.Delete :
                    if (Id == 0) {
                        GetIdsFromRequest("id");
                        if (Ids.Length != 0) DeleteItems(Ids);
                    } else {
                        DeleteItem(Id);
                    }
                    Id = 0;
                    break;
               default :
                    if (ProcessSpecificRequest(operationName)) return;
                    noOutput = false;
                    break;
            }
            //context.KeepOpen = false;
            noOutput = false;

            if (noOutput && !context.IsUserAuthenticated) {
                context.StartXmlResponse();
                context.EndXmlResponse();
                return;
            }

            //if (CanViewList && ViewingState != ViewingState.DefiningItem && !createError && Id == 0 && !CompositeItem) {
            if (CanViewList && ViewingState == ViewingState.ShowingList && !CompositeItem) {
                if (operationName == "describe" || operationName == "description") {
                    WriteOpenSearchDescription(false);
                } else {
                    xmlWriter = context.StartXmlResponse();
                    if (OptionList) xmlWriter.WriteStartElement("list");
                    WriteItemList();
                    if (OptionList) xmlWriter.WriteEndElement(); // </list>
                }
            } else {
                WriteSingleItem();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>When overridden in a derived class, performs entity-specific operations that are different from the standard operations handled in ProcessGenericRequest().</summary>
        /*!
        /// <param name="operationName">the name of the entity-specific operation</param>
        */
        protected virtual bool ProcessSpecificRequest(string operationName) { return false; }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void GetAllowedAdministratorOperations() {
            if (context.UserLevel == UserLevel.Administrator) return;

            CanCreate = false;
            CanModify = false;
            CanDelete = false;
            CanViewItem = false;
            CanViewList = false;

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(entityType.GetAdministratorOperationsQuery(context.UserId, Id), dbConnection);

            while (reader.Read()) {
                switch (reader.GetChar(0)) {
                    case 'v' :
                    case 'V' :
                        CanViewItem = true;
                        CanViewList = true;
                        break;
                    case 'c' :
                        CanCreate = true;
                        break;
                    case 'm' :
                        CanModify = true;
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

            CanViewItem |= CanCreate || CanModify || CanDelete;
            CanViewList |= CanViewItem;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void GetOperation() {
            if (context == null || Operations == null) return;

            string operationIdentifier = context.RequestedOperation;

            if (!String.IsNullOrEmpty(operationIdentifier)) {
                foreach (EntityOperation operation in Operations) {
                    if (operationIdentifier == operation.Identifier) {
                        RequestedOperation = operation;
                        //context.AddInfo("operation[identifier] = " + operation.Identifier);
                        return;
                    }
                }
            }

            string httpMethod = context.HttpMethod;
            if (httpMethod == "GET") return;

            /*            foreach (EntityOperation operation in Operations) {
                if (httpMethod == operation.HttpMethod) {
                    RequestedOperation = operation; 
                    //context.AddInfo("operation[method] = " + operation.Identifier);
                    return;
                }
            }*/

        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns an item list interface containing the given number of items ordered by the given sorting criteria to the client.</summary>
        /*!
        /// <param name="sorting">initial SQL sorting fields expression (without the <i>ORDER BY</i> keyword)</param>
        /// <param name="count">number of items in the list</param>
        /// <returns>the number of returned items</returns>
        */
        public int WriteItemList(string sorting, int count) {
            CustomSorting = sorting;
            ItemsPerPage = count;
            Page = 1;
            return WriteItemList(0);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns an item list interface containing the given number of items matching the given condition ordered by the given sorting criteria to the client.</summary>
        /*!
        /// <param name="condition">initial SQL conditional expression (without the <i>WHERE</i> keyword) for filtering</param>
        /// <param name="sorting">initial SQL sorting fields expression (without the <i>ORDER BY</i> keyword)</param>
        /// <param name="count">number of items in the list</param>
        /// <returns>the number of returned items</returns>
        */
        public int WriteItemList(string condition, string sorting, int count) {
            CustomSorting = sorting;
            ItemsPerPage = count;
            Page = 1;
            if (condition != null) {
                if (FilterCondition == null) FilterCondition = ""; else FilterCondition += " AND ";
                FilterCondition += condition;
            }
            return WriteItemList(0);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the default item list interface to the client.</summary>
        /*!
        /// <returns>the number of returned items</returns>
        */
        public int WriteItemList() {
            return WriteItemList(0);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns an item list interface containing the given number of random items to the client.</summary>
        /*!
        /// <returns>the number of returned items</returns>
        */
        public int WriteRandomItemList(int count) {
            CustomSorting = "RAND()"; //!!!
            ItemsPerPage = count;
            Page = 1;
            return WriteItemList(0);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the item list interface for the specified nesting level to the client.</summary>
        /*!
        /// <param name="level">the level of nested entities (0 for top level)</param>
        /// <returns>the number of returned items</returns>
        */
        protected virtual int WriteItemList(int level) {

            string listToken = null;
            if (context.AllowViewTokenList && HasExtensionTypes && (listToken = context.ListToken) != null) {
                IDbConnection dbConnection = context.GetDbConnection();
                IDataReader reader = context.GetQueryResult(String.Format("SELECT definition FROM filter WHERE id_basetype={0} AND token={1};", EntityType.GetEntityType(this.GetType()).TopTypeId, StringUtils.EscapeSql(listToken)), dbConnection);
                if (!reader.Read()) {
                    reader.Close();
                    context.ReturnError("Invalid list token");
                    return 0;
                }
                string filterDefinition = context.GetValue(reader, 0);
                context.CloseQueryResult(reader, dbConnection);
                inputSource = new FilterInputSource(filterDefinition);

            } else if (context.UserLevel < context.Privileges.MinUserLevelView) {
                if (!context.IsUserAuthenticated) context.RejectUnauthenticatedRequest();
                else context.ReturnError("You are not authorized to view this information");
                return 0;
            }
            // Get the entity's data structure if it is null
            if (Operations == null) AddBasicOperations();

            StartIndex = 0;
            if (Paging) {
                int i;
                string itemsPerPageStr = inputSource.GetValue("count");
                if (itemsPerPageStr == "*") {
                    Paging = false;
                } else {
                    if (Int32.TryParse(itemsPerPageStr, out i)) ItemsPerPage = i; else if (ItemsPerPage == 0) ItemsPerPage = 20;
                    if (Int32.TryParse(inputSource.GetValue("page"), out i)) {
                        Page = i;
                        StartIndex = (Page - 1) * ItemsPerPage;
                    } else if (Int32.TryParse(inputSource.GetValue("startIndex"), out i)) {
                        StartIndex = i;
                    } else if (Page == 0) {
                        StartIndex = 0;
                    }
                }
            }

            int aliasIndex = 0;
            string join = Join;

            string condition = FilterCondition;
            ExpandQuery(ref join, ref condition, ref aliasIndex);

            // -------------------------------------------------------------------------------------

            // STEP (2) : Retrieve SELECT and ORDER BY clause for the main query

            string countFields = null;
            string groupBy = CustomAggregation;
            string orderBy = CustomSorting;
            ExpandSortingClause(ref countFields, ref groupBy, ref orderBy);

            // -------------------------------------------------------------------------------------

            // STEP (3) : Retrieve the total number of results matching the search criteria

            string countQuery = null;

            switch (context.DatabaseSystem) {
                case DatabaseSystemType.Postgres :
                    countQuery = String.Format("WITH s1 AS (SELECT COUNT(DISTINCT {0}) FROM {1}{2}) SELECT COUNT(*) FROM s1;",
                                               countFields,
                                               join,
                                               (condition == null ? "" : " WHERE " + condition)
                                               );
                    break;
                    case DatabaseSystemType.MySql :
                    countQuery = String.Format("SELECT COUNT(DISTINCT {0}) FROM {1}{2};",
                                               countFields,
                                               join,
                                               (condition == null ? "" : " WHERE " + condition)
                                               );
                    break;
                    default :
                    break;
            }

            context.AddDebug(3, countQuery);
            //context.ReturnError("CIAO 2 " + countQuery + " " + Table + " " + Join);

            int totalResults = context.GetQueryIntegerValue(countQuery);
            //int totalResults = 0;

            // -------------------------------------------------------------------------------------

            // STEP (4) : In case of enabled paging, build the LIMIT and OFFSET clause

            string delimiters = null;
            if (Paging) {
                if (ItemsPerPage == -1) {
                    ItemsPerPage = totalResults;
                    StartIndex = 0;
                    delimiters = "";
                } else {
                    delimiters = " LIMIT " + ItemsPerPage + " OFFSET " + StartIndex;
                }
            }

            // -------------------------------------------------------------------------------------

            // STEP (5) : In case of enabled paging and ordered main query result with possibly more than one row per item, retrieve the delimiting values of the key fields

            if (Paging && InnerSorting != null && sortSelectCount != 0) {

                string pageQuery = String.Format("SELECT DISTINCT {0} FROM {1}{2}{3}{4}{5};",
                                                 sortSelect,
                                                 join,
                                                 (condition == null ? "" : " WHERE " + condition),
                                                 (groupBy == null ? "" : " GROUP BY " + groupBy),
                                                 (orderBy == null ? "" : " ORDER BY " + orderBy),
                                                 delimiters
                                                 );
                context.AddDebug(3, pageQuery + " " + sortSelectCount);

                bool first = true;
                IDbConnection dbConnection = context.GetDbConnection();
                IDataReader reader = context.GetQueryResult(pageQuery, dbConnection);
                while (reader.Read()) {
                    for (int i = 0; i < sortSelectCount; i++) {
                        if (first) sorts[i].PageFirstValue = reader.GetString(i);
                        sorts[i].PageLastValue = reader.GetString(i);
                    }
                    first = false;
                }
                context.CloseQueryResult(reader, dbConnection);
                //for (int i = 0; i < sortSelectCount; i++) context.AddInfo(i + " " + sorts[i].PageFirstValue + " ... " + sorts[i].PageLastValue);

                string pageCondition = BuildPageCondition();

                if (condition == null) condition = String.Empty; else condition += " AND ";
                condition += pageCondition;

                context.AddDebug(3, pageCondition);
            }

            // -------------------------------------------------------------------------------------

            // STEP (6) : Get rows of multiple value, multiple reference and multiple entity fields

            // GetMultipleFieldRows(FiedFlags.List, pageCondition);

            // -------------------------------------------------------------------------------------

            // STEP (7) : Retrieve the actual items and write the result to the output

            // Order records of item internally to have most relevant record as first (if one item can have several records)
            //if (innerGrouping != null) groupBy = (groupBy == null ? "" : groupBy + ", ") + innerGrouping;
            if (InnerSorting != null) orderBy = (orderBy == null ? "" : orderBy + ", ") + InnerSorting;

            //string additionalSelect = select;
            //select = (distinctSelect ? "DISTINCT " : "") + "t." + IdField;
            string select = "t." + IdField;
            int columnIndex = 1;

            ExpandSelectClause(ref columnIndex, ref select, "t", FieldFlags.List);

            //if (additionalSelect != null) select += ", " + additionalSelect;
            string sql = String.Format("SELECT DISTINCT {0} FROM {1}{2}{3}{4}{5};", 
                                       select,
                                       join,
                                       (condition == null ? "" : " WHERE " + condition),
                                       (groupBy == null ? "" : " GROUP BY " + groupBy),
                                       (orderBy == null ? "" : " ORDER BY " + orderBy),
                                       (Paging && InnerSorting == null ? delimiters : "")
                                       );
            //context.ReturnError(sql);
            context.AddDebug(3, sql);

            if (xmlWriter == null) xmlWriter = (level == 0 ? context.StartXmlResponse() : context.XmlWriter);

            int count = 0;

            if (OptionList) {
                count = WriteItemContents(sql);

            } else {
                if (level == 0) {
                    xmlWriter.WriteStartElement("itemList");
                    xmlWriter.WriteAttributeString("entity", EntityName);
                    xmlWriter.WriteAttributeString("link", ListUrl);
                    EntityType entityType = EntityType.GetEntityType(this.GetType()); 
                    if (PersistentFilters && entityType != null) xmlWriter.WriteAttributeString("filter", String.Format(context.AccountRootUrl == null ? "/account/filter.aspx?e={3}&url={0}" : "{1}/filters/{2}?url={0}", context.ScriptName, context.AccountRootUrl, entityType.Keyword, entityType.TopTypeId));

                    xmlWriter.WriteNamespaceDefinition(openSearchNamespacePrefix, "http://a9.com/-/spec/opensearch/1.1/");
                    WriteOpenSearchNamespaces();

                    xmlWriter.WritePrefixElementString(openSearchNamespacePrefix, "totalResults", totalResults.ToString());
                    if (Paging) {
                        xmlWriter.WritePrefixElementString(openSearchNamespacePrefix, "startIndex", StartIndex.ToString());
                        xmlWriter.WritePrefixElementString(openSearchNamespacePrefix, "startPage", Page.ToString());
                        xmlWriter.WritePrefixElementString(openSearchNamespacePrefix, "itemsPerPage", ItemsPerPage.ToString());
                    }

                    xmlWriter.WritePrefixStartElement(openSearchNamespacePrefix, "Query");
                    xmlWriter.WriteAttributeString("role", "request");
                    if (openSearchDescription != null) {
                        if (WriteOpenSearchParameterValues()) {
                            string value = inputSource.GetValue(searchTermsParamName);
                            if (value != null) xmlWriter.WriteAttributeString("searchTerms", value);
                        }
                    }
                    xmlWriter.WriteEndElement();

                    if (openSearchDescription != null) {
                        WriteOpenSearchDescription(true);
                        WriteAtomLink();
                    }

                    xmlWriter.WriteStartElement("operations");

                    foreach (EntityOperation operation in Operations) {
                        if (operation.MultipleItems) {
                            xmlWriter.WriteAttributeString("multiple", "true");
                            break; 
                        }
                    }
                    WriteOperations();
                    xmlWriter.WriteEndElement(); // </operations>

                    xmlWriter.WriteStartElement("fields");
                    WriteFields(FieldFlags.List, 0);
                    xmlWriter.WriteEndElement(); // </fields>
                    xmlWriter.WriteStartElement("items");
                }

                // Write result

                /*context.AddInfo(query);
                foreach (FieldExpression field in Fields) {
                    context.AddInfo("FIELD " + field.Name + ": columnIndex = " + field.ColumnIndex); 
                }*/

                count = WriteItemContents(0, sql, FieldFlags.List, level);

                if (level == 0) {
                    xmlWriter.WriteEndElement(); // </items>
                    xmlWriter.WriteEndElement(); // </itemList>
                }
            }

            return count;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the single item interface of a random item to the client.</summary>
        public void WriteRandomItem() {
            Random random = new Random();
            string condition = null;
            if (ExcludeIds != null && ExcludeIds.Length != 0) {
                for (int i = 0; i < ExcludeIds.Length; i++) {
                    if (i == 0) condition = ""; else condition += ",";
                    condition += ExcludeIds[i];
                }
                condition = " WHERE t." + IdField + (ExcludeIds.Length == 1 ? "!=" : " NOT IN (") + condition + (ExcludeIds.Length == 1 ? "" : ")");
            }
            int count = random.Next(context.GetQueryIntegerValue("SELECT COUNT(*) FROM " + Join + condition + ";"));
            int randomId = context.GetQueryIntegerValue("SELECT id FROM " + Join + condition + " LIMIT 1 OFFSET " + count + ";");
            Id = randomId;
            WriteSingleItem();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the single item interface of the specified item to the client.</summary>
        /*!
        /// <param name="id">the ID of the item</param>
        */
        public void WriteSingleItem(int id) {
            this.Id = id;
            DoWriteSingleItem("t." + IdField + "=" + Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteSingleItem(string identifier) {
            DoWriteSingleItem("t.identifier=" + StringUtils.EscapeSql(identifier));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteEmptyItem() {
            RequestedOperation = new EntityOperation(context, OperationType.Define, null, null, false, null, null);
            WriteSingleItem();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteSingleItem() {
            DoWriteSingleItem("t." + IdField + "=" + Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the single item interface of the current item to the client.</summary>
        private void DoWriteSingleItem(string condition) {
            if (CompositeItem) {
                WriteCompositeItem();
                return;
            }
            //if (id == 0) context.ReturnError("Content cannot be displayed"); // !!! what happens if id == 0 && !define ???

            if (CanViewItem) {
                if (Operations == null) AddBasicOperations();
                if (ViewingState != ViewingState.DefiningItem) CheckItem();
            }

            if (!CanViewItem) context.ReturnError("You are not authorized to view this information");

            if (ViewingState == ViewingState.DefiningItem) {
                OnBeforeDefine();
            } else {
                GetItem(condition, 0);
                OnBeforeWrite();
            }

            xmlWriter = context.StartXmlResponse();

            if (!OptionList) {
                xmlWriter.WriteStartElement("singleItem");
                xmlWriter.WriteAttributeString("entity", EntityName);
                //if (OperationCaption != null) xmlWriter.WriteAttributeString("caption", OperationCaption);
                if (Id != 0) xmlWriter.WriteAttributeString("link", ItemUrl);

                xmlWriter.WriteStartElement("operations");
                WriteOperations();
                xmlWriter.WriteEndElement(); // </operations>


                xmlWriter.WriteStartElement("fields");
                WriteFields(FieldFlags.Item, 0);
                xmlWriter.WriteEndElement(); // </fields>

                xmlWriter.WriteStartElement("item");
            }

            if (ViewingState == ViewingState.DefiningItem && RequestedOperation.Type == OperationType.Define) {
                foreach (FieldExpression field in Fields) {
                    if ((field.Flags & (FieldFlags.Item | FieldFlags.Attribute)) != (FieldFlags.Item)) continue;
                    xmlWriter.WriteStartElement(field.Name);
                    xmlWriter.WriteString("DEFINE-VALUE");
                    xmlWriter.WriteEndElement();
                }

            } else {
                if (Id != 0 && xmlWriter.WriteState == WriteState.Element) {
                    if (ShowItemIds) xmlWriter.WriteAttributeString("id", Id.ToString());
                    //if (ShowItemLinks) xmlWriter.WriteAttributeString("link", String.Format("{0}?id={1}", ItemUrl, Id));
                    if (ShowItemLinks) xmlWriter.WriteAttributeString("link", ItemUrl);

                    foreach (FieldExpression field in Fields) {
                        // OLD !!! if ((field.Flags & (FieldFlags.Item | FieldFlags.Attribute | FieldFlags.Hidden)) != (FieldFlags.Item | FieldFlags.Attribute)) continue;

                        // if ((field.Flags & flags) == 0 || (field.Flags & FieldFlags.Attribute) == 0) continue;
                        if ((field.Flags & FieldFlags.Item) == 0 || (field.Flags & FieldFlags.Attribute) == 0) continue; // use flags argument instead of FieldFlags.Item

                        string value = field.ToString();
                        //if ((field.Flags & FieldFlags.Custom) != 0) value = GetSpecificReadValue(field, value);
                        if ((field.Flags & FieldFlags.Hidden) != 0) continue; // this is done after getting the value, which may be used internally in the subclass

                        //if (value == null) value = reader.GetValue(index).ToString();
                        xmlWriter.WriteAttributeString(field.Name, value);
                    }
                }

                foreach (FieldExpression field in Fields) {
                    if ((field.Flags & (FieldFlags.Item | FieldFlags.Attribute | FieldFlags.Hidden)) != FieldFlags.Item) continue;

                    // Process fields according to type:
                    // * multiple entity fields: select entities with subordinate query (1)
                    // * multiple reference fields: select values with subordinate query (2)
                    // * single reference fields: select reference ID and optionally value (3)
                    // * single value field field (4)

                    bool multiple = field is IMultipleField;
                    //if (multiple && field.Values.Length == 0 || !multiple && field.Value == null) continue; // !!!

                    xmlWriter.WriteStartElement(field.Name);
                    if (multiple) {
                        //if ((field.Flags & FieldFlags.Hidden) != 0) continue;

                        if (!fromRequest) {
                            if (field is MultipleEntityField) { // (1)
                                MultipleEntityField field1 = field as MultipleEntityField;
                                if (field1.ForeignEntity.FilterCondition == null) field1.ForeignEntity.FilterCondition = ""; else field1.ForeignEntity.FilterCondition += " AND ";
                                field1.ForeignEntity.FilterCondition += "t." + field1.MultipleLinkField + "=" + Id + (field1.Condition == null ? "" : " AND " + field1.Condition);
                                field1.ForeignEntity.Paging = false;
                                field1.ForeignEntity.WriteItemList(1);

                            } else { // (2)
                                MultipleReferenceField field1 = field as MultipleReferenceField;

                                if (field1.ReferenceEntity == null) {

                                    string sql;
                                    if (field1.MultipleTable == null) {
                                        sql = "SELECT DISTINCT(t.{2}), {4} FROM {1} AS t WHERE t.{6}={0}{7};";
                                    } else {
                                        sql = "SELECT DISTINCT(t.{2}), {4} FROM {1} AS t INNER JOIN {5} AS m ON t.{2}=m.{3} WHERE m.{6}={0}{7};";
                                    }

                                    string refCondition = (field1.Condition == null ? String.Empty : " AND " + field1.Condition);

                                    if (field1.MultipleEntity != null) {
                                        foreach (FieldExpression multipleField in field1.MultipleEntity.Fields) {
                                            FixedValueField fixedField = multipleField as FixedValueField;
                                            if (fixedField != null) refCondition += " AND m." + fixedField.Field + "=" + fixedField.ToSqlString(); 
                                        }
                                    }

                                    sql = String.Format(sql,
                                                        Id,
                                                        field1.ReferenceTable,
                                                        field1.ReferenceIdField,
                                                        field1.ReferenceLinkField,
                                                        field1.ReferenceValueExpr.Replace("@.", "t."),
                                                        field1.MultipleTable,
                                                        field1.MultipleLinkField,
                                                        refCondition
                                                        );


                                    context.AddDebug(3, sql);
                                    IDbConnection dbConnection2 = context.GetNewDbConnection();
                                    IDataReader reader2 = context.GetQueryResult(sql, dbConnection2);

                                    while (reader2.Read()) {
                                        if (field1.SingleValue) {
                                            xmlWriter.WriteAttributeString("value", context.GetIntegerValue(reader2, 0).ToString());
                                            break;
                                        }
                                        xmlWriter.WriteStartElement("element");
                                        xmlWriter.WriteAttributeString("value", context.GetIntegerValue(reader2, 0).ToString());
                                        xmlWriter.WriteString(context.GetValue(reader2, 1));
                                        xmlWriter.WriteEndElement();
                                    }
                                    dbConnection2.Close();

                                } else {
                                    string join = field1.ReferenceEntity.Join;
                                    string refCondition = field1.ReferenceEntity.FilterCondition;

                                    if (field1.MultipleTable == null) {
                                        field1.ReferenceEntity.Join = join;
                                        field1.ReferenceEntity.FilterCondition = String.Format("t.{1}={0}{2}", Id, field1.MultipleLinkField, refCondition == null ? String.Empty : " AND " + refCondition);
                                    } else {
                                        field1.ReferenceEntity.Join = String.Format("{0} INNER JOIN {1} AS m ON t.{2}=m.{3}", join, field1.MultipleTable, field1.ReferenceIdField, field1.ReferenceLinkField);
                                        field1.ReferenceEntity.FilterCondition = String.Format("m.{1}={0}{2}", Id, field1.MultipleLinkField, refCondition == null ? String.Empty : " AND " + refCondition);
                                    }

                                    //context.AddInfo("FROM " + field.ReferenceEntity.Data.Join + " WHERE " + field.ReferenceEntity.FilterCondition); 
                                    field1.ReferenceEntity.WriteValues(xmlWriter);
                                    field1.ReferenceEntity.FilterCondition = refCondition;
                                    field1.ReferenceEntity.Join = join;
                                }
                            }
                        } else {
                            if (field.Invalid) xmlWriter.WriteAttributeString("valid", "false");
                        }

                    } else if (field is IReferenceField) { // (3)

                        IReferenceField field1 = field as IReferenceField;

                        if ((field.Flags & FieldFlags.Reduced) == 0) xmlWriter.WriteAttributeString("value", field.Value);
                        if (field1.ReferenceValueExpr != null) xmlWriter.WriteString(field.ValueCaption);

                    } else { // (4)
                        bool writeValue = true;
                        switch (field.Type) {
                            case "password" :
                                if (field.Value != null) xmlWriter.WriteAttributeString("stored", "true");
                                writeValue = false;
                                break;
                                case "link" :
                                xmlWriter.WriteAttributeString("link", field.Value);
                                writeValue = false;
                                break;
                        }
                        if (field.Invalid) xmlWriter.WriteAttributeString("valid", "false");
                        if (writeValue) xmlWriter.WriteString(field.Value);
                    }
                    xmlWriter.WriteEndElement(); // field.Name
                }
                //WriteItemContents(Id, null, FieldFlags.Item, 0);

            }
            if (!OptionList) {
                xmlWriter.WriteEndElement(); // </item>

                xmlWriter.WriteEndElement(); // </singleItem>
            }
        }

        // http://portal2.terradue-local.com/admin/ce.aspx?id=1&_request=modify&_format=xml&availability=3&caption=Terradue%20CE03&capacity=100&defaultServices=27&services=1,2,3,4,5,6,7,8,9,10&groups=1&address=ify-ce03.terradue.com&cePort=2&gsiPort=2&jobManager=manager&gridType=Local&jobQueue=infinite&statusMethod=1&wdirs:path=W1&rdirs:path=R1

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new item in the database using the data from the request.</summary>
        /*!
            An authorization check is performed before evaluating the data and writing the item to the database.

        /// <param name="owner">the MultipleEntityField containing the fields of the item (null for top level)</param>
        /// <param name="ownerId">the ID of the containing MultipleEntityField (ignored for top level)</param>
        /// <param name="startFrom">the index of the first sub-items to be created as new</param>
        /// <returns>the ID of the new item if the operation was successful, otherwise 0</returns>
        */
        protected virtual int CreateItem() {
            if (CompositeItem) return 0;

            if (!CanCreate) context.ReturnError("You are not authorized to add items");
            bool success = false;
            int missingValues = 0, invalidValues = 0;

            string sql, sqlFields = "", sqlValues = "", value;
            string[] sqlFieldsExtended, sqlValuesExtended;
            if (ExtensionTables == null) {
                sqlFieldsExtended = null;
                sqlValuesExtended = null;
            } else {
                sqlFieldsExtended = new string[ExtensionTables.Length];  
                sqlValuesExtended = new string[ExtensionTables.Length];
            }
            bool hasFields = false;
            int count = 0;

            bool validItem = false;


            // If at top level, insert one record with the given single value/single reference fields (1);
            // if at a sub-level (multiple entities), insert multiple records (2)
            Entity item = OnStore();

            if (item == null) {
                validItem = GetValuesFromRequest(null, null);

                if (!CheckItemConstraints()) return 0;

                sqlValues += "(";

                foreach (FieldExpression field in Fields) {

                    // Ignore read-only fields, list-only fields and other fields that cannot be written to a single data field

                    // Set the single reference to NULL if the reference is optional and the empty value has been provided // (1)
                    // and check for validity if necessary // (2)
                    if (field.Invalid) {
                        invalidValues++;
                        if (field.Value == null && (field.Flags & FieldFlags.Optional) == 0) missingValues++;
                        continue;
                    }

                    if (!(field is FixedValueField) && ((field.Flags & (FieldFlags.Item | FieldFlags.ReadOnly)) != FieldFlags.Item || !(field is SingleValueField) && !(field is SingleReferenceField))) continue;

                    if ((field.Flags & FieldFlags.Custom) == 0) {
                        value = field.ToSqlString();
                        if (value == null) {
                            AddValueFormatError(field);
                            field.Invalid = true;
                            invalidValues++;
                            continue;
                        }
                    } else {
                        value = field.SqlValue;
                        if (value == null) continue;
                    } 

                    if (field.ExtensionTable == null) {
                        sqlFields += (hasFields ? ", " : "") + field.Field;
                        sqlValues += (hasFields ? ", " : "") + value;
                        hasFields = true;
                    } else {
                        int index = field.ExtensionTable.Index;
                        if (sqlFieldsExtended[index] == null) {
                            sqlFieldsExtended[index] = String.Empty;
                            sqlValuesExtended[index] = String.Empty;
                        } else {
                            sqlFieldsExtended[index] += ", ";
                            sqlValuesExtended[index] += ", ";
                        }
                        //bool hasExtendedFields = (sqlFieldsExtended[index] != "");
                        sqlFieldsExtended[index] += field.Field;
                        sqlValuesExtended[index] += value;
                    }
                }
                sqlValues += ")";
                count = 1;

            } else {
                item.Store();
                return item.Id;
            }

            if (validItem && count != 0 && missingValues == 0 && invalidValues == 0) {
                //context.AddInfo("BEFORE");
                sql = "INSERT INTO " + Table + " (" + sqlFields + ") VALUES " + sqlValues + ";";
                context.AddDebug(3, sql);
                IDbConnection dbConnection = context.GetDbConnection();
                context.Execute(sql, dbConnection);
                //context.AddInfo("AFTER");
                success = true;
                Id = context.GetInsertId(dbConnection); // if at top level, set the ID of the main item that has just been created
                context.CloseDbConnection(dbConnection);
                if (ExtensionTables != null) {
                    for (int i = 0; i < ExtensionTables.Length; i++) { 
                        if (sqlFieldsExtended == null) continue;
                        sql = String.Format("INSERT INTO {1} ({2}{3}) VALUES ({0}{4});",
                                            Id,
                                            ExtensionTables[i].Name,
                                            ExtensionTables[i].IdField,
                                            sqlFieldsExtended[i] == null ? String.Empty : ", " + sqlFieldsExtended[i],
                                            sqlFieldsExtended[i] == null ? String.Empty : ", " + sqlValuesExtended[i]
                                            );
                        context.AddDebug(3, sql);
                        context.Execute(sql);
                    }
                }
            }

            if (success) {
                // Only at top level: add multiple reference fields (3), multiple value fields (4) and multiple entity fields (5)
                foreach (FieldExpression field in Fields) {
                    if ((field.Flags & (FieldFlags.Item | FieldFlags.ReadOnly | FieldFlags.Ignore)) != FieldFlags.Item || !(field is IMultipleField)) continue;

                    if (field is MultipleEntityField) { // (5)

                    } else if (field is MultipleReferenceField) { // (3)
                        MultipleReferenceField field1 = field as MultipleReferenceField;

                        //if ((field.Flags & FieldFlags.Custom) != 0 && field.SqlValue == null) continue;

                        //if (field1.Values.Length == 0) continue;

                        string fixedFields = String.Empty, fixedValues = String.Empty;
                        if (field1.MultipleEntity != null) {
                            foreach (FieldExpression multipleField in field1.MultipleEntity.Fields) {
                                FixedValueField fixedField = multipleField as FixedValueField;
                                if (fixedField != null) {
                                    fixedFields += ", " + fixedField.Field;
                                    fixedValues += ", " + fixedField.ToSqlString();
                                }
                            }
                        }

                        sql = null;
                        if (field1.MultipleTable == null) {
                            int recordCount = 0;
                            for (int j = 0; j < field1.Values.Length; j++) {
                                if (field1.Values[j] == null) continue;
                                if (sql == null) sql = String.Empty; else sql += ", ";
                                sql += field1.Values[j];
                                recordCount++;
                            }
                            if (sql == null) continue;
                            sql = String.Format("UPDATE {1} SET {2}={0} WHERE {3}{4}{5}{6};", Id, field1.ReferenceTable, field1.MultipleLinkField, field1.ReferenceIdField, recordCount == 1 ? "=" : " IN (", sql, recordCount == 1 ? String.Empty : ")");
                        } else {
                            for (int j = 0; j < field1.Values.Length; j++) {
                                if (field1.Values[j] == null) continue;

                                if (sql == null) sql = String.Empty; else sql += ", ";
                                sql += "(" + Id + ", " + field1.Values[j] + fixedValues + ")";
                            }
                            if (sql == null) continue;
                            sql = String.Format("INSERT INTO {0} ({1}, {2}{3}) VALUES {4};", field1.MultipleTable, field1.MultipleLinkField, field1.ReferenceLinkField, fixedFields, sql);
                        }
                        context.AddDebug(3, sql);
                        context.Execute(sql);

                    } else if (field is MultipleValueField) { // (4)
                        MultipleValueField field1 = field as MultipleValueField;
                        if (field1.Values.Length == 0) continue;

                        // Insert new records
                        sql = null;
                        for (int j = 0; j < field1.Values.Length; j++) {
                            if (field1.Values[j] == null) continue;

                            if (sql == null) sql = String.Empty; else sql += ", ";
                            sql += "(" + Id + ", " + field.ToSqlString(j) + ")";
                        }
                        if (sql == null) continue;

                        sql = String.Format("INSERT INTO {0} ({1}, {2}) VALUES {3};", field1.MultipleTable, field1.MultipleLinkField, field1.Expression.Replace("@.", ""), sql); // !!! field.Expression ??? why not field.Field? same in ModifyItem
                        context.AddDebug(3, sql);
                        context.Execute(sql);
                    }
                }
            }
            if (success) {
                if (!OnItemProcessed(OperationType.Create, Id)) context.AddInfo("A new item has been created");
                return Id;
            } else {
                if (missingValues == 0) {
                    if (OnItemNotProcessed(OperationType.Modify, Id)) return 0;
                    if (invalidValues == 0) context.ReturnError(new ArgumentException("The item was not created."), "incompleteInput");
                    else context.ReturnError(new ArgumentException("Not all fields have correct values."), "invalidInput");
                } else {
                    context.ReturnError(new ArgumentException("Not all mandatory fields have been filled."), "incompleteInput");
                }
                return 0;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Updates the current item in the database using the data from the request.</summary>
        /*!
            An authorization check is performed before evaluating the data and writing the item to the database.
        /// <returns><i>true</i> if the operation was successful</returns>
        */
        protected virtual bool ModifyItem() {
            if (CanModify) {
                CheckItem();
            }
            if (!CanModify) context.ReturnError("You are not authorized to change this item");

            if (CompositeItem) return ModifyCompositeItem();

            bool success = false;
            int missingValues = 0, invalidValues = 0;

            bool selective = (context.GetParamValue("_selective") == "true"); // "selective" means that only the values of the provided fields are changed

            string sql, sqlFields = "", value;
            string[] sqlFieldsExtended;
            if (ExtensionTables == null) sqlFieldsExtended = null;
            else sqlFieldsExtended = new string[ExtensionTables.Length]; 
            bool hasFields = false;
            int count = 0;

            bool validItem = false;

            Entity item = OnStore();

            if (item == null) {
                if (Id == 0) context.ReturnError("Cannot update, item ID missing or non-numeric");

                validItem = GetValuesFromRequest(null, null);
                if (!CheckItemConstraints()) return false;

                foreach (FieldExpression field in Fields) {
                    //context.AddInfo(field.Name + "=" + field.Value + "\n");

                    // Ignore empty fields if they are passwords or selective (manual) modification is requested
                    if (field.Value == null && (selective || (field.Flags & FieldFlags.Ignore) != 0)) {
                        field.Invalid = false;
                        continue;
                    }

                    // Ignore read-only fields, list-only fields and other fields that cannot be written to a single data field
                    if ((field.Flags & (FieldFlags.Item | FieldFlags.ReadOnly)) != FieldFlags.Item || !(field is SingleValueField) && !(field is SingleReferenceField)) continue;

                    // Set the single reference to NULL if the reference is optional and the empty value has been provided // (1)
                    // and check for validity if necessary // (2)
                    if (field.Invalid) {
                        invalidValues++;
                        if (field.Value == null && (field.Flags & FieldFlags.Optional) == 0) missingValues++;
                        continue;
                    } else if ((field.Flags & FieldFlags.Custom) == 0) {
                        value = field.ToSqlString();
                        if (value == null) {
                            AddValueFormatError(field);
                            field.Invalid = true;
                            invalidValues++;
                            continue;
                        }
                    } else {
                        value = field.SqlValue;
                        if (value == null) continue;
                    } 

                    if (field.ExtensionTable == null) {
                        sqlFields += (hasFields ? ", " : "") + field.Field + "=" + value;
                        hasFields = true;
                    } else {
                        int index = field.ExtensionTable.Index;
                        if (sqlFieldsExtended[index] == null) sqlFieldsExtended[index] = String.Empty;
                        else sqlFieldsExtended[index] += ", ";
                        sqlFieldsExtended[index] += field.Field + "=" + value;
                    }
                }

                //context.AddDebug(3, String.Format("validItem = {0}, hasFields = {1}, missingValues = {2}, invalidValues = {3}", validItem, hasFields, missingValues, invalidValues)); 

                if (validItem && hasFields && missingValues == 0 && invalidValues == 0) {
                    sql = String.Format("UPDATE {1} SET {2} WHERE {3}={0};",
                        Id,
                        Table,
                        sqlFields, 
                        IdField
                    );
                    context.AddDebug(3, sql);
                    context.Execute(sql);

                    if (ExtensionTables != null) {
                        for (int i = 0; i < ExtensionTables.Length; i++) {
                            if (sqlFieldsExtended == null || sqlFieldsExtended[i] == null) continue;
                            sql = String.Format("UPDATE {1} SET {3} WHERE {2}={0};",
                                Id,
                                ExtensionTables[i].Name,
                                ExtensionTables[i].IdField,
                                sqlFieldsExtended[i] 
                            );
                            context.AddDebug(3, sql);
                            context.Execute(sql);
                        }
                    }

                    success = true;
                }

            } else {

                item.Store();
                return true;

            }


            // Process fields according to type:
            // * multiple entity fields: select entities with subordinate query (1)
            // * multiple reference fields: select values with subordinate query (2)
            // * single reference fields: select reference ID and optionally value (3)
            // * single value field field (4)

            foreach (FieldExpression field in Fields) {
                if ((field.Flags & (FieldFlags.Item | FieldFlags.ReadOnly | FieldFlags.Ignore)) != FieldFlags.Item || !(field is IMultipleField)) continue;

                if (!validItem) {
                    if (field.Invalid) {
                        if (field.Values.Length == 0) missingValues++;
                        else invalidValues++;
                    }
                    continue;
                }

                if (field is MultipleEntityField) { // (5)
                } else if (field is MultipleReferenceField) { // (3)
                    MultipleReferenceField field1 = field as MultipleReferenceField;

                    //if ((field.Flags & FieldFlags.Custom) != 0 && field.SqlValue == null) continue;

                    //if (field1.Values.Length == 0) continue;
                    string condition = String.Empty;
                    EntityType entityType;

                    // In case a non-full administrator made the change, delete only the references that the user is authorized to set 
                    if (context.AdminMode && context.UserLevel < UserLevel.Administrator && (entityType = EntityType.GetEntityTypeFromName(field1.ReferenceTable)) != null) {
                        condition = String.Format("SELECT DISTINCT(t.id) FROM {0} AS t INNER JOIN priv AS r ON r.id_basetype={1} OR r.operation IN ('m', 'M') INNER JOIN role_priv AS r1 ON r.id=r1.id_priv INNER JOIN usr_role AS r2 ON r1.id_role=r2.id_role AND r2.id_usr={2}",
                                                  entityType.TopTable.Name,
                                                  entityType.TopTypeId,
                                                  context.UserId
                                                  );
                        if (entityType.TopTable.HasDomainReference) {
                            condition += String.Format(" INNER JOIN role AS r3 ON r1.id_role=r3.id AND (r.operation='A'{0} OR r3.id_domain=t.id_domain)", entityType.TopTable.Name == "usr" ? String.Empty : this is Gpod.Portal.GroupStructure ? " AND false" : " AND (t.id_domain IS NULL OR t.conf_deleg)");
                        }
                    }

                    if (field1.MultipleTable == null) {
                        if (condition != String.Empty) condition = String.Format(" AND t.{0} IN ({1})", field1.ReferenceIdField, condition);
                        sql = String.Format("UPDATE {0} SET {1}=NULL WHERE {1}={2}{3}{4};",
                                            field1.ReferenceTable,
                                            field1.MultipleLinkField,
                                            Id,
                                            field1.Condition == null ? "" : " AND " + field1.Condition,
                                            condition
                                            );
                    } else {
                        if (condition != String.Empty) condition = String.Format(" AND {0} IN ({1})", field1.ReferenceLinkField, condition);
                        sql = String.Format("DELETE FROM {0} WHERE {1}={2}{3}{4};",
                                            field1.MultipleTable,
                                            field1.MultipleLinkField,
                                            Id,
                                            field1.Condition == null ? "" : " AND " + field1.Condition,
                                            condition
                                            );
                    }
                    context.AddDebug(3, sql);
                    context.Execute(sql);

                    string fixedFields = String.Empty, fixedValues = String.Empty;
                    if (field1.MultipleEntity != null) {
                        foreach (FieldExpression multipleField in field1.MultipleEntity.Fields) {
                            FixedValueField fixedField = multipleField as FixedValueField;
                            if (fixedField != null) {
                                fixedFields += ", " + fixedField.Field;
                                fixedValues += ", " + fixedField.ToSqlString();
                            }
                        }
                    }

                    sql = null;

                    if (field1.MultipleTable == null) {
                        int recordCount = 0;
                        for (int j = 0; j < field1.Values.Length; j++) {
                            if (field1.Values[j] == null) continue;
                            if (sql == null) sql = String.Empty; else sql += ", ";
                            sql += field1.Values[j];
                            recordCount++;
                        }
                        if (sql == null) continue;
                        sql = String.Format("UPDATE {1} SET {2}={0} WHERE {3}{4}{5}{6};", Id, field1.ReferenceTable, field1.MultipleLinkField, field1.ReferenceIdField, recordCount == 1 ? "=" : " IN (", sql, recordCount == 1 ? String.Empty : ")");
                    } else {
                        for (int j = 0; j < field1.Values.Length; j++) {
                            if (field1.Values[j] == null) continue;

                            if (sql == null) sql = String.Empty; else sql += ", ";
                            sql += "(" + Id + ", " + field1.Values[j] + fixedValues + ")";
                        }
                        if (sql == null) continue;

                        sql = String.Format("INSERT INTO {0} ({1}, {2}{3}) VALUES {4};", field1.MultipleTable, field1.MultipleLinkField, field1.ReferenceLinkField, fixedFields, sql);
                    }
                    context.AddDebug(3, sql);
                    context.Execute(sql);

                } else if (field is MultipleValueField) { // (4)
                    MultipleValueField field1 = field as MultipleValueField;

                    // Delete records that are marked for being deleted
                    sql = "";
                    if (field1.DeleteIds != null && field1.DeleteIds.Length != 0) {
                        for (int j = 0; j < field1.DeleteIds.Length; j++) sql += (j == 0 ? "" : ", ") + field1.DeleteIds[j];
                        sql = String.Format("DELETE FROM {0} WHERE {1}{2}{3}{4};",
                                            field1.MultipleTable,
                                            field1.MultipleIdField,
                                            field1.DeleteIds.Length == 1 ? "=" : " IN (",
                                            sql,
                                            field1.DeleteIds.Length == 1 ? String.Empty : ")"
                                            );
                        context.AddDebug(3, sql);
                        context.Execute(sql);
                    }
                    if (field1.Values.Length == 0) continue;

                    // Change value of records that are marked for being updated
                    sql = "";
                    count = 0;
                    if (field1.UpdateIds != null && field1.UpdateIds.Length != 0) {
                        for (int j = 0; j < field1.UpdateIds.Length; j++) {
                            context.AddDebug(3, sql);
                            sql = "UPDATE " + field1.MultipleTable + " SET " + field1.Expression.Replace("@.", "") + "=" + field.ToSqlString(j) + " WHERE " + field1.MultipleIdField + "=" + field1.UpdateIds[j] + ";";
                        }
                        count = field1.UpdateIds.Length;
                    }
                    if (field1.Values.Length <= count) continue;

                    // Insert new records
                    for (int j = 0; j < field1.Values.Length; j++) sql += (j == 0 ? "" : ", ") + "(" + Id + ", " + field.ToSqlString(j) + ")";
                    sql = "INSERT INTO " + field1.MultipleTable + " (" + field1.MultipleLinkField + ", " + field1.Expression.Replace("@.", "") + ") VALUES " + sql + ";";
                    context.AddDebug(3, sql);
                    context.Execute(sql);
                }
            }
            if (success) {
                if (!OnItemProcessed(OperationType.Modify, Id)) context.AddInfo("The changes have been saved");
                return true;

            } else {
                if (missingValues == 0) {
                    if (OnItemNotProcessed(OperationType.Modify, Id)) return false;
                    if (invalidValues == 0) context.ReturnError(new ArgumentException("The item was not modified."), "incompleteInput");
                    else context.ReturnError(new ArgumentException("Not all fields have correct values."), "invalidInput");
                } else {
                    context.ReturnError(new ArgumentException("Not all mandatory fields have been filled."), "incompleteInput");
                }
                return false;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Deletes the item with the specified ID from the database.</summary>
        /*!
            An authorization check is performed before deleting the item from the database.

        /// <param name="id">the ID of the item to be deleted</param>
        */
        protected virtual void DeleteItem(int id) {
            if (CanDelete) {
                CheckItem();
            }
            if (!CanDelete) context.ReturnError("You are not authorized to delete this item");

            if (id == 0) context.ReturnError("Cannot delete, item ID missing or non-numeric");
            string sql;
            sql = "DELETE FROM " + Table + " WHERE " + IdField + "=" + id + ";";
            bool success = (context.Execute(sql) > 0);
            if (!OnItemProcessed(OperationType.Delete, id)) {
                if (success) context.WriteInfo("The item was deleted");
                else context.WriteWarning("No item could be deleted");
                ViewingState = ViewingState.ShowingList;
                if (Operations != null) Operations.Clear();
                AddBasicOperations();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Deletes the items with the specified IDs from the database.</summary>
        /*!
            An authorization check is performed before deleting the items from the database.

        /// <param name="ids">the array containing the IDs of the items to be deleted</param>
        */
        protected virtual void DeleteItems(int[] ids) {
            if (!CanDelete) context.ReturnError("You are not authorized to delete items");

            CheckItem();
            if (!CanDelete) context.ReturnError("You are not authorized to delete these items");

            string idsStr = "";
            for (int i = 0; i < ids.Length; i++) idsStr += (idsStr == "" ? "" : ",") + ids[i]; 
            string sql;
            sql = "DELETE FROM " + Table + " WHERE " + IdField + (ids.Length == 1 ? "=" + idsStr : " IN (" + idsStr + ")") + ";";
            int count = context.Execute(sql);
            if (!OnItemProcessed(OperationType.Delete, 0)) {
                if (count > 0) context.WriteInfo(count + " item" + (count == 1 ? " was" : "s were") + " deleted");
                else context.WriteWarning("Nothing was deleted");
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Updates the fields of the current composite item in the database using the data from the request.</summary>
        /*!
            An authorization check is performed before evaluating the data and writing any field of the item.
            
        /// <returns><i>true</i> if the operation was successful</returns>
        */
        protected virtual bool ModifyCompositeItem() {
            string value, condition = null;

            if (!CanModify) {
                context.ReturnError("You are not authorized to change this item");
                return false;
            }

            foreach (FieldExpression field in Fields) {
                if (field is FixedValueField) {
                    if (condition == null) condition = ""; else condition += " AND ";
                    condition += field.Field + (field.Value == null ? " IS NULL" : "=" + StringUtils.EscapeSql(field.Value)); 
                }
            }

            string fieldsCondition = FilterCondition;
            if (condition != null) fieldsCondition = (fieldsCondition == null ? "" : fieldsCondition + " AND ") + condition;  

            GetCompositeItemFields(fieldsCondition);
            CheckCompositeItemFields(true);

            int invalidValues = 0;
            for (int i = 0; i < compositeFields.Count; i++) {
                RequestParameter field = compositeFields[i];
                if (field.ReadOnly) continue;

                //value = field.ToSqlString();

                if (!field.AllValid) {
                    invalidValues++;
                    continue;
                }
                value = StringUtils.EscapeSql(field.Value); // all values must be quoted, because Boolean values without quotes are ignored by MySQL

                if (field.AllValid) context.Execute("UPDATE " + Table + " AS t SET value=" + value + " WHERE " + (condition == null ? "" : condition + " AND ") + "name=" + StringUtils.EscapeSql(field.Name) + ";"); 
            }

            if (!OnItemProcessed(OperationType.Modify, 0)) {
                if (invalidValues == 0) context.AddInfo("The changes have been saved");
                else context.AddWarning("The changes have been saved (only fields with correct values)", "invalidInput");
            }
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        private bool CheckItemConstraints() {
            string condition = null;

            foreach (FieldExpression field in Fields) {
                if (field.Value == null || (field.Flags & (FieldFlags.Item | FieldFlags.ReadOnly | FieldFlags.Unique)) != (FieldFlags.Item | FieldFlags.Unique) || !(field is SingleValueField) && !(field is SingleReferenceField) && !(field is FixedValueField)) continue;

                if (condition == null) condition = String.Empty; else condition += " AND ";
                IReferenceField field1;
                if ((field1 = field as IReferenceField) == null) condition += field.Expression + "=" + field.ToSqlString();
                else condition += condition += field1.ReferenceLinkField + "=" + field.ToSqlString();
            }

            if (condition == null) return true;

            string sql = String.Format("SELECT true FROM {1} WHERE {2} AND {3}!={0};", Id, Join, condition, IdField);
            if (context.DebugLevel == 3) context.AddDebug(3, sql);
            if (context.GetQueryBooleanValue(sql)) {
                HandleConstraintError();
                return false;
            }
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual bool HandleConstraintError() {
            int count = 0;
            string fields = null;

            foreach (FieldExpression field in Fields) {
                if ((field.Flags & (FieldFlags.Item | FieldFlags.ReadOnly | FieldFlags.Unique)) != (FieldFlags.Item | FieldFlags.Unique) || !(field is SingleValueField) || field.Value == null) continue;

                if (count++ == 0) fields = String.Empty; else fields += ", ";
                fields += (field.Caption == null ? field.Name : field.Caption);
            }

            if (count == 1) context.ReturnError(String.Format("The value for the field {0} already belongs to another item", fields), "uniqueConstraint");
            else context.ReturnError(String.Format("The values for the fields {0} already belong to another item", fields), "uniqueConstraint");
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the OpenSearch description for the entity.</summary>
        /*!
        /// <param name="shortName">the content for the OpenSearch description's <i>&lt;ShortName&gt;</i> element</param>
        /// <param name="longName">the content for the OpenSearch description's <i>&lt;LongName&gt;</i> element</param>
        /// <param name="description">the content for the OpenSearch description's <i>&lt;Description&gt;</i> element</param>
        */
        public void SetOpenSearchDescription(string shortName, string longName, string description) {
            this.openSearchDescription = new OpenSearchDescription(
                shortName, 
                longName, 
                description,
                (ListUrl == null ? context.ScriptUrl + "?" : ListUrl + (ListUrl.Contains("?") ? "&" : "?")) + "{format}&{searchfields}", // !!! use only ListUrl
                //context.ScriptUrl + "?{format}&{searchfields}",
                new FormatInfo[] {
                new FormatInfo(true, "_format=xml", "application/xhtml+xml"),
                new FormatInfo(true, "", "text/html")
            }
            );
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the OpenSearch description XML to the client.</summary>
        /*!
        /// <param name="urlsOnly">return only the <i>&lt;Url&gt;</i> elements instead of returning the complete description</param>
        */
        public void WriteOpenSearchDescription(bool urlsOnly) {

            if (openSearchDescription == null) {
                context.ReturnError("OpenSearch description not available");
                return;
            }

            bool searchable = false;

            bool hasFields = (FieldsTemplateStr != null);
            if (Paging) {
                FieldsTemplateStr += (hasFields ? "&" : String.Empty) + "count={count}&page={startPage}&startIndex={startIndex}";
                hasFields = true;
            }

            AppendOpenSearchExtensions(ref searchable);

            if (searchable) FieldsTemplateStr += (hasFields ? "&" : String.Empty) + searchTermsParamName + "={searchTerms}";

            if (context != null && !urlsOnly) xmlWriter = context.StartXmlResponse("text/xml");
            openSearchDescription.FieldsTemplate = FieldsTemplateStr;
            openSearchDescription.WriteNamespaces = WriteOpenSearchNamespaces;
            openSearchDescription.XmlWriter = xmlWriter;
            if (urlsOnly) openSearchDescription.WriteUrls(openSearchNamespacePrefix); 
            else openSearchDescription.Write();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Appends the OpenSearch extensions of the entity fields to the field template and checks whether the entity can be searched using the generic search terms extension.</summary>
        /*!
        /// <param name="searchable">reference to the value that determines whether the entity contains at least one field that accepts the generic search terms extension</param>
        */
        private void AppendOpenSearchExtensions(ref bool searchable) {
            foreach (FieldExpression field in Fields) {
                searchable |= ((field.Flags & FieldFlags.Searchable) != 0);
                if (field.SearchExtension != null) FieldsTemplateStr += (FieldsTemplateStr == String.Empty ? "" : "&") + field.Name + "=" + "{" + field.SearchExtension + "}";
                if (field is SingleReferenceField) {
                    SingleReferenceField field1 = field as SingleReferenceField;
                    if (field1.ReferenceEntity != null) field1.ReferenceEntity.AppendOpenSearchExtensions(ref searchable);
                } else if (field is MultipleEntityField) {
                    MultipleEntityField field1 = field as MultipleEntityField;
                    field1.ForeignEntity.AppendOpenSearchExtensions(ref searchable);
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Declares the default namespaces for the output. When overridden in a derived class, declares different or additional namespaces.</summary>
        protected virtual void WriteOpenSearchNamespaces() {
            xmlWriter.WriteNamespaceDefinition("ify", "http://www.terradue.com/ify");
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the OpenSearch parameter values into appropriate attributes of an OpenSearch <i>&lt;Query&gt;</i> element and checks whether the entity can be searched using the generic search terms extension.</summary>
        /*!
        /// <returns><i>true</i> if the entity contains at least one field that accepts the generic search terms extension</returns>
        */
        protected bool WriteOpenSearchParameterValues() {
            bool searchable = false;
            foreach (FieldExpression field in Fields) {
                if (field.Name == null) continue; // !!! because of FilterInputSource.GetValue(null) !!!

                searchable |= ((field.Flags & FieldFlags.Searchable) != 0);
                string value = inputSource.GetValue(field.Name);
                // Write the attribute (use the WebContext's XmlWriter reference, since an enclosed entity's XmlWriter reference may be unset)
                if (value != null && field.SearchExtension != null) context.XmlWriter.WriteAttributeString(field.SearchExtension, value);
                if (field is SingleReferenceField) {
                    SingleReferenceField field1 = field as SingleReferenceField;
                    if (field1.ReferenceEntity != null && field1.ReferenceEntity.WriteOpenSearchParameterValues()) searchable = true;
                } else if (field is MultipleEntityField) {
                    MultipleEntityField field1 = field as MultipleEntityField;
                    if (field1.ForeignEntity.WriteOpenSearchParameterValues()) searchable = true;
                }
            }
            return searchable;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the link element for the OpenSearch interface if available.</summary>
        private void WriteAtomLink() {
            if (openSearchDescription == null || context.UserLevel < context.Privileges.MinUserLevelView) return;

            xmlWriter.WriteStartElement("link");
            xmlWriter.WriteAttributeString("rel", "search");
            xmlWriter.WriteAttributeString("href", context.ScriptUrl + "?" + IfyWebContext.OperationParameterName + "=description");
            xmlWriter.WriteAttributeString("type", "application/opensearchdescription+xml");
            xmlWriter.WriteAttributeString("title", openSearchDescription.LongName);
            xmlWriter.WriteEndElement(); // </link>
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Retrieves the content of multiple fields of the entity for use in item lists.</summary>
        /*!
        /// <param name="flags">the combination flags of the fields that must be matched (e.g. <i>List</i> or <i>Item</i>)</param>
        /// <param name="mainCondition">... !!!</param>
        */
        private void GetMultipleFieldRows(FieldFlags flags, string mainCondition) {
            /*            foreach (FieldExpression field in Fields) {
                if ((field.Flags & (FieldFlags.Item | FieldFlags.Attribute)) != FieldFlags.Item) continue;
                
                // Process fields according to type:
                // * multiple entity fields: select entities with subordinate query (1)
                // * multiple reference fields: select values with subordinate query (2)
                // * single reference fields: select reference ID and optionally value (3)
                // * single value field field (4)

                if (field is IMultipleField) {
                    if ((field.Flags & FieldFlags.Hidden) != 0) continue;
                    
                    if (field is MultipleEntityField) { // (1)
                        MultipleEntityField field1 = field as MultipleEntityField;
                        xmlWriter.WriteStartElement(field.Name);
                        if (field1.ForeignEntity.FilterCondition == null) field1.ForeignEntity.FilterCondition = ""; else field1.ForeignEntity.FilterCondition += " AND ";
                        field1.ForeignEntity.FilterCondition += "t." + field1.MultipleLinkField + "=" + Id + (field1.Condition == null ? "" : " AND " + field1.Condition);
                        field1.ForeignEntity.Paging = false;
                        field1.ForeignEntity.WriteItemList(1, null, null);
                        xmlWriter.WriteEndElement();
                        
                    } else { // (2)
                        MultipleReferenceField field1 = field as MultipleReferenceField;

                        xmlWriter.WriteStartElement(field.Name);
                        context.OpenNewDbConnection();
                        
                        if (field1.ReferenceEntity == null) {

                            string condition = "m." + field1.MultipleLinkField + "=" + Id + (field1.Condition == null ? "" : " AND " + field1.Condition);
                            
                            if (field1.MultipleEntity != null) {
                                foreach (FieldExpression multipleField in field1.MultipleEntity.Fields) {
                                    FixedValueField fixedField = multipleField as FixedValueField;
                                    if (fixedField != null) condition += " AND m." + fixedField.Field + "=" + fixedField.ToSqlString(); 
                                }
                            }
                            
                            string sql = String.Format("SELECT DISTINCT(t.{1}), {3} FROM {0} AS t INNER JOIN {4} AS m ON t.{1}=m.{2} WHERE {5};",
                                    field1.ReferenceTable,
                                    field1.ReferenceIdField,
                                    field1.ReferenceLinkField,
                                    field1.ReferenceValueExpr.Replace("@.", "t."),
                                    field1.MultipleTable,
                                    condition
                            );


                            //context.AddError(sql);
                            IDataReader reader2 = context.GetQueryResult(sql);
                            
                            while (reader2.Read()) {
                                if (field1.SingleValue) {
                                    xmlWriter.WriteAttributeString("value", context.GetIntegerValue(reader2, 0).ToString());
                                    break;
                                }
                                xmlWriter.WriteStartElement("element");
                                xmlWriter.WriteAttributeString("value", context.GetIntegerValue(reader2, 0).ToString());
                                xmlWriter.WriteString(context.GetValue(reader2, 1));
                                xmlWriter.WriteEndElement();
                            }
                            reader2.Close();

                        } else {
                            string join = field1.ReferenceEntity.Join;
                            string condition = field1.ReferenceEntity.FilterCondition;
                            
                            field1.ReferenceEntity.Join = String.Format("{0} INNER JOIN {1} AS m ON t.{2}=m.{3}", join, field1.MultipleTable, field1.ReferenceIdField, field1.ReferenceLinkField);
                            field1.ReferenceEntity.FilterCondition = "m." + field1.MultipleLinkField + "=" + Id + (condition == null ? String.Empty : " AND " + condition);
                            //context.AddInfo("FROM " + field.ReferenceEntity.Join + " WHERE " + field.ReferenceEntity.FilterCondition); 
                            field1.ReferenceEntity.WriteValues(xmlWriter);
                            field1.ReferenceEntity.FilterCondition = condition;
                            field1.ReferenceEntity.Join = join;
                        }
                        context.CloseLastDbConnection();
                        xmlWriter.WriteEndElement();
                    }
                }*/

        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void GetViewingState() {
            bool UseRest = false; // TODO: make this a Control Panel variable
            if (Id == 0) {
                if (UseRest && context.HttpMethod == "POST" || context.RequestedOperation == "define"  || context.RequestedOperation == "create") ViewingState = ViewingState.DefiningItem;
                else ViewingState = ViewingState.ShowingList;
            } else {
                ViewingState = ViewingState.ShowingItem;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityOperation AddOperation(OperationType type, string identifier, string name, string httpMethod, string url, EntityOperationMethodType operationMethod) {
            return AddOperation(type, identifier, name, false, httpMethod, url, true, operationMethod);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityOperation AddOperation(OperationType type, string identifier, string name, bool multipleItems, string httpMethod, string url, bool appendItemUrl, EntityOperationMethodType operationMethod) {
            if (Operations == null) Operations = new List<EntityOperation>();
            if (appendItemUrl) url = String.Format("{0}{1}", ItemUrl, url == null ? String.Empty : String.Format("{0}{1}", ItemUrl.Contains("?") ? "&" : "?", url));
            EntityOperation result = new EntityOperation(context, type, identifier, name, multipleItems, httpMethod, url);
            Operations.Add(result);
            return result;

        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the generic set of operations that are available in the current interface to the output.</summary>
        /*!
        /// <param name="type">the output type (single item, item list etc.), see OutputType</param>
        /// <param name="multiple">in case of item list output type, determines whether there is at least one operation applicable on multiple items</param>
            
            It is assumed that the <i>&lt;operations&gt;</i> element is opened immediately before and closed after the call by the calling method.
        */
        protected virtual void WriteOperations() {
            if (Operations == null) return;
            foreach (EntityOperation operation in Operations) {
                if ((operation.Type == OperationType.Define || operation.Type == OperationType.Create) && !CanCreate) continue;
                if (operation.Type == OperationType.Modify && !CanModify) continue;
                if (operation.Type == OperationType.Delete && !CanDelete) continue;
                WriteOperation(operation);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void AddBasicOperations() {
            //if (Operations != null) Operations.Clear();

            if (IsComposite) {
                if (CanModify) AddOperation(OperationType.Modify, "modify", "Modify", "POST", "_request=modify&_all=true", null);
                return;
            }

            if (ViewingState == ViewingState.ShowingList) {
                if (CanCreate) {
                    int count = 0;
                    if (HasExtensionTypes) {
                        IDataReader reader = context.GetExtensionTypeQuery(this.GetType());
                        while (reader.Read()) {
                            AddOperation(OperationType.Define, "define", String.Format("Create New ({0})", reader.GetString(1)), "GET", String.Format("_request=define&_type={0}", reader.GetInt32(0)), null);
                            count++;
                        }
                        reader.Close();
                    }
                    if (count == 0) AddOperation(OperationType.Define, "define", "Create New", "GET", String.Format("{0}_request=define", ExtraParameter == null ? String.Empty : ExtraParameter + "&"), null);
                }
                if (CanDelete) AddOperation(OperationType.Delete, "delete", "Delete", true, "POST", "_request=delete&_mult=true", true, null); // multiple delete, must be POST
            } else if (ViewingState == ViewingState.DefiningItem) {
                int typeId = 0;
                if (HasExtensionTypes) typeId = GetExtensionTypeFromRequest();
                if (CanCreate) AddOperation(OperationType.Create, "create", "Save", "POST", String.Format("{0}_request=create{1}", ExtraParameter == null ? String.Empty : ExtraParameter + "&", typeId == 0 ? String.Empty : "&_type=" + typeId.ToString()), null);
            } else if (ViewingState == ViewingState.ShowingItem) {
                if (CanModify) AddOperation(OperationType.Modify, "modify", "Modify", "POST", "_request=modify", null);
                if (CanDelete) AddOperation(OperationType.Delete, "delete", "Delete", "GET", "_request=delete", null);
            }
            if (Operations == null) Operations = new List<EntityOperation>();
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual void WriteOperation(EntityOperation operation) {
            xmlWriter.WriteStartElement("operation");
            xmlWriter.WriteAttributeString("name", operation.Identifier);
            if (operation.Parent != null) xmlWriter.WriteAttributeString("owner", operation.Parent.Identifier);
            xmlWriter.WriteAttributeString("caption", operation.Name);
            //xmlWriter.WriteAttributeString("link", String.Format("{0}{1}", ItemUrl, queryString == null ? String.Empty : String.Format("{0}{1}", ItemUrl.Contains("?") ? "&" : "?", queryString)));
            xmlWriter.WriteAttributeString("link", operation.Url);
            xmlWriter.WriteAttributeString("method", operation.HttpMethod);
            xmlWriter.WriteEndElement(); // </operation>
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes a single operation that uses the current interface to the output.</summary>
        /*!
        /// <param name="name">the name of the operation (must be unique within its owning operation set)</param>
        /// <param name="owner">the name of the owning operation if it modifies another operation; <i>null</i> otherwise</param>
        /// <param name="caption">the operation caption displayed in the GUI</param>
        /// <param name="queryString">an optional query string to be appended to the current script name</param>
        /// <param name="method">the HTTP method to be used for the operation (<i>GET</i>, <i>POST</i> or <i>PUT</i>)</param>
        */
        protected void WriteOperation(string name, string owner, string caption, string queryString, string method) {
            xmlWriter.WriteStartElement("operation");
            xmlWriter.WriteAttributeString("name", name);
            if (owner != null) xmlWriter.WriteAttributeString("owner", owner);
            xmlWriter.WriteAttributeString("caption", caption);
            xmlWriter.WriteAttributeString("link", String.Format("{0}{1}", ItemUrl, queryString == null ? String.Empty : String.Format("{0}{1}", ItemUrl.Contains("?") ? "&" : "?", queryString)));
            //xmlWriter.WriteAttributeString("link", "/analyze.aspx?file=true");
            xmlWriter.WriteAttributeString("method", method);
            xmlWriter.WriteEndElement(); // </operation>
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes a single operation that uses another interface to the output.</summary>
        /*!
        /// <param name="name">the name of the operation (must be unique within its owning operation set)</param>
        /// <param name="owner">the name of the owning operation if it modifies another operation; <i>null</i> otherwise</param>
        /// <param name="caption">the operation caption displayed in the GUI</param>
        /// <param name="link">the absolute or relative URL of the interface that handles the operation</param>
        /// <param name="method">the HTTP method to be used for the operation (<i>GET</i>, <i>POST</i> or <i>PUT</i>)</param>
        */
        protected void WriteExternalOperation(string name, string owner, string caption, string link, string method) {
            xmlWriter.WriteStartElement("operation");
            xmlWriter.WriteAttributeString("name", name);
            if (owner != null) xmlWriter.WriteAttributeString("owner", owner);
            xmlWriter.WriteAttributeString("caption", caption);
            xmlWriter.WriteAttributeString("link", link);
            xmlWriter.WriteAttributeString("method", method);
            xmlWriter.WriteEndElement(); // </operation>
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the requested item exists and what privileges the current user has before performing an operation on the item.</summary>
        /*!
            If the item does not exist, an exception is thrown.
            
            If the item exists but the user is not allowed to process it, e.g. because he is not the owner, the behaviour depends on the user level:
            <ul>
                <li>If the user is an administrator, a warning message is emitted,</li>
                <li>Otherwise the values of CanViewItem, CanModify and CanDelete are set to <i>false</i> and the calling method may throw an exception.</li>
            </ul>
        */
        protected virtual void CheckItem() {
            int scopeValue = 0;
            bool single = (Id != 0 || Identifier != null);
            if (single || Ids != null && Ids.Length != 0) {
                string where;
                if (!single) {
                    where = String.Empty;
                    for (int i = 0; i < Ids.Length; i++) where += (i == 0 ? "" : ", ") + Ids[i];
                    where = "t." + IdField + " IN (" + where + ")";
                } else if (Id != 0) {
                    where = "t." + IdField + "=" + Id;
                } else {
                    where = "t.identifier=" + StringUtils.EscapeSql(Identifier);
                }
                string condition = FilterCondition;
                foreach (FieldExpression field in Fields) {
                    if (field is FixedValueField) {
                        if (condition == null) condition = ""; else condition += " AND ";
                        condition += field.Field + (field.Value == null ? " IS NULL" : "=" + StringUtils.EscapeSql(field.Value)); 
                    }
                }
                if (condition == null) condition = "true";
                scopeValue = context.GetQueryIntegerValue(String.Format("SELECT MIN(CASE WHEN {0} THEN 2 ELSE 1 END) FROM {1} WHERE {2};", condition, Join, where));
                if (scopeValue == 1) {
                    if (context.UserLevel >= UserLevel.Administrator) {
                        context.AddWarning(single ? "The specified item is usually not accessible from this page" : "At least one of the specified items is usually not accessible from this page");
                    } else {
                        CanViewItem = false;
                        CanModify = false;
                        CanDelete = false;
                    }
                } else if (scopeValue == 0) {
                    context.ReturnError(single ? "The specified item does not exist" : "At least one of the specified items does not exist");
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void WriteFields(FieldFlags flags, int level) {
            if (xmlWriter == null) xmlWriter = context.XmlWriter;

            // Assign value sets if single item is requested
            AssignValueSets(); // !!! should not be done if already assigned after CreateItem or ModifyItem

            foreach (FieldExpression field in Fields) {
                if ((field.Flags & (flags | FieldFlags.Attribute | FieldFlags.Hidden)) != flags) continue;

                if (field is IEntityField) {
                    if (!OptionList) {
                        xmlWriter.WriteStartElement("field");
                        WriteFieldAttributes(field);
                        xmlWriter.WriteStartElement("fields");
                    }
                    (field as IEntityField).ForeignEntity.WriteFields(flags, level + 1);
                    if (!OptionList) {
                        xmlWriter.WriteEndElement(); // </fields>
                        xmlWriter.WriteEndElement(); // </field>
                    }
                } else if (!OptionList) {
                    xmlWriter.WriteStartElement("field");
                    WriteFieldAttributes(field);
                    if (field.ValueSet != null) {
                        IReferenceField field1;
                        if ((field.Flags & FieldFlags.Optional) != 0 && !(field is IMultipleField) && (field1 = field as IReferenceField) != null) {
                            xmlWriter.WriteStartElement("element");
                            xmlWriter.WriteAttributeString("value", String.Empty);
                            xmlWriter.WriteString(field1.NullCaption == null ? "[no selection]" : field1.NullCaption);
                            xmlWriter.WriteEndElement();
                        }
                        field.ValueSet.WriteValues(xmlWriter);
                    }
                    xmlWriter.WriteEndElement(); // </field>
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void AssignValueSets() {
            foreach (FieldExpression field in Fields) {
                if (field.ValueSet != null) continue;

                if (field is IReferenceField) {
                    IReferenceField field1 = field as IReferenceField;

                    if (field1.ReferenceEntity != null) {
                        field.ValueSet = field1.ReferenceEntity;
                    } else {
                        string join = field1.ReferenceTable + " AS t";
                        EntityType entityType;
                        if (context.AdminMode && context.UserLevel < UserLevel.Administrator && (entityType = EntityType.GetEntityTypeFromName(field1.ReferenceTable)) != null) {
                            join += String.Format(" INNER JOIN priv AS r ON r.id_basetype={0} OR r.operation IN ('m', 'M') INNER JOIN role_priv AS r1 ON r.id=r1.id_priv INNER JOIN usr_role AS r2 ON r1.id_role=r2.id_role AND r2.id_usr={1}",
                                                  entityType.TopTypeId,
                                                  context.UserId
                                                  );
                            if (entityType.TopTable.HasDomainReference) {
                                join += String.Format(" INNER JOIN role AS r3 ON r1.id_role=r3.id AND (r.operation='A'{0} OR r3.id_domain=t.id_domain)", entityType.TopTable.Name == "usr" ? String.Empty : this is Gpod.Portal.GroupStructure ? " AND false" : " AND (t.id_domain IS NULL OR t.conf_deleg)");
                            } else if (entityType.ClassType == typeof(Domain)) {
                                join += " INNER JOIN role AS r3 ON r1.id_role=r3.id AND (r3.id_domain IS NULL AND r.operation='A' OR r3.id_domain=t.id)";
                                field.Flags = field.Flags & FieldFlags.AllButOptional;
                            }
                        }
                        ReferenceValueSet valueSet = new ReferenceValueSet(
                            context,
                            join,
                            "t." + field1.ReferenceIdField, 
                            (field1.ReferenceValueExpr == null ? field1.ReferenceIdField : field1.ReferenceValueExpr.Replace("@.", "t."))
                            );
                        valueSet.SortExpression = field1.SortExpression;
                        field.ValueSet = valueSet;
                    }
                } else if (!(field is IEntityField) && (field.Flags & FieldFlags.Lookup) != 0) {
                    field.ValueSet = new LookupValueSet(
                        context,
                        field.Type,
                        true
                        );
                    field.Type = (field is IMultipleField ? "multiple" : "select");
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void CheckField(FieldExpression field) {
            // Check that non-optional fields have a value, but empty password fields in "modify" requests are skipped
            context.AddDebug(3, "field " + field.Name + ", value = " + (field.Value == null ? "NULL" : field.Value) + " " + (field.Invalid ? " (invalid)" : String.Empty));
            if (field.Type == "password" && Id != 0) {
                if ((field.Flags & FieldFlags.Custom) == 0) field.Flags |= FieldFlags.Ignore; // skip empty password field in "modify" request

            } else if (field.ValueSet == null) {
                field.Invalid |= (field.Value == null && field.Type != "bool" && (field.Flags & (FieldFlags.Optional | FieldFlags.ReadOnly)) == 0);
            } else {
                bool write = false;//!OptionList && (field.Flags & FieldFlags.Reduced) == 0;
                if (field is IMultipleField) {
                    if (field.Values == null) field.Values = new string[0];
                    // //field.ValueSet.WriteValues(OptionList ? null : xmlWriter);//(field.Values, ref valid, ref defaultValue, xmlWriter);

                    MultipleReferenceField field1;
                    if (write && (field.Flags & FieldFlags.Optional) != 0 && (field1 = field as MultipleReferenceField) != null && field1.SingleValue) {
                        xmlWriter.WriteStartElement("element");
                        xmlWriter.WriteAttributeString("value", String.Empty);
                        xmlWriter.WriteString(field1.NullCaption == null ? "[no selection]" : field1.NullCaption);
                        xmlWriter.WriteEndElement();
                    }
                    if (ViewingState == ViewingState.DefiningItem) {
                        field.Invalid = false; // flag field as valid ??? !!!
                        if (write) field.ValueSet.WriteValues(OptionList ? null : xmlWriter);
                    } else {
                        //context.AddInfo("field(0) " + field.Name + " invalid=" + field.Invalid);
                        field.Invalid |= field.Values.Length == 0 && (field.Flags & (FieldFlags.Optional | FieldFlags.ReadOnly)) == 0;
                        //context.AddInfo("field(1) " + field.Name + " invalid=" + field.Invalid);
                        string[] defaultValues;
                        bool[] valid = field.ValueSet.CheckValues(field.Values, out defaultValues);
                        for (int i = 0; i < valid.Length; i++) {
                            if (!valid[i] && field.Values[i] != null && (field.Flags & FieldFlags.ReadOnly) == 0) { // second condition maybe integrate in ValueSet classes
                                field.Invalid = true;
                                break;
                            }
                        }
                        if (!field.Invalid && field.Values.Length == 0 && defaultValues != null) field.Values = defaultValues;

                        //if (!valid) field.Value = String.Empty;
                    }
                } else {
                    IReferenceField field1;

                    // If field is optional, write optional caption
                    if (write && (field.Flags & FieldFlags.Optional) != 0 && (field1 = field as IReferenceField) != null) {
                        xmlWriter.WriteStartElement("element");
                        xmlWriter.WriteAttributeString("value", String.Empty);
                        xmlWriter.WriteString(field1.NullCaption == null ? "[no selection]" : field1.NullCaption);
                        xmlWriter.WriteEndElement();
                    }
                    field.Invalid = false;
                    if (ViewingState == ViewingState.DefiningItem) {
                        if (write) field.ValueSet.WriteValues(OptionList ? null : xmlWriter);
                    } else {
                        string defaultValue;
                        string selectedCaption;
                        bool valid = field.ValueSet.CheckValue(field.Value, out defaultValue, out selectedCaption);
                        if (field.Value == null && defaultValue != null) field.Value = defaultValue;
                        else if (!valid && (field.Value != null || (field.Flags & (FieldFlags.Optional | FieldFlags.ReadOnly)) == 0)) field.Invalid = true;
                        field.ValueCaption = selectedCaption;
                        //if (!valid) field.Value = String.Empty;
                    }
                }
            }
            /* if (!OptionList) xmlWriter.WriteEndElement(); // </field>*/
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteFieldAttributes(FieldExpression field) {
            xmlWriter.WriteAttributeString("name", field.Name);
            xmlWriter.WriteAttributeString("type", field.Type);
            if (field.ValueSet != null && field.ValueSet is LookupValueSet) xmlWriter.WriteAttributeString("source", (field.ValueSet as LookupValueSet).ListNames);
            xmlWriter.WriteAttributeString("caption", field.Caption);
            if (field.SearchExtension != null) xmlWriter.WriteAttributeString("ext", field.SearchExtension);
            if ((field.Flags & FieldFlags.ReadOnly) != 0) xmlWriter.WriteAttributeString("readonly", "true");
            if ((field.Flags & FieldFlags.Optional) != 0) xmlWriter.WriteAttributeString("optional", "true");
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteCompositeItem() {
            if (context.UserLevel < context.Privileges.MinUserLevelView) context.ReturnError("You are not authorized to view this information");

            xmlWriter = context.StartXmlResponse();

            xmlWriter.WriteStartElement("singleItem");
            xmlWriter.WriteAttributeString("entity", EntityName);
            xmlWriter.WriteAttributeString("link", String.Format("{0}{1}", context.ScriptName, Id == 0 ? String.Empty : "?id=" + Id));

            xmlWriter.WriteStartElement("operations");
            WriteOperations();
            xmlWriter.WriteEndElement(); // </operations>

            if (compositeFields == null) GetCompositeItemFields(FilterCondition);
            WriteCompositeItemFields();

            xmlWriter.WriteStartElement("item");

            foreach (FieldExpression field in Fields) {
                if (field is FixedValueField) { 
                    xmlWriter.WriteAttributeString(field.Name, field.Value);
                    break;
                }
            }
            for (int i = 0; i < compositeFields.Count; i++) {
                RequestParameter field = compositeFields[i];
                xmlWriter.WriteStartElement(FieldNamePrefix + field.Name);
                if (!field.AllValid) xmlWriter.WriteAttributeString("valid", "false");
                if (field.Message != null) xmlWriter.WriteAttributeString("message", field.Message);
                xmlWriter.WriteString(field.ToString());
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement(); // </item>


            /*            IDataReader reader = context.GetQueryResult("SELECT name, NULL, value FROM " + Table + ";");

            xmlWriter.WriteStartElement("item");
            while (reader.Read()) {
                xmlWriter.WriteStartElement(context.GetValue(reader, 0));
                xmlWriter.WriteString(context.GetValue(reader, 2));
                xmlWriter.WriteEndElement();
            }
            reader.Close();

            xmlWriter.WriteEndElement(); // </item>
*/            xmlWriter.WriteEndElement(); // </singeItem>
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void ExpandCompositeItemQuery(ref int columnIndex, ref int aliasIndex, ref string select, ref string join, ref string condition, ref string orderBy) {
            string defaultOrderBy = null;
            string currentAlias = "t" + (aliasIndex == 0 ? "" : aliasIndex.ToString());
            
            foreach (FieldExpression field in Fields) {
                
                if (field is FixedValueField) {
                    if (condition == null) condition = ""; else condition += " AND ";
                    condition += field.Field + (field.Value == null ? " IS NULL" : "=" + StringUtils.EscapeSql(field.Value)); 
                    
                } else if (field is SingleReferenceField) {
                    SingleReferenceField field1 = (field as SingleReferenceField);
                    
                    field1.ReferenceTableAlias = "t" + (++aliasIndex).ToString();
                    join += " " + ((field.Flags & FieldFlags.Optional) != 0 ? "LEFT" : "INNER") + " JOIN " + field1.ReferenceTable + " AS " + field1.ReferenceTableAlias + " ON " + currentAlias + "." + field1.ReferenceLinkField + "=" + field1.ReferenceTableAlias + "." + field1.ReferenceIdField;
                        
                    if (field1.ReferenceEntity == null) {
                        if ((field.Flags & FieldFlags.Sort) != 0) {
                            if (defaultOrderBy == null) defaultOrderBy = ""; else defaultOrderBy += ", ";
                            defaultOrderBy +=
                                    ((field.Flags & FieldFlags.Item) == 0 ? field.Expression.Replace("@.", field1.ReferenceTableAlias + ".") : "_" + field.Name) + 
                                    ((field.Flags & FieldFlags.SortAsc) == 0 ? " DESC" : "");
                        }
                    } else {
                        field1.ReferenceEntity.ExpandCompositeItemQuery(ref columnIndex, ref aliasIndex, ref select, ref join, ref condition, ref orderBy);
                        continue;
                    }

                } else {
                    if ((field.Flags & FieldFlags.Sort) != 0) {
                        if (defaultOrderBy == null) defaultOrderBy = ""; else defaultOrderBy += ", ";
                        defaultOrderBy +=
                                ((field.Flags & FieldFlags.Item) == 0 ? field.Expression.Replace("t.", currentAlias + ".") : "_" + field.Name) + 
                                ((field.Flags & FieldFlags.SortAsc) == 0 ? " DESC" : "");
                    }
                }
                
                //context.AddInfo("ECIQ " + field.Name + " " + ((field.Flags & FieldFlags.Item) != 0)); 
                
                if ((field.Flags & FieldFlags.Item) == 0) continue;
                
                field.ColumnIndex = columnIndex++;
                //context.AddInfo("ECIQ " + field.Name + " " + field.ColumnIndex); 

                if (select == null) select = ""; else select += ", ";
                if (field is SingleReferenceField) {
                    SingleReferenceField field1 = (field as SingleReferenceField);
                    select += field.Expression.Replace("@.", field1.ReferenceTableAlias + ".") + " AS _" + field.Name;
                } else {
                    select += field.Expression.Replace("t.", currentAlias + ".") + " AS _" + field.Name;
                }
            }
            if (defaultOrderBy != null) orderBy = (orderBy == null ? "" : orderBy + ", ") + defaultOrderBy;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        private void WriteAdditionalCompositeItemFields(IDataReader reader) {
            if (xmlWriter == null) xmlWriter = context.XmlWriter;
            
            foreach (FieldExpression field in Fields) {
                if (field is SingleReferenceField) {
                    SingleReferenceField field1 = (field as SingleReferenceField);
                    if (field1.ReferenceEntity != null) {
                        field1.ReferenceEntity.WriteAdditionalCompositeItemFields(reader);
                        continue;
                    }
                }
                if ((field.Flags & FieldFlags.Item) == 0 || field.Reserved) continue; 

                xmlWriter.WriteAttributeString(field.Name, context.GetValue(reader, field.ColumnIndex));
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        private void CheckCompositeItemFields(bool getFromRequest) {
            if (compositeFields == null) compositeFields = new RequestParameterCollection();
            bool all = (getFromRequest && context.GetParamValue("_all") == "true");
            
            for (int i = 0; i < compositeFields.Count; i++) {
                RequestParameter param = compositeFields[i];
                if (getFromRequest) {
                    string value = context.GetParamValue(FieldNamePrefix + param.Name);
                    if (all && value == null && param.Type != "password") param.Unset(); else param.Value = value;
                }
                
                // Note: virtual item fields keep their original values if the request does not contain a new value (-> RequestParameter.Value.set) 
                param.Check(null, false);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void WriteCompositeItemFields() {
            if (compositeFields == null) compositeFields = new RequestParameterCollection();

            xmlWriter.WriteStartElement("fields");
            for (int i = 0; i < compositeFields.Count; i++) {
                RequestParameter param = compositeFields[i];
                xmlWriter.WriteStartElement("field");
                param.WriteFieldAttributes(xmlWriter);
                param.Check(xmlWriter, false); // no need to check again, but no param.WriteElements method available
                if (!param.AllValid) {
                    if (param.Value == null) context.AddError("A value is required for \"" + param.Caption + "\"");
                    else context.AddError("The value for \"" + param.Caption + "\" is not valid");
                }
                xmlWriter.WriteEndElement(); // </field>
            }
            xmlWriter.WriteEndElement(); // </fields>
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void GetCompositeItemFields(string condition) {
            string select = null;
            string join = Join;
            string orderBy = null;
            int aliasIndex = 0;
            int columnIndex = 0;
            
            ExpandCompositeItemQuery(ref columnIndex, ref aliasIndex, ref select, ref join, ref condition, ref orderBy);
            string query = "SELECT " + select + " FROM " + join + (condition == null ? "" : " WHERE " + condition) + (orderBy == null ? "" : " ORDER BY " + orderBy) + ";";
            
            int nameIndex = -1, typeIndex = -1, sourceIndex = -1, captionIndex = -1, valueIndex = -1, optionalIndex = -1, readonlyIndex = -1;
            string name = null, type = null, source = null, caption = null, value = null;
            bool optional, readOnly;// = false, readOnly = false;
            
            foreach (FieldExpression field in Fields) {
                if ((field.Flags & FieldFlags.Item) == 0) continue;
                field.Reserved = true;
                switch (field.Name) {
                    case "name" :
                        nameIndex = field.ColumnIndex;
                        break;
                    case "type" : 
                        typeIndex = field.ColumnIndex;
                        break;
                    case "source" : 
                        sourceIndex = field.ColumnIndex;
                        break;
                    case "caption" :
                        captionIndex = field.ColumnIndex;
                        break;
                    case "value" :
                        valueIndex = field.ColumnIndex;
                        break;
                    case "optional" :
                        optionalIndex = field.ColumnIndex;
                        break;
                    case "readonly" :
                        readonlyIndex = field.ColumnIndex;
                        break;
                    default :
                        field.Reserved = false;
                        break;
                }
            }
            
            compositeFields = new RequestParameterCollection();
            
            if (nameIndex != -1 && valueIndex != -1) {

                // Add specific fields
                AddSpecificCompositeFields(compositeFields);

                IDbConnection dbConnection = context.GetDbConnection();
                IDataReader reader = context.GetQueryResult(query, dbConnection);
                while (reader.Read()) {
                    name = context.GetValue(reader, nameIndex);
                    type = (typeIndex == -1 ? null : context.GetValue(reader, typeIndex));
                    source = (sourceIndex == -1 ? null : context.GetValue(reader, sourceIndex));
                    caption = (captionIndex == -1 ? name : context.GetValue(reader, captionIndex));
                    value = context.GetValue(reader, valueIndex);
                    optional = (optionalIndex == -1 ? type == "debug" : context.GetBooleanValue(reader, optionalIndex));  
                    readOnly = readonlyIndex != -1 && context.GetBooleanValue(reader, readonlyIndex);  

                    RequestParameter param = compositeFields.GetParameter(context, null, name, type, caption, value);
                    param.Optional = optional;
                    param.ReadOnly = readOnly;
                    if (source != null && param.ValueSet == null) {
                        param.ValueSet = new LookupValueSet(context, source, this is Configuration);
                        param.Reference = true;
                        param.Type = "select";
                        param.Source = source;
                        if (optional) param.NullCaption = "[no selection]";
                    }
                    foreach (FieldExpression field in Fields) {
                        if ((field.Flags & FieldFlags.Item) == 0 || field.Reserved) continue;
                        SingleReferenceField field1 = field as SingleReferenceField;
                        if (field1 != null && field1.ReferenceEntity != null) {
                            foreach (FieldExpression field2 in field1.ReferenceEntity.Fields) {
                                if ((field2.Flags & FieldFlags.Item) == 0 || field2.Reserved) continue;
                                param.AddAdditionalAttribute(field2.Name, context.GetValue(reader, field2.ColumnIndex));
                            }
                        } else {
                            param.AddAdditionalAttribute(field.Name, context.GetValue(reader, field.ColumnIndex));
                        }
                    }
                }
                context.CloseQueryResult(reader, dbConnection);
            }

        }

        //---------------------------------------------------------------------------------------------------------------------

        private void GetItem(string condition, int level) {
            if (OnLoad()) return;

            if (condition == null) return;

            string select = "t." + IdField;
            string join = Join;
            int aliasIndex = 0;
            int columnIndex = 1;
            ExpandQuery(ref join, ref condition, ref aliasIndex);
            ExpandSelectClause(ref columnIndex, ref select, "t", FieldFlags.Item);
            string sql = "SELECT " + select + " FROM " + join + (condition == null ? "" : " WHERE " + condition) + ";";
            //context.AddInfo(sql);
            
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            int count = 0;
            
            // Get result
            //try {
            if (reader.Read()) {
                Id = reader.GetInt32(0);
                
                foreach (FieldExpression field in Fields) {
                    if ((field.Flags & FieldFlags.Item) != FieldFlags.Item) continue;
                    
                    // Process fields according to type:
                    // * multiple entity fields: select entities with subordinate query (1)
                    // * multiple reference fields: select values with subordinate query (2)
                    // * single reference fields: select reference ID and optionally value (3)
                    // * single value field field (4)

                    if (field is IMultipleField) {
                        if ((field.Flags & FieldFlags.Hidden) != 0) continue;
                        //if (id == 0) continue;
                        
                        if (!(field is MultipleEntityField)) { // (1)
                        }

                    } else if (field is IReferenceField) { // (3)
                        if ((field.Flags & FieldFlags.Hidden) != 0) continue;

                        IReferenceField field1 = field as IReferenceField;
                        
                        if ((field.Flags & FieldFlags.Reduced) == 0) field.Value = context.GetValue(reader, field.ColumnIndex);
                        if (field1.ReferenceValueExpr != null) {
                            field.ValueCaption = context.GetValue(reader, field.ColumnIndex + 1);
                            //if ((field.Flags & FieldFlags.Custom) != 0) field.ValueCaption = GetSpecificReadValue(field, field.ValueCaption); // !!! what to do about the value caption?
                        }

                    } else { // (4)
                        // bool values can be 1 and 0, transform them to true or false
                        switch (field.Type) {
                            case "bool" : 
                                field.Value = context.GetBooleanValue(reader, field.ColumnIndex).ToString().ToLower();
                                break;
                            case "date" : 
                            case "datetime" :
                            case "startdate" :
                            case "enddate" :
                                field.Value = context.GetDateTimeValue(reader, field.ColumnIndex, field.Type == "date" ? @"yyyy\-MM\-dd" : @"yyyy\-MM\-dd\THH\:mm\:ss\Z");
                                break;
                            case "password" :
                                field.Value = context.GetValue(reader, field.ColumnIndex);
                                continue;
                            default:
                                field.Value = context.GetValue(reader, field.ColumnIndex);
                                break;
                        }

                        // !!! also elsewhere boolean "1" -> "false"
                        
                        //if ((field.Flags & FieldFlags.Hidden) != 0) continue; // this is done after getting the value, which may be used internally in the subclass
                    }
                }
            }
            //} catch (Exception e) {context.AddError(e.Message + " " + e.StackTrace);}
            context.CloseQueryResult(reader, dbConnection);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected virtual int WriteItemContents(int id, string query, FieldFlags flags, int level) {
            if (query == null) {
                if (id == 0) return 0;

                string select = "t." + IdField;
                string join = Join;
                string condition = "t." + IdField + "=" + id;
                int aliasIndex = 0;
                int columnIndex = 1;
                ExpandQuery(ref join, ref condition, ref aliasIndex);
                ExpandSelectClause(ref columnIndex, ref select, "t", FieldFlags.Item);
                query = "SELECT " + select + " FROM " + join + (condition == null ? "" : " WHERE " + condition) + ";";
            }
            
            if (xmlWriter == null) xmlWriter = context.XmlWriter;
            
            //context.AddInfo(query);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(query, dbConnection);
            int count = 0, itemId = 0, lastItemId = 0;
            //string value;
            bool removeDuplicates = (InnerSorting != null);
            
            // Write result
            while (reader.Read()) {
                itemId = reader.GetInt32(0);
                if (removeDuplicates && itemId == lastItemId) continue;

                count++;
                
                xmlWriter.WriteStartElement("item");
                if (xmlWriter.WriteState == WriteState.Element) {
                    if (ShowItemIds || level != 0) xmlWriter.WriteAttributeString((level == 0 ? "id" : "value"), itemId.ToString());
                    if (ShowItemLinks) xmlWriter.WriteAttributeString("link", String.Format(context.UsesUrlRewriting ? "{0}/{1}" : "{0}?id={1}", ItemBaseUrl, itemId));

                    foreach (FieldExpression field in Fields) {
                        if ((field.Flags & flags) == 0 || (field.Flags & FieldFlags.Attribute) == 0) continue;

                        switch (field.Type) {
                            case "bool" :
                                field.Value = context.GetBooleanValue(reader, field.ColumnIndex).ToString().ToLower();
                                break;
                            case "date" : 
                            case "datetime" :
                            case "startdate" :
                            case "enddate" :
                                field.Value = context.GetDateTimeValue(reader, field.ColumnIndex, field.Type == "date" ? @"yyyy\-MM\-dd" : @"yyyy\-MM\-dd\THH\:mm\:ss\Z");
                                break;
                            case "password" :
                                continue;
                            default:
                                field.Value = context.GetValue(reader, field.ColumnIndex);
                                break;
                        }
                        if ((field.Flags & FieldFlags.Hidden) != 0) continue; // this is done after getting the value, which may be used internally in the derived class
                            
                        //if (value == null) value = reader.GetValue(index).ToString();
                        xmlWriter.WriteAttributeString(field.Name, field.Value);
                    }
                }
                //if (level == 0) xmlWriter.WriteAttributeString("link", context.ScriptName + "?id=" + reader.GetValue(0).ToString());
                foreach (FieldExpression field in Fields) {
                    if ((field.Flags & flags) == 0 || (field.Flags & FieldFlags.Attribute) != 0) continue;
                    
                    // Process fields according to type:
                    // * multiple entity fields: select entities with subordinate query (1)
                    // * multiple reference fields: select values with subordinate query (2)
                    // * single reference fields: select reference ID and optionally value (3)
                    // * single value field field (4)

                    if (field is IMultipleField) {
                        if ((field.Flags & FieldFlags.Hidden) != 0) continue;
                        if (id == 0) continue;
                        
                        if (field is MultipleEntityField) { // (1)
                            MultipleEntityField field1 = field as MultipleEntityField;
                            xmlWriter.WriteStartElement(field.Name);

                            context.DynamicDbConnections = true;

                            if (field1.ForeignEntity.FilterCondition == null) field1.ForeignEntity.FilterCondition = ""; else field1.ForeignEntity.FilterCondition += " AND ";
                            field1.ForeignEntity.FilterCondition += "t." + field1.MultipleLinkField + "=" + id + (field1.Condition == null ? "" : " AND " + field1.Condition);
                            
                            field1.ForeignEntity.WriteItemList(level + 1);
                            context.DynamicDbConnections = context.DynamicDbConnectionsGlobal;
                            xmlWriter.WriteEndElement();
                            
                        } else { // (2)
                            MultipleReferenceField field1 = field as MultipleReferenceField;
                            
                            string sql;
                            if (field1.MultipleTable == null) {
                                sql = "SELECT DISTINCT(t.{2}), {4} FROM {1} AS t WHERE t.{6}={0}{7};";
                            } else {
                                sql = "SELECT DISTINCT(t.{2}), {4} FROM {1} AS t INNER JOIN {5} AS m ON t.{2}=m.{3} WHERE m.{6}={0}{7};";
                            }
                            
                            string condition = (field1.Condition == null ? String.Empty : " AND " + field1.Condition);
                            
                            if (field1.MultipleEntity != null) {
                                foreach (FieldExpression multipleField in field1.MultipleEntity.Fields) {
                                    FixedValueField fixedField = multipleField as FixedValueField;
                                    if (fixedField == null) continue;
                                    //context.AddInfo(" AND m." + fixedField.Field + "=" + StringUtils.EscapeSql(fixedField.Value)); 
                                }
                            }

                            sql = String.Format(sql,
                                    id,
                                    field1.ReferenceTable,
                                    field1.ReferenceIdField,
                                    field1.ReferenceLinkField,
                                    field1.ReferenceValueExpr.Replace("@.", "t."),
                                    field1.MultipleTable,
                                    field1.MultipleLinkField,
                                    condition
                            );

                            //context.ReturnError(sql);
                            context.DynamicDbConnections = true;
                            IDbConnection dbConnection2 = context.GetNewDbConnection();
                            IDataReader reader2 = context.GetQueryResult(sql, dbConnection2);
                            xmlWriter.WriteStartElement(field.Name);
                            while (reader2.Read()) {
                                xmlWriter.WriteStartElement("element");
                                xmlWriter.WriteAttributeString("value", reader2.GetValue(0).ToString());
                                xmlWriter.WriteString(reader2.GetValue(1).ToString());
                                xmlWriter.WriteEndElement();
                            }
                            xmlWriter.WriteEndElement();
                            reader2.Close();
                            dbConnection2.Close();
                            context.DynamicDbConnections = context.DynamicDbConnectionsGlobal;
                        }

                    } else if (field is IReferenceField) { // (3)
                        if ((field.Flags & FieldFlags.Hidden) != 0) continue;

                        IReferenceField field1 = field as IReferenceField;
                        
                        xmlWriter.WriteStartElement(field.Name);
                        if ((field.Flags & FieldFlags.Reduced) == 0) xmlWriter.WriteAttributeString("value", reader.GetValue(field.ColumnIndex).ToString());
                        if (field1.ReferenceValueExpr != null) {
                            field.Value = reader.GetValue(field.ColumnIndex + 1).ToString();
                            xmlWriter.WriteString(field.Value);
                        }
                        xmlWriter.WriteEndElement();

                    } else { // (4)
                        switch (field.Type) {
                            case "bool" :
                                field.Value = context.GetBooleanValue(reader, field.ColumnIndex).ToString().ToLower();
                                break;
                            case "date" : 
                            case "datetime" :
                            case "startdate" :
                            case "enddate" :
                                field.Value = context.GetDateTimeValue(reader, field.ColumnIndex, field.Type == "date" ? @"yyyy\-MM\-dd" : @"yyyy\-MM\-dd\THH\:mm\:ss\Z");
                                break;
                            default:
                                field.Value = context.GetValue(reader, field.ColumnIndex);
                                break;
                        }
                        if ((field.Flags & FieldFlags.Hidden) != 0) continue;

                        xmlWriter.WriteStartElement(field.Name);
                        switch (field.Type) {
                            case "password" :
                                if (field.Value != null) xmlWriter.WriteAttributeString("stored", "true");
                                break;
                            case "link" :
                                xmlWriter.WriteAttributeString("link", field.Value);
                                break;
                            default :
                                xmlWriter.WriteString(field.Value);
                                break;
                        }
                        xmlWriter.WriteEndElement();
                    }
                }
                xmlWriter.WriteEndElement(); // </item>

                if (id != 0) break; // continue only if item list is requested
                
                if (removeDuplicates) lastItemId = itemId;
            }
            context.CloseQueryResult(reader, dbConnection);

            OnBeforeWrite();

            return count;
        }

        //---------------------------------------------------------------------------------------------------------------------

        private int WriteItemContents(string query) {
            if (OptionListElementName == null) OptionListElementName = "element";

            if (xmlWriter == null) xmlWriter = context.XmlWriter;
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(query, dbConnection);
            
            int count = 0;
            
            // Write result
            while (reader.Read()) {
                count++;
                foreach (FieldExpression field in Fields) {
                    if (!(field is SingleValueExpression) || (field.Flags & FieldFlags.List) == 0) continue;
                    switch (field.Type) {
                        case "bool" :
                            field.Value = context.GetBooleanValue(reader, field.ColumnIndex).ToString().ToLower();
                            break;
                        case "date" :
                        case "datetime" :
                        case "startdate" :
                        case "enddate" :
                            field.Value = context.GetDateTimeValue(reader, field.ColumnIndex, field.Type == "date" ? @"yyyy\-MM\-dd" : @"yyyy\-MM\-dd\THH\:mm\:ss\Z");
                            break;
                        case "password" : 
                            continue;
                        default:
                            field.Value = context.GetValue(reader, field.ColumnIndex);
                            break;
                    }
                }
                    
                xmlWriter.WriteStartElement(OptionListElementName);
                foreach (FieldExpression field in Fields) {
                    if (!(field is SingleValueExpression) || (field.Flags & (FieldFlags.List | FieldFlags.Hidden)) != FieldFlags.List) continue;

                    if (field.Name == "text") {
                        xmlWriter.WriteString(field.Value);
                        break;
                    } else {
                        xmlWriter.WriteAttributeString(field.Name, field.Value);
                    }
                }
                xmlWriter.WriteEndElement(); // </element>
            }
            context.CloseQueryResult(reader, dbConnection);

            OnBeforeWrite();

            return count;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Expands the query with a given join and conditional expressions for the the single item or an item list query.</summary>
        /*!
        /// <param name="join">reference to the SQL join expression (without the <i>FROM</i> keyword) </param>
        /// <param name="condition">reference to the SQL conditional expression (without the <i>WHERE</i> keyword)</param>
        /// <param name="aliasIndex">reference to the table alias index to be used for the entity</param>
        */
        protected void ExpandQuery(ref string join, ref string condition, ref int aliasIndex) {
            if (condition != null) condition = condition.Trim();

            if (inputSource == null) inputSource = new FilterInputSource(null);

            searchTermsValue = inputSource.GetValue(searchTermsParamName);
            if (searchTermsValue != null) searchTermsValue = StringUtils.EscapeSql("%" + searchTermsValue + "%");

            // Go recursively through fields with search extension and add conditions and joins
            MarkTables(ref aliasIndex);

            string searchTermsCondition = null;
            ExpandCondition(ref join, ref condition, ref searchTermsCondition, "t");

            if (searchTermsCondition != null) {
                if (condition == null) condition = searchTermsCondition; else condition += " AND (" + searchTermsCondition  + ")";
            }

            if (ExcludeIds != null && ExcludeIds.Length != 0) {
                string excludeCondition = "";
                for (int i = 0; i < ExcludeIds.Length; i++) {
                    excludeCondition += (i == 0 ? "" : ",") + ExcludeIds[i];
                }
                if (condition == null) condition = ""; else condition += " AND ";
                condition += "t." + IdField + (ExcludeIds.Length == 1 ? "!=" : " NOT IN (") + excludeCondition + (ExcludeIds.Length == 1 ? "" : ")");
            }

            foreach (FieldExpression field in Fields) {
                if (field is FixedValueField) {
                    if (condition == null) condition = ""; else condition += " AND ";
                    string alias = (field.ExtensionTable == null ? "t" : field.ExtensionTable.Alias); 
                    condition += alias + "." + field.Field + (field.Value == null ? " IS NULL" : "=" + StringUtils.EscapeSql(field.Value)); 
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Expands the given field clause of the query for the single item or an item list query.</summary>
        /*!
    /// <param name="columnIndex">reference to the variable holding the index of the next column in the query result set </param>
    /// <param name="select">reference to the SQL selection fields expression (without the <i>SELECT</i> keyword) </param>
    /// <param name="currentAlias">table alias to be used for the entity</param>
    /// <param name="flags">the combination flags of the fields that must be matched (e.g. <i>List</i> or <i>Item</i>)</param>
    */
        protected void ExpandSelectClause(ref int columnIndex, ref string select, string currentAlias, FieldFlags flags) {
            bool hasFields = (select != "");
            foreach (FieldExpression field in Fields) {
                string alias = (field.ExtensionTable == null ? currentAlias : field.ExtensionTable.Alias);
                if ((field.Flags & flags) != flags) {
                    field.ColumnIndex = -1;
                    continue;
                }

                if (field is IMultipleField) { // (1)
                    field.ColumnIndex = -1;
                } else {
                    IReferenceField field1;
                    if ((field1 = field as IReferenceField) == null) { // (2)
                        select += (hasFields ? ", " : "") + field.Expression.Replace("t.", alias + ".") + " AS _" + field.Name;
                        field.ColumnIndex = columnIndex++;
                        hasFields = true;
                    } else { // (3)
                        select += (hasFields ? ", " : "") + alias + "." + field1.ReferenceLinkField + " AS _" + field.Name;
                        field.ColumnIndex = columnIndex++;
                        if (field1.ReferenceValueExpr != null) {
                            string expr = field1.ReferenceValueExpr.Replace("@.", field1.ReferenceTableAlias + ".");
                            if ((field.Flags & FieldFlags.Optional) != 0) expr = "CASE WHEN " + alias + "." + field1.ReferenceLinkField + " IS NULL THEN " + StringUtils.EscapeSql(field1.NullCaption) + " ELSE " + expr + " END";
                            select += ", " + expr;
                            columnIndex++;

                            if (field1.ReferenceEntity != null) field1.ReferenceEntity.ExpandSelectClause(ref columnIndex, ref select, field1.ReferenceTableAlias, flags);
                            //if ((field.Flags & FieldFlags.Optional) != 0 && field1.NullCaption == null) field1.NullCaption = "[no selection]";
                        }
                        hasFields = true;
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Expands the given aggregation and sorting clauses for the item list query.</summary>
        /*!
/// <param name="countFields">reference to the SQL selection fields expression for the counting query (without the <i>SELECT</i> keyword)</param>
/// <param name="aggregation">reference to the SQL aggregation fields expression (without the <i>GROUP BY</i> keyword)</param>
/// <param name="sorting">reference to the SQL sorting fields expression (without the <i>ORDER BY</i> keyword)</param>
*/
        protected void ExpandSortingClause(ref string countFields, ref string aggregation, ref string sorting) {
            bool hasFields = false;
            // Order by pre-defined group fields
            if (!hasFields) {
                foreach (FieldExpression field in Fields) {
                    if ((field.Flags & FieldFlags.Group) == 0) continue;
                    if (hasFields) countFields += ", "; else countFields = String.Empty;
                    if (hasFields) aggregation += ", "; else aggregation = String.Empty;
                    string expression = field.Expression;
                    IReferenceField field1;
                    if ((field1 = field as IReferenceField) != null) expression = expression.Replace("@.", field1.ReferenceTableAlias + ".");
                    countFields += expression + ((field.Flags & FieldFlags.Sort) == FieldFlags.SortDesc ? " DESC" : String.Empty);
                    aggregation += ((field.Flags & FieldFlags.List) == 0 ? field.Expression : "_" + field.Name) + ((field.Flags & FieldFlags.Sort) == FieldFlags.SortDesc ? " DESC" : String.Empty);
                    hasFields = true;
                }
            }
            if (hasFields) countFields += ", "; else countFields = "";
            countFields += "t." + IdField;

            sortSelect = null;
            sortSelectCount = 0;
            sorts = new SortInfo[Fields.Count]; 
            hasFields = (sorting != null);

            // Custom sorting overrides the sorting defined in the data structure
            if (!hasFields) {
                // Order by requested fields
                if (true) { // (sortField != null) !!! do this only if order may be defined in request
                    string currentAlias = "t";
                    foreach (FieldExpression field in Fields) {
                        string alias = (field.ExtensionTable == null ? currentAlias : field.ExtensionTable.Alias);
                        if (!(field is SortField)) continue;

                        string paramValue = inputSource.GetValue(field.Name);
                        if (paramValue == null) continue;

                        MatchCollection matches = Regex.Matches(paramValue, "(-)? *([^, ]+)");
                        for (int j = 0; j < matches.Count; j++) {
                            string obf = matches[j].Groups[2].Value;
                            FieldExpression fe = GetField(obf, FieldFlags.Both);
                            if (fe == null) continue;
                            if (hasFields) sorting += ", "; else sorting = "";
                            if (hasFields) sortSelect += ", "; else sortSelect = "";
                            if (fe is IReferenceField && !(fe is IMultipleField)) {
                                IReferenceField field1 = fe as IReferenceField;
                                if (field1.ReferenceValueExpr != null) obf = field1.ReferenceValueExpr.Replace("@.", field1.ReferenceTableAlias + ".");
                            } else if (fe is SingleValueField) {
                                SingleValueField field1 = fe as SingleValueField;
                                obf = field1.Expression.Replace("t.", alias + ".");
                            } else {
                                obf = "_" + obf;
                            }
                            sorting += obf + (matches[j].Groups[1].Success ? " DESC" : "");
                            sortSelect += obf + " AS _" + fe.Name;
                            sorts[sortSelectCount].Descending = matches[j].Groups[1].Success;
                            sorts[sortSelectCount++].Field = fe;
                            hasFields = true;
                        }
                        break;
                    }
                }
            }

            // Order by pre-defined order fields
            if (!hasFields) {
                foreach (FieldExpression field in Fields) {
                    if ((field.Flags & FieldFlags.Sort) == 0) continue;

                    if (hasFields) sorting += ", "; else sorting = String.Empty;
                    if (hasFields) sortSelect += ", "; else sortSelect = String.Empty;
                    sorting += ((field.Flags & FieldFlags.List) == 0 ? field.Expression : "_" + field.Name) + ((field.Flags & FieldFlags.Sort) == FieldFlags.SortDesc ? " DESC" : String.Empty);
                    sortSelect += field.Expression + " AS _" + field.Name;

                    sorts[sortSelectCount].Descending = ((field.Flags & FieldFlags.Sort) == FieldFlags.SortDesc);
                    sorts[sortSelectCount++].Field = field;
                    hasFields = true;
                }
            }

            // Additionally order by the key field, to have deterministic ordering
            if (hasFields) sorting += ", "; else sorting = String.Empty;
            if (hasFields) sortSelect += ", "; else sortSelect = String.Empty;
            sorting += "t." + IdField + (KeyDescending ? " DESC" : String.Empty);
            sortSelect += "t." + IdField + " AS _id";
            sorts[sortSelectCount++].Descending = KeyDescending;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Expands the given join and conditional expressions for the single item or the item list query.</summary>
        /*!
/// <param name="join">reference to the SQL join expression (without the <i>FROM</i> keyword) </param>
/// <param name="condition">reference to the SQL conditional expression (without the <i>WHERE</i> keyword)</param>
/// <param name="searchTermsCondition">reference to the SQL conditional expression regarding the search terms extension (without the <i>WHERE</i> keyword)</param>
/// <param name="currentAlias">table alias to be used for the entity</param>
*/
        protected void ExpandCondition(ref string join, ref string condition, ref string searchTermsCondition, string currentAlias) {
            foreach (FieldExpression field in Fields) {
                string alias = (field.ExtensionTable == null ? currentAlias : field.ExtensionTable.Alias);
                string paramValue = null;
                if (field.SearchExtension != null) paramValue = inputSource.GetValue(field.Name);

                string paramJoin = null;

                if ((field.Flags & FieldFlags.Custom) != 0) {
                    paramJoin = GetSpecificJoin(field, alias, paramValue);
                    if (paramJoin != null) join += (join == "" ? "" : " ") + paramJoin;
                }

                if (field is SingleReferenceField) {
                    SingleReferenceField field1 = field as SingleReferenceField;
                    if (field1.ReferenceTableAlias != null) {
                        join += (join == "" ? "" : " ") + ((field.Flags & FieldFlags.Optional) != 0 ? "LEFT" : "INNER") + " JOIN " + field1.ReferenceTable + " AS " + field1.ReferenceTableAlias + " ON " + alias + "." + field1.ReferenceLinkField + "=" + field1.ReferenceTableAlias + "." + field1.ReferenceIdField;
                        if (field1.ReferenceEntity != null) field1.ReferenceEntity.ExpandCondition(ref join, ref condition, ref searchTermsCondition, field1.ReferenceTableAlias);
                    }
                } else if (field is MultipleReferenceField) {
                    MultipleReferenceField field1 = field as MultipleReferenceField;
                    if (field1.MultipleTableAlias != null) {
                        join += (join == "" ? "" : " ") + "LEFT JOIN " + field1.MultipleTable + " AS " + field1.MultipleTableAlias + " ON " + alias + "." + IdField + "=" + field1.MultipleTableAlias + "." + field1.MultipleLinkField;
                    }
                } else if (field is MultipleEntityField) {
                    MultipleEntityField field1 = field as MultipleEntityField;
                    if (field1.MultipleTableAlias != null) {
                        join += (join == "" ? "" : " ") + "LEFT JOIN " + field1.MultipleTable + " AS " + field1.MultipleTableAlias + " ON " + alias + "." + IdField + "=" + field1.MultipleTableAlias + "." + field1.MultipleLinkField;
                        field1.ForeignEntity.ExpandCondition(ref join, ref condition, ref searchTermsCondition, field1.MultipleTableAlias);
                    }
                }
                if (Id != 0) continue; // !!! DO THIS BETTER!

                string paramCondition = null;
                if ((field.Flags & FieldFlags.Custom) != 0) paramCondition = GetSpecificCondition(field, alias, paramValue);
                if (paramValue == null && paramCondition == null && (field.Flags & FieldFlags.Searchable) == 0) continue;

                if (field is SingleValueExpression) {

                    if (paramValue != null && paramCondition == null) {
                        switch (field.Type) {
                            case "int" :
                                case "float" :
                                paramCondition = SearchExtension.GetNumericConditionSql(field.Expression, paramValue);
                                break;
                                case "date" :
                                case "datetime" :
                                TimeZoneInfo timeZoneInfo = (UseUserTimeZone ? context.UserTimeZone : TimeZoneInfo.Utc);
                                paramCondition = SearchExtension.GetDateTimeConditionSql(field.Expression, paramValue, timeZoneInfo);
                                break;
                                default :
                                paramValue = StringUtils.EscapeSql(paramValue);
                                paramCondition = field.Expression.Replace("t.", alias + ".") + (paramValue.Contains("*") ? " LIKE " + paramValue.Replace('*', '%') : "=" + paramValue);
                                break;
                        }
                    }
                    if (paramCondition != null) {
                        if (condition == null) condition = ""; else condition += " AND ";
                        condition += paramCondition;
                    }

                    if ((field.Flags & FieldFlags.Searchable) != 0 && searchTermsValue != null) {
                        if (searchTermsCondition == null) searchTermsCondition = ""; else searchTermsCondition += " OR ";
                        searchTermsCondition += field.Expression.Replace("t.", alias + ".") + " LIKE " + searchTermsValue;
                    }

                } else if (field is SingleReferenceField) {
                    SingleReferenceField field1 = field as SingleReferenceField;


                    if (field1.ReferenceEntity == null) {
                        if (paramValue != null) {
                            if ((field.Flags & FieldFlags.TextSearch) == 0) {
                                paramCondition = "";
                                if ((field.Flags & FieldFlags.Optional) != 0) {
                                    if (condition == null) condition = ""; else condition += " AND ";
                                    condition += field1.ReferenceLinkField + " IS NULL";

                                } else {
                                    string[] ids = paramValue.Split(',');
                                    int intValue, count = 0;
                                    for (int j = 0; j < ids.Length; j++) {
                                        if (!Int32.TryParse(ids[j], out intValue) || intValue == 0) continue;
                                        paramCondition += (count == 0 ? "" : ", ") + intValue;
                                        count++;
                                    }
                                    if (paramCondition != "") {
                                        if (condition == null) condition = ""; else condition += " AND ";
                                        condition += field1.ReferenceLinkField + (count == 1 ? "=" + paramCondition : " IN (" + paramCondition + ")");
                                    }
                                }

                            } else if (field1.ReferenceTableAlias != null) {
                                paramValue = inputSource.GetValue(field.Name + "Text");
                                if (paramValue == null) continue;

                                if (condition == null) condition = ""; else condition += " AND ";
                                condition += field1.ReferenceValueExpr.Replace("@.", field1.ReferenceTableAlias + ".") + "=" + StringUtils.EscapeSql(paramValue);
                            }
                        }

                        if ((field.Flags & FieldFlags.Searchable) != 0 && searchTermsValue != null) {
                            if (searchTermsCondition == null) searchTermsCondition = ""; else searchTermsCondition += " OR ";
                            searchTermsCondition += field1.ReferenceValueExpr.Replace("@.", field1.ReferenceTableAlias + ".") + " LIKE " + searchTermsValue;
                        }
                    } else {
                        if (paramValue != null) {
                            paramCondition = "";
                            string[] ids = paramValue.Split(',');
                            int intValue, count = 0;
                            for (int j = 0; j < ids.Length; j++) {
                                if (!Int32.TryParse(ids[j], out intValue) || intValue == 0) continue;
                                paramCondition += (count == 0 ? "" : ", ") + intValue;
                                count++;
                            }
                            if (paramCondition != "") {
                                if (condition == null) condition = ""; else condition += " AND ";
                                condition += field1.ReferenceLinkField + (count == 1 ? "=" + paramCondition : " IN (" + paramCondition + ")");
                            }
                        }

                    }

                } else if (field is MultipleReferenceField) {
                    MultipleReferenceField field1 = field as MultipleReferenceField;

                    if (paramValue != null) {
                        paramCondition = "";
                        string[] ids = paramValue.Split(',');
                        int intValue, count = 0;
                        for (int j = 0; j < ids.Length; j++) {
                            if (!Int32.TryParse(ids[j], out intValue) || intValue == 0) continue;
                            paramCondition += (count == 0 ? "" : ", ") + intValue;
                            count++;
                        }
                        if (paramCondition != "") {
                            if (condition == null) condition = ""; else condition += " AND ";
                            condition += field1.MultipleTableAlias + "." + field1.ReferenceLinkField + (count == 1 ? "=" + paramCondition : " IN (" + paramCondition + ")");
                        }
                    }
                }

            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual void AddSpecificCompositeFields(RequestParameterCollection fields) {
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>When overridden in a derived class, returns an additional join clause depending on the field and its value.</summary>
        /*!
/// <param name="field">the field for which a custom processing is implemented in the derived class</param>
/// <param name="tableAlias">the table alias that applies to the field</param>
/// <param name="value">the field value read from the database</param>
    \returns the additional SQL join expression
*/
        protected virtual string GetSpecificJoin(FieldExpression field, string tableAlias, string value) {
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>When overridden in a derived class, returns an additional conditional expression depending on the field and its value.</summary>
        /*!
/// <param name="field">the field for which a custom processing is implemented in the derived class</param>
/// <param name="tableAlias">the table alias that applies to the field</param>
/// <param name="value">the field value read from the database</param>
    \returns the additional SQL conditional expression
*/
        protected virtual string GetSpecificCondition(FieldExpression field, string tableAlias, string value) {
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>When overridden in a derived class, changes values of custom fields after reading from the database.</summary>
        /*!
/// <param name="field">the field for which a custom processing is implemented in the derived class</param>
/// <param name="value">the field value read from the database</param>
*/
        //---------------------------------------------------------------------------------------------------------------------

        protected virtual bool OnLoad() {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual void OnBeforeDefine() {}

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual void OnBeforeWrite() {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>When overridden in a derived class, prepares the values of custom fields to be written into the database.</summary>
        /*!
/// <param name="field">the field for which a custom processing is implemented in the derived class</param>
/// <returns><i>true</i> if the value is supposed to be written, <i>false</i> if it is to be ignored</returns>
*/
        protected virtual void OnAfterRead() {}

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual Entity OnStore() {
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Marks the tables that are needed to perform an item list query and assigns aliases to them.</summary>
        /*!
    If the data structure of the entity references other tables, these are included in the query if there are search parameters regarding these tables.
    The method is called recursively for sub-entities of fields.
/// <param name="aliasIndex">reference to the table alias index to be used for the entity</param>
/// <returns><i>true</i> if the table is included</returns>
*/
        protected bool MarkTables(ref int aliasIndex) {
            bool hasFields = false;
            foreach (FieldExpression field in Fields) {
                if (field is MultipleEntityField) {
                    MultipleEntityField field1 = field as MultipleEntityField;
                    field1.MultipleTableAlias = "t" + (++aliasIndex).ToString();
                    if (field1.ForeignEntity.MarkTables(ref aliasIndex)) {
                        hasFields = true;
                    } else {
                        field1.MultipleTableAlias = null;
                        aliasIndex--;
                    }
                    continue;
                }

                // A searchable (searchTerms) parameter is only considered if the corresponding search parameter (q) has a value;
                // a search extension parameter is only considered if the corresponding search parameter has a value
                if (!(field is SingleReferenceField) && ((field.Flags & FieldFlags.Searchable) == 0 || searchTermsValue == null) && (field.SearchExtension == null || inputSource.GetValue(field.Name) == null)) continue;
                hasFields = true;

                if (field is SingleReferenceField) {
                    SingleReferenceField field1 = field as SingleReferenceField;
                    if (field1.ReferenceEntity == null) {
                        if ((field.Flags & FieldFlags.TextSearch) != 0 || field1.ReferenceValueExpr != null) field1.ReferenceTableAlias = "t" + (++aliasIndex).ToString();
                    } else {
                        field1.ReferenceTableAlias = "t" + (++aliasIndex).ToString();
                        if (field1.ReferenceEntity.MarkTables(ref aliasIndex) || field1.ReferenceValueExpr != null) {
                            hasFields = true;
                        } else {
                            field1.ReferenceTableAlias = null;
                            aliasIndex--;
                        }
                    }
                } else if (field is MultipleReferenceField) {
                    MultipleReferenceField field1 = field as MultipleReferenceField;
                    field1.MultipleTableAlias = "t" + (++aliasIndex).ToString();
                }
            }

            return hasFields;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Builds the SQL conditional clause for obtaining only the items of the requested item list page.</summary>
        /*!
/// <returns>the SQL conditional expression for the paging</returns>
*/
        protected string BuildPageCondition() {
            string result = null;

            int equalUpTo = -1;
            for (int i = 0; i < sortSelectCount; i++) {
                if (equalUpTo == i - 1 && sorts[i].PageFirstValue == sorts[i].PageLastValue) {
                    equalUpTo = i;
                    if (i == 0) result = String.Empty; else result += " AND ";
                    result += (i == sortSelectCount - 1 ? "t." + IdField : sorts[i].Field.Expression) + "=" + StringUtils.EscapeSql(sorts[i].PageFirstValue);
                } else break;
            }

            if (equalUpTo + 1 == sortSelectCount) return result;

            for (int i = equalUpTo + 1; i < sortSelectCount; i++) {
                string firstOperator = (sorts[i].Descending ? "<" : ">");
                string lastOperator = (sorts[i].Descending ? ">" : "<");
                string expression;
                if (i == sortSelectCount - 1) {
                    expression = "t." + IdField;
                    firstOperator += "=";
                    lastOperator += "=";
                } else {
                    expression = sorts[i].Field.Expression;
                }
                if (i != 0) {
                    result += " OR ";
                    for (int j = 0; j < i; j++) result += (j == 0 ? "" : " AND ") + sorts[j].Field.Expression + "=" + StringUtils.EscapeSql(sorts[j].PageFirstValue);
                    result += " AND ";
                }
                result += expression + firstOperator + StringUtils.EscapeSql(sorts[i].PageFirstValue);
                if (i != 0) {
                    result += " OR ";
                    for (int j = 0; j < i; j++) result += (j == 0 ? "" : " AND ") + sorts[j].Field.Expression + "=" + StringUtils.EscapeSql(sorts[j].PageLastValue);
                }
                result += " AND ";// + i + " " + sorts.Length + " " + (sorts[i].Field == null ? "NULL" : sorts[i].Field.Name);
                result += expression + lastOperator + StringUtils.EscapeSql(sorts[i].PageLastValue);
            }

            result = "(" + result + ")";

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>When overridden in a derived class, performs an additional operation on the item that was just processed.</summary>
        /// <param name="type">the operation type that has been applied to the item</param>
        /// <param name="itemId">the ID of the item</param>
        protected virtual bool OnItemProcessed(OperationType type, int itemId) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>When overridden in a derived class, performs an additional operation on the item if it could not be processed.</summary>
        /// <param name="type">the operation type that has been applied to the item</param>
        /// <param name="itemId">the ID of the item</param>
        protected virtual bool OnItemNotProcessed(OperationType type, int itemId) {
            return false;
        }

        /*        public void GetOperation(OperationType @default) {
    switch (context.GetParamValue(OperationParameterName)) {
        case "create" :
            operationType = OperationType.Create;
            break;
        case "modify" :
            operationType = OperationType.Modify;
            break;
        case "delete" :
            operationType = OperationType.Delete;
            break;
        default :
            operationType = @default;
            break;
    }
}*/

        //---------------------------------------------------------------------------------------------------------------------

        protected int GetExtensionTypeFromRequest() {
            int result = 0;
            Int32.TryParse(context.GetParamValue("_type"), out result);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Obtains the ID to be used for the subsequent single item processing.</summary>
        public void GetIdFromRequest() {
            int id = 0;
            Int32.TryParse(context.GetParamValue("id"), out id);
            this.Id = id;
            this.ItemUrl = String.Format(context.UsesUrlRewriting ? "{0}" : "{0}{1}", ItemBaseUrl, Id == 0 ? String.Empty : "?id=" + Id);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Obtains the list of IDs to be used for the subsequent multiple item processing.</summary>
        /*!
/// <param name="paramName">the name of the request parameter containing the IDs (separated by commas)</param>
*/
        public void GetIdsFromRequest(string paramName) {
            string[] idsStr = StringUtils.Split(context.GetParamValue(paramName), ',');
            Ids = new int[idsStr.Length];
            for (int i = 0; i < Ids.Length; i++) Int32.TryParse(idsStr[i], out Ids[i]);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Obtains the field values for a create or modify request of a single item.</summary>
        /*!
/// <param name="owner">the MultipleEntityField containing the fields of the item (null for top level)</param>
/// <param name="prefix">the prefix preceding the parameter names (the prefix does not not contain the <i>:</i>, which is present in parameter names with prefix)</param>
/// <returns><i>true</i> if the obtained values are correct, <i>false</i> otherwise</returns>
    
*/
        protected bool GetValuesFromRequest(MultipleEntityField owner, string prefix) {
            bool result = true;

            if (CompositeItem) {
                return false;

            } else {
                if (owner == null) {
                    AssignValueSets();
                }

                if (prefix == null) prefix = String.Empty; else if (prefix != String.Empty) prefix += ":";

                foreach (FieldExpression field in Fields) {
                    if ((field.Flags & FieldFlags.Item) != FieldFlags.Item) continue;

                    bool custom = ((field.Flags & FieldFlags.Custom) != 0);

                    if (owner != null) {
                        if (field is FixedValueField) continue;
                        string value = context.GetParamValue(prefix + field.Name);
                        char valueSeparator = (field is IMultipleField ? (field as IMultipleField).ValueSeparator : owner.ValueSeparator);
                        field.Value = value;
                        field.Values = (value == null ? new string[0] : value.Split(valueSeparator));
                        context.AddDebug(3, "O=N " + prefix + field.Name + " = " + value);
                        CheckField(field);

                    } else if (field is IMultipleField) {
                        string value = context.GetParamValue(prefix + field.Name);
                        char valueSeparator = (field is IMultipleField ? (field as IMultipleField).ValueSeparator : owner.ValueSeparator);
                        field.Values = (value == null ? new string[0] : value.Split(valueSeparator));
                        if (!(field is MultipleEntityField)) CheckField(field);

                    } else if (field is SingleValueField || field is SingleReferenceField) { // !!! maybe invent a property for this
                        field.Value = context.GetParamValue(prefix + field.Name);

                        // Deal with custom fields
                        if (custom) {
                            field.SqlValue = field.ToSqlString();
                        }
                        SingleReferenceField field1;
                        if ((field.Flags & FieldFlags.ReadOnly) != 0 && field.Value != null && (field1 = field as SingleReferenceField) != null && field1.ReferenceValueExpr != null) {
                            field.Type = "string";
                        }
                        CheckField(field);

                    } else if (custom && field is SingleValueExpression) {
                        field.Value = context.GetParamValue(prefix + field.Name);
                    }

                    if (Id != 0 && field is IMultipleField && !(field is IReferenceField)) {
                        IMultipleField field1 = field as IMultipleField;
                        string idsStr;
                        string[] ids;
                        idsStr = context.GetParamValue(prefix + field.Name + ":delete");
                        if (idsStr != null) {
                            ids = idsStr.Split(',');
                            field1.DeleteIds = new int[ids.Length];
                            for (int j = 0; j < ids.Length; j++) Int32.TryParse(ids[j], out field1.DeleteIds[j]);
                        }
                        idsStr = context.GetParamValue(prefix + field.Name + ":modify");
                        if (idsStr != null) {
                            ids = idsStr.Split(',');
                            field1.UpdateIds = new int[ids.Length];
                            for (int j = 0; j < ids.Length; j++) Int32.TryParse(ids[j], out field1.UpdateIds[j]);
                        }
                    }
                    if (owner == null && field is MultipleEntityField) {
                        MultipleEntityField field1 = field as MultipleEntityField;
                        field.Invalid = !field1.ForeignEntity.GetValuesFromRequest(field1, prefix + field.Name);
                    }

                    if (field.Invalid) result = false;
                }

                OnAfterRead();

            }

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the specified value is contained in the entity option list.</summary>
        /*!
/// <param name="value">the value to be checked</param>
/// <param name="defaultValue">contains the default value of the value set or <i>null</i> if there is no default value (output parameter)</param>
/// <param name="count">contains the number of elements in the value set (output parameter)</param>
/// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
/// <returns><i>true</i> if <i>value</i> is contained in the value set</returns>
    \sa For more information, see the documentation of IValueSet::CheckValue().
*/
        public virtual bool CheckValue(string value, out string defaultValue, out string selectedCaption) {
            bool result = true;
            defaultValue = null;
            selectedCaption = null;

            OptionList = true;

            int aliasIndex = 0;
            string join = Join;

            string condition = FilterCondition;
            ExpandQuery(ref join, ref condition, ref aliasIndex);

            string groupBy = CustomAggregation;
            string orderBy = CustomSorting;
            string countFields = null;
            ExpandSortingClause(ref countFields, ref groupBy, ref orderBy);

            string select = "t." + IdField;
            int columnIndex = 1;
            ExpandSelectClause(ref columnIndex, ref select, "t", FieldFlags.List);

            // Get index of column that has the flag for the default value(s)
            bool numericValue = false, uniqueCaption = false;
            int valueIndex = -1, defaultIndex = -1, captionIndex = -1;
            string valueExpr = null, captionExpr = null;
            foreach (FieldExpression field in Fields) {
                if ((field.Flags & FieldFlags.List) == 0) continue;
                field.Reserved = true;
                switch (field.Name) {
                    case "value" :
                        valueExpr = field.Expression;
                        valueIndex = field.ColumnIndex;
                        numericValue = ((field.Flags & FieldFlags.Id) != 0);
                        //context.AddInfo("valueIndex = " + field.ColumnIndex);
                        break;
                        case "default" :
                        defaultIndex = field.ColumnIndex;
                        //context.AddInfo("defaultIndex = " + field.ColumnIndex);
                        break;
                        case "caption" :
                        captionIndex = field.ColumnIndex;
                        uniqueCaption = ((field.Flags & FieldFlags.Unique) != 0);
                        if (uniqueCaption) captionExpr = field.Expression;
                        //context.AddInfo("captionIndex = " + field.ColumnIndex);
                        break;
                        default :
                        if ((field.Flags & FieldFlags.Hidden) == 0) field.Reserved = false;
                        break;
                }
            }

            if (valueIndex == -1) return true;

            if (value != null) {
                int d;
                // Note: for selecting the number of matching records, using COUNT(DISTINCT valueExpr) would be correct here, but is not needed for existence check.
                // More than one matching value can be caused by joins or aggregations; in absence of those it is an anomaly.
                result = (context.GetQueryIntegerValue(
                    String.Format("SELECT COUNT(*) FROM {0} WHERE {1}({2}{3});",
                              join,
                              (condition == null ? "" : " " + condition + " AND "),
                              (numericValue ? Int32.TryParse(value, out d) ? valueExpr + "=" + value.ToString() : "false" : valueExpr + "=" + StringUtils.EscapeSql(value)),
                              uniqueCaption ? " OR " + captionExpr + "=" + StringUtils.EscapeSql(value) : String.Empty
                              )
                    ) != 0);

            }


            //            if (!write) return result;

            string sql = String.Format("SELECT {0} FROM {1}{2}{3}{4};", 
                                       select,
                                       join,
                                       (condition == null ? "" : " WHERE " + condition),
                                       (groupBy == null ? "" : " GROUP BY " + groupBy),
                                       (orderBy == null ? "" : " ORDER BY " + orderBy)
                                       );

            Console.WriteLine(sql);

            //context.AddInfo(sql);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);

            int maxDefault = 0;
            while (reader.Read()) {
                string elementValue = context.GetValue(reader, valueIndex);
                int elementDefault = (defaultIndex == -1 ? 0 : context.GetIntegerValue(reader, defaultIndex));
                string elementCaption = (captionIndex == -1 ? elementValue : context.GetValue(reader, captionIndex));

                if (elementValue == value) {
                    result = true;
                    selectedCaption = elementCaption;
                }
                if (elementDefault > maxDefault) {
                    defaultValue = elementValue;
                    maxDefault = elementDefault;
                }

            }
            context.CloseQueryResult(reader, dbConnection);

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks for each element in the specified value array whether it is contained in the entity option list.</summary>
        /*!
/// <param name="values">the array of values to be checked</param>
/// <param name="defaultValues">contains the default values of the value set or <i>null</i> if there are no default values (output parameter)</param>
/// <param name="count">contains the number of elements in the value set (output parameter)</param>
/// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
/// <returns>an array of the same shape as <i>values</i>, where an element has the value <i>true</i> if the corresponding element in <i>values</i> is contained in the value set</returns>
    \sa For more information, see the documentation of IValueSet::CheckValues().
*/
        public virtual bool[] CheckValues(string[] values, out string[] defaultValues) {
            if (values == null) values = new string[0];
            bool[] result = new bool[values.Length];
            defaultValues = new string[0]; // !!! should contain default values

            OptionList = true;

            // Get the entity's data structure if it is null

            string select = "t." + IdField;
            string join = Join;
            string condition = null;

            if (ExcludeIds != null && ExcludeIds.Length != 0) {
                string excludeCondition = "";
                for (int i = 0; i < ExcludeIds.Length; i++) {
                    excludeCondition += (i == 0 ? "" : ",") + ExcludeIds[i];
                }
                if (condition == null) condition = ""; else condition += " AND ";
                condition += "t." + IdField + (ExcludeIds.Length == 1 ? "!=" : " NOT IN (") + excludeCondition + (ExcludeIds.Length == 1 ? "" : ")");
            }

            int index = 0;
            foreach (FieldExpression field in Fields) {
                if (index == 0) select = field.Expression;
                if (field is FixedValueField) {
                    if (condition == null) condition = ""; else condition += " AND ";
                    condition += field.Field + (field.Value == null ? " IS NULL" : "=" + StringUtils.EscapeSql(field.Value)); 
                }
                index++;
            }

            string query = "SELECT " + select + " FROM " + join + (condition == null ? "" : " WHERE " + condition);

            string value;

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(query, dbConnection);

            // Write result
            while (reader.Read()) {
                value = reader.GetString(0);
                for (int i = 0; i < values.Length; i++) {
                    if (values[i] == value) {
                        result[i] = true;
                        break;
                    }
                }
            }
            context.CloseQueryResult(reader, dbConnection);

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual int WriteValues(XmlWriter output) {
            OptionList = true;
            return WriteItemList();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual int WriteValues(MonoXmlWriter output) {
            OptionList = true;
            MonoXmlWriter oldXmlWriter = xmlWriter;
            xmlWriter = output;
            int count = WriteItemList();
            xmlWriter = oldXmlWriter;
            return count;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void GetValues(ProcessValueSetElementCallbackType processValueSetElement) { // TODO !!! Upgrade this to an interface method of IValueList
            OptionList = true;

            int aliasIndex = 0;
            string join = Join;

            string condition = FilterCondition;
            ExpandQuery(ref join, ref condition, ref aliasIndex);

            string groupBy = CustomAggregation;
            string orderBy = CustomSorting;
            string countFields = null;
            ExpandSortingClause(ref countFields, ref groupBy, ref orderBy);

            string select = "t." + IdField;
            int columnIndex = 1;
            ExpandSelectClause(ref columnIndex, ref select, "t", FieldFlags.List);

            // Get index of column that has the flag for the default value(s)
            int valueIndex = -1, defaultIndex = -1, captionIndex = -1;
            foreach (FieldExpression field in Fields) {
                if ((field.Flags & FieldFlags.List) == 0) continue;
                field.Reserved = true;
                switch (field.Name) {
                    case "value" :
                        valueIndex = field.ColumnIndex;
                        break;
                        case "default" :
                        defaultIndex = field.ColumnIndex;
                        break;
                        case "caption" :
                        captionIndex = field.ColumnIndex;
                        break;
                }
            }

            if (valueIndex == -1) return;

            string sql = String.Format("SELECT {0} FROM {1}{2}{3}{4};", 
                                       select,
                                       join,
                                       (condition == null ? "" : " WHERE " + condition),
                                       (groupBy == null ? "" : " GROUP BY " + groupBy),
                                       (orderBy == null ? "" : " ORDER BY " + orderBy)
                                       );

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);

            while (reader.Read()) {
                string elementValue = context.GetValue(reader, valueIndex);
                int elementDefault = (defaultIndex == -1 ? 0 : context.GetIntegerValue(reader, defaultIndex));
                string elementCaption = (captionIndex == -1 ? elementValue : context.GetValue(reader, captionIndex));

                processValueSetElement(elementValue, elementCaption, elementDefault);
            }
            context.CloseQueryResult(reader, dbConnection);

        }

        //---------------------------------------------------------------------------------------------------------------------

        private void AddValueFormatError(TypedValue field) {
            switch (field.Type) {
                case "bool" :
                    context.AddError("The value for \"" + field.Caption + "\" must be \"true\" or \"false\"");
                    break;
                    case "int" :
                    context.AddError("The value for \"" + field.Caption + "\" must be an integer number");
                    break;
                    case "float" :
                    context.AddError("The value for \"" + field.Caption + "\" must be a real number");
                    break;
                    case "date" :
                    case "datetime" :
                    case "startdate" :
                    case "enddate" :
                    context.AddError("The value for \"" + field.Caption + "\" must be a date or date/time according to ISO 8601");
                    break;
            }
        }


    }

}
