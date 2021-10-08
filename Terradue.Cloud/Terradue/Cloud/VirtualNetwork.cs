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
using System.Net;





namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a virtual network defined on a cloud provider.</summary>
    /// <remarks>
    ///     <p>An instance of this class may represent the virtual network in general or the specific network configuration of a virtual machine. The latter is the case if the property Appliance is set.</p>
    /// </remarks>
    [Serializable]
    [DataContract]
    public class VirtualNetwork : VirtualResource {
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets or sets the MAC address for a cloud appliance.</summary>
        [DataMember]
        public string MacAddress { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets or sets the internal IP address for a cloud appliance.</summary>
        [DataMember]
        public string IpAddress { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets or sets the external IP address for a cloud appliance.</summary>
        [DataMember]
        public string ExternalIpAddress { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public VirtualNetwork(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new VirtualNetwork instance.</summary>
        /// <param name="context">the execution environment context</param>
        /// <returns>the created VirtualNetwork object</returns>
        public static new VirtualNetwork GetInstance(IfyContext context) {
            return new VirtualNetwork(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public static VirtualNetwork FromXmlElement(IfyContext context, CloudProvider provider, XmlElement elem) {
            VirtualNetwork result = new VirtualNetwork(context);
            result.Provider = provider;
            result.RemoteId = (elem.HasAttribute("href") ? elem.Attributes["href"].Value : null);
            if (result.RemoteId != null) result.RemoteId = Regex.Replace(result.RemoteId, "^.*/", String.Empty);
            result.Name = (elem.HasAttribute("name") ? elem.Attributes["name"].Value : null);
            return result;
        }

		public bool IsReachable () {

			if ( this.IpAddress == null ) return false;

			return false;

		}

		public string FQDN {
			get {
				if (this.IpAddress == null)
					return null;
				try{
					IPAddress addr = IPAddress.Parse(this.IpAddress);
					IPHostEntry entry = Dns.GetHostEntry(addr);
					return entry.HostName;
				}catch(Exception e){
					if (this.IpAddress != null) return this.IpAddress;
					throw e;
				}
			}
		}
        
    }

}

