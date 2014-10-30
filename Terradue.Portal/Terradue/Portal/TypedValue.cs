using System;
using System.IO;
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


    
    /// <summary>Holds a value that can be accessed by typed properties.</summary>
    public class TypedValue {

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the type identifier for the typed value.</summary>
        /*!
            Known type identifiers are:
            <ul>
                <li><b>string</b>: string values (most generic type)</li>
                <li><b>bool</b>: Boolean values: "true", "yes" and "on" are interpreted as <i>true</i></li>
                <li><b>int</b>: integer numbers</li>
                <li><b>float</b>: real (floating-point) numbers</li>
                <li><b>date</b>: date values</li>
                <li><b>datetime</b>, <b>startdate</b>, <b>enddate</b>: date and time values</li>
                <li><b>textfile</b>: text file content (to be used with a scope)</li>
            </ul>
        */
        public virtual string Type { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the raw value of the request parameter as received by the web server or from the database. </summary>
        public virtual string Value { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the caption representing the value or field. </summary>
        public virtual string Caption { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Empty instance of a typed value.</summary>
        public static TypedValue Empty = new TypedValue(null);
        
        //---------------------------------------------------------------------------------------------------------------------

        // Default constructor.
        protected TypedValue() {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a TypedValue instance for the specified value.</summary>
        public TypedValue(string value) {
            this.Value = value;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the value as string.</summary>
        public override string ToString() {
            bool valid;
            switch (Type) {
                case "bool" :
                    if (Value == null) return "false";
                    string s = Value.ToLower();
                    return (s == "true" || s == "yes" || s == "on" ? "true" : "false");
                case "int" :
                    if (Value == null) return String.Empty;
                    int ip;
                    valid = Int32.TryParse(Value, out ip);
                    return (valid ? ip.ToString() : String.Empty);
                case "float" :
                    if (Value == null) return String.Empty;
                    double dp;
                    valid = Double.TryParse(Value, out dp);
                    return (valid ? dp.ToString() : String.Empty);
                case "date" :
                case "datetime" :
                case "startdate" :
                case "enddate" :
                    if (Value == null) return String.Empty;
                    DateTime dtp;
                    valid = DateTime.TryParse(Value, out dtp);
                    return (valid ? dtp.ToString(Type == "date" ? @"yyyy\-MM\-dd" : @"yyyy\-MM\-dd\THH\:mm\:ss") : String.Empty);
                case "password" : 
                    return String.Empty;
                default :
                    //if (Value == null) return String.Empty;
                    return Value;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the value as string.</summary>
        public string AsString {
            get { return Value; }
            set { Value = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the value as Boolean.</summary>
        /*!
            "true", "yes" and "on" are the values that result in <i>true</i>.
        */
        public bool AsBoolean {
            get { 
                if (Value == null) return false;
                string s = Value.ToLower(); 
                return (s == "true" || s == "yes" || s == "on");
            }
            set {
                Value = value.ToString().ToLower();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the value as integer number.</summary>
        public int AsInteger {
            get { 
                int result = 0;
                if (Value != null) Int32.TryParse(Value, out result);
                return result;
            }
            set {
                Value = value.ToString();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the value as real number.</summary>
        public double AsDouble {
            get {
                double result = 0;
                if (Value != null) Double.TryParse(Value, out result);
                return result;
            }
            set {
                Value = value.ToString();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the value as date and time.</summary>
        public DateTime AsDateTime {
            get {
                DateTime result = DateTime.MinValue;
                if (Value != null) DateTime.TryParse(Value, out result);
                return result;
            }
            set {
                Value = value.ToString(Type == "date" ? @"yyyy\-MM\-dd" : @"yyyy\-MM\-dd\THH\:mm\:ssZ");
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether a value respects its type format and transforms it to a value usable in SQL statements.</summary>
        /*!
        /// <param name="value">contains the resulting SQL equivalent value in simple quotes for non-numeric and non-boolean types</param>
        /// <returns>true if the value is valid</returns>
        */
        public string ToSqlString() {
            string newValue = null;
            bool valid;
            
            if (Value == null) return "NULL";
        
            switch (Type) {
                case "bool" :
                    if (Value == null) return "false";
                    newValue = Value.ToLower();
                    valid = (newValue == "true" || newValue == "false" || newValue == "on" || newValue == "yes" || newValue == "no");
                    if (valid) newValue = (newValue == "true" || newValue == "yes" || newValue == "on" ? "true" : "false");
                    else newValue = null;
                    break;
                case "int" :
                    if (Value == null) return null;
                    int ip;
                    valid = Int32.TryParse(Value, out ip);
                    if (valid) newValue = ip.ToString();
                    break;
                case "float" :
                    if (Value == null) return null;
                    double dp;
                    valid = Double.TryParse(Value, out dp);
                    if (valid) newValue = dp.ToString();
                    break;
                case "date" :
                case "datetime" :
                case "startdate" :
                case "enddate" :
                    if (Value == null) return null;
                    DateTime dtp;
                    valid = DateTime.TryParse(Value, out dtp);
                    if (valid) newValue = "'" + dtp.ToString(Type == "date" ? @"yyyy\-MM\-dd" : @"yyyy\-MM\-dd\THH\:mm\:ss") + "'";
                    break;
                default :
                    newValue = (Value == null ? "NULL" : "'" + Value.Replace("'", "''").Replace(@"\", @"\\") + "'");
                    break;
            }
            return newValue;
        }

    }

}

