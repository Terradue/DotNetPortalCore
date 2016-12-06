using System;
using System.Collections.Specialized;
using NUnit.Framework;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;

namespace Terradue.Portal.Test
{

    [TestFixture]
    public class WpsProcessOfferingTest : BaseTest{
    
        [TestFixtureSetUp]
        public void FixtureSetup(){
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";
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

    }
}

