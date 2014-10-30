<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/image.aspx
# Version:      2.3
# Description:  Provides the administration interface for managing featured images
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
        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "image.xsl";
        try {
        context.OpenNewsDatabase();
        } catch (Exception) {
        context.ReturnError("Image database not available");
        }

        Terradue.Portal.Image image = Terradue.Portal.Image.GetInstance(context);
        image.SetOpenSearchDescription("Image", "Image search", "Search frequently asked questions by keyword or any of the specific fields defined in the OpenSearch URL.");
        image.ProcessGenericRequest();
        
        context.CloseNewsDatabase();
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
