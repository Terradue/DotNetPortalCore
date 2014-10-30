<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/task.rdf.aspx
# Version:      2.3
# Description:  Returns an RDF document of a task's results
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
        string uid = context.GetParamValue("uid");
        
        Task task;
        
        // If an ID or a UID was specified in request write the task result RDF
        if (uid == null) {
            context.ReturnError("No task specified");

        } else {
            task = Task.FromUid(context, uid);
            context.ContentType = "text/xml";
            task.DownloadProviderUrl = "/tasks/download/";
            task.WriteResultRdf();
        }

        context.Close();

    } catch (Exception e) { 
        context.Close(e);
    }
}

</script>
