<%@ Import Namespace="System" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /wps.aspx
# Version:      2.3
# Description:  Provides the WPS interface
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact:      info@terradue.com
*/

const int PROCESS_TYPE_UNKNOWN = 0;
const int PROCESS_TYPE_WPS = 1;
const int PROCESS_TYPE_WPS_STATUS = 2;
const int PROCESS_TYPE_WPS_RESULT = 3;


void Page_Load(object sender, EventArgs ea) {
    IfyWebContext context = IfyContext.GetWebContext(PagePrivileges.EverybodyView);
    try {
        context.Open();
        context.ContentType = "text/xml";
        string[] parts = context.ResourcePathParts;
        
        bool error = false;
        int type = PROCESS_TYPE_UNKNOWN;
        
        if (parts.Length >= 1) {
            switch (parts[0]) {
                case "data" :
                    type = PROCESS_TYPE_WPS;
                    break;
                case "status" :
                    type = PROCESS_TYPE_WPS_STATUS;
                    break;
                case "result" :
                    type = PROCESS_TYPE_WPS_RESULT;
                    break;
                default :
                    break;
            }
        } else {
            type = PROCESS_TYPE_WPS;
        }
        
        if (type == PROCESS_TYPE_UNKNOWN || (type == PROCESS_TYPE_WPS_STATUS || type == PROCESS_TYPE_WPS_RESULT) && parts.Length < 2) {
            context.ReturnError("Invalid request");
        }

        Terradue.Portal.WpsApplication application;
        string xslDirectory = "/usr/local/terradue/portal/sites/gpod2/root/wps";
        
        switch (type) {
            case PROCESS_TYPE_WPS :
                application = Terradue.Portal.WpsApplication.FromName(context, "wps");
                application.UseEPSG4326BoundingBox = true;
                application.AtomMetadata = true;
                application.AbbreviatedAbstract = true;
                application.OutputFormatParameterName = "xsl";
                application.XslDirectory = xslDirectory;
                context.HideMessages = true;
                application.ProcessRequest();
                break;
            case PROCESS_TYPE_WPS_STATUS :
                application = Terradue.Portal.WpsApplication.FromName(context, "wps");
                application.XslDirectory = xslDirectory;
                application.LoadTask(parts[1]);
                if (!application.Error) application.WriteExecuteResponse();
                break;
            case PROCESS_TYPE_WPS_RESULT :
                Task task = Task.FromUid(context, parts[1]);
                string xslTransformation = (parts.Length < 3 || parts[2] == "rdf" ? null : parts[2]);
                string xslFile = (xslTransformation == null ? null : String.Format("{0}/{1}.xsl", xslDirectory, xslTransformation));
                if (xslTransformation != null && !File.Exists(xslFile)) context.ReturnError("Requested output format not supported");
                context.ContentType = (xslFile == null ? "application/rdf+xml" : IfyWebContext.GetXslMediaType(xslFile));
                context.StartXmlResponse();
                WpsApplication.WriteTaskResult(task, xslFile, context.XmlWriter);
                break;
        }
        context.Close();

    } catch (Exception e) {
        context.Close(e);
    }
}

</script>
