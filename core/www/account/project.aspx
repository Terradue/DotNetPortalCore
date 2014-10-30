<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/project.aspx
# Version:      2.3
# Description:  Displays featured projects
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
        context.XslFilename = Server.MapPath("/template/xsl/") + "project.xsl";
        try {
            context.OpenNewsDatabase();
        } catch (Exception) {
            context.ReturnError("News database not available");
        }

        Project list = Project.GetInstance(context);
        list.SetOpenSearchDescription("Project", "Search Projects", "Search news by keyword or any of the specific fields defined in the OpenSearch URL.");
        int id = context.GetIdFromRequest();
            if (id == 0) {
                if (context.GetParamValue("request") == "description") list.WriteOpenSearchDescription(false);
                else list.WriteItemList("id DESC",10);
            } else {
                list.WriteSingleItem(id);
                list.WriteItemList("id DESC",5);
            }
        context.CloseNewsDatabase();
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
