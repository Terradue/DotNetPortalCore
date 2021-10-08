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
    public class VirtualDisk : VirtualResource {

		//---------------------------------------------------------------------------------------------------------------------
		[DataMember]
		public string Type { get; set; }
		
		//---------------------------------------------------------------------------------------------------------------------
		[DataMember]
		public string Target { get; set; }

		//---------------------------------------------------------------------------------------------------------------------
		[DataMember]
		public string SavedAs { get; set; }

		//---------------------------------------------------------------------------------------------------------------------
		[DataMember]
		public RestAction SaveOp { get; set; }
        
        public VirtualDisk(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new VirtualDisk instance.</summary>
        /// <param name="context">the execution environment context</param>
        /// <returns>the created VirtualDisk object</returns>
        public static new VirtualDisk GetInstance(IfyContext context) {
            return new VirtualDisk(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public static VirtualDisk FromXmlElement(IfyContext context, CloudProvider provider, XmlElement elem) {
            VirtualDisk result = new VirtualDisk(context);
            result.Provider = provider;
            result.RemoteId = (elem.HasAttribute("href") ? elem.Attributes["href"].Value : null);
            if (result.RemoteId != null) result.RemoteId = Regex.Replace(result.RemoteId, "^.*/", String.Empty);
            result.Name = (elem.HasAttribute("name") ? elem.Attributes["name"].Value : null);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

    }


}

