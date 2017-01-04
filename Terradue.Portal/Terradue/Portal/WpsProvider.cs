using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using Terradue.Util;
using OpenGis.Wps;
using System.Xml.Serialization;
using Terradue.OpenSearch.Result;
using System.Collections.Specialized;
using Terradue.ServiceModel.Syndication;
using Terradue.OpenSearch;



//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using Terradue.OpenSearch.Schema;
using Terradue.OpenSearch.Engine;
using System.Web;
using System.Runtime.Caching;

/*!
\defgroup CoreWPS WPS
@{

This component is an helper for 
- providing with WPS Server as a processing resource;
- providing with WPS process as processing offerings.


Main Functions
--------------

- analyses the GetCapabilities() of the WPS server of a \ref WpsProvider to retrieve all the process offered;
- retrieves the DescribeProcess() of the previously discovered process to describe the process with input and ouput parameters and create a \ref WpsProcessOffering;
- submits, controls and monitors processing via the Execute() of the WPS server of a \ref WpsProvider

Below, the sequence diagrams describe the previously listed functions.

\startuml "WPS Service Analysis Sequence Diagram - Get Capabilities"

actor "User or System" as WC
participant "Portal" as WS
participant "WPS Provider" as P
participant "Cloud Controller" as C
participant "Portal DataBase" as DB

autonumber

== Get Capabilities ==

WC -> WS: GetCapabilities request
activate WS
WS -> DB: Load all Providers (Proxy=true)
loop on each provider
    WS <-> DB: load services
    loop on each service
        WS <-> WS: get service info (identifier, title, abstract)
    end
end
WS -> C: discover Providers
loop on each provider
    WS <-> P: GetCapabilities
    WS <-> WS: extract services from GetCapabilities using request identifier
    loop on each service
        WS <-> WS: get service info (identifier, title, abstract)
    end
end
WS -> WS: aggregate all services info into response offering
WS -> WC: return aggregated GetCapabilities
deactivate WS

\enduml

\startuml "WPS Service Analysis Sequence Diagram - Describe Process"

participant "User or System" as WC
participant "Portal" as WS
participant "WPS Provider" as P
participant "Portal DataBase" as DB

autonumber

== Describe Process ==

WC -> WS: DescribeProcess request
activate WS
alt case process from db
    WS <-> DB: load service from request identifier
    WS <-> DB: get provider url + service identifier on the provider
else case process from cloud provider
    WS <-> P: GetCapabilities
    WS <-> WS: extract describeProcess url from GetCapabilities using request identifier
end
WS -> WS: build "real" describeProcess request
WS <-> P: DescribeProcess
WS -> WC: return result from describeProcess
deactivate WS

\enduml

\startuml "WPS Service Analysis Sequence Diagram - Execute"

participant "User or System" as WC
participant "Portal" as WS
participant "WPS Provider" as P
participant "Portal DataBase" as DB

autonumber

== Execute ==

WC -> WS: Execute request
activate WS
alt case process from db
    WS -> DB: load service from request identifier
    WS -> DB: get provider url + service identifier on the provider
else case process 'from cloud provider'
    WS -> P: GetCapabilities
    WS -> WS: extract execute url from GetCapabilities using request identifier
end
WS -> WS: build "real" execute request
WS -> P: Execute
alt case error
    WS -> WC: return error
else case success
    WS -> DB: store job
    WS -> WS: update job RetrieveResultServlet url
    WS -> WC: return created job
end
deactivate WS

\enduml

\startuml "WPS Service Analysis Sequence Diagram - Retrieve Result"

participant "User or System" as WC
participant "Portal" as WS
participant "WPS Provider" as P
participant "Portal DataBase" as DB

autonumber

== Retrieve Result Servlet ==

WC -> WS: RetrieveResultServlet request
activate WS
WS -> DB: load job info from request identifier
WS -> P: call "real" statusLocation url
WS -> WS: update href in response to put local server url instead of real provider
WS -> P: GET results
activate P
P -> WS: results, results status and results logs
deactivate P
WS -> WC: return results, results status and results logs
deactivate WS

\enduml

\startuml "WPS Service Analysis Sequence Diagram - Search WPS process"

participant "User or System" as WC
participant "Portal" as WS
participant "WPS Provider" as P
participant "Cloud Controller" as C
participant "Portal DataBase" as DB

autonumber

== Search WPS process ==

WC -> WS: WPS search request
activate WS
WS -> DB: Load all Providers
WS -> C: discover Providers
loop on each provider
    WS -> P: GetCapabilities
    WS -> WS: get services info
    loop on each service
        alt provider is Proxied
            WS -> WS: create local identifier and save remote identifier
            WS -> WS: use local server url as baseurl
        end
        WS -> WS: add service info to the response
    end
end
deactivate WS

\enduml

\startuml "WPS Service Analysis Sequence Diagram - Integrate WPS provider"

participant "User or System" as WC
participant "Portal" as WS
participant "WPS Provider" as P
participant "Portal DataBase" as DB

autonumber

== Integrate WPS provider ==

WC -> WS: POST provider
activate WS
WS -> DB: store provider
WS -> P: GetCapabilities
WS -> WS: get services info
loop on each service
    alt provider is Proxied
        WS -> WS: create local identifier and save remote identifier
        WS -> WS: use local server url as baseurl
    end
    WS -> DB: store service
end

\enduml


Model and Representation
------------------------ 

This components has also a function to represent a \ref Terradue::Portal::WpsProcessOffering object as a \ref Terradue.ServiceModel.Ogc.OwsModel.OwcOffering in the \ref OWSContext model.
It implements the mechanism to search for \ref Terradue::Portal::WpsProvider and the \ref Terradue::Portal::WpsProcessOffering via an \ref OpenSearchable interface.

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence stores the \ref Terradue::Portal::WpsProvider and \ref Terradue::Portal::WpsProcessOffering references in the database

\xrefitem dep "Dependencies" "Dependencies" \ref Authorisation controls the access on the WPS services

\xrefitem int "Interfaces" "Interfaces" connects \ref RWPS interface to retrieve process offerings from WPS Server and to submit, control and monitor prcoessing.

\ingroup Core

@}

\defgroup RWPS Remote Web Processing Services Interface
@{

    This is the interface to remote Web Processing Services. They are hosted by substem of the platform of thirs party system external to the platform.

    \xrefitem cptype_int "Interfaces" "Interfaces"

    \xrefitem norm "Normative References" "Normative References" [OGC Web Processing Service 1.0](http://portal.opengeospatial.org/files/?artifact_id=24151)

@}
*/







namespace Terradue.Portal {



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>WPS Provider</summary>
    /// <description>Represents a remote provider of a Web Processing Service.</description>
    /// <remarks>This class is used as the computing resource on which WPS processes, which are equivalent to tasks, run.</remarks>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    /// \ingroup "Core"
    [EntityTable("wpsprovider", EntityTableConfiguration.Custom)]
    public class WpsProvider : ComputingResource, IAtomizable {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
           (System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType);

        static MemoryCache DescribeProcessCache = new MemoryCache ("wpsProviderDescribeProcessCache");
        static MemoryCache GetCapabilitiesCache = new MemoryCache ("wpsProviderGetCapabilitiesCache");

        /// <summary>Gets or sets the base access point of the WPS provider.</summary>
        /// <remarks>The value must not contain the <c>service</c> or <c>request</c> query string parameters.</remarks>
        [EntityDataField("url")]
        public string BaseUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Define if the provider is to be proxied or not
        /// </summary>
        /// <value>The proxy.</value>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("proxy")]
        public bool Proxy { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the contact url or email.
        /// </summary>
        /// <value>The contact.</value>
        [EntityDataField("contact")]
        public string Contact { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Specify if the provider is a sandbox or is operational.
        /// </summary>
        public bool IsSandbox;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// The tags used to describe and filter the provider.
        /// </summary>
        public List<string> Tags;

        //---------------------------------------------------------------------------------------------------------------------

        protected OpenSearchEngine ose;

        public OpenSearchEngine OpenSearchEngine {
            get {
                return ose;
            }
            set {
                ose = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new WpsProvider instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public WpsProvider(IfyContext context) : base(context) {
            ose = new OpenSearchEngine();
            CanCache = true;//default is true, to be set to false to disable the cache
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Terradue.Portal.WpsProvider"/> can cache.
        /// </summary>
        /// <value><c>true</c> if can cache; otherwise, <c>false</c>.</value>
        public bool CanCache { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the WPS provider can monitor its own status.</summary>
        /// <remarks>There is no status monitoring for WPS providers; therefore the value of this property is always <c>false</c>.</remarks>
        public override bool CanMonitorStatus {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <remarks>There is no status monitoring for WPS providers; therefore this method is not implemented.</remarks>
        public override void GetStatus() {
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a WPS process instance on the WPS provider from the specified task via a WPS Execute request.</summary>
        /// <param name="task">The task to be created.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        public override bool CreateTask(Task task) {

            WpsProcessOffering processOffering = GetProcessOffering(task);

            // Get identifier of response document
            //XmlDocument processDescriptionDoc = new XmlDocument();
            //processDescriptionDoc.Load(String.Format("{0}{1}service=WPS&version=1.0.0&request=DescribeProcess&identifier={2}", BaseUrl, BaseUrl.Contains("?") ? "&" : "?", processOffering.ProcessIdentifier));
            string responseIdentifier = "Metalink";

            // Make request to WPS 
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(BaseUrl);
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";

            UTF8StringWriter stringWriter = new UTF8StringWriter();
            MonoXmlWriter writer = MonoXmlWriter.Create(stringWriter);

            writer.WriteStartDocument();
            writer.WriteStartElement("wps:Execute");
            writer.WriteNamespaceDefinition("wps", WpsApplication.NAMESPACE_URI_WPS);
            writer.WriteNamespaceDefinition("ows", WpsApplication.NAMESPACE_URI_OWS);
            writer.WriteNamespaceDefinition("xlink", WpsApplication.NAMESPACE_URI_XLINK);
            writer.WriteNamespaceDefinition("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("service", "WPS");
            writer.WriteAttributeString("version", "1.0.0");
            writer.WriteElementString("ows:Identifier", processOffering.RemoteIdentifier);
            writer.WriteStartElement("wps:DataInputs");
            foreach (ExecutionParameter param in task.ExecutionParameters) {
                if (param.Value == null)
                    continue;
                if (param.Values == null) {
                    writer.WriteStartElement("wps:Input");
                    writer.WriteElementString("ows:Identifier", param.Name);
                    writer.WriteStartElement("wps:Data");
                    writer.WriteElementString("wps:LiteralData", param.Value);
                    writer.WriteEndElement(); // </wps:Data>
                    writer.WriteEndElement(); // </wps:Input>
                    continue;
                }
                foreach (string value in param.Values) {
                    writer.WriteStartElement("wps:Input");
                    writer.WriteElementString("ows:Identifier", param.Name);
                    writer.WriteStartElement("wps:Data");
                    writer.WriteElementString("wps:LiteralData", value);
                    writer.WriteEndElement(); // </wps:Data>
                    writer.WriteEndElement(); // </wps:Input>
                }
            }
            writer.WriteEndElement(); // </wps:DataInputs>

            writer.WriteStartElement("wps:ResponseForm");
            writer.WriteStartElement("wps:ResponseDocument");
            writer.WriteAttributeString("storeExecuteResponse", "true");
            writer.WriteAttributeString("status", "true");
            writer.WriteStartElement("wps:Output");
            writer.WriteAttributeString("asReference", "true");
            writer.WriteElementString("ows:Identifier", responseIdentifier);
            writer.WriteEndElement(); // </wps:Output>
            writer.WriteEndElement(); // </wps:ResponseDocument>
            writer.WriteEndElement(); // </wps:ResponseForm>

            writer.WriteEndElement(); // </wps:Execute>

            writer.Close();

            byte[] byteArray = Encoding.UTF8.GetBytes(stringWriter.ToString());

            request.ContentLength = byteArray.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(byteArray, 0, byteArray.Length);
            requestStream.Close();

            XmlDocument executeResponseDoc = new XmlDocument ();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse ()) {
                Stream stream = response.GetResponseStream ();
                executeResponseDoc.Load (stream);
                stream.Close();
            }

            XmlNamespaceManager nsm = new XmlNamespaceManager(executeResponseDoc.NameTable);
            nsm.AddNamespace("wps", WpsApplication.NAMESPACE_URI_WPS);
            XmlNode xmlNode = executeResponseDoc.SelectSingleNode("wps:ExecuteResponse/@statusLocation", nsm);
            if (xmlNode != null)
                task.StatusUrl = xmlNode.Value;

            xmlNode = executeResponseDoc.SelectSingleNode("wps:ExecuteResponse/wps:Status/*[1]", nsm);
            if (xmlNode == null)
                return false;

            task.ActualStatus = GetStatusFromExecuteResponse(xmlNode.LocalName);

            if (task.Finished)
                GetTaskResult(task, executeResponseDoc); // task may be synchronous processing

            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Get and stores the process offerings from GetCapabilities url
        /// </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public void StoreProcessOfferings() {

            List<WpsProcessOffering> processes = GetWpsProcessOfferingsFromRemote(true);
            foreach (WpsProcessOffering process in processes) {
                try {
                    process.Store();
                } catch (Exception) {
                    //do nothing, process already in db, skip it
                }
            }
        }

        public void UpdateProcessOfferings() {
            List<WpsProcessOffering> remoteProcesses = GetWpsProcessOfferingsFromRemote(true);
            EntityList<WpsProcessOffering> dbProcesses = this.GetWpsProcessOfferings(false);

            foreach (WpsProcessOffering pR in remoteProcesses) {
                bool existsPrInDb = false;
                foreach(WpsProcessOffering pDB in dbProcesses) {
                    // if pDB in pR -> we update pDB with pR
                    if (pDB.RemoteIdentifier.Equals(pR.RemoteIdentifier)) {
                        existsPrInDb = true;
                        pDB.Name = pR.Name;
                        pDB.Description = pR.Description;
                        pDB.Version = pR.Version;
                        pDB.Store();
                        break;
                    }
                }
                // if pR not in pDB -> we add pR (store in DB)
                if (!existsPrInDb) 
                    pR.Store(); 
            }

            foreach (WpsProcessOffering pDB in dbProcesses) {
                bool existsPdbInPr = false;
                foreach (WpsProcessOffering pR in remoteProcesses) {
                    if (pDB.RemoteIdentifier.Equals(pR.RemoteIdentifier))
                        existsPdbInPr = true;
                }
                // if pDb not in pR -> we remove pDb
                if (!existsPdbInPr)
                    pDB.Delete();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Return a GetCapabilities object from GetCapabilities url
        /// </summary>
        /// <returns>GetCapabilities.</returns>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public WPSCapabilitiesType GetWPSCapabilities() {

            if (string.IsNullOrEmpty (BaseUrl)) throw new Exception("Cannot GetCapabilities, baseUrl of WPS is null");

            string query = "Service=WPS&Request=GetCapabilities";
            var uri = new UriBuilder (BaseUrl);
            uri.Query = query;
            var getCapUrl = uri.Uri.AbsoluteUri;
            context.LogDebug (this, "GetWPSCapabilities -- url = " + getCapUrl);

            WPSCapabilitiesType response = null;
            var cacheItem = GetCapabilitiesCache.GetCacheItem (getCapUrl);
            if (cacheItem != null && CanCache) {
                response = (WPSCapabilitiesType)cacheItem.Value;
            } else {
                HttpWebRequest request = CreateWebRequest (getCapUrl);
                try {
                    using (var httpResponse = (HttpWebResponse)request.GetResponse ()) {
                        using (var stream = httpResponse.GetResponseStream ()) {
                            response = (WPSCapabilitiesType)new System.Xml.Serialization.XmlSerializer (typeof (WPSCapabilitiesType)).Deserialize (stream);
                            GetCapabilitiesCache.Set (new CacheItem (getCapUrl, response), new CacheItemPolicy () { AbsoluteExpiration = DateTimeOffset.Now.AddHours (12) });
                        }
                    }
                } catch (Exception e) {
                    throw e;
                }
            }
            return response;

        }

        /// <summary>
        /// Gets the WPS describe process from URL.
        /// </summary>
        /// <returns>The WPS describe process from URL.</returns>
        /// <param name="url">URL.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public ProcessDescriptionType GetWPSDescribeProcess(string identifier, string version) {

            if (string.IsNullOrEmpty(BaseUrl)) throw new Exception("Cannot get DescribeProcess, baseUrl of WPS is null");

            string query = string.Format("Service=WPS&Request=DescribeProcess&Identifier={0}&Version={1}",identifier,version);
            var uri = new UriBuilder (BaseUrl);
            uri.Query = query;
            var descPUrl = uri.Uri.AbsoluteUri;
            context.LogDebug (this, "GetWPSDescribeProcess -- url = " + descPUrl);

            ProcessDescriptionType result = null;

            var cacheItem = DescribeProcessCache.GetCacheItem (descPUrl);
            if (cacheItem != null && CanCache) {
                result = (ProcessDescriptionType)cacheItem.Value;
            } else {
                HttpWebRequest request = CreateWebRequest (descPUrl);
                try {
                    using (var httpResponse = (HttpWebResponse)request.GetResponse ()) {
                        using (var stream = httpResponse.GetResponseStream ()) {
                            var response = (ProcessDescriptions)new System.Xml.Serialization.XmlSerializer (typeof (ProcessDescriptions)).Deserialize (stream);
                            result = response.ProcessDescription [0];
                            DescribeProcessCache.Set (new CacheItem (descPUrl, result), new CacheItemPolicy () { AbsoluteExpiration = DateTimeOffset.Now.AddHours (12) });
                        }
                    }
                } catch (Exception e) {
                    throw e;
                }
            }
            return result;

        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the wps process offerings from URL.
        /// </summary>
        /// <returns>The wps process offerings from URL.</returns>
        /// <param name="baseurl">Baseurl.</param>
        /// <param name="updateProviderInfo">If set to <c>true</c> update provider info.</param>
        public List<WpsProcessOffering> GetWpsProcessOfferingsFromRemote(bool updateProviderInfo = false) {
            List<WpsProcessOffering> wpsProcessList = new List<WpsProcessOffering>();
            OpenGis.Wps.WPSCapabilitiesType capabilities = GetWPSCapabilities();
            List<Operation> operations = capabilities.OperationsMetadata.Operation;
            string url = "";
            foreach (Operation operation in operations) {
                if (operation.name == "DescribeProcess") {
                    url = operation.DCP[0].Item.Items[0].href;
                    break;
                }
            }

            foreach (ProcessBriefType process in capabilities.ProcessOfferings.Process) {
                WpsProcessOffering wpsProcess = new WpsProcessOffering(context);
                wpsProcess.Provider = this;
                wpsProcess.RemoteIdentifier = (process.Identifier != null ? process.Identifier.Value : null);
                if (this.Id == 0) {
                    wpsProcess.Identifier = this.Identifier + "-" + wpsProcess.RemoteIdentifier;
                } else {
                    wpsProcess.Identifier = Guid.NewGuid().ToString();
                }
                wpsProcess.Name = (process.Title != null ? process.Title.Value : null);
                wpsProcess.Description = (process.Abstract != null ? process.Abstract.Value : null);
                wpsProcess.Version = process.processVersion;
                wpsProcess.Url = url;

                //get more infos (if necessary)
                if (wpsProcess.Name == null || wpsProcess.Description == null) {
                    try{
                        ProcessDescriptionType describeProcess = GetWPSDescribeProcess(wpsProcess.RemoteIdentifier, process.processVersion);
                        wpsProcess.Description = (describeProcess.Abstract != null ? describeProcess.Abstract.Value : null);
                    }catch(Exception e){
                    }
                }
                wpsProcessList.Add(wpsProcess);
            }

            if (updateProviderInfo) {
                if (capabilities.ServiceProvider != null) {
                    if (capabilities.ServiceProvider.ServiceContact != null
                        && capabilities.ServiceProvider.ServiceContact.ContactInfo != null
                        && capabilities.ServiceProvider.ServiceContact.ContactInfo.Address != null
                        && capabilities.ServiceProvider.ServiceContact.ContactInfo.Address.ElectronicMailAddress != null
                        && capabilities.ServiceProvider.ServiceContact.ContactInfo.Address.ElectronicMailAddress.Count > 0
                        && !string.IsNullOrEmpty(capabilities.ServiceProvider.ServiceContact.ContactInfo.Address.ElectronicMailAddress[0])) {
                        this.Contact = string.Format("{0}{1}{2}",
                                                     capabilities.ServiceProvider.ServiceContact.IndividualName,
                                                     string.IsNullOrEmpty(capabilities.ServiceProvider.ServiceContact.IndividualName) || string.IsNullOrEmpty(capabilities.ServiceProvider.ServiceContact.ContactInfo.Address.ElectronicMailAddress[0]) ? "" : " - ",
                                                     capabilities.ServiceProvider.ServiceContact.ContactInfo.Address.ElectronicMailAddress[0]);
                        this.Store();
                    } else {
                        if (capabilities.ServiceProvider.ProviderSite != null) {
                            this.Contact = string.Format("{0}{1}{2}",
                                                     capabilities.ServiceProvider.ProviderName,
                                                     string.IsNullOrEmpty(capabilities.ServiceProvider.ProviderName) || string.IsNullOrEmpty(capabilities.ServiceProvider.ProviderSite.href) ? "" : " - ",
                                                     capabilities.ServiceProvider.ProviderSite.href);
                            this.Store();
                        }
                    }
                }
            }
            return wpsProcessList;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the wps process offerings from services in DB.
        /// </summary>
        /// <param name="availables">True to return only available services</param>
        /// <returns>The wps process offerings.</returns>
        public EntityList<WpsProcessOffering> GetWpsProcessOfferings(bool availables = true) {
            EntityList<WpsProcessOffering> wpsProcessList = new EntityList<WpsProcessOffering>(context);
            wpsProcessList.Template.Provider = this;
            wpsProcessList.Template.Available = availables;
            wpsProcessList.Load();
            return wpsProcessList;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the process brief types.
        /// </summary>
        /// <returns>The process brief types.</returns>
        public List<ProcessBriefType> GetProcessBriefTypes() {
            List<ProcessBriefType> result = new List<ProcessBriefType>();
            EntityList<WpsProcessOffering> wpsProcessList = new EntityList<WpsProcessOffering>(context);
            wpsProcessList.Template.Provider = this;
            wpsProcessList.Load();
            foreach (WpsProcessOffering wpsProcess in wpsProcessList) {
                if (wpsProcess.ProviderId == this.Id) {
                    ProcessBriefType processOffering = new ProcessBriefType();
                    processOffering.Identifier = new CodeType{ Value = wpsProcess.Identifier };
                    processOffering.Title = new LanguageStringType{ Value = wpsProcess.Name };
                    processOffering.Abstract = new LanguageStringType{ Value = wpsProcess.Description };
                    processOffering.processVersion = wpsProcess.Version;
                    result.Add(processOffering);
                }
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Creates the web request (integrates username/password defined for the WpsProvider)
        /// </summary>
        /// <returns>The web request.</returns>
        /// <param name="url">URL.</param>
        /// <param name="method">Method.</param>
        public HttpWebRequest CreateWebRequest (string url)
        {
            this.context.LogDebug (this, "CreateWebRequest : " + url);

            //credentials in BaseUrl ?
            NetworkCredential credentials = null;
            var uri = new UriBuilder (this.BaseUrl);
            if (!string.IsNullOrEmpty (uri.UserName) && !string.IsNullOrEmpty (uri.Password)) {
                credentials = new NetworkCredential (uri.UserName, uri.Password);
            }

            var request = CreateWebRequest (url, credentials, context.Username);
            return request;
        }

        public static HttpWebRequest CreateWebRequest (string url, NetworkCredential credentials, string username)
        {

            HttpWebRequest request;
            request = (HttpWebRequest)WebRequest.Create (url);
            request.Proxy = null;
            request.Method = "GET";
            request.Timeout = 5000;

            log.DebugFormat ("CreateWebRequest '{0}' with Header REMOTE_USER={1}", url, username);

            if (!string.IsNullOrEmpty (username) && !(url.Contains ("gpod.eo.esa.int"))) request.Headers.Add ("REMOTE_USER", username);
            if (credentials != null) request.Credentials = credentials;

            return request;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns><c>true</c>, if task was executed, <c>false</c> otherwise.</returns>
        /// <param name="task">Task.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public bool ExecuteTask(Task task) {
            WpsProcessOffering processOffering = GetProcessOffering(task);

            Execute exec = new Execute();
            exec.service = "WPS";
            exec.version = "1.0.0";
            exec.Identifier = new CodeType{ Value = processOffering.RemoteIdentifier };
            exec.DataInputs = new List<InputType>();
            foreach (ExecutionParameter param in task.ExecutionParameters) {
                if (param.Value == null)
                    continue;
                if (param.Values == null) {
                    InputType input = new InputType();
                    input.Identifier = new CodeType{ Value = param.Name };
                    input.Data = new DataType{ Item = new LiteralDataType{ Value = param.Value } };
                    exec.DataInputs.Add(input);
                    continue;
                }
                foreach (string value in param.Values) {
                    InputType input = new InputType();
                    input.Identifier = new CodeType{ Value = param.Name };
                    input.Data = new DataType{ Item = new LiteralDataType{ Value = value } };
                    exec.DataInputs.Add(input);
                }
            }
            exec.ResponseForm = new ResponseFormType();
            List<DocumentOutputDefinitionType> outputs = new List<DocumentOutputDefinitionType>();
            DocumentOutputDefinitionType output = new DocumentOutputDefinitionType {
                asReference = true,
                Identifier = new CodeType{ Value = "Metalink" }
            };
            outputs.Add(output);
            exec.ResponseForm.Item = new ResponseDocumentType {
                storeExecuteResponse = true,
                status = true,
                Output = outputs
            };

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(BaseUrl);
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";

            Stream requestStream = request.GetRequestStream();
            new XmlSerializer(typeof(Execute)).Serialize(requestStream, exec);
            requestStream.Close();

            ExecuteResponse execResponse;
            using (var response = (HttpWebResponse)request.GetResponse ()) {
                using (var stream = response.GetResponseStream ())
                    execResponse = (ExecuteResponse)new System.Xml.Serialization.XmlSerializer (typeof (ExecuteResponse)).Deserialize (stream);
            }
            if (execResponse != null) {
                if (execResponse.statusLocation != null)
                    task.StatusUrl = execResponse.statusLocation;
                if (execResponse.Status.Item == null)
                    return false;
            }

            task.ActualStatus = GetStatusFromExecuteResponse(execResponse.Status.Item);
            if (task.Finished)
                GetTaskResult(task, execResponse); // task may be synchronous processing

            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether it is possible to start the execution of the specified task on the WPS provider.</summary>
        /// <remarks>Since there is no status and capacity information for WPS providers, it is assumed that a new process instance can be started at any time.</remarks>
        /// <returns><c>true</c>.</returns>
        public override bool CanStartTask(Task task) {
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <remarks>Since the task is created and started at the same time with the submission of the Execute request, this method does nothing.</remarks>
        /// <param name="task">The task to be started.</param>
        /// <returns><c>true</c>.</returns>
        public override bool StartTask(Task task) {
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <remarks>WPS providers do not currently allow stopping running process instances; therefore this method does nothing.</remarks>
        /// <param name="task">The task to be stopped.</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        public override bool StopTask(Task task) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets status information for the task from the WPS status location.</summary>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        public override bool GetTaskStatus(Task task) {
            // Make request to status location
            XmlDocument statusDoc = new XmlDocument();
            statusDoc.Load(task.StatusUrl);
            XmlNamespaceManager nsm = new XmlNamespaceManager(statusDoc.NameTable);
            nsm.AddNamespace("wps", WpsApplication.NAMESPACE_URI_WPS);
            XmlNode xmlNode = statusDoc.SelectSingleNode("wps:ExecuteResponse/wps:Status/*[1]", nsm);
            if (xmlNode == null)
                return false;

            task.ActualStatus = GetStatusFromExecuteResponse(xmlNode.LocalName);

            if (task.Finished) GetTaskResult(task, statusDoc);

            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool DestroyTask(Task task) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <remarks>WPS does not provide interfaces at sub-process level, therefore all methods operating on jobs do nothing.</remarks>
        /// <returns><c>false</c></returns>
        public override bool CreateJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <remarks>WPS does not provide interfaces at sub-process level, therefore all methods operating on jobs do nothing.</remarks>
        /// <returns><c>false</c></returns>
        public override bool UpdateJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <remarks>WPS does not provide interfaces at sub-process level, therefore all methods operating on jobs do nothing.</remarks>
        /// <returns><c>false</c></returns>
        public override bool StartJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <remarks>WPS does not provide interfaces at sub-process level, therefore all methods operating on jobs do nothing.</remarks>
        /// <returns><c>false</c></returns>
        public override bool SuspendJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <remarks>WPS does not provide interfaces at sub-process level, therefore all methods operating on jobs do nothing.</remarks>
        /// <returns><c>false</c></returns>
        public override bool ResumeJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <remarks>WPS does not provide interfaces at sub-process level, therefore all methods operating on jobs do nothing.</remarks>
        /// <returns><c>false</c></returns>
        public override bool StopJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <remarks>WPS does not provide interfaces at sub-process level, therefore all methods operating on jobs do nothing.</remarks>
        /// <returns><c>false</c></returns>
        public override bool CompleteJob(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <remarks>WPS does not provide interfaces at sub-process level, therefore all methods operating on jobs do nothing.</remarks>
        /// <returns><c>false</c></returns>
        public override bool GetJobStatus(Job job) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        /// <returns><c>false</c></returns>
        public override bool GetTaskResult(Task task) {
            XmlDocument statusDoc = new XmlDocument();
            statusDoc.Load(task.StatusUrl);
            return GetTaskResult(task, statusDoc);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the task result.
        /// </summary>
        /// <returns><c>true</c>, if task result was gotten, <c>false</c> otherwise.</returns>
        /// <param name="task">Task.</param>
        /// <param name="doc">Document.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        protected bool GetTaskResult(Task task, XmlDocument doc) {
            XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);
            nsm.AddNamespace("wps", WpsApplication.NAMESPACE_URI_WPS);
            nsm.AddNamespace("ows", WpsApplication.NAMESPACE_URI_OWS);
            XmlNodeList xmlNodes = doc.SelectNodes("wps:ExecuteResponse/wps:ProcessOutputs/wps:Output/wps:Data/wps:LiteralData", nsm);

            string metalinkUrl = null;
            foreach (XmlNode xmlNode in xmlNodes) {
                XmlNode identifierNode = xmlNode.ParentNode.ParentNode.SelectSingleNode("ows:Identifier", nsm);
                if (identifierNode == null || identifierNode.InnerText != "Metalink")
                    continue;
                metalinkUrl = xmlNode.InnerText;
                break;
            }
            if (metalinkUrl == null)
                return false;
            task.ResultMetadataFiles.Add(metalinkUrl);

            XmlDocument metalinkDoc = new XmlDocument();
            metalinkDoc.Load(metalinkUrl);
            nsm = new XmlNamespaceManager(metalinkDoc.NameTable);
            nsm.AddNamespace("m", "http://www.metalinker.org/");
            xmlNodes = metalinkDoc.SelectNodes("m:metalink/m:files/m:file/m:resources/m:url", nsm);
            foreach (XmlNode xmlNode in xmlNodes) {
                string resource = xmlNode.InnerText;
                XmlElement fileElem = xmlNode.ParentNode.ParentNode as XmlElement;
                string identifier = (fileElem != null && fileElem.HasAttribute("name") ? fileElem.Attributes["name"].Value : null);
                XmlElement sizeElem = fileElem.SelectSingleNode("m:size", nsm) as XmlElement;
                long size = 0;
                if (sizeElem != null)
                    Int64.TryParse(sizeElem.InnerText, out size);
                task.OutputFiles.Add(new DataSetInfo(resource, identifier, size, context.Now));
            }

            return true;
        }

        protected bool GetTaskResult(Task task, ExecuteResponse exec) {
            string metalinkUrl = null;
            foreach (OutputDataType output in exec.ProcessOutputs) {
                string literal = ((LiteralDataType)((DataType)output.Item).Item).Value;
                if (literal == null || literal != "Metalink")
                    continue;
                metalinkUrl = literal;
            }

            if (metalinkUrl == null)
                return false;
            task.ResultMetadataFiles.Add(metalinkUrl);

            XmlDocument metalinkDoc = new XmlDocument();
            metalinkDoc.Load(metalinkUrl);
            XmlNamespaceManager nsm = new XmlNamespaceManager(metalinkDoc.NameTable);
            nsm.AddNamespace("m", "http://www.metalinker.org/");
            XmlNodeList xmlNodes = metalinkDoc.SelectNodes("m:metalink/m:files/m:file/m:resources/m:url", nsm);
            foreach (XmlNode xmlNode in xmlNodes) {
                string resource = xmlNode.InnerText;
                XmlElement fileElem = xmlNode.ParentNode.ParentNode as XmlElement;
                string identifier = (fileElem != null && fileElem.HasAttribute("name") ? fileElem.Attributes["name"].Value : null);
                XmlElement sizeElem = fileElem.SelectSingleNode("m:size", nsm) as XmlElement;
                if (sizeElem != null) {
                    //int size;
                    //InTryParse();
                }
                task.OutputFiles.Add(new DataSetInfo(resource, identifier, 0, context.Now));
            }

            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Overrides abstract method of ComputingResource; does nothing.</summary>
        public override void WriteTaskResultRdf(Task task, XmlWriter output) {
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Get all the process offering of the WPS provider
        /// </summary>
        /// <returns>The process offering.</returns>
        /// <param name="task">Task.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        private WpsProcessOffering GetProcessOffering(Task task) {
            WpsProcessOffering result = task.Service as WpsProcessOffering;
            if (result == null || result.Provider != this)
                throw new InvalidOperationException("The service used by this task is not offered by this WPS provider");
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        private int GetStatusFromExecuteResponse(string elementName) {
            switch (elementName) {
                case "ProcessAccepted":
                    return ProcessingStatus.Queued;
                case "ProcessStarted":
                    return ProcessingStatus.Active;
                case "ProcessPaused":
                    return ProcessingStatus.Paused;
                case "ProcessFailed":
                    return ProcessingStatus.Failed;
                case "ProcessSucceeded":
                    return ProcessingStatus.Completed;
                default :
                    return ProcessingStatus.Created;
            }
        }

        private int GetStatusFromExecuteResponse(object status) {
            string statusName = "";
            if (status is string) {
                statusName = status as string;
            } else {
                if (status is ProcessFailedType)
                    statusName = "ProcessFailed";
                else if (status is ProcessStartedType)
                    statusName = (status as ProcessStartedType).Value;
            }

            switch (statusName) {
                case "ProcessAccepted":
                    return ProcessingStatus.Queued;
                case "ProcessStarted":
                    return ProcessingStatus.Active;
                case "ProcessPaused":
                    return ProcessingStatus.Paused;
                case "ProcessFailed":
                    return ProcessingStatus.Failed;
                case "ProcessSucceeded":
                    return ProcessingStatus.Completed;
                default :
                    return ProcessingStatus.Created;
            }
        }

        #region IAtomizable implementation

        public AtomItem ToAtomItem(NameValueCollection parameters) {

            string identifier = (this.Identifier != null ? this.Identifier : "service" + this.Id);
            string name = (this.Name != null ? this.Name : identifier);
            string text = (this.TextContent != null ? this.TextContent : "");

            if (parameters["q"] != null) {
                string q = parameters["q"];
                if (!(name.Contains(q) || identifier.Contains(q) || text.Contains(q)))
                    return null;
            }

            if (parameters["url"] != null) {
                if (this.BaseUrl != parameters["url"])
                    return null;
            }

            Uri alternate = (this.Proxy ? new Uri(context.BaseUrl + "/wps/WebProcessingService") : new Uri(this.BaseUrl));

            //AtomItem atomEntry = null;
            var entityType = EntityType.GetEntityType(typeof(WpsProvider));
            Uri id = null;
            if (this.Id == 0)
                id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?url=" + HttpUtility.UrlEncode(this.BaseUrl));
            else
                id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + identifier);

            AtomItem entry = new AtomItem(identifier, name, alternate, id.ToString(), DateTime.UtcNow);
            entry.Categories.Add(new SyndicationCategory("service"));

            entry.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));

            entry.Summary = new TextSyndicationContent(name);
            entry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", identifier);

            return entry;
        }


        public NameValueCollection GetOpenSearchParameters() {
            var parameters = OpenSearchFactory.GetBaseOpenSearchParameter();
            parameters.Add("id", "{geo:uid?}");
            parameters.Add("url", "{ows:url?}");
            return parameters;
        }

        #endregion

        public virtual OpenSearchUrl GetSearchBaseUrl(string mimeType) {
            return new OpenSearchUrl(string.Format("{0}/wps/{1}/search", context.BaseUrl, this.Identifier));
        }

        public virtual OpenSearchUrl GetDescriptionBaseUrl() {
            return new OpenSearchUrl(string.Format("{0}/wps/{1}/description", context.BaseUrl, this.Identifier));
        }

        public NameValueCollection GetOpenSearchParameters(string mimeType) {
            NameValueCollection nvc = OpenSearchFactory.GetBaseOpenSearchParameter();
            return nvc;
        }

        /// <summary>
        /// Gets the local open search description.
        /// </summary>
        /// <returns>The local open search description.</returns>
        public OpenSearchDescription GetLocalOpenSearchDescription() {

            OpenSearchDescription osd = new OpenSearchDescription();
            osd.ShortName = this.Identifier;
            osd.Contact = context.GetConfigValue("CompanyEmail");
            osd.SyndicationRight = "open";
            osd.AdultContent = "false";
            osd.Language = "en-us";
            osd.OutputEncoding = "UTF-8";
            osd.InputEncoding = "UTF-8";
            osd.Developer = "Terradue OpenSearch Development Team";
            osd.Attribution = context.GetConfigValue("CompanyName");

            List<OpenSearchDescriptionUrl> urls = new List<OpenSearchDescriptionUrl>();

            UriBuilder urlb = new UriBuilder(GetDescriptionBaseUrl());

            OpenSearchDescriptionUrl urlxml = new OpenSearchDescriptionUrl("application/opensearchdescription+xml", urlb.ToString(), "self");
            urls.Add(urlxml);

            urlb = new UriBuilder(GetSearchBaseUrl("application/atom+xml"));
            NameValueCollection query = GetOpenSearchParameters("application/atom+xml");

            NameValueCollection nvc = HttpUtility.ParseQueryString(urlb.Query);
            foreach (var key in nvc.AllKeys) {
                query.Set(key, nvc[key]);
            }

            foreach (var osee in OpenSearchEngine.Extensions.Values) {
                query.Set("format", osee.Identifier);

                string[] queryString = Array.ConvertAll(query.AllKeys, key => string.Format("{0}={1}", key, query[key]));
                urlb.Query = string.Join("&", queryString);
                urlxml = new OpenSearchDescriptionUrl(osee.DiscoveryContentType, urlb.ToString(), "search");
                urls.Add(urlxml);
            }

            osd.Url = urls.ToArray();


            //OpenSearchDescriptionUrl urld = osd.Url[0];

            query.Set("format", "json");
            urlb.Query = query.ToString();
            OpenSearchDescriptionUrl urljson = new OpenSearchDescriptionUrl("application/json", urlb.ToString(), "search");
            urls.Add(urljson);
            osd.Url = urls.ToArray();

            return osd;

        }
    }
}

