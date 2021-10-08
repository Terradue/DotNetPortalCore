using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Portal;




//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Runtime.Serialization;
using Mono.Unix;





namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    //! Represents a Globus computing resource that is accessed through an LGE interface.
	[Serializable]
	[DataContract]
	
    [EntityTable("cloudcr", EntityTableConfiguration.Custom)]
    public abstract class CloudComputingResource : ComputingResource {
        
        private CloudAppliance appliance;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets (or sets) the cloud appliance hosting the computing resource.</summary>
		[IgnoreDataMember]
		[EntityDataField("id_cloud", IsForeignKey = true)]
		public int ApplianceId { get; protected set; }

		[DataMember]
		public abstract List<CloudComputingService> Services { get; protected set; }

		[DataMember]
		public abstract List<CloudComputingDriveInfo> DrivesInfo { get; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets (or sets) the cloud appliance hosting the computing resource.</summary>
		[IgnoreDataMember]
        public CloudAppliance Appliance {
            get {
                if (appliance == null && ApplianceId != 0) {
                    appliance = CloudAppliance.FromId(context, ApplianceId) as CloudAppliance; // !!! TODO change this
                }
                return appliance;
            }
            protected set { 
                appliance = value;
                ApplianceId = (value == null ? 0 : value.Id);
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        //! Creates a new ComputingResource instance.
        /*!
            \param context the execution environment context
        */
        public CloudComputingResource(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

    }

	public static class CloudComputingExtensions
	{
		public static UnixDriveInfo GetDriveInfo(this UnixFileInfo fileInfo)
		{
			Mono.Unix.UnixDirectoryInfo file = new Mono.Unix.UnixDirectoryInfo (fileInfo.FullName);
			Mono.Unix.UnixDriveInfo drive = new Mono.Unix.UnixDriveInfo (fileInfo.FullName);
			return drive;
			
		}
	}

}

