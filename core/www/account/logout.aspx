<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/logout.aspx
# Version:      2.3
# Description:  Ends the session of the current user (logout)
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
        context.Open();

        context.LogoutUser();
        context.WriteInfo("You are now logged out", "userLogOut");
        
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
