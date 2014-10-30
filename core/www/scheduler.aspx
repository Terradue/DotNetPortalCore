<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-04-26
# File:         /scheduler.aspx
# Version:      2.4
# Description:  Provides the scheduler workspace interface
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact:      info@terradue.com
*/

void Page_Load(object sender, EventArgs ea) {
    IfyWebContext context = IfyContext.GetWebContext(PagePrivileges.UserEdit);
    
    try {
        context.Open();
        string[] parts = context.ResourcePathParts; 
        
        context.XslFilename = Server.MapPath("/template/xsl/") + "scheduler.xsl";
        
        if (parts.Length == 0) {
            Scheduler scheduler = Scheduler.GetInstance(context);
            scheduler.SetOpenSearchDescription("Scheduler", "Scheduler search", "Search task schedulers by keyword or any of the specific fields defined in the OpenSearch URL.");
            scheduler.ProcessRequest();
        } else {
            string identifier = parts[0];
            Scheduler scheduler = Scheduler.FromIdentifier(context, identifier);
            
            if (parts.Length == 1) {
                scheduler.ProcessRequest();
            } else {
                if (parts[1] == "tasks") {
                    context.XslFilename = Server.MapPath("/template/xsl/") + "workspace.xsl";
                    Task task = Task.GetInstance(context);
                    task.SetOpenSearchDescription("Task", "Task search", "Search tasks of this scheduler by keyword or any of the specific fields defined in the OpenSearch URL.");
                    task.SchedulerId = scheduler.Id;
                    task.ProcessRequest();
                } else if (parts[1] == "timeline") {
                    context.XslFilename = Server.MapPath("/template/xsl/") + "scheduler_timeline.xsl";
                    scheduler.ProcessRequest();
                }
            }
        }
        
        context.Close();

    } catch (Exception e) {
        context.Close(e);
    }
}

</script>
