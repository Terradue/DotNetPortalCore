using System;
using NUnit.Framework;
using OpenGis.Wps;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Xml.Linq;

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
        public void DeserializeCapabilities() {
            System.IO.FileStream atom = new System.IO.FileStream("../../Terradue.Portal/Schemas/examples/geohazards-capabilities.xml", System.IO.FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(WPSCapabilitiesType));
            WPSCapabilitiesType capabilities = (WPSCapabilitiesType)serializer.Deserialize(atom);

            Assert.AreEqual("Geohazard Tep WPS", capabilities.ServiceIdentification.Title[0].Value);
            Assert.AreEqual("WPS", capabilities.ServiceIdentification.ServiceType.Value);
            Assert.AreEqual(4, capabilities.ServiceIdentification.Keywords[0].Keyword.Count);
            Assert.AreEqual("Geohazards Tep", capabilities.ServiceProvider.ProviderName);
            Assert.AreEqual(3, capabilities.OperationsMetadata.Operation.Count);
            Assert.AreEqual(28, capabilities.ProcessOfferings.Process.Count);
            Assert.AreEqual("2ceb1e69-6ab2-4dab-9f7e-4a594924267c", capabilities.ProcessOfferings.Process[0].Identifier.Value);
            Assert.AreEqual("ASAR PF", capabilities.ProcessOfferings.Process[0].Title.Value);
            Assert.AreEqual("The ENVISAT ASAR PF is the ESA operational Level-1 processor developed by MDA. This processor, integrated on the ESA's Grid Processing On Demand , perform on-demand production of L1 products.", capabilities.ProcessOfferings.Process[0].Abstract.Value);

            var stream = new MemoryStream();
            System.Xml.Serialization.XmlSerializerNamespaces ns = new System.Xml.Serialization.XmlSerializerNamespaces();
            ns.Add("wps", "http://www.opengis.net/wps/1.0.0");
            ns.Add("ows", "http://www.opengis.net/ows/1.1");
            ns.Add("xlink", "http://www.w3.org/1999/xlink");
            serializer.Serialize(stream, capabilities,ns);
            stream.Seek(0, SeekOrigin.Begin);
            string capabilitiesText;
            using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                capabilitiesText = reader.ReadToEnd();
            }
            Assert.IsNotNull(capabilitiesText);
        }

        [Test()]
        public void DeserializeDescribeProcess() {
            System.IO.FileStream atom = new System.IO.FileStream("../../Terradue.Portal/Schemas/examples/describeprocess.xml", System.IO.FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(ProcessDescriptions));
            ProcessDescriptions describe = (ProcessDescriptions)serializer.Deserialize(atom);

            Assert.AreEqual("com.terradue.wps_oozie.process.OozieAbstractAlgorithm", describe.ProcessDescription[0].Identifier.Value);
            Assert.AreEqual("SRTM Digital Elevation Model", describe.ProcessDescription[0].Title.Value);
            Assert.AreEqual(2, describe.ProcessDescription[0].DataInputs.Count);
            Assert.AreEqual("Level0_ref", describe.ProcessDescription[0].DataInputs[0].Identifier.Value);
            Assert.AreEqual("string", describe.ProcessDescription[0].DataInputs[0].LiteralData.DataType.Value);
            Assert.AreEqual("https://data.terradue.com/gs/catalogue/tepqw/gtfeature/search?format=json&uid=ASA_IM__0PNPAM20120407_082248_000001263113_00251_52851_2317.N1", describe.ProcessDescription[0].DataInputs[0].LiteralData.DefaultValue);
            Assert.AreEqual("format", describe.ProcessDescription[0].DataInputs[1].Identifier.Value);
            Assert.AreEqual("string", describe.ProcessDescription[0].DataInputs[1].LiteralData.DataType.Value);
            Assert.AreEqual("gamma", describe.ProcessDescription[0].DataInputs[1].LiteralData.DefaultValue);
            Assert.AreEqual(2, describe.ProcessDescription[0].ProcessOutputs.Count);
            Assert.AreEqual("result_distribution", describe.ProcessDescription[0].ProcessOutputs[0].Identifier.Value);
            Assert.True(describe.ProcessDescription[0].ProcessOutputs[0].Item is SupportedComplexDataType);
            Assert.AreEqual("result_osd", describe.ProcessDescription[0].ProcessOutputs[1].Identifier.Value);

            var stream = new MemoryStream();
            System.Xml.Serialization.XmlSerializerNamespaces ns = new System.Xml.Serialization.XmlSerializerNamespaces();
            ns.Add("wps", "http://www.opengis.net/wps/1.0.0");
            ns.Add("ows", "http://www.opengis.net/ows/1.1");
            ns.Add("xlink", "http://www.w3.org/1999/xlink");
            serializer.Serialize(stream, describe,ns);
            stream.Seek(0, SeekOrigin.Begin);
            string executeText;
            using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                executeText = reader.ReadToEnd();
            }
            Assert.IsNotNull(executeText);
        }

        [Test()]
        public void DeserializeExecute() {
            System.IO.FileStream atom = new System.IO.FileStream("../../Terradue.Portal/Schemas/examples/execute.xml", System.IO.FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(Execute));
            Execute execute = (Execute)serializer.Deserialize(atom);

            Assert.AreEqual("b4d3a590-c29c-46db-9b55-7e82cf74ab2e", execute.Identifier.Value);
            Assert.AreEqual(14, execute.DataInputs.Count);
            Assert.True(execute.DataInputs[1].Data.Item is BoundingBoxType);
            Assert.AreEqual("-54.58 35.532",((BoundingBoxType)execute.DataInputs[1].Data.Item).LowerCorner);
            Assert.AreEqual("-16.875 59.356",((BoundingBoxType)execute.DataInputs[1].Data.Item).UpperCorner);

            var stream = new MemoryStream();
            System.Xml.Serialization.XmlSerializerNamespaces ns = new System.Xml.Serialization.XmlSerializerNamespaces();
            ns.Add("wps", "http://www.opengis.net/wps/1.0.0");
            ns.Add("ows", "http://www.opengis.net/ows/1.1");
            ns.Add("xlink", "http://www.w3.org/1999/xlink");
            serializer.Serialize(stream, execute,ns);
            stream.Seek(0, SeekOrigin.Begin);
            string executeText;
            using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                executeText = reader.ReadToEnd();
            }
            Assert.IsNotNull(executeText);
        }

        [Test()]
        public void DeserializeExecuteResponse() {
            System.IO.FileStream atom = new System.IO.FileStream("../../Terradue.Portal/Schemas/examples/executeresponse.xml", System.IO.FileMode.Open);
            XmlSerializer serializer = new XmlSerializer(typeof(ExecuteResponse));
            ExecuteResponse execute = (ExecuteResponse)serializer.Deserialize(atom);

            Assert.AreEqual("com.terradue.wps_oozie.process.OozieAbstractAlgorithm", execute.Process.Identifier.Value);
            Assert.AreEqual("ADORE DORIS interferometric processor", execute.Process.Title.Value);
            Assert.True(execute.Status.Item is ProcessAcceptedType);

            var stream = new MemoryStream();

            System.Xml.Serialization.XmlSerializerNamespaces ns = new System.Xml.Serialization.XmlSerializerNamespaces();
            ns.Add("wps", "http://www.opengis.net/wps/1.0.0");
            ns.Add("ows", "http://www.opengis.net/ows/1.1");
            ns.Add("xlink", "http://www.w3.org/1999/xlink");
            serializer.Serialize(stream, execute,ns);
            stream.Seek(0, SeekOrigin.Begin);
            string executeText;
            using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                executeText = reader.ReadToEnd();
            }
            Assert.IsNotNull(executeText);
        }
    }
}

