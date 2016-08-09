using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Terradue.ServiceModel.Syndication;
using Terradue.OpenSearch;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Engine.Extensions;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;
using Terradue.Util;


/*!
\defgroup Series Series
@{

This component manages all types of dataset series.
It implements the mechanism to search for the dataset defined in the series via an \ref OpenSearch interface.

\ingroup Core

\xrefitem int "Interfaces" "Interfaces" connects to \ref OpenSearch interfaces defined in the series to proxy the queries

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence stores persistently the series information in the database

\xrefitem dep "Dependencies" "Dependencies" \ref Authorisation controls the access on the series



@}
 */

 
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {
    
    
    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    
/*    
    public class SeriesCollection {
        
        private Dictionary<string, Series> dict = new Dictionary<string, Series>();
        private Series[] items = new Series[0];
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public int Count {
            get { return items.Length; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public Series this[string name] { 
            get { if (dict.ContainsKey(name)) return dict[name]; else return null; } 
            set { if (dict.ContainsKey(name)) dict[name] = value; } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public Series this[int index] {
            get { return items[index]; } 
            set { items[index] = value; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public string Names(int index) { 
            return items[index].Identifier; 
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public string Identifiers(int index) { 
            return items[index].Identifier; 
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public SeriesCollection() {}
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public bool Contains(string name) {
            return dict.ContainsKey(name);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public void Add(string name, Series series) {
            dict.Add(name, series);
            Array.Resize(ref items, items.Length + 1);
            items[items.Length - 1] = series;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public void Clear() {
            dict.Clear();
            Array.Resize(ref items, 0);
        }
        
    }
    
*/
    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    
    
    
    /// <summary>Data Series</summary>
    /// <description>Represents a series of data sets that are available from a catalogue.</description>
    /// \ingroup Series
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    [EntityTable("series", EntityTableConfiguration.Full, HasExtensions = true, HasDomainReference = true, HasPermissionManagement = true)]
    [EntityReferenceTable("catalogue", CATALOGUE_TABLE)]
    public class Series : Entity, IOpenSearchable {
        
        private const int CATALOGUE_TABLE = 1;
        private int catalogueId;
        private Catalogue catalogue;

        OpenSearchDescription osd;
        
        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Obsolete, please use Name instead.")]
        public string Caption { 
            get { return Name; }
            set { Name = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Detailed description of the series.</summary>
        /// \ingroup Series
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        [EntityDataField("description")]
        public string Description { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the raw catalogue description URL.
        /// </summary>
        /// <value>The raw catalogue description URL.</value>
        [EntityDataField("cat_description")]
        public string RawCatalogueDescriptionUrl { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        [EntityDataField("cat_template")]
        public string RawCatalogueUrlTemplate { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>OpenSearch description document URL of the remote dataset series</summary>
        /// \ingroup Series
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public string CatalogueDescriptionUrl { 
            get {
                if (RawCatalogueDescriptionUrl == null) throw new Exception("Missing catalogue description URL");
                if (RawCatalogueDescriptionUrl.Contains("$(CATALOGUE)")) return RawCatalogueDescriptionUrl.Replace("$(CATALOGUE)", CatalogueBaseUrl);
                else return RawCatalogueDescriptionUrl;
            }
            set {
                RawCatalogueDescriptionUrl = value;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the OpenSearch URL templete of the series.</summary>
        /// \ingroup Series
        /// The corresponding database field is filled by the background agent action <b>Catalogue&nbsp;series&nbsp;refresh</b>.
        public string CatalogueUrlTemplate { 
            get {
                if (RawCatalogueUrlTemplate == null) GetCatalogueUrlTemplate();
                if (RawCatalogueUrlTemplate == null) {
                    throw new Exception("Missing catalogue URL template");
                } else {
                    if (RawCatalogueUrlTemplate.Contains("$(CATALOGUE)")) return RawCatalogueUrlTemplate.Replace("$(CATALOGUE)", CatalogueBaseUrl);
                    else return RawCatalogueUrlTemplate;
                }
            }
            set {
                RawCatalogueUrlTemplate = value;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the database ID of the catalogue hosting this series.</summary>
        /// \ingroup Series
        [EntityDataField("id_catalogue", IsForeignKey = true)]
        public int CatalogueId {
            get {
                return (catalogue == null ? catalogueId : catalogue.Id); 
            }
            set {
                if (value != catalogueId) catalogue = null;
                catalogueId = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the catalogue hosting this series.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public Catalogue Catalogue {
            get {
                if (catalogue == null && catalogueId != 0) catalogue = Catalogue.FromId(context, catalogueId);
                return catalogue;
            }
            set {
                catalogue = value;
                if (value == null) catalogueId = 0;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("dataset_count")]
        public long DataSetCount { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the date/time when the last update was made to this series or its entries.</summary>
        [EntityDataField("last_update_time")]
        public DateTime LastUpdateTime { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the unique identifier of the catalogue hosting this series.</summary>
        /// \ingroup Series
        [EntityForeignField("name", CATALOGUE_TABLE)]
        public string CatalogueIdentifier { get; protected set; } 

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the URL part shared by all series hosted by the catalogue like this series.</summary>
        /// \ingroup Series
        [EntityForeignField("base_url", CATALOGUE_TABLE)]
        public string CatalogueBaseUrl { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the current user is authorised to search within this series.</summary>
        [EntityPermissionField("can_search")]
        public bool CanSearchWithin { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the current user is authorised to download products from this series.</summary>
        [EntityPermissionField("can_download")]
        public bool CanDownload { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the current user is authorised to use products of this series for processing.</summary>
        [EntityPermissionField("can_process")]
        public bool CanProcess { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets or sets a reference to the input files request parameter that depends on the series.</summary>
        public RequestParameter DataSetParameter { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        // ! Gets or sets the name of the identifier element (qualified with the namespace prefix).
        //public string IdentifierElementName { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets or sets the name of the identifier element (qualified with the namespace prefix).</summary>
        //public CatalogueResult CatalogueResult { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets or sets the ID of the service that must be compatible with the series.</summary>
        public int ServiceId { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new Series instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public Series(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new Series instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <returns>the created Series object</returns>
        public static Series GetInstance(IfyContext context) {
            return new Series(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new Series instance representing the series with the specified ID.</summary>
        /// \ingroup Series
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the series ID</param>
        /// <returns>the created Series object</returns>
        public static Series FromId(IfyContext context, int id) {
            EntityType entityType = EntityType.GetEntityType(typeof(Series));
            Series result = (Series)entityType.GetEntityInstanceFromId(context, id); 
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new Series instance representing the series with the specified unique identifier.</summary>
        /// \ingroup Series
        /// <param name="context">The execution environment context.</param>
        /// <param name="name">The unique series identifier.</param>
        /// <returns>The created Series object.</returns>
        public static Series FromIdentifier(IfyContext context, string identifier) {
            EntityType entityType = EntityType.GetEntityType(typeof(Series));
            Series result = (Series)entityType.GetEntityInstanceFromIdentifier(context, identifier); 
            result.Identifier = identifier;
            result.Load();
            return result;
        }

        [Obsolete("Obsolete, please use FromIdentifier instead")]
        public static Series FromName(IfyContext context, string name) {
            return FromIdentifier(context, name);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Returns a Series instance representing the series with the specified ID or name.</summary>
        /// \ingroup Series
        /// <param name="context">The execution environment context.</param>
        /// <param name="s">a search value that must match the series ID (preferred) or name.</param>
        public static Series FromString(IfyContext context, string s) {
            int id = 0;
            Int32.TryParse(s, out id);
            EntityType entityType = EntityType.GetEntityType(typeof(Series));
            Series result = (Series)entityType.GetEntityInstanceFromCondition(context, String.Format("t.identifier={0} OR t.id={1}", StringUtils.EscapeSql(s), id), String.Format("t.id!={0}", id)); 
            result.Id = id;
            result.Identifier = s;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Create the series from an OpenSearch url.
        /// </summary>
        /// <returns>The open search URL.</returns>
        /// <param name="osUrl">Os URL.</param>
        /// <param name="context">Context.</param>
        /// <param name="exists">If set to <c>true</c> exists.</param>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public static Series FromOpenSearchUrl(OpenSearchUrl osUrl, IfyContext context, bool exists = true) {
            Series result = new Series (context);
            OpenSearchDescription osdd = OpenSearchFactory.LoadOpenSearchDescriptionDocument(osUrl);

            result.Identifier = osdd.ShortName;

            if (exists) return Series.FromIdentifier(context, result.Identifier);

            result.Identifier = osdd.ShortName;
            result.Name = osdd.LongName;

            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static int GetIdFromName(IfyContext context, string identifier) {
            return context.GetQueryIntegerValue(String.Format("SELECT id FROM series WHERE identifier={0}", StringUtils.EscapeSql(identifier)));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Store() {
            if (Name == null) Name = Identifier;
            base.Store();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool CanBeUsedWithService(int serviceId) {
            return (context.GetQueryIntegerValue("SELECT COUNT(*) FROM service_series WHERE id_service=" + serviceId + " AND id_series=" + Id + ";") != 0);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public string GetCatalogueUrlTemplate() {
            string catalogueDescriptionUrl = CatalogueDescriptionUrl;
            bool usePlaceholder = (catalogueDescriptionUrl != RawCatalogueDescriptionUrl && CatalogueBaseUrl != null);
            string catalogueUrlTemplate = null;
            try {
                catalogueUrlTemplate = Terradue.Metadata.OpenSearch.OpenSearchDescription.GetUrlTemplate(catalogueDescriptionUrl, new string[] {"application/rdf+xml", "application/xhtml+xml"});
            } catch (XmlException) {
                context.AddError(String.Format("Catalogue description URL for \"{0}\" returns invalid description", Identifier));
            } catch (Exception) {
                context.AddError(String.Format("No catalogue URL template found for series \"{0}\" at {1}", Identifier, catalogueDescriptionUrl));
            }
            usePlaceholder &= catalogueUrlTemplate != null;
            RawCatalogueUrlTemplate = (usePlaceholder ? catalogueUrlTemplate.Replace(CatalogueBaseUrl, "$(CATALOGUE)" + (CatalogueBaseUrl.EndsWith("/") ? "/" : String.Empty)) : catalogueUrlTemplate);
            return RawCatalogueUrlTemplate;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public static void ExecuteCatalogueSeriesRefresh(IfyContext context) {
            string sql = "SELECT t.id, t.identifier, t.cat_description, t1.base_url FROM series AS t LEFT JOIN catalogue AS t1 ON t.id_catalogue=t1.id WHERE t.auto_refresh ORDER BY t.identifier;";
            int count = 0;
            IDataReader reader = context.GetQueryResult(sql);
            while (reader.Read()) {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                string catalogueDescriptionUrl = reader.GetString(2);
                string catalogueBaseUrl = context.GetValue(reader, 3);
                if (catalogueDescriptionUrl == null || catalogueDescriptionUrl == String.Empty) {
                    context.AddError("No catalogue description URL defined for \"" + name + "\"");
                    continue;
                }
                bool usePlaceholder = (catalogueDescriptionUrl.Contains("$(CATALOGUE)") && catalogueBaseUrl != null);
                if (usePlaceholder) catalogueDescriptionUrl = catalogueDescriptionUrl.Replace("$(CATALOGUE)", catalogueBaseUrl);
                string catalogueUrlTemplate = null;
                try {
                    catalogueUrlTemplate = Terradue.Metadata.OpenSearch.OpenSearchDescription.GetUrlTemplate(catalogueDescriptionUrl, new string[]{"application/rdf+xml", "application/xhtml+xml"});
                } catch (XmlException) {
                    context.AddError("Catalogue description URL for \"" + name + "\" returns invalid description");
                } catch (Exception) {
                    context.AddError("No catalogue URL template found for series \"" + name + "\"");
                }
                if (catalogueUrlTemplate == null) continue;
                if (usePlaceholder) catalogueUrlTemplate = catalogueUrlTemplate.Replace(catalogueBaseUrl, "$(CATALOGUE)" + (catalogueBaseUrl.EndsWith("/") ? "/" : String.Empty));
                context.Execute(String.Format("UPDATE series SET cat_template={1} WHERE id={0};", id, StringUtils.EscapeSql(catalogueUrlTemplate)));
                count++;
            }
            context.AddInfo(String.Format("Updated series: {0}", count));

            count = context.GetQueryIntegerValue("SELECT COUNT(*) FROM series WHERE NOT auto_refresh;");
            if (count != 0) context.AddInfo(String.Format("Ignored series: {0}", count)); 

            sql = "SELECT t.id, t.identifier, t.cat_description, t1.base_url FROM producttype AS t LEFT JOIN catalogue AS t1 ON t.id_catalogue=t1.id ORDER BY t.identifier;";
            reader = context.GetQueryResult(sql);
            while (reader.Read()) {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                string catalogueDescriptionUrl = reader.GetString(2);
                string catalogueBaseUrl = context.GetValue(reader, 3);
                if (catalogueDescriptionUrl == null || catalogueDescriptionUrl == String.Empty) {
                    context.AddError("No catalogue description URL defined for product type \"" + name + "\"");
                    continue;
                }
                bool usePlaceholder = (catalogueDescriptionUrl.Contains("$(CATALOGUE)") && catalogueBaseUrl != null);
                if (usePlaceholder) catalogueDescriptionUrl = catalogueDescriptionUrl.Replace("$(CATALOGUE)", catalogueBaseUrl);
                string catalogueUrlTemplate = null;
                try {
                    catalogueUrlTemplate = Terradue.Metadata.OpenSearch.OpenSearchDescription.GetUrlTemplate(catalogueDescriptionUrl, new string[]{"application/rdf+xml", "application/xhtml+xml"});
                } catch (XmlException) {
                    context.AddError("Catalogue description URL for product type \"" + name + "\" returns invalid description");
                } catch (Exception) {
                    context.AddError("No catalogue URL template found for product type \"" + name + "\"");
                }
                if (catalogueUrlTemplate == null) continue;
                if (usePlaceholder) catalogueUrlTemplate = catalogueUrlTemplate.Replace(catalogueBaseUrl, "$(CATALOGUE)" + (catalogueBaseUrl.EndsWith("/") ? "/" : String.Empty));
                context.Execute(String.Format("UPDATE producttype SET cat_template={1} WHERE id={0};", id, StringUtils.EscapeSql(catalogueUrlTemplate)));
            }
            reader.Close();

        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
/*        public static SeriesCollection GetRestrictedList(IfyContext context) {
            SeriesCollection result = new SeriesCollection();
            string sql = String.Format("SELECT DISTINCT t.id, t.identifier, t.name, t.description, t.cat_description, t.cat_template FROM series AS t INNER JOIN series_priv AS p ON t.id=p.id_series INNER JOIN usr_grp AS ug ON p.id_grp=ug.id_grp AND (p.id_usr={0} OR ug.id_usr={0}) WHERE {1};",
                                       context.UserId,
                                       "true" // condition
                                       );
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(sql, dbConnection);
            while (reader.Read()) {
                Series series = Series.GetInstance(context);
                series.Id = reader.GetInt32(0);
                series.Identifier = context.GetValue(reader, 1);
                series.Name = context.GetValue(reader, 2);
                series.Description = context.GetValue(reader, 3);
                series.RawCatalogueDescriptionUrl = context.GetValue(reader, 4);
                series.RawCatalogueUrlTemplate = context.GetValue(reader, 5);
                result.Add(series.Identifier, series);
            }
            context.CloseQueryResult(reader, dbConnection);

            return result;
        }
*/        
        /// <summary>Generates the corresponding OpenSearch description.</summary>
        /// <returns>An OpenSearch description document.</returns>
        /// \ingroup Series
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual OpenSearchDescription GetLocalOpenSearchDescription(string basePath) {

            OpenSearchDescription osd = GetOpenSearchDescription();
            osd.ShortName = this.Identifier;
            osd.LongName = this.Name;
            osd.Developer = "Terradue GeoSpatial Development Team";
            osd.SyndicationRight = "open";
            osd.AdultContent = "false";
            osd.Language = "en-us";
            osd.OutputEncoding = "UTF-8";
            osd.InputEncoding = "UTF-8";
            osd.Description = String.Format("This Search Service performs queries in the series {0}. This search service is in accordance with the OGC 10-032r3 specification.", Identifier);

            return osd;
        }

        /// <summary>Updates the count cache.</summary>
        /// <param name="count">Count.</param>
        /// \ingroup Series
        public void UpdateCountCache(long count) {
            context.Execute(String.Format("UPDATE series SET dataset_count={1}, last_update_time='{2}' WHERE id={0};", Id, count, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        /// <summary>Try to get the Count cache.</summary>
        /// <returns>The cache count if up to date</returns>
        /// <param name="throwException">If set to <c>true</c>, it throws exception if the value is missing or not up to date.</param>
        /// \ingroup Series
        public virtual ulong CountCache (bool throwException) {
            int seriesInfoValidityTime = StringUtils.StringToSeconds(context.GetConfigValue("SeriesInfoValidityTime"));
            DateTime outdatedEndTime = DateTime.UtcNow.AddSeconds(- seriesInfoValidityTime);

            string countStr = context.GetQueryStringValue(String.Format("SELECT dataset_count FROM series WHERE id={0} AND last_update_time>'{1}';", Id, outdatedEndTime.ToString("yyyy-MM-dd HH:mm:ss"), seriesInfoValidityTime));
            if (countStr == null && throwException) throw new ResourceNotFoundException("series count expired");

            ulong result;
            UInt64.TryParse(countStr, out result);
            return result;
        }

        #region IOpenSearchable implementation

        public virtual OpenSearchRequest Create(string type, NameValueCollection parameters) {
            return OpenSearchRequest.Create(this, type, parameters);
        }

        public virtual QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByContentTypeAbility(this.DefaultMimeType);
            if (osee == null)
                return null;
            return new QuerySettings(this.DefaultMimeType, osee.ReadNative);
        }

        [EntityDataField("default_mime_type")]
        public virtual string DefaultMimeType { get; set; }

        /// <summary>
        /// Opensearch parameters.
        /// </summary>
        /// <returns>The search parameters.</returns>
        /// <param name="mimeType">MIME type.</param>
        /// \ingroup Series
        public virtual NameValueCollection GetOpenSearchParameters(string mimeType) {
            NameValueCollection nvc = new NameValueCollection ();
            OpenSearchDescription osd = this.GetOpenSearchDescription ();

            //OpenSearchDescriptionUrl[] osu = osd.Url.Where(u => u.Type == mimeType).Select(u => u).ToArray();

            if (osu[0] != null) {
                nvc = HttpUtility.ParseQueryString(new Uri(osu[0].Template).Query);
            }

            return nvc;

        }

        /// <summary>
        /// Query the remote catalogue for the open search description.
        /// </summary>
        /// <returns>
        /// an open search description.
        /// </returns>
        /// \ingroup Series
        public virtual OpenSearchDescription GetOpenSearchDescription(){
            if ( osd == null )  
                osd = OpenSearchFactory.LoadOpenSearchDescriptionDocument(new OpenSearchUrl(CatalogueDescriptionUrl));
            return osd;
        }

        public virtual string GetName ()
        {
            return Name;
        }

        long totalResult = 0;
        public virtual long TotalResults {
            get {
                
                if (totalResult <= 0) {
                    OpenSearchEngine ose = new OpenSearchEngine();
                    AtomOpenSearchEngineExtension aosee = new AtomOpenSearchEngineExtension();
                    ose.RegisterExtension(aosee);

                    try {
                        // Let's try to get the cache count value
                        totalResult = (long)CountCache(true);
                    } catch (ResourceNotFoundException) {

                        // update the series table with data retrieved by the acs catalog
                        try {
                    
                            AtomFeed osr = (AtomFeed)ose.Query(this, new NameValueCollection(), typeof(AtomFeed));
                            totalResult = osr.TotalResults;

                        } catch (Exception) {
                            // no error managment, set the number of product to 0
                            totalResult = 0;
                        }

                        // update series table with new value
                        this.UpdateCountCache(totalResult);

                    }
                }

                return totalResult;
            }
        }

        public virtual bool CanCache {
            get { return false; }
        }


        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr, string finalContentType) {
        }
        #endregion

    }

    
    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    
    
    
    public class CustomSeries : Series {
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>.</summary>
        public CustomSeries(IfyContext context, string identifier, string name, string catalogueDescriptionUrl, string catalogueUrlTemplate) : base(context) {
            this.Identifier = identifier;
            this.Name = name;
            this.Description = name;
            this.CatalogueDescriptionUrl = catalogueDescriptionUrl;
            this.CatalogueUrlTemplate = catalogueUrlTemplate;
        }
        
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    [EntityTable("service_series", EntityTableConfiguration.Custom, IdField = "id_series")]
    public class SeriesServiceCompatibility : Entity {

        [EntityDataField("is_default")]
        public bool IsDefault { get; set; }

        public SeriesServiceCompatibility(IfyContext context) : base(context) {}

    }

    public class SeriesServiceCompatibilityCollection : EntityList<SeriesServiceCompatibility> {

        public SeriesServiceCompatibilityCollection(IfyContext context) : base(context) {}

        //public override void  {

        //}

    }

}

