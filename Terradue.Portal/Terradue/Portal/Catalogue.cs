using System;
using System.Collections.Specialized;
using System.Data;
using System.Web;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Engine.Extensions;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
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

    

    /// <summary>Represents a metadata catalogue.</summary>
    /// <remarks>
    ///     <p>The catalogue follows the schema adopted by the Terradue Catalogue Access Service (CAS) and other metadata catalogues in which entries are catagorised as series and datasets and organised using virtual URLs that make their relationship clear.</p>
    ///     <list type="bullet">
    ///         <item>Series (often also referred to as datasets or collections) contain products of a certain type. They can be likened as folders in a file system.</item>
    ///         </item>Data sets (also called products) are the individual items that belong to a series. In the file system metaphor, they would correspond to the files.</item>
    ///     </list>
    ///     <p>A typical CAS provides RESTful interfaces for adding, altering or deleting series and data sets where the URLs are organised as &lt;base-url&gt;[/&lt;series-name&gt;[/&lt;dataset-name&gt;]]/&lt;format&gt;. The Catalogue class follows this model.</p>
    ///     <p>Note that there is ongoing confusion between the terms series (or dataset) on one side and data set (or product) on the other side.</p>
    /// </remarks>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("catalogue", EntityTableConfiguration.Custom, IdentifierField = "name")]
    public class Catalogue : Entity, IOpenSearchable {

        OpenSearchDescription osd;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the description.</summary>
        [EntityDataField("description")]
        public string Description { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>URL of the \ref OpenSearch description document link of the Catalogue</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("osd_url")]
        public string OpenSearchDescriptionUrl {
            get { return (OpenSearchDescriptionUri == null ? null : OpenSearchDescriptionUri.AbsoluteUri); }
            set { OpenSearchDescriptionUri = new Uri(value); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the base URL part that precedes all relative URLs to series or datasets.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("base_url")]
        public string BaseUrl {
            get { return (BaseUri == null ? null : BaseUri.AbsoluteUri); }
            set { BaseUri = new Uri(value); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the REST URL at which new series are ingested.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("series_rel_url")]
        public string SeriesIngestionUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the REST URL at which new data sets are ingested.</summary>
        /// <remarks>Dataset URLs are usually "file nodes" under a series "directory node". The placeholder <c>$(SERIES)</c> represents the name of the series to which the dataset belongs.</remarks>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("dataset_rel_url")]
        public string DataSetIngestionUrl { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Obsolete, please use OpenSearchDescriptionUri instead.")]
        public Uri osdUrl {
            get { return OpenSearchDescriptionUri; }
            protected set { OpenSearchDescriptionUri = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Use BaseUri instead.")]
        public Uri baseUrl {
            get { return BaseUri; }
            protected set { BaseUri = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the URI at which this catalogue's OpenSearch description can be accessed.</summary>
        public Uri OpenSearchDescriptionUri { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the URI at which this catalogue's OpenSearch description can be accessed.</summary>
        public Uri BaseUri { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Catalogue instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Catalogue(IfyContext context) : base(context) {
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Catalogue instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>The created Catalogue object.</returns>
        public static Catalogue GetInstance(IfyContext context) {
            return new Catalogue(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Catalogue instance representing the catalogue with the specified database ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The database ID of the catalogue.</param>
        /// <returns>The created Catalogue object.</returns>
        public static Catalogue FromId(IfyContext context, int id) {
            Catalogue result = new Catalogue(context);
            result.Id = id;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new Catalogue instance representing the catalogue with the specified unique identifier.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="identifier">The unique identifier of the catalogue.</param>
        /// <returns>The created Catalogue object.</returns>
        public static Catalogue FromIdentifier(IfyContext context, string identifier) {
            Catalogue result = new Catalogue(context);
            result.Identifier = identifier;
            result.Load();
            return result;
        }

        [Obsolete("Use FromIdentifier instead")]
        public static Catalogue FromName(IfyContext context, string name) {
            return FromIdentifier(context, name);
        }

        //---------------------------------------------------------------------------------------------------------------------

        #region IOpenSearchable implementation

        public int CompareTo(object obj) {
            throw new NotImplementedException();
        }

        public OpenSearchUrl GetSearchBaseUrl(string mimetype) {
            return new OpenSearchUrl(string.Format("{0}/catalogue/{1}/search", context.BaseUrl, this.Identifier));
        }

        public QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByContentTypeAbility(this.DefaultMimeType);
            if (osee == null) return null;
            return new QuerySettings(this.DefaultMimeType, osee.ReadNative);
        }

        public string DefaultMimeType {
            get {
                return "application/atom+xml";
            }
        }

        public OpenSearchRequest Create(QuerySettings querySettings, NameValueCollection parameters) {
            return OpenSearchRequest.Create(this, querySettings, parameters);
        }

        public OpenSearchDescription GetOpenSearchDescription() {

            if (osd == null) osd = OpenSearchFactory.LoadOpenSearchDescriptionDocument(new OpenSearchUrl(this.OpenSearchDescriptionUri));
            return osd;

        }

        public NameValueCollection GetOpenSearchParameters(string mimeType) {
            OpenSearchDescription osd = this.GetOpenSearchDescription(); 
            foreach (OpenSearchDescriptionUrl url in osd.Url) {
                if (url.Type == mimeType) return HttpUtility.ParseQueryString(new Uri(url.Template).Query);
            }

            return null;
        }

        public long TotalResults {
            get {
                return 0;

            }
        }

        public virtual bool CanCache {
            get { return false; }
        }


        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType) {
        }
        #endregion
    }

}

