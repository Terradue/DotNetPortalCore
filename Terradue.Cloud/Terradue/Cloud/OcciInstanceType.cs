using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Portal;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    //! Represents a Globus computing resource that is accessed through an LGE interface.
	[Serializable]
	[DataContract]
    public class OcciInstanceType : VirtualMachineTemplate {
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public OcciInstanceType(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new OcciInstanceType instance.</summary>
        /// <param name="context">the execution environment context</param>
        /// <returns>the created OcciInstanceType object</returns>
        public static new OcciInstanceType GetInstance(IfyContext context) {
            return new OcciInstanceType(context);
        }

		public OcciInstanceType (IfyContext context, string name) : base (context)
		{
			this.Name = name;
		}
        
        //---------------------------------------------------------------------------------------------------------------------

        public static OcciInstanceType FromRemoteId(IfyContext context, CloudProvider provider, string remoteId) {
            OcciInstanceType result = new OcciInstanceType(context);
            result.Provider = provider;
            result.RemoteId = remoteId;
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static OcciInstanceType FromListXml(IfyContext context, CloudProvider provider, XmlElement elem) {
            OcciInstanceType result = new OcciInstanceType(context);
            result.Provider = provider;
            result.RemoteId = (elem.HasAttribute("href") ? elem.Attributes["href"].Value : null);
            if (result.RemoteId != null) result.RemoteId = Regex.Replace(result.RemoteId, "^.*/", String.Empty);
            result.Name = (elem.HasAttribute("name") ? elem.Attributes["name"].Value : null);
            if (result.Name == null) result.Name = elem.InnerXml;
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static OcciInstanceType FromItemXml(IfyContext context, CloudProvider provider, XmlElement elem) {
            OcciInstanceType result = new OcciInstanceType(context);
            result.Provider = provider;
            result.RemoteId = (elem.HasAttribute("href") ? elem.Attributes["href"].Value : null);
            if (result.RemoteId != null) result.RemoteId = Regex.Replace(result.RemoteId, "^.*/", String.Empty);
            result.Name = (elem.HasAttribute("name") ? elem.Attributes["name"].Value : null);
            XmlNode node;
            if ((node = elem.SelectSingleNode("CLASS")) != null) result.Class = node.InnerXml;   
            return result;
        }
    }


}

