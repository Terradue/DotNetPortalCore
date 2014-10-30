<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/task.aspx
# Version:      2.3
# Description:  Provides the user interface for controlling tasks
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
        context.AllowViewTokenList = true;
        context.Open();
        int id = context.GetIdFromRequest();
        string uid = context.GetParamValue("uid");

        context.XslFilename = Server.MapPath("/template/xsl/") + "task.xsl";
        
        Task task;
        
        // If no ID and no UID was specified in request, write the user-specific task list,
        // otherwise write the task details
        if (id == 0 && uid == null) {
            task = Task.GetInstance(context);
            task.PersistentFilters = true;
            task.SetOpenSearchDescription("Task", "Task search", "Search tasks by keyword or any of the specific fields defined in the OpenSearch URL.");
            task.ProcessRequest();

        } else {
            if (id == 0) task = Task.FromUid(context, uid);
            else task = Task.FromId(context, id);
            task.ProcessRequest();
        }

        context.Close();

    } catch (Exception e) { 
        context.Close();
    }
}

</script>
