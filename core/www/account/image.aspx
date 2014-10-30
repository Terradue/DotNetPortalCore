<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/image.aspx
# Version:      2.3
# Description:  Displays featured images
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
        context.XslFilename = Server.MapPath("/template/xsl/") + "image.xsl";
        try {
            context.OpenNewsDatabase();
        } catch (Exception) {
            context.ReturnError("News database not available");
        }

        Terradue.Portal.Image list = Terradue.Portal.Image.GetInstance(context);
        list.CanCreate = false;
        list.CanDelete = false;
        list.CanModify = false;
        list.SetOpenSearchDescription("Images", "Search EO Images higlights ", "Search images by keyword or any of the specific fields defined in the OpenSearch URL.");
        int id = context.GetIdFromRequest();
            if (id == 0) {
                if (context.GetParamValue("request") == "description") list.WriteOpenSearchDescription(false);
                else list.WriteItemList();//"time DESC",10);
            } else {
                list.WriteSingleItem(id);
                list.WriteItemList("ID DESC",5);
            }
        context.CloseNewsDatabase();
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
