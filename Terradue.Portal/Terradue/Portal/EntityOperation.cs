using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MySql.Data.MySqlClient;
using Terradue.Metadata.OpenSearch;
using Terradue.Util;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {

    
    public delegate void EntityOperationMethodType();
    
    
    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    [Obsolete("This class will be removed (operations on entities are done by method calls, usually from the web server code)")]
    public class EntityOperation {
        
        //---------------------------------------------------------------------------------------------------------------------

        public OperationType Type { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string Identifier { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string Name { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool MultipleItems { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public string HttpMethod { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public string Url { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public EntityOperationMethodType OperationMethod { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public EntityOperation Parent { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public EntityOperation(IfyContext context, string identifier, string httpMethod, EntityOperationMethodType operationMethod) : this(context, OperationType.Other, identifier, identifier, false, httpMethod, null) {}
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public EntityOperation(IfyContext context, OperationType type, string identifier, string name, bool multipleItems, string httpMethod, string url) {
            this.Type = type;
            this.Identifier = identifier;
            this.Name = name;
            this.MultipleItems = multipleItems;
            this.HttpMethod = httpMethod;
            this.Url = url;
            //this.OperationMethod = operationMethod;
        }

    }

}
