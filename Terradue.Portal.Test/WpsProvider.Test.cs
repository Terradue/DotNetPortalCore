using NUnit.Framework;
using System;
using System.Collections.Generic;
using OpenGis.Wps;

namespace Terradue.Portal.Test {

    [TestFixture()]
    public class WpsProviderTest : BaseTest{

        WpsProvider provider;

        [TestFixtureSetUp]
        public void FixtureSetup(){
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";

            provider = new WpsProvider(context);
            provider.Name = "test-wpsprovider";
            provider.BaseUrl = "http://dem.terradue.int:8080/wps/WebProcessingService";
            provider.Proxy = false;
        }

        [Test()]
        public void GetCapabilities() {
            WPSCapabilitiesType capabilities = WpsProvider.GetWPSCapabilitiesFromUrl(provider.BaseUrl);
            Assert.That(capabilities.ProcessOfferings.Process.Count > 0);
        }

        [Test()]
        public void StoreProcessOffering() {
            provider.Store();
            provider.StoreProcessOfferings();
            List<ProcessBriefType> process = provider.GetProcessOfferings();
            Assert.That(process.Count == 1);
            Assert.That(process[0].Title != null);
        }
    }
}

