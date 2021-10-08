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
    public class OcciStorage : VirtualDisk {
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public OcciStorage(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new OcciStorage instance.</summary>
        /// <param name="context">the execution environment context</param>
        /// <returns>the created OcciStorage object</returns>
        public static new OcciStorage GetInstance(IfyContext context) {
            return new OcciStorage(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static OcciStorage FromRemoteId(IfyContext context, CloudProvider provider, string remoteId) {
            OcciStorage result = new OcciStorage(context);
            result.Provider = provider;
            result.RemoteId = remoteId;
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static OcciStorage FromListXml(IfyContext context, CloudProvider provider, XmlElement elem) {
            OcciStorage result = new OcciStorage(context);
            result.Provider = provider;
            result.RemoteId = (elem.HasAttribute("href") ? elem.Attributes["href"].Value : null);
            if (result.RemoteId != null) result.RemoteId = Regex.Replace(result.RemoteId, "^.*/", String.Empty);
            result.Name = (elem.HasAttribute("name") ? elem.Attributes["name"].Value : null);
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static void FromItemXml(OcciStorage storage, CloudProvider provider, XmlElement elem) {
            storage.RemoteId = (elem.HasAttribute("href") ? elem.Attributes["href"].Value : null);
            if (storage.RemoteId != null) storage.RemoteId = Regex.Replace(storage.RemoteId, "^.*/", String.Empty);
            foreach (XmlNode node in elem) { 
                XmlElement subElem = node as XmlElement;
                if (subElem == null) continue;
                switch (subElem.Name) {
                    case "NAME" :
                        storage.Name = subElem.InnerXml;
                        break;
                    case "TYPE" :
                        storage.Type = subElem.InnerXml;
                        break;
                    case "CLASS" :
                        storage.Class = subElem.InnerXml;
                        break;
                    
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static OcciStorage FromComputeXml (IfyContext context, CloudAppliance appliance, XmlElement elem)
		{
			OcciStorage result = new OcciStorage (context);
			result.Appliance = appliance;
			result.Id = int.Parse (elem.Attributes ["id"].Value);
			foreach (XmlNode node in elem) { 
				XmlElement subElem = node as XmlElement;
				if (subElem == null)
					continue;
				switch (subElem.Name) {
				case "STORAGE":
					if (subElem.HasAttribute ("href"))
						result.RemoteId = Regex.Replace (subElem.Attributes ["href"].Value, "^.*/", String.Empty);
					if (subElem.HasAttribute ("name"))
						result.Name = subElem.Attributes ["name"].Value;
					break;
				case "TYPE":
					result.Type = subElem.InnerXml;
					break;
				case "TARGET":
					result.Target = subElem.InnerXml;
					break;
				case "SAVE_AS":
					if (subElem.HasAttribute ("href"))
						result.SavedAs = Regex.Replace (subElem.Attributes ["href"].Value, "^.*/", String.Empty);
					break;
				}
			}


            
            return result;
        }
        
    }


}

