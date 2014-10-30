<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/filter.aspx
# Version:      2.3
# Description:  Provides the user interface for managing filtered views
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact       info@terradue.com
*/

void Page_Load(object sender, EventArgs ea) {

    IfyWebContext context = new IfyWebContext(PagePrivileges.UserView);
    try {
        context.ContentType = "text/xml";
        context.Open();
        Filter filter = Filter.GetInstance(context);
        filter.ProcessRequest();
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
