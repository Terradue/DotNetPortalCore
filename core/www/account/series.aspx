<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>
<%@ Import Namespace="Terradue.Util" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/series.aspx
# Version:      2.3
# Description:  Lists the data collections (series) accessible by the current user
#               and provides an interface for querying the related catalogue
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact       info@terradue.com
*/

void Page_Load(object sender, EventArgs ea) {

    IfyWebContext context = IfyContext.GetWebContext(PagePrivileges.UserView);
    try {
        context.Open();
        context.XslFilename = Server.MapPath("/template/xsl/") + "series.xsl";
        
        Service services = Service.GetInstance(
                context,
                new EntityData(
                        "Service",
                        "service",
                        new FieldExpression[] {
                                new SingleValueField("value", "id", "id", null, null, FieldFlags.Both | FieldFlags.Searchable),
                                new SingleValueExpression("link", "CONCAT('" + context.HostUrl + "', REPLACE(root, '$(SERVICEROOT)', " + StringUtils.EscapeSql(context.ServiceWebRoot) + "))", null, null, null, FieldFlags.Both | FieldFlags.Searchable),
                                new SingleValueField("text", "name", "identifier", "Identifier", null, FieldFlags.Both | FieldFlags.Searchable),
                        }
                )
        );
        
        Series series = Series.GetInstance(
                context, 
                new EntityData(
                        "Dataset series",
                        "series",
                        new FieldExpression[] {
                            new SingleValueField("name", "identifier", "Identifier", FieldFlags.Both | FieldFlags.Searchable),
                            new SingleValueField("caption", "string", "Caption", FieldFlags.Both | FieldFlags.Searchable | FieldFlags.SortAsc),
                            new SingleValueField("descr", "text", "Description", FieldFlags.Both | FieldFlags.Searchable),
                            new SingleValueExpression("catalogueDescription", "REPLACE(cat_description, '$(CATALOGUE)', " + StringUtils.EscapeSql(context.DefaultCatalogueBaseUrl) + ")", "url", "Catalogue Description URL", null, FieldFlags.Item),
                            new SingleValueExpression("catalogueTemplate", "REPLACE(cat_template, '$(CATALOGUE)', " + StringUtils.EscapeSql(context.DefaultCatalogueBaseUrl) + ")", "url", "Catalogue URL Template", null, FieldFlags.Item),
                            new SingleValueField("logo_url", "url", "Logo URL", FieldFlags.Both),
                            new MultipleReferenceField("services", services, "service_series", "Services accepting this dataset series", FieldFlags.Item | FieldFlags.Reduced)
                        }
                )
        );

        series.CanCreate = false;
        series.CanDelete = false;
        series.CanModify = false;
        series.ShowItemLinks = true;
        
        series.SetOpenSearchDescription("Dataset Series", "Dataset Series Search", "Search dataset series by keyword or any of the specific fields defined in the OpenSearch URL.");
        
        series.GetIdFromRequest();
        series.WriteItemList();
        if (series.Id != 0) series.WriteSingleItem();
        
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
