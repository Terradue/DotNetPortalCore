<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/series.aspx
# Version:      2.3
# Description:  Provides the administration interface for managing actions of data collections
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
        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "series.xsl";

        Series series = Series.GetInstance(context);
        series.NoRestriction = true;
        series.SetOpenSearchDescription("Dataset series", "Dataset series search", "Search dataset series by keyword or any of the specific fields defined in the OpenSearch URL.");
        series.ProcessGenericRequest();
        
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
