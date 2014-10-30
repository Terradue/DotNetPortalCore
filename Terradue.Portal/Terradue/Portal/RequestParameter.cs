using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
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

    

    /// <summary>Represents a collection of request parameters.</summary>
    public class RequestParameterCollection {

        private Dictionary<string, RequestParameter> dict = new Dictionary<string, RequestParameter>();
        private RequestParameter[] items = new RequestParameter[0];

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of request parameters in the collection.</summary>
        public int Count {
            get { return items.Length; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the request parameter with the specified name.</summary>
        public RequestParameter this[string name] { 
            get { if (dict.ContainsKey(name)) return dict[name]; else return RequestParameter.Empty; } 
            set { if (dict.ContainsKey(name)) dict[name] = value; } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the request parameter at the specified position in the list.</summary>
        public RequestParameter this[int index] { 
            get { return items[index]; } 
            set { items[index] = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the request parameter collection contains a request parameter with the specified name.</summary>
        /*!
        /// <param name="name">the request parameter name</param>
        /// <returns>true if the request parameter collection contains such a request parameter</returns>
        */
        public bool Contains(string name) {
            return dict.ContainsKey(name);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets a RequestParameter instance based on an XML element from a service definition file.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="derivate">the service derivate for which the request parameter is validated</param>
        /// <param name="element">the XML element <i>&lt;param&gt;</i> containing the parameter definition</param>
        /// <returns>derivate if the request parameter collection contains such a request parameter</returns>
        */
        public RequestParameter GetParameter(IfyContext context, Service service, XmlElement element, bool defining, bool assignValueSets) {
            if (element == null) return null;
            
            string name = (element.HasAttribute("name") ? element.Attributes["name"].Value : null);
            if (name == null && element.HasAttribute("source")) name = element.Attributes["source"].Value;
            if (name == null && element.Name == "param") return null;
            
            RequestParameter result;
            
            if (dict.ContainsKey(name)) {
                result = dict[name];
                bool after = false;
                for (int i = 0; i < items.Length - 1; i++) {
                    if (!after) after = (items[i].Name == name);
                    if (after) items[i] = items[i + 1];
                }
                items[items.Length - 1] = result;
            
            } else {
                result = new RequestParameter(context, service, name);
                Add(result);
            }

            result.GetXmlInformation(element, defining, assignValueSets, true);
                
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets a RequestParameter instance based on an XML element from a source other than a service definition file.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="derivate">the service derivate for which the request parameter is validated</param>
        /// <param name="element">the XML element <i>&lt;param&gt;</i> containing the parameter definition</param>
        /// <returns>derivate if the request parameter collection contains such a request parameter</returns>
        */
        public RequestParameter GetParameter(IfyContext context, Service service, string name, string type, string caption, string value) {
            if (name == null) return null;
            
            RequestParameter result;
            
            if (dict.ContainsKey(name)) {
                result = dict[name];
                result.Value = value;
            
            } else {
                result = new RequestParameter(context, service, name, type, caption, value);
                Add(result); 
            }

            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a new request parameter to the request parameter collection.</summary>
        /*!
        /// <param name="param">the request parameter to be added</param>
        */
        public void Add(RequestParameter param) {
            if (param == null || dict.ContainsKey(param.Name)) return;
            dict.Add(param.Name, param);
            Array.Resize(ref items, items.Length + 1);
            items[items.Length - 1] = param;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets an URL for an OpenSearch request based on an URL template.</summary>
        /*!
            This is mainly used for the catalogue request for the task input files.
            The OpenSearch URL parameters with a extension name (between curly brackets in the OpenSearch URL template) 
            <ul>
                <li>If the request parameter collection contains a request parameter with the same extension name, the parameter is included in the returned URL with its value.</li>
                <li>If the request parameter collection does not contain such a request parameter, the OpenSearch parameter is not included in the returned URL.</li>
            </ul>
        /// <param name="urlTemplate">the OpenSearch URL template containing the OpenSearch parameters</param>
            \returns the URL containing the OpenSearch parameters matching with the request parameters in the collection and their values
        */
        public string GetRequestUrlForTemplate(string urlTemplate) {
            Match match = Regex.Match(urlTemplate, "([^\\?]+)\\?(.*)");
            if (!match.Success) return null;

            string script = match.Groups[1].Value;
            string[] parameters = match.Groups[2].Value.Split('&');
            string queryString = "";

            // For each query string parameter, add the value of the matching parameter of the current request to the forward request URL
            // ("current request" means the request we are currently serving, "forward request" means the request we have to make to the next CAS)
            // The parameters match if they have the same extension name, i.e. the name in the curly brackets in the template,
            // e.g. "time:start" in "startdate={time:start?}" 
            for (int i = 0; i < parameters.Length; i++) {
                match = Regex.Match(parameters[i], @"^([^=]+)={([^?]+)\??}$");
                
                // Keep parameters that are no OpenSearch extensions
                if (!match.Success) {
                    queryString += (queryString == "" ? "" : "&") + parameters[i];
                    continue;
                }
                
                string name = match.Groups[1].Value;
                string extension = match.Groups[2].Value;
                
                // Add the parameter only if it is defined for the current request and if it is provided in the current request's URL
                bool found = false;
                for (int j = 0; j < items.Length; j++) {
                    if (items[j].SearchExtension == extension) {
                        string value = items[j].Value;
                        //if (value == null) continue;
                        queryString += (queryString == "" ? "" : "&") + name + "=" + value;
                        found = true;
                    }
                }
                
                if (!found) queryString += (queryString == "" ? "" : "&") + name + "=";
            }
            
            // Return the complete forward request URL
            return script + "?" + queryString;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Analyzes the request parameters in the collection and detect ownerships between them. </summary>
        /*!
            Request parameters may depend on each other, for example a task parameter for the results of a catalogue query depends on the parameter for the series used in the query.  
        */
        public void ResolveOwnerships() {
            for (int i = 0; i < items.Length; i++) {
                if (items[i].OwnerName == null) continue;
                if (dict.ContainsKey(items[i].OwnerName)) items[i].Owner = dict[items[i].OwnerName]; 
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Removes all request parameters from the collection.</summary>
        public void Clear() {
            Array.Resize(ref items, 0);
            dict.Clear();
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    /// <summary>Represents a request parameter</summary>
    /*!
        Request parameters can be instantiated anywhere, but their usually they are instantiated by the method Service::CheckParameters that parses a service's definition file <i>service.xml</i>.
        The service definition files contain XML elements <i>&lt;param&gt;</i>, each of which is translated into a RequestParameter object.
        
        The following table shows the XML attributes in the <i>&lt;param&gt;</i> and their translation into RequestParameter properties:
        
        <table border="1" cellpadding="4" style="border-collapse:collapse; border:2px solid #c0c0c0; empty-cells:show">
            <tr>
                <th>Attribute</th>
                <th>Optional</th>
                <th>Explanation</th>
                <th>Property in RequestParameter</th>
            </tr>
            <tr>
                <th>name</th>
                <td>No</td>
                <td>Sets the name of the request parameter. The name must be unique and identifies the parameter in requests from the client.</td>
                <td>#Name</td>
            </tr>
            <tr>
                <th>type</th>
                <td>Yes</td>
                <td>If no value is provided, the value depends on the value of <b>source</b> (if set) or is set to <i>string</i>.</td>
                <td>#Type</td>
            </tr>
            <tr>
                <th>source</th>
                <td>Yes</td>
                <td>Sets the source of the request parameter.</td>
                <td>#Source</td>
            </tr>
            <tr>
                <th>ext</th>
                <td>Yes</td>
                <td/>
                <td>#SearchExtension</td>
            </tr>
            <tr>
                <th>optional</th>
                <td>Yes</td>
                <td/>
                <td>#Optional</td>
            </tr>
            <tr>
                <th>scope</th>
                <td>Yes</td>
                <td/>
                <td>#Scope</td>
            </tr>
            <tr>
                <th>range</th>
                <td>Yes</td>
                <td/>
                <td>#Range</td>
            </tr>
        </table>
        
        
    */
    public class RequestParameter : TypedValue {
        private IfyContext context;
        private IfyWebContext webContext;
        private Service service;
        private string value;
        private bool allowEmpty;
        private bool configurable;
        private bool[] valid = new bool[0];
        private bool allValid = false;
        private bool validityUpdated = false;
        private string selectedCaption;
        private OperationType operationType;
        private string nullCaption;
        private Dictionary<string, string> additionalAttributes;
        
        public static new RequestParameter Empty = new RequestParameter(null as IfyContext, null as Service, null);
        
        //---------------------------------------------------------------------------------------------------------------------

        public Service Service {
            get { return service; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool IsConstant {
            get; protected set;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool IsRow {
            get; set;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the request parameter is used to switch between different request parameter collections.</summary>
        public bool IsSwitch { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the request parameter.</summary>
        public string Name { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the request parameter.</summary>
        public override string Value { 
            get { return this.value; } 
            set { 
                if (value != null) this.value = value; // !!! why this condition?
                UpdateValues();
            } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the request parameter does not contain the actual value(s), but refers to a value or to values.</summary>
        public bool Reference { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the request parameter does not contain or refer to a single value, but contains or refers to multiple values.</summary>
        public bool Multiple { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the value set of the request parameter.</summary>
        /*!
            Request parameters with a source generate selection (single reference) lists and are translated into corresponding properties:
            <table border="1" cellpadding="4" style="border-collapse:collapse; border:2px solid #c0c0c0; empty-cells:show">
                <tr>
                    <th>Value of <i>source</i> attribute</th>
                    <th>Content of selection list</th>
                    <th>Property in service derivate instance</th>
                </tr>
                <tr>
                    <td><b>%Task.%ComputingResource</b>&nbsp;or<br/><b>computingResource</b></td>
                    <td>All computing resources that are compatible with the service and for which the requesting user has usage privileges</td>
                    <td>ServiceDerivate::ComputingResource</td>
                </tr>
                <tr>
                    <td><b>%Task.%PublishServer</b>&nbsp;or<br/><b>publishServer</b></td>
                    <td>All publish servers owned by the requesting user or shared among all users</td>
                    <td>ServiceDerivate::PublishServer</td>
                </tr>
                <tr>
                    <td><b>%Task.%Priority</b>&nbsp;or<br/><b>priority</b></td>
                    <td>Priority values up to the maximum value that applies for the requesting user</td>
                    <td>ServiceDerivate::Priority</td>
                </tr>
                <tr>
                    <td><b>%Task.%Compression</b>&nbsp;or<br/><b>compress</b></td>
                    <td>Result compression values defined in the service definition file</td>
                    <td>ServiceDerivate::Compression</td>
                </tr>
                <tr>
                    <td><b>%Task.%Series</b>&nbsp;or<br/><b>series</b></td>
                    <td>All series that are compatible with the service and for which the requesting user has usage privileges</td>
                    <td>N/A<br/>(no corresponding attribute)</td>
                </tr>
                <tr>
                    <td><b>%Task.%InputFiles</b> or<br/><b>dataset</b></td>
                    <td>N/A<br/>No selection list is generated.
                        <ul>
                            <li>In the interactive mode (GUI), the client performs a catalogue query and sends the list of file identifiers to the server.</li>
                            <li>In the direct task creation mode the values are received through a request to the catalogue.</li>
                        </ul>
                    </td>
                    <td>Task::InputFiles<br/>(is not a task attribute)</td>
                </tr>
            </table>
        */
        public string Source { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the OpenSearch extension of the request parameter.</summary>
        public string SearchExtension { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets or sets the name of the owner of the request parameter.</summary>
        public string OwnerName { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the owner of the request parameter (another request parameter).</summary>
        public RequestParameter Owner { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the request parameter is optional.</summary>
        public bool Optional { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the request parameter is presented as possible parameter to external applications and stored.</summary>
        public bool Ignore { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the request parameter is is read-only.</summary>
        public bool ReadOnly { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the request parameter is optional.</summary>
        public bool AllowEmpty {
            get { return (Optional || allowEmpty); }   // derivate != null && (derivate.Defining || derivate.OptionalSearchParameters && SearchExtension != null)
            set { allowEmpty = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the scope of the request parameter.</summary>
        /*!
            In service definition file <i>service.xml</i>:
            
            If the scope is set, a selection list is generated containing values for the requesting user and scope.
            
            \sa ValueSelectScope
        */
        public ValueSelectScope Scope { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the value of the request parameter.</summary>
        /*!
            To be used together with #Scope.
            
            \sa ValueSelectRange
        */
        public ValueSelectRange Range { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the value list is withheld and sent to the client only on request. </summary>
        public bool Withhold { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the character that separates multiple values or references.</summary>
        public char Separator { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        // / <summary>Gets or sets the usage level of the request parameter.</summary>
        /* !
            A request parameter may be 
            <ul>
                <li>translated into a service derivate attribute (e.g. computing resource),</li>
                <li>translated into a service derivate parameter (service-specific parameters), or</li>
                <li>not taken into account if unset and optional.</li>
            <ul>
            
            \sa RequestParameterLevel
        */
        // public RequestParameterLevel Level { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets an alternative value set for the request parameter. </summary>
        public IValueSet ValueSet { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the caption of the selected value if the parameter uses a value set.</summary>
        public string SelectedCaption {
            get { return selectedCaption; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets an alternative value set for the request parameter. </summary>
        public ServiceParameterValueSet SpecificValueSet { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets a message connected to the request parameter. </summary>
        public string Message { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        // ! Gets or sets the validity state of the request parameter. 
        //public RequestParameterState State { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the array of logical values. </summary>
        public string[] Values { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the array of validity flags relative to the logical values. </summary>
        public bool[] Valid {
            get { return valid; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets a flag representing the overall validity of the request parameter. </summary>
        public bool AllValid {
            get {
                if (ReadOnly) allValid = true;
                else if (!validityUpdated) Check(null, false);
                return allValid; 
            }
            set { // !!! AllValid should not have a setter
                allValid = value;
                validityUpdated = true;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the caption for the empty value of the request parameter if it is optional. </summary>
        public string NullCaption { 
            get { return (nullCaption == null ? "[no selection]" : nullCaption); }
            set { nullCaption = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a RequestParameter instance with a name.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="service">the service that defines the request parameter</param>
        /// <param name="name">the name of the request parameter</param>
        */
        public RequestParameter(IfyContext context, Service service, string name) {
            this.context = context;
            this.webContext = context as IfyWebContext;
            this.service = service;
            this.Name = name;
            Initialize();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a RequestParameter instance with.</summary>
        /*!
            This constructor is used to define additional request parameters that beyond those from the service definition file.
        /// <param name="context">The execution environment context.</param>
        /// <param name="service">the service that defines the request parameter</param>
        /// <param name="name">the name of the request parameter</param>
        /// <param name="type">the type identifier of the request parameter</param>
        /// <param name="caption">the caption of the request parameter</param>
        /// <param name="value">the initial value of the request parameter</param>
        */
        public RequestParameter(IfyContext context, Service service, string name, string type, string caption, string value) {
            this.context = context;
            this.webContext = context as IfyWebContext;
            this.service = service;
            this.Name = name;
            this.Type = type;
            this.Caption = caption;
            Initialize();
            this.Value = value;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        private void Initialize() {
            IsRow = false;
            //Level = RequestParameterLevel.Unused;
            Separator = ',';
            Values = new string[0];
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the request parameter information from the XML element <i>&lt;param&gt;</i>.</summary>
        /*!
        /// <param name="element">the XML element <i>&lt;param&gt;</i> defining the request parameter</param>
        */
        public bool GetXmlInformation(XmlElement element, bool defining, bool assignValueSets, bool setConstant) {
            if (element == null) return false;
            if (element.Name == "const") {
            } else {
                if (!element.HasAttribute("name")) return false;
            }
            
            string scopeStr = null, rangeStr = null, listNames = null, filePattern = null; 
            
            foreach (XmlAttribute attribute in element.Attributes) {
                switch (attribute.Name) {
                    case "switch" :
                        IsSwitch = attribute.Value == "true";
                        break;
                    case "type" :
                        Type = attribute.Value;
                        break;
                    case "caption" :
                        Caption = attribute.Value;
                        break;
                    case "owner" :
                        OwnerName = attribute.Value;
                        break;
                    case "source" :
                        Source = attribute.Value;
                        break;
                    case "ext" :
                        SearchExtension = attribute.Value;
                        break;
                    case "optional" :
                        Optional |= attribute.Value == "true";
                        break;
                    case "list" :
                        listNames = attribute.Value;
                        break;
                    case "scope" :
                        scopeStr = attribute.Value;
                        break;
                    case "range" :
                        rangeStr = attribute.Value;
                        break;
                    case "pattern" :
                        filePattern = attribute.Value;
                        break;
                    case "withhold" :
                        Withhold = attribute.Value == "true";
                        break;
                    case "empty" :
                        nullCaption = attribute.Value;
                        Optional = true;
                        break;
                    case "ignore" :
                        if (attribute.Value == "true") Ignore = true;
                        Optional |= attribute.Value == "true";
                        break;
                    case "name" :
                    case "value" :
                    case "default" :
                        break;
                    default :
                        AddAdditionalAttribute(attribute.Name, attribute.Value);
                        break;
                }
            }
            
            if (Caption == null) Caption = Name;
            UpdateType();
            
            configurable = (scopeStr != null);
            if (configurable) {
                Reference = true;
                Scope = ScopeFromString(scopeStr);
                Range = RangeFromString(rangeStr);
            } else if (filePattern != null) {
                Reference = true;
            }

            if (Multiple && element != null && element.HasAttribute("separator") && element.Attributes["separator"].Value.Length == 1) Separator = element.Attributes["separator"].Value[0];
            
            if (Value == null) {
                if (defining && element.HasAttribute("value")) Value = element.Attributes["value"].Value;
                if (element.HasAttribute("default")) Value = element.Attributes["default"].Value;
            }
            
            Reference |= (Type == "series"); // !!! Change this!!

            if (Reference || Multiple) {
                string separatorStr = Separator.ToString();
                if (Reference) {
                    if (assignValueSets && Source == null) {
                        if (configurable) {
                            if (listNames != null) {
                                ValueSet = new StoredLookupParameterValueSet(
                                        context,
                                        service,
                                        Name,
                                        Scope,
                                        listNames
                                );
                            } else if (Type == "textfile") {
                                ValueSet = new StoredFileParameterValueSet(
                                        context,
                                        service,
                                        Name,
                                        Scope,
                                        element.HasAttribute("pattern") ? element.Attributes["pattern"].Value : null,
                                        element.HasAttribute("userPattern") ? element.Attributes["userPattern"].Value : null
                                );
                
                            } else {
                                ValueSet = new StoredParameterValueSet(
                                        context,
                                        service,
                                        Name,
                                        Scope
                                );
                            }
                        } else if (listNames != null) {
                            ValueSet = new LookupValueSet(
                                    context,
                                    listNames,
                                    false
                            );
                        } else if (filePattern != null) {
                            ValueSet = new FileListParameterValueSet(
                                    context,
                                    service,
                                    Name,
                                    filePattern
                            );
                            Type = "select";
                            UpdateType();

                        } else if (Type == "series") {
                            Series result = Series.GetInstance(context);
                            result.UserId = service.UserId;
                            // TODO: re-enable!!!
                            //result.DefaultIdentifier = element.HasAttribute("default") ? element.Attributes["default"].Value : null;
                            //result.RegexPattern = element.HasAttribute("regex") ? element.Attributes["regex"].Value : null;
                            //ValueSet = result;
                            Type = "select";
                            UpdateType();
            
                        }
                    }
                    
                    if (assignValueSets && element.HasChildNodes) SpecificValueSet = new ServiceParameterValueSet(element);
                    
                } else {
                    bool optionSelected = false;
                    XmlNodeList valueNodes = element.SelectNodes("element[@value]");
                    foreach (XmlNode node in valueNodes) {
                        XmlElement elem = node as XmlElement;
                        if (!optionSelected) value = String.Empty; else value += separatorStr; 
                        value += elem.Attributes["value"].Value.Replace(separatorStr, @"\,");
                        optionSelected = true;
                    }
                }

            } else if (defining && element.ChildNodes.Count != 0 && element.ChildNodes[0] is XmlText) {
                Values = new string[]{(element.ChildNodes[0] as XmlText).Value};
            }
            
            if (setConstant && element.Name == "const") IsConstant = true;

            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public void AddAdditionalAttribute(string name, string value) {
            if (additionalAttributes == null) additionalAttributes = new Dictionary<string, string>();
            if (!additionalAttributes.ContainsKey(name)) additionalAttributes.Add(name, value); // !!! THIS IS CALLED TWICE IN CASE OF A TASK OF A SCHEDULER
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Unsets the value.</summary>
        public void Unset() {
            value = null;
            UpdateValues();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Determines the type identifier, if not yet set, from the reference and cardinality properties.</summary>
        public void SetType() {
            if (Type != null) return;
        
            if (Reference && Multiple) Type = "multiple";
            else if (Multiple) Type = "values";
            else if (Reference) Type = "select";
            else Type = "string";
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Determines the reference and cardinality properties from the type identifier.</summary>
        protected void UpdateType() {
            Reference = (Type == "select" || Type == "multiple");
            Multiple = (Type == "multiple" || Type == "values");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool GetValues(ValueUpdateMethod method) {
            bool changed = false;

            if (method != ValueUpdateMethod.NoReplace && !ReadOnly) {
                string newValue = webContext.GetParamValue(Name);
                
                if ((newValue != null || method == ValueUpdateMethod.ForceReplace) && newValue != value) {
                    value = newValue;
                    changed = true;
                }
            }
            
            UpdateValues();
            
            return changed;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Performs the validity checks based on the raw value.</summary>
        protected void UpdateValues() {
            // (1) If the parameter value is not provided in the request (GET or POST), use the default value if defined in the service.xml;
            // (2) otherwise if the parameter value is defined as configurable in the service.xml, use the user-specific value from the database (table serviceconfig); 
            // (3) otherwise use the parameter value(s) from the request
            
            string separatorStr = Separator.ToString();

            if (value == null) {
                Values = new string[0];
                valid = new bool[0];
            } else { // if there is a value, assign it to the values array (split if multiple values) (4) 
                if (Multiple) {
                    Values = value.Split(Separator);
                    for (int i = 0; i < Values.Length; i++) Values[i] = Values[i].Replace(@"\,", separatorStr);
                } else {
                    Values = new string[]{value};
                    valid = new bool[]{false};
                }
            }
            
            Message = null; 
            validityUpdated = false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void Check() {
            Check(null, false);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void Check(XmlWriter output, bool writeContainer) {
            if (Values == null) Values = new string[0];
            valid = new bool[Values.Length];
            
            if (output != null && writeContainer) {
                output.WriteStartElement("field");
                WriteFieldAttributes(output);
            }

            if (ValueSet != null || SpecificValueSet != null) {
                if (Multiple) {
                    bool[] validSingle;
                    string[] defaultValues = new string[0];
                    if (SpecificValueSet != null) {
                        validSingle = SpecificValueSet.CheckValues(Values, out defaultValues);
                        for (int i = 0; i < valid.Length; i++) valid[i] |= validSingle[i];
                        if (!Optional && Values.Length == 0) {
                            if (defaultValues != null && defaultValues.Length != 0) {
                                Values = defaultValues;
                                valid = new bool[Values.Length];
                                for (int i = 0; i < Values.Length; i++) valid[i] = true;
                            }
                        }
                    }
                    if (ValueSet != null) {
                        validSingle = ValueSet.CheckValues(Values, out defaultValues);
                        for (int i = 0; i < valid.Length; i++) valid[i] |= validSingle[i];
                        if (!Optional && Values.Length == 0) {
                            if (defaultValues != null && defaultValues.Length != 0) {
                                Values = defaultValues;
                                valid = new bool[Values.Length];
                                for (int i = 0; i < Values.Length; i++) valid[i] = true;
                            }
                        }
                    }
                } else {
                    string defaultValue = "";
                    string tempSelectedCaption = "";
                    if (Optional && output != null) {
                        output.WriteStartElement("element");
                        output.WriteAttributeString("value", String.Empty);
                        output.WriteString(NullCaption == null ? "[no selection]" : NullCaption);
                        output.WriteEndElement();
                    }
                    selectedCaption = null;
                    if (SpecificValueSet != null) {
                        bool tempValid;
                        tempValid = Range != ValueSelectRange.Restricted || SpecificValueSet.CheckValue(value, out defaultValue, out tempSelectedCaption);
                        //context.AddDebug(3, "tsc1(" + Name + ") = " + tempSelectedCaption);
                        if (Values.Length != 0) {
                            valid[0] |= tempValid;
                            if (selectedCaption == null) selectedCaption = tempSelectedCaption;
                        } else if (!Optional && defaultValue != null) {
                            Values = new string[]{defaultValue}; // !!! assign consolidated default values after second check (ValueSet)
                            valid = new bool[]{true};
                        }
                    }
                    if (ValueSet != null) {
                        bool tempValid;
                        tempValid = Range != ValueSelectRange.Restricted || ValueSet.CheckValue(value, out defaultValue, out tempSelectedCaption);
                        //context.AddDebug(3, "tsc2(" + Name + ") = " + tempSelectedCaption);
                        if (Values.Length != 0) {
                            valid[0] |= tempValid;
                            if (selectedCaption == null) selectedCaption = tempSelectedCaption;
                        } else if (!Optional && defaultValue != null) {
                            Values = new string[]{defaultValue}; // !!! assign consolidated default values after this check
                            valid = new bool[]{true};
                        }
                    }
                }
                
            } else if (Multiple) {
                for (int i = 0; i < Values.Length; i++) valid[i] = true;

            } else if (Values.Length != 0) {
                switch (Type) {
                    case "bool" :
                        Values[0] = Values[0].ToLower();
                        valid[0] = (Values[0] == "true" || Values[0] == "false" || Values[0] == "on" || Values[0] == "yes" || Values[0] == "no");
                        //if (values[0] == "on") values[0] = "true";
                        if (!valid[0]) Message = "Value must be \"true\" or \"false\"";
                        break;
                    case "int" :
                        int i;
                        valid[0] = Int32.TryParse(Values[0], out i);
                        if (!valid[0]) Message = "Value must be an integer number";
                        break;
                    case "float" :
                        double d;
                        valid[0] = Double.TryParse(Values[0], out d);
                        if (!valid[0]) Message = "Value must be a real number";
                        break;
                    case "date" :
                    case "datetime" :
                    case "startdate" :
                    case "enddate" :
                        DateTime dt;
                        valid[0] = DateTime.TryParse(Values[0], out dt);
                        if (valid[0]) Values[0] = dt.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss"); 
                        else Message = "Date value must be in the format " + (Type == "date" ? "\"YYYY-MM-DD\"" : "\"YYYY-MM-DDThh:mm:ss\"");
                        break;
                    default :
                        valid[0] = true;
                        break;
                }
            }

            if (Values.Length == 0 && !AllowEmpty && Type != "bool" && Source != "dataset") {
                allValid = false;
                Message = "Value cannot be empty";
            } else {
                allValid = true;
                for (int i = 0; i < valid.Length; i++) {
                    if (!valid[i]) {
                        allValid = false;
                        if (Reference && Message == null) Message = "Value is not in list";
                        break;
                    }
                }
            }
            validityUpdated = true;
            
            if (output != null && writeContainer) output.WriteEndElement(); // </field>
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void WriteField(XmlWriter output) {
            if (output == null) return;
            output.WriteStartElement("field");
            WriteFieldAttributes(output);
            output.WriteEndElement();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the attributes of the <i>&lt;field&gt;</i> element for the request parameter.</summary>
        public void WriteFieldAttributes(XmlWriter output) {
            if (output == null || output.WriteState != WriteState.Element) return;

            output.WriteAttributeString("name", Name); 
            if (IsSwitch) output.WriteAttributeString("switch", "true");
            output.WriteAttributeString("type", (Type == null ? "string" : Type));
            output.WriteAttributeString("caption", Caption);
            if (Owner != null) output.WriteAttributeString("owner", Owner.Name);
            if (Source != null) output.WriteAttributeString("source", Source);
            if (SearchExtension != null) output.WriteAttributeString("ext", SearchExtension);
            if (Optional) output.WriteAttributeString("optional", "true");
            if (ReadOnly) output.WriteAttributeString("readonly", "true");
            if (configurable) {
                output.WriteAttributeString("scope", ScopeToString(Scope));
                if (Range != ValueSelectRange.Restricted) output.WriteAttributeString("range", RangeToString(Range));
            }
            if (Withhold) output.WriteAttributeString("withhold", "true");
            if (additionalAttributes != null) {
                foreach (KeyValuePair<string, string> kvp in additionalAttributes) {
                    output.WriteAttributeString(kvp.Key, kvp.Value);
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the parameter state to <i>Wrong</i> together with an error message.</summary>
        /*!
        /// <param name="message">the error message </param>
        */
        public void Invalidate(string message) {
            allValid = false;
            validityUpdated = true;
            this.Message = message;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the parameter state to <i>Corrected</i> together with a corrction message.</summary>
        /*!
        /// <param name="newValue">the corrected value (if <i>null</i>, value is not changed) </param>
        /// <param name="message">the error message</param>
        */
        public void Correct(string newValue, string message, bool trust) {
            // Value = newValue; // !!! better!
            this.value = newValue;
            UpdateValues();                
            if (trust) {
                for (int i = 0; i < valid.Length; i++) valid[i] = true;
                allValid = true;
                validityUpdated = true;
            }
            this.Message = message;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Processes a request on a RequestParameter entity.</summary>
        /*!
            This is used for listing, viewing, creating, modifying and deleting request parameters that have configurable value lists, i.e. service parameters that have a scope (and optionally a range).
            
            The values of these parameter are stored either in the <b>database</b> or in <b>files</b> (if the parameter type is <i>textfile</i>).
            
            Each selectable value has a caption by which it is shown to the user. The caption is the filename if the values are stored in files.
        */
        public void ProcessRequest() {

            string valueCaption = webContext.GetParamValue("caption");
            string value = webContext.GetParamValue("value");
            
            switch (webContext.RequestedOperation) {
                case "create" :
                    operationType = OperationType.Create;
                    break;
                case "modify" :
                    operationType = OperationType.Modify;
                    break;
                case "delete" :
                    operationType = OperationType.Delete;
                    break;
                default :
                    operationType = (valueCaption == null ? OperationType.ViewList : OperationType.ViewItem);
                    break;
            }
            
            XmlElement element = (service as ScriptBasedService).GetParameterElement(Name);
            if (element == null || !GetXmlInformation(element, false, true, true)) context.ReturnError(new ArgumentException("Parameter not defined"), null);
            
            IEditableValueSet storedValueSet = ValueSet as IEditableValueSet;

            if (!configurable || storedValueSet == null) context.ReturnError(new ArgumentException("Service parameter does not maintain a value list"), null);
            
            if (Range < ValueSelectRange.Extensible && operationType != OperationType.ViewList && operationType != OperationType.ViewItem) context.ReturnError(new ArgumentException("Service parameter value list cannot be modified"), null);

            storedValueSet.SingleValueOutput = true;
        
            switch (operationType) {
                case OperationType.Create :
                    storedValueSet.CreateValue(valueCaption, value);
                    break;
                case OperationType.Modify :
                    storedValueSet.ModifyValue(valueCaption, value);
                    break;
                case OperationType.Delete :
                    storedValueSet.DeleteValue(valueCaption);
                    break;
            }
            // File output goes to text output, not XML !!!
            if (operationType == OperationType.Create || operationType == OperationType.Modify || operationType == OperationType.ViewItem) {
                StoredFileParameterValueSet storedFileValueSet = storedValueSet as StoredFileParameterValueSet;
                if (storedFileValueSet == null) storedValueSet.WriteValue(valueCaption, webContext.StartXmlResponse());
                else storedFileValueSet.WriteValue(valueCaption, webContext.StartTextResponse());
            } else {
                storedValueSet.WriteValues(webContext.StartXmlResponse());
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static ValueSelectScope ScopeFromString(string value) {
            switch (value) {
                case "user" : return ValueSelectScope.UserOnly;
                case "group" : return ValueSelectScope.UpToGroup;
                case "service" : return ValueSelectScope.UpToService;
                case "*" : return ValueSelectScope.All;
                default : return ValueSelectScope.UpToService;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static string ScopeToString(ValueSelectScope value) {
            switch (value) {
                case ValueSelectScope.UserOnly : return "user";
                case ValueSelectScope.UpToGroup : return "group";
                case ValueSelectScope.UpToService : return "service";
                case ValueSelectScope.All : return "*";
                default : return "service";
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static ValueSelectRange RangeFromString(string value) {
            switch (value) {
                case "open" : return ValueSelectRange.Open;
                case "extensible" : return ValueSelectRange.Extensible;
                default : return ValueSelectRange.Restricted;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static string RangeToString(ValueSelectRange value) {
            switch (value) {
                case ValueSelectRange.Open : return "open";
                case ValueSelectRange.Extensible : return "extensible";
                default : return null;
            }
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    // ! Validity states of request parameters.
    /*public enum RequestParameterState {
        
        /// <summary>Valid request parameters</summary>
        Ok,

        /// <summary>Corrected request parameters (that were considered as invalid but can be accepted as valid after an optional value correction)</summary>
        Corrected,

        /// <summary>Invalid request parameters</summary>
        Wrong
    }*/

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    /// <summary>Usage levels of request parameters that determine the parameter behaviour.</summary>
/*    public enum RequestParameterLevel {

        /// <summary>Request parameter has no value and is not stored in the database.</summary>
        Unused,

        // / <summary>Request parameter is a constant and is not stored in the database.</summary>
        //Constant,

        /// <summary>Request parameter is translated into a service derivate parameter (all service-specific task parameters).</summary>
        Custom,

        /// <summary>Request parameter is translated into a service derivate attribute (task attributes, e.g. "computing resource").</summary>
        System
    }*/

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    /// <summary>Possible scopes for the generation of request parameter value lists.</summary>
    public enum ValueSelectScope {
        
        /// <summary>Value list includes only the values defined for (or by) the current user.</summary>
        UserOnly,

        /// <summary>Value list includes the values defined for (or by) the current user and for his group(s).</summary>
        UpToGroup,

        /// <summary>Value list includes the values defined for (or by) the current user, for his group(s) and for all users of the service.</summary>
        UpToService,

        /// <summary>Value list includes the values defined for (or by) the current user, for his group(s), for all users of the service and for all services.</summary>
        All
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Possible selection ranges for request parameter value lists.</summary>
    public enum ValueSelectRange {
        
        /// <summary>Only values that are present in the value list can be selected.</summary>
        Restricted,
        
        /// <summary>Values can be selected from the list or entered manually.</summary>
        Open,
        
        /// <summary>The value list can be extended by adding new entries to it.</summary>
        Extensible
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Possible methods of replacement of a value with a new value</summary>
    public enum ValueUpdateMethod {
        
        /// <summary>The existing value is not replaced.</summary>
        NoReplace,
        
        /// <summary>The existing value is only replaced if the new value it is not <c>null</c>.</summary>
        NoNullReplace,
        
        /// <summary>The existing value is in any case replaced.</summary>
        ForceReplace
    
    }
        
        

    
}

