using System;
using NUnit.Framework;
using System.Linq;
using Mono.Addins;
using System.Collections.Specialized;
using Terradue.Portal;
using Terradue.ServiceModel.Ogc.OwsContext;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class WpsProcessOfferingTest : BaseTest{
    
        [TestFixtureSetUp]
        public void FixtureSetup(){
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";
        }

        private WpsProcessOffering CreateProcess(bool proxy){
            WpsProvider provider;
            provider = new WpsProvider(context);
            provider.Name = "test provider" + (proxy ? "proxy" : "no proxy");
            provider.BaseUrl = "http://dem.terradue.int:8080/wps/WebProcessingService";
            provider.Proxy = proxy;
            try{
                provider.Store();
            }catch(Exception e){
                throw e;
            }

            WpsProcessOffering process = new WpsProcessOffering(context);
            process.RemoteIdentifier = "com.test.provider";
            process.Identifier = Guid.NewGuid().ToString();
            process.Url = "http://dem.terradue.int:8080/wps/WebProcessingService";
            process.Version = "1.0.0";
            process.Provider = provider;

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

    }
}

