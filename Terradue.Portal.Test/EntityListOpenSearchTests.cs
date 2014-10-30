using System;
using NUnit.Framework;
using System.Linq;
using Mono.Addins;
using Terradue.OpenSearch.Engine;

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
            list.Identifier = "test";

            var osd = list.GetOpenSearchDescription();

            Assert.AreEqual("http://loacalhost:8877/sID/test/description", osd.Url.FirstOrDefault(p => p.Relation == "self").Template);
            Assert.AreEqual("http://loacalhost:8877/sID/test/search?count={count?}&startPage={startPage?}&startIndex={startIndex?}&q={searchTerms?}&lang={language?}&format=atom", osd.Url.FirstOrDefault(p => p.Relation == "search" && p.Type == "application/atom+xml").Template);

            Assert.AreEqual(context.GetConfigValue("CompanyName"), osd.Attribution);

        }
    }
}

