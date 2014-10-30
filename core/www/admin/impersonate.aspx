<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/impersonate.aspx
# Version:      2.3
# Description:  Provides the administrator interface for starting/ending the impersonation of other users
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact       info@terradue.com
*/

void Page_Load(object sender, EventArgs ea) {
    IfyWebContext context = new IfyWebContext(PagePrivileges.AdminOnly);
    try {
        context.Open();
        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "default.xsl";

        Terradue.Portal.User user = Terradue.Portal.User.GetInstance(context);
        user.SetOpenSearchDescription("Users", "User search", "Search users by keyword or any of the specific fields defined in the OpenSearch URL.");
        user.Impersonation = true;
        user.ProcessGenericRequest();
        
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
