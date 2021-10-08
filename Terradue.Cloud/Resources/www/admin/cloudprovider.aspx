<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>
<%@ Import Namespace="Terradue.Cloud" %>


<%@ Import Namespace="System.Collections.Generic" %>

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

		CloudProvider cloudProvider;
		
		int id = context.GetIdFromRequest();
		if (id == 0) {
			int extensionType = 0;
			Int32.TryParse(context.GetParamValue("_type"), out extensionType);
			if (extensionType == 0) cloudProvider = CloudProvider.GetInstance(context);
			else cloudProvider = CloudProvider.WithExtensionType(context, extensionType);
		} else {
			cloudProvider = CloudProvider.FromId(context, id);
		}
		
		cloudProvider.SetOpenSearchDescription("Cloud Providers", "Cloud provider search", "Search cloud providers by keyword.");
		cloudProvider.ProcessGenericRequest();
		
		context.Close();

	} catch (Exception e) {
		context.Close(e);
	}
}

</script>
