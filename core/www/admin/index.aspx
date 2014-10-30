<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/index.aspx
# Version:      2.3
# Description:  Index page for administration area
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
        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "controlpanel.xsl";
        context.StartXmlResponse();
        context.WriteXmlFile(Server.MapPath("/") + "../config/controlpanel.xml"); 
        context.EndXmlResponse();

        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
