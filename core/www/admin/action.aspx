<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/action.aspx
# Version:      2.3
# Description:  Provides the administration interface for managing actions of the background agent
#               (portal-agent)
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

        int id = context.GetIdFromRequest();
        string name = context.GetParamValue("name");
        
        Terradue.Portal.Action action;
        if (context.GetParamValue("_request") == "execute" && (id != 0 || name != null)) {
        if (id != 0) action = Terradue.Portal.Action.FromId(context, id);
        else action = Terradue.Portal.Action.FromName(context, name);
        action.ProcessRequest();
        }
        
        action = Terradue.Portal.Action.GetInstance(context);
        action.CanCreate = false;
        action.CanDelete = false;
        action.ProcessGenericRequest();
        
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
