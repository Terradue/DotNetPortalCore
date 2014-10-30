<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/check.aspx
# Version:      2.3
# Description:  Checks whether there is a valid session (user logged in)
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
        context.StartXmlResponse();
        context.EndXmlResponse();
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
