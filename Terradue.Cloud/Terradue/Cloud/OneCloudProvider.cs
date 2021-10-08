using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Portal;
using Terradue.OpenNebula;



//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------


/*!

\defgroup OneCloudProvider OpenNebula Cloud Provider
@{

The component represents a Cloud Provider for an OpenNebula instance by extending the \ref CloudProvider module

\ingroup Cloud

\xrefitem dep "Dependencies" "Dependencies" implements \ref CloudProvider for OpenNebula

\xrefitem dep "Dependencies" "Dependencies" calls \ref OneClient to performs the provider operations on OpenNebula


@}
*/


namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    [EntityTable("onecloudprov", EntityTableConfiguration.Custom)]
    public class OneCloudProvider : CloudProvider {

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("admin_usr")]
        public string AdminUsr { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("admin_pwd")]
        public string AdminPwd { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        private OneClient xmlrpc { get; set; }
        public OneClient XmlRpc { 
            get{ 
                if (xmlrpc == null) xmlrpc = new OneClient(this.AccessPoint, AdminUsr, AdminPwd);
                return xmlrpc;
            } 
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public OneCloudProvider(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public OneCloudProvider(IfyContext context, string accessPoint) : base(context, accessPoint) {}

        //---------------------------------------------------------------------------------------------------------------------

        public OneCloudProvider(IfyContext context, string accessPoint, string username, string password) : base(context, accessPoint) {
            this.AdminUsr = username;
            this.AdminPwd = password;
        }
		
		//---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new XmlRpcCloudProvider instance.</summary>
        /// <param name="context">the execution environment context</param>
        /// <returns>the created XmlRpcCloudProvider object</returns>
        public static new OneCloudProvider GetInstance(IfyContext context) {
            return new OneCloudProvider(context);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void StartDelegate(string username){
            USER_POOL users = xmlrpc.UserGetPoolInfo();
            foreach (object user in users.Items) {
                if (user.GetType() == typeof(USER_POOLUSER)) {
                    if (((USER_POOLUSER)user).NAME.Equals(username)) {
                        xmlrpc.StartDelegate(username);
                        return;
                    }
                }
            }
            throw new Exception(String.Format("User {0} not found on One Cloud Provider",username));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void EndDelegate(){
            xmlrpc.EndDelegate();
        }

        //---------------------------------------------------------------------------------------------------------------------

        #region VIRTUAL_MACHINE_TEMPLATE
                
        /// <summary>Queries the cloud provider to get a list of the virtual machine templates (in OCCI: <i>instance types</i>) defined on it.</summary>
        public override VirtualMachineTemplate[] FindVirtualMachineTemplates(bool detailed) {
            List<VirtualMachineTemplate> result = new List<VirtualMachineTemplate> ();
            VMTEMPLATE_POOL pool = this.XmlRpc.TemplateGetPoolInfo(-2, -1, -1);
            foreach (VMTEMPLATE vm in pool.VMTEMPLATE) result.Add(new OneVMTemplate(context, vm, this));
            return result.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Queries the cloud provider to get the vm template corresponding to the Id.</summary>
        public VirtualMachineTemplate GetTemplate(string remoteId) {
            VMTEMPLATE temp = this.XmlRpc.TemplateGetInfo(Int32.Parse(remoteId));
            return new OneVMTemplate(context, temp, this);
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------------

        #region VIRTUAL_DISKS
        
        /// <summary>Queries the cloud provider to get a list of the virtual disks (in OCCI: <i>storages</i>) defined on it.</summary>
        public override VirtualDisk[] FindVirtualDisks(bool detailed) {
            List<VirtualDisk> list = new List<VirtualDisk>();
            IMAGE_POOL pool = this.XmlRpc.ImageGetPoolInfo(-2, -1, -1);
            foreach (IMAGE vm in pool.IMAGE) list.Add(new OneImage(context, vm, this));
            return list.ToArray();
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------------

        #region VIRTUAL_NETWORK

        /// <summary>Queries the cloud provider to get a list of the virtual networks defined on it.</summary>
        public override VirtualNetwork[] FindVirtualNetworks(bool detailed) {
            List<VirtualNetwork> result = new List<VirtualNetwork> ();
            VNET_POOL pool = this.XmlRpc.VNetGetPoolInfo(-2, -1, -1);
            foreach (VNET vm in pool.VNET) result.Add(new OneNetwork(context, vm, this));
            return result.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Queries the cloud provider to get the virtual networks corresponding to the Id.</summary>
        public override VirtualNetwork GetNetwork(string remoteId) {
            VNET vn = this.XmlRpc.VNetGetInfo(Int32.Parse(remoteId));
            return new OneNetwork(context, vn, this);
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------------

        #region IMAGE

        public VirtualDisk GetImage(string remoteId) {
            IMAGE img = this.XmlRpc.ImageGetInfo(Int32.Parse(remoteId));
            return new OneImage(context, img, this);
        }

        #endregion

        //---------------------------------------------------------------------------------------------------------------------

        #region APPLIANCE

        /// <summary>Queries the cloud provider to get a list of the cloud appliances created on it.</summary>
        public override CloudAppliance[] FindAppliances(bool detailed) {
            List<CloudAppliance> list = new List<CloudAppliance>();
            VM_POOL pool = this.XmlRpc.VMGetPoolInfo(-2,-1,-1,-2);
            foreach (VM vm in pool.VM) list.Add(new OneCloudAppliance(context, vm, this));
            return list.ToArray();
        }

        /// <summary>
        /// Gets the appliance.
        /// </summary>
        /// <returns>The appliance.</returns>
        /// <param name="remoteId">Remote identifier.</param>
        public CloudAppliance GetAppliance(string remoteId) {
            VM vm = this.XmlRpc.VMGetInfo(Int32.Parse(remoteId));
            return new OneCloudAppliance(context, vm, this);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override CloudAppliance CreateInstance(string name, string templateName, string[] diskNames, string networkName) {
            OneVMTemplate template = (OneVMTemplate)this.GetTemplate(templateName);
            OneImage[] disks = new OneImage[diskNames.Length];
            for (int i = 0; i < diskNames.Length; i++) disks[i] = (OneImage)this.GetImage(diskNames[i]);
            OneNetwork network = (networkName != null ? (OneNetwork)this.GetNetwork(networkName) : null);
            return CreateInstance(name, template, disks, network);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override CloudAppliance CreateInstance(string name, string templateName, string networkName) {
            OneVMTemplate template = (OneVMTemplate)this.GetTemplate(templateName);
            OneImage[] disks = new OneImage[0];
            OneNetwork network = (networkName != null && networkName != "" ? (OneNetwork)this.GetNetwork(networkName) : null);
            return CreateInstance(name, template, disks, network);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <returns>The instance.</returns>
        /// <param name="name">Name.</param>
        /// <param name="templateName">Template name.</param>
        /// <param name="networkName">Network name.</param>
        /// <param name="additionalTemplate">Additional template, value should be XML</param>
        public override CloudAppliance CreateInstance(string name, string templateName, string networkName, List<KeyValuePair<string,string>> additionalTemplate) {
            OneVMTemplate template = (OneVMTemplate)this.GetTemplate(templateName);
            string templatexml = "";
            foreach (KeyValuePair<string,string> kvp in additionalTemplate) {
                string xml = template.GetTemplateXml(kvp.Key);
                if (xml != null) {
                    templatexml += "<" + kvp.Key + ">" + xml + kvp.Value + "</" + kvp.Key + ">";
                }
            }
            if (templatexml == "") template.AdditionalContent = "";
            else template.AdditionalContent = "<TEMPLATE>" + templatexml + "</TEMPLATE>";
            OneImage[] disks = new OneImage[0];
            OneNetwork network = (networkName != null && networkName != "" ? (OneNetwork)this.GetNetwork(networkName) : null);
            return CreateInstance(name, template, disks, network);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override CloudAppliance CreateInstance(string name, VirtualMachineTemplate template, VirtualDisk[] disks, VirtualNetwork network) {

            OneCloudAppliance appliance = OneCloudAppliance.FromResources(context, template as OneVMTemplate, disks as OneImage[], network as OneNetwork);
            appliance.XmlRpcProvider = this;
            appliance.Name = name;
            appliance.AdditionalTemplate = template.AdditionalContent;

            //delegate client user
            CloudUser owner = CloudUser.FromIdAndProvider(context, context.OwnerId, this.Id);
            this.StartDelegate(owner.CloudUsername);
            appliance.Create();
            this.EndDelegate();

            foreach (OneImage disk in disks) this.XmlRpc.VMAttachDisk(Int32.Parse(appliance.Vm.ID), "<TEMPLATE><DISK><IMAGE_ID>"+disk.RemoteId+"</IMAGE_ID></DISK></TEMPLATE>");

            return appliance;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool DeleteInstance(CloudAppliance appliance) {
            try{
                appliance.Delete(); 
            }catch(Exception){
                return false;
            }
            return true;
        }

        #endregion
        
        //---------------------------------------------------------------------------------------------------------------------

    }


}

