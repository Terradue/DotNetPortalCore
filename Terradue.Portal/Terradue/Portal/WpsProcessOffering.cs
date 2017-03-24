using System;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using System.Collections.Specialized;
using Terradue.ServiceModel.Syndication;
using System.Collections.Generic;

//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Web;

using System.Net;
using System.IO;
using OpenGis.Wps;
using System.Xml;
using Terradue.ServiceModel.Ogc.Owc.AtomEncoding;
using System.Linq;
using Terradue.Portal.OpenSearch;

namespace Terradue.Portal {



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    /// <summary>WPS processing Service</summary>
    /// <description>Represents a processing service available via a remote WPS server.</description>
    /// \ingroup Core
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    /// \ingroup "Core"
    [EntityTable("wpsproc", EntityTableConfiguration.Custom)]
    public class WpsProcessOffering : Service, IAtomizable {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        //---------------------------------------------------------------------------------------------------------------------

        private WpsProvider provider;

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataFieldAttribute("id_provider", IsForeignKey = true)]
        public int ProviderId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Remote identifier of the process offering on the \ref WPSProvider it belongs to.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataFieldAttribute("remote_id")]
        public string RemoteIdentifier { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
       
        [Obsolete("Use RemoteIdentifier")]
        public string ProcessIdentifier {
            get { return RemoteIdentifier; }
            set { RemoteIdentifier = value; } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>WPS provider to which the WPS process offering comes from</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        /// <value>is offered by \ref WpsProvider thats host the service of the offering</value>
        public WpsProvider Provider {
            get {
                if (provider != null)
                    return provider;
                if (provider == null || provider.Id != ProviderId) provider = ComputingResource.FromId(context, ProviderId) as WpsProvider;
                return provider;
            }
            set {
                provider = value;
                ProviderId = (provider == null ? 0 : provider.Id);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the computing resource that must be used by the WPS process offering.</summary>
        /// <remarks>The value is always the same as the WPS provider to which the WPS process offering belongs.</remarks>
        public override ComputingResource FixedComputingResource {
            get { return Provider; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new WpsProcessOffering instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public WpsProcessOffering(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <returns>The parameters.</returns>
        public override ServiceParameterSet GetParameters() {
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds the task.
        /// </summary>
        /// <param name="task">Task.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public override void BuildTask(Task task) {
            //this.ComputingResourceId = Provider;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public object DescribeProcess(){

            var query = "Service=WPS&Request=DescribeProcess";

            var identifier = (RemoteIdentifier != null ? RemoteIdentifier : Identifier);
            query += "&Identifier=" + identifier;
            
            if (this.Provider != null) 
                query += "&Version=" + this.Provider.WPSVersion;

            var uri = new UriBuilder (Provider.BaseUrl);
            uri.Query = query;

            HttpWebRequest describeHttpRequest = this.Provider.CreateWebRequest(uri.Uri.AbsoluteUri);
            ProcessDescriptions describeProcess = null;

            //call describe url
            using (HttpWebResponse describeResponse = (HttpWebResponse)describeHttpRequest.GetResponse ()) {
                using (var memStream = new MemoryStream ()) {
                    describeResponse.GetResponseStream ().CopyTo (memStream);
                    memStream.Seek (0, SeekOrigin.Begin);
                    if (describeResponse.StatusCode != HttpStatusCode.OK) {
                        using (StreamReader reader = new StreamReader (memStream)) {
                            string errormsg = reader.ReadToEnd ();
                            log.Error (errormsg);
                            throw new Exception (errormsg);//TEMPORARY - 52 North bug
                        }
                    }
                    describeProcess = (ProcessDescriptions)new System.Xml.Serialization.XmlSerializer (typeof (ProcessDescriptions)).Deserialize (memStream);
                }
            }
            return describeProcess;
        }

        public HttpWebRequest PrepareExecuteRequest(Execute executeInput){
            //build "real" execute url
            var uriExec = new UriBuilder(Provider.BaseUrl);
            uriExec.Query = "";
            if (Provider.BaseUrl.Contains("gpod.eo.esa.int")) {
                uriExec.Query += "_format=xml&_user=" + context.GetConfigIntegerValue("GpodWpsUserId");
            }

            var identifier = (RemoteIdentifier != null ? RemoteIdentifier : Identifier);
            executeInput.Identifier = new CodeType{ Value = identifier };

            if (this.Provider != null && !this.Provider.WPSVersion.Equals(executeInput.version)) executeInput.version = this.Provider.WPSVersion;

            log.Info("Execute Uri: " + uriExec.Uri.AbsoluteUri);
            HttpWebRequest executeHttpRequest = this.Provider.CreateWebRequest(uriExec.Uri.AbsoluteUri);

            executeHttpRequest.Method = "POST";
            executeHttpRequest.ContentType = "application/xml";

            //if gpod service, we need to add extra infos to the request
            if (Provider.BaseUrl.Contains("gpod.eo.esa.int")) {
                executeHttpRequest.Headers.Add("X-UserID", context.GetConfigValue("GpodWpsUser"));
            }

            System.Xml.Serialization.XmlSerializerNamespaces ns = new System.Xml.Serialization.XmlSerializerNamespaces();
            ns.Add("wps", "http://www.opengis.net/wps/1.0.0");
            ns.Add("ows", "http://www.opengis.net/ows/1.1");

            XmlWriterSettings settings = new XmlWriterSettings{ 
                Encoding = new System.Text.UTF8Encoding(false)
            };

            using (var writer = XmlWriter.Create(executeHttpRequest.GetRequestStream(),settings)) {
                new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.Execute)).Serialize(writer, executeInput, ns);
                writer.Flush();
                writer.Close();
            }

            using(StringWriter textWriter = new StringWriter())
            {
                new System.Xml.Serialization.XmlSerializer(typeof(OpenGis.Wps.Execute)).Serialize(textWriter, executeInput, ns);
                var xmlinput = textWriter.ToString();
                log.Debug("Execute request : " + xmlinput);
            }

            return executeHttpRequest;
        }

        private ExceptionReport ExecuteError(Stream stream){
            stream.Seek(0, SeekOrigin.Begin);
            try {
                return (ExceptionReport)new System.Xml.Serialization.XmlSerializer(typeof(ExceptionReport)).Deserialize(stream);
            } catch (Exception e) {
                stream.Seek(0, SeekOrigin.Begin);
                using (StreamReader reader = new StreamReader(stream)) {
                    string errormsg = reader.ReadToEnd();
                    log.Error(errormsg);
                    throw new Exception(errormsg);
                }
            }
        }

        public object Execute(Execute executeInput){

            //We first validate that the user can use the service
            if (!CanUse) throw new Exception ("The current user is not allowed to Execute on the service " + Name);

            var executeHttpRequest = PrepareExecuteRequest(executeInput);        

            OpenGis.Wps.ExecuteResponse execResponse = null;
            OpenGis.Wps.ExceptionReport exceptionReport = null;
            MemoryStream memStream = new MemoryStream();
            try {
                using (var executeResponse = (HttpWebResponse)executeHttpRequest.GetResponse ()) {
                    using (var stream = executeResponse.GetResponseStream ()){
                        stream.CopyTo (memStream);
                    }

                    if (executeResponse.StatusCode != HttpStatusCode.OK) {
                        log.Debug ("Execute response code : " + executeResponse.StatusCode);
                        return ExecuteError (memStream);
                    }
                }

                memStream.Seek(0, SeekOrigin.Begin);
                execResponse = (ExecuteResponse)new System.Xml.Serialization.XmlSerializer(typeof(ExecuteResponse)).Deserialize(memStream);
                return execResponse;
            } catch (WebException we){
                using (WebResponse response = we.Response){
                    using (var httpResponse = (HttpWebResponse)response){
                        using (var stream = httpResponse.GetResponseStream ()){
                            stream.CopyTo (memStream);
                        }
                        log.Debug ("Execute response code : " + httpResponse.StatusCode);
                    }
                    return ExecuteError(memStream);
                }
            } catch (InvalidOperationException ioe) {
                log.Error("InvalidOperationException : " + ioe.Message + " - " + ioe.StackTrace);
                //bug 52 NORTH - to be removed once AIR updated
                return ExecuteError(memStream);
            } catch (Exception e) {
                log.Error("Execute request failed");
                throw e;
            } finally {
                memStream.Close();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        #region IAtomizable implementation
        public new bool IsSearchable (System.Collections.Specialized.NameValueCollection parameters)
        {
            
            string identifier = "";

            log.Debug ("WpsProcessOffering - ToAtomItem");

            if (this.ProviderId == 0 || this.Provider.Proxy) {
                identifier = this.Identifier;
            } else {
                identifier = this.RemoteIdentifier;
            }

            string name = (this.Name != null ? this.Name : identifier);
            string text = (this.TextContent != null ? this.TextContent : "");

            //if query on parameter q we check one of the properties contains q
            if (parameters ["q"] != null) {
                string q = parameters ["q"].ToLower ();
                if (!(name.ToLower ().Contains (q) || identifier.ToLower ().Contains (q) || text.ToLower ().Contains (q))) return false;
            }

            //case of Provider not on db (on the cloud), we don't have any identifier so we use the couple wpsUrl/pId to identify it
            if (parameters ["wpsUrl"] != null && parameters ["pId"] != null) {
                if (this.Provider.BaseUrl != parameters ["wpsUrl"] || this.RemoteIdentifier != parameters ["pId"]) return false;
            }

            //case of query for sandbox or operational provider
            if (parameters["sandbox"] != null) {
                if (parameters["sandbox"] == "true" && !this.Provider.IsSandbox) return false;
                if (parameters["sandbox"] == "false" && this.Provider.IsSandbox) return false;
            }

            //case of query on provider hostname
            if (parameters["hostname"] != null) {
                var uriHost = new UriBuilder(this.Provider.BaseUrl);
                var r = new System.Text.RegularExpressions.Regex(parameters["hostname"]);
                var m = r.Match(uriHost.Host);
                if (!m.Success) return false;
            }

            //case of query on service tags
            if (parameters["tag"] != null) {
                var queryTags = parameters["tag"].Split(",".ToCharArray()).ToList();
                var serviceTags = GetTagsAsList();

                bool found = false;
                foreach (var qtag in queryTags)
                    if (serviceTags.Any(str => str.Contains(qtag))) found = true;
                return found;
            }

            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override AtomItem ToAtomItem(NameValueCollection parameters) {

            string providerUrl = null;
            string identifier = null;

            log.Debug("WpsProcessOffering - ToAtomItem");

            if (this.ProviderId == 0 || this.Provider.Proxy) {
                providerUrl = context.BaseUrl + "/wps/WebProcessingService";
                identifier = this.Identifier;
            } else {
                identifier = this.RemoteIdentifier;
                if (this.Provider.BaseUrl.Contains("request=")) {
                    providerUrl = this.Provider.BaseUrl.Substring(0,this.Provider.BaseUrl.IndexOf("?"));
                } else {
                    providerUrl = this.Provider.BaseUrl;
                }
            }

            if (identifier == null) identifier = "";
            string name = (this.Name != null ? this.Name : identifier);
            string description = this.Description;
            string text = (this.TextContent != null ? this.TextContent : "");

            if (!IsSearchable (parameters)) return null;

            var capurl = providerUrl + "?service=WPS&request=GetCapabilities";
            log.Debug("capabilities = " + capurl);
                
            Uri capabilitiesUri = new Uri(capurl);

            AtomItem atomEntry = null;
            var entityType = EntityType.GetEntityType(typeof(WpsProcessOffering));
            Uri id = null;
            var idurl = context.BaseUrl;
            if (this.ProviderId == 0) {
                idurl = context.BaseUrl + "/" + entityType.Keyword + "/search?wpsUrl=" + HttpUtility.UrlEncode(this.Provider.BaseUrl) + "&pId=" + this.RemoteIdentifier;
            } else {
                idurl = context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier;
            }
            log.Debug("id url = " + idurl);
            id = new Uri(idurl);

            try{
                atomEntry = new AtomItem(name, description, capabilitiesUri, id.ToString(), DateTime.UtcNow);
            } catch(Exception) {
                atomEntry = new AtomItem();
            }
            log.Debug("Adding owscontext");
            OwsContextAtomEntry entry = new OwsContextAtomEntry(atomEntry);
            var offering = new OwcOffering();
            List<OwcOperation> operations = new List<OwcOperation>();

            var describeurl = providerUrl + "?service=WPS" +
                              "&request=DescribeProcess" +
                              "&version=" + this.Provider.WPSVersion +
                              "&identifier=" + identifier;
            log.Debug("describeprocess url = " + describeurl);
            Uri describeUri = new Uri(describeurl);

            var executeurl = providerUrl + "?service=WPS" +
                "&request=Execute" +
                "&version=" + this.Provider.WPSVersion +
                "&identifier=" + identifier;
            log.Debug("execute url = " + executeurl);
            Uri executeUri = new Uri(executeurl);

            operations.Add(new OwcOperation{ Method = "GET",Code = "GetCapabilities", Href = capabilitiesUri.AbsoluteUri});
            operations.Add(new OwcOperation{ Method = "GET",Code = "DescribeProcess", Href = describeUri.AbsoluteUri});
            operations.Add(new OwcOperation{ Method = "POST",Code = "Execute", Href = executeUri.AbsoluteUri});

            offering.Operations = operations.ToArray();
            entry.Offerings = new List<OwcOffering>{ offering };
            if (string.IsNullOrEmpty(this.provider.Description))
                entry.Publisher = (this.Provider != null ? this.Provider.Name : "Unknown");
            else
                entry.Publisher = this.Provider.Name + " (" + this.Provider.Description + ")";

            //categories
            if ( this.Provider.Id == 0 )
                entry.Categories.Add(new SyndicationCategory("Discovered"));
            if(this.Provider.IsSandbox) entry.Categories.Add (new SyndicationCategory ("sandbox"));
            entry.Categories.Add(new SyndicationCategory("WpsOffering"));
            foreach (var tag in GetTagsAsList ()){
                entry.Categories.Add (new SyndicationCategory (tag));
            }

            if (this.IsQuotable) entry.Categories.Add(new SyndicationCategory("Quotable"));

            entry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);

            entry.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));

            if (!string.IsNullOrEmpty(this.IconUrl)) {
                log.Debug("icon link = " + IconUrl);
                entry.Links.Add(new SyndicationLink(new Uri(this.IconUrl), "icon", null, null, 0));
            }

            return new AtomItem(entry);
        }

        public new NameValueCollection GetOpenSearchParameters() {
            var parameters = OpenSearchFactory.GetBaseOpenSearchParameter();
            parameters.Add("id", "{geo:uid?}");
            parameters.Add("wpsUrl", "{ows:url?}");
            parameters.Add("pid", "{ows:id?}");
            parameters.Add ("sandbox", "{t2:sandbox?}");
            parameters.Add ("hostname", "{t2:hostname?}");
            parameters.Add ("tag", "{t2:tag?}");
            return parameters;
        }

        #endregion

    }
}

