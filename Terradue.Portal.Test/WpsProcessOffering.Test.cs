using System;
using NUnit.Framework;
using System.Linq;
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

        [OneTimeSetUp]
        public override void FixtureSetup() {
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";
            context.AccessLevel = EntityAccessLevel.Administrator;

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

        private WpsProcessOffering CreateProcess(WpsProvider provider, string identifier, string name, bool available){
            WpsProcessOffering process = new WpsProcessOffering(context);
            process.Name = name;
            process.Description = name;
            process.RemoteIdentifier = identifier;
            process.Identifier = Guid.NewGuid().ToString();
            process.Url = provider.BaseUrl;
            process.Version = "1.0.0";
            process.Provider = provider;
            process.Available = available;
            return process;
        }

        private WpsProcessOffering CreateProcess(bool proxy){
            WpsProvider provider = CreateProvider("test-wps-"+proxy.ToString(), "test provider " + (proxy ? "p" : "np"), "http://dem.terradue.int:8080/wps/WebProcessingService", proxy);
            WpsProcessOffering process = CreateProcess(provider, "com.test.provider", "test provider " + (proxy ? "p" : "np"),true);
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
            WpsProcessOffering process = CreateProcess(provider, "com.test.provider.1", "test provider 1",false);
            process.Store();
            Assert.IsFalse(process.Available);
            WpsProvider provider2 = CreateProvider("test-wps-2","test provider 2", "http://dem.terradue.int:8080/wps/WebProcessingService", false);
            WpsProcessOffering process2 = CreateProcess(provider2, "com.test.provider.2", "test provider 2",false);
            process2.Store();
            Assert.IsFalse(process2.Available);
            EntityList<WpsProcessOffering> processes = provider.GetWpsProcessOfferings(false);
            Assert.That(processes.Count == 1);
            provider.Delete();
            provider2.Delete();
        }

        [Test]
        public void SearchWpsServicesByTags() {
            WpsProvider provider = CreateProvider("test-wps-search-1", "test provider 1", "http://dem.terradue.int:8080/wps/WebProcessingService", false);
            WpsProcessOffering process = CreateProcess(provider, "com.test.provider.1", "test provider 1",true);
            process.AddTag("mytag");
            process.AddTag("mytag1");
            process.AddTag("mytag2");
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
            
            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("tag", "tag,mytag");

            osr = ose.Query(services, parameters);
            Assert.AreEqual(0, osr.Items.Count());

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("tag", "mytag,mytag1");

            osr = ose.Query(services, parameters);
            Assert.AreEqual(1, osr.Items.Count());

            provider.Delete();
        }

        [Test]
        public void SearchWpsServicesByQ() {
            var uid = Guid.NewGuid().ToString();
            WpsProvider provider = CreateProvider(uid, "test provider", "http://gpod.eo.esa.int/wps?service=WPS&version=1.0.0&request=GetCapabilities", true);
            provider.UpdateProcessOfferings(true);

            EntityList<WpsProcessOffering> wpsProcessList = new EntityList<WpsProcessOffering>(context);
            wpsProcessList.Template.Provider = provider;
            wpsProcessList.Load();

            var nbprocesses = wpsProcessList.Items.Count();

            WpsProcessOffering service1 = wpsProcessList.Items.First();
            service1.Identifier = "searchbyQidentifier";
            service1.Name = "searchbyQname";
            service1.Description = "searchbyQdescription";
            service1.Store();

            EntityList<WpsProcessOffering> services = new EntityList<WpsProcessOffering>(context);

            var parameters = new NameValueCollection();
            parameters.Set("count", (nbprocesses + 1) + "");

            IOpenSearchResultCollection osr = ose.Query(services, parameters);
            parameters.Set("q", "searchbyNoQidentifier");
            osr = ose.Query(services, parameters);
            Assert.AreEqual(0, osr.Items.Count());

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("q", "searchbyQ");
            osr = ose.Query(services, parameters);
            Assert.AreEqual(1, osr.Items.Count());

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("q", "searchbyQidentifier");
            osr = ose.Query(services, parameters);
            Assert.AreEqual(1, osr.Items.Count());

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("q", "searchbyQname");
            osr = ose.Query(services, parameters);
            Assert.AreEqual(1, osr.Items.Count());

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("q", "searchbyQdescription");
            osr = ose.Query(services, parameters);
            Assert.AreEqual(1, osr.Items.Count());

            provider.Delete();
        }

        [Test]
        public void SearchWpsServicesByAvailability() {
            var uid = Guid.NewGuid().ToString();
            WpsProvider provider = CreateProvider(uid, "test provider", "http://gpod.eo.esa.int/wps?service=WPS&version=1.0.0&request=GetCapabilities", false);
            provider.UpdateProcessOfferings();

            EntityList<WpsProcessOffering> wpsProcessList = new EntityList<WpsProcessOffering>(context);
            wpsProcessList.Template.Provider = provider;
            wpsProcessList.Load();

            var nbprocesses = wpsProcessList.Items.Count();

            EntityList<WpsProcessOffering> services = new EntityList<WpsProcessOffering>(context);

            var parameters = new NameValueCollection();
            parameters.Set("count", (nbprocesses + 1) + "");

            IOpenSearchResultCollection osr = ose.Query(services, parameters);
            Assert.AreEqual(nbprocesses, osr.Items.Count());

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("available", "all");
            osr = ose.Query(services, parameters);
            Assert.AreEqual(nbprocesses, osr.Items.Count());

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("available", "false");
            osr = ose.Query(services, parameters);
            Assert.AreEqual(nbprocesses, osr.Items.Count());

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("available", "true");
            osr = ose.Query(services, parameters);
            Assert.AreEqual(0, osr.Items.Count());

            WpsProcessOffering service1 = wpsProcessList.Items.First();
            service1.Available = true;
            service1.Store();

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("available", "all");
            osr = ose.Query(services, parameters);
            Assert.AreEqual(nbprocesses, osr.Items.Count());

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("available", "false");
            osr = ose.Query(services, parameters);
            Assert.AreEqual(nbprocesses - 1, osr.Items.Count());

            services = new EntityList<WpsProcessOffering>(context);
            parameters.Set("available", "true");
            osr = ose.Query(services, parameters);
            Assert.AreEqual(1, osr.Items.Count());

            provider.Delete();
        }

        [Test]
        public void CreateWpsServiceFromRemote(){
            WpsProvider provider = CreateProvider("test-wps-1-remote", "test provider 1", "http://dem.terradue.int:8080/wps/WebProcessingService", false);
            List<WpsProcessOffering> services = provider.GetWpsProcessOfferingsFromRemote();
            Assert.AreEqual(1, services.Count());
            provider.Delete();
        }

        [Test]
        public void CreateWpsServiceFromRemoteWithTags() {
            WpsProvider provider = CreateProvider("test-wps-1-tag", "test provider 1", "http://dem.terradue.int:8080/wps/WebProcessingService", false);
            provider.AddTag("mytag");
            List<WpsProcessOffering> services = provider.GetWpsProcessOfferingsFromRemote();
            Assert.AreEqual(1, services.Count());
            Assert.True(!string.IsNullOrEmpty(services[0].Tags) && services[0].Tags.Contains("mytag"));
            provider.Delete();
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
            provider.Delete();
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
            provider.Delete();
        }

    }
}

