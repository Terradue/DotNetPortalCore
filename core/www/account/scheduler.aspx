<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/scheduler.aspx
# Version:      2.3
# Description:  Provides the user interface for controlling schedulers
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

        context.XslFilename = Server.MapPath("/template/xsl/") + "scheduler.xsl";
        if (context.Format=="timeline") {
            context.XslFilename = Server.MapPath("/template/xsl/") + "scheduler_timeline.xsl";
        }

        Scheduler scheduler;
        
        // If no ID was specified in request, write the user-specific scheduler list,
        // otherwise write the scheduler details
        scheduler = Scheduler.GetInstance(context);
        scheduler.SetOpenSearchDescription("Scheduler", "Scheduler search", "Search task schedulers by keyword or any of the specific fields defined in the OpenSearch URL.");
        if (context.GetParamValue("_request") == "description") scheduler.WriteOpenSearchDescription(false);
        else scheduler.ProcessRequest();

        context.Close();

    } catch (Exception e) { 
        context.Close(e);
    }
}

</script>
