using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using MySql.Data.MySqlClient;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Engine.Extensions;
using Terradue.OpenSearch.Filters;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.ServiceModel.Syndication;
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



    public abstract class EntityCollection {
        
        //---------------------------------------------------------------------------------------------------------------------

        protected IfyContext context;

        //---------------------------------------------------------------------------------------------------------------------

        public PrivilegeSet PrivilegeSet { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        protected Entity ReferringItem { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public int UserId { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string Identifier { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool OwnedItemsOnly { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        protected bool IsReadOnly { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool AllowDuplicates { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indictes whether a .</summary>
        /// <remarks></remarks>
        public abstract bool CanSearch { get; }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>A list of entities of a specific type.</summary>
    public abstract class EntityCollection<T> : EntityCollection, IEnumerable<T>, IMonitoredOpenSearchable where T : Entity {

        private EntityType entityType;
        private T template;
        private OpenSearchEngine ose;
        private Dictionary<FieldInfo, string> filterValues;

        public EntityType EntityType {
            get { return entityType; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets a template object of this collection's underlying type the template for initialization of new items and filtering of item lists.</summary>
        /// <remarks>The template object is created automatically if it is accessed.</remarks>
        public T Template {
            get {
                if (template == null) template = entityType.GetEntityInstance(context) as T;
                return template;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets an IEnumerable of all items contained in this collection.</summary>
        public abstract IEnumerable<T> Items { get; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the <see cref="EntityDictionary"/> that is used as an alternative source for the content reusing existing item instances.</summary>
        protected EntityDictionary<T> ItemSource { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of items in this collection.</summary>
        public abstract int Count { get; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether this collection is currently being loaded.</summary>
        /// <remarks>This property is used internally for optimization of duplicate checking and similar.</remarks>
        protected bool IsLoading { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public override bool CanSearch {
            get { return true; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityCollection(IfyContext context) : this(context, null, null) {
            this.entityType = GetEntityStructure();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityCollection(IfyContext context, EntityType entityType, Entity referringItem) {
            this.context = context;
            if (context != null) this.UserId = context.UserId;
            this.entityType = entityType;
            this.ReferringItem = referringItem;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected EntityType GetEntityStructure() {
            EntityType result = EntityType.GetEntityType(typeof(T));
            if (result == null) {
                result = EntityType.AddEntityType(typeof(T));
                if (result == null || result.Tables.Count == 0) throw new InvalidOperationException(String.Format("Entity information not available: {0}", typeof(T).FullName));
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the entity collection so that each item has the correct instance type.</summary>
        /// <remarks>Only entity lists loaded with this method can be stored back into the database.</remarks>
        public virtual void Load() {
            Identifier = entityType.Keyword;

            if (!(entityType is EntityRelationshipType) && !entityType.TopTable.HasExtensions && !entityType.HasNestedData) {
                LoadList();
                return;
            }

            Clear();

            string sql;
            if (OwnedItemsOnly) sql = entityType.GetListQueryForOwnedItems(context, UserId, true);
            else sql = entityType.GetListQuery(context, UserId, template, filterValues, true);
            if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);

            List<int> ids = new List<int>();

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) ids.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);
            if (context.ConsoleDebug) Console.WriteLine("COUNT: " + ids.Count);

            IsLoading = true;
            foreach (int id in ids) {
                if (context.ConsoleDebug) Console.WriteLine("ID = {0}", id);
                T item = entityType.GetEntityInstanceFromId(context, id) as T;
                item.UserId = UserId;
                item.UserId = UserId;
                item.Load(id);
                IncludeInternal(item);
            }
            IsLoading = false;
            if (template != null) foreach (T item in this) AlignWithTemplate(item, false);
            IsReadOnly = false;

        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the items of the entity collection so that each item has the same instance type, the base type or a generic type.</summary>
        /// <remarks>If this collections's underlying entity type has extensions, the collection contains items of the generic instance type of the (often abstract) base type. The generic instance type is usually not complete and has no functionality. Entity collections loaded with this method cannot be stored back into the database.</remarks>
        public virtual void LoadReadOnly() {
            IsReadOnly = true;
            LoadList();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the items of the entity collection from another collection rather than from the database.</summary>
        /// <param name="source">The source collection from which the items are obtained.</param>
        /// <param name="ignoreMissingItems">Decides whether items that should be contained in the resulting collection but are missing in the source collection are ignored or whether an exception is thrown.</param>
        public void LoadFromSource(EntityDictionary<T> source, bool ignoreMissingItems) {
            this.IsReadOnly = true;
            this.ItemSource = source;

            string sql;
            if (entityType is EntityRelationshipType && ReferringItem != null) sql = entityType.GetListQueryOfRelationship(context, UserId, ReferringItem, true);
            else if (OwnedItemsOnly) sql = entityType.GetListQueryForOwnedItems(context, UserId, true);
            else sql = entityType.GetListQuery(context, UserId, template, filterValues, true);
            if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);

            List<int> ids = new List<int>();

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            IsLoading = true;
            while (reader.Read()) ids.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            IsLoading = true;
            foreach (int id in ids) {
                if (context.ConsoleDebug) Console.WriteLine("ID = {0}", id);
                if (!source.Contains(id)) {
                    if (ignoreMissingItems) continue;
                    throw new EntityNotFoundException("{0} not found in source collection", EntityType, EntityType.GetItemTerm(id));
                }
                IncludeInternal(source[id]);
            }
            IsLoading = false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual void LoadList() {
            Clear();

            string sql;
            if (entityType is EntityRelationshipType && ReferringItem != null) sql = entityType.GetListQueryOfRelationship(context, UserId, ReferringItem, false);
            else if (OwnedItemsOnly) sql = entityType.GetListQueryForOwnedItems(context, UserId, false);
            else sql = entityType.GetListQuery(context, UserId, template, filterValues, false);

            if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            IsLoading = true;
            while (reader.Read()) {
                T item = entityType.GetEntityInstance(context) as T;
                item.Load(entityType, reader, EntityAccessLevel.None);
                if (template != null) AlignWithTemplate(item, false);
                IncludeInternal(item);
            }
            IsLoading = false;
            context.CloseQueryResult(reader, dbConnection);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads a list of items that are accessible by the specified groups.</summary>
        /// <param name="groupIds">An array of database IDs of groups</param>
        public void LoadGroupAccessibleItems(int[] groupIds) {
            IsReadOnly = true;
            string sql = entityType.GetGroupQuery(context, groupIds, false, null);
            if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                T item = entityType.GetEntityInstance(context) as T;
                item.Load(entityType, reader, EntityAccessLevel.None);
                if (template != null) AlignWithTemplate(item, false);
                IncludeInternal(item);
            }
            context.CloseQueryResult(reader, dbConnection);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Stores the entity list.</summary>
        public void Store() {
            StoreList(false, false);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Stores the items exactly as in the in the entity list.</summary>
        public void StoreExactly() {
            StoreList(true, false);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Stores only new items of the entity list.</summary>
        public void StoreNew() {
            StoreList(false, true);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Stores the entity list.</summary>
        /// <remarks></remearks>
        protected virtual void StoreList(bool removeOthers, bool onlyNewItems) {

            if (IsReadOnly) throw new InvalidOperationException("Cannot store read-only entity collection");

            //bool isRelationship = (entityType is EntityRelationshipType);
            foreach (T item in Items) {
                if (onlyNewItems && item.Exists || !item.IsInCollection) continue;
                /*if (isRelationship) item.Store(entityType as EntityRelationshipType, ReferringItem);
                else*/ item.Store();
            }

            // TODO: This was done before the storage of the contained items (previous block).
            // TODO: To avoid exceptions with references to non-existing during the store (e.g. native datasets contained in virtual datasets), the deletion is done afterwards now. Check whether there is a better way
            EntityTableAttribute storeTable = entityType.TopStoreTable;
            string sql = null;

            if (entityType is EntityRelationshipType) {
                sql = String.Format("{1}={0}", ReferringItem.Id, storeTable.ReferringItemField);

            } else {
                if (removeOthers) {
                    // Remove items that are not or no longer contained in the collection
                    string keepIds = null;
                    foreach (T item in Items) {
                        if (!item.IsInCollection) continue;
                        if (keepIds == null) keepIds = String.Empty; else keepIds += ", ";
                        keepIds += item.Id;
                    }
                    if (keepIds != null) {
                        sql = String.Format("{0} NOT IN ({1})", storeTable.IdField, keepIds);
                        if (template != null) {
                            string condition = entityType.GetTemplateCondition(template, true);
                            if (condition != null) sql = String.Format("{0} AND {1}", sql, condition);
                            //if (hasParentReference && Parent != null) sql += String.Format(" AND {1}={0}", Parent.Id, storeTable.ParentReferenceField);
                        }
                    }
                } else {
                    // Remove items that are no longer in the collection (unlinked)
                    string deleteIds = null;
                    foreach (T item in Items) {
                        if (item.IsInCollection) continue;
                        if (deleteIds == null) deleteIds = String.Empty;
                        else deleteIds += ", ";
                        deleteIds += item.Id;
                    }
                    if (deleteIds != null) sql = String.Format("{0} IN ({1})", storeTable.IdField, deleteIds);
                }
            }

            if (sql != null) {
                sql = String.Format("DELETE FROM {0} WHERE {1};", storeTable.Name, sql);
                if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);
                context.Execute(sql);
            }

        }

        //---------------------------------------------------------------------------------------------------------------------

        public void PrepareUpdate() {
            foreach (T item in Items) item.IsInCollection = false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public abstract void Clear();

        //---------------------------------------------------------------------------------------------------------------------

        public T CreateItem(string identifier) {
            return entityType.GetEntityInstanceFromIdentifier(context, identifier) as T;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a new instance of the item of the underlying type.
        /// <returns>The created item.</returns>
        public T CreateItem() {
            return entityType.GetEntityInstance(context) as T;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Includes an item in the collection.</summary>
        /// <parameter name="item">The item to be included.</parameter>
        public void Include(T item) {
            AlignWithTemplate(item, true);
            IncludeInternal(item);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Includes an item in the collection according to the of internal mechanism the derived class.</summary>
        /// <parameter name="item">The item to be included.</parameter>
        protected virtual void IncludeInternal(T item) {
            if (!IsReadOnly && item.Collection == null) {
                item.Collection = this;
                item.IsInCollection = true;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a list of the items contained in this collection.</summary>
        public virtual List<T> GetItemsAsList() {
            return new List<T>(Items);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Aligns the properties of the specified item with the values in the template.</summary>
        /// <param name="item">The item whose properties are to be aligned with the template.</param>
        /// <param name="all">Decides whether all properties are aligned or only those that link to other entities.</param>
        protected void AlignWithTemplate(T item, bool all) {
            if (template == null) return;

            foreach (FieldInfo field in entityType.Fields) {
                if (field.FieldType == EntityFieldType.EntityField) {
                    PropertyInfo propertyInfo = field.Property;
                    object value = propertyInfo.GetValue(template, null);
                    if (value != null) propertyInfo.SetValue(item, value, null);

                } else if (all && field.FieldType == EntityFieldType.DataField) {
                    PropertyInfo propertyInfo = field.Property;
                    object value = propertyInfo.GetValue(template, null);
                    if (value == null) continue;

                    if (propertyInfo.PropertyType == typeof(bool) && !(bool)value) continue;
                    if (propertyInfo.PropertyType == typeof(int) && (int)value == 0) continue;
                    if (propertyInfo.PropertyType == typeof(long) && (long)value == 0) continue;
                    if (propertyInfo.PropertyType == typeof(double) && (double)value == 0) continue;
                    if (propertyInfo.PropertyType == typeof(DateTime) && (DateTime)value == DateTime.MinValue) continue;
                    if (propertyInfo.PropertyType.IsEnum && value == field.NullValue) continue;

                    propertyInfo.SetValue(item, value, null);
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the filter search term for the specified property.</summary>
        /// <param name="propertyName">The name of the property of the underlying <see cref="Entity"/> subclass on which the filter is applied.</param>
        /// <param name="searchTerm">The filter search string according to the property type.</param>
        public void SetFilter(string propertyName, string searchTerm) {
            FieldInfo field = null;
            if (propertyName == "Id") {
                field = new FieldInfo(entityType.ClassType.GetProperty("Id"), 0, entityType.TopTable.IdField);
            } else if (propertyName == "Identifier") {
                if (entityType.TopTable.HasIdentifierField) field = new FieldInfo(entityType.ClassType.GetProperty("Identifier"), 0, entityType.TopTable.IdentifierField);
            } else if (propertyName == "Name") {
                if (entityType.TopTable.HasNameField) field = new FieldInfo(entityType.ClassType.GetProperty("Name"), 0, entityType.TopTable.NameField);
            } else {
                field = entityType.GetField(propertyName);
            }
            if (field == null) throw new ArgumentException(String.Format("Property {0}.{1} does not exist or cannot be used for filtering", entityType.ClassType.FullName, propertyName));
            SetFilter(field, searchTerm);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the filter search term for the specified field of the underlying entity type.</summary>
        /// <param name="field">The <see cref="FieldInfo"/> instance on which the filter is applied.</param>
        /// <param name="searchTerm">The filter search string according to the property type.</param>
        public void SetFilter(FieldInfo field, string searchTerm) {
            if (field == null) throw new ArgumentNullException("No filtering field specified");
            if (!entityType.Fields.Contains(field) && !field.Property.DeclaringType.IsAssignableFrom(entityType.ClassType)) throw new InvalidOperationException("Invalid filtering field specified");
            if (filterValues == null) filterValues = new Dictionary<FieldInfo, string>();
            filterValues[field] = searchTerm;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ClearFilters() {
            filterValues = null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() {
            return Items.GetEnumerator();
        }

        //---------------------------------------------------------------------------------------------------------------------

        IEnumerator IEnumerable.GetEnumerator() {
            return Items.GetEnumerator();
        }

        //---------------------------------------------------------------------------------------------------------------------

        private AtomFeed GenerateSyndicationFeed(NameValueCollection parameters) {
            UriBuilder myUrl = new UriBuilder(context.BaseUrl + "/" + this.Identifier);
            string[] queryString = Array.ConvertAll(parameters.AllKeys, key => String.Format("{0}={1}", key, parameters[key]));
            myUrl.Query = string.Join("&", queryString);

            AtomFeed feed = new AtomFeed("Discovery feed for "+this.Identifier,
                                                       "This OpenSearch Service allows the discovery of the different items which are part of the "+this.Identifier+" collection" +
                                                       "This search service is in accordance with the OGC 10-032r3 specification.",
                                                       myUrl.Uri, myUrl.ToString(), DateTimeOffset.UtcNow);

            feed.Generator = "Terradue Web Server";

            List<AtomItem> items = new List<AtomItem>();

            foreach (T s in Items) {

                if (!string.IsNullOrEmpty(parameters["id"])) { 
                    if ( s.Identifier != parameters["id"] )
                        continue;
                }

                if (!string.IsNullOrEmpty(parameters["author"])) {
                    if (!(User.ForceFromId(context, s.OwnerId)).Username.Equals(parameters["author"])) continue;
                }

                if (s is IAtomizable) {
                    AtomItem item = (s as IAtomizable).ToAtomItem(parameters);
                    if(item != null) items.Add(item);
                } else {

                    var name = s.Name == null ? "" : s.Name;
                    var identifier = s.Identifier == null ? "" : s.Identifier;
                    var content = s.TextContent == null ? "" : s.TextContent;

                    if (!string.IsNullOrEmpty(parameters["q"])) {  
                        string q = parameters["q"];
                        if (!(name.Contains(q) || identifier.Contains(q) || content.Contains(q)))
                            continue;
                    }

                    if (identifier == "")
                        identifier = Guid.NewGuid().ToString();

                    Uri id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + identifier);

                    AtomItem entry = new AtomItem(name, content, id, id.ToString(), DateTime.UtcNow);
                    entry.Categories.Add(new SyndicationCategory(entityType.Keyword));

                    entry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", identifier);

                    items.Add(entry);
                }
            }

            // Load all avaialable Datasets according to the context

            Terradue.OpenSearch.Request.PaginatedList<AtomItem> pds = new Terradue.OpenSearch.Request.PaginatedList<AtomItem>();

            int startIndex = 1;
            if (parameters["startIndex"] != null) startIndex = int.Parse(parameters["startIndex"]);

            pds.PageNo = 1;
            if (parameters["startPage"] != null) pds.PageNo = int.Parse(parameters["startPage"]);

            pds.PageSize = 20;
            if (parameters["count"] != null) pds.PageSize = int.Parse(parameters["count"]);

            pds.StartIndex = startIndex - 1;

            if(this.Identifier != null) feed.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);

            pds.AddRange(items);

            feed.Items = pds.GetCurrentPage();

            feed.TotalResults = pds.Count;

            return feed;

        }

        public OpenSearchEngine OpenSearchEngine {
            get {
                if (ose == null) {
                    ose = new OpenSearchEngine();
                    ose.RegisterExtension(new AtomOpenSearchEngineExtension());
                }
                return ose;
            }
            set {
                ose = value;
            }
        }


        public virtual OpenSearchUrl GetSearchBaseUrl(string mimetype) {
            return new OpenSearchUrl (string.Format("{0}/{1}/search", context.BaseUrl, entityType.Keyword));
        }

        public virtual OpenSearchUrl GetDescriptionBaseUrl() {
            return new OpenSearchUrl (string.Format("{0}/{1}/description", context.BaseUrl, entityType.Keyword));
        }

        //---------------------------------------------------------------------------------------------------------------------

        #region IOpenSearchable implementation

        public QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByContentTypeAbility(this.DefaultMimeType);
            if (osee == null) return null;
            return new QuerySettings(this.DefaultMimeType, osee.ReadNative);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string DefaultMimeType {
            get {
                return "application/atom+xml";
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public OpenSearchRequest Create(string type, System.Collections.Specialized.NameValueCollection parameters) {
            UriBuilder url = new UriBuilder(context.BaseUrl);
            url.Path += "/"+this.Identifier+"/";
            var array = (from key in parameters.AllKeys
                         from value in parameters.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            url.Query = string.Join("&", array);

            var request = new AtomOpenSearchRequest(new OpenSearchUrl(url.Uri), GenerateSyndicationFeed);

            return request;
        }

        public OpenSearchDescription GetOpenSearchDescription() {
            OpenSearchDescription osd = new OpenSearchDescription();
            osd.ShortName = this.entityType.ClassName;
            osd.Contact = context.GetConfigValue("CompanyEmail");
            osd.SyndicationRight = "open";
            osd.AdultContent = "false";
            osd.Language = "en-us";
            osd.OutputEncoding = "UTF-8";
            osd.InputEncoding = "UTF-8";
            osd.Developer = "Terradue OpenSearch Development Team";
            osd.Attribution = context.GetConfigValue("CompanyName");

            List<OpenSearchDescriptionUrl> urls = new List<OpenSearchDescriptionUrl>();

            UriBuilder urlb = new UriBuilder(GetDescriptionBaseUrl());

            OpenSearchDescriptionUrl url = new OpenSearchDescriptionUrl("application/opensearchdescription+xml", urlb.ToString(), "self");
            urls.Add(url);

            urlb = new UriBuilder(GetSearchBaseUrl("application/atom+xml"));
            NameValueCollection query = GetOpenSearchParameters("application/atom+xml");

            NameValueCollection nvc = HttpUtility.ParseQueryString(urlb.Query);
            foreach (var key in nvc.AllKeys) {
                query.Set(key, nvc[key]);
            }

            foreach (var osee in OpenSearchEngine.Extensions.Values) {
                query.Set("format", osee.Identifier);

                string[] queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
                urlb.Query = string.Join("&", queryString);
                url = new OpenSearchDescriptionUrl(osee.DiscoveryContentType, urlb.ToString(), "search");
                urls.Add(url);
            }

            osd.Url = urls.ToArray();

            return osd;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public System.Collections.Specialized.NameValueCollection GetOpenSearchParameters(string mimeType) {

            return OpenSearchFactory.GetBaseOpenSearchParameter();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public long TotalResults {
            get {
                return this.Count;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual bool CanCache {
            get { return false; }
        }

        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType) {
        }

        #endregion

        public event OpenSearchableChangeEventHandler OpenSearchableChange;

        public void OnOpenSearchableChange (object sender,  OnOpenSearchableChangeEventArgs data)
        {
            // Check if there are any Subscribers
            if (OpenSearchableChange != null)
            {
                // Call the Event
                OpenSearchableChange (this, data);
            }
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>A list of entities of a specific type.</summary>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    public class EntityList<T> : EntityCollection<T>, IOpenSearchable where T : Entity {

        private List<T> items;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets an IEnumerable of all items contained in this list.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public override IEnumerable<T> Items {
            get { return items; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of items in this list.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public override int Count {
            get { return items.Count; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new EntityList instance instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public EntityList(IfyContext context) : base(context) {
            this.items = new List<T>();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new EntityList instance instance for items related to another entity item.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="entityType">The entity type to which the items of this dictionary refer.</param>
        /// <param name="referringItem">The referring item to which the items in the dictionary are related (items belonging to an item of another entity type, e.g. resources of a specific resource set). This parameter is ignored if the underlying entity type's base table has no <c>ReferringItemField</c>.</param>
        public EntityList(IfyContext context, EntityType entityType, Entity referringItem) : base(context, entityType, referringItem) {
            this.items = new List<T>();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new EntityDictionary instance instance for items owned by the specified user.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="userId">The user who must be the owner of the items in the list.</param>
        public static EntityList<T> ForUser(IfyContext context, int userId) {
            EntityList<T> result = new EntityList<T>(context);
            result.UserId = userId;
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Removes all items from this list.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public override void Clear() {
            items.Clear();
            OnOpenSearchableChange(this, new OnOpenSearchableChangeEventArgs(this));
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Includes an item in this list.</summary>
        /// <parameter name="item">The item to be included.</parameter>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        protected override void IncludeInternal(T item) {
            base.IncludeInternal(item);
            T newItem = null;
            if (!IsLoading && !AllowDuplicates && item.Identifier != null) {
                foreach (T collectionItem in Items) {
                    if (collectionItem.Identifier == item.Identifier) newItem = item;
                }
            }
            if (newItem != null) items.Remove(newItem);
            items.Add(item);
            OnOpenSearchableChange(this, new OnOpenSearchableChangeEventArgs(this));
        }

        //---------------------------------------------------------------------------------------------------------------------

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>A dictionary of entities of a specific type, where items are addressed by their unique numeric database (or temporary) ID or by their string identifier.</summary>
    /// <remarks>
    ///     <para>The key type of the dictionary can be both <i>int</i> or <i>string</i>.</para>
    ///     <para>An <i>int</i> key is an item's database ID that corresponds to the property <see cref="Entity.Id"/> or, if the item does not exist in the database, a temporary negative number.</para>
    ///     <para>A <i>string</i> key is an item's unique identifier <see cref="Entity.Identifier"/>. Not all entity types support identifiers; in those cases the <i>string</i> index is not usable.</para>
    /// </remarks>
    public class EntityDictionary<T> : EntityCollection<T> where T : Entity {

        private Dictionary<int, T> itemsById;
        private Dictionary<string, T> itemsByIdentifier;
        private int temporaryId = 0; // decreases by 1 with every new added item that is not yet stored in the database

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets an IEnumerable of all items contained in this dictionary.</summary>
        public override IEnumerable<T> Items {
            get { return itemsById.Values; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the dictionary item with the specified numeric value.</summary>
        /// <remarks>The numeric value is the database ID if the item exists or, otherwise, a temporary negative value.</remarks>
        public T this[int id] {
            get { return itemsById[id]; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the dictionary item with the specified alphanumeric identifier.</summary>
        /// <remarks>This accessor is only available if the underlying entity type supports identifiers.</remarks>
        public T this[string identifier] {
            get {
                if (!EntityType.TopTable.HasIdentifierField) throw new InvalidOperationException(String.Format("An instance of {0} cannot be addressed by an identifier", EntityType.SingularCaption));
                return itemsByIdentifier[identifier];
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override int Count {
            get { return itemsById.Count; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new EntityDictionary instance instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public EntityDictionary(IfyContext context) : base(context) {
            this.itemsById = new Dictionary<int, T>();
            this.itemsByIdentifier = new Dictionary<string, T>();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new EntityDictionary instance instance related to another entity item.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="entityType">The entity type to which the items of this dictionary refer.</param>
        /// <param name="referringItem">The referring item to which the items in the dictionary are related (items belonging to an item of another entity type, e.g. resources of a specific resource set). This parameter is ignored if the underlying entity type's base table has no <c>ReferringItemField</c>.</param>
        public EntityDictionary(IfyContext context, EntityType entityType, Entity referringItem) : base(context, entityType, referringItem) {
            this.itemsById = new Dictionary<int, T>();
            this.itemsByIdentifier = new Dictionary<string, T>();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Removes all items from this dictionary.</summary>
        public override void Clear() {
            itemsById.Clear();
            itemsByIdentifier.Clear();
            OnOpenSearchableChange(this, new OnOpenSearchableChangeEventArgs(this));
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Includes an item in this dictionary.</summary>
        /// <parameter name="item">The item to be included.</parameter>
        protected override void IncludeInternal(T item) {
            base.IncludeInternal(item);
            int itemId = (item.Exists ? item.Id : --temporaryId);
            itemsById[itemId] = item;
            if (EntityType.TopTable.HasIdentifierField) itemsByIdentifier[item.Identifier] = item;
            OnOpenSearchableChange(this, new OnOpenSearchableChangeEventArgs(this));
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether this dictionary contains an item with the specified numeric key.</summary>
        /// <returns><c>true</c> if the item was found, <c>false</c> otherwise.</returns>
        /// <param name="key">The numeric key under which the item can be found in this dictionary. The numeric key is either the item's database ID or a temporary negative value.</param>
        public bool Contains(int key) {
            if (key == 0) return false;
            return itemsById.ContainsKey(key);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether this dictionary contains an item with the specified alphanumeric key.</summary>
        /// <returns><c>true</c> if the item was found, <c>false</c> otherwise.</returns>
        /// <param name="key">The alphanumeric key under which the item can be found in this dictionary.</param>
        public bool Contains(string key) {
            if (key == null) return false;
            return itemsByIdentifier.ContainsKey(key);
        }

        [Obsolete("Use matching overload of Contains")]
        public bool ContainsIdentifier(string identifier) {
            return Contains(identifier);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Updates the internal dictionary for the numeric IDs replacing temporary IDs with actual database IDs for items that have been stored recently.</summary>
        public void UpdateIndex() {
            foreach (int key in itemsById.Keys) {
                if (key > 0) continue;
                T item = itemsById[key];
                if (item.Exists) {
                    itemsById.Remove(key);
                    itemsById[item.Id] = item;
                }
            }
        }

    }
    

    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>A dictionary of entities of a specific type, where items are addressed by their numeric ID.</summary>
    /// <remarks>The key type of the dictionary is <i>int</i>. The key value of an item is the database ID of the item (property Entity.Id).</remarks>
    [Obsolete("EntityDictionary<T> supports also a numeric indexer for the items' IDs")]
    public class EntityIdDictionary<T> : EntityCollection<T> where T : Entity {

        private Dictionary<int, T> items;
        
        //---------------------------------------------------------------------------------------------------------------------

        public T this[int id] {
            get { return items[id]; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override IEnumerable<T> Items {
            get { return items.Values; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override int Count {
            get { return items.Count; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public EntityIdDictionary(IfyContext context) : base(context) {
            this.items = new Dictionary<int, T>();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityIdDictionary(IfyContext context, EntityType entityType, Entity referringItem) : base(context, entityType, referringItem) {
            this.items = new Dictionary<int, T>();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Removes all items from the list.</summary>
        public override void Clear() {
            items.Clear();
            OnOpenSearchableChange(this, new OnOpenSearchableChangeEventArgs(this));
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Includes an item in this dictionary.</summary>
        /// <parameter name="item">The item to be included.</parameter>
        protected override void IncludeInternal(T item) {
            base.IncludeInternal(item);
            T newItem = null;
            if (!IsLoading && !AllowDuplicates && item.Identifier != null) {
                foreach (T collectionItem in Items) {
                    if (collectionItem.Identifier == item.Identifier) newItem = item;
                }
            }
            if (newItem != null) items.Remove(newItem.Id);
            items[item.Id] = item;
            OnOpenSearchableChange(this, new OnOpenSearchableChangeEventArgs(this));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool ContainsId(int id) {
            return items.ContainsKey(id);
        }
        
    }

}
