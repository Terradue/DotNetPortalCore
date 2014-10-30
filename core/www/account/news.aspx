<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/news.aspx
# Version:      2.3
# Description:  Displays news articles
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
        context.Open();
        context.XslFilename = Server.MapPath("/template/xsl/") + "news.xsl";
        try {
            context.OpenNewsDatabase();
        } catch (Exception) {
            context.ReturnError("News database not available");
        }
        
        Article list = Article.GetInstance(context);

        if (context.Format == "rss") context.XslFilename = Server.MapPath("/template/xsl/") + "rss_news.xsl";
        
        list.CanCreate = false;
        list.CanDelete = false;
        list.CanModify = false;
        list.FilterCondition = "t.time IS NOT NULL AND t.time<=NOW()";
        list.CustomSorting = "t.time DESC";
        list.SetOpenSearchDescription("Article", "Search News Articles ", "Search news by keyword or any of the specific fields defined in the OpenSearch URL.");
        list.ListUrl = context.HostUrl + "/news.aspx";
        list.ItemBaseUrl = list.ListUrl;
        // list.ExcludeIds = new int[] {1,12};
        int id = context.GetIdFromRequest();
        if (id == 0) {
            if (context.GetParamValue("request") == "description") list.WriteOpenSearchDescription(false);
            else list.WriteItemList();//"time DESC",10);
        } else {
            list.WriteSingleItem(id);
            list.ExcludeIds = new int[] {id};
            list.WriteItemList("time DESC",5);
        }
        context.CloseNewsDatabase();
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
