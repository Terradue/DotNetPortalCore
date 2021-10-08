using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Portal;





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
    [EntityTable("occicloudprov", EntityTableConfiguration.Custom)]
    public class OcciCloudProvider : CloudProvider {
        
        //---------------------------------------------------------------------------------------------------------------------
        
        [EntityDataField("occi_version")]
        public string OcciVersion { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public OcciCloudProvider(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

		public OcciCloudProvider(IfyContext context, string accessPoint) : base(context, accessPoint) {}
		
		//---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new OcciCloudProvider instance.</summary>
        /// <param name="context">the execution environment context</param>
        /// <returns>the created OcciCloudProvider object</returns>
        public static new OcciCloudProvider GetInstance(IfyContext context) {
            return new OcciCloudProvider(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Queries the cloud provider to get a list of the virtual machine templates (in OCCI: <i>instance types</i>) defined on it.</summary>
        public override VirtualMachineTemplate[] FindVirtualMachineTemplates(bool detailed) {
            XmlNodeList nodes = GetResourceNodeList("/instance_type", "INSTANCE_TYPE_COLLECTION/INSTANCE_TYPE");
            if (nodes == null) return null;

            List<OcciInstanceType> list = new List<OcciInstanceType>();
            foreach (XmlNode node in nodes) {
                XmlElement elem = node as XmlElement;
                if (elem == null) continue;
                OcciInstanceType item = null;
                if (detailed) {
                    if (elem.HasAttribute("href")) {
                        elem = GetResourceNode(String.Format("/instance_type/{0}", Regex.Replace(node.Attributes["href"].Value, "^.*/", String.Empty))); 
                        item = OcciInstanceType.FromItemXml(context, this, elem);
                    }
                } else {
                    item = OcciInstanceType.FromListXml(context, this, elem);
                }
                list.Add(item);
            }
            return list.ToArray();
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Queries the cloud provider to get a list of the virtual disks (in OCCI: <i>storages</i>) defined on it.</summary>
        public override VirtualDisk[] FindVirtualDisks(bool detailed) {
            XmlNodeList nodes = GetResourceNodeList("/storage", "STORAGE_COLLECTION/STORAGE");
            if (nodes == null) return null;

            List<OcciStorage> list = new List<OcciStorage>();
            foreach (XmlNode node in nodes) {
                XmlElement elem = node as XmlElement;
                if (elem == null) continue;
                OcciStorage item = new OcciStorage(context);
                if (detailed) {
                    if (elem.HasAttribute("href")) {
                        elem = GetResourceNode(String.Format("/storage/{0}", Regex.Replace(node.Attributes["href"].Value, "^.*/", String.Empty))); 
                        OcciStorage.FromItemXml(item, this, elem);
                    }
                } else {
                    item = OcciStorage.FromListXml(context, this, elem);
                }
                list.Add(item);
            }
            return list.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Queries the cloud provider to get a list of the virtual networks (in OCCI: <i>networks</i>) defined on it.</summary>
        public override VirtualNetwork[] FindVirtualNetworks(bool detailed) {
            XmlNodeList nodes = GetResourceNodeList("/network", "NETWORK_COLLECTION/NETWORK");
            if (nodes == null) return null;
            
            List<OcciNetwork> list = new List<OcciNetwork>();
            foreach (XmlNode node in nodes) {
                XmlElement elem = node as XmlElement;
                if (elem == null) continue;
                OcciNetwork item = null;
                if (detailed) {
                    if (elem.HasAttribute("href")) {
                        elem = GetResourceNode(String.Format("/network/{0}", Regex.Replace(node.Attributes["href"].Value, "^.*/", String.Empty))); 
                        item = OcciNetwork.FromItemXml(context, this, elem);
                    }
                } else {
                    item = OcciNetwork.FromListXml(context, this, elem);
                }
                list.Add(item);
            }
            return list.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Queries the cloud provider to get a list of the virtual networks (in OCCI: <i>networks</i>) defined on it.</summary>
        public override VirtualNetwork GetNetwork(string remoteId) {
            XmlElement elem = GetResourceNode(String.Format("/network/{0}", remoteId));
            if (elem == null) return null;
            
            OcciNetwork result = null;
            result = OcciNetwork.FromItemXml(context, this, elem);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Queries the cloud provider to get a list of the cloud appliances created on it.</summary>
        public override CloudAppliance[] FindAppliances(bool detailed) {
            //XmlNodeList nodes = GetResourceNodeList("/compute", "COMPUTE_COLLECTION/COMPUTE");
            
            List<CloudAppliance> list = new List<CloudAppliance>();
            /*foreach (XmlNode node in nodes) {
                OcciCloudAppliance item = OcciCloudAppliance.FromListXml(context, this, node as XmlElement);
                list.Add(item);
            }*/
            return list.ToArray();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override CloudAppliance CreateInstance(string name, string templateName, string[] diskNames, string networkName) {
            OcciInstanceType template = OcciInstanceType.FromRemoteId(context, this, templateName);
            OcciStorage[] disks = new OcciStorage[diskNames.Length];
            for (int i = 0; i < diskNames.Length; i++) disks[i] = OcciStorage.FromRemoteId(context, this, diskNames[i]);
            OcciNetwork network = OcciNetwork.FromRemoteId(context, this, networkName);
            return CreateInstance(name, template, disks, network);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override CloudAppliance CreateInstance(string name, string templateName, string networkName) {
            OcciInstanceType template = OcciInstanceType.FromRemoteId(context, this, templateName);
            OcciStorage[] disks = new OcciStorage[0];//this.GetDisks();//\todo GetDisks
            //for (int i = 0; i < diskNames.Length; i++) disks[i] = OcciStorage.FromRemoteId(context, this, diskNames[i]);
            OcciNetwork network = OcciNetwork.FromRemoteId(context, this, networkName);
            return CreateInstance(name, template, disks, network);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override CloudAppliance CreateInstance(string name, string templateName, string networkName, List<KeyValuePair<string,string>> additionalTemplate) {
            return CreateInstance(name, templateName, networkName);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override CloudAppliance CreateInstance(string name, VirtualMachineTemplate template, VirtualDisk[] disks, VirtualNetwork network) {
            CloudAppliance appliance = OcciCloudAppliance.FromResources(context, template as OcciInstanceType, disks as OcciStorage[], network as OcciNetwork);
            appliance.Name = name;
            appliance.Create();
            return appliance;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool DeleteInstance(CloudAppliance appliance) {
            return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual object QueryCompute(Object obj) {
            return null;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual object QueryStorage(Object obj) {
            return null;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual object QueryNetwork(Object obj) {
            return null;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual XmlNodeList GetResourceNodeList(string path, string xPathExpression) {
            // Get document
            string url = String.Format("{0}{1}", AccessPoint, path);
			HttpWebRequest request = context.GetSslRequest(url, "GET", null);
            try {
                // Get response stream.
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                XmlDocument doc = new XmlDocument();
                doc.Load(response.GetResponseStream());
                XmlNodeList result = doc.SelectNodes(xPathExpression);
                response.Close();

                return result;
                
            } catch (Exception e) {
                context.ReturnError("Could not access cloud provider [" + url + "] :" + e.Message);
            }
            return null;
            /*StreamReader reader = new StreamReader(data);
            string s = reader.ReadToEnd();
            ret
            Console.WriteLine(s);
            data.Close();
            reader.Close();*/
            
            
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual XmlElement GetResourceNode(string path) {
            string url = String.Format("{0}{1}", AccessPoint, path);
			HttpWebRequest request = context.GetSslRequest(url, "GET", null);
            try {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                XmlDocument doc = new XmlDocument();
                doc.Load(response.GetResponseStream());
                XmlElement result = doc.DocumentElement;
                response.Close();

                return result;
            } catch (Exception e) {
                context.ReturnError("Could not access cloud provider [" + url + "] :" + e.Message);
            }
            return null;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

    }


}

