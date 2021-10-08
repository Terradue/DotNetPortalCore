using System;
using System.Data;
using System.Xml;
using Terradue.Portal;
using System.Runtime.Serialization;
using System.Net;
using System.Collections.Generic;

/*!

\defgroup CloudAppliance Cloud Applicance
@{

The component represents an abstract Cloud Appliance available as a computing resources.
Practically, a class that implements \ref Terradue.Cloud#CloudAppliance represents a virtual machine
running on a \ref Terradue.Cloud#CloudProvider

\ingroup Cloud

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence stores/loads persistently the appliance in the database

\xrefitem dep "Dependencies" "Dependencies" \ref CloudProvider controls the appliance

\xrefitem dep "Dependencies" "Dependencies" \ref Authorisation controls the access on the appliance


@}
*/

namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    /// \ingroup CloudApplicance
    /// <summary>Cloud Appliance</summary>
    /// <description>
    /// A Cloud Appliance represents any virtual machine running on a cloud infrastructure. It is a generic object intended to be
    /// extended to implement a specific architecture.
    /// </description>
	[Serializable]
	[DataContract]
    [EntityTable("cloud", EntityTableConfiguration.Custom, HasOwnerReference = true, HasExtensions = true, NameField = "caption")]
    public abstract class CloudAppliance : Entity {
        
		private string hostname;
        private CloudProvider provider;
        
        //---------------------------------------------------------------------------------------------------------------------
        
		[IgnoreDataMember]
		[EntityDataField("id_cloudprov", IsForeignKey = true)]
        public int ProviderId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Provider of the cloud appliance</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        /// \return is provisioned by \ref Terradue.Cloud#CloudProvider that controls the appliance
		[IgnoreDataMember]
        public CloudProvider Provider {
            get {
                if (provider == null && ProviderId != 0) provider = CloudProvider.FromId(context, ProviderId);
                return provider;
            }
            protected set {
                provider = value;
                ProviderId = (value == null ? 0 : value.Id);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Remote identifier that identifies the applicance on the \ref CloudProvider</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
		[DataMember]
		[EntityDataField("remote_id")]
        public string RemoteId { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

		[DataMember]
        [Obsolete("Obsolete, please use Name instead.")]
        public string Caption {
            get { return Name; }
            set { Name = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

		[IgnoreDataMember]
		[EntityDataField("description")]
        public string Description { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>hostname of the cloud appliance to identify it on the network</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
		[DataMember]
		[EntityDataField("hostname")]
        public virtual string Hostname { 
			get {
				try{
					string fqdn = this.VirtualNetwork.FQDN;
					if (fqdn == null) {
						if (hostname == null) return "localhost";
						else return hostname;
					}
					return fqdn;
				}catch(Exception e){
					return "localhost";
				}
			}
			set { hostname = value; }
		}

        //---------------------------------------------------------------------------------------------------------------------

		[DataMember]
        public string Username { get; set; }

        /// <summary>Owner of the cloud appliance</summary>.
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
		[DataMember]
		public string Owner { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        [DataMember]
        public string StatusText { get; protected set; }

		[DataMember]
		public string Message {
			get;
			set;
		}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>The template the appliance has been instantiated from.</summary>
        /// \return is specified by \ref Terradue.Cloud#VirtualMachineTemplate that describe the appliance specifications (CPU, memory, network...)
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
		[DataMember]
        public abstract VirtualMachineTemplate VirtualMachineTemplate { get; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
		[DataMember]
        public abstract VirtualDisk[] VirtualDisks { get; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
		[DataMember]
        public abstract VirtualNetwork VirtualNetwork { get; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary></summary>Architecture of the instance.</summary>
        public ProcessorArchitecture Architecture { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Number of CPU cores assigned to the instance.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public int Cores { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>CPU clock frequency (speed) in GHz.</summary>
		public float Speed { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Maximum RAM allocated to the instance in gigabytes.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
		public float Memory { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// Current state of the cloud appliance (stopped, started, paused...)
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual MachineState State { get; protected set; }


		//---------------------------------------------------------------------------------------------------------------------
        
        public CloudAppliance(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns an instance of a CloudAppliance subclass representing the cloud appliance with the specified ID.</summary>
        /// <param name="context">the execution environment context</param>
        /// <param name="id">the cloud appliance ID</param>
        /// <returns>the created Terradue.Cloud#CloudAppliance subclass instance</returns>
        public static CloudAppliance FromId(IfyContext context, int id) {
            EntityType entityType = EntityType.GetEntityType(typeof(CloudAppliance));
            CloudAppliance result = (CloudAppliance)entityType.GetEntityInstanceFromId(context, id); 
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public abstract bool Create();
        
        //---------------------------------------------------------------------------------------------------------------------

        /// Start the cloud appliance
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public abstract bool Start();
        
        //---------------------------------------------------------------------------------------------------------------------

        /// Stop the cloud appliance
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public abstract bool Stop(MachineStopMethod method);
        
        //---------------------------------------------------------------------------------------------------------------------

        /// Susspend the cloud appliance
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public abstract bool Suspend(MachineSuspendMethod method);
        
        //---------------------------------------------------------------------------------------------------------------------

        /// Resume the cloud appliance
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public abstract bool Resume(MachineRestartMethod method);
        
        //---------------------------------------------------------------------------------------------------------------------

        /// Shutdown the cloud appliance
        public abstract bool Shutdown(MachineStopMethod method);

        //---------------------------------------------------------------------------------------------------------------------
        
        public abstract void GetStatus(XmlDocument xmlDocument);
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public abstract bool SaveDiskAs(int id, string caption); 
        
        //---------------------------------------------------------------------------------------------------------------------


    }


    /// <summary>Empty implementation of CloudProvider.</summary>
    /// <remarks>
    ///  <p>This class is only used when a generic instance derived from the abstract CloudProvider class is needed in order to combine different types of cloud providers (e.g. for producing an item list).</p>
    ///  <p>It provides only the functionality inherited from the superclasses of CloudProvider (e.g. Entity) but no functionality of a real cloud provider.</p>
    /// </remarks>
    public class GenericCloudAppliance : CloudAppliance {
        //---------------------------------------------------------------------------------------------------------------------
        
        public override VirtualMachineTemplate VirtualMachineTemplate {
            get { return null; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override VirtualDisk[] VirtualDisks {
            get { return null; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override VirtualNetwork VirtualNetwork {
            get { return null; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public GenericCloudAppliance(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Create() {
            return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Start() {
            return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Stop(MachineStopMethod method) {
            return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Suspend(MachineSuspendMethod method) {
            return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Resume(MachineRestartMethod method) {
            return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override bool Shutdown(MachineStopMethod method) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override void GetStatus(XmlDocument xmlDocument) {}
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool SaveDiskAs(int id, string caption) { 
            return false;
        }
        
    }
        

}

