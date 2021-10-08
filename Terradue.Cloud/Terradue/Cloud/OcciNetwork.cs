using System;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Portal;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Runtime.Serialization;





namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    //! Represents a Globus computing resource that is accessed through an LGE interface.
	[Serializable]
	[DataContract]
    public class OcciNetwork : VirtualNetwork {
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public OcciNetwork(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new OcciNetwork instance.</summary>
        /// <param name="context">the execution environment context</param>
        /// <returns>the created OcciNetwork object</returns>
        public static new OcciNetwork GetInstance(IfyContext context) {
            return new OcciNetwork(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static OcciNetwork FromRemoteId(IfyContext context, CloudProvider provider, string remoteId) {
            OcciNetwork result = new OcciNetwork(context);
            result.Name = provider.GetNetwork(remoteId).Name;
            result.Provider = provider;
            result.RemoteId = remoteId;
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static OcciNetwork FromListXml(IfyContext context, CloudProvider provider, XmlElement elem) {
            OcciNetwork result = new OcciNetwork(context);
            result.Provider = provider;
            result.RemoteId = (elem.HasAttribute("href") ? elem.Attributes["href"].Value : null);
            if (result.RemoteId != null) result.RemoteId = Regex.Replace(result.RemoteId, "^.*/", String.Empty);
            result.Name = (elem.HasAttribute("name") ? elem.Attributes["name"].Value : null);
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static OcciNetwork FromItemXml(IfyContext context, CloudProvider provider, XmlElement elem) {
            OcciNetwork result = new OcciNetwork(context);
            result.Provider = provider;
            result.RemoteId = (elem.HasAttribute("href") ? elem.Attributes["href"].Value : null);
            if (result.RemoteId != null) result.RemoteId = Regex.Replace(result.RemoteId, "^.*/", String.Empty);
            XmlNode subElem = elem.SelectSingleNode("NAME");
            result.Name = (subElem == null ? result.RemoteId : subElem.InnerXml);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static OcciNetwork FromComputeXml(IfyContext context, CloudAppliance appliance, XmlElement elem) {
            OcciNetwork result = new OcciNetwork(context);
            result.Appliance = appliance;
            foreach (XmlNode node in elem) { 
                XmlElement subElem = node as XmlElement;
                if (subElem == null) continue;
                switch (subElem.Name) {
                    case "NETWORK" :
                        if (subElem.HasAttribute("href")) result.RemoteId = subElem.Attributes["href"].Value;
                        if (subElem.HasAttribute("name")) result.Name = subElem.Attributes["name"].Value;
                        break;
                    case "IP" :
                        result.IpAddress = subElem.InnerXml;
                        break;
                    case "MAC" :
                        result.MacAddress = subElem.InnerXml;
                        break;
                }
            }
            
            return result;
        }

    }

}

