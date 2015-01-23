using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

    

    /// <summary>Represents a collection of task or job parameters.</summary>
    /// \xrefitem uml "UML" "UML Diagram"
    public class ExecutionParameterSet : IEnumerable<ExecutionParameter> {
        private Dictionary<string, ExecutionParameter> dict = new Dictionary<string, ExecutionParameter>();
        private ExecutionParameter[] items = new ExecutionParameter[0];

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the number of task or job parameters in the collection.</summary>
        public int Count {
            get { return items.Length; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the task or job parameter with the specified name.</summary>
        public ExecutionParameter this[string name] { 
            get { if (dict.ContainsKey(name)) return dict[name]; else return ExecutionParameter.Empty; } 
            set { if (dict.ContainsKey(name)) dict[name].Value = value.Value; else Add(name, value.Value); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the task or job parameter at the specified position in the list.</summary>
        public ExecutionParameter this[int index] { 
            get { return items[index]; } 
            set { items[index].Value = value.Value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool Contains(string name) {
            return dict.ContainsKey(name);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a task or job parameter to the collection</summary>
        /// <param name="name">the parameter name</param>
        /// <param name="name">the parameter value</param>
        /// \xrefitem uml "UML" "UML Diagram"
        public void Add(string name, object value) {
            Add(name, null, value, false);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a task or job parameter to the collection</summary>
        /// <param name="name">the parameter name</param>
        /// <param name="type">the parameter type</param>
        /// <param name="name">the parameter value</param>
        /// <param name="metadata">determines whether the job parameter may still be modified using the job parameter modification interface. </param>
        public void Add(string name, string type, object value, bool metadata) {
            string valueStr = null;

            if (type == null && value != null) {
                Type actualType = (value.GetType().IsArray ? value.GetType().GetElementType() : value.GetType());
                if (actualType == typeof(bool)) type = "bool";
                else if (actualType == typeof(int) || actualType == typeof(short)) type = "int";
                else if (actualType == typeof(double) || actualType == typeof(float)) type = "float";
                else if (actualType == typeof(DateTime)) type = "datetime";
            }
            if (type == null) type = "string";

            ExecutionParameter taskParameter;
            if (value != null && value.GetType().IsArray && value is object[]) {
                object[] valueArray = value as object[];
                string[] values = new string[valueArray.Length];
                for (int i = 0; i < valueArray.Length; i++) values[i] = valueArray[i].ToString();
                taskParameter = new ExecutionParameter(name, type, valueStr, metadata);
            } else {
                if (value is String || value is Int32 || value is Double) valueStr = value.ToString();
                else if (value is Boolean) valueStr = value.ToString().ToLower();
                else if (value is DateTime) {
                    DateTime dt;
                    if (DateTime.TryParse(value.ToString(), out dt)) valueStr = dt.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss");
                } else if (value != null) {
                    valueStr = value.ToString();
                }
                taskParameter = new ExecutionParameter(name, type, valueStr, metadata);
            }
            
            dict.Add(name, taskParameter);
            Array.Resize(ref items, items.Length + 1);
            items[items.Length - 1] = taskParameter;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Adds a task or job parameter needed only for the task or job submission to the collection.</summary>
        /*!
        /// <param name="name">the parameter name</param>
        /// <param name="name">the parameter value</param>
        */
        public void AddSubmissionParameter(string name, string value) {
            ExecutionParameter taskParameter = new ExecutionParameter(name, null, value, false);
            taskParameter.Submission = true;
            if (dict.ContainsKey(name)) {
                //context.AddWarning("The parameter \"" + name + "\" is reserved for the Grid Engine and cannot be used as a job parameter");
                dict[name] = taskParameter;
            } else {
                dict.Add(name, taskParameter);
                Array.Resize(ref items, items.Length + 1);
                items[items.Length - 1] = taskParameter;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string[] ToArray() {
            string[] result = new string[items.Length];
            for (int i = 0; i < items.Length; i++) result[i] = items[i].Name + "=" + items[i].Value;
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        IEnumerator<ExecutionParameter> System.Collections.Generic.IEnumerable<ExecutionParameter>.GetEnumerator() {
            return dict.Values.GetEnumerator();
        }

        //---------------------------------------------------------------------------------------------------------------------

        IEnumerator IEnumerable.GetEnumerator() {
            return dict.Values.GetEnumerator();
        }


    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    
    /// <summary>Represents a task or job parameter.</summary>
    /// \xrefitem uml "UML" "UML Diagram"
    public class ExecutionParameter {
        private string name;
        private string type = "string";
        private string value;
        private string[] values;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Empty task or job parameter.</summary>
        public static ExecutionParameter Empty = new ExecutionParameter(null, null, null as string, false);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the name of the task or job parameter.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public string Name {
            get { return name; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the type identifier of the task or job parameter.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public string Type {
            get { return type; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the value of the task or job parameter.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public string Value {
            get { return value; }
            set { this.value = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the value of the task or job parameter.</summary>
        public string[] Values {
            get { return values; }
            protected set { this.values = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the flag indicating whether the job parameter may still be modified using the job parameter modification interface (only for job parameters).</summary>
        public bool Metadata { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the flag indicating whether the task or job parameter is only needed for the submission to the Grid engine.</summary>
        public bool Submission { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a TaskParameter instance.</summary>
        /*!
        /// <param name="name">the name of the parameter</param>
        /// <param name="type">the type identifier of the parameter</param>
        /// <param name="value">the value of the parameter</param>
        /// <param name="metadata">indicates whether the job parameter may still be modified using the job parameter modification interface (only for job parameters)</param>
        */
        public ExecutionParameter(string name, string type, string value, bool metadata) {
            this.name = name;
            this.type = type;
            this.value = value;
            this.Metadata = metadata;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a TaskParameter instance.</summary>
        /*!
        /// <param name="name">the name of the parameter</param>
        /// <param name="type">the type identifier of the parameter</param>
        /// <param name="value">the value of the parameter</param>
        /// <param name="metadata">indicates whether the job parameter may still be modified using the job parameter modification interface (only for job parameters)</param>
        */
        public ExecutionParameter(string name, string type, string[] values, bool metadata) {
            this.name = name;
            this.type = type;
            if (values != null) {
                foreach (string part in values) {
                    if (value == null) value = String.Empty; else value += "\t";
                    value += part;
                }
            }
            this.values = values;
            this.Metadata = metadata;
        }

    }

}

