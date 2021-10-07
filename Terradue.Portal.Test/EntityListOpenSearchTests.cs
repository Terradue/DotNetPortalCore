using System;
using NUnit.Framework;
using System.Linq;
using Terradue.OpenSearch.Engine;
using System.Collections.Specialized;
using Terradue.ServiceModel.Syndication;
using Terradue.OpenSearch.Response;
using System.Xml;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class EntityListOpenSearchTests : BaseTest {

        OpenSearchEngine ose;

        [OneTimeSetUp]
        public new void FixtureSetup(){

            base.FixtureSetup();

            ose = new OpenSearchEngine();
            ose.LoadPlugins();

            context.BaseUrl = "http://loacalhost:8877/sID";
        }

        [Test]
        public void CheckEntityListOsd() {

            EntityList<Service> list = new EntityList<Service>(context);
            list.OpenSearchEngine = ose;
            list.Identifier = "services";

            var osd = list.GetOpenSearchDescription();
            var descriptionTemplate = osd.Url.FirstOrDefault(p => p.Relation == "self").Template;
            Assert.AreEqual("http://loacalhost:8877/sID/services/description", descriptionTemplate);
            var searchTemplate = osd.Url.FirstOrDefault(p => p.Relation == "search" && p.Type == "application/atom+xml").Template;
            Assert.AreEqual("http://loacalhost:8877/sID/services/search?count={count?}&startPage={startPage?}&startIndex={startIndex?}&q={searchTerms?}&lang={language?}&id={t2:uid?}&sl={t2:sl?}&disableCache={t2:cache?}&domain={t2:domain?}&author={t2:author?}&visibility={t2:visibility?}&correlatedTo={cor:with?}&format=atom", searchTemplate);
            Assert.AreEqual(context.GetConfigValue("CompanyName"), osd.Attribution);
        }

        //[Test]
        public void CheckEntityListPaginated() {

            EntityList<Job> list = new EntityList<Job>(context);
            list.OpenSearchEngine = ose;
            list.Identifier = "test";

            for ( int i = 1 ; i <=10; i++){
                list.Include(new Job(context){ Name = "" + i });
            }

            var nvc = new NameValueCollection();
            var request = (AtomOpenSearchRequest)list.Create(new QuerySettings("application/atom+xml", ose.GetExtensionByExtensionName("atom").ReadNative) , nvc);
            AtomFeed feed = (AtomFeed)request.GetResponse().GetResponseObject();

            Assert.That(feed.Items.First().Title.Text == "1");
            Assert.That(feed.Items.Last().Title.Text == "10");

            nvc.Add("startIndex", "2");
            request = (AtomOpenSearchRequest)list.Create(new QuerySettings("application/atom+xml", ose.GetExtensionByExtensionName("atom").ReadNative) , nvc);
            feed = (AtomFeed)request.GetResponse().GetResponseObject();

            Assert.That(feed.Items.First().Title.Text == "2");
            Assert.That(feed.Items.Last().Title.Text == "10");

            nvc.Remove("startIndex");
            nvc.Add("q", "4");
            request = (AtomOpenSearchRequest)list.Create(new QuerySettings("application/atom+xml", ose.GetExtensionByExtensionName("atom").ReadNative), nvc);
            feed = (AtomFeed)request.GetResponse().GetResponseObject();

            Assert.That(feed.Items.First().Title.Text == "4");
            Assert.That(feed.Items.Last().Title.Text == "4");

        }
    }
}

