using System;
using NUnit.Framework;
using System.Linq;
using Mono.Addins;
using System.Collections.Specialized;
using NUnit.Framework;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Result;
using System.Collections.Generic;

namespace Terradue.Portal.Test
{

    [TestFixture]
    public class WpsProcessOfferingTest : BaseTest{

        OpenSearchEngine ose;

        [TestFixtureSetUp]
        public override void FixtureSetup() {
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";
            context.AccessLevel = EntityAccessLevel.Administrator;

            AddinManager.Initialize();
            AddinManager.Registry.Update(null);

            ose = new OpenSearchEngine();
            ose.LoadPlugins();
        }

        private WpsProvider CreateProvider(string identifier, string name, string url, bool proxy){
            WpsProvider provider;
            provider = new WpsProvider(context);
            provider.Identifier = identifier;
            provider.Name = name;
            provider.Description = name;
            provider.BaseUrl = url;
            provider.Proxy = proxy;
            try{
                provider.Store();
            }catch(Exception e){
                throw e;
            }
            return provider;
        }

        private WpsProcessOffering CreateProcess(WpsProvider provider, string identifier, string name){
            WpsProcessOffering process = new WpsProcessOffering(context);
            process.Name = name;
            process.Description = name;
            process.RemoteIdentifier = identifier;
            process.Identifier = Guid.NewGuid().ToString();
            process.Url = provider.BaseUrl;
            process.Version = "1.0.0";
            process.Provider = provider;
            return process;
        }

        private WpsProcessOffering CreateProcess(bool proxy){
            WpsProvider provider = CreateProvider("test-wps-"+proxy.ToString(), "test provider " + (proxy ? "p" : "np"), "http://dem.terradue.int:8080/wps/WebProcessingService", proxy);
            WpsProcessOffering process = CreateProcess(provider, "com.test.provider", "test provider " + (proxy ? "p" : "np"));
            return process;
        }

        [Test]
        public void GetAtomItemWithProxy() {
            WpsProcessOffering process = CreateProcess(true);
            var entry = process.ToAtomItem(new NameValueCollection());
            OwsContextAtomEntry result = new OwsContextAtomEntry(entry);
            Assert.That(result != null);
        }

        [Test]
        public void GetAtomItemWithoutProxy() {
            WpsProcessOffering process = CreateProcess(false);
            var entry = process.ToAtomItem(new NameValueCollection());
            OwsContextAtomEntry result = new OwsContextAtomEntry(entry);
            Assert.That(result != null);
        }

        [Test]
        public void GetProcessOfferingEntityList(){
            WpsProvider provider = CreateProvider("test-wps-1","test provider 1", "http://dem.terradue.int:8080/wps/WebProcessingService", false);
            WpsProcessOffering process = CreateProcess(provider, "com.test.provider.1", "test provider 1");
            process.Store();
            Assert.IsFalse(process.Available);
            WpsProvider provider2 = CreateProvider("test-wps-2","test provider 2", "http://dem.terradue.int:8080/wps/WebProcessingService", false);
            WpsProcessOffering process2 = CreateProcess(provider2, "com.test.provider.2", "test provider 2");
            process2.Store();
            Assert.IsFalse(process2.Available);
            EntityList<WpsProcessOffering> processes = provider.GetWpsProcessOfferings(false);
            Assert.That(processes.Count == 1);
        }

        [Test]
        public void SearchWpsServicesByTags() {
            //context.AccessLevel = EntityAccessLevel.Privilege;
            //var usr1 = User.FromUsername(context, "testusr1");

            WpsProvider provider = CreateProvider("test-wps-search-1", "test provider 1", "http://dem.terradue.int:8080/wps/WebProcessingService", false);
            WpsProcessOffering process = CreateProcess(provider, "com.test.provider.1", "test provider 1");
            process.AddTag("mytag");
            process.Store();

            EntityList<WpsProcessOffering> services = new EntityList<WpsProcessOffering>(context);

            var parameters = new NameValueCollection();
            parameters.Set("tag", "mytag");

            IOpenSearchResultCollection osr = ose.Query(services, parameters);
            Assert.AreEqual(1, osr.Items.Count());

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("tag", "tag");

            osr = ose.Query(services, parameters);
            Assert.AreEqual(0, osr.Items.Count());

        }

        [Test]
        public void CreateWpsServiceFromRemote(){
            WpsProvider provider = CreateProvider("test-wps-1-remote", "test provider 1", "http://dem.terradue.int:8080/wps/WebProcessingService", false);
            List<WpsProcessOffering> services = provider.GetWpsProcessOfferingsFromRemote();
            Assert.AreEqual(1, services.Count());
        }

        [Test]
        public void CreateWpsServiceFromRemoteWithTags() {
            WpsProvider provider = CreateProvider("test-wps-1-tag", "test provider 1", "http://dem.terradue.int:8080/wps/WebProcessingService", false);
            provider.AddTag("mytag");
            List<WpsProcessOffering> services = provider.GetWpsProcessOfferingsFromRemote();
            Assert.AreEqual(1, services.Count());
            Assert.True(!string.IsNullOrEmpty(services[0].Tags) && services[0].Tags.Contains("mytag"));
        }

        [Test]
        public void CreateWpsServiceFromRemoteWithDomain() {
            Domain domainpub = new Domain(context);
            domainpub.Identifier = "domainPublic";
            domainpub.Kind = DomainKind.Public;
            domainpub.Store();

            WpsProvider provider = CreateProvider("test-wps-1-domain", "test provider 1", "http://dem.terradue.int:8080/wps/WebProcessingService", false);
            provider.Domain = domainpub;
            List<WpsProcessOffering> services = provider.GetWpsProcessOfferingsFromRemote();
            Assert.AreEqual(1, services.Count());
            Assert.True(services[0].DomainId == domainpub.Id);
        }

        [Test]
        public void UpdateProcessOfferings() {
            WpsProvider provider = CreateProvider("test-wps-1-sync", "test provider 1", "http://dem.terradue.int:8080/wps/WebProcessingService", false);
            provider.AddTag("mytag1");
            List<WpsProcessOffering> services = provider.GetWpsProcessOfferingsFromRemote();
            Assert.AreEqual(1, services.Count());
            var service = services[0];
            foreach (var s in services) s.Store();
            Assert.True(!string.IsNullOrEmpty(service.Tags) && service.Tags.Contains("mytag1"));
            provider.AddTag("mytag2");//mytag2 is added only for new services
            provider.UpdateProcessOfferings();
            Assert.True(!string.IsNullOrEmpty(service.Tags) && service.Tags.Contains("mytag1") && !service.Tags.Contains("mytag2"));
            service.Delete();
            provider.UpdateProcessOfferings();
            EntityList<WpsProcessOffering> dbProcesses = provider.GetWpsProcessOfferings(false);
            Assert.AreEqual(1, dbProcesses.Count());
            service = dbProcesses.Items.First();
            Assert.True(!string.IsNullOrEmpty(service.Tags) && service.Tags.Contains("mytag1") && service.Tags.Contains("mytag2"));
        }

    }
}

