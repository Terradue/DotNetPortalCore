using System;
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
    public abstract class VirtualResource : VirtualEntity {

        private CloudAppliance appliance;
        
        private int id;
        
		[DataMember]
		public new int Id {
		    get {
		        return id;
		    }
		    protected set {
		        id = value;
		        base.Id = value;
		    }
		        
		}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the Architecture of the instance.</summary>
		[IgnoreDataMember]
        public CloudProvider Provider { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
		[IgnoreDataMember]
        public CloudAppliance Appliance {
            get {
                return appliance;
            }
            protected set {
                appliance = value;
                if (appliance != null) Provider = appliance.Provider;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        [DataMember]
        [Obsolete("Obsolete, please use Name instead.")]
        public string Caption { 
            get { return Name; }
            set { Name = value; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
		[DataMember]
        public string Description { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets (or sets) the reference string that describes the resource on the cloud provider.</summary>
		[DataMember]
        public string RemoteId { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
		[DataMember]
        public string Class { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public VirtualResource(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------
    }


}

