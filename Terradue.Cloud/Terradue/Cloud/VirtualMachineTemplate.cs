using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using Terradue.Portal;

/*!

\defgroup VirtualMachineTemplate Virtual Machine Template
@{

The component represents an abstract Virtual Machine Template to instantiate a \ref CloudApppliance.
Practically, a class that implements \ref Terradue.Cloud#VirtualMachineTemplate represents a specification
of the virtual machine to instantiate on a \ref Terradue.Cloud#CloudProvider

\ingroup Cloud

\xrefitem dep "Dependencies" "Dependencies" \ref CloudProvider provides with the virtual machines templates


@}
*/



namespace Terradue.Cloud {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
    /// \ingroup VirtualMachineTemplate
	[Serializable]
	[DataContract]
    public class VirtualMachineTemplate : VirtualResource {

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the additional content to be add to the template during the instantiation
        /// </summary>
        /// <value>The additional content of the template.</value>
        public string AdditionalContent { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public VirtualMachineTemplate(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new VirtualMachineTemplate instance.</summary>
        /// <param name="context">the execution environment context</param>
        /// <returns>the created VirtualMachineTemplate object</returns>
        public static new VirtualMachineTemplate GetInstance(IfyContext context) {
            return new VirtualMachineTemplate(context);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
    }


}

