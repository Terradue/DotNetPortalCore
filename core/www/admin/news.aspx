<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/news.aspx
# Version:      2.3
# Description:  Provides the administration interface for managing news articles
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
        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "news.xsl";
        try {
        context.OpenNewsDatabase();
        } catch (Exception) {
        context.ReturnError("News database not available");
        }

        Article article = Article.GetInstance(context);
        article.SetOpenSearchDescription("News articles", "News article search", "Search news articles by keyword or any of the specific fields defined in the OpenSearch URL.");

        EntityData commentsData = new EntityData(
        "Comments", 
        "articlecomment",
        new FieldExpression[] {
        new SingleValueField("author", "string", "Author", FieldFlags.Both | FieldFlags.Searchable),
        new SingleValueField("email", "email", "E-mail", FieldFlags.Both),
        new SingleValueField("country", "countries", "Country", FieldFlags.Both),
        new SingleValueField("time", "datetime", "Date", FieldFlags.Both),
        new SingleValueField("ip", "ip", "IP Address", FieldFlags.Both),
        new SingleValueField("comments", "string", "Comment", FieldFlags.Both | FieldFlags.Searchable),
        }
        );

        Entity comments = Entity.GetInstance(context, commentsData);

        article.Data = new EntityData(
        "News articles",
        "article",
        new FieldExpression[] {
        new SingleValueField("title", "string", "Title", FieldFlags.Both | FieldFlags.Searchable),
        new SingleValueField("abstract", "text", "Abstract", FieldFlags.Both | FieldFlags.Searchable),
        new SingleValueField("content", "text", "Content", FieldFlags.Item | FieldFlags.Searchable),
        new SingleValueField("time", "datetime", "Date and time", FieldFlags.Both | FieldFlags.Optional),
        new SingleValueField("url", "url", "Article URL", FieldFlags.Item | FieldFlags.Optional),
        new SingleValueField("author", "string", "Author", FieldFlags.Item | FieldFlags.Optional),
        new SingleValueField("tags", "string", "Tags", FieldFlags.Item | FieldFlags.Optional)
        //,
        //new MultipleEntityField("comments", comments, "id_article", "Comments", FieldFlags.Item | FieldFlags.Optional)
        }
        );
        article.ShowItemIds = true;
        article.ShowItemLinks = true;
        article.CustomSorting = "time DESC";

        article.ProcessGenericRequest();
        
        context.CloseNewsDatabase();
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
