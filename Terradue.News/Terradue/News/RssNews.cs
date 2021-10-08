using System;
using System.Collections.Generic;
using System.Xml;
using Terradue.Portal;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Schema;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Terradue.OpenSearch.Request;
using System.IO;
using System.Linq;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Engine;
using Terradue.ServiceModel.Syndication;

namespace Terradue.News {
    public class RssNews : Article, IOpenSearchable {

        public RssNews(IfyContext context) : base(context){}

        /// <summary>
        /// Froms the identifier.
        /// </summary>
        /// <returns>The identifier.</returns>
        /// <param name="context">Context.</param>
        /// <param name="id">Identifier.</param>
        public new static RssNews FromId(IfyContext context, int id){
            return (RssNews)Article.FromId(context, id);
        }

        /// <summary>
        /// Gets the rss feeds.
        /// </summary>
        /// <returns>The rss feeds.</returns>
        /// <param name="context">Context.</param>
        /// <param name="uri">URI.</param>
        public List<RssNews> GetFeeds()
        {
            List<RssNews> result = new List<RssNews>();
            if (!string.IsNullOrEmpty(this.Url))
            {
                var ff = new Rss20FeedFormatter(); // for Atom you can use Atom10FeedFormatter()
                var xr = XmlReader.Create(this.Url);
                ff.ReadFrom(xr);

                AtomFeed feed = new AtomFeed(ff.Feed);
                foreach (AtomItem item in feed.Items) {
                    RssNews rss = new RssNews(context);
                    rss.Title = item.Title.Text;
                    rss.Url = item.Links[0].Uri.AbsoluteUri;
                    rss.Content = item.Summary.Text;
                    rss.Time = item.PublishDate.DateTime;
                    if (item.Authors.Count > 0)
                        rss.Author = item.Authors[0].Name;
                    result.Add(rss);
                }           
            }
            return result;
        }

        AtomFeed GenerateAtomFeed(NameValueCollection parameters) {

            XmlReader xr;

            var ff = new Rss20FeedFormatter();
            AtomFeed feed = null;
            try{
                xr = XmlReader.Create(this.Url);    
                ff.ReadFrom(xr);

                feed = new AtomFeed(ff.Feed);
                var count = 0;
                foreach (AtomItem item in feed.Items) {
                    item.Content = item.Summary;
                    item.Categories.Add(new SyndicationCategory("rss"));
                    count++;
                }
                feed.TotalResults = count;
                feed.Language = null;
            } catch (Exception) {
                return null;
            }
            return feed;
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
            url.Path += "rss/"+this.Identifier+"/search";
            var array = (from key in parameters.AllKeys
                         from value in parameters.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            url.Query = string.Join("&", array);

            AtomOpenSearchRequest request = new AtomOpenSearchRequest(new OpenSearchUrl(url.ToString()), GenerateAtomFeed);

            return request;
        }

        public Terradue.OpenSearch.Schema.OpenSearchDescription GetOpenSearchDescription() {
            OpenSearchDescription OSDD = new OpenSearchDescription();

            OSDD.ShortName = "Terradue Catalogue";
            OSDD.Attribution = "European Space Agency";
            OSDD.Contact = "info@esa.int";
            OSDD.Developer = "Terradue GeoSpatial Development Team";
            OSDD.SyndicationRight = "open";
            OSDD.AdultContent = "false";
            OSDD.Language = "en-us";
            OSDD.OutputEncoding = "UTF-8";
            OSDD.InputEncoding = "UTF-8";
            OSDD.Description = "This Search Service performs queries in the available services of Tep QuickWin. There are several URL templates that return the results in different formats (RDF, ATOM or KML). This search service is in accordance with the OGC 10-032r3 specification.";

            OSDD.ExtraNamespace.Add("geo", "http://a9.com/-/opensearch/extensions/geo/1.0/");
            OSDD.ExtraNamespace.Add("time", "http://a9.com/-/opensearch/extensions/time/1.0/");
            OSDD.ExtraNamespace.Add("dct", "http://purl.org/dc/terms/");

            // The new URL template list 
            Hashtable newUrls = new Hashtable();
            UriBuilder urib;
            NameValueCollection query = new NameValueCollection();
            string[] queryString;

            urib = new UriBuilder(context.BaseUrl);
            urib.Path = String.Format("/{0}/search",this.Identifier);
            query.Add(this.GetOpenSearchParameters("application/atom+xml"));

            query.Set("format", "atom");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("application/atom+xml", new OpenSearchDescriptionUrl("application/atom+xml", urib.ToString(), "search", OSDD.ExtraNamespace));

            query.Set("format", "json");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("application/json", new OpenSearchDescriptionUrl("application/json", urib.ToString(), "search", OSDD.ExtraNamespace));

            query.Set("format", "html");
            queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
            urib.Query = string.Join("&", queryString);
            newUrls.Add("text/html", new OpenSearchDescriptionUrl("application/html", urib.ToString(), "search", OSDD.ExtraNamespace));

            OSDD.Url = new OpenSearchDescriptionUrl[newUrls.Count];

            newUrls.Values.CopyTo(OSDD.Url, 0);

            return OSDD;
        }

        public System.Collections.Specialized.NameValueCollection GetOpenSearchParameters(string mimeType) {
            return OpenSearchFactory.GetBaseOpenSearchParameter();
        }

        public long TotalResults {
            get {
                return 0;
            }
        }
            
        public OpenSearchUrl GetSearchBaseUrl(string mimeType) {
            return new OpenSearchUrl (string.Format("{0}/{1}/search", context.BaseUrl, "rss"));
        }

        public bool CanCache {
            get {
                return true;
            }
        }

        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType) {
        }
    }
}

