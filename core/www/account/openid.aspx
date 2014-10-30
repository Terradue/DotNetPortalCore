<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/openid.aspx
# Version:      2.3
# Description:  Provides the user interface for managing personal OpenIDs
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact       info@terradue.com
*/

void Page_Load(object sender, EventArgs ea) {
    IfyWebContext context = IfyContext.GetWebContext(PagePrivileges.EverybodyView);
    try {
        context.Open();
        context.XslFilename = Server.MapPath("/template/xsl/") + "pubserver.xsl";

        Terradue.Portal.UserOpenId userOpenId;
        userOpenId = Terradue.Portal.UserOpenId.GetInstance(context);
        
        context.Privileges = PagePrivileges.UserEdit;
        if (!userOpenId.ProcessRequest()) {
            if (context.IsUserAuthenticated && !userOpenId.MissingIdentifier) {
                userOpenId.WriteItemList();
            } else {
                context.Privileges = PagePrivileges.EverybodyView;
                userOpenId.WriteProviderList();
            }
        }
        
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
