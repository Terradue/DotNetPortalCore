using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MySql.Data.MySqlClient;
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

    

    /// <summary>Defines class members for listing elemets of a value set and validation of single or multiple values.</summary>
    /*!
        Implementing classes represent value sets with values from a specific source, such as values from a lookup list, values defined for a service defined by or for a user, or the options of a service parameter defined in a service definition file. 
        
        For the listing of the values, each value (in many cases the value is only a short code) has a caption assigned, which is the meaning of the value and more understandable to a human.
        Value lists are in XML format and written to an XmlWriter object. In a value list, the elements of a value set are displayed as a list of XML elements of the following kind:
        
        <i>&lt;element value="value"&gt;caption&lt;/element&gt;</i>
        
        A derived class may add further attributes to the <i>&lt;element&gt;</i> element.
    */
    public interface IValueSet {
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, gets the values contained in the value set as an array or a <i>null</i> reference if the value range is open.</summary>
        string[] GetValues();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, generates a string containing explanations for the values contained in the value set.</summary>
        /*!
        /// <returns>a string containing a list of value/caption pairs</returns>

            The returned value is a list, separated by a comma and a new line, of values and their corresponding captions, which are separated by a colon and a space.
            
            <i>Example:</i><br/>
            value1: Meaning of value 1,<br/>
            value2: Meaning of value 2,<br/>
            ...
        */
        string GetExplanation();
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, checks whether the specified value is contained in the value set.</summary>
        /*!
        /// <param name="value">the value to be checked</param>
        /// <param name="defaultValue">contains the default value of the value set or <i>null</i> if there is no default value (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns><i>true</i> if <i>value</i> is contained in the value set</returns>

            Besides checking the validity of the specified value, the method provides useful information to the caller regarding the value set:
            <ul>
                <li>The output parameter <i>defaultValue</i> returns the default value of the value set (if there is any).</li>
                <li>The output parameter <i>count</i> returns the the number of elements in the value set.</li>
                <li>If <i>output</i> is a reference to an XmlWriter object, the elements are written to the XmlWriter in <i>&lt;element&gt;</i> elements.<br/><b>Note:</b> if the list output is desired, the containing XML element must be opened before and closed after the call to this method by the caller.</li>
            </ul>
        */
        bool CheckValue(string value, out string defaultValue, out string selectedCaption);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, checks for each element in the specified value array whether it is contained in the value set.</summary>
        /*!
        /// <param name="values">the array of values to be checked</param>
        /// <param name="defaultValues">contains the default values of the value set or <i>null</i> if there are no default values (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns>an array of the same shape as <i>values</i>, where an element has the value <i>true</i> if the corresponding element in <i>values</i> is contained in the value set</returns>

            Besides checking the validity of the specified values, the method provides useful information to the caller regarding the value set:
            <ul>
                <li>The output parameter <i>defaultValues</i> returns the default values of the value set (if there are any).</li>
                <li>The output parameter <i>count</i> returns the the number of elements in the value set.</li>
            </ul>
        */
        bool[] CheckValues(string[] values, out string[] defaultValues);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, writes all elements of the value set to the specified XmlWriter object.</summary>
        /*!
        /// <param name="output">the XmlWriter object for the output of the element</param>
        /// <returns>the number of elements that have been written to <i>output</i></returns>
        */
        int WriteValues(XmlWriter output);

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public interface IEditableValueSet : IValueSet {

        /// <summary>Gets the service that defines the service parameter.</summary>
        Service Service { get; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the parameter name as defined in the service configuration file.</summary>
        string Name { get; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the value selection scope as defined in the service configuration file.</summary>
        ValueSelectScope Scope { get; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the</summary>
        bool SingleValueOutput { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        bool CreateValue(string caption, string value);
    
        //---------------------------------------------------------------------------------------------------------------------

        bool ModifyValue(string caption, string value);
        
        //---------------------------------------------------------------------------------------------------------------------

        bool DeleteValue(string caption);
        
        //---------------------------------------------------------------------------------------------------------------------

        bool WriteValue(string caption, XmlWriter output);

    }
    
    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Represents a value set containing fixed values that can be hard-coded or result from a simple string in some configuration.</summary>
    /*
        This type of value set is used internally for the entity structure definition for generic operations (e.g. in the Control Panel) or in other situations with precise sematics.
    */
    public class FixedValueSet : IValueSet {
        private string[] values, captions;
        private bool[] valuesDefault;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets an array of the fixed values.</summary>
        public string[] GetValues() {
            return values;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a FixedValueSet instance based on arrays.</summary>
        /*!
        /// <param name="values">a string array containing the values of the value set</param>
        /// <param name="captions">a string array containing the captions associated with the values</param>
            
            The two arrays must have the same number of elements. None of the values is considered a default value.
        */
        public FixedValueSet(string[] values, string[] captions) {
            this.values = values;
            this.captions = captions;
            this.valuesDefault = new bool[values.Length];
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a FixedValueSet instance based on arrays including default information.</summary>
        /*!
        /// <param name="values">a string array containing the values of the value set</param>
        /// <param name="captions">a string array containing the captions associated with the values</param>
        /// <param name="valuesDefault">an array of boolean values where an element has the value <i>true</i> if the corresponding value is a default value.</param>
            
            The three arrays must have the same number of elements.
        */
        public FixedValueSet(string[] values, string[] captions, bool[] valuesDefault) {
            this.values = values;
            this.captions = captions;
            this.valuesDefault = valuesDefault;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a FixedValueSet instance based on a definition string.</summary>
        /*!
        /// <param name="definition">a string containing values, captions and default information</param>
            
            The definition string is in the format <i>value1:caption1;value2:caption2;...</i>. An asterisk before a value means that the value is a default value.
        */
        
        public FixedValueSet(string definition) {
            values = StringUtils.Split(definition, ';');
            captions = new string[values.Length];
            valuesDefault = new bool[values.Length];

            for (int i = 0; i < values.Length; i++) {
                Match match = Regex.Match(values[i], @"^(\*)?([^:]+):(.+)");
                if (!match.Success) continue;
                values[i] = match.Groups[2].Value;
                captions[i] = match.Groups[3].Value;
                valuesDefault[i] = match.Groups[1].Success;
            }

        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Generates a string containing explanations for the fixed values.</summary>
        /*!
        /// <returns>a string containing a list of value/caption pairs</returns>
            \sa For more information, see the documentation of IValueSet::GetExplanation().
        */
        public string GetExplanation() {
            string result = null;
            for (int i = 0; i < values.Length; i++) {
                if (i == 0) result = String.Empty; else result += Environment.NewLine;
                result += values[i] + "\t" + captions[i];
            }
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the specified value is contained in the set of fixed values.</summary>
        /*!
        /// <param name="value">the value to be checked</param>
        /// <param name="defaultValue">contains the default value of the value set or <i>null</i> if there is no default value (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns><i>true</i> if <i>value</i> is contained in the value set</returns>
            \sa For more information, see the documentation of IValueSet::CheckValue().
        */
        public bool CheckValue(string value, out string defaultValue, out string selectedCaption) {
            bool result = false;
            defaultValue = null;
            selectedCaption = null;

            for (int i = 0; i < this.values.Length; i++) {
                if (values[i] == value) {
                    result = true;
                    selectedCaption = captions[i];
                }
                if (defaultValue == null && valuesDefault[i]) defaultValue = values[i];
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks for each element in the specified value array whether it is contained in the set of fixed values.</summary>
        /*!
        /// <param name="values">the array of values to be checked</param>
        /// <param name="defaultValues">contains the default values of the value set or <i>null</i> if there are no default values (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns>an array of the same shape as <i>values</i>, where an element has the value <i>true</i> if the corresponding element in <i>values</i> is contained in the value set</returns>
            \sa For more information, see the documentation of IValueSet::CheckValues().
        */
        public bool[] CheckValues(string[] values, out string[] defaultValues) {
            if (values == null) values = new string[0];
            bool[] result = new bool[values.Length];
            defaultValues = new string[0]; // !!! should contain default values

            for (int i = 0; i < this.values.Length; i++) {
                for (int j = 0; j < values.Length; j++) {
                    if (values[j] == this.values[i]) {
                        result[j] = true;
                        break;
                    }
                }
            }
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes all fixed values to the specified XmlWriter object.</summary>
        /*!
        /// <param name="output">the XmlWriter object for the output of the element</param>
        /// <returns>the number of elements that have been written to <i>output</i></returns>
        */
        public int WriteValues(XmlWriter output) {
            for (int i = 0; i < this.values.Length; i++) {
                output.WriteStartElement("element");
                output.WriteAttributeString("value", values[i]);
                output.WriteString(captions[i]);
                output.WriteEndElement();
            }
            return values.Length;
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a value set containing values resulting from the XML content of a service parameter definition in a service definition file.</summary>
    /*!
        This type of value set is used for service parameters defined in a service definition file.

        <b>Usage with a service parameter:</b> Provide <i>&lt;element&gt;</i> elements of the same kind as used in the value lists inside the parameter's <i>&lt;param&gt;</i> element:
        Each <i>&lt;element&gt;</i> element must contain a <i>value</i> attribute that holds the actual value. It may also contain additional attributes as well as a string content (the value caption). The additional attributes are also written to the resulting value list.
    */
    public class ServiceParameterValueSet : IValueSet {
        private XmlElement parameterElement;
        private XmlElement selectedElement;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets an array of the possible service parameter values.</summary>
        public string[] GetValues() { 
            List<string> values = new List<string>();
            XmlNodeList valueNodes = parameterElement.SelectNodes("element[@value]");
            foreach (XmlNode node in valueNodes) {
                XmlElement elem = node as XmlElement;
                if (!elem.HasAttribute("value")) continue;
                values.Add(elem.Attributes["value"].Value);
            }

            return values.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the XML element that matches the last validated value.</summary>
        public XmlElement SelectedElement { // !!! needed also as array
            get { return selectedElement; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new ServiceParameterValueSet instance based on an XML element describing a service parameter.</summary>
        /*!
        /// <param name="element">the <i>&lt;param&gt;</i> element describing the service parameter</param>
        */
        public ServiceParameterValueSet(XmlElement element) {
            this.parameterElement = element;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Generates a string containing explanations for the possible service parameter values.</summary>
        /*!
        /// <returns>a string containing a list of value/caption pairs</returns>
            \sa For more information, see the documentation of IValueSet::GetExplanation().
        */
        public string GetExplanation() {
            string result = null;
            XmlNodeList valueNodes = parameterElement.SelectNodes("element[@value]");
            bool first = true;
            foreach (XmlNode node in valueNodes) {
                XmlElement elem = node as XmlElement;
                if (first) {
                    result = String.Empty;
                    first = false;
                } else {
                    result += Environment.NewLine;
                }
                result += elem.Attributes["value"].Value + "\t" + elem.InnerXml;
            }
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the specified value is contained in the set of possible service parameter values.</summary>
        /*!
        /// <param name="value">the value to be checked</param>
        /// <param name="defaultValue">contains the default value of the value set or <i>null</i> if there is no default value (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns><i>true</i> if <i>value</i> is contained in the value set</returns>
            \sa For more information, see the documentation of IValueSet::CheckValue().
        */
        public bool CheckValue(string value, out string defaultValue, out string selectedCaption) {
            bool result = false;
            defaultValue = null;
            selectedCaption = null;

            XmlNodeList valueNodes = parameterElement.SelectNodes("element[@value]");
            foreach (XmlNode node in valueNodes) {
                XmlElement elem = node as XmlElement;
                if (!elem.HasAttribute("value")) continue;
                
                string elemValue = elem.Attributes["value"].Value;
                if (value == elemValue) {
                    result = true;
                    selectedCaption = elem.InnerXml;
                    selectedElement = elem;
                }

                if (defaultValue == null && (elem.HasAttribute("default") && elem.Attributes["default"].Value == "true" || elem.HasAttribute("selected") && elem.Attributes["selected"].Value == "true")) defaultValue = elemValue;
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks for each element in the specified value array whether it is contained in the set of possible service parameter values.</summary>
        /*!
        /// <param name="values">the array of values to be checked</param>
        /// <param name="defaultValues">contains the default values of the value set or <i>null</i> if there are no default values (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns>an array of the same shape as <i>values</i>, where an element has the value <i>true</i> if the corresponding element in <i>values</i> is contained in the value set</returns>
            \sa For more information, see the documentation of IValueSet::CheckValues().
        */
        public bool[] CheckValues(string[] values, out string[] defaultValues) {
            if (values == null) values = new string[0];
            bool[] result = new bool[values.Length];
            List<string> defaultValuesList = null;

            XmlNodeList valueNodes = parameterElement.SelectNodes("element[@value]");
            foreach (XmlNode node in valueNodes) {
                XmlElement elem = node as XmlElement;
                if (!elem.HasAttribute("value")) continue;
                
                string elemValue = elem.Attributes["value"].Value;
                for (int i = 0; i < values.Length; i++) if (values[i] == elemValue) result[i] = true;

                if (elem.HasAttribute("default") && elem.Attributes["default"].Value == "true") {
                    if (defaultValuesList == null) defaultValuesList = new List<string>();
                    defaultValuesList.Add(elemValue);
                }
            }
            if (defaultValuesList != null) defaultValues = defaultValuesList.ToArray();
            else defaultValues = null;
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes all service parameter values to the specified XmlWriter object.</summary>
        /*!
        /// <param name="output">the XmlWriter object for the output of the element</param>
        /// <returns>the number of elements that have been written to <i>output</i></returns>
        */
        public int WriteValues(XmlWriter output) {
            XmlNodeList valueNodes = parameterElement.SelectNodes("element[@value]");
            int count = 0;
            foreach (XmlNode node in valueNodes) {
                node.WriteTo(output);
                count++;
            }
            return count;
        }

    }
    

    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Abstract base class that represents a value set containing values from a database.</summary>
    public abstract class RelationalValueSet : IValueSet {

        protected IfyContext context;

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets an array of the values representing the referenced items.</summary>
        public virtual string[] GetValues() { 
            List<string> values = new List<string>();
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(GetListQuery(), dbConnection);
            while (reader.Read()) values.Add(reader.GetString(0));
            context.CloseQueryResult(reader, dbConnection);
            return values.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>In a derived class, returns the query for checking whether the specified value is contained in the value set. </summary>
        /*!
        /// <param name="value">the value to be checked</param>
        /// <returns>an SQL query selecting the number of values matching <i>value</i></returns>
        */
        protected abstract string GetValidationQuery(string value);

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>In a derived class, returns the query for retrieving the number of elements in the value set. </summary>
        /*!
        /// <returns>an SQL query selecting the total number of values in the value set</returns>
        */
        protected abstract string GetCountQuery();

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>In a derived class, returns the query for listing all values contained in the value set.</summary>
        /*!
        /// <returns>an SQL query selecting the values (and optionally the associated captions) contained in the value set</returns>
        */
        protected abstract string GetListQuery();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, generates a string containing explanations for the values contained in the value set.</summary>
        /*!
        /// <returns>a string containing a list of value/caption pairs</returns>
            \sa For more information, see the documentation of IValueSet::GetExplanation().
        */
        public string GetExplanation() {
            string result = null;
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(GetListQuery(), dbConnection);
            bool first = true;
            while (reader.Read()) {
                if (first) {
                    result = String.Empty;
                    first = false;
                } else {
                    result += Environment.NewLine;
                }
                result += reader.GetString(0) + "\t" + reader.GetString(1);
            }
            context.CloseQueryResult(reader, dbConnection);
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the specified value is contained in the value set.</summary>
        /*!
        /// <param name="value">the value to be checked</param>
        /// <param name="defaultValue">contains the default value of the value set or <i>null</i> if there is no default value (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns><i>true</i> if <i>value</i> is contained in the value set</returns>
            \sa For more information, see the documentation of IValueSet::CheckValue().
        */
        public virtual bool CheckValue(string value, out string defaultValue, out string selectedCaption) {
            bool result = (context.GetQueryIntegerValue(GetValidationQuery(value)) != 0);
            //context.AddInfo(GetValidationQuery(value) + " " + result);
            defaultValue = null;
            selectedCaption = null;

            if (!result) return false;

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(GetListQuery(), dbConnection);
            while (reader.Read()) {
                string elemValue = reader.GetString(0);
                if (elemValue == value) selectedCaption = reader.GetString(1);
            }
            context.CloseQueryResult(reader, dbConnection);

            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks for each element in the specified value array whether it is contained in the value set.</summary>
        /*!
        /// <param name="values">the array of values to be checked</param>
        /// <param name="defaultValues">contains the default values of the value set or <i>null</i> if there are no default values (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns>an array of the same shape as <i>values</i>, where an element has the value <i>true</i> if the corresponding element in <i>values</i> is contained in the value set</returns>
            \sa For more information, see the documentation of IValueSet::CheckValues().
        */
        public virtual bool[] CheckValues(string[] values, out string[] defaultValues) {
            if (values == null) values = new string[0];
            bool[] result = new bool[values.Length];
            defaultValues = new string[0]; // !!! should contain default values

            //context.ReturnError(query);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(GetListQuery(), dbConnection);
            while (reader.Read()) {
                string value = reader.GetString(0);
                for (int i = 0; i < values.Length; i++) {
                    if (values[i] == value) {
                        result[i] = true;
                        break;
                    }
                }
            }
            context.CloseQueryResult(reader, dbConnection);

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes all values to the specified XmlWriter object.</summary>
        /*!
        /// <param name="output">the XmlWriter object for the output of the element</param>
        /// <returns>the number of elements that have been written to <i>output</i></returns>
        */
        public virtual int WriteValues(XmlWriter output) {
            int count = 0;
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(GetListQuery(), dbConnection);
            while (reader.Read()) {
                count++;
                output.WriteStartElement("element");
                output.WriteAttributeString("value", reader.GetString(0));
                output.WriteString(reader.GetString(1));
                output.WriteEndElement();
            }
            context.CloseQueryResult(reader, dbConnection);

            return count;
        }
    }
    

    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a value set containing values referencing items of another entity that are stored in a database table.</summary>
    /*!
        This type of value set is used internally for the entity structure definition for generic operations (e.g. in the Control Panel).
        
        <b>Data storage:</b> Any database table can contain the values usable for this type of value set. The database table (or join) and the fields from which the values and captions are taken, are specified in the class properties.
    */
    public class ReferenceValueSet : RelationalValueSet {

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the table name or SQL join expression for the query.</summary>
        public string Join { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the SQL expression for the selection of the values.</summary>
        public string ValueExpression { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the SQL expression for the selection of the value captions.</summary>
        public string CaptionExpression { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the SQL expression for the ordering of the list items.</summary>
        public string SortExpression { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new ReferenceValueSet instance based on database table and field names.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="join">the table name or SQL join expression for the query</param>
        /// <param name="valueExpression">the SQL expression for the selection of the values</param>
        /// <param name="captionExpression">the SQL expression for the selection of the value captions</param>
        */
        public ReferenceValueSet(IfyContext context, string join, string valueExpression, string captionExpression) {
            this.context = context;
            this.Join = join;
            this.ValueExpression = valueExpression;
            this.CaptionExpression = (captionExpression == null ? valueExpression : captionExpression);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Returns the query for checking whether the specified value is contained in the set of referenced items. </summary>
        /*!
        /// <param name="value">the value to be checked</param>
        /// <returns>an SQL query selecting the number of reference values matching <i>value</i></returns>
        */
        protected override string GetValidationQuery(string value) {
            return String.Format(
                    "SELECT COUNT(*) FROM {0} WHERE {1}={2};",
                    Join,
                    ValueExpression,
                    StringUtils.EscapeSql(value)
            );
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Returns the query for retrieving the number of elements in the set of referenced items.</summary>
        /*!
        /// <returns>an SQL query selecting the total number of reference values in the value set</returns>
        */
        protected override string GetCountQuery() {
            return String.Format("SELECT COUNT(*) FROM {0};", Join);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Returns the query for listing all values contained in the set of referenced items.</summary>
        /*!
        /// <returns>an SQL query selecting the reference values (and optionally the associated captions) contained in the value set</returns>
        */
        protected override string GetListQuery() {
            if (SortExpression == null) SortExpression = "caption";
            return String.Format(
                    "SELECT DISTINCT {1}, {2} AS caption FROM {0} ORDER BY {3};", 
                    Join,
                    ValueExpression, 
                    CaptionExpression,
                    SortExpression
            );
        }

    }
    

    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a value set containing values from a lookup list that are stored in the database table <i>lookup</i>.</summary>
    /*!
        This type of value set is used internally for the entity structure definition for generic operations (e.g. in the Control Panel) and for service parameters defined in a service definition file.
        
        <b>Usage with a service parameter:</b> Set the following attribute of the parameter's <i>&lt;param&gt;</i> element:
        <ul>
            <li>the <i>list</i> attribute must contain the comma-separated list of lookup list names.</i>
        </ul>
        
        <b>Data storage:</b> The values are defined in the database table <i>lookup</i>, which belong to lookup lists, defined in the database table <i>lookuplist</i>.
    */
    public class LookupValueSet : RelationalValueSet {

        private string listCondition;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the names of the lookup lists separated by comma.</summary>
        public string ListNames { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether a list flagged as system list should be preferred to a custom list.</summary>
        /*!
            The included lookup lists depend on the property value and the lists that are available in the database:
            <ul>
                <li>If set to <i>false</i> in case of homonymous lists (one system list and one custom list), the custom list is included</li>
                <li>If set to <i>false</i> in case of only one available list, that list is included (regardless whether system or custom list)</li>
                <li>If set to <i>true</i> in case of homonymous lists (one system list and one included list), the system list is included</li>
                <li>If set to <i>true</i> in case of only one available list, the list is included only if it is a system list</li>
            </ul>
        */
        public bool ForceSystemList { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new LookupValueSet instance based on lookup list names.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="listNames">the names of the lookup lists separated by comma</param>
        /// <param name="forceSystemList">determines whether a list flagged as system list should be preferred</param>
        */
        public LookupValueSet(IfyContext context, string listNames, bool forceSystemList) {
            this.context = context;
            this.ListNames = listNames;
            this.ForceSystemList = forceSystemList;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Returns the query for checking whether the specified value is contained in the lookup list. </summary>
        /*!
        /// <param name="value">the value to be checked</param>
        /// <returns>an SQL query selecting the number of lookup values matching <i>value</i></returns>
        */
        protected override string GetValidationQuery(string value) {
            if (listCondition == null) {
                GetListCondition();
                if (listCondition == null) listCondition = "false";
            }
            return String.Format(
                    "SELECT COUNT(*) FROM lookup WHERE {0} AND value={1};",
                    listCondition,
                    StringUtils.EscapeSql(value)
            );
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Returns the query for retrieving the number of elements in the lookup list.</summary>
        /*!
        /// <returns>an SQL query selecting the total number of lookup in the value set</returns>
        */
        protected override string GetCountQuery() {
            if (listCondition == null) {
                GetListCondition();
                if (listCondition == null) listCondition = "false";
            }
            return String.Format("SELECT COUNT(*) FROM lookup WHERE {0};", listCondition);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Returns the query for listing all values contained in the lookup list.</summary>
        /*!
        /// <returns>an SQL query selecting the lookup values (and optionally the associated captions) contained in the value set</returns>
        */
        protected override string GetListQuery() {
            if (listCondition == null) {
                GetListCondition();
                if (listCondition == null) listCondition = "false";
            }
            return String.Format(
                    "SELECT value, caption FROM lookup WHERE {0} ORDER BY pos, caption;", 
                    listCondition
            );
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the SQL conditional expression for retrieving the values from the included lookup list(s).</summary>
        private void GetListCondition() {
            if (ListNames == null) return;
            string[] lists = ListNames.Split(',');
            string listNameCondition = String.Empty;
            for (int i = 0; i < lists.Length; i++) listNameCondition += (i == 0 ? String.Empty : ",") + StringUtils.EscapeSql(lists[i].Trim());
            if (lists.Length == 1) listNameCondition = "name=" + listNameCondition; 
            else listNameCondition = "name IN (" + listNameCondition + ")"; 
            
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT id FROM lookuplist WHERE {0} AND system={1};", listNameCondition, ForceSystemList ? "true" : "false"), dbConnection);
            int listCount = 0;
            while (reader.Read()) {
                listCount++;
                if (listCondition == null) listCondition = String.Empty; else listCondition += ",";
                listCondition += reader.GetInt32(0);
            }
            context.CloseQueryResult(reader, dbConnection);
            if (listCondition == null) return;
            if (listCount == 1) listCondition = "id_list=" + listCondition;
            else listCondition = "id_list IN (" + listCondition + ")";
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class FileListParameterValueSet : IValueSet {
        protected IfyContext context;

        private string pattern, path;
        private int fixLengthLeft, fixLengthTotal;
        private string paramRoot;
        
        /// <summary>Gets the service that defines the service parameter.</summary>
        public Service Service { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the parameter name as defined in the service configuration file.</summary>
        public string Name { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the</summary>
        public bool SingleValueOutput { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the values contained in the value set as an array or a <i>null</i> reference if the value range is open.</summary>
        /*!
            <b>Note:</b> This property returns a <i>null</i> reference, since service parameters that use predefined text files have by definition an open value range.
        */
        public string[] GetValues() {
            string[] files = null;
            if (pattern != null && Directory.Exists(path)) {
                files = Directory.GetFiles(path, GetPattern(pattern, path.Length));
                for (int i = 0; i < files.Length; i++) files[i] = files[i].Substring(fixLengthLeft, files[i].Length - fixLengthTotal);
            }
            return files;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a StoredFileParameterValueSet instance based on the characteristics of a service parameter.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="service">the service that defines the parameter</param>
        /// <param name="name">the parameter name</param>
        /// <param name="scope">the values selection scope</param>
        /// <param name="pattern">the search pattern for the files available to all users</param>
        /// <param name="userPattern">the search pattern for the current user's own files</param>
        */
        public FileListParameterValueSet(IfyContext context, Service service, string name, string pattern) {
            this.context = context;
            this.Service = service;
            this.Name = name;
            
            if (pattern != null) {
                if (pattern.IndexOf("$(PARAM") == 0) paramRoot = context.ServiceParamFileRoot;
                if (paramRoot == null) paramRoot = Service.FileRootDir;
                pattern = pattern.Replace("$(PARAMROOT)", paramRoot).Replace("$(PARAMDIR)", Service.FileRootDir + Path.DirectorySeparatorChar + Name);
                if (pattern.Contains("$(SERVICEROOT)")) pattern = pattern.Replace("$(SERVICEROOT)", Service.FileRootDir);

                pattern = Regex.Replace(pattern, @"[/\\]+", Path.DirectorySeparatorChar.ToString());

                Match match = Regex.Match(pattern, @"^([^\*]+)\" + Path.DirectorySeparatorChar + @"(([^\*\" + Path.DirectorySeparatorChar + @"]*)\*([^\*\" + Path.DirectorySeparatorChar + @"]*))$");
                if (!match.Success) context.ReturnError(new ArgumentException("Invalid file pattern"), null);
    
                this.path = match.Groups[1].Value;
                this.pattern = match.Groups[2].Value;
            }

        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Generates a string containing explanations for the service parameter values in the selection scope.</summary>
        /*!
        /// <returns>a string containing a list of value/caption pairs</returns>
            \sa For more information, see the documentation of IValueSet::GetExplanation().
        */
        public string GetExplanation() {
            return null;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the list of text files containing predefined service parameter values to the specified XmlWriter object. </summary>
        /*!
        /// <param name="value">the value to be checked</param>
        /// <param name="defaultValue">contains the default value of the value set or <i>null</i> if there is no default value (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns><i>true</i></returns>

            <b>Note:</b> This method performs no validity check, since service parameters that use predefined text files have by definition an open value range.
        */
        public bool CheckValue(string value, out string defaultValue, out string selectedCaption) {
            bool result = false;
            defaultValue = null;
            selectedCaption = null;

            string[] files;
            string elemValue;

            if (pattern != null && Directory.Exists(path)) {
                files = Directory.GetFiles(path, GetPattern(pattern, path.Length));
                for (int i = 0; i < files.Length; i++) {
                    elemValue = files[i].Substring(fixLengthLeft, files[i].Length - fixLengthTotal);
                    if (elemValue == value) {
                        result = true;
                        selectedCaption = elemValue;
                    }
                }
            }
            
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the list of text files containing predefined service parameter values to the specified XmlWriter object. </summary>
        /*!
        /// <param name="values">the array of values to be checked</param>
        /// <param name="defaultValues">contains the default values of the value set or <i>null</i> if there are no default values (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns>an array of the same shape as <i>values</i>, where all elements have the value <i>true</i></returns>

            <b>Note:</b> This method performs no validity check, since service parameters that use predefined text files have by definition an open value range. Furthermore, this method should never be called since this kind of service parameter cannot have multiple values.
        */
        public bool[] CheckValues(string[] values, out string[] defaultValues) {
            if (values == null) values = new string[0];
            bool[] result = new bool[values.Length];
            defaultValues = new string[0];
            for (int i = 0; i < values.Length; i++) result[i] = true;

            string[] files;
            string elemValue;
            
            if (pattern != null && Directory.Exists(path)) {
                files = Directory.GetFiles(path, GetPattern(pattern, path.Length));
                for (int i = 0; i < files.Length; i++) {
                    elemValue = files[i].Substring(fixLengthLeft, files[i].Length - fixLengthTotal);
                    for (int j = 0; j < values.Length; j++) {
                        if (values[j] == elemValue) {
                            result[j] = true;
                            break;
                        }
                    }
                }
            }

            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes all captions of predefined text files to the specified XmlWriter object.</summary>
        /*!
        /// <param name="output">the XmlWriter object for the output of the element</param>
        /// <returns>the number of elements that have been written to <i>output</i></returns>
        */
        public int WriteValues(XmlWriter output) {
    
            int count = 0;
            
            if (SingleValueOutput) output.WriteStartElement(Name);

            if (context.DebugLevel >= 1) {
                context.WriteDebug(1, "$(PARAMROOT) = " + paramRoot);
                context.WriteDebug(1, "$(PARAMDIR) = " + Service.FileRootDir + Path.DirectorySeparatorChar + Name);
                context.WriteDebug(1, "Search pattern for shared files: " + (pattern == null ? "[not used]" : path + Path.DirectorySeparatorChar + GetPattern(pattern, path.Length)));
            }
            string[] files;
            string value;
            
            if (pattern != null && Directory.Exists(path)) {
                files = Directory.GetFiles(path, GetPattern(pattern, path.Length));
                count += files.Length;
                for (int i = 0; i < files.Length; i++) {
                    value = files[i].Substring(fixLengthLeft, files[i].Length - fixLengthTotal);
                    output.WriteStartElement("element");
                    output.WriteAttributeString("value", value);
                    output.WriteString(value);
                    output.WriteEndElement(); // </element>
                }
            }

            if (SingleValueOutput) output.WriteEndElement(); // Name
            return count;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the filter pattern for the list of selectable files.</summary>
        /*!
        /// <param name="pattern">the file part of the pattern</param>
        /// <param name="lengthLeft">the length of the path part</param>
        /// <returns>the modified filter pattern</returns>
        */
        private string GetPattern(string pattern, int lengthLeft) {
            string result = pattern;
            fixLengthLeft = lengthLeft + result.IndexOf('*') + 1; 
            if (result.EndsWith("*")) result += ".txt";
            fixLengthTotal = fixLengthLeft + result.Length - result.IndexOf('*') - 1;
            return result;
        }
        
    }


    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a value set containing service parameter values applicable to a specific scope that are stored in the database.</summary>
    /*!
        This type of value set is used for service parameters defined in a service definition file.
        
        <b>Usage with a service parameter:</b> Set the following attribute of the parameter's <i>&lt;param&gt;</i> element:
        <ul>
            <li>the <i>scope</i> attribute must contain the value selection scope (<i>"user"</i>, <i>"group"</i>, <i>"service"</i> or <i>"all"</i>).</li>
        </ul>

        <b>Data storage:</b> The values are defined in the database table <i>serviceconfig</i>, which has references to the tables <i>user</i>, <i>group</i> and <i>service</i>.
        The specified value selection <b>scope</b> determines the content of the value set, i.e. whether  
        <ul>
            <li>the <b>service</b>, and/or</li>
            <li>the current <b>user</b> or the groups he is assigned to</li>
        </ul>
        are used as selection criteria.
        
        This type of value set adds also methods for the maintenance of user-specific values, including creation, modification and deletion of such values.
    */
    public class StoredParameterValueSet : IEditableValueSet {
        protected IfyContext context;
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the service that defines the service parameter.</summary>
        public Service Service { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the parameter name as defined in the service configuration file.</summary>
        public string Name { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the value selection scope as defined in the service configuration file.</summary>
        public ValueSelectScope Scope { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the</summary>
        public bool SingleValueOutput { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets an array of the service parameter values in the selection scope.</summary>
        public virtual string[] GetValues() {
            List<string> values = new List<string>();
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(GetListQuery(false), dbConnection);
            while (reader.Read()) values.Add(reader.GetString(0));
            context.CloseQueryResult(reader, dbConnection);
            return values.ToArray();

            /*List<string> values = new List<string>();
            string cond1 = null, cond2 = null;
            cond1 = "t.name=" + StringUtils.EscapeSql(Name) + " AND (id_service=" + Service.Id + (Scope == ValueSelectScope.All ? " OR id_service IS NULL" : "") + ")";
            if (Scope != ValueSelectScope.UserOnly) cond2 = cond1 + " AND t1.id_usr=" + context.UserId;
            cond1 += " AND (t.id_usr=" + context.UserId + (Scope >= ValueSelectScope.UpToService ? " OR t.id_grp IS NULL AND t.id_usr IS NULL OR t.id_usr=" + context.UserId : "") + ")";
            
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(
                    "SELECT DISTINCT CASE WHEN t.id_usr IS NOT NULL THEN 1 WHEN t.id_service IS NOT NULL THEN 3 ELSE 4 END AS level, t.caption AS caption, t.value AS value FROM serviceconfig AS t WHERE " + cond1 +
                    (cond2 == null ? "" : " UNION SELECT DISTINCT 2 AS level, t.caption AS caption, t.value AS value FROM serviceconfig AS t INNER JOIN usr_grp AS t1 ON t.id_grp=t1.id_grp WHERE " + cond2) +
                    " ORDER BY level, caption;",
                    dbConnection
            );
            while (reader.Read()) values.Add(reader.GetString(1));
            context.CloseQueryResult(reader, dbConnection);
            return values.ToArray();*/
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a StoredParameterValueSet instance based on the characteristics of a service parameter.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="service">the service that defines the parameter</param>
        /// <param name="name">the parameter name</param>
        /// <param name="scope">the values selection scope</param>
        */
        public StoredParameterValueSet(IfyContext context, Service service, string name, ValueSelectScope scope) {
            this.context = context;
            this.Service = service;
            this.Name = name;
            this.Scope = scope;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Generates a string containing explanations for the service parameter values in the selection scope.</summary>
        /*!
        /// <returns>a string containing a list of value/caption pairs</returns>
            \sa For more information, see the documentation of IValueSet::GetExplanation().
        */
        public string GetExplanation() {
            string result = null;
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(GetListQuery(true), dbConnection);
            bool first = true;
            while (reader.Read()) {
                if (first) {
                    result = String.Empty;
                    first = false;
                } else {
                    result += Environment.NewLine;
                }
                result += reader.GetString(0) + "\t" + reader.GetString(1);
            }
            context.CloseQueryResult(reader, dbConnection);
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the specified value is contained in the set of service parameter values in the selection scope.</summary>
        /*!
        /// <param name="value">the value to be checked</param>
        /// <param name="defaultValue">contains the default value of the value set or <i>null</i> if there is no default value (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns><i>true</i> if <i>value</i> is contained in the value set</returns>
            \sa For more information, see the documentation of IValueSet::CheckValue().
        */
        public virtual bool CheckValue(string value, out string defaultValue, out string selectedCaption) {
            bool result = false;
            defaultValue = null;
            selectedCaption = null;

            string listQuery = GetListQuery(false);
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(listQuery, dbConnection);
            
            while (reader.Read()) {
                string elemValue = reader.GetString(0);
                if (elemValue == value) {
                    result = true;
                    selectedCaption = reader.GetString(1);
                }
            }
            context.CloseQueryResult(reader, dbConnection);

            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks for each element in the specified value array whether it is contained in the set of service parameter values in the selection scope.</summary>
        /*!
        /// <param name="values">the array of values to be checked</param>
        /// <param name="defaultValues">contains the default values of the value set or <i>null</i> if there are no default values (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns>an array of the same shape as <i>values</i>, where an element has the value <i>true</i> if the corresponding element in <i>values</i> is contained in the value set</returns>
            \sa For more information, see the documentation of IValueSet::CheckValues().
        */
        public virtual bool[] CheckValues(string[] values, out string[] defaultValues) {
            if (values == null) values = new string[0];
            bool[] result = new bool[values.Length];
            defaultValues = new string[0]; // !!! should contain default values

            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(GetListQuery(false), dbConnection);
            while (reader.Read()) {
                string value = reader.GetString(0);
                for (int i = 0; i < values.Length; i++) {
                    if (values[i] == value) {
                        result[i] = true;
                        break;
                    }
                }
            }
            context.CloseQueryResult(reader, dbConnection);

            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes all service parameter values in the selection scope to the specified XmlWriter object.</summary>
        /*!
        /// <param name="output">the XmlWriter object for the output of the element</param>
        /// <returns>the number of elements that have been written to <i>output</i></returns>
        */
        public virtual int WriteValues(XmlWriter output) {
            if (SingleValueOutput) output.WriteStartElement(Name);
            
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(GetListQuery(true), dbConnection);
            
            int count = 0;
            while (reader.Read()) {
                count++;
                output.WriteStartElement("element");
                output.WriteAttributeString("value", reader.GetString(0));
                output.WriteAttributeString(reader.GetBoolean(1) ? "list" : "scope", reader.GetString(2));
                output.WriteString(reader.GetString(3));
                output.WriteEndElement(); // </element>
            }
            context.CloseQueryResult(reader, dbConnection);

            if (SingleValueOutput) output.WriteEndElement(); // Name
            
            return count;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the service parameter value with the specified caption to the specified XmlWriter object.</summary>
        /*!
        /// <param name="caption">the caption assigned to the value set element (the caption is a user-friendly representation of the value and usually different from it)</param>
        /// <param name="output">the XmlWriter object for the output of the element</param>
        /// <returns><i>true</i> if the element with the caption <i>caption</i> has been written to <i>output</i></returns>
        */
        public virtual bool WriteValue(string caption, XmlWriter output) {
            if (caption == null) context.ReturnError(new ArgumentException("Missing caption"), null);

            bool found = false;
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT caption, value FROM serviceconfig WHERE id_usr={0} AND id_service={1} AND name={2} AND caption={3};", context.UserId, Service.Id, StringUtils.EscapeSql(Name), StringUtils.EscapeSql(caption)), dbConnection);
            if (reader.Read()) {
                output.WriteStartElement("element");
                output.WriteAttributeString("value", reader.GetString(1));
                output.WriteString(reader.GetString(0));
                output.WriteEndElement(); // </element>
                found = true;
            }
            context.CloseQueryResult(reader, dbConnection);
            if (!found) context.ReturnError(new ArgumentException("Parameter value not found"), null);
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new value for the request parameter with the received caption (i.e. a new record in the database).</summary>
        public virtual bool CreateValue(string caption, string value) {
            if (caption == null || value == null) context.ReturnError(new ArgumentException("Missing caption or value"), null);

            if (context.GetQueryIntegerValue("SELECT COUNT(*) FROM serviceconfig WHERE id_usr=" + context.UserId + " AND id_service=" + Service.Id + " AND name=" + StringUtils.EscapeSql(Name) + " AND caption="  + StringUtils.EscapeSql(caption) + ";") == 0) {
                context.Execute("INSERT INTO serviceconfig (id_usr, id_service, name, caption, value) VALUES (" + context.UserId + ", " + Service.Id + ", " + StringUtils.EscapeSql(Name) + ", " + StringUtils.EscapeSql(caption) + ", " + StringUtils.EscapeSql(value) + ");");
            } else {
                return ModifyValue(caption, value);
            }
            return true;
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Modifies the value for the request parameter with the received caption (i.e. an existing record in the database).</summary>
        public virtual bool ModifyValue(string caption, string value) {
            if (caption == null || value == null) context.ReturnError(new ArgumentException("Missing caption or value"), null);

            context.Execute("UPDATE serviceconfig SET value=" + StringUtils.EscapeSql(value) + " WHERE id_usr=" + context.UserId + " AND id_service=" + Service.Id + " AND name=" + StringUtils.EscapeSql(Name) + " AND caption="  + StringUtils.EscapeSql(caption) + ";");
            return true;
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Deletes the value with the specified caption (i.e. the record in the databse).</summary>
        public virtual bool DeleteValue(string caption) {
            if (caption == null) context.ReturnError(new ArgumentException("Missing caption"), null);

            context.Execute("DELETE FROM serviceconfig WHERE id_usr=" + context.UserId + " AND id_service=" + Service.Id + " AND name=" + StringUtils.EscapeSql(Name) + " AND caption="  + StringUtils.EscapeSql(caption) + ";"); 
            return true;
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the query for listing all values contained in the set of predefined or user-defined values.</summary>
        protected virtual string GetListQuery(bool withScope) {
            string condition = "t.name=" + StringUtils.EscapeSql(Name) + " AND (id_service=" + Service.Id + (Scope == ValueSelectScope.All ? " OR id_service IS NULL" : "") + ")";
            condition += " AND (t.id_usr=" + context.UserId + (Scope == ValueSelectScope.UserOnly ? String.Empty : " OR t.id_usr IS NULL" + (Scope == ValueSelectScope.UpToGroup ? " AND t.id_grp IS NOT NULL" : String.Empty)) + ")";
            string result = String.Format(
                    "SELECT DISTINCT t.value AS value{2}, t.caption AS caption FROM serviceconfig AS t{0} WHERE {1} ORDER BY caption;",
                    (Scope == ValueSelectScope.UserOnly ? String.Empty : " LEFT JOIN usr_grp AS t1 ON t.id_grp=t1.id_grp AND t1.id_usr=" + context.UserId),
                    condition,
                    withScope ? ", false AS list, CASE" + (Scope == ValueSelectScope.UserOnly ? String.Empty : " WHEN t1.id_grp IS NOT NULL THEN 'group'") + " WHEN t.id_usr IS NOT NULL THEN 'user' WHEN t.id_service IS NOT NULL THEN 'service' ELSE '*' END AS scope" : String.Empty
            );
            
            return result;
        }
        
    }


    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a value set containing values from a lookup list and service parameter values applicable to a specific scope that are stored in the database.</summary>
    /*!
        This type of value set is used for service parameters defined in a service definition file. The resulting value set is a combination of a LookupValueSet and a StoredParameterValueSet.
        This is useful for parameters in different services that have a common basic value set (these values are defined in the lookup list(s)), where an extension by service-specific additional values or the support for user-specific values is desired.
        
        <b>Usage with a service parameter:</b> Set the following attributes of the parameter's <i>&lt;param&gt;</i> element:
        <ul>
            <li>the <i>list</i> attribute must contain the comma-separated list of lookup list names.</i>
            <li>the <i>scope</i> attribute must contain the value selection scope (<i>"user"</i>, <i>"group"</i>, <i>"service"</i> or <i>"all"</i>).</li>
        </ul>

        <b>Data storage:</b> The values are defined in the database table <i>serviceconfig</i>, which has references to the tables <i>user</i>, <i>group</i> and <i>service</i> and in the database table <i>lookup</i> which has a reference to the table <i>lookuplist</i>.
        The specified value selection <b>scope</b> determines the content of one part of the value set, i.e. whether  
        <ul>
            <li>the <b>service</b>, and/or</li>
            <li>the current <b>user</b> or the groups he is assigned to</li>
        </ul>
        are used as selection criteria.
        The names of the <b>lookup lists</b> determine the content of the other part of the value set.
    */
    public class StoredLookupParameterValueSet : StoredParameterValueSet {
        private string lookupListCondition;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the names of the lookup lists separated by comma.</summary>
        public string ListNames { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a StoredLookupParameterValueSet instance based on the characteristics of a service parameter.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="service">the service that defines the parameter</param>
        /// <param name="name">the parameter name</param>
        /// <param name="scope">the values selection scope</param>
        /// <param name="listNames">the names of the lookup lists separated by comma</param>
        */
        public StoredLookupParameterValueSet(IfyContext context, Service service, string name, ValueSelectScope scope, string listNames) : base(context, service, name, scope) {
            this.ListNames = listNames;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the query for listing all values contained in the set of predefined or user-defined values.</summary>
        protected override string GetListQuery(bool withScope) {
            string condition = "t.name=" + StringUtils.EscapeSql(Name) + " AND (id_service=" + Service.Id + (Scope == ValueSelectScope.All ? " OR id_service IS NULL" : "") + ")";
            //condition += " AND (t.id_usr=" + context.UserId + (Scope >= ValueSelectScope.UpToService ? " OR t.id_grp IS NULL AND t.id_usr IS NULL OR t.id_usr=" + context.UserId : "") + ")";
            condition += " AND (t.id_usr=" + context.UserId + (Scope == ValueSelectScope.UserOnly ? String.Empty : " OR t.id_usr IS NULL" + (Scope == ValueSelectScope.UpToGroup ? " AND t.id_grp IS NOT NULL" : String.Empty)) + ")";
            if (lookupListCondition == null) {
                GetLookupListCondition();
                if (lookupListCondition == null) lookupListCondition = "false";
            }

            string result = String.Format(
                    "SELECT DISTINCT t.value AS value{2}, t.caption AS caption FROM serviceconfig AS t{0} WHERE {1} UNION SELECT t.value AS value{4}, t.caption AS caption FROM lookup AS t INNER JOIN lookuplist AS t1 ON t.id_list=t1.id WHERE {3} ORDER BY caption;",
                    (Scope == ValueSelectScope.UserOnly ? String.Empty : " LEFT JOIN usr_grp AS t1 ON t.id_grp=t1.id_grp AND t1.id_usr=" + context.UserId),
                    condition,
                    withScope ? ", false AS list, CASE" + (Scope == ValueSelectScope.UserOnly ? String.Empty : " WHEN t1.id_grp IS NOT NULL THEN 'group'") + " WHEN t.id_usr IS NOT NULL THEN 'user' WHEN t.id_service IS NOT NULL THEN 'service' ELSE '*' END AS scope" : String.Empty,
                    lookupListCondition,            
                    withScope ? ", true AS list, t1.name" : String.Empty
            );
            
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the SQL conditional expression for retrieving the values from the included lookup list(s).</summary>
        private void GetLookupListCondition() {
            if (ListNames == null) return;
            string[] lists = ListNames.Split(',');
            string listNameCondition = String.Empty;
            for (int i = 0; i < lists.Length; i++) listNameCondition += (i == 0 ? String.Empty : ",") + StringUtils.EscapeSql(lists[i].Trim());
            if (lists.Length == 1) listNameCondition = "name=" + listNameCondition; 
            else listNameCondition = "name IN (" + listNameCondition + ")";
            
            IDbConnection dbConnection = context.GetDbConnection();
            IDataReader reader = context.GetQueryResult(String.Format("SELECT id FROM lookuplist WHERE {0} AND system=false;", listNameCondition), dbConnection);
            int listCount = 0;
            while (reader.Read()) {
                listCount++;
                if (lookupListCondition == null) lookupListCondition = String.Empty; else lookupListCondition += ",";
                lookupListCondition += reader.GetInt32(0);
            }
            context.CloseQueryResult(reader, dbConnection);
            if (lookupListCondition == null) return;
            if (listCount == 1) lookupListCondition = "id_list=" + lookupListCondition;
            else lookupListCondition = "id_list IN (" + lookupListCondition + ")";
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a value set containing service parameter values applicable to a specific scope that are stored text files.</summary>
    /*!
        This type of value set is used for service parameters defined in a service definition file.
        It is useful for service parameters with values that are very large and contain multiple lines, such as configuration files.
        The value set is composed of values that are usable for all users of a service and/or values that are defined by or for specific users.
        These two types of values are stored in text files in different locations. 
        
        <b>Usage with a service parameter:</b> Set the following attributes of the parameter's <i>&lt;param&gt;</i> element:
        <ul>
            <li>the <i>type</i> attribute must contain the value <i>"textfile"</i>,</li>
            <li>the <i>scope</i> attribute must contain the value selection scope (<i>"user"</i> or <i>"service"</i>), and</li>
            <li>at least one of the <i>pattern</i> or <i>userPattern</i> must contain a search pattern for the files to be included.</li>
        </ul>

        <b>Data storage:</b> The values contained in this type of value set are stored in text files on the local file system.
        The specified value selection <b>scope</b> and the presence of <b>search patterns</b> determine the content of one part of the value set, i.e. whether  
        <ul>
            <li>the files defined at the <b>service</b> level, and/or</li>
            <li>the files defined by or for the current <b>user</b></li>
        </ul>
        are included in the value set.
        
        <b>Output differences:</b> Since the values can be very large and have multiple lines, they are not shown in value lists and in the output of a single value, the value is shown as the element content rather than in the <i>value</i> attribute.        
        In value lists, the <i>value</i> attribute of an <i>&lt;element&gt;</i> element contains an internal representation of the name of the file containing the actual value, while the element content is formed by a user-readable caption based on the filename (i.e. the displayed value is similar to or the same as the caption). 

    */
    public class StoredFileParameterValueSet : IEditableValueSet {
        protected IfyContext context;

        private string pattern, path, userPattern, userPath, fullFilename;
        private int fixLengthLeft, fixLengthTotal;
        private string paramRoot;
        
        /// <summary>Gets the service that defines the service parameter.</summary>
        public Service Service { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the parameter name as defined in the service configuration file.</summary>
        public string Name { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the value selection scope as defined in the service configuration file.</summary>
        public ValueSelectScope Scope { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether the</summary>
        public bool SingleValueOutput { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the values contained in the value set as an array or a <i>null</i> reference if the value range is open.</summary>
        /*!
            <b>Note:</b> This property returns a <i>null</i> reference, since service parameters that use predefined text files have by definition an open value range.
        */
        public string[] GetValues() {
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a StoredFileParameterValueSet instance based on the characteristics of a service parameter.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="service">the service that defines the parameter</param>
        /// <param name="name">the parameter name</param>
        /// <param name="scope">the values selection scope</param>
        /// <param name="pattern">the search pattern for the files available to all users</param>
        /// <param name="userPattern">the search pattern for the current user's own files</param>
        */
        public StoredFileParameterValueSet(IfyContext context, Service service, string name, ValueSelectScope scope, string pattern, string userPattern) {
            this.context = context;
            this.Service = service;
            this.Name = name;
            this.Scope = scope;
            
            if (pattern != null) {
                if (pattern.IndexOf("$(PARAM") == 0) paramRoot = context.ServiceParamFileRoot;
                if (paramRoot == null) paramRoot = Service.FileRootDir;
                pattern = pattern.Replace("$(PARAMROOT)", paramRoot).Replace("$(PARAMDIR)", Service.FileRootDir + Path.DirectorySeparatorChar + Name);
                if (pattern.Contains("$(SERVICEROOT)")) pattern = pattern.Replace("$(SERVICEROOT)", Service.FileRootDir);

                pattern = Regex.Replace(pattern, @"[/\\]+", Path.DirectorySeparatorChar.ToString());

                Match match = Regex.Match(pattern, @"^([^\*]+)\" + Path.DirectorySeparatorChar + @"(([^\*\" + Path.DirectorySeparatorChar + @"]*)\*([^\*\" + Path.DirectorySeparatorChar + @"]*))$");
                if (!match.Success) context.ReturnError(new ArgumentException("Invalid file pattern"), null);
    
                this.path = match.Groups[1].Value;
                this.pattern = match.Groups[2].Value;
            }

            if (userPattern != null) {
                if (paramRoot == null && userPattern.IndexOf("$(PARAM") == 0) paramRoot = context.ServiceParamFileRoot;
                if (paramRoot == null) paramRoot = Service.FileRootDir; 
                userPattern = userPattern.Replace("$(PARAMROOT)", paramRoot).Replace("$(PARAMDIR)", Service.FileRootDir + Path.DirectorySeparatorChar + Name);
                if (userPattern.Contains("$(SERVICEROOT)")) userPattern = userPattern.Replace("$(SERVICEROOT)", Service.FileRootDir);

                userPattern = Regex.Replace(userPattern.Replace("$(USER)", context.Username), @"[/\\]+", Path.DirectorySeparatorChar.ToString());

                Match match = Regex.Match(userPattern, @"^([^\*]+)\" + Path.DirectorySeparatorChar + @"(([^\*\" + Path.DirectorySeparatorChar + @"]*)\*([^\*\" + Path.DirectorySeparatorChar + @"]*))$");
                if (!match.Success) context.ReturnError(new ArgumentException("Invalid file pattern"), null);
    
                this.userPath = match.Groups[1].Value;
                this.userPattern = match.Groups[2].Value;
            }

        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Generates a string containing explanations for the service parameter values in the selection scope.</summary>
        /*!
        /// <returns>a string containing a list of value/caption pairs</returns>
            \sa For more information, see the documentation of IValueSet::GetExplanation().
        */
        public string GetExplanation() {
            return null;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the list of text files containing predefined service parameter values to the specified XmlWriter object. </summary>
        /*!
        /// <param name="value">the value to be checked</param>
        /// <param name="defaultValue">contains the default value of the value set or <i>null</i> if there is no default value (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns><i>true</i></returns>

            <b>Note:</b> This method performs no validity check, since service parameters that use predefined text files have by definition an open value range.
        */
        public bool CheckValue(string value, out string defaultValue, out string selectedCaption) {
            defaultValue = null;
            selectedCaption = null;
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the list of text files containing predefined service parameter values to the specified XmlWriter object. </summary>
        /*!
        /// <param name="values">the array of values to be checked</param>
        /// <param name="defaultValues">contains the default values of the value set or <i>null</i> if there are no default values (output parameter)</param>
        /// <param name="count">contains the number of elements in the value set (output parameter)</param>
        /// <param name="output">the XmlWriter object for the output of the value list (if <i>null</i>, the value list is not written)</param>
        /// <returns>an array of the same shape as <i>values</i>, where all elements have the value <i>true</i></returns>

            <b>Note:</b> This method performs no validity check, since service parameters that use predefined text files have by definition an open value range. Furthermore, this method should never be called since this kind of service parameter cannot have multiple values.
        */
        public bool[] CheckValues(string[] values, out string[] defaultValues) {
            if (values == null) values = new string[0];
            bool[] result = new bool[values.Length];
            defaultValues = new string[0];
            for (int i = 0; i < values.Length; i++) result[i] = true;
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes all captions of predefined text files to the specified XmlWriter object.</summary>
        /*!
        /// <param name="output">the XmlWriter object for the output of the element</param>
        /// <returns>the number of elements that have been written to <i>output</i></returns>
        */
        public int WriteValues(XmlWriter output) {
    
            int count = 0;
            
            if (SingleValueOutput) output.WriteStartElement(Name);

            if (context.DebugLevel >= 1) {
                context.WriteDebug(1, "$(PARAMROOT) = " + paramRoot);
                context.WriteDebug(1, "$(PARAMDIR) = " + Service.FileRootDir + Path.DirectorySeparatorChar + Name);
                context.WriteDebug(1, "Search pattern for shared files: " + (pattern == null ? "[not used]" : path + Path.DirectorySeparatorChar + GetPattern(pattern, path.Length)));
                context.WriteDebug(1, "Search pattern for user files: " + (userPattern == null ? "[not used]" : userPath + Path.DirectorySeparatorChar + GetPattern(userPattern, userPath.Length)));
            }
            string[] files;
            string value;
            string level = "user";
            
            if (userPattern != null && Directory.Exists(userPath)) {
                files = Directory.GetFiles(userPath, GetPattern(userPattern, userPath.Length));
                count += files.Length;
                for (int i = 0; i < files.Length; i++) {
                    value = files[i].Substring(fixLengthLeft, files[i].Length - fixLengthTotal);
                    output.WriteStartElement("element");
                    output.WriteAttributeString("value", value + "*");
                    output.WriteAttributeString("scope", level);
                    output.WriteString(value);
                    output.WriteEndElement(); // </element>
                }
                level = "service";
            }

            if (pattern != null && Directory.Exists(path) && Scope >= ValueSelectScope.UpToService) {
                files = Directory.GetFiles(path, GetPattern(pattern, path.Length));
                count += files.Length;
                for (int i = 0; i < files.Length; i++) {
                    value = files[i].Substring(fixLengthLeft, files[i].Length - fixLengthTotal);
                    output.WriteStartElement("element");
                    output.WriteAttributeString("value", value);
                    output.WriteAttributeString("scope", level);
                    output.WriteString(value);
                    output.WriteEndElement(); // </element>
                }
            }

            if (SingleValueOutput) output.WriteEndElement(); // Name
            return count;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the content of the file associated with the specified caption to the specified XmlWriter object.</summary>
        /*!
        /// <param name="caption">the caption assigned to the value set element (the caption is a user-friendly representation of the value based on the filename)</param>
        /// <param name="output">the XmlWriter object for the output of the element</param>
        /// <returns><i>true</i> if the element with the caption <i>caption</i> has been written to <i>output</i></returns>
        */
        public bool WriteValue(string caption, XmlWriter output) {
            if (caption == null) context.ReturnError(new ArgumentException("Missing filename"), null);
            
            GetFullFilename(caption, userPattern != null && caption.EndsWith("*")); 

            try {
                output.WriteStartElement("value");
                output.WriteAttributeString("file", fullFilename);
                StreamReader file = null;
                file = new StreamReader(fullFilename);
                string line;
                bool written = false;
                while ((line = file.ReadLine()) != null) {
                    if (written) output.WriteString("\n");
                    output.WriteString(line);
                    written = true;
                }
                output.WriteEndElement(); // </value>
                file.Close();

            } catch (DirectoryNotFoundException e) {
                context.ReturnError(e, "directoryNotFound");
            } catch (FileNotFoundException e) {
                context.ReturnError(e, "fileNotFound");
            } catch (Exception e) {
                context.ReturnError(new Exception("IO Error: " + e.Message), "ioError");
            }
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes the content of the file associated with the specified caption to the specified StreamWriter object.</summary>
        /*!
        /// <param name="caption">the caption assigned to the value set element (the caption is a user-friendly representation of the value based on the filename)</param>
        /// <param name="output">the StreamWriter object for the output of the element</param>
        /// <returns><i>true</i> if the element with the caption <i>caption</i> has been written to <i>output</i></returns>
        */
        public bool WriteValue(string caption, StreamWriter output) {
            if (caption == null) context.ReturnError(new ArgumentException("Missing filename"), null);
            
            GetFullFilename(caption, userPattern != null && caption.EndsWith("*"));

            try {
                StreamReader file = null;
                file = new StreamReader(fullFilename);
                string line;
                bool written = false;
                while ((line = file.ReadLine()) != null) {
                    if (written) output.WriteLine();
                    output.Write(line);
                    written = true;
                }
                file.Close();

            } catch (DirectoryNotFoundException e) {
                context.ReturnError(e, "directoryNotFound");
            } catch (FileNotFoundException e) {
                context.ReturnError(e, "fileNotFound");
            } catch (Exception e) {
                context.ReturnError(new Exception("IO Error: " + e.Message), "ioError");
            }
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Create a file containing the specified service parameter value and names it according to the specified caption.</summary>
        /*!
            \caption the caption that is associated to the file is translated into the filename
            \value the new value (i.e. file content)
        /// <returns><i>true</i> if the file was modified successfully</returns>
            
            If the file existed before, it is overwritten.
        */
        public bool CreateValue(string caption, string value) {
            if (caption == null || value == null) context.ReturnError(new ArgumentException("Missing filename or content"), null);
            
            if (userPattern == null) context.ReturnError("No user-defined files permitted");

            GetFullFilename(caption, userPattern != null);

            try {
                if (!Directory.Exists(userPath)) Directory.CreateDirectory(userPath);
                StreamWriter file = new StreamWriter(fullFilename);
                file.Write(value);
                file.Close();
            } catch (DirectoryNotFoundException e) {
                context.ReturnError(e, "directoryNotFound");
            } catch (FileNotFoundException e) {
                context.ReturnError(e, "fileNotFound");
            } catch (Exception e) {
                context.ReturnError(new Exception("IO Error: " + e.Message), "ioError");
            }
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Modifies the file with the specified caption changing its content to the specified service parameter value.</summary>
        /*!
            \caption the caption that is associated to the file is translated into the filename
            \value the new value (i.e. file content)
        /// <returns><i>true</i> if the file was modified successfully</returns>
            
            If the file does not exist, it is created.
        */
        public bool ModifyValue(string caption, string value) {
            return CreateValue(caption, value);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Deletes the file with the specified caption.</summary>
        /*!
            \caption the caption that is associated to the file is translated into the filename
        /// <returns><i>true</i> if the file was deleted successfully</returns>
        */
        public bool DeleteValue(string caption) {
            if (caption == null) context.ReturnError(new ArgumentException("Missing filename"), null);

            if (userPattern != null && !caption.EndsWith("*")) context.ReturnError(new UnauthorizedAccessException("You cannot delete this file"), null);

            GetFullFilename(caption, userPattern != null); 

            try {
                fullFilename = fullFilename.Replace("*", String.Empty);
                File.Delete(fullFilename);

            } catch (DirectoryNotFoundException e) {
                context.ReturnError(e, "directoryNotFound");
            } catch (FileNotFoundException e) {
                context.ReturnError(e, "fileNotFound");
            } catch (Exception e) {
                context.ReturnError(new Exception("IO Error: " + e.Message), "ioError");
            }
            return true;
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        private void GetFullFilename(string caption, bool userOwned) {

            // Send error if filename is missing or invalid
            if (fullFilename != null) return;
            
            if (caption == null) context.ReturnError(new ArgumentException("Missing filename"), null);
            else if (!Regex.Match(caption, @"^[ -\.0-9a-z_]{0,50}$", RegexOptions.IgnoreCase).Success) context.ReturnError(new ArgumentException("Invalid filename"), null);
            
            if (userOwned) {
                fullFilename = userPath + Path.DirectorySeparatorChar + userPattern;
                caption = caption.Replace("*", String.Empty);
                
            } else {
                fullFilename = path + Path.DirectorySeparatorChar + pattern;
            }
            if (fullFilename.EndsWith("*")) fullFilename += ".txt";
            
            fullFilename = fullFilename.Replace("*", caption);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the filter pattern for the list of selectable files.</summary>
        /*!
        /// <param name="pattern">the file part of the pattern</param>
        /// <param name="lengthLeft">the length of the path part</param>
        /// <returns>the modified filter pattern</returns>
        */
        private string GetPattern(string pattern, int lengthLeft) {
            string result = pattern;
            fixLengthLeft = lengthLeft + result.IndexOf('*') + 1; 
            if (result.EndsWith("*")) result += ".txt";
            fixLengthTotal = fixLengthLeft + result.Length - result.IndexOf('*') - 1;
            return result;
        }
        
    }


    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public delegate void ProcessValueSetElementCallbackType(string value, string caption, int isDefault);    
}
