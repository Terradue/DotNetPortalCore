<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>
<%@ Import Namespace="Terradue.Cloud" %>


<%@ Import Namespace="System.Collections.Generic" %>

<script language="C#" runat="server">
/*
# Project:  	G-POD (ESA)
# Author: 		Terradue Srl
# Last update: 	26.03.2012
# Element:     	ify web portal  
# Name:        	cloud/admin/virtualresource.aspx
# Version      	0.1 
# Description: 	Control panel short interface for virtual resources on cloud providers 
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact 		info@terradue.com
*/

void Page_Load(object sender, EventArgs ea) {

	IfyWebContext context = IfyContext.GetWebContext(PagePrivileges.AdminOnly);
	try {
		context.ContentType = "text/xml";
		context.Open();

		int providerId;
		Int32.TryParse(context.GetParamValue("provider"), out providerId);
		
		if (providerId == 0) context.ReturnError("Missing or invalid cloud provider");

		CloudProvider cloudProvider = CloudProvider.FromId(context, providerId);
		
		string type = context.GetParamValue("type");
		VirtualResource[] list = null;
		
		switch (type) {
		    case "vmt" :
		        list = cloudProvider.FindVirtualMachineTemplates(false);
		        break;
		    case "vd" :
		        list = cloudProvider.FindVirtualDisks(false);
		        break;
		    case "vn" :
		        list = cloudProvider.FindVirtualNetworks(false);
		        break;
		    /*case "ca" :
		        list = cloudProvider.FindAppliances(false);
		        break;*/
		    default :
		        context.ReturnError("Missing or invalid virtual resource type");
		        break;
		        
		}
		
		context.StartXmlResponse();
		context.XmlWriter.WriteStartElement("virtualResource");
		context.XmlWriter.WriteAttributeString("provider", cloudProvider.AccessPoint);
		context.XmlWriter.WriteAttributeString("type", type);
		cloudProvider.GetValueSetFromList(list).WriteValues(context.XmlWriter);
		context.XmlWriter.WriteEndElement(); // </virtualResource>
		
		context.Close();

	} catch (Exception e) {
		context.Close(e);
	}
}

</script>
