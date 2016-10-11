using System;
using System.Linq;
using System.Collections.Generic;
using Terradue.Util;
using System.Data;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine.Extensions;
using System.Collections.Specialized;
using System.Web;
using System.Text.RegularExpressions;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.OpenSearch.Filters;
using Terradue.Portal.OpenSearch;

namespace Terradue.Portal {

    /// <summary>
    /// Remote resource set.
    /// </summary>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("resourceset", EntityTableConfiguration.Full, HasOwnerReference = true, HasPrivilegeManagement = true, HasExtensions = true)]
    public class RemoteResourceSet : Entity, IMonitoredOpenSearchable, IProxiedOpenSearchable {

        protected OpenSearchEngine ose;

        //---------------------------------------------------------------------------------------------------------------------
		
        [EntityDataField("is_default")]
        public bool IsDefault { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("access_key")]
        public string AccessKey { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the UTC date and time of the resourceset's creation.</summary>
        [EntityDataField("creation_time")]
        public DateTime CreationTime { 
            get{ 
                return DateTime.SpecifyKind(creationtime, DateTimeKind.Utc);
            } 
            protected set{ creationtime=value; } 
        }
        private DateTime creationtime { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
		
        //[EntityComplexField(ReferenceField = "id_set", AutoLoad = true)]
        /// <summary>
        /// Gets or sets the resources.
        /// </summary>
        /// <value>The resources.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual RemoteResourceEntityCollection Resources { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the open search engine.
        /// </summary>
        /// <value>The open search engine.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public OpenSearchEngine OpenSearchEngine {
            get {
                return ose;
            }
            set {
                ose = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
		
        public RemoteResourceSet(IfyContext context) : base(context) {
            ose = new OpenSearchEngine();
        }

        //---------------------------------------------------------------------------------------------------------------------
		
        public static RemoteResourceSet FromId(IfyContext context, int id) {
            RemoteResourceSet result = new RemoteResourceSet(context);
            result.Id = id;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static RemoteResourceSet FromIdentifier(IfyContext context, string identifier) {
            RemoteResourceSet result = new RemoteResourceSet(context);
            result.Identifier = identifier;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Delete() {
            if (UserId != context.UserId) throw new UnauthorizedAccessException("You are not the owner of the item");
            base.Delete();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void LoadResources() {
            Resources = new RemoteResourceEntityCollection(context);
            Resources.Template.ResourceSet = this;
            Resources.Load();
            Resources.OpenSearchableChange += new OpenSearchableChangeEventHandler(OnOpenSearchableChange);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual IOpenSearchable[] GetOpenSearchableArray() {
            List<SmartGenericOpenSearchable> osResources = new List<SmartGenericOpenSearchable>(Resources.Count);

            foreach (RemoteResource res in Resources) {
                var entity = new SmartGenericOpenSearchable(new OpenSearchUrl(res.Location), ose);
                var eosd = entity.GetOpenSearchDescription();
                if (eosd.DefaultUrl != null && eosd.DefaultUrl.Type == "application/json") {
                    var atomUrl = eosd.Url.FirstOrDefault(u => u.Type == "application/atom+xml");
                    if (atomUrl != null) eosd.DefaultUrl = atomUrl;
                }

                osResources.Add(entity);
            }

            return osResources.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------

        #region IOpenSearchable implementation

        //---------------------------------------------------------------------------------------------------------------------

        public OpenSearchRequest Create(string type, System.Collections.Specialized.NameValueCollection parameters) {

            UriBuilder url = new UriBuilder(context.BaseUrl);
            url.Path += "/remoteresource/" + this.Identifier;
            var array = (from key in parameters.AllKeys
                                  from value in parameters.GetValues(key)
                                  select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            url.Query = string.Join("&", array);

            /*if (!String.IsNullOrEmpty(parameters["grouped"]) && parameters["grouped"] == "true") {
                return new MultiAtomGroupedOpenSearchRequest(ose, GetOpenSearchableArray(), type, new OpenSearchUrl(url.ToString()), true);
            }*/

            return new MultiOpenSearchRequest<AtomFeed, AtomItem>(ose, GetOpenSearchableArray(), type, new OpenSearchUrl(url.ToString()), true, this);
        }

        public QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByContentTypeAbility(this.DefaultMimeType);
            if (osee == null) return null;
            return new QuerySettings(this.DefaultMimeType, osee.ReadNative);
        }

        public string DefaultMimeType {
            get {
                return "application/atom+xml";
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public OpenSearchDescription GetOpenSearchDescription() {
            OpenSearchDescription osd = new OpenSearchDescription();
            osd.ShortName = this.Identifier;
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
            if (mimeType != "application/atom+xml") return null;
            var parameters = OpenSearchFactory.MergeOpenSearchParameters(GetOpenSearchableArray(), mimeType);
            parameters.Set("grouped", "{os:grouped?}");
            return parameters;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public long TotalResults {

            get {
                if (this.Resources != null) {
                    OpenSearchEngine ose = new OpenSearchEngine();
                    AtomOpenSearchEngineExtension aosee = new AtomOpenSearchEngineExtension();
                    ose.RegisterExtension(aosee);

                    try {

                        AtomFeed osr = (AtomFeed)ose.Query(this, new NameValueCollection(), typeof(AtomFeed));
                        return osr.TotalResults;

                    } catch (Exception e) {
                        // no error managment, set the number of product to 0
                        return 0;
                    }

                }
                return 0;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType) {
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual bool CanCache {
            get { return false; }
        }

        #endregion

        public virtual OpenSearchUrl GetSearchBaseUrl(string mimeType) {
            return new OpenSearchUrl(string.Format("{0}/remoteresource/{1}/search", context.BaseUrl, this.Identifier));
        }

        public virtual OpenSearchUrl GetDescriptionBaseUrl() {
            return new OpenSearchUrl(string.Format("{0}/remoteresource/{1}/description", context.BaseUrl, this.Identifier));
        }


        /// <summary>
        /// Gets the proxy OpenSearch Description.
        /// </summary>
        /// <returns>The proxy OpenSearch Description.</returns>
        /// remote resource must be IProxiedOpenSearchable because it is a virtual container for resource. 
        /// Therefore it must implement IProxiedOpenSearchable that will indicate to the OpenSearch Engine how to deal with its own description.
        /// In this function, self link description is set to be used by the engine later for describing the resource
        /// This function must be overidden by derived class in order to fit with the correct route in the application
        public virtual OpenSearchDescription GetProxyOpenSearchDescription() {
            return GetOpenSearchDescription();
        }

        public event OpenSearchableChangeEventHandler OpenSearchableChange;

        public void OnOpenSearchableChange(object sender, OnOpenSearchableChangeEventArgs data) {
            // Check if there are any Subscribers
            if (OpenSearchableChange != null) {
                // Call the Event
                OpenSearchableChange(this, data);
            }
        }
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class RemoteResourceEntityCollection : EntityList<RemoteResource> {

        protected override void IncludeInternal(RemoteResource item) {
            if (!IsLoading && item.Location == null) throw new InvalidOperationException("The location of a remote resource cannot be null");
            RemoteResource newItem = null;
            if (!IsLoading && !AllowDuplicates) {
                foreach (RemoteResource resource in Items) {
                    if (resource.Location == item.Location) newItem = item;
                }
            }
            base.IncludeInternal(newItem == null ? item : newItem);
        }

        public RemoteResourceEntityCollection(IfyContext context) : base(context) {
        }

    }



    /// <summary>
    /// Remote resource.
    /// </summary>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("resource", EntityTableConfiguration.Custom, NameField = "name")]
    public class RemoteResource : Entity {
        private RemoteResourceSet resourceSet;

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("id_set")]
        public int ResourceSetId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityEntityField("id_set")]
        /// <summary>
        /// Gets or sets the resource set.
        /// </summary>
        /// <value>The resource set.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public RemoteResourceSet ResourceSet { 
            get {
                if (this.resourceSet == null) resourceSet = RemoteResourceSet.FromId(context, ResourceSetId);
                return this.resourceSet;
            }
            set {
                this.resourceSet = value;
                if (value != null) ResourceSetId = value.Id;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("location")]
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The location.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string Location { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public RemoteResource(IfyContext context) : base(context) {
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static RemoteResource FromId(IfyContext context, int id) {
            RemoteResource result = new RemoteResource(context);
            result.Id = id;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static RemoteResourceEntityCollection GetResources(IfyContext context, RemoteResourceSet resourceSet) {
            RemoteResourceEntityCollection result = new RemoteResourceEntityCollection(context);
            result.Template.ResourceSet = resourceSet;
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT t.id, t.location, t.name FROM resource AS t WHERE id_set={0}", resourceSet.Id), dbConnection);
            while (reader.Read()) {
                RemoteResource resource = new RemoteResource(context);
                resource.Id = reader.GetInt32(0);
                resource.ResourceSet = resourceSet;
                resource.Location = reader.GetString(1);
                resource.Name = reader.GetString(2);
            }
            context.CloseQueryResult(reader, dbConnection);
            return result;
        }
    }
}

