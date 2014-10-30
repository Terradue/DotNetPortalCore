using NUnit.Framework;
using System;
using System.Collections.Generic;
using OpenGis.Wps;

namespace Terradue.Portal.Test {

    [TestFixture()]
    public class WpsProviderTest {

        WpsProvider provider;
        WPSCapabilitiesType capabilities;

        [SetUp]
        public void Setup(){
            IfyWebContext context = new IfyWebContext(PagePrivileges.EverybodyView);
            provider = new WpsProvider(context);
            provider.Name = "test-wpsprovider";
            provider.BaseUrl = "http://dem.terradue.int:8080/wps/WebProcessingService?service=wps&request=getCapabilities";
        }

        [Test()]
        public void GetCapabilities() {
            capabilities = WpsProvider.GetWPSCapabilitiesFromUrl(provider.BaseUrl);
            Assert.That(capabilities.ProcessOfferings.Process.Count > 0);
        }

        [Test()]
        public void GetDescribeProcess() {
            List<ProcessBriefType> process = provider.GetProcessOfferings();
            Assert.That(process.Count == 1);
            Assert.That(process[0].Title != null);
        }
    }
}

