using System;
using System.Linq;
using Terradue.OpenSearch;
using System.Collections.Specialized;
using Terradue.OpenSearch.Engine.Extensions;
using System.Web;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.ServiceModel.Syndication;

namespace Terradue.Portal {
    [EntityTable("multiseries", EntityTableConfiguration.Full, HasExtensions = true, HasPermissionManagement = true)]
    public class MultiSeries : Entity, IOpenSearchable {

        private EntityList<Series> series;

        private OpenSearchEngine ose;

        public MultiSeries(IfyContext context) : base(context) {

            ose = new OpenSearchEngine();
            AtomOpenSearchEngineExtension aosee = new AtomOpenSearchEngineExtension();
            //Type type = aosee.GetTransformType();
            ose.RegisterExtension((OpenSearchEngineExtension<AtomFeed>)aosee);
            
            // series = new EntityList<Terradue.Portal.Series>(context, this); // OLD
            series = new EntityList<Terradue.Portal.Series>(context); // NEW - TODO: check whether this works
            // series.Template.ParentSeries = this;
        }

        [EntityDataField("description")]
        public string Description { get; set; }

        public EntityList<Series> Series {
            get {
                return series;
            }
        }

        public void Add(Series series) {
            this.series.Include(series);
        }

        private NameValueCollection MergeSeriesOpenSearchParameters() {

            NameValueCollection nvc = new NameValueCollection();

            foreach (Series s in series) {
                IOpenSearchEngineExtension osee = ose.GetFirstExtensionByTypeAbility(typeof(AtomFeed));
                //string type = OpenSearchFactory.GetBestQuerySettingsByNumberOfParam(s, osee).PreferredContentType;
                if (osee == null) {
                    context.LogError(this, "MultiSeries [{0}] : Series [{0}] does not expose a valid search template for transforming to Atom. Skipping it");
                    continue;
                }
                NameValueCollection nvc2 = s.GetOpenSearchParameters(s.DefaultMimeType);
                int i = 1;
                foreach (string param in nvc2.Keys) {

                    if (nvc2[param] == nvc[param]) continue;

                    if (nvc[param] == null) {
                        nvc.Add(param,nvc2[param]);
                        continue;
                    }

                    if (nvc[param] != null) {
                        nvc.Add(param + i++,nvc2[param]);
                        continue;
                    }

                }

            }

            return nvc;

        }

        #region IOpenSearchable implementation
       
        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType) {
        }

        public OpenSearchUrl GetSearchBaseUrl(string mimeType) {
            return new OpenSearchUrl (string.Format("{0}/virtual/{1}/search", context.BaseUrl, this.Identifier));
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

        public OpenSearchRequest Create(QuerySettings querySettings, NameValueCollection parameters) {

            UriBuilder url = new UriBuilder(context.BaseUrl);
            url.Path += "/series/" + this.Identifier;
            var array = (from key in parameters.AllKeys
                from value in parameters.GetValues(key)
                select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            url.Query = string.Join("&", array);
            OpenSearchableFactorySettings settings = new OpenSearchableFactorySettings(ose) { Credentials = querySettings.Credentials };
            settings.MergeFilters = Terradue.Metadata.EarthObservation.Helpers.GeoTimeOpenSearchHelper.MergeGeoTimeFilters;
            return new MultiOpenSearchRequest<AtomFeed, AtomItem>(settings, Series.ToArray(), querySettings.PreferredContentType, new OpenSearchUrl(url.ToString()), true, this);
        }

        public OpenSearchDescription GetOpenSearchDescription() {

            OpenSearchDescription osd = new OpenSearchDescription();
            osd.ShortName = this.Name;
            osd.Contact = context.GetConfigValue("CompanyEmail");
            osd.Description = this.Description;
            osd.SyndicationRight = "open";
            osd.AdultContent = "false";
            osd.Language = "en-us";
            osd.OutputEncoding = "UTF-8";
            osd.InputEncoding = "UTF-8";
            osd.Developer = "Terradue GeoSpatial Development Team";
            osd.Attribution = context.GetConfigValue("CompanyName");

            osd.ExtraNamespace.Add("geo", "http://a9.com/-/opensearch/extensions/geo/1.0/");
            osd.ExtraNamespace.Add("time", "http://a9.com/-/opensearch/extensions/time/1.0/");
            osd.ExtraNamespace.Add("dct", "http://purl.org/dc/terms/");

            // Create the union Link

            OpenSearchDescriptionUrl url = new OpenSearchDescriptionUrl("application/atom+xml", "dummy://dummy" , "results", osd.ExtraNamespace);

            osd.Url = new OpenSearchDescriptionUrl[1];
            osd.Url[0] = url;

            return osd;

        }

        public NameValueCollection GetOpenSearchParameters(string mimeType) {
            if (mimeType != "application/atom+xml") return null;
            return this.MergeSeriesOpenSearchParameters();
        }

        public long TotalResults {
            get {
                if (this.Series != null) {
                    OpenSearchEngine ose = new OpenSearchEngine();
                    AtomOpenSearchEngineExtension aosee = new AtomOpenSearchEngineExtension();
                    ose.RegisterExtension(aosee);

                    try {

                        AtomFeed osr = (AtomFeed)ose.Query(this, new NameValueCollection(), typeof(AtomFeed));
                        return osr.TotalResults;

                    } catch (Exception) {
                        // no error managment, set the number of product to 0
                        return 0;
                    }

                }
                return 0;
            }
        }

        public virtual bool CanCache {
            get { return false; }
        }

        #endregion

    }
}

