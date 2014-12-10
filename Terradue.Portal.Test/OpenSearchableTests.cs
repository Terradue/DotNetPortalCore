using System;
using NUnit.Framework;
using Terradue.OpenSearch.Schema;
using System.Linq;
using Mono.Addins;
using Terradue.OpenSearch.Engine;
using System.Collections.Specialized;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class OpenSearchableTests : BaseTest{

        OpenSearchEngine ose;

        [TestFixtureSetUp]
        public void FixtureSetup(){

            base.FixtureSetup();

            AddinManager.Initialize();
            AddinManager.Registry.Update(null);

            ose = new OpenSearchEngine();
            ose.LoadPlugins();

            context.BaseUrl = "http://loacalhost:8877/sID";
        }

        public void RemoteResourceIProxiedOpenSearchable() {

            RemoteResourceSet set = new RemoteResourceSet(context);

            RemoteResource rr = new RemoteResource(context);

            rr.ResourceSet = set;
            set.Resources = new EntityList<RemoteResource>(context);
            set.Resources.Include(rr);
            rr.Location = "http://catalogue.terradue.int/catalogue/search/MER_FRS_1P/rdf?startIndex=0&q=MER_FRS_1P&start=1992-01-01&stop=2014-10-24&bbox=-72,47,-57,58";
            set.OpenSearchEngine = ose;
            set.Identifier = "test";

            OpenSearchDescription osd = set.GetProxyOpenSearchDescription();

            OpenSearchDescriptionUrl url = osd.Url.FirstOrDefault(p => p.Relation == "self");

            Assert.That(url != null);
            Assert.AreEqual("http://loacalhost:8877/sID/remoteresource/test/description", url.Template);

            NameValueCollection nvc = new NameValueCollection();
            nvc.Set("count", "100");

            var osr = ose.Query(set, nvc, "rdf");

            Assert.LessOrEqual(osr.Result.Count, 100);


        }
    }
}

