<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/pubserver.aspx
# Version:      2.3
# Description:  Provides the administration interface for managing publish locations
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact       info@terradue.com
*/

void Page_Load(object sender, EventArgs ea) {
    IfyWebContext context = IfyContext.GetWebContext(PagePrivileges.AdminOnly);
    try {
        context.Open();
        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "pubserver.xsl";

        Terradue.Portal.PublishServer publishServer = Terradue.Portal.PublishServer.GetInstance(context);
        publishServer.NoRestriction = true;
        publishServer.SetOpenSearchDescription("Publish Servers", "Publish server search", "Search publish servers by keyword or any of the specific fields defined in the OpenSearch URL.");
        publishServer.ProcessGenericRequest();
        
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
