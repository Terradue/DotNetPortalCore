<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-04-26
# File:         /admin.aspx
# Version:      2.4
# Description:  Provides the admin/manager interface
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact:      info@terradue.com
*/

IfyWebContext context;
string[] parts;

void Page_Load(object sender, EventArgs ea) {
    context = IfyContext.GetWebContext(PagePrivileges.ManagerOnly);
    context.AdminMode = true;
    try {
        context.Open();
        parts = context.ResourcePathParts;
        
        if (parts.Length == 0) { // Control panel home page
            context.XslFilename = Server.MapPath("/template/xsl/admin/") + "controlpanel.xsl";
            EntityType.GetInstance(context).ProcessGenericRequest();

        } else if (parts.Length >= 1 && parts[0] == IfyContext.GetEntityType(typeof(Task)).Keyword) { // Control panel task viewer
            ProcessAdminTaskRequest();
            
        } else if (parts.Length >= 1 && parts[0] == IfyContext.GetEntityType(typeof(Scheduler)).Keyword) { // Control panel scheduler viewer
            ProcessAdminSchedulerRequest();
            
        } else { // Control panel interfaces for all other entities
            ProcessAdminGenericEntityRequest();
        }
        
        context.Close();

    } catch (Exception e) {
        context.Close(e);
    }
    
}

//---------------------------------------------------------------------------------------------------------------------

void ProcessAdminGenericEntityRequest() {
    EntityType entityType = IfyContext.GetEntityTypeFromKeyword(parts[0]);
    if (entityType == null) context.ReturnError("Invalid request :" + parts[0]);
    string xslFilename = GetAdminXslFileName(entityType);
    context.XslFilename = String.Format("{0}{1}", xslFilename.Contains("/") ? String.Empty : Server.MapPath("/template/xsl/admin/"), xslFilename);
    Entity entity;

    bool useNewsDatabase = false;
    switch (parts[0]) {
        case "news" :
        case "images" :
        case "faqs" :
        case "projects" :
            useNewsDatabase = true;
            break;
    }
    if (useNewsDatabase) {
        try {
            context.OpenNewsDatabase();
        } catch (Exception) {
            context.ReturnError("News database not available");
        }
    }

    if (parts.Length == 1) { // Control Panel entity list
        context.StartXmlResponse();
        int extensionTypeId = 0;
        if (context.RequestedOperation == "define" || context.RequestedOperation == "create") Int32.TryParse(Request["_type"], out extensionTypeId);
        entity = (extensionTypeId == 0 || !entityType.HasExtensions ? entityType.GetEntityInstance(context) : entityType.GetEntityExtensionInstance(context, extensionTypeId));
        entity.SetOpenSearchDescription(entityType.Caption, "Search " + entityType.Caption, "Search by keyword or any of the specific fields defined in the OpenSearch URL.");
        entity.ProcessAdministrationRequest();

    } else {  // Control Panel entity item
        int id;
        Int32.TryParse(parts[1], out id);
        entity = entityType.GetEntityInstanceFromId(context, id);
        if (entity == null) entity = entityType.GetEntityInstance(context);
        entity.ProcessAdministrationRequest(id);
    }
    
    if (useNewsDatabase) {
        context.CloseNewsDatabase();
    }
}

//---------------------------------------------------------------------------------------------------------------------

void ProcessAdminTaskRequest() {
    context.XslFilename = Server.MapPath("/template/xsl/admin/") + "task.xsl";
    Task task;
    if (parts.Length == 1) {
        task = Task.GetInstance(context);
        task.PersistentFilters = true;
        task.SetOpenSearchDescription("Task", "Task search", "Search tasks by keyword or any of the specific fields defined in the OpenSearch URL.");
        task.ProcessRequest();
    } else {
        string identifier = parts[1];
        task = Task.FromIdentifier(context, identifier);
        
        if (parts.Length == 2) {
            task.ProcessRequest();
        } else {
            if (parts[2] == context.TaskWorkspaceJobDir) {
                if (parts.Length >= 4) {
                    string name = parts[3];
                    Job job = Job.OfTask(context, task, name);
                    bool details = (parts.Length == 4 || parts[4] == "details");
                    job.ShowParameters = !details;
                    job.ShowNodes = details;
                    
                    context.XslFilename = Server.MapPath("/template/xsl/") + (details ? "job.xsl" : "parameter.xsl");
                    job.ProcessRequest();
                } else {
                    context.ReturnError("Missing job reference");
                }
            } else if (parts[2] == "download") {
                string url = context.GetParamValue("url");
                if (url == null) context.ReturnError("No URL specified");
                task.ProvideDownload(url);
            } else {
                context.ReturnError("Invalid request");
            }
        }
    }
}

//---------------------------------------------------------------------------------------------------------------------

void ProcessAdminSchedulerRequest() {
    context.XslFilename = Server.MapPath("/template/xsl/") + "scheduler.xsl";
    
    if (parts.Length == 1) {
        Scheduler scheduler = Scheduler.GetInstance(context);
        scheduler.PersistentFilters = true;
        scheduler.SetOpenSearchDescription("Scheduler", "Scheduler search", "Search task schedulers by keyword or any of the specific fields defined in the OpenSearch URL.");
        scheduler.ProcessRequest();
    } else {
        string identifier = parts[1];
        Scheduler scheduler = Scheduler.FromIdentifier(context, identifier);
        
        if (parts.Length == 2) {
            scheduler.ProcessRequest();
        } else {
            if (parts[2] == "tasks") {
                context.XslFilename = Server.MapPath("/template/xsl/") + "workspace.xsl";
                Task task = Task.GetInstance(context);
                task.SetOpenSearchDescription("Task", "Task search", "Search tasks of this scheduler by keyword or any of the specific fields defined in the OpenSearch URL.");
                task.SchedulerId = scheduler.Id;
                task.ProcessRequest();
            } else if (parts[2] == "timeline") {
                context.XslFilename = Server.MapPath("/template/xsl/") + "scheduler_timeline.xsl";
                scheduler.ProcessRequest();
            }
        }
    }
}

string GetAdminXslFileName(EntityType entityType) {
    switch (entityType.Name) {
        case "config" : return "config.xsl";
        case "usr" : return "user.xsl";
        case "grp" : return "group.xsl";
        case "cr" : return "ce.xsl";
        case "series" : return "series.xsl";
        case "producttype" : return "producttype.xsl";
        case "pubserver" : return "pubserver.xsl";
        case "service" : return "service.xsl";
        case "article" : return "news.xsl";
        case "faq" : return "ce.xsl";
        default: return "default.xsl";
    }
}

//---------------------------------------------------------------------------------------------------------------------

</script>
