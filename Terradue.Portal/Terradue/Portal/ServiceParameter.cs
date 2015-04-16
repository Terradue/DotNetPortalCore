using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

    

    public class ServiceParameter : TypedValue {

        private IfyContext context;
        private Service service;
        private string value;
        private string[] values;
        private bool[] valid = new bool[0];
        private bool isValid = false;
        private bool validityUpdated = false;
        private string selectedCaption;
        private string nullCaption;
        private string additionalAttributes;

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceParameterSet ParameterSet { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static new ServiceParameter Empty = new ServiceParameter(null as ServiceParameterSet, null as XmlElement);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the service parameter.</summary>
        public string Name { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the service parameter does not contain the actual value(s), but refers to a value or to values.</summary>
        public bool Reference { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the lowest possible number of values that have to be provided for the parameter.</summary>
        /// <remarks>A value of <c>1</c> indicates that the parameter is required.</summary>
        public int MinOccurs { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the highest possible number of values that can be provided for the parameter.</summary>
        /// <remarks>A value greater than <c>1</c> indicates that the parameter is multiple; The special value of <c>0</c> indicates that the parameter can occur unlimitedly.</summary>
        public int MaxOccurs { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the service parameter is required.</summary>
        public bool IsRequired {
            get {
                return MinOccurs >= 1; 
            }
            set {
                if (value) {
                    if (MinOccurs < 1) MinOccurs = 1;
                } else {
                    MinOccurs = 0;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the service parameter does not contain or refer to a single value, but contains or refers to multiple values.</summary>
        public bool Multiple {
            get {
                return MaxOccurs == 0 || MaxOccurs > 1;
            }
            set {
                if (value) {
                    if (MaxOccurs <= 1) MaxOccurs = 0;
                } else {
                    MaxOccurs = 1;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /*public bool IsRow {
            get; set;
        }*/

        //---------------------------------------------------------------------------------------------------------------------

        /*protected bool HasDefaultValue {
            get; set;
        }*/

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the service parameter is used to switch between different service parameter collections.</summary>
        public bool IsSwitch { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the service parameter.</summary>
        public override string Value {
            get { return this.value; } 
            set {
                this.value = value;
                UpdateValues();
            } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the parameter is ignored during checks and output.</summary>
        public bool Ignore { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the value set of the service parameter.</summary>
        public string Source { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the OpenSearch extension of the service parameter.</summary>
        public string SearchExtension { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the service parameter is is read-only.</summary>
        public bool ReadOnly { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the service parameter allows configuring values for users and groups.</summary>
        public bool HasConfigurableValueSet { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the scope of the service parameter.</summary>
        /*!
            In service definition file <i>service.xml</i>:
            
            If the scope is set, a selection list is generated containing values for the requesting user and scope.
            
            \sa ValueSelectScope
        */
        public ValueSelectScope Scope { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the value of the service parameter.</summary>
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

        /// <summary>Gets or sets an alternative value set for the service parameter. </summary>
        public IValueSet ValueSet { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets an alternative value set for the service parameter. </summary>
        public ServiceParameterValueSet SpecificValueSet { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the caption of the selected value if the parameter uses a value set.</summary>
        public string SelectedCaption {
            get { return selectedCaption; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets a message connected to the service parameter. </summary>
        public string Message { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the array of logical values. </summary>
        public string[] Values {
            get { return values; }
            set { values = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the array of validity flags relative to the logical values. </summary>
        public bool[] Valid {
            get { return valid; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets a flag representing the overall validity of the service parameter. </summary>
        public bool IsValid {
            get {
                if (ReadOnly) isValid = true;
                else if (!validityUpdated) Check();
                return isValid;
            }
            set { // !!! AllValid should not have a setter
                isValid = value;
                validityUpdated = true;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the caption for the empty value of the service parameter if it is optional. </summary>
        public string NullCaption { 
            get { return (nullCaption == null ? "[no selection]" : nullCaption); }
            set { nullCaption = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceParameter(ServiceParameterSet parameterSet) {
            this.ParameterSet = parameterSet;
            if (parameterSet != null) {
                this.context = parameterSet.Context;
                this.service = parameterSet.Service;
            }
            this.MinOccurs = 1; // TODO: remove (in future service parameters are not required by default)
            this.MaxOccurs = 1;
            this.Separator = ',';
        }

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceParameter(ServiceParameterSet parameterSet, XmlElement element) : this(parameterSet) {
            if (element != null) Initialize(element);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /*public ServiceParameter(IfyContext context, ServiceParameterSet parameterSet, string name, string type, string caption, string value) : this(parameterSet, null as XmlElement) {
            this.Name = name;
            this.Type = type;
            this.Caption = caption;
            Initialize();
            this.Value = value;
        }*/

        //---------------------------------------------------------------------------------------------------------------------

        private void Initialize() {
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected virtual void Initialize(XmlElement element) {
            string scopeStr = null;
            string rangeStr = null;
            string listNames = null;
            string filePattern = null; 
            
            foreach (XmlAttribute attribute in element.Attributes) {
                switch (attribute.Name) {
                    case "name" :
                        Name = attribute.Value;
                        break;
                    case "switch" :
                        IsSwitch = attribute.Value == "true";
                        break;
                    case "type" :
                        Type = attribute.Value;
                        break;
                    case "caption" :
                        Caption = attribute.Value;
                        break;
                    case "value" :
                    case "default" :
                        break;
                    case "multiple" :
                        Multiple = attribute.Value == "true";
                        break;
                    case "ext" : // input
                        SearchExtension = attribute.Value;
                        break;
                    case "required" :
                        IsRequired = attribute.Value == "true";
                        break;
                    case "optional" :
                        IsRequired = attribute.Value != "true";
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
                    case "pattern" : // file
                        filePattern = attribute.Value;
                        break;
                    case "withhold" :
                        Withhold = attribute.Value == "true";
                        break;
                    case "empty" :
                        nullCaption = attribute.Value;
                        IsRequired = false;
                        break;
                    default :
                        if (additionalAttributes == null) additionalAttributes = String.Empty; else additionalAttributes += " ";
                        additionalAttributes += attribute.OuterXml; 
                        break;
                }
            }
            
            if (Caption == null) Caption = Name;
            UpdateType();
            
            if (Multiple && element != null && element.HasAttribute("separator") && element.Attributes["separator"].Value.Length == 1) Separator = element.Attributes["separator"].Value[0];
            
            // Assign value sets
            if (scopeStr != null) {
                HasConfigurableValueSet = true;
                Scope = ScopeFromString(scopeStr);
                Range = RangeFromString(rangeStr);
            } else if (filePattern != null) {
                Reference = true;
            }

            if (HasConfigurableValueSet) {
                if (listNames != null) {
                    ValueSet = new StoredLookupParameterValueSet(context, service, Name, Scope, listNames);
                } else if (Type == "textfile") {
                    ValueSet = new StoredFileParameterValueSet(context, service, Name, Scope, element.HasAttribute("pattern") ? element.Attributes["pattern"].Value : null, element.HasAttribute("userPattern") ? element.Attributes["userPattern"].Value : null);
                } else {
                    if (context.ConsoleDebug) Console.WriteLine("SCOPE = " + Scope);
                    ValueSet = new StoredParameterValueSet(context, service, Name, Scope);
                }
            } else if (listNames != null) {
                ValueSet = new LookupValueSet(context, listNames, false);
            } else if (filePattern != null) {
                ValueSet = new FileListParameterValueSet(context, service, Name, filePattern);
                Type = "select";
                UpdateType();
            }
            
            if (element.HasChildNodes) SpecificValueSet = new ServiceParameterValueSet(element);
                
            // Set the parameter default value(s)
            if (element.HasAttribute("default")) {
                Value = element.Attributes["default"].Value;
            } else if (element.HasAttribute("value")) {
                Value = element.Attributes["value"].Value;
            } else if (element.ChildNodes.Count != 0 && element.ChildNodes[0] is XmlText) {
                Value = element.ChildNodes[0].InnerText;
            } else if (Multiple) {
                string separatorStr = Separator.ToString();
                XmlNodeList valueNodes = element.SelectNodes("element[@value]");
                string defaultValues = null;
                foreach (XmlNode node in valueNodes) {
                    XmlElement elem = node as XmlElement;
                    if ((!elem.HasAttribute("default") || elem.Attributes["default"].Value != "true") && (!elem.HasAttribute("selected") || elem.Attributes["selected"].Value != "true")) continue;
                    if (defaultValues == null) defaultValues = String.Empty; else defaultValues += separatorStr; 
                    defaultValues += elem.Attributes["value"].Value.Replace(separatorStr, @"\" + separatorStr);
                }
                Value = defaultValues;

            }

            // Adjust date/time values (allow interval syntax for expressing dates relative to the current date)
            if (Value != null && (Type == "datetime" || Type == "date" || Type == "startdate" || Type == "enddate")) {
                DateTime dt = context.Now;
                if (StringUtils.CalculateDateTime(ref dt, Value, true)) Value = dt.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss");
            }
            // TODO if (element.Name == "const") IsConstant = true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Determines the reference and cardinality properties from the type identifier.</summary>
        protected void UpdateType() {
            Reference = (Type == "select" || Type == "multiple");
            Multiple = (Type == "multiple" || Type == "values");
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void AddValue(string value) {
            int len = (values == null ? 0 : values.Length);
            if (len != 0 && MaxOccurs != 0 && len >= MaxOccurs && ParameterSet.CollectValidityErrors) {
                throw new InvalidServiceParameterException(String.Format("More than {0} for parameter {1}", MaxOccurs == 1 ? "one value" : String.Format("{0} values", Values.Length), Name));
            }
            if (len == 0) {
                values = new string[1];
                this.value = value;
            } else {
                Array.Resize(ref values, len + 1);
                this.value = String.Format("{0}{2}{1}", this.value, value, Separator);
            }
            values[len] = value;
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
                    for (int i = 0; i < Values.Length; i++) Values[i] = Values[i].Replace(@"\" + separatorStr, separatorStr);
                } else {
                    Values = new string[]{value};
                    valid = new bool[]{false};
                }
            }
            
            Message = null; 
            validityUpdated = false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void Check() {
            
            if (context.ConsoleDebug) Console.WriteLine("CHECK PARAM {0}: VALUESET: {1}, SPECIFIC VALUESET: {2}, REFERENCE: {3}, VALUE = '{4}'", Name, ValueSet == null ? "NO" : ValueSet.GetType().Name, SpecificValueSet == null ? "NO" : SpecificValueSet.GetType().Name, Reference, Value);
//                param.AllowEmpty = Defining || OptionalSearchParameters && param.SearchExtension != null;
            
            //param.SetType();
            
            /*if (IsSwitch && ParameterSet.Service.DynamicDefinitionDocument != null) {
                //param.ValueSet = null;
                //param.Reference = false;
                Type = "hidden";
            }*/ // TODO-NEW-SERVICE

            if (Values == null) Values = new string[0];
            bool[] valid = new bool[Values.Length];
            
            if (Reference) {//(ValueSet != null || SpecificValueSet != null) {
                int countSingle = 0;
                if (Multiple) {
                    string separatorStr = Separator.ToString();
                    bool[] validSingle;
                    string[] defaultValues = new string[0];
                    if (SpecificValueSet != null) {
                        validSingle = SpecificValueSet.CheckValues(Values, out defaultValues);
                        for (int i = 0; i < valid.Length; i++) valid[i] |= validSingle[i];
                        if (IsRequired && Values.Length == 0) {
                            if (defaultValues != null && defaultValues.Length != 0) {
                                Values = defaultValues;
                                valid = new bool[Values.Length];
                                this.value = null;
                                for (int i = 0; i < Values.Length; i++) {
                                    valid[i] = true;
                                    if (i == 0) this.value = String.Empty; else this.value += separatorStr;
                                    this.value += (Values[i] == null ? String.Empty : Values[i].Replace(separatorStr, @"\" + separatorStr));
                                }
                            }
                        }
                    }
                    if (ValueSet != null) {
                        validSingle = ValueSet.CheckValues(Values, out defaultValues);
                        for (int i = 0; i < valid.Length; i++) valid[i] |= validSingle[i];
                        if (IsRequired && Values.Length == 0) {
                            if (defaultValues != null && defaultValues.Length != 0) {
                                Values = defaultValues;
                                valid = new bool[Values.Length];
                                this.value = null;
                                for (int i = 0; i < Values.Length; i++) {
                                    valid[i] = true;
                                    if (i == 0) this.value = String.Empty; else this.value += separatorStr;
                                    this.value += (Values[i] == null ? String.Empty : Values[i].Replace(separatorStr, @"\" + separatorStr));
                                }
                            }
                        }
                    }
                } else {
                    string defaultValue = "";
                    string tempSelectedCaption = "";
                    selectedCaption = null;
                    if (SpecificValueSet != null) {
                        bool tempValid;
                        tempValid = Range != ValueSelectRange.Restricted || SpecificValueSet.CheckValue(value, out defaultValue, out tempSelectedCaption);
                        //context.AddDebug(3, "tsc1(" + Name + ") = " + tempSelectedCaption);
                        if (Values.Length != 0) {
                            valid[0] |= tempValid;
                            if (selectedCaption == null) selectedCaption = tempSelectedCaption;
                        } else if (IsRequired && defaultValue != null) {
                            Values = new string[]{defaultValue}; // !!! assign consolidated default values after second check (ValueSet)
                            valid = new bool[]{true};
                        }
                    }
                    if (ValueSet != null) {
                        bool tempValid;
                        tempValid = Range != ValueSelectRange.Restricted || ValueSet.CheckValue(value, out defaultValue, out tempSelectedCaption);
                        if (context.ConsoleDebug) Console.WriteLine("VALID = " + tempValid + " " + Value);
                        //context.AddDebug(3, "tsc2(" + Name + ") = " + tempSelectedCaption);
                        if (Values.Length != 0) {
                            valid[0] |= tempValid;
                            if (selectedCaption == null) selectedCaption = tempSelectedCaption;
                        } else if (IsRequired && defaultValue != null) {
                            Values = new string[]{defaultValue}; // !!! assign consolidated default values after this check
                            valid = new bool[]{true};
                        }
                    }
                }
                
            } else if (Multiple) {
                for (int i = 0; i < Values.Length; i++) valid[i] = true;

            } else if (Values.Length != 0) {
                string errorMessage;
                switch (Type) {
                    case "bool" :
                        Values[0] = Values[0].ToLower();
                        valid[0] = (Values[0] == "true" || Values[0] == "false" || Values[0] == "on" || Values[0] == "yes" || Values[0] == "no");
                        if (!valid[0]) HandleError(String.Format("{0} must be \"true\" or \"false\"", ParameterSet.CollectValidityErrors ? "Value" : "value"), true);
                        break;
                    case "int" :
                        int i;
                        valid[0] = Int32.TryParse(Values[0], out i);
                        if (!valid[0]) HandleError(String.Format("{0} must be an integer number", ParameterSet.CollectValidityErrors ? "Value" : "value"), true);
                        break;
                    case "float" :
                        double d;
                        valid[0] = Double.TryParse(Values[0], out d);
                        if (!valid[0]) HandleError(String.Format("{0} must be a real number", ParameterSet.CollectValidityErrors ? "Value" : "value"), true);
                        break;
                    case "date" :
                    case "datetime" :
                    case "startdate" :
                    case "enddate" :
                        DateTime dt;
                        valid[0] = DateTime.TryParse(Values[0], out dt);
                        if (valid[0]) Values[0] = dt.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss"); 
                        else HandleError(String.Format("{0} must be in the format \"{1}\"", ParameterSet.CollectValidityErrors ? "Date value" : "date value", Type == "date" ? "YYYY-MM-DD" : "YYYY-MM-DDThh:mm:ss"), true);
                        break;
                    default :
                        valid[0] = true;
                        break;
                }
            }
            
            isValid = true;

            if ((Values == null || Values.Length == 0) && MinOccurs >= 1) {
                isValid = false;
                Message = "Value cannot be empty";
            } else if (Values.Length < MinOccurs) {
                isValid = false;
                Message = "Less than {0} values";
            } else if (MaxOccurs != 0 && Values != null && Values.Length > MaxOccurs) {
                isValid = false;
                Message = String.Format("More than {0}", MaxOccurs == 1 ? "one value" : String.Format("{0} values", MaxOccurs));
                //HandleError(String.Format("More than {0}{1}", MaxOccurs == 1 ? "one value" : String.Format("{0} values", Values.Length), ParameterSet.CollectValidityErrors ? String.Empty : String.Format(" for parameter {0}", Name)), false);
            } else {
                for (int i = 0; i < valid.Length; i++) {
                    if (!valid[i]) {
                        isValid = false;
                        if (Reference && Message == null) Message = "Value is not in list";
                        break;
                    }
                }
            }

            validityUpdated = true;

            if (context.ConsoleDebug) {
                Console.WriteLine("    -> [" + Values.Length + "] \"" + Value + "\" " + (IsRequired ? "[required]" : "[optional]") + " " + (isValid ? "[valid]" : "[invalid] " + Message));
                if (Name == "output_format") Console.WriteLine("VALUE = " + Values[0]);
            }

            if (!isValid) throw new InvalidServiceParameterException(String.Format("Invalid value for parameter \"{0}\": {1}", Name, Message));

            //context.AddDebug(3, Name + ": VALUE = " + Value);
            
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sets the parameter state to invalid together with an error message.</summary>
        /// <param name="message">The error message.</param>
        public void Invalidate(string message) {
            isValid = false;
            validityUpdated = true;
            this.Message = message;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void HandleError(string message, bool withPrefix) {
            if (ParameterSet.CollectValidityErrors) Message = message;
            else throw new InvalidServiceParameterException(String.Format("{0}{1}", withPrefix ? String.Format("Invalid value for {0}: ", Name) : String.Empty, Message));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void WriteField(XmlWriter output) {
            output.WriteStartElement("field");
            output.WriteAttributeString("name", Name); 
            if (IsSwitch) output.WriteAttributeString("switch", "true");
            output.WriteAttributeString("type", (Type == null ? "string" : Type));
            output.WriteAttributeString("caption", Caption);
            if (Source != null) output.WriteAttributeString("source", Source);
            if (SearchExtension != null) output.WriteAttributeString("ext", SearchExtension);
            if (!IsRequired) output.WriteAttributeString("optional", "true");
            if (ReadOnly) output.WriteAttributeString("readonly", "true");
            if (HasConfigurableValueSet) {
                output.WriteAttributeString("scope", ScopeToString(Scope));
                if (Range != ValueSelectRange.Restricted) output.WriteAttributeString("range", RangeToString(Range));
            }
            if (Withhold) output.WriteAttributeString("withhold", "true");
            if (additionalAttributes != null) output.WriteRaw(additionalAttributes);
            
            if (!IsRequired && SpecificValueSet != null || ValueSet != null) {
                output.WriteStartElement("element");
                output.WriteAttributeString("value", String.Empty);
                output.WriteString(NullCaption == null ? "[no selection]" : NullCaption);
                output.WriteEndElement();
            }
            if (SpecificValueSet != null) SpecificValueSet.WriteValues(output);
            if (ValueSet != null) ValueSet.WriteValues(output);
            
            output.WriteEndElement(); // </field>
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void WriteValue(XmlWriter output) {
            bool written = false;
            
            output.WriteStartElement(Name);
            if (!IsValid) {
                output.WriteAttributeString("valid", "false");
                if (context.DebugLevel >= 1) context.AddDebug(1, String.Format("Parameter \"{0}\" invalid{1}", Name, Message == null ? String.Empty : ": " + Message));
            }
            
            if (Values.Length != 0 && (Reference && !Multiple && Range == ValueSelectRange.Restricted)) {
                output.WriteAttributeString("value", Values[0]);
                written = true;
            }
            if (Message != null) output.WriteAttributeString("message", Message);

            if (Reference && Multiple) {
                for (int j = 0; j < Values.Length; j++) {
                    output.WriteStartElement("element");
                    if (!Valid[j]) output.WriteAttributeString("valid", "false");
                    output.WriteAttributeString("value", Values[j]);
                    output.WriteEndElement(); // </element>
                }

            } else if (!Reference && Multiple) {
                for (int j = 0; j < Values.Length; j++) output.WriteElementString("element", Values[j]);

            } else if (!written && Values.Length != 0) {
                output.WriteString(Values[0]);
            }
            
            output.WriteEndElement(); // Name
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

    

    public class ProcessingInputSet {
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public ServiceParameterSet ParameterSet { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates or determines whether a catalogue query is performed to obtain the inputs.</summary>
        /// <remarks>If set to <c>true</c>, the value of the ProductParameter contains a list of catalogue URLs of the individual input products, otherwise it is the single catalogue URL that selects all products.</remarks>
        public bool AutomaticQuery { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public bool IsEmpty { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceInputCollectionParameter CollectionParameter { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public ServiceInputProductParameter ProductParameter { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public ServiceParameterArray SearchParameters { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public ProcessingInputSet(ServiceParameterSet parameterSet) {
            this.ParameterSet = parameterSet;
            this.SearchParameters = new ServiceParameterArray();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public ProcessingInputSet(ServiceParameterSet parameterSet, XmlElement element) : this(parameterSet) {

            if (element.HasAttribute("automatic") && element.Attributes["automatic"].Value == "true") AutomaticQuery = true;
            string name = (element.HasAttribute("name") ? element.Attributes["name"].Value : "unknown");
            
            foreach (XmlNode node in element.ChildNodes) {
                XmlElement subElement = node as XmlElement;
                if (subElement == null) continue;

                switch (subElement.Name) {
                    case "collection" :
                        if (CollectionParameter == null) CollectionParameter = new ServiceInputCollectionParameter(this, subElement);
                        else throw new Exception(String.Format("Multiple input collection parameters defined for input \"{0}\"", name));
                        //param.ValueSet = GetSeriesValueSet();
                        break;
                    case "product" : // files
                        if (ProductParameter == null) ProductParameter = new ServiceInputProductParameter(this, subElement);
                        else throw new Exception(String.Format("Multiple input product parameters defined for input \"{0}\"", name));
                        break;
                    case "metadata" :
                        //TODO MetadataFields.Add(new MetadataField(this, subElement);
                        //param.ValueSet = GetSeriesValueSet();
                        break;
                    case "parameter" :
                        SearchParameters.Add(new ServiceInputSearchParameter(this, subElement));
                        break;
                }
            }

        }




        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the URL for an OpenSearch request based on an URL template and the search parameters of the input set.</summary>
        /// <remarks>
        ///     This is mainly used for the catalogue request for the task input files.
        ///     The OpenSearch URL parameters with a extension name (between curly brackets in the OpenSearch URL template) 
        ///     <ul>
        ///         <li>If the service parameter collection contains a service parameter with the same extension name, the parameter is included in the returned URL with its value.</li>
        ///         <li>If the service parameter collection does not contain such a service parameter, the OpenSearch parameter is not included in the returned URL.</li>
        ///     </ul>
        /// </remarks>
        /// <param name="urlTemplate">the OpenSearch URL template containing the OpenSearch parameters</param>
        public string GetRequestUrlForTemplate(string urlTemplate) {
            Match match = Regex.Match(urlTemplate, "([^\\?]+)\\?(.*)");
            if (!match.Success) return null;

            string script = match.Groups[1].Value;
            string[] parameters = match.Groups[2].Value.Split('&');
            string queryString = null;

            // For each query string parameter, add the value of the matching parameter of the current request to the forward request URL
            // ("current request" means the request we are currently serving, "forward request" means the request we have to make to the next CAS)
            // The parameters match if they have the same extension name, i.e. the name in the curly brackets in the template,
            // e.g. "time:start" in "startdate={time:start?}" 
            foreach (string extensionStr in parameters) {
                match = Regex.Match(extensionStr, @"^([^=]+)={([^?]+)\??}$");
                
                // Keep parameters that are no OpenSearch extensions
                if (!match.Success) {
                    if (queryString == null) queryString = String.Empty; else queryString += "&";
                    queryString += extensionStr;
                    continue;
                }
                
                string name = match.Groups[1].Value;
                string extension = match.Groups[2].Value;
                
                // Add the parameter if it is provided in the current request's URL
                string value = null;
                foreach (ServiceParameter param in SearchParameters) {
                    if (param.SearchExtension == extension) {
                        value = param.Value;
                        break;
                    }
                }
                
                if (queryString == null) queryString = String.Empty; else queryString += "&";
                queryString += String.Format("{0}={1}", name, value == null ? String.Empty : value);
            }
            
            // Return the complete forward request URL
            return script + "?" + queryString;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void GetInputData() {
/*            if (Scheduler != null) {
                //AllowEmpty = Scheduler.CanCreateEmptyTasks;
                done = Scheduler.GetNextInputData(this);
                errorAdded = Error;
            }*/

            //InputFiles = new CatalogueDataSetCollection(0);
            
            if (CollectionParameter == null || CollectionParameter.Series == null || ProductParameter == null) throw new InvalidOperationException("Incomplete input parameter configuration");
            
            CatalogueResult result;
            
            // If no files have been provided in the request, perform catalogue query
            if (ProductParameter.Values.Length == 0) {
                string url = CollectionParameter.Series.CatalogueUrlTemplate;
                url = GetRequestUrlForTemplate(url);
                //result = new CatalogueResult(ParameterSet.Context, url, 100);
                
            } else {
                //result = new CatalogueResult(ParameterSet.Context, ProductParameter);
            }
            
            
                // TODO move to caller            if (InputFiles != null && InputFiles.Count != 0) return;
            
                /*foreach (XmlNode xmlNode in Service.DefinitionDocument.DocumentElement.ChildNodes) {
                    if ((elem = xmlNode as XmlElement) == null || elem.Name != "metadata") continue;
    
                    elemOwner = (elem.HasAttribute("owner") ? elem.Attributes["owner"].Value : null);
                    if (elemOwner == null) continue;
                    
                    ownerParam = requestParameters[elemOwner]; 
                    if (ownerParam == null || ownerParam == ServiceParameter.Empty) continue;
                
                    if (ownerParam == ProductParameter) {
                        // If the owner of the owning dataset (usually named "files") parameter is a series parameter, get the corresponding series
                        if (ownerParam.Owner != null && ownerParam.Owner.Name != null && ownerParam.Owner.Source == "series") fileSeries = seriesx[ownerParam.Owner.Name];
                        if (fileSeries == null) break;

                        foreach (XmlNode optionNode in elem.ChildNodes) {
                            XmlElement optionElem = optionNode as XmlElement;
                            if (optionElem == null || !optionElem.HasAttribute("name") || !optionElem.HasAttribute("value")) continue;

                            string resultElemName = null, resultFieldName = null;
                            resultFieldName = optionElem.Attributes["name"].Value;
                            if (resultFieldName == String.Empty) continue;
                            resultElemName = optionElem.Attributes["value"].Value;  //.Replace(",", "\\,"); // !!! why this?
                            result.AddMetadataField(resultFieldName, resultElemName);
                            //if (ownerParam.Element.HasAttribute("identifier")) result.IdentifierElementName = ownerParam.Element.Attributes["identifier"].Value;
                        }
                    }
                }*/
                
            /*result.GetDataSets(0, 1000);

            ProductParameter.Values = new string[result.ReceivedResults];
            for (int j = 0; j < result.ReceivedResults; j++) ProductParameter.Values[j] = result.DataSetResources[j];
            IsEmpty = (result.ReceivedResults == 0);
            
            if (IsEmpty && ProductParameter.IsRequired) {
                ProductParameter.Invalidate("No input files found for the specified criteria");
            }*/

            if (true) {
            }


            /* if (!IsEmpty) {
                int start = InputFiles.Count;
                InputFiles.Append(catalogueResult.DataSets.Length);
                for (int j = 0; j < catalogueResult.DataSets.Length; j++) InputFiles[start + j] = catalogueResult.DataSets[j];
            }*/

            if (true) {
            }


        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class ProcessingOutputSet {
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public ServiceParameterSet ParameterSet { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceOutputLocationParameter LocationParameter { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceOutputCompressionParameter CompressionParameter { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public ProcessingOutputSet(ServiceParameterSet parameterSet) {
            this.ParameterSet = parameterSet;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public ProcessingOutputSet(ServiceParameterSet parameterSet, XmlElement element) : this(parameterSet) {

            string name = (element.HasAttribute("name") ? element.Attributes["name"].Value : "unknown");

            foreach (XmlNode node in element.ChildNodes) {
                XmlElement subElement = node as XmlElement;
                if (subElement == null) continue;

                switch (subElement.Name) {
                    case "location" :
                        break;
                    case "compression" :
                        if (CompressionParameter == null) CompressionParameter = new ServiceOutputCompressionParameter(this, subElement);
                        else throw new Exception(String.Format("Multiple output compression parameters defined for output \"{0}\"", name));
                        //param.ValueSet = GetSeriesValueSet();
                        break;
                    case "filter" :
                        //if (OutputCompressionParameter == null) OutputFilterParameter = new ServiceOutputFilterParameter(this, subElement);
                        //else throw new Exception(String.Format("Multiple output filter parameters defined for output location {0}", Name));
                        //param.ValueSet = GetSeriesValueSet();
                        break;
                    case "collection" :
                        // TODO
                        break;
                    default :
                        // TODO throw exception;
                        break;
                }
            }

        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class ServiceInputCollectionParameter : ServiceParameter {
        
        //---------------------------------------------------------------------------------------------------------------------

        public Series Series { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceInputCollectionParameter(ProcessingInputSet inputSet, XmlElement element) : base(inputSet.ParameterSet, element) {
            Reference = true;

            /*Series result = Series.GetInstance(inputSet.ParameterSet.Context);
            result.ServiceId = inputSet.ParameterSet.Service.Id;
            result.UserId = inputSet.ParameterSet.Service.UserId;*/
            // TODO: re-enable!!!
            //result.AcceptCaptionsAsValues = true;
            //result.RegexPattern = element.HasAttribute("regex") ? element.Attributes["regex"].Value : null;
            //ValueSet = result;
            Type = "select";
            UpdateType();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override void Check() {
            base.Check();
            //Console.WriteLine("IS_VALID " + IsValid + ", VALUE = " + Value);
            if (IsValid) Series = Series.FromString(ParameterSet.Context, Value);
        }
        
        /*
            param.Reference = true;
            param.Multiple = false;
            param.SetType();
            param.Source = "series";
            //param.Level = RequestParameterLevel.Custom;

            if (param.GetValues(ParameterUpdateMethod)) Changed = true;

            Series item = null;
            
            int count = param.Check(xmlWriter, true);
            ServiceParameterValueSet optionSource = param.SpecificValueSet as ServiceParameterValueSet;
            // if (param.AllValid && optionSource != null && (!Defining || Exists)) {
            if (param.AllValid && optionSource != null && (!Defining)) {
                    XmlElement seriesOption = optionSource.SelectedElement;
                    string caption = seriesOption.InnerXml;
                    string catalogueDescriptionUrl = (seriesOption.HasAttribute("description") ? seriesOption.Attributes["description"].Value : null);
                    string catalogueUrlTemplate = (seriesOption.HasAttribute("template") ? seriesOption.Attributes["template"].Value : null);
                    if (catalogueDescriptionUrl != null) {
                        item = new CustomSeries(context, param.Values[0], caption, catalogueDescriptionUrl, catalogueUrlTemplate);
                    }
            }

            if (count == 0) { 
                param.Invalidate("There are no series available");
            } else if (param.Values.Length != 0) {
                context.HideMessages = true;
                try {
                    if (item == null) item = Series.FromString(context, param.Values[0]);
                    item.UserId = UserId;
                    param.Values[0] = item.Identifier;
                } catch (EntityNotFoundException) {}
                context.HideMessages = false;

                if (param.AllValid) {
                    if (!Defining || Exists) seriesx.Add(param.Name, item);
                    if (!Defining) seriesx.Add(param.Name, item);
                } else if (context.UserLevel == UserLevel.Administrator) {
                    if (item != null && item.Exists) {
                        if (item.CanBeUsedWithService(ServiceId)) param.Invalidate((UserId == context.UserId ? "You are" : "The owner is") + " not authorized to use the selected series with this service");
                        else param.Invalidate("The selected series is not compatible with this service");
                    } else {
                        param.Invalidate("The specified series does not exist");
                    }
                } else {
                    param.Invalidate("The specified series does not exist, is not compatible with this service or " + (UserId == context.UserId ? "you are" : "the task owner is") + " not authorized to use it with this service");
                }*/

    }


    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class ServiceInputProductParameter : ServiceParameter {

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceInputProductParameter(ProcessingInputSet inputSet, XmlElement element) : base(inputSet.ParameterSet, element) {

        }
        
        public override void WriteValue(XmlWriter output) {
            /*Series fileSeries = seriesx[Owner.Name];
            if (fileSeries != null) {
                for (int j = 0; j < fileSeries.DataSets.Length; j++) {
                    output.WriteStartElement("element");
                    output.WriteAttributeString("value", fileSeries.DataSets[j].Name);
                    // output.WriteAttributeString("size", fileSeries.DataSets[j].Size.ToString());
                    // output.WriteAttributeString("start", fileSeries.DataSets[j].StartTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss"));
                    // output.WriteAttributeString("end", fileSeries.DataSets[j].EndTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss"));
                    foreach (KeyValuePair<string, string> kvp in fileSeries.MetadataElementNames) {
                        output.WriteAttributeString(kvp.Key, fileSeries.DataSets[j][kvp.Key].ToString());
                    }
                    output.WriteEndElement(); // </element>
                }
            }*/
        }
    }


    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class ServiceInputSearchParameter : ServiceParameter {

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceInputSearchParameter(ProcessingInputSet inputSet, XmlElement element) : base(inputSet.ParameterSet, element) {

        }
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class ServiceOutputLocationParameter : ServiceParameter {

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceOutputLocationParameter(ProcessingOutputSet outputSet, XmlElement element) : base(outputSet.ParameterSet, element) {
            PublishServer result = PublishServer.GetInstance(outputSet.ParameterSet.Context);
            //result.IncludePublic = true;
            result.UserId = outputSet.ParameterSet.Service.UserId;
            //result.AcceptCaptionsAsValues = true;
            ValueSet = result;
            Type = "select";
            UpdateType();
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class ServiceOutputCompressionParameter : ServiceParameter {

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceOutputCompressionParameter(ProcessingOutputSet outputSet, XmlElement element) : base(outputSet.ParameterSet, element) {
            string compressionList = outputSet.ParameterSet.Context.GetConfigValue("CompressionValues");
            if (compressionList == null) compressionList = "NONE:No compression;GZ:Compressed individually;TGZ:Compressed in one file;TAR:TAR archive";
            ValueSet = new FixedValueSet(compressionList);
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class ServiceOutputCollectionParameter : ServiceParameter {

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceOutputCollectionParameter(ProcessingOutputSet outputSet, XmlElement element) : base(outputSet.ParameterSet, element) {

        }
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class ServiceComputingResourceParameter : ServiceParameter {
        
        //---------------------------------------------------------------------------------------------------------------------

        public ServiceComputingResourceParameter(ServiceParameterSet parameterSet, XmlElement element) : base(parameterSet, element) {
            ComputingResource result = ComputingResource.GetInstance(parameterSet.Context);
            //result.ServiceId = parameterSet.Service.Id;
            result.UserId = parameterSet.Service.UserId;
            //result.AcceptCaptionsAsValues = true;
            ValueSet = result;
            Type = "select";
            UpdateType();
        }
        
    }


    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class ServicePriorityParameter : ServiceParameter {
        
        //---------------------------------------------------------------------------------------------------------------------

        public ServicePriorityParameter(ServiceParameterSet parameterSet, XmlElement element) : base(parameterSet, element) {
            /*string priorityList = Context.GetConfigValue("PriorityValues");
            if (priorityList == null) priorityList = "0.25:Very low;0.5:Low;1:Normal;2:High;4:Very high";

            string[] priorityValues = priorityList.Split(';');
            string[] priorityCaptions = new string[priorityValues.Length];
            double[] priorities = new double[priorityValues.Length];
            for (int i = 0; i < priorityValues.Length; i++) {
                Match match = Regex.Match(priorityValues[i], @"^(\*)?([^:]+):(.+)");
                if (!match.Success) continue;
                priorityValues[i] = match.Groups[2].Value;
                Double.TryParse(priorityValues[i], out priorities[i]);
                priorityValues[i] = priorities[i].ToString();
                priorityCaptions[i] = match.Groups[3].Value;
            }
            
            int count = 0;
            for (int i = 0; i < priorities.Length; i++) {
                if (maxPriority != 0 && (priorities[i] == 0 || priorities[i] < minPriority || priorities[i] > maxPriority)) continue;
                
                if (i != count) {
                    priorities[count] = priorities[i];
                    priorityValues[count] = priorityValues[i];
                    priorityCaptions[count] = priorityCaptions[i];
                }
                count++;
            }
            
            if (count != priorities.Length) {
                Array.Resize(ref priorities, count);
                Array.Resize(ref priorityValues, count);
                Array.Resize(ref priorityCaptions, count);
            }
            AllowedPriorities = priorities;
            return new FixedValueSet(priorityValues, priorityCaptions);*/ 
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ServiceProcessNameParameter : ServiceParameter {

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceProcessNameParameter(ServiceParameterSet parameterSet, XmlElement element) : base(parameterSet, element) {

        }

    }

}

