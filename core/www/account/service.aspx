<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/service.aspx
# Version:      2.3
# Description:  Lists the services accessible by the current user
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
        
        context.AllowViewTokenList = true;
        context.Open();
        if (context.Format != "rss" && !context.IsUserAuthenticated) {
            context.RejectUnauthenticatedRequest();
        } else {
            context.XslFilename = Server.MapPath("/template/xsl/") + "services.xsl";
            
            Service service = Service.GetInstance(context);
            service.OnlyAvailable = true;
            service.SetOpenSearchDescription("Service", "Service search", "Search processing services by keyword or any of the specific fields defined in the OpenSearch URL.");
            if (context.Format == "rss") {
                context.XslFilename = Server.MapPath("/template/xsl/") + "rss_services.xsl";
                service.Data = new EntityData(
                        "Service",
                        "service",new FieldExpression[] {
                            new SingleValueExpression("link", "CONCAT('" + context.ScriptName + "?id=', t.id)", "url", "Link", null, FieldFlags.Both | FieldFlags.Attribute),
                            new SingleValueField("caption", "string", "Caption", FieldFlags.Both | FieldFlags.Searchable | FieldFlags.SortAsc),
                            new SingleValueField("name", "identifier", "Identifier", FieldFlags.Both | FieldFlags.Searchable | FieldFlags.Unique),
                            new SingleValueField("description", "descr", "text", "Description", null, FieldFlags.Both | FieldFlags.Searchable),
                            new SingleValueField("available", "bool", "Available", FieldFlags.Both | FieldFlags.Custom),
                            new SingleValueField("version", "string", "Version2", FieldFlags.Both | FieldFlags.Optional),
                            new SingleValueField("modified", "datetime", "Last modification", FieldFlags.Both),
                            new SingleValueField("rating", "rating", "rating", "Rating", "ify:rating", FieldFlags.Both | FieldFlags.Optional | FieldFlags.Lookup), 
                            new SingleReferenceField("class", "serviceclass", "@.caption", "id_class", "Class", "ify:serviceClass", FieldFlags.Item | FieldFlags.Optional | FieldFlags.Optional),
                            new SingleValueField("logoUrl", "logo_url", "url", "Logo URL", null, FieldFlags.Item | FieldFlags.Optional),
                            new SingleValueField("viewUrl", "view_url", "url", "View URL", null, FieldFlags.Item | FieldFlags.Optional)
                        }
                ); 
             
                service.NoRestriction = true;
            }

            if (context.GetParamValue("request") == "description") service.WriteOpenSearchDescription(false);
            else service.WriteItemList();
    
        }
        context.Close();

    } catch (Exception e) { 
        context.Close(e);
    }
}

</script>
