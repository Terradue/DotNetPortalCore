<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/task.job.parameter.aspx
# Version:      2.3
# Description:  Provides the interface for reviewing or changing the parameters of a task's jobs
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
        int id = context.GetIdFromRequest();

        context.XslFilename = Server.MapPath("/template/xsl/") + "parameter.xsl";
        
        Job job;

        if (id == 0) {
            context.ReturnError("No or invalid job ID provided", "invalidJob");
        } else {
            job = Job.FromId(context, id);
            job.ShowParameters = true;
            job.ProcessRequest();
        }

        context.Close();

    } catch (Exception e) { 
        context.Close(e);
    }
}

</script>
