using NUnit.Framework;
using System;
using System.Collections.Generic;
using OpenGis.Wps;

namespace Terradue.Portal.Test {

    [TestFixture]
    public class WpsProviderTest : BaseTest{

        WpsProvider provider;
        WpsProvider providerSecret;
        string processIdentifier, processVersion, processSecretIdentifier, processSecretVersion;

        [TestFixtureSetUp]
        public override void FixtureSetup(){
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";

            context.AccessLevel = EntityAccessLevel.Administrator;
            provider = new WpsProvider(context);
            provider.Identifier = "wpsprovider";
            provider.Name = "test-wpsprovider";
            provider.BaseUrl = "http://dem.terradue.int:8080/wps/WebProcessingService";
            provider.Proxy = false;

            providerSecret = new WpsProvider (context);
            providerSecret.Identifier = "wpsproviderSecret";
            providerSecret.Name = "test-wpsproviderSecret";
            providerSecret.BaseUrl = "http://urban1:qeF2pzNm@www.brockmann-consult.de/bc-wps/wps/calvalus";
            providerSecret.Proxy = true;

            processIdentifier = "com.terradue.wps_oozie.process.OozieAbstractAlgorithm";
            processVersion = "1.0.0";

            processSecretIdentifier = "urbantep-local~1.0~Subset";
            processSecretVersion = "1.0";

            provider.Store();
            provider.StoreProcessOfferings();

            providerSecret.Store();
            providerSecret.StoreProcessOfferings();
        }

        [Test]
        public void GetCapabilities() {
            var capabilities = provider.GetWPSCapabilities();
            Assert.That(capabilities.ProcessOfferings.Process.Count > 0);
        }

        [Test]
        public void DescribeProcess ()
        {
            var describe = provider.GetWPSDescribeProcess (processIdentifier, processVersion);
            Assert.That (describe.DataInputs.Count > 0 );
        }

        [Test]
        public void GetCapabilitiesSecret ()
        {
            var capabilities = providerSecret.GetWPSCapabilities ();
            Assert.That (capabilities.ProcessOfferings.Process.Count > 0);
        }

        [Test]
        public void DescribeProcessSecret ()
        {
            var describe = providerSecret.GetWPSDescribeProcess (processSecretIdentifier, processSecretVersion);
            Assert.That (describe.DataInputs.Count > 0);
        }

        [Test]
        public void GetProcessBriefTypes() {
            
            List<ProcessBriefType> process = provider.GetProcessBriefTypes();
            Assert.That(process.Count == 1);
            Assert.That(process[0].Title != null);
        }
    }
}

