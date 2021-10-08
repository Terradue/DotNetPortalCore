using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
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

/*!

\defgroup OneCloudAppliance OpenNebula Cloud Applicance
@{

The component represents a Cloud Appliance on \ref OneCloudProvider by extending the \ref CloudAppliance module

\ingroup Cloud

\xrefitem dep "Dependencies" "Dependencies" \ref OneCloudProvider controls the appliance on OpenNebula platform

\xrefitem dep "Dependencies" "Dependencies" implements \ref CloudAppliance for OpenNebula appliance

\xrefitem dep "Dependencies" "Dependencies" calls \ref OneClient to performs the appliance operations on OpenNebula


@}
*/




namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    //! Represents a Globus computing resource that is accessed through an LGE interface.
	[Serializable]
	[DataContract]
	[EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above)]

    public class OneCloudAppliance : CloudAppliance {
        
        //---------------------------------------------------------------------------------------------------------------------
        
		[DataMember]
        public override VirtualMachineTemplate VirtualMachineTemplate {
            get { return VMTemplate; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
		[DataMember]
        public override VirtualDisk[] VirtualDisks {
            get { return Images; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
		[DataMember]
        public override VirtualNetwork VirtualNetwork {
            get { return Network; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        [DataMember]
        public new CloudProvider Provider {
            get { return XmlRpcProvider; }
        }

        //---------------------------------------------------------------------------------------------------------------------
		[IgnoreDataMember]
        public OneVMTemplate VMTemplate { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
		[IgnoreDataMember]
        public OneImage[] Images { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
		[IgnoreDataMember]
        public OneNetwork Network { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        [IgnoreDataMember]
        public string AdditionalTemplate { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        [IgnoreDataMember]
        private OneCloudProvider xmlrpcprovider { get; set;}
        [IgnoreDataMember]
        public OneCloudProvider XmlRpcProvider { 
            get{ 
                if (xmlrpcprovider == null && this.ProviderId != 0) xmlrpcprovider = (OneCloudProvider)OneCloudProvider.FromId(context, this.ProviderId);
                return xmlrpcprovider;
            } 
            set{ 
                xmlrpcprovider = value;
                base.ProviderId = xmlrpcprovider.Id;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override MachineState State { 
            get { 
                return this.GetStatus();
            }
        }

        private string hostname { get; set; }
        public override string Hostname{
            get{ 
                context.LogDebug(typeof(OneCloudAppliance),String.Format ("Hostname = {0}",hostname));
                if (hostname == null || hostname == "localhost") {
                    context.LogDebug(typeof(OneCloudAppliance),String.Format ("Get hostname - VM is {0}null",Vm == null ? "" : "not "));
                    XmlNode[] template = (XmlNode[])Vm.TEMPLATE;
                    bool aws = false;

                    context.LogDebug(typeof(OneCloudAppliance),String.Format ("Get hostname - VMtemplate is {0}null",Vm.TEMPLATE == null ? "" : "not "));

                    foreach (XmlNode nodeT in template) {
                        context.LogDebug(typeof(OneCloudAppliance),String.Format ("Inside template - {0}",nodeT.Name));
                        if (nodeT.Name == "CONTEXT") {
                            try{
                                aws = (nodeT["PUBLIC"].InnerText == "aws-ec2");
                            }catch(Exception e){
                                //no PUBLIC node
                                aws = false;
                            }
                            break;
                        }
                    }

                    foreach (XmlNode nodeT in template) {
                        if (aws) {
                            if (nodeT.Name.Contains("_PUBLICADDRESSES")) {
                                var ips = nodeT.InnerText.Split(",".ToCharArray());
                                hostname = ips[0];
                                break;
                            }
                        } else {
                            context.LogDebug(typeof(OneCloudAppliance),String.Format ("Inside template(2) - {0}",nodeT.Name));
                            if (nodeT.Name == "NIC") {
                                context.LogDebug(typeof(OneCloudAppliance),String.Format ("Hostname is now - {0}",nodeT["IP"].InnerText));
                                hostname = nodeT["IP"].InnerText;
                                break;
                            }
                        }
                    }
                    if (hostname == null) hostname = "localhost";
                }
                return hostname;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        [IgnoreDataMember]
        private VM vm { get; set; }
        [IgnoreDataMember]
        public VM Vm { 
            get{
                if (this.RemoteId == null || this.XmlRpcProvider == null) return null;
                if (vm == null ) vm = this.XmlRpcProvider.XmlRpc.VMGetInfo(Int32.Parse(this.RemoteId));
                return vm;
            }
            set{ 
                vm = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        //! Creates a new ComputingResource instance.
        /*!
            \param context the execution environment context
        */
        public OneCloudAppliance(IfyContext context) : base(context) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Cloud.XmlRpcCloudAppliance"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="vm">Vm.</param>
        /// <param name="provider">Provider.</param>
        public OneCloudAppliance(IfyContext context, VM vm, OneCloudProvider provider) : base(context) {
            this.XmlRpcProvider = provider;
            this.RemoteId = vm.ID;
            this.Name = vm.NAME;
            this.Vm = vm;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        //! Creates a new ComputingResource instance.
        /*!
            \param context the execution environment context
            \return the created ComputingResource object
        */
        public static new OneCloudAppliance GetInstance(IfyContext context) {
            return new OneCloudAppliance(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static OneCloudAppliance FromResources(IfyContext context, OneVMTemplate template, OneImage[] storages, OneNetwork network) {
            OneCloudAppliance result = new OneCloudAppliance(context);
            result.VMTemplate = template;
            result.Images = storages;
            result.Network = network;
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static CloudAppliance FromOneXmlTemplate (IfyContext context, string sbXmlTemplate)
        {

            OneCloudAppliance result = new OneCloudAppliance (context);

            // One of the parameter is a base64 xml template from opennebula
            XmlDocument sbXmlDoc = new XmlDocument ();
            sbXmlDoc.LoadXml (sbXmlTemplate);

            result.Name = sbXmlDoc.SelectSingleNode ("/VM/NAME").InnerText;
            result.RemoteId = sbXmlDoc.SelectSingleNode ("/VM/ID").InnerText;
            XmlNode itnode = sbXmlDoc.SelectSingleNode("/VM/TEMPLATE/INSTANCE_TYPE");
            if (itnode != null)
                result.VMTemplate = new OneVMTemplate(context);
            result.Network = new OneNetwork (context);
            result.Network.IpAddress = sbXmlDoc.SelectSingleNode ("/VM/TEMPLATE/NIC/IP").InnerText;
            result.State = MachineState.Active;
            result.StatusText = "UNKNOWN";
            return result;

        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates the instance on the cloud provider and stores the record in the local database.</summary>
        public override bool Create() {
            if (this.AdditionalTemplate == null) this.AdditionalTemplate = "";
            this.RemoteId = this.XmlRpcProvider.XmlRpc.TemplateInstanciateVM(Int32.Parse(this.VMTemplate.RemoteId), this.Name, false, this.AdditionalTemplate).ToString();
            Store();
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override void Delete() {
            this.XmlRpcProvider.XmlRpc.VMAction(Int32.Parse(this.RemoteId), "delete");
            base.Delete();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Start() {
            return this.XmlRpcProvider.XmlRpc.VMAction(Int32.Parse(this.RemoteId), "restart");
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Stop(MachineStopMethod method) {
            return this.XmlRpcProvider.XmlRpc.VMAction(Int32.Parse(this.RemoteId), "stop");
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Resume(MachineRestartMethod method) {
            return this.XmlRpcProvider.XmlRpc.VMAction(Int32.Parse(this.RemoteId), "resume");
        }


        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Suspend(MachineSuspendMethod method) {
            return this.XmlRpcProvider.XmlRpc.VMAction(Int32.Parse(this.RemoteId), "suspend");
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Shutdown(MachineStopMethod method) {
            return this.XmlRpcProvider.XmlRpc.VMAction(Int32.Parse(this.RemoteId), "shutdown");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static MachineState GetStateFromString(string s) {
            switch (s) {
                case "ACTIVE" :
                case "RESUME" :
                case "REBOOT" :
                    return MachineState.Active;
                case "SUSPENDED" :
                    return MachineState.Suspended;
                default :
                    return MachineState.Inactive;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override bool SaveDiskAs(int diskId, String caption) {
            try {
                if (Images[diskId] != null) {
                    this.XmlRpcProvider.XmlRpc.VMSaveDisks(Int32.Parse(this.RemoteId),diskId,caption, "");
                }
                return true;

            } catch (Exception e) {
               context.AddError("Error saving disk "+diskId+" OCCI server returned: "+e.Message);
                return false;
            }
        }
        
        public override void GetStatus(XmlDocument xmlDocument) {}

        public MachineState GetStatus(){
            int state = Int32.Parse(Vm.STATE);

            /*
            0    INIT
            1    PENDING
            2    HOLD
            3    ACTIVE
            4    STOPPED
            5    SUSPENDED
            6    DONE
            7    FAILED
            */

            switch (state) {
                case 3:
                    return MachineState.Active;
                case 4:
                    return MachineState.Stopped;
                case 5:
                    return MachineState.Suspended;
                default:
                    return MachineState.Inactive;
            }
        }
    }


}

