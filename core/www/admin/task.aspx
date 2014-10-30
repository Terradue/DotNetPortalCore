<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/task.aspx
# Version:      2.3
# Description:  Provides the administration interface for controlling tasks
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
        context.AllowViewTokenList = true;
        context.Open();

        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "task.xsl";
        
        Task task;
        
        // If no ID and no name was specified in request, write the user-specific task list,
        // otherwise write the task details
        task = Task.GetInstance(context);
        task.NoRestriction = true;
        task.PersistentFilters = true;
        task.SetOpenSearchDescription("Task", "Task search", "Search tasks by keyword or any of the specific fields defined in the OpenSearch URL.");
        if (context.GetParamValue("_request") == "description") task.WriteOpenSearchDescription(false);
        else task.ProcessRequest();

        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}

</script>
