<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/task.download.aspx
# Version:      2.3
# Description:  Redirects to the download URL of a task's result file
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
        string url = context.GetParamValue("url");
        
        Task task;
        
        if (url == null) {
            context.ReturnError("No URL specified");

        } else {
            task = Task.GetInstance(context);
            task.ProvideDownload(url);
        }

        context.Close();

    } catch (Exception e) { 
        context.Close(e);
    }
}

</script>
