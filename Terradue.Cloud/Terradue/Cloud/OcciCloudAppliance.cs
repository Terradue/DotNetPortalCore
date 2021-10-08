using System;
using System.Collections.Generic;
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
using Terradue.Util;





namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    //! Represents a Globus computing resource that is accessed through an LGE interface.
	[Serializable]
	[DataContract]
	[EntityTable(null, EntityTableConfiguration.Custom, Storage = EntityTableStorage.Above)]

    public class OcciCloudAppliance : CloudAppliance {
        
        //---------------------------------------------------------------------------------------------------------------------
        
		[DataMember]
        public override VirtualMachineTemplate VirtualMachineTemplate {
            get { return InstanceType; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
		[DataMember]
        public override VirtualDisk[] VirtualDisks {
            get { return Storages; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
		[DataMember]
        public override VirtualNetwork VirtualNetwork {
            get { return Network; }
        }

        //---------------------------------------------------------------------------------------------------------------------
		[IgnoreDataMember]
        public OcciInstanceType InstanceType { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
		[IgnoreDataMember]
        public OcciStorage[] Storages { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
		[IgnoreDataMember]
        public OcciNetwork Network { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        //! Creates a new ComputingResource instance.
        /*!
            \param context the execution environment context
        */
        public OcciCloudAppliance(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        //! Creates a new ComputingResource instance.
        /*!
            \param context the execution environment context
            \return the created ComputingResource object
        */
        public static new OcciCloudAppliance GetInstance(IfyContext context) {
            return new OcciCloudAppliance(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static OcciCloudAppliance FromResources(IfyContext context, OcciInstanceType instanceType, OcciStorage[] storages, OcciNetwork network) {
            OcciCloudAppliance result = new OcciCloudAppliance(context);
            result.Provider = instanceType.Provider;
            result.InstanceType = instanceType;
            result.Storages = storages;
            result.Network = network;
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Froms the OCCI interface.
		/// </summary>
		/// <returns>
		/// The OCCI interface.
		/// </returns>
		/// <param name='context'>
		/// Context.
		/// </param>
		/// <param name='provider'>
		/// Provider.
		/// </param>
		public static OcciCloudAppliance FromOCCIInterface (IfyContext context, CloudProvider provider, string remoteId)
		{
			OcciCloudAppliance result = new OcciCloudAppliance (context);

			result.Provider = provider;
			result.RemoteId = remoteId;

			HttpWebRequest httpRequest = context.GetSslRequest(String.Format("{0}/compute/{1}", provider.AccessPoint, remoteId), "GET", null);
			HttpWebResponse response = null;
			try {
				// Get response stream.
				response = (HttpWebResponse)httpRequest.GetResponse ();
				XmlDocument doc = new XmlDocument ();
				doc.Load (response.GetResponseStream ());
				result.Load (doc.DocumentElement);
				
				response.Close ();
				
			} catch (WebException we) {
				throw new WebException("Error during request to " + httpRequest.RequestUri, we, we.Status, response);
			}

			return result;
		}

		public static CloudAppliance FromOneXmlTemplate (IfyContext context, string sbXmlTemplate)
		{

			OcciCloudAppliance result = new OcciCloudAppliance (context);

			// One of the parameter is a base64 xml template from opennebula
			XmlDocument sbXmlDoc = new XmlDocument ();
			sbXmlDoc.LoadXml (sbXmlTemplate);

			result.Name = sbXmlDoc.SelectSingleNode ("/VM/NAME").InnerText;
			result.RemoteId = sbXmlDoc.SelectSingleNode ("/VM/ID").InnerText;
            XmlNode itnode = sbXmlDoc.SelectSingleNode("/VM/TEMPLATE/INSTANCE_TYPE");
            if ( itnode != null )
                result.InstanceType = new OcciInstanceType(context,itnode.InnerText);
			result.Network = new OcciNetwork (context);
			result.Network.IpAddress = sbXmlDoc.SelectSingleNode ("/VM/TEMPLATE/NIC/IP").InnerText;
			result.State = MachineState.Active;
			result.StatusText = "UNKNOWN";
			return result;

		}
		
        //---------------------------------------------------------------------------------------------------------------------

        //! Loads the Globus computing resource information from the database.
        /*!
            \param condition SQL conditional expression without WHERE keyword
        */
        public override void Load() {
            base.Load();
            
			HttpWebRequest request = context.GetSslRequest(String.Format("{0}/compute/{1}", Provider.AccessPoint, RemoteId), "GET", null, OwnerId);
            try {
                // Get response stream.
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                XmlDocument doc = new XmlDocument();
                doc.Load(response.GetResponseStream());
                Load(doc.DocumentElement);
    
                //Console.WriteLine(StringUtils.GetXmlFromStream(response.GetResponseStream(), true));
                response.Close();

            } catch (Exception e) {
                 context.ReturnError(e.Message);
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected void Load(XmlElement elem) {
            RemoteId = (elem.HasAttribute("href") ? elem.Attributes["href"].Value : null);
            if (RemoteId != null) RemoteId = Regex.Replace(RemoteId, "^.*/", String.Empty);
            Name = (elem.HasAttribute("name") ? elem.Attributes["name"].Value : null);
            List<OcciStorage> storageList = new List<OcciStorage>(); 
            foreach (XmlNode node in elem) { 
                XmlElement subElem = node as XmlElement;
                if (subElem == null) continue;
                switch (subElem.Name) {
                    case "USER" :
                        if (subElem.HasAttribute("name")) Owner = subElem.Attributes["name"].Value;
                        break;
                    case "NAME" :
                        Name = subElem.InnerXml;
                        break;
                    case "INSTANCE_TYPE" :
                        InstanceType = OcciInstanceType.FromListXml(context, Provider, subElem);
                        break;
                    case "STATE" :
                        StatusText = subElem.InnerXml;
                        State = GetStateFromString(StatusText);
                        break;
                    case "DISK" :
                        storageList.Add(OcciStorage.FromComputeXml(context, this, subElem));
                        break;
                    case "NIC" :
                        Network = OcciNetwork.FromComputeXml(context, this, subElem);
                        break;
                    case "CONTEXT" :
                        foreach (XmlNode node2 in subElem) { 
                            XmlElement subElem2 = node2 as XmlElement;
                            if (subElem2 == null) continue;
                            switch (subElem2.Name) {
								case "CIOP_USERNAME":
									Username = subElem2.InnerText;
									break;
                            }
                        }
                        break;
                }
            }
            
            switch (StatusText) {
               case "ACTIVE" :
               // If the IP Address is not yet ready, the system is probably booting.
               if ( Network != null && VirtualNetwork.IpAddress == null ){
                      StatusText = "PREPARING";
               }
               break;
            }
            
            Storages = storageList.ToArray();
            
            int diskErrorCount = 0;
            for (int i = 0; i < Storages.Length; i++) {
                OcciStorage storage = Storages[i]; 
                try {
                    if (storage.RemoteId != String.Empty) {
                        XmlElement elem2 = (Provider as OcciCloudProvider).GetResourceNode(String.Format("/storage/{0}", storage.RemoteId)); 
                        OcciStorage.FromItemXml(storage, Provider, elem2);
                    }
                    if (storage.SavedAs != null) {
                       context.AddDebug(3,"Found Disk "+storage.Id+" to be saved as "+storage.SavedAs);
                        XmlElement elem2 = (Provider as OcciCloudProvider).GetResourceNode(String.Format("/storage/{0}", storage.SavedAs)); 
                        storage.SavedAs = elem2["NAME"].InnerXml;
                    }
                } catch (Exception e) {
                   context.AddWarning(e.Message);
                    diskErrorCount++;
                }
            }
            
            if (diskErrorCount != 0) context.AddWarning(String.Format("No information available for {0}", diskErrorCount == 1 ? "one storage" : diskErrorCount + " storages")); 
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates the instance on the cloud provider and stores the record in the local database.</summary>
        public override bool Create() {
			HttpWebRequest request = context.GetSslRequest(String.Format("{0}/compute", Provider.AccessPoint), "POST", "text/xml");
            try {
                Stream requestStream = request.GetRequestStream();
                MonoXmlWriter writer = MonoXmlWriter.Create(requestStream);
                WriteXmlForProvider(writer, null);
                writer.Close();
                requestStream.Close();

                // Get response stream.
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                XmlDocument doc = new XmlDocument();
                doc.Load(response.GetResponseStream());
                Load(doc.DocumentElement);
    
                //Console.WriteLine(StringUtils.GetXmlFromStream(response.GetResponseStream(), true));
                response.Close();

            } catch (Exception e) {
                context.ReturnError(e.Message);
            }
            
            Store();

            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override void Delete() {
            SendHttpOperation("DELETE", null);
            base.Delete();
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Start() {
            return Resume(MachineRestartMethod.Cold);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Stop(MachineStopMethod method) {
            return SendPutOperation("STOPPED");
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Resume(MachineRestartMethod method) {
            return SendPutOperation("RESUME");
        }


        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Suspend(MachineSuspendMethod method) {
            return SendPutOperation("SUSPENDED");
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool Shutdown(MachineStopMethod method) {
            return SendPutOperation("SHUTDOWN");
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
        
        protected bool SendPutOperation(string state) {
            return SendHttpOperation("PUT", state);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected bool SendHttpOperation(string method, string state) {
			HttpWebRequest request = context.GetSslRequest(String.Format("{0}/compute/{1}", Provider.AccessPoint, RemoteId), method, "text/xml", OwnerId);
            try {
                if (state != null) {
                    Stream requestStream = request.GetRequestStream();
                    MonoXmlWriter writer = MonoXmlWriter.Create(requestStream);
                    WriteXmlForProvider(writer, state);
                    writer.Close();
                    requestStream.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Close();

				request = context.GetSslRequest(String.Format("{0}/compute/{1}", Provider.AccessPoint, RemoteId), "GET", null, OwnerId);

                // Get response stream.
                response = (HttpWebResponse)request.GetResponse();
                XmlDocument doc = new XmlDocument();
                doc.Load(response.GetResponseStream());
                Load(doc.DocumentElement);
    
                response.Close();
    
                return true;

            } catch (WebException e) {
                
                HttpWebResponse responseError = (HttpWebResponse)e.Response;
                StreamReader reader = new StreamReader(responseError.GetResponseStream(), Encoding.UTF8);
                String responseString = reader.ReadToEnd();
                responseError.Close();
                
                throw new Exception(responseString);
                
            } catch (Exception e) {
                
                throw e;
                
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected void WriteXmlForProvider(XmlWriter xmlWriter, string state) {
            xmlWriter.WriteStartElement("COMPUTE");
            if (Id == 0 && state == null) {
                xmlWriter.WriteElementString("NAME", Name);
                xmlWriter.WriteStartElement("INSTANCE_TYPE");
                xmlWriter.WriteAttributeString("href", String.Format("{0}/instance_type/{1}", Provider.AccessPoint, InstanceType.RemoteId));
                xmlWriter.WriteEndElement(); // </INSTANCE_TYPE>
                foreach (VirtualDisk storage in Storages) {
                    xmlWriter.WriteStartElement("DISK");
                    xmlWriter.WriteStartElement("STORAGE");
                    xmlWriter.WriteAttributeString("href", String.Format("{0}/storage/{1}", Provider.AccessPoint, storage.RemoteId));
                    xmlWriter.WriteEndElement(); // </STORAGE>
                    xmlWriter.WriteEndElement(); // </DISK>
                    
                }
                xmlWriter.WriteStartElement("NIC");
                xmlWriter.WriteStartElement("NETWORK");
                xmlWriter.WriteAttributeString("href", String.Format("{0}/network/{1}", Provider.AccessPoint, Network.RemoteId));
                xmlWriter.WriteEndElement(); // </NETWORK>
                xmlWriter.WriteEndElement(); // </NIC>
            } else {
                if (Id != 0) xmlWriter.WriteElementString("ID", Id.ToString());
                if (state != null) xmlWriter.WriteElementString("STATE", state);
            }
            xmlWriter.WriteEndElement(); // </COMPUTE>
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void GetStatus(XmlDocument xmlDocument) {
            
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static OcciCloudAppliance FromListXml(IfyContext context, CloudProvider provider, XmlElement elem) {
            OcciCloudAppliance result = new OcciCloudAppliance(context);
            result.Provider = provider;
            result.RemoteId = (elem.HasAttribute("href") ? elem.Attributes["href"].Value : null);
            if (result.RemoteId != null) result.RemoteId = Regex.Replace(result.RemoteId, "^.*/", String.Empty);
            result.Name = (elem.HasAttribute("name") ? elem.Attributes["name"].Value : null);
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static OcciCloudAppliance FromItemXml(IfyContext context, CloudProvider provider, XmlElement elem) {
            OcciCloudAppliance result = new OcciCloudAppliance(context);
            result.Provider = provider;
            result.Load(elem);
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override bool SaveDiskAs(int diskId, String caption) {
			HttpWebRequest request = context.GetSslRequest(String.Format("{0}/compute/{1}", Provider.AccessPoint, RemoteId), "PUT", "text/xml", OwnerId);
            try {
                context.AddDebug(3,caption);
                if (Storages[diskId] != null) {
                    Stream requestStream = request.GetRequestStream();
                    MonoXmlWriter writer = MonoXmlWriter.Create(requestStream);
                    writer.WriteStartElement("COMPUTE");
                    writer.WriteElementString("ID", Id.ToString());
                    writer.WriteElementString("NAME", Name);
                    writer.WriteStartElement("DISK");
                    writer.WriteAttributeString("id",Storages[diskId].Id.ToString());
                    writer.WriteStartElement("SAVE_AS");
                    writer.WriteAttributeString("name",caption);
                    writer.WriteEndElement(); // </SAVE_AS>
                    writer.WriteEndElement(); // </DISK>
                    writer.WriteEndElement(); // </COMPUTE>
                    writer.Close();
                    requestStream.Close();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Close();
    
                return true;

            } catch (Exception e) {
               context.AddError("Error saving disk "+diskId+" OCCI server returned: "+e.Message);
                return false;
            }
        }
        
    }


}

