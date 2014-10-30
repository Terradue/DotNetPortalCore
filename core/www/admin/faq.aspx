<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/faq.aspx
# Version:      2.3
# Description:  Provides the administration interface for managing frequently asked questions (FAQ)
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
        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "faq.xsl";
        try {
        context.OpenNewsDatabase();
        } catch (Exception) {
        context.ReturnError("FAQ database not available");
        }
        
        Faq faq = Faq.GetInstance(context);
        faq.SetOpenSearchDescription("FAQ", "FAQ search", "Search frequently asked questions by keyword or any of the specific fields defined in the OpenSearch URL.");
        faq.ProcessGenericRequest();
        
        context.CloseNewsDatabase();
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
