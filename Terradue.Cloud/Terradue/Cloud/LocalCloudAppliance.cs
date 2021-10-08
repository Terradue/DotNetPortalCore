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
using System.Linq;






namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    //! Represents a Globus computing resource that is accessed through an LGE interface.
	[Serializable]
	[DataContract]
	[EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above)]

    public class LocalCloudAppliance : CloudAppliance {
        
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
		[IgnoreDataMember]
        public OneVMTemplate VMTemplate { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
		[IgnoreDataMember]
        public OneImage[] Images { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
		[IgnoreDataMember]
        public OneNetwork Network { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public override MachineState State { 
            get { 
                return this.GetStatus();
            }
        }


        //---------------------------------------------------------------------------------------------------------------------

        //! Creates a new ComputingResource instance.
        /*!
            \param context the execution environment context
        */
        public LocalCloudAppliance(IfyContext context) : base(context) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Cloud.XmlRpcCloudAppliance"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="vm">Vm.</param>
        /// <param name="provider">Provider.</param>
        public LocalCloudAppliance(IfyContext context, VM vm, OneCloudProvider provider) : base(context) {
            this.RemoteId = vm.ID;
            this.Name = vm.NAME;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        //! Creates a new ComputingResource instance.
        /*!
            \param context the execution environment context
            \return the created ComputingResource object
        */
        public static new LocalCloudAppliance GetInstance(IfyContext context) {
            return new LocalCloudAppliance(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static LocalCloudAppliance FromResources(IfyContext context, OneVMTemplate template, OneImage[] storages, OneNetwork network) {
            LocalCloudAppliance result = new LocalCloudAppliance(context);
            result.VMTemplate = template;
            result.Images = storages;
            result.Network = network;
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static CloudAppliance FromOneXmlTemplate (IfyContext context, string sbXmlTemplate)
        {

            LocalCloudAppliance result = new LocalCloudAppliance (context);

            // One of the parameter is a base64 xml template from opennebula
            XmlDocument sbXmlDoc = new XmlDocument ();
            sbXmlDoc.LoadXml (sbXmlTemplate);

            result.Name = sbXmlDoc.SelectSingleNode ("/VM/NAME").InnerText;
            result.RemoteId = sbXmlDoc.SelectSingleNode ("/VM/ID").InnerText;
            XmlNode itnode = sbXmlDoc.SelectSingleNode("/VM/TEMPLATE/INSTANCE_TYPE");
            result.Network = new OneNetwork (context);
            result.Network.IpAddress = GetLocalIPAddress();
            result.State = MachineState.Active;
            result.StatusText = "ACTIVE";
            return result;

        }

        private static string GetLocalIPAddress(){
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return "localhost";

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            var ipaddress = host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            return ipaddress.ToString();
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

        
        public override void GetStatus(XmlDocument xmlDocument) {}

        public MachineState GetStatus(){

            return MachineState.Active;

        }

        #region implemented abstract members of CloudAppliance

        public override bool Create() {
            throw new NotImplementedException();
        }

        public override bool Start() {
            throw new NotImplementedException();
        }

        public override bool Stop(MachineStopMethod method) {
            throw new NotImplementedException();
        }

        public override bool Suspend(MachineSuspendMethod method) {
            throw new NotImplementedException();
        }

        public override bool Resume(MachineRestartMethod method) {
            throw new NotImplementedException();
        }

        public override bool Shutdown(MachineStopMethod cdmethod) {
            throw new NotImplementedException();
        }

        public override bool SaveDiskAs(int id, string caption) {
            throw new NotImplementedException();
        }

        #endregion
    }


}

