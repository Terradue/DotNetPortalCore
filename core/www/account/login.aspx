<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/login.aspx
# Version:      2.3
# Description:  Starts a session for a user based on username and password (login)
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact       info@terradue.com
*/

void Page_Load(object sender, EventArgs ea) {

    IfyWebContext context = new IfyWebContext(PagePrivileges.EverybodyView);
    try {
        context.SkipChecks = true;
        context.Open();
        context.XslFilename = Server.MapPath("/template/xsl/") + "login.xsl";
        context.LoginUser(Request["username"], Request["password"]);
        context.CheckAvailability(false);
        context.WriteInfo("You are now logged in");
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
