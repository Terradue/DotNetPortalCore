<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>
<%@ Import Namespace="Terradue.Util" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-04-26
# File:         /catalogue.aspx
# Version:      2.4
# Description:  Provides the catalogue interface
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact:      info@terradue.com
*/

IfyWebContext context;

void Page_Load(object sender, EventArgs ea) {

    context = IfyContext.GetWebContext(PagePrivileges.UserView);
    try {
        context.Open();
        string seriesName = context.ResourcePathParts.Length == 0 ? null : context.ResourcePath.Substring(1);

        context.XslFilename = Server.MapPath("/template/xsl/") + "series.xsl";
        context.StartXmlResponse();

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
        "series AS t LEFT JOIN catalogue AS t1 ON t.id_catalogue=t1.id",
        new FieldExpression[] {
                            new SingleValueExpression("link", "CONCAT(" + StringUtils.EscapeSql(context.ScriptRoot) + ", '/', t.identifier)", "string", "Name", null, FieldFlags.Both | FieldFlags.Attribute),
                            new SingleValueField("name", "identifier", "string", "Name", null, FieldFlags.Both | FieldFlags.Searchable | FieldFlags.SortAsc),
                            new SingleValueField("caption", "name", "identifier", "Identifier", null, FieldFlags.Both | FieldFlags.Searchable | FieldFlags.Custom),
                            new SingleValueField("descr", "description", "text", "Description", null, FieldFlags.Item | FieldFlags.Searchable | FieldFlags.Optional),
        new SingleValueExpression("catalogueDescription", "REPLACE(cat_description, '$(CATALOGUE)', t1.base_url)", "url", "Catalogue Description URL", null, FieldFlags.Item),
        new SingleValueExpression("catalogueTemplate", "REPLACE(cat_template, '$(CATALOGUE)', t1.base_url)", "url", "Catalogue URL Template", null, FieldFlags.Item),
                            new SingleValueField("icon_url", "icon_url", "url", "Icon/logo URL", null, FieldFlags.Both | FieldFlags.Optional),
        new MultipleReferenceField("services", services, "service_series", "Services accepting this dataset series", FieldFlags.Item | FieldFlags.Reduced)
        }
        )
        );

        series.CanCreate = false;
        series.CanDelete = false;
        series.CanModify = false;
        series.ShowItemLinks = true;
        series.Paging = false;
        series.ItemBaseUrl = "/catalogue";
        series.ShowItemLinks = false;
        
        series.SetOpenSearchDescription("Dataset Series", "Dataset Series Search", "Search dataset series by keyword or any of the specific fields defined in the OpenSearch URL.");
    
        series.WriteItemList();
        
        if (seriesName != null) series.WriteSingleItem(context.ResourcePath.Substring(1));
    

        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
