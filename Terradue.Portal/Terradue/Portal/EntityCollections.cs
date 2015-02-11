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

    

    /// <summary>Interface that defines methods for different types of collections of entities of the same type.</summary>
    /// <remarks>This interface defines the minimum functionality that an entity collection must provide regarding reading and writing access for the collection and for loading and storing the items of the collection in the database.</remarks>
    public interface IEntityCollection<T> {
        
        IEnumerable<T> Items { get; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the entity collection should contain only items owned by the current user.</summary>
        /// <remarks>This setting has only effect if the underlying Entity type has an owner reference.</remarks>
        bool OwnedItemsOnly { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the entity collection where each item has the correct instance type.</summary>
        /// <remarks>Only entity collections loaded with this method can be stored back into the database.</remarks>
        void Load();
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the entity collection where items have a generic instance type.</summary>
        /// <remarks>If the collections's entity type has extensions, the collection contains items of the generic instance type of the (often abstract) base type. The generic instance type is usually not complete and has no functionality. Entity collections loaded with this method cannot be stored back into the database.</remarks>
        void LoadReadOnly();
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Stores the entity list.</summary>
        void Store();
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Stores the entity list removing the items that are not contained in the collection.</summary>
        void StoreExactly();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads a list of items that are accessible by the specified groups.</summary>
        /// <param name="groupIds">An array of database IDs of groups</param>
        void LoadGroupAccessibleItems(int[] groupIds);

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Temporarilly unlinks all items from the collection to prepare the addition of new and substitution of existing items.</summary>
        /// <remarks>The items are not deleted from the collection, but their flag IsInCollection is set to <i>false</i>. This allows to recognize items that are not contained any longer in the collection after the update of the list has taken place.</remarks>
        void PrepareUpdate();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Includes an item in the collection.</summary>
        /// <parameter name="item">The item to be included.</parameter>
        void Include(T item);
        
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    

    /// <summary>A list of entities of a specific type.</summary>
    public abstract class EntityCollection<T> : IEntityCollection<T>, IEnumerable<T>, IOpenSearchable where T : Entity {

        private EntityType entityType;
        private T template;
        private OpenSearchEngine ose;

        //---------------------------------------------------------------------------------------------------------------------

        protected IfyContext context;

        //---------------------------------------------------------------------------------------------------------------------

        public T Template {
            get {
                if (template == null) template = entityType.GetEntityInstance(context) as T;
                return template;
            }
        }

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

        public abstract IEnumerable<T> Items { get; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of items in the collection.</summary>
        public abstract int Count { get; }

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

        /// <summary>Loads the entity list where each item has the correct instance type.</summary>
        /// <remarks>Only entity lists loaded with this method can be stored back into the database.</remarks>
        public virtual void Load() {
            Identifier = entityType.Keyword;

            if (!entityType.TopTable.HasExtensions && !entityType.HasNestedData) {
                LoadList();
                return;
            }

            Clear();

            string sql = (OwnedItemsOnly ? entityType.GetListQueryForOwnedItems(context, UserId, true) : entityType.GetListQueryWithTemplate(context, UserId, template, true));
            if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);

            List<int> ids = new List<int>();

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) ids.Add(reader.GetInt32(0));
            context.CloseQueryResult(reader, dbConnection);

            foreach (int id in ids) {
                T item = entityType.GetEntityInstanceFromId(context, id) as T;
                item.Load(id);
                IncludeInternal(item);
            }
            if (template != null) foreach (T item in this) AlignWithTemplate(item, false);
            IsReadOnly = false;

        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Loads the entity list where items have a generic instance type.</summary>
        /// <remarks>If the collections's entity type has extensions, the collection contains items of the generic instance type of the (often abstract) base type. The generic instance type is usually not complete and has no functionality. Entity collections loaded with this method cannot be stored back into the database.</remarks>
        public virtual void LoadReadOnly() {
            IsReadOnly = true;
            LoadList();
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual void LoadList() {
            Clear();

            string sql;
            if (entityType is EntityRelationshipType && ReferringItem != null) sql = entityType.GetListQueryOfRelationship(context, UserId, ReferringItem, false);
            else if (OwnedItemsOnly) sql = entityType.GetListQueryForOwnedItems(context, UserId, false);
            else sql = entityType.GetListQueryWithTemplate(context, UserId, template, false);

            if (context.ConsoleDebug) Console.WriteLine("SQL: " + sql);

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                Console.WriteLine("LOAD {0}", entityType.TopStoreTable.Name);
                T item = entityType.GetEntityInstance(context) as T;
                item.Load(entityType, reader);
                if (template != null) AlignWithTemplate(item, false);
                IncludeInternal(item);
            }
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
                item.Load(entityType, reader);
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

            if (IsReadOnly) throw new InvalidOperationException("Cannot store read-only entity list");

            EntityTableAttribute storeTable = entityType.TopStoreTable;
            string sql = null;

            if (entityType is EntityRelationshipType) {
                sql = String.Format("{1}={0}", ReferringItem.Id, entityType.TopStoreTable.ReferringItemField);

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

            foreach (T item in Items) {
                if (onlyNewItems && item.Exists || !item.IsInCollection) continue;
                item.Store(entityType as EntityRelationshipType, ReferringItem);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void PrepareUpdate() {
            foreach (T item in Items) item.IsInCollection = false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public abstract void Clear();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Includes an item in the list.</summary>
        /// <parameter name="item">The item to be included.</parameter>
        public void Include(T item) {
            AlignWithTemplate(item, true);
            IncludeInternal(item);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Includes an item in the list.</summary>
        /// <parameter name="item">The item to be included.</parameter>
        protected abstract void IncludeInternal(T item);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the template.</summary>
        /// <param name="item">The item.</param>
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

            PaginatedList<AtomItem> pds = new PaginatedList<AtomItem>();

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
            get { return Convert.ToInt64(this.Count); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr) {

        }


        public ParametersResult DescribeParameters() {
            return OpenSearchFactory.GetDefaultParametersResult();
        }
        #endregion
    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>A list of entities of a specific type.</summary>
    /// \xrefitem uml "UML" "UML Diagram"
    public class EntityList<T> : EntityCollection<T>, IOpenSearchable where T : Entity {

        private List<T> items;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <value>The items.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public override IEnumerable<T> Items {
            get { return items; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <value>The count.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public override int Count {
            get { return items.Count; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public EntityList(IfyContext context) : base(context) {
            this.items = new List<T>();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityList(IfyContext context, EntityType entityType, Entity referringItem) : base(context, entityType, referringItem) {
            this.items = new List<T>();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public static EntityList<T> ForUser(IfyContext context, int userId) {
            EntityList<T> result = new EntityList<T>(context);
            result.UserId = userId;
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Removes all items from the list.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public override void Clear() {
            items.Clear();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Includes an item in the list.</summary>
        /// <parameter name="item">The item to be included.</parameter>
        /// \xrefitem uml "UML" "UML Diagram"
        protected override void IncludeInternal(T item) {
            item.IsInCollection = true;
            items.Add(item);
        }

        //---------------------------------------------------------------------------------------------------------------------

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>A dictionary of entities of a specific type, where items are addressed by their unique string identifier.</summary>
    /// <remarks>The key type of the dictionary is <i>string</i>. The key value of an item is the identifier of the item (property Entity.Identifier).</remarks>
    public class EntityDictionary<T> : EntityCollection<T> where T : Entity {

        private Dictionary<string, T> items;
        
        //---------------------------------------------------------------------------------------------------------------------

        public T this[string identifier] {
            get { return items[identifier]; }
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
        
        public EntityDictionary(IfyContext context) : base(context) {
            this.items = new Dictionary<string, T>();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public EntityDictionary(IfyContext context, EntityType entityType, Entity referringItem) : base(context, entityType, referringItem) {
            this.items = new Dictionary<string, T>();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Removes all items from the list.</summary>
        public override void Clear() {
            items.Clear();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Includes an item in the dictionary.</summary>
        /// <parameter name="item">The item to be included.</parameter>
        protected override void IncludeInternal(T item) {
            item.IsInCollection = true;
            items[item.Identifier] = item;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool ContainsIdentifier(string identifier) {
            if (identifier == null) return false;
            return items.ContainsKey(identifier);
        }

    }
    

    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>A dictionary of entities of a specific type, where items are addressed by their numeric ID.</summary>
    /// <remarks>The key type of the dictionary is <i>int</i>. The key value of an item is the database ID of the item (property Entity.Id).</remarks>
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
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Includes an item in the dictionary.</summary>
        /// <parameter name="item">The item to be included.</parameter>
        protected override void IncludeInternal(T item) {
            item.IsInCollection = true;
            items[item.Id] = item;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool ContainsId(int id) {
            return items.ContainsKey(id);
        }
        
    }
    
}
