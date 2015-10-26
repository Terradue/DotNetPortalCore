using System;
using Terradue.OpenSearch;
using System.Collections.Generic;
using Terradue.OpenSearch.Schema;
using Terradue.OpenSearch.Engine;

namespace Terradue.Portal.OpenSearch {
    public class SmartGenericOpenSearchable : GenericOpenSearchable {

        static Dictionary<string,OpenSearchDescription> smartOpenSearchDescriptionCache = new Dictionary<string, OpenSearchDescription>();

        //
        // Constructors
        //
        public SmartGenericOpenSearchable(OpenSearchDescription osd, OpenSearchEngine ose) : base(osd, ose) {
        }

        public SmartGenericOpenSearchable(OpenSearchUrl url, OpenSearchEngine ose) : base(new OpenSearchDescription(), ose) {

            base.url = url;
            base.ose = ose;
            base.osd = FindSmartOpenSearchDescription();

        }

        public new OpenSearchDescription GetOpenSearchDescription() {
            if (base.osd == null) {
                base.osd = FindSmartOpenSearchDescription();
            }
            return base.osd;
        }

        private OpenSearchDescription FindSmartOpenSearchDescription(){

            UriBuilder baseUri = new UriBuilder(this.url.ToString());
            baseUri.Query = "";

            string baseUrl = baseUri.ToString();

            if (smartOpenSearchDescriptionCache.ContainsKey(baseUrl))
                return smartOpenSearchDescriptionCache[baseUrl];

            base.osd = this.ose.AutoDiscoverFromQueryUrl(base.url);
            if (base.osd != null)
                smartOpenSearchDescriptionCache.Add(baseUrl, base.osd);

            return base.osd;
        }
    }
}

