using System;
using System.Collections;
using System.Collections.Generic;
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

    

    public interface IInputSource {
        string GetValue(string key);
    }


    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents the execution environment context and manages the client connection, database access and other resources.</summary>
    public class FilterInputSource : IInputSource {
        
        private Dictionary<string, string> parameters;
        
        public string GetValue(string key) {
            if (parameters.ContainsKey(key)) return parameters[key]; 
            return null;
        }
        
        public FilterInputSource(string definition) {
            parameters = new Dictionary<string, string>(); 
            
            if (definition != null) {
                string[] items = definition.Split('\t');
                foreach (string item in items) {
                    int equalPos = item.IndexOf('=');
                    if (equalPos == -1) parameters[item] = null;
                    else parameters[item.Substring(0, equalPos)] = item.Substring(equalPos + 1);
                }
            }
        }

    }

    

}

