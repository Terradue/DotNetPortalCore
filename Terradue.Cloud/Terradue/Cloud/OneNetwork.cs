using System;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Portal;
using System.Runtime.Serialization;
using Terradue.OpenNebula;




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
    public class OneNetwork : VirtualNetwork {
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public OneNetwork(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public OneNetwork(IfyContext context, VNET xmlrpcNet, CloudProvider provider) : base(context) {
            this.Name = xmlrpcNet.NAME;
            this.RemoteId = xmlrpcNet.ID;
            this.Provider = provider;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new XmlRpcNetwork instance.</summary>
        /// <param name="context">the execution environment context</param>
        /// <returns>the created XmlRpcNetwork object</returns>
        public static new OneNetwork GetInstance(IfyContext context) {
            return new OneNetwork(context);
        }

    }

}

