using System;
using NUnit.Framework;
using System.Linq;
using Mono.Addins;
using Terradue.OpenSearch.Engine;
using System.Collections.Specialized;
using Terradue.ServiceModel.Syndication;
using Terradue.OpenSearch.Response;
using System.Xml;
using Terradue.OpenSearch.Request;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class EntityListOpenSearchTests : BaseTest {

        OpenSearchEngine ose;

        [TestFixtureSetUp]
        public new void FixtureSetup(){

            base.FixtureSetup();

            AddinManager.Initialize();
            AddinManager.Registry.Update(null);

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

            Assert.AreEqual("http://loacalhost:8877/sID/services/description", osd.Url.FirstOrDefault(p => p.Relation == "self").Template);
            Assert.AreEqual("http://loacalhost:8877/sID/services/search?count={count?}&startPage={startPage?}&startIndex={startIndex?}&q={searchTerms?}&lang={language?}&format=atom", osd.Url.FirstOrDefault(p => p.Relation == "search" && p.Type == "application/atom+xml").Template);

            Assert.AreEqual(context.GetConfigValue("CompanyName"), osd.Attribution);

        }

        [Test]
        public void CheckEntityListPaginated() {

            EntityList<Job> list = new EntityList<Job>(context);
            list.OpenSearchEngine = ose;
            list.Identifier = "test";

            for ( int i = 1 ; i <=10; i++){
                list.Include(new Job(context){ Name = "" + i });
            }

            var nvc = new NameValueCollection();
            var request = (AtomOpenSearchRequest)list.Create("application/atom+xml", nvc);
            SyndicationFeed feed = (SyndicationFeed)request.GetResponse().GetResponseObject();

            Assert.That(feed.Items.First().Title.Text == "1");
            Assert.That(feed.Items.Last().Title.Text == "10");

            nvc.Add("startIndex", "2");
            request = (AtomOpenSearchRequest)list.Create("application/atom+xml", nvc);
            feed = (SyndicationFeed)request.GetResponse().GetResponseObject();

            Assert.That(feed.Items.First().Title.Text == "2");
            Assert.That(feed.Items.Last().Title.Text == "10");

            nvc.Remove("startIndex");
            nvc.Add("q", "4");
            request = (AtomOpenSearchRequest)list.Create("application/atom+xml", nvc);
            feed = (SyndicationFeed)request.GetResponse().GetResponseObject();

            Assert.That(feed.Items.First().Title.Text == "4");
            Assert.That(feed.Items.Last().Title.Text == "4");

        }
    }
}

