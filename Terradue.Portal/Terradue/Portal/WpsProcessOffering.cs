using System;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Result;
using System.Collections.Specialized;
using Terradue.ServiceModel.Syndication;
using Terradue.ServiceModel.Ogc.OwsContext;
using System.Collections.Generic;

//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Web;

/*!
\defgroup WpsService WPS Service
@{

This component is an extension of \ref Service for providing with WPS process as processing offerings.
Practically, it get services offered in the \ref WpsProvider associated and then for each process exposed,
retrieve the DescribeProcess() from \ref WpsProvider to describe the process with input and ouput parameters.

\ingroup "Core"

\xrefitem dep "Dependencies" "Dependencies" extends \ref Service for WPS specific offerings

\xrefitem dep "Dependencies" "Dependencies" exports WPS Service as a service resource as a \ref OWSContext model.

\xrefitem int "Interfaces" "Interfaces" implements \ref OpenSearchable interface to search WPS Service in OpenSearch.



@}
*/


namespace Terradue.Portal {



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Represents a WPS process offering on a remote WPS server.</summary>
    /// \ingroup WpsService
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("wpsproc", EntityTableConfiguration.Custom)]
    public class WpsProcessOffering : Service, IAtomizable {

        private WpsProvider provider;

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataFieldAttribute("id_provider", IsForeignKey = true)]
        public int ProviderId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the remote identifier of the process offering on the \ref WPSProvider it belongs to.</summary>
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

        /// <summary>Gets or sets the WPS provider to which the WPS process offering belongs.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        /// \return is offered by \ref WpsProvider thats host the service of the offering.
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

        public override void BuildTask(Task task) {
            //this.ComputingResourceId = Provider;
        }

        //---------------------------------------------------------------------------------------------------------------------

        #region IAtomizable implementation

        public new AtomItem ToAtomItem(NameValueCollection parameters) {

            string providerUrl = null;
            string identifier = null;

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

            string name = (this.Name != null ? this.Name : identifier);
            string description = this.Description;
            string text = (this.TextContent != null ? this.TextContent : "");

            //if query on parameter q we check one of the properties contains q
            if (parameters["q"] != null) {
                string q = parameters["q"].ToLower();
                if (!(name.ToLower().Contains(q) || identifier.ToLower().Contains(q) || text.ToLower().Contains(q))) return null;
            }

            //case of Provider not on db (on the cloud), we don't have any identifier so we use the couple wpsUrl/pId to identify it
            if (parameters["wpsUrl"] != null && parameters["pId"] != null) {
                if (this.Provider.BaseUrl != parameters["wpsUrl"] || this.RemoteIdentifier != parameters["pId"]) return null;
            }
                
            Uri capabilitiesUri = new Uri(providerUrl + "?service=WPS" + 
                                          "&request=GetCapabilities");

            AtomItem atomEntry = null;
            var entityType = EntityType.GetEntityType(typeof(WpsProcessOffering));
            Uri id = null;
            if(this.ProviderId == 0) id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?wpsUrl=" + HttpUtility.UrlEncode(this.Provider.BaseUrl) + "&pId=" + this.RemoteIdentifier);
            else  id = new Uri(context.BaseUrl + "/" + entityType.Keyword + "/search?id=" + this.Identifier);
            try{
                atomEntry = new AtomItem(name, description, capabilitiesUri, id.ToString(), DateTime.UtcNow);
            }catch(Exception e){
                atomEntry = new AtomItem();
            }
            OwsContextAtomEntry entry = new OwsContextAtomEntry(atomEntry);
            var offering = new OwcOffering();
            List<OwcOperation> operations = new List<OwcOperation>();


            Uri describeUri = new Uri(providerUrl + "?service=WPS" +
                                      "&request=DescribeProcess" +
                                      "&version=" + this.Version +
                                      "&identifier=" + identifier);
            Uri executeUri = new Uri(providerUrl + "?service=WPS" +
                                     "&request=Execute" +
                                     "&version=" + this.Version +
                                     "&identifier=" + identifier);

            operations.Add(new OwcOperation{ Method = "GET",Code = "GetCapabilities", Href = capabilitiesUri});
            operations.Add(new OwcOperation{ Method = "GET",Code = "DescribeProcess", Href = describeUri});
            operations.Add(new OwcOperation{ Method = "POST",Code = "Execute", Href = executeUri});

            offering.Operations = operations.ToArray();
            entry.Offerings = new List<OwcOffering>{ offering };
            if (string.IsNullOrEmpty(this.provider.Description))
                entry.Publisher = (this.Provider != null ? this.Provider.Name : "Unknown");
            else
                entry.Publisher = this.Provider.Name + " (" + this.Provider.Description + ")";
            if ( this.Provider.Id == 0 )
                entry.Categories.Add(new SyndicationCategory("Discovered"));
            entry.Categories.Add(new SyndicationCategory("WpsOffering"));
            entry.ElementExtensions.Add("identifier", "http://purl.org/dc/elements/1.1/", this.Identifier);

            entry.Links.Add(new SyndicationLink(id, "self", name, "application/atom+xml", 0));

            if (!string.IsNullOrEmpty(this.IconUrl)) {
                entry.Links.Add(new SyndicationLink(new Uri(this.IconUrl), "icon", null, null, 0));
            }

            return new AtomItem(entry);
        }

        public NameValueCollection GetOpenSearchParameters() {
            var parameters = OpenSearchFactory.GetBaseOpenSearchParameter();
            parameters.Add("id", "{geo:uid?}");
            parameters.Add("wpsUrl", "{ows:url?}");
            parameters.Add("pid", "{ows:id?}");
            return parameters;
        }

        #endregion

    }
}

