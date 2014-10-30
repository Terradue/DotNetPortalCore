/// <remarks/>
/// <remarks>
///gSOAP 2.7.0e generated service definition
///</remarks>

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Services.Protocols;
using System.Xml;
using Terradue.Util;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    [System.Web.Services.WebServiceBinding(Name="wsServer", Namespace="http://engine.terradue.com:8080/wsServer.wsdl")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class wsServerClient : System.Web.Services.Protocols.SoapHttpClientProtocol {
        
        public wsServerClient(string url) {
            this.Url = url;
        }
        

        public string createTask(string user, string password, string myProxyServer, string descr, string[] @params) {
            // TODO: Use Mono standard serialisation instead of the manually creating the SOAP XML request message (SubmitExt())
            StringWriter stringWriter = new StringWriter();
            MonoXmlWriter writer = MonoXmlWriter.Create(stringWriter);
            //XmlWriter writer = XmlWriter.Create(new StreamWriter("/Users/floeschau/Programming/ify.mono/release/ws-test-client/wsServer_createTask_5.xml"));
            writer.WriteStartDocument();
    
            writer.WriteStartElement("soap", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            writer.WriteNamespaceDefinition("xsd", "http://www.w3.org/2001/XMLSchema");
            writer.WriteNamespaceDefinition("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteNamespaceDefinition("enc", "http://schemas.xmlsoap.org/soap/encoding/");
            writer.WriteNamespaceDefinition("wss", "http://tempuri.org/eo.gridEngine.frontEnd.ContactService");
            
            writer.WritePrefixStartElement("soap", "Body");
            writer.WriteAttributeString("soap", "encodingStyle", "http://schemas.xmlsoap.org/soap/encoding/");
            
            writer.WritePrefixStartElement("wss", "createTask");
    
            writer.WriteElementString("user", user);
            writer.WriteElementString("password", password);
            writer.WriteElementString("myProxyServer", myProxyServer);
            writer.WriteElementString("descr", descr);
            
            writer.WriteStartElement("params");
            writer.WritePrefixAttributeString("xsi", "type", "xsd:string");
            writer.WritePrefixAttributeString("enc", "arrayType", "xsd:string[" + @params.Length + "]");
            writer.WritePrefixAttributeString("enc", "offset", "[0]");
            for (int i = 0; i < @params.Length; i++) writer.WriteElementString("item", @params[i]);
            writer.WriteEndElement(); // </params>
            
            writer.WriteEndElement(); // </wss:createTask>
            
            writer.WriteEndElement(); // </soap:Body>
            
            writer.WriteEndElement(); // </soap:Envelope>
            
            writer.WriteEndDocument();
            writer.Close();
            stringWriter.Close();
            
            return ReceiveSoapResponse("createTask", "result", stringWriter.ToString());  
        }

        [System.Web.Services.Protocols.SoapRpcMethodAttribute("createTask", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string createTask(string user, string password, string myProxyServer, string descr, ArrayOfString @params) {
            object[] results = this.Invoke("createTask", new object[] {
                        user,
                        password,
                        myProxyServer,
                        descr,
                        @params});
            return ((string)(results[0]));
        }
        

        public string insertJob(string sessionID, string jobType, string jobName, string[] jobParentID, string[] @params) {
            // TODO: Use Mono standard serialisation instead of the manually creating the SOAP XML request message (SubmitExt())
            StringWriter stringWriter = new StringWriter();
            MonoXmlWriter writer = MonoXmlWriter.Create(stringWriter);
            //XmlWriter writer = XmlWriter.Create(new StreamWriter("/Users/floeschau/Programming/ify.mono/release/ws-test-client/wsServer_createTask_5.xml"));
            writer.WriteStartDocument();
    
            writer.WriteStartElement("soap", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            writer.WriteNamespaceDefinition("xsd", "http://www.w3.org/2001/XMLSchema");
            writer.WriteNamespaceDefinition("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteNamespaceDefinition("enc", "http://schemas.xmlsoap.org/soap/encoding/");
            writer.WriteNamespaceDefinition("wss", "http://tempuri.org/eo.gridEngine.frontEnd.ContactService");
            
            writer.WritePrefixStartElement("soap", "Body");
            writer.WriteAttributeString("soap", "encodingStyle", "http://schemas.xmlsoap.org/soap/encoding/");
            
            writer.WritePrefixStartElement("wss", "insertJob");
    
            writer.WriteElementString("sessionID", sessionID);
            writer.WriteElementString("jobType", jobType);
            writer.WriteElementString("jobName", jobName);
            
            writer.WriteStartElement("jobParentID");
            writer.WritePrefixAttributeString("xsi", "type", "xsd:string");
            writer.WritePrefixAttributeString("enc", "arrayType", "xsd:string[" + jobParentID.Length + "]");
            writer.WritePrefixAttributeString("enc", "offset", "[0]");
            for (int i = 0; i < jobParentID.Length; i++) writer.WriteElementString("item", jobParentID[i]);
            if (jobParentID.Length == 0) writer.WriteElementString("item", "");
            writer.WriteEndElement(); // </jobParentID>

            writer.WriteStartElement("params");
            writer.WritePrefixAttributeString("xsi", "type", "xsd:string");
            writer.WritePrefixAttributeString("enc", "arrayType", "xsd:string[" + @params.Length + "]");
            writer.WritePrefixAttributeString("enc", "offset", "[0]");
            for (int i = 0; i < @params.Length; i++) writer.WriteElementString("item", @params[i]);
            if (@params.Length == 0) writer.WriteElementString("item", "");
            writer.WriteEndElement(); // </params>
            
            writer.WriteEndElement(); // </wss:insertJob>
            
            writer.WriteEndElement(); // </soap:Body>
            
            writer.WriteEndElement(); // </soap:Envelope>
            
            writer.WriteEndDocument();
            writer.Close();
            stringWriter.Close();
            
            return ReceiveSoapResponse("insertJob", "result", stringWriter.ToString());  
        }

        [System.Web.Services.Protocols.SoapRpcMethodAttribute("insertJob", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string insertJob(string sessionID, string jobType, string jobName, ArrayOfString jobParentID, ArrayOfString @params) {
            object[] results = this.Invoke("insertJob", new object[] {
                        sessionID,
                        jobType,
                        jobName,
                        jobParentID,
                        @params});
            return ((string)(results[0]));
        }
        
        public string modifyJob(string sessionID, string jobName, string[] @params) {
            // TODO: Use Mono standard serialisation instead of the manually creating the SOAP XML request message (SubmitExt())
            StringWriter stringWriter = new StringWriter();
            MonoXmlWriter writer = MonoXmlWriter.Create(stringWriter);
            //XmlWriter writer = XmlWriter.Create(new StreamWriter("/Users/floeschau/Programming/ify.mono/release/ws-test-client/wsServer_createTask_5.xml"));
            writer.WriteStartDocument();
    
            writer.WriteStartElement("soap", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            writer.WriteNamespaceDefinition("xsd", "http://www.w3.org/2001/XMLSchema");
            writer.WriteNamespaceDefinition("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteNamespaceDefinition("enc", "http://schemas.xmlsoap.org/soap/encoding/");
            writer.WriteNamespaceDefinition("wss", "http://tempuri.org/eo.gridEngine.frontEnd.ContactService");
            
            writer.WritePrefixStartElement("soap", "Body");
            writer.WriteAttributeString("soap", "encodingStyle", "http://schemas.xmlsoap.org/soap/encoding/");
            
            writer.WritePrefixStartElement("wss", "modifyJob");
    
            writer.WriteElementString("sessionID", sessionID);
            writer.WriteElementString("jobName", jobName);
            
            writer.WriteStartElement("params");
            writer.WritePrefixAttributeString("xsi", "type", "xsd:string");
            writer.WritePrefixAttributeString("enc", "arrayType", "xsd:string[" + @params.Length + "]");
            writer.WritePrefixAttributeString("enc", "offset", "[0]");
            for (int i = 0; i < @params.Length; i++) writer.WriteElementString("item", @params[i]);
            if (@params.Length == 0) writer.WriteElementString("item", "");
            writer.WriteEndElement(); // </params>
            
            writer.WriteEndElement(); // </wss:modifyJob>
            
            writer.WriteEndElement(); // </soap:Body>
            
            writer.WriteEndElement(); // </soap:Envelope>
            
            writer.WriteEndDocument();
            writer.Close();
            stringWriter.Close();
            
            return ReceiveSoapResponse("modifyJob", "result", stringWriter.ToString());  
        }

        [System.Web.Services.Protocols.SoapRpcMethodAttribute("modifyJob", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string modifyJob(string sessionID, string jobName, ArrayOfString @params) {
            object[] results = this.Invoke("modifyJob", new object[] {
                        sessionID,
                        jobName,
                        @params});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("submit", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string submit(string sessionID) {
            object[] results = this.Invoke("submit", new object[] {
                        sessionID});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("timeLeft", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string timeLeft(string myProxyServer, string user, string password) {
            object[] results = this.Invoke("timeLeft", new object[] {
                        myProxyServer,
                        user,
                        password});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("getJobStatus", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string getJobStatus(string sessionID, string jobName) {
            object[] results = this.Invoke("getJobStatus", new object[] {
                        sessionID,
                        jobName});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("setJobStatus", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string setJobStatus(string sessionID, string jobName, string value) {
            object[] results = this.Invoke("setJobStatus", new object[] {
                        sessionID,
                        jobName,
                        value});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("taskClean", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string taskClean(string sessionID) {
            object[] results = this.Invoke("taskClean", new object[] {
                        sessionID});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("jobClean", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string jobClean(string sessionID, string jobName) {
            object[] results = this.Invoke("jobClean", new object[] {
                        sessionID,
                        jobName});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("insertText", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string insertText(string sessionID, string jobName, string filename, string value) {
            object[] results = this.Invoke("insertText", new object[] {
                        sessionID,
                        jobName,
                        filename,
                        value});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("readText", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string readText(string sessionID, string jobName, string filename) {
            object[] results = this.Invoke("readText", new object[] {
                        sessionID,
                        jobName,
                        filename});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("statusNotify", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string statusNotify(string sessionID, string jobName, string messageName, string messageValue, string messageType) {
            object[] results = this.Invoke("statusNotify", new object[] {
                        sessionID,
                        jobName,
                        messageName,
                        messageValue,
                        messageType});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("getLastNotify", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string getLastNotify(string sessionID, string jobName, string messageName) {
            object[] results = this.Invoke("getLastNotify", new object[] {
                        sessionID,
                        jobName,
                        messageName});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("getProxy", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string getProxy(string sessionID) {
            object[] results = this.Invoke("getProxy", new object[] {
                        sessionID});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("jobResubmit", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string jobResubmit(string sessionID, string jobName) {
            object[] results = this.Invoke("jobResubmit", new object[] {
                        sessionID,
                        jobName});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("jobRestart", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string jobRestart(string sessionID, string jobName) {
            object[] results = this.Invoke("jobRestart", new object[] {
                        sessionID,
                        jobName});
            return ((string)(results[0]));
        }
        
        [System.Web.Services.Protocols.SoapRpcMethodAttribute("taskAbort", RequestNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService", ResponseNamespace="http://tempuri.org/eo.gridEngine.frontEnd.ContactService")]
        [return: System.Xml.Serialization.SoapElement("result")]
        public string taskAbort(string sessionID) {
            object[] results = this.Invoke("taskAbort", new object[] {
                        sessionID});
            return ((string)(results[0]));
        }
        
        private string ReceiveSoapResponse(string soapAction, string resultElem, string message) {
            
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] byteArray = encoding.GetBytes(message);
    
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";
            request.ContentLength = byteArray.Length;
            request.Headers.Add("SoapAction", "\"" + soapAction + "\"");
            
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(byteArray, 0, byteArray.Length);
            requestStream.Close();
    
            // Get response stream
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            
            Stream responseStream = response.GetResponseStream();
            XmlDocument responseXml = new XmlDocument();
            XmlNodeList nodes; 
            try {
                responseXml.Load(responseStream);
                nodes = responseXml.GetElementsByTagName(resultElem);
            } finally {
                response.Close();
            }

            if (nodes.Count != 0) {
                return nodes[0].InnerXml;
            } else {
                nodes = responseXml.GetElementsByTagName("faultstring");
                if (nodes.Count != 0) throw new System.Web.Services.Protocols.SoapException(nodes[0].InnerXml, new XmlQualifiedName("faultstring"));
            }
            
            return null;
        }

        public static string GetErrorDetail(Exception e) {
            if (e is SoapException) {
                SoapException e1 = e as SoapException;
                XmlElement detailElem = e1.Detail as XmlElement;
                if (detailElem == null) return null;
                return detailElem.InnerXml;
            }
            
            if (e is WebException) {
                WebException e1 = e as WebException;
                try {
                    XmlDocument errorDoc = new XmlDocument();
                    errorDoc.Load(e1.Response.GetResponseStream());
                    XmlElement detailElem = errorDoc.SelectSingleNode("//detail") as XmlElement;
                    if (detailElem != null) return detailElem.InnerXml;
                } catch (Exception) {}
            }
            
            if (e.InnerException == null) return e.Message;
            return e.InnerException.Message;

            /*string result = e.InnerException.Message;
            if (result == null) return null;
            
            if (result.Contains("ConnectFailure")) return "Could not connect to Grid engine";
            else return result;*/
        }

    }

}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "2.0.50727.1433")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.SoapType(Namespace="http://engine.terradue.com:8080/wsServer.wsdl")]
public partial class ArrayOfString {
    
    private string[] itemField;
    
    /// <remarks/>
    [System.Xml.Serialization.SoapElement("item")]
    public string[] item {
        get {
            return this.itemField;
        }
        set {
            this.itemField = value;
        }
    }
}

