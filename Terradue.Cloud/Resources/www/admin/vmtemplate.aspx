<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>
<%@ Import Namespace="Terradue.Cloud" %>

<script language="C#" runat="server">
/*
# Project:  	G-POD (ESA)
# Author: 		Terradue Srl
# Last update: 	26.09.2010
# Element:     	ify web portal  
# Name:        	admin/application.aspx 
# Version      	0.1 
# Description: 	External application control panel 
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
		context.Open();
		context.XslFilename = Server.MapPath("/template/xsl/admin/") + "ce.xsl";

		VirtualMachineTemplate template = VirtualMachineTemplate.GetInstance(context);
		template.SetOpenSearchDescription("Virtual Machine Templates", "Virtual machine search", "Search virtual machines by keyword.");
		template.ProcessGenericRequest();
		
		context.Close();

	} catch (Exception e) { 
		context.Close();
	}
}

</script>
