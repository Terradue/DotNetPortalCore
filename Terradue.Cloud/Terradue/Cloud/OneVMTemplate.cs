using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Portal;
using Terradue.OpenNebula;




//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;

/*!

\defgroup OneVMTemplate OpenNebula Virtual Machine Template
@{

The component represents a Virtual Machine Template in OpenNebula to instantiate a \ref OneCloudApppliance by extending \ref VirtualMachineTemplate.

\ingroup Cloud

\xrefitem dep "Dependencies" "Dependencies" \ref OneCloudProvider controls the appliance on OpenNebula platform

\xrefitem dep "Dependencies" "Dependencies" implements \ref VirtualMachineTemplate for OpenNebula VM Template

\xrefitem dep "Dependencies" "Dependencies" calls \ref OneClient to performs the template operations on OpenNebula


@}
*/




namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    //! Represents a Globus computing resource that is accessed through an LGE interface.
	[Serializable]
	[DataContract]
    public class OneVMTemplate : VirtualMachineTemplate {

        //---------------------------------------------------------------------------------------------------------------------

        public VMTEMPLATE OneTemplate { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public OneVMTemplate(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public OneVMTemplate (IfyContext context, string name) : base (context)
        {
            this.Name = name;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new XmlRpcVMTemplate instance.</summary>
        /// <param name="context">the execution environment context</param>
        /// <returns>the created XmlRpcVMTemplate object</returns>
        public static new OneVMTemplate GetInstance(IfyContext context) {
            return new OneVMTemplate(context);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Cloud.XmlRpcVMTemplate"/> class.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="template">Template.</param>
        public OneVMTemplate (IfyContext context, VMTEMPLATE template, CloudProvider provider) : base (context)
        {
            this.RemoteId = template.ID;
            this.Name = template.NAME;
            this.Provider = provider;
            this.OneTemplate = template;
        }

        //---------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Merges the content of the additional info with the existing template.
        /// </summary>
        /// <returns>The additional content.</returns>
        /// <param name="addContent">Add content.</param>
        public string GetTemplateXml(string key){
            if (this.OneTemplate == null || this.OneTemplate.TEMPLATE == null) return null;
            //we add the new content
            XmlNode[] user_template = (XmlNode[])this.OneTemplate.TEMPLATE;
            foreach (XmlNode nodeUT in user_template) {
                if (nodeUT.LocalName == key) return nodeUT.InnerXml;
            }
            return null;
        }
    }


}

