<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>
<%@ Import Namespace="Terradue.Cloud" %>

<script language="C#" runat="server">
/*
#
# This document is the property of Terradue and contains information directly 
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
# 
# Contact               info@terradue.com
*/

void Page_Load(object sender, EventArgs ea) {

        IfyWebContext context = IfyContext.GetWebContext(PagePrivileges.AdminOnly);
        try {
                context.Open();
                context.XslFilename = Server.MapPath("/template/xsl/admin/") + "ce.xsl";

                CloudProvider cr;

                int id = context.GetIdFromRequest();
                if (id == 0) {
                        int extensionType = 0;
                        Int32.TryParse(context.GetParamValue("_type"), out extensionType);
                        if (extensionType == 0) cr = CloudProvider.GetInstance(context);
                        else cr = CloudProvider.WithExtensionType(context, extensionType);
                } else {
                        cr = CloudProvider.FromId(context, id);
                }

                //cr.NoRestriction = true;
                cr.SetOpenSearchDescription("Cloud Provider", "Cloud provider search", "Search cloud provider by keyword or any of the specific fields defined in the OpenSearch URL.");
                cr.ProcessGenericRequest();
                context.Close();

        } catch (Exception e) { 
                context.Close(e);
        }
}

</script>