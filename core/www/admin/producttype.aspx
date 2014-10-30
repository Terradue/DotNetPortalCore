<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/producttype.aspx
# Version:      2.3
# Description:  Provides the administration interface for managing product types
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
        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "producttype.xsl";

        ProductType productType = ProductType.GetInstance(context);
        productType.NoRestriction = true;
        productType.SetOpenSearchDescription("Product types", "Product type search", "Search product types by keyword or any of the specific fields defined in the OpenSearch URL.");
        productType.ProcessGenericRequest();
        
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
