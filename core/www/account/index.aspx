<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/index.aspx
# Version:      2.3
# Description:  Provides the user profile creation or modification interface
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
        if (!context.IsUserAuthenticated && context.RequestedOperation != "activate" && context.RequestedOperation != "signin" && context.RequestedOperation != "recover" && !context.AllowSelfRegistration) context.RejectUnauthenticatedRequest();

        context.XslFilename = Server.MapPath("/template/xsl/account.xsl");

        Terradue.Portal.User user = Terradue.Portal.User.GetInstance(context);
        user.OwnAccount = true;
        user.ProcessGenericRequest();

        context.Close();
        
        if (user.Activated) context.Redirect("/", true, true);

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
