<%@ Import Namespace="System" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /service.parameter.aspx
# Version:      2.3
# Description:  Provides the user interface for managing personal values or files
#               for service parameters  
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
        context.ContentType = "text/xml";
        context.Open();
        string name = context.GetParamValue("name");
        RequestParameter serviceParam = new RequestParameter(context, Service.FromString(context, context.GetParamValue("service")), name);
        serviceParam.ProcessRequest();
        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }
}


</script>
