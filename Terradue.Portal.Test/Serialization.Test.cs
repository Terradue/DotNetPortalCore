using System;
using NUnit.Framework;
using OpenGis.Wps;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Xml.Linq;
using Terradue.ServiceModel.Ogc.OwsContext;

namespace Terradue.Portal.Test {
    [TestFixture()]
    public class SerializationTest : BaseTest{

        WpsProvider provider;

        [TestFixtureSetUp]
        public void FixtureSetup(){
            base.FixtureSetup();
            context.BaseUrl = "http://localhost:8080/api";
        }

        [Test()]
        public void SerializeCapabilities(){
            WPSCapabilitiesType capabilities = new WPSCapabilitiesType();
            capabilities.ServiceIdentification = new ServiceIdentification{ 
                Title = new List<LanguageStringType>{ new LanguageStringType{ Value = "Capabilities-Test-Title" } },
                Abstract = new List<LanguageStringType>{ new LanguageStringType{ Value = "Capabilities-Test-Abstract" } },
                Keywords = new List<KeywordsType>{ new KeywordsType{ Keyword = new List<LanguageStringType>{ new LanguageStringType{ Value = "Capabilities-Test-Keyword" } } } },
                ServiceType = new CodeType{ Value = "Capabilities-Test-ServiceType" },
                ServiceTypeVersion = new List<string>{ "Capabilities-Test-ServiceTypeVersion" },
                Fees = "Capabilities-Test-Fees",
                AccessConstraints  = new List<string>{ "Capabilities-Test-AccessConstraints" }
            };
            capabilities.ServiceProvider = new ServiceProvider{ 
                ProviderName = "Capabilities-Test-ProviderName",
                ProviderSite = new OnlineResourceType{ href="http://localhost:8080/test" }
            };
            capabilities.OperationsMetadata = new OperationsMetadata{
                Operation = new List<Operation>{
                    new Operation{
                        name = "GetCapabilities",
                        DCP = new List<DCP>{ new DCP{ Item = new HTTP{ Items = new List<OpenGis.Wps.RequestMethodType>{ new GetRequestMethodType{ href="https://geohazards-tep.eo.esa.int/t2api/wps/WebProcessingService" }}}}}
                    },
                    new Operation{
                        name = "DescribeProcess",
                        DCP = new List<DCP>{ new DCP{ Item = new HTTP{ Items = new List<OpenGis.Wps.RequestMethodType>{ new GetRequestMethodType{ href="https://geohazards-tep.eo.esa.int/t2api/wps/WebProcessingService" }}}}}
                    },
                    new Operation{
                        name = "Execute",
                        DCP = new List<DCP>{ new DCP{ Item = new HTTP{ Items = new List<OpenGis.Wps.RequestMethodType>{ 
                                        new GetRequestMethodType{ href="https://geohazards-tep.eo.esa.int/t2api/wps/WebProcessingService" },
                                        new PostRequestMethodType{ href="https://geohazards-tep.eo.esa.int/t2api/wps/WebProcessingService" }
                                    }}}}
                    }
                }
            };
            capabilities.ProcessOfferings = new ProcessOfferings{ 
                Process = new List<ProcessBriefType>{
                    new ProcessBriefType{
                        Identifier = new CodeType{ Value = "Capabilities-Test-Offering-Identifier" },
                        Title = new LanguageStringType{ Value = "Capabilities-Test-Offering-Title"},
                        Abstract = new LanguageStringType{ Value = "Capabilities-Test-Offering-Abstract"}
                    }
                }
            };

            MemoryStream stream = new MemoryStream();
            XmlSerializer serializer = new XmlSerializer(typeof(WPSCapabilitiesType));
            serializer.Serialize(stream, capabilities);
            stream.Seek(0, SeekOrigin.Begin);
            XDocument doc = XDocument.Load(stream);
            Assert.NotNull(doc.Element(XName.Get("Capabilities", WpsNamespaces.Wps)));
            Assert.NotNull(doc.Element(XName.Get("Capabilities", WpsNamespaces.Wps)).Element(XName.Get("OperationsMetadata", WpsNamespaces.Ows)));
            Assert.NotNull(doc.Element(XName.Get("Capabilities", WpsNamespaces.Wps)).Element(XName.Get("ProcessOfferings", WpsNamespaces.Wps)));
        }

        [Test()]
        public void Deserialize() {
            System.IO.FileStream atom = new System.IO.FileStream("../../Terradue.Portal/Schemas/examples/geohazards-capabilities.xml", System.IO.FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(WPSCapabilitiesType));
            WPSCapabilitiesType capabilities = (WPSCapabilitiesType)serializer.Deserialize(atom);

            Assert.AreEqual("Geohazard Tep WPS", capabilities.ServiceIdentification.Title[0].Value);
            Assert.AreEqual("WPS", capabilities.ServiceIdentification.ServiceType.Value);
            Assert.AreEqual(4, capabilities.ServiceIdentification.Keywords[0].Keyword.Count);
            Assert.AreEqual("Geohazards Tep", capabilities.ServiceProvider.ProviderName);
            Assert.AreEqual(3, capabilities.OperationsMetadata.Operation.Count);
            Assert.AreEqual(28, capabilities.ProcessOfferings.Process.Count);
            Assert.AreEqual("2ceb1e69-6ab2-4dab-9f7e-4a594924267c", capabilities.ProcessOfferings.Process[0].Identifier);
            Assert.AreEqual("ASAR PF", capabilities.ProcessOfferings.Process[0].Title);
            Assert.AreEqual("The ENVISAT ASAR PF is the ESA operational Level-1 processor developed by MDA. This processor, integrated on the ESA's Grid Processing On Demand , perform on-demand production of L1 products.", capabilities.ProcessOfferings.Process[0].Abstract);
        }
    }
}

