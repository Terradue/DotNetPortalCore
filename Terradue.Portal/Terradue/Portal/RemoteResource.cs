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

namespace Terradue.Portal {

	[EntityTable("resourceset", EntityTableConfiguration.Full, HasOwnerReference = true, HasPrivilegeManagement = true)]
    public class RemoteResourceSet : Entity, IOpenSearchable, IProxiedOpenSearchable {

        protected OpenSearchEngine ose;

		//---------------------------------------------------------------------------------------------------------------------
		
        [EntityDataField("is_default")]
		public bool IsDefault { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        [EntityDataField("access_key")]
        public string AccessKey { get; set; }

		//---------------------------------------------------------------------------------------------------------------------
		
        //[EntityComplexField(ReferenceField = "id_set", AutoLoad = true)]
		public virtual EntityList<RemoteResource> Resources { get; set; }

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
            Resources = new EntityList<RemoteResource>(context);
            Resources.Template.ResourceSet = this;
            Resources.Load();
		}

		//---------------------------------------------------------------------------------------------------------------------

		public virtual IOpenSearchable[] GetOpenSearchableArray() {
			List<GenericOpenSearchable> osResources = new List<GenericOpenSearchable>(Resources.Count);

			foreach (RemoteResource res in Resources) {
				osResources.Add(new GenericOpenSearchable(new OpenSearchUrl(res.Location), ose));
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
            return new MultiAtomOpenSearchRequest(ose, GetOpenSearchableArray(), type, new OpenSearchUrl(url.ToString()), true );
		}

        public QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByContentTypeAbility(this.DefaultMimeType);
            if (osee == null)
                return null;
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
			return OpenSearchFactory.MergeOpenSearchParameters(GetOpenSearchableArray(), mimeType);
		}

        //---------------------------------------------------------------------------------------------------------------------

        public long TotalResults { 
            get { return 0; } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr) {

        }

        //---------------------------------------------------------------------------------------------------------------------



        public ParametersResult DescribeParameters() {
            return OpenSearchFactory.GetDefaultParametersResult();
        }
		#endregion

        public virtual OpenSearchUrl GetSearchBaseUrl(string mimeType) {
            return new OpenSearchUrl (string.Format("{0}/remoteresource/{1}/search", context.BaseUrl, this.Identifier));
        }

        public virtual OpenSearchUrl GetDescriptionBaseUrl() {
            return new OpenSearchUrl (string.Format("{0}/remoteresource/{1}/description", context.BaseUrl, this.Identifier));
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

	}

    [EntityTable("resource", EntityTableConfiguration.Custom, NameField = "name")]
    public class RemoteResource : Entity {
		private RemoteResourceSet resourceSet;

		//---------------------------------------------------------------------------------------------------------------------

		[EntityDataField("id_set")]
		public int ResourceSetId { get; protected set; }

		//---------------------------------------------------------------------------------------------------------------------

		[EntityEntityField("id_set")]
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
        public string Location { get; set; }

		//---------------------------------------------------------------------------------------------------------------------

		public RemoteResource(IfyContext context) : base(context) {}

		//---------------------------------------------------------------------------------------------------------------------

        public static RemoteResource FromId(IfyContext context, int id) {
            RemoteResource result = new RemoteResource(context);
            result.Id = id;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

		public static EntityList<RemoteResource> GetResources(IfyContext context, RemoteResourceSet resourceSet) {
			EntityList<RemoteResource> result = new EntityList<RemoteResource>(context);
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

