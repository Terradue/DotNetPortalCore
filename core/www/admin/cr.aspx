<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/cr.aspx
# Version:      2.3
# Description:  Provides the administration interface for managing computing resources
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
        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "ce.xsl";

        ComputingResource cr;

        int id = context.GetIdFromRequest();
        if (id == 0) {
        int extensionType = 0;
        Int32.TryParse(context.GetParamValue("_type"), out extensionType);
        if (extensionType == 0) cr = ComputingResource.GetInstance(context);
        else cr = ComputingResource.WithExtensionType(context, extensionType);
        } else {
        cr = ComputingResource.FromId(context, id);
        }

        cr.NoRestriction = true;
        cr.SetOpenSearchDescription("Computing Resources", "Computing resource search", "Search computing resources by keyword or any of the specific fields defined in the OpenSearch URL.");
        cr.ProcessGenericRequest();
        context.Close();

    } catch (Exception e) { 
        context.Close(e);
    }
}

</script>
