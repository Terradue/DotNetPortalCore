using System;
using System.Collections.Specialized;
using System.Linq;
using System.Data;
using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;
using System.Web;
using Terradue.Util;
using Terradue.OpenSearch;


/*!
\defgroup core_Series Series
@{
This component represents a dataset series. Practically it is a non-final component that can be extended to implement other collections.

\ingroup core

\section sec_core_ComputingResourceDependencies Dependencies

- \ref core_DataModelAccess, used to store persistently the series information in the database
- \ref core_UserGroupACL, used to check the access on the series

@}
 */

 
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using Terradue.OpenSearch.Engine.Extensions;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Schema;
using Terradue.ServiceModel.Syndication;





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
    
    
    
    /// <summary>Represents a series of data sets that are available from a catalogue.</summary>
    /// \ingroup core_Series
    [EntityTable("series", EntityTableConfiguration.Full, HasExtensions = true, HasPrivilegeManagement = true)]
    [EntityReferenceTable("catalogue", CATALOGUE_TABLE)]
    public class Series : Entity, IOpenSearchable {
        
        private const int CATALOGUE_TABLE = 1;
        private int catalogueId;
        private Catalogue catalogue;

        OpenSearchDescription osd;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// \ingroup core_Series
        [Obsolete("Obsolete, please use Name instead.")]
        public string Caption { 
            get { return Name; }
            set { Name = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the detailed description of the series.</summary>
        /// \ingroup core_Series
        [EntityDataField("description")]
        public string Description { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        [EntityDataField("cat_description")]
        public string RawCatalogueDescriptionUrl { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        [EntityDataField("cat_template")]
        public string RawCatalogueUrlTemplate { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the OpenSearch description URL of the series.</summary>
        /// \ingroup core_Series
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
        /// \ingroup core_Series
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

        /// <summary>Gets or sets the ID of the catalogue hosting the series.</summary>
        /// \ingroup core_Series
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

        /// <summary>Gets or sets the Download Manager that handles the Data Access Request.</summary>
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

        /// <summary>Gets the catalogue base URL.</summary>
        /// \ingroup core_Series
        [EntityForeignField("name", CATALOGUE_TABLE)]
        public string CatalogueIdentifier { get; protected set; } 

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the catalogue base URL.</summary>
        /// \ingroup core_Series
        [EntityForeignField("base_url", CATALOGUE_TABLE)]
        public string CatalogueBaseUrl { get; protected set; } 
        
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
        public static new Series GetInstance(IfyContext context) {
            return new Series(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new Series instance representing the series with the specified ID.</summary>
        /// \ingroup core_Series
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
        /// \ingroup core_Series
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
        /// \ingroup core_Series
        /// <param name="context">The execution environment context.</param>
        /// <param name="s">a search value that must match the series ID (preferred) or name</param>
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

        public static Series FromOpenSearchUrl (OpenSearchUrl osUrl, IfyContext context, Boolean exists = true)
        {
            Series result = new Series (context);
            OpenSearchDescription osdd = OpenSearchFactory.LoadOpenSearchDescriptionDocument (osUrl);

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
        /// \ingroup core_Series
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
        /// \ingroup core_Series
		public void UpdateCountCache(long count) {
            context.Execute(String.Format("UPDATE series SET dataset_count={1}, last_update_time='{2}' WHERE id={0};", Id, count, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        /// <summary>Try to get the Count cache.</summary>
        /// <returns>The cache count if up to date</returns>
        /// <param name="throwException">If set to <c>true</c>, it throws exception if the value is missing or not up to date.</param>
        /// \ingroup core_Series
		public ulong CountCache (bool throwException) {
            int seriesInfoValidityTime = StringUtils.StringToSeconds(context.GetConfigValue("SeriesInfoValidityTime"));
            DateTime outdatedEndTime = DateTime.UtcNow.AddSeconds(- seriesInfoValidityTime);

            string countStr = context.GetQueryStringValue(String.Format("SELECT dataset_count FROM series WHERE id={0} AND last_update_time>'{1}';", Id, outdatedEndTime.ToString("yyyy-MM-dd HH:mm:ss"), seriesInfoValidityTime));
            if (countStr == null && throwException) throw new ResourceNotFoundException("series count expired");

            ulong result;
            UInt64.TryParse(countStr, out result);
            return result;
        }

        #region IOpenSearchable implementation

        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr) {

        }

		public OpenSearchRequest Create(string type, NameValueCollection parameters) {
			return OpenSearchRequest.Create(this, type, parameters);
		}

        public QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByContentTypeAbility(this.DefaultMimeType);
            if (osee == null)
                return null;
            return new QuerySettings(this.DefaultMimeType, osee.ReadNative);
        }

        [EntityDataField("default_mime_type")]
        public string DefaultMimeType { get; set; }

        /// <summary>
        /// Opensearch parameters.
        /// </summary>
        /// <returns>The search parameters.</returns>
        /// <param name="mimeType">MIME type.</param>
        /// \ingroup core_Series
        public NameValueCollection GetOpenSearchParameters(string mimeType) {
			NameValueCollection nvc = new NameValueCollection ();
			OpenSearchDescription osd = this.GetOpenSearchDescription ();

			OpenSearchDescriptionUrl[] osu = osd.Url.Where(u => u.Type == mimeType).Select(u => u).ToArray();

			if (osd.Url [0] != null) {
				nvc = HttpUtility.ParseQueryString(osd.Url [0].Template);
			}

			return nvc;

        }

        /// <summary>
        /// Query the remote catalogue for the open search description.
        /// </summary>
        /// <returns>
        /// an open search description.
        /// </returns>
        /// \ingroup core_Series
        public virtual OpenSearchDescription GetOpenSearchDescription(){
            if ( osd == null )  
                osd = OpenSearchFactory.LoadOpenSearchDescriptionDocument(new OpenSearchUrl(CatalogueDescriptionUrl));
            return osd;
        }

        public virtual string GetName ()
        {
            return Name;
        }

        public OpenSearchUrl GetSearchBaseUrl(string mimetype) {
            return new OpenSearchUrl (string.Format("{0}/series/{1}/search", context.BaseUrl, this.Identifier));
        }

        public long TotalResults { 
            get { return this.GetTotalResults(); } 
        }

        /// <summary>
        /// Get the OpenSearch field TotalResults.
        /// </summary>
        /// <returns>The results.</returns>
        /// <param name="ose">Ose.</param>
        /// \ingroup core_Series
		public long GetTotalResults() {
			long result = 0;
            int seriesInfoValidityTime = StringUtils.StringToSeconds(context.GetConfigValue("SeriesInfoValidityTime"));

			OpenSearchEngine ose = new OpenSearchEngine();
			AtomOpenSearchEngineExtension aosee = new AtomOpenSearchEngineExtension();
            ose.RegisterExtension(aosee);
            DateTime outdatedEndTime = DateTime.UtcNow.AddSeconds(- seriesInfoValidityTime);

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT dataset_count FROM series WHERE id={0} AND last_update_time>'{1}';", Id, outdatedEndTime.ToString ("yyyy-MM-dd HH:mm:ss"), seriesInfoValidityTime), dbConnection);
            if (!reader.Read ()){
                // the value is not up to date
                context.CloseQueryResult(reader, dbConnection);

                // update the series table with data retrieved by the acs catalog
                try{
                    NameValueCollection nvc = new NameValueCollection();
                    nvc.Add ("count","0");

                    IOpenSearchResult osr = ose.Query(this,nvc,typeof(AtomFeed));
                    AtomFeed sc = (AtomFeed)osr.Result;

                    result = Int64.Parse(sc.ElementExtensions.ReadElementExtensions<string>("totalResults","http://a9.com/-/spec/opensearch/1.1/").Single());

                } catch (Exception e) {
                    // no error managment, set the number of product to 0
                    result = 0;
                }

                // update series table with new value
                this.UpdateCountCache(result);

            } else {
                // the value is up to date
                result = Int64.Parse(context.GetValue (reader, 0));
                context.CloseQueryResult(reader, dbConnection);
            }

            return result;
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

