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
            Assert.NotNull(doc.Element(XName.Get("Capabilities", "http://www.opengis.net/wps/1.0.0")));
            Assert.NotNull(doc.Element(XName.Get("Capabilities", "http://www.opengis.net/wps/1.0.0")).Element(XName.Get("ProcessOfferings", "http://www.opengis.net/wps/1.0.0")));
        }

        public static string StreamToString(Stream stream)
        {
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        [Test()]
        public void Deserialize() {
            System.IO.FileStream atom = new System.IO.FileStream("../../Terradue.Portal/Schemas/examples/geohazards-capabilities.xml", System.IO.FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(WPSCapabilitiesType));
            WPSCapabilitiesType capabilities = (WPSCapabilitiesType)serializer.Deserialize(atom);

            Assert.AreEqual("Geohazard Tep WPS", capabilities.ServiceIdentification.Title[0].Value);
        }
    }
}

