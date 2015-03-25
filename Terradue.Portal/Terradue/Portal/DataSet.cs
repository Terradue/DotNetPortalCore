using System;
using System.Collections.Generic;
using System.Collections;
using System.Xml;
using System.Text.RegularExpressions;
using Terradue.Util;
using Terradue.OpenSearch;
using System.Collections.Specialized;

//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using Terradue.OpenSearch.Engine;
using Terradue.OpenSearch.Request;
using Terradue.OpenSearch.Response;
using Terradue.OpenSearch.Result;
using Terradue.OpenSearch.Schema;


namespace Terradue.Portal {
	
    /// <summary>
    /// Data set.
    /// </summary>
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    public class DataSet : Entity, IOpenSearchable {
		//public string Identifier { get; set; }
		private Dictionary<KeyValuePair<string,string>, object> elementExtensions;

		public DataSet(IfyContext context) : base(context) {
			elementExtensions = new Dictionary<KeyValuePair<string, string>, object>();
		}

		public Dictionary<KeyValuePair<string,string>, object> ElementExtensions { get { return elementExtensions; } }

		public void AddElementExtension(string name, string nspace, object extension) {
			elementExtensions.Add(new KeyValuePair<string, string>(name, nspace), extension);
		}

		#region IOpenSearchable implementation

        public QuerySettings GetQuerySettings(OpenSearchEngine ose) {
            IOpenSearchEngineExtension osee = ose.GetExtensionByContentTypeAbility(this.DefaultMimeType);
            if (osee == null)
                return null;
            return new QuerySettings(this.DefaultMimeType, osee.ReadNative);
        }

        public string DefaultMimeType {
            get {
                return "application/atom+xml";
            }
        }

		public OpenSearchRequest Create(string type, NameValueCollection parameters) {
			return OpenSearchRequest.Create(this, type, parameters);
		}

		public NameValueCollection GetOpenSearchParameters(string mimeType) {
			NameValueCollection nvc = OpenSearchFactory.GetBaseOpenSearchParameter();
			return nvc;
        }

		public OpenSearchDescription GetOpenSearchDescription() {

			return null;
		}

        public void ApplyResultFilters(OpenSearchRequest request, ref IOpenSearchResultCollection osr) {

        }

        public long TotalResults { 
            get { return 1; } 
        }

        public OpenSearchUrl GetSearchBaseUrl(string mimetype) {
            return new OpenSearchUrl (string.Format("{0}/dataset/{1}/search", context.BaseUrl, this.Identifier));
        }


        public ParametersResult DescribeParameters() {
            return OpenSearchFactory.GetDefaultParametersResult();
        }
		#endregion

	}

	public class DataSetCollection {
		private DataSetInfo[] dataSetInfos;

		public int Count { get; protected set; }

		public long TotalSize {
			get {
				long result = 0;
				foreach (DataSetInfo dataSetInfo in dataSetInfos) result += dataSetInfo.Size;
				return result;
			}
		}

		public virtual DataSetInfo this[int index] {
			get { return dataSetInfos[index]; }
			set { dataSetInfos[index] = value; }
		}

		public DataSetCollection() {
		}

		public DataSetCollection(int count) {
			this.dataSetInfos = new DataSetInfo[count];
			this.Count = count;
		}

		public void Append(int count) {
			Array.Resize(ref dataSetInfos, this.Count += count);
		}
	}

	public class CatalogueDataSetCollection : DataSetCollection {
		private CatalogueDataSetInfo[] dataSetInfos;

		public new long TotalSize {
			get {
				long result = 0;
				foreach (CatalogueDataSetInfo dataSetInfo in dataSetInfos) result += dataSetInfo.Size;
				return result;
			}
		}

		public override DataSetInfo this[int index] {
			get { return dataSetInfos[index]; }
			set { dataSetInfos[index] = value as CatalogueDataSetInfo; }
		}

		public CatalogueDataSetCollection(int count) : base() {
			this.dataSetInfos = new CatalogueDataSetInfo[count];
			this.Count = count;
		}

		public new void Append(int count) {
			Array.Resize(ref dataSetInfos, this.Count += count);
		}
	}
	//-------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------
	public class DataSetInfo {
		private string resource;
		private string identifier;
		private long size;
		private DateTime creationTime;
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets the resource URL of the data set.</summary>
		public virtual string Resource { 
			get { return resource; }
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets the unique identifier of the data set.</summary>
		public virtual string Identifier {
			get { return identifier; }
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets the size of the data set file.</summary>
		public virtual long Size {
			get { return size; }
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets the size of the data set file.</summary>
		public virtual DateTime CreationTime {
			get { return creationTime; }
		}
		//---------------------------------------------------------------------------------------------------------------------
		public DataSetInfo(string resource, string identifier, long size, DateTime creationTime) {
			this.resource = resource;
			this.identifier = identifier;
			this.size = size;
			this.creationTime = creationTime;
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>.</summary>
		public virtual TypedValue this[string name] {
			get { return TypedValue.Empty; }
		}
		//---------------------------------------------------------------------------------------------------------------------
		public DataSetInfo() {
		}
	}
	//-------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------
	public class CatalogueDataSetInfo : DataSetInfo {
		//protected DataSetInfo[] subDatasets;
		private CatalogueResult result;
		private int index;
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets the resource URL of the data set.</summary>
		public override string Resource {
			get { return result.DataSetResources[index]; }
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets the unique identifier of the data set.</summary>
		public override string Identifier {
			get { return result.DataSetIdentifiers[index]; }
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets the size of the data set file.</summary>
		public override long Size {
			get { return result.DataSetSizes[index]; }
		}
		//---------------------------------------------------------------------------------------------------------------------
		// / <summary>Indicates or determines whether the data set is used as an input of a job of a task.</summary>
		//public bool Used { get; set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>.</summary>
		public CatalogueDataSetInfo(CatalogueResult result, int index) {
			this.result = result;
			this.index = index;
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>.</summary>
		public override TypedValue this[string name] {
			get { return result.GetMetadataValue(index, name); }
		}
	}
	//-------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------
	//-------------------------------------------------------------------------------------------------------------------------
	public class CatalogueResult {
		private IfyContext context;
		private XmlNamespaceManager namespaceManager;
		private Dictionary<string, TypedValue[]> metadataValues = new Dictionary<string, TypedValue[]>();
		private Dictionary<string, string> metadataElementNames = new Dictionary<string, string>();
		private int minCount, maxCount;
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets or sets the request URL.</summary>
		public string Url { get; protected set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets or sets the request parameter for the input files.</summary>
		public ServiceParameter DataSetParameter { get; protected set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets or sets the maximum number of results for a partial request.</summary>
		public int ResultsPerRequest { get; set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets the total number of results matching the request criteria.</summary>
		public int TotalResults { get; protected set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets the number of results actually received from the catalogue.</summary>
		public int ReceivedResults { get; protected set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets an array of data sets retrieved by a catalogue query on the series.</summary>
		public CatalogueDataSetInfo[] DataSets { get; protected set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets an array of of data set identifiers retrieved by a catalogue query on the series. </summary>
		public string[] DataSetIdentifiers { get; protected set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets an array of of data set resource URLs retrieved by a catalogue query on the series. </summary>
		public string[] DataSetResources { get; protected set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets an array of of data set file sizes retrieved by a catalogue query on the series. </summary>
		public long[] DataSetSizes { get; protected set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets or sets the name of the identifier element (qualified with the namespace prefix).</summary>
		protected string DataSetXmlName { get; set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets or sets the name of the identifier element (qualified with the namespace prefix).</summary>
		protected string ResourceXmlName { get; set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets or sets the name of the identifier element (qualified with the namespace prefix).</summary>
		protected string IdentifierXmlName { get; set; }
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets or sets the name of the identifier element (qualified with the namespace prefix).</summary>
		protected string SizeXmlName { get; set; }
		//---------------------------------------------------------------------------------------------------------------------
		/*        /// <summary>Gets or sets the name of the identifier element (qualified with the namespace prefix).</summary>
        protected bool ResourceFromXmlAttribute { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the identifier element (qualified with the namespace prefix).</summary>
        protected bool IdentifierFromXmlAttribute { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the identifier element (qualified with the namespace prefix).</summary>
        protected bool SizeFromXmlAttribute { get; set; }*/
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>Gets a dictionary of metadata element names.</summary>
		/*!
            The dictionary contains in its key the metadata names and in its values the corresponding XML element of the catalogue result RDF content.
        */
		public Dictionary<string, string> MetadataElementNames { 
			get { return metadataElementNames; }
			protected set { metadataElementNames = value; }
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>.</summary>
		public CatalogueResult(IfyContext context, string url, int resultsPerRequest) {
			this.context = context;
			this.Url = url;
			this.ResultsPerRequest = resultsPerRequest;
			this.DataSetXmlName = "//dclite4g:DataSet";
			this.ResourceXmlName = "@rdf:about";
			this.IdentifierXmlName = "dc:identifier"; // !!!
			this.SizeXmlName = "eop:size"; // !!!
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>.</summary>
		public CatalogueResult(IfyContext context, ServiceParameter dataSetParameter) {
			this.context = context;
			this.DataSetParameter = dataSetParameter;
		}
		//---------------------------------------------------------------------------------------------------------------------
		public void GetTotalResults() {
			if (Url != null) {
                
				// Get the element names from the configuration file
				// Get first result
				context.AddDebug(2, "Catalogue query (base): " + Url);
				if (!AddResultsFromRdf(Url, 0, 0)) return;
                
				context.AddDebug(2, "Catalogue total results: " + TotalResults);

			} else if (DataSetParameter != null) {
                
				TotalResults = DataSetParameter.Values.Length;
			}
		}
		//---------------------------------------------------------------------------------------------------------------------
		public void GetDataSets(int minCount, int maxCount) {
			this.minCount = minCount;
			this.maxCount = maxCount;
            
			if (Url != null) {
                
				// Get the element names from the configuration file
				XmlDocument configDoc = new XmlDocument();
				XmlElement catalogueElem = null;
				bool success = false;
				try {
					configDoc.Load(context.SiteConfigFolder + "/services.xml");
					success = ((catalogueElem = configDoc.SelectSingleNode("/services/dataset/catalogue[@type='application/rdf+xml']") as XmlElement) != null);
				} catch (Exception) {
				}
                
				if (success) {
					for (int i = 0; i < catalogueElem.ChildNodes.Count; i++) {
						XmlElement elem = catalogueElem.ChildNodes[i] as XmlElement;
						if (elem == null || !elem.HasAttribute("name")) {
							continue;
						}
						switch (elem.Attributes["name"].Value) {
							case "dataset":
								DataSetXmlName = elem.InnerXml;
								break;
							case "value":
								ResourceXmlName = elem.InnerXml;
								break;
							case "_identifier":
								IdentifierXmlName = elem.InnerXml;
								break;
							case "_size":
								SizeXmlName = elem.InnerXml;
								break;
						}
					}
				} else if (context.UserLevel == UserLevel.Administrator) {
					context.AddWarning("Invalid catalogue metadata configuration");
				}
                
				// Get first result
				context.AddDebug(2, "Catalogue query (base): " + Url);
				if (!AddResultsFromRdf(Url, 0, (maxCount > ResultsPerRequest ? ResultsPerRequest : maxCount))) {
					context.WriteInfo(String.Format("Return 1"));
					return;
				}
                
				if (maxCount == 0 || TotalResults < minCount) {
					InitializeData(0);
					return;
				}
                
				// If necessary, do more requests
				if (maxCount > TotalResults) maxCount = TotalResults;
				while (ReceivedResults < maxCount) {
					if (!AddResultsFromRdf(Url, ReceivedResults, (maxCount > ReceivedResults + ResultsPerRequest ? ResultsPerRequest : maxCount - ReceivedResults))) break;
				}
				context.AddDebug(2, "Catalogue results: " + ReceivedResults + " (total results: " + TotalResults + ")");

			} else if (DataSetParameter != null && context is IfyWebContext) {
                
				TotalResults = DataSetParameter.Values.Length;
				ReceivedResults = TotalResults;
                
				InitializeData(ReceivedResults);
                
				string[] paramValues;
				paramValues = StringUtils.Split((context as IfyWebContext).GetParamValue(DataSetParameter.Name + ":_identifier"), DataSetParameter.Separator);
				if (paramValues.Length == ReceivedResults) for (int i = 0; i < ReceivedResults; i++) DataSetIdentifiers[i] = paramValues[i];
                paramValues = StringUtils.Split((context as IfyWebContext).GetParamValue(DataSetParameter.Name + ":_size"), DataSetParameter.Separator);
				if (paramValues.Length == ReceivedResults) for (int i = 0; i < ReceivedResults; i++) Int64.TryParse(paramValues[i], out DataSetSizes[i]);
    
				/*for (int i = 0; i < dataSetTotalCount; i++) DataSets[i] = new DataSetInfo(this, i, DataSetParameter.Values[i], sizes[i], startTimes[i], endTimes[i]); */
                
				for (int i = 0; i < ReceivedResults; i++) {
					DataSets[i] = new CatalogueDataSetInfo(this, i);
					DataSetResources[i] = DataSetParameter.Values[i];
				}
                
				foreach (KeyValuePair<string, string> kvp in MetadataElementNames) {
					metadataValues[kvp.Key] = new TypedValue[ReceivedResults];
                    paramValues = StringUtils.Split((context as IfyWebContext).GetParamValue(DataSetParameter.Name + ":" + kvp.Key), DataSetParameter.Separator);
					if (paramValues.Length == ReceivedResults) for (int i = 0; i < ReceivedResults; i++) SetMetadataValue(i, kvp.Key, paramValues[i]);
				}
                
				/*string paramValue = context.GetParamValue(paramName);
                datasetParam
                string[] paramValues = StringUtils.Split(paramValue, ';');
                DataSets = new DataSetInfo[];
                int[] fileSizes;
                DateTime[] startDates;
                DateTime[] endDates;*/ 
			}
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>.</summary>
		protected bool AddResultsFromRdf(string url, int startIndex, int count) {
			/*url = Regex.Replace(url, @"([^\&\?]+)=\{startIndex\??\}", "$1=" + startIndex);
            url = Regex.Replace(url, @"([^\&\?]+)=\{count\??\}", "$1=" + count);*/
			url = Regex.Replace(url, @"([\?&])startIndex=", (startIndex == -1 ? "" : "$1startIndex=" + startIndex));
			url = Regex.Replace(url, @"([\?&])count=", "$1count=" + count);
			context.AddDebug(3, "Catalogue query (page): " + url);

			try {
				XmlDocument doc = new XmlDocument();
				doc.Load(url);
                
				if (namespaceManager == null) {
					namespaceManager = new XmlNamespaceManager(doc.NameTable);
					foreach (XmlAttribute attr in doc.DocumentElement.Attributes) if (attr.Prefix == "xmlns") namespaceManager.AddNamespace(attr.LocalName, attr.Value);
				}
                
				// In the first request, retrieve meta information on results
				if (startIndex == 0) {
                    
					// Extract the number of results from the retreived XML document and add it to the local totalResults
					XmlNode node = doc.SelectSingleNode("//rdf:Description/os:totalResults", namespaceManager);
					int tr = 0;
					if (node != null) Int32.TryParse(node.InnerText, out tr);
					TotalResults = tr;

					// Extract the first start index (i.e. the index offset)
					node = doc.SelectSingleNode("//rdf:Description/os:startIndex", namespaceManager);
					startIndex = 0;
					if (node != null) Int32.TryParse(node.InnerText, out startIndex);

					// Extract the actual number of items per page, in case it does not correspond to the requested count value
					node = doc.SelectSingleNode("//rdf:Description/os:itemsPerPage", namespaceManager);
					//if (node != null) Int32.TryParse(node.InnerText, out dataSetsPerRequest);
                    
					// Set length of dataSets array and of metadata arrays
                    
					count = (TotalResults <= maxCount ? TotalResults : maxCount);
					InitializeData(count);
    
					ReceivedResults = 0;
				}
                
				if (count == 0 || TotalResults < minCount) return true; // continue only if results are requested
                
				XmlNodeList nodes = doc.SelectNodes(DataSetXmlName, namespaceManager);
				for (int i = 0; i < nodes.Count; i++) {
					DataSets[ReceivedResults] = new CatalogueDataSetInfo(this, ReceivedResults);
					GetDataSetInformation(ReceivedResults, nodes[i] as XmlElement);
					ReceivedResults++;
				}
				context.AddDebug(3, "Results: " + ReceivedResults);

			} catch (Exception e) {
				context.AddError("Catalogue query failed: " + e.Message + " " + url);
				return false;
			}
            
			return true;
		}
		//---------------------------------------------------------------------------------------------------------------------
		private void InitializeData(int count) {
			DataSets = new CatalogueDataSetInfo[count];
			DataSetIdentifiers = new string[count];
			DataSetResources = new string[count];
			DataSetSizes = new long[count];
			foreach (KeyValuePair<string, string> kvp in MetadataElementNames) metadataValues[kvp.Key] = new TypedValue[count];
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>.</summary>
		protected void GetDataSetInformation(int index, XmlElement element) {
			// Check for common metadata (resource, identifier and size) and for specific metadata in child elements
			string value;
			XmlNode node;
			/*DataSetResources[index] = "RESOURCE_" + index;
            DataSetIdentifiers[index] = "IDENTIFIER_" + index;
            DataSetSizes[index] = 1000 + index;
            foreach (KeyValuePair<string, string> kvp in MetadataElementNames) {
                metadataValues[kvp.Key][index] = new TypedValue("XXX");
            }*/
			if ((node = element.SelectSingleNode(ResourceXmlName, namespaceManager)) != null) DataSetResources[index] = node.InnerXml;
			if ((node = element.SelectSingleNode(IdentifierXmlName, namespaceManager)) != null) DataSetIdentifiers[index] = node.InnerXml;
			if ((node = element.SelectSingleNode(SizeXmlName, namespaceManager)) != null) Int64.TryParse(node.InnerXml, out DataSetSizes[index]);
                
			foreach (KeyValuePair<string, string> kvp in MetadataElementNames) {
				value = String.Empty; 
				foreach (XmlNode node2 in element.SelectNodes(kvp.Value, namespaceManager)) value += (value == String.Empty ? String.Empty : " ") + node2.InnerXml;
				metadataValues[kvp.Key][index] = new TypedValue(value);
			}
		}
		//---------------------------------------------------------------------------------------------------------------------
		public void AddMetadataField(string fieldName, string elementName) {
			if (MetadataElementNames.ContainsKey(fieldName)) return;
			MetadataElementNames.Add(fieldName, elementName);
			metadataValues.Add(fieldName, null);
		}
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>.</summary>
		public TypedValue GetMetadataValue(int index, string name) {
			TypedValue result = null; 
			if (metadataValues.ContainsKey(name) && index >= 0 && index < ReceivedResults) result = metadataValues[name][index];
			if (result == null) result = TypedValue.Empty;
			return result;
		}
		//---------------------------------------------------------------------------------------------------------------------
		/*        /// <summary>.</summary>
        protected void SetMetadataValueForElement(int index, string elementName, string value) {
            if (index >= ReceivedResults) return;
            foreach (KeyValuePair<string, string> kvp in MetadataElementNames) {
                if (kvp.Value == elementName) metadataValues[kvp.Key][index] = new TypedValue(value);
            }
        }*/
		//---------------------------------------------------------------------------------------------------------------------
		/// <summary>.</summary>
		protected void SetMetadataValue(int index, string name, string value) {
			if (!metadataValues.ContainsKey(name) || index >= ReceivedResults) return;
			metadataValues[name][index] = new TypedValue(value);
		}
	}
}

