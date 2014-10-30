<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/application.aspx
# Version:      2.3
# Description:  Provides the administration interface for managing external applications
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
        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "default.xsl";

        Terradue.Portal.Application application = Terradue.Portal.Application.GetInstance(context);
        application.SetOpenSearchDescription("External applications", "External application search", "Search external applications by keyword or any of the specific fields defined in the OpenSearch URL.");
        application.ProcessGenericRequest();
        
        context.Close();

    } catch (Exception e) { 
        context.Close(e);
    }
}

</script>
