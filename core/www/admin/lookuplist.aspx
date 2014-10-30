<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/lookuplist.aspx
# Version:      2.3
# Description:  Provides the administration interface for managing value lookup lists
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

        int id = context.GetIdFromRequest();

        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "default.xsl";

        LookupList lookupList;
        
        // If no ID and no UID was specified in request, write the user-specific task list,
        // otherwise write the task details
        if (id == 0) {
        lookupList = LookupList.GetInstance(context);
        lookupList.SetOpenSearchDescription("Lookup lists", "Lookup list search", "Search lookup list by keyword or any of the specific fields defined in the OpenSearch URL.");

        } else {
        lookupList = LookupList.FromId(context, id);
        }
        lookupList.ProcessRequest();

        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }

}

</script>
