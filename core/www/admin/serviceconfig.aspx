<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /admin/serviceconfig.aspx
# Version:      2.3
# Description:  Provides the administrator interface for managing user/group-specific
#               or general service parameter values
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

        int serviceId;
        Int32.TryParse(context.GetParamValue("service"), out serviceId);

        context.XslFilename = Server.MapPath("/template/xsl/admin/") + "default.xsl";

        ServiceParameterConfiguration serviceConfig;
        
        // If no ID and no UID was specified in request, write the user-specific task list,
        // otherwise write the task details
        
        if (serviceId == 0) {
        context.ReturnError("No service specified");
        return;
        } else {
        string paramName = context.GetParamValue("param");
        int subjectType;
        int subjectId = 0;
        Int32.TryParse(context.GetParamValue("type"), out subjectType);
        if (subjectType != 0) {
        Int32.TryParse(context.GetParamValue("subject"), out subjectId);
        if (subjectId == 0) Int32.TryParse(context.GetParamValue(subjectType == ConfigurationSubjectType.Group ? "group" : "user"), out subjectId);
        }
        
        if (paramName == null || subjectType == 0) {
        serviceConfig = ServiceParameterConfiguration.FromService(context, Service.FromId(context, serviceId));
        } else {
        Service service = Service.FromId(context, serviceId);
        RequestParameter param = new RequestParameter(context, service, paramName, "string", "string", "string");
        //context.AddError(param.Name + " " + service.Name + " " + subjectType + " " + subjectId);
        //serviceConfig = ServiceParameterConfiguration.FromService(context, Service.FromId(context, serviceId));
        serviceConfig = ServiceParameterConfiguration.FromServiceParameter(context, param, subjectType, subjectId);
        }
        }
        serviceConfig.ProcessRequest();

        context.Close();

    } catch (Exception e) { 
        context.Close(e); 
    }

}

</script>
