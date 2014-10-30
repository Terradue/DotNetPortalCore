<%@ Import Namespace="System" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-04-26
# File:         /task.aspx
# Version:      2.4
# Description:  Provides the task workspace interface
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
        
        context.XslFilename = Server.MapPath("/template/xsl/") + "workspace.xsl";
        Task task;

        if (parts.Length == 0) {
            task = Task.GetInstance(context);
            task.SetOpenSearchDescription("Task", "Task search", "Search tasks by keyword or any of the specific fields defined in the OpenSearch URL.");
            task.ProcessRequest();

        } else if (parts[0] == "download") {
            task = Task.GetInstance(context);
            string url = context.GetParamValue("url");
            if (url == null) context.ReturnError("No URL specified");
            task.ProvideDownload(url);
            
        } else {
            string identifier = parts[0];
            task = Task.FromIdentifier(context, identifier);
            
            if (parts.Length == 1) {
                task.ProcessRequest();
            } else {
                if (parts[1] == context.TaskWorkspaceJobDir) {
                    if (parts.Length >= 3) {
                        string name = parts[2];
                        Job job = Job.OfTask(context, task, name);
                        bool details = (parts.Length == 3 || parts[3] == "details");
                        job.ShowParameters = !details;
                        job.ShowNodes = details;
                        
                        context.XslFilename = Server.MapPath("/template/xsl/") + (details ? "details.xsl" : "parameter.xsl");
                        job.ProcessRequest();
                    } else {
                        context.ReturnError("Missing job reference");
                    }
                    
                } else if (parts[1] == "rdf") {
                    context.ContentType = "text/xml";
                    task.DownloadProviderUrl = "/tasks/download/";
                    context.StartXmlResponse();
                    task.WriteResultRdf();
                    
                } else if (parts[1] == "metalink") {
                    XmlDocument metalinkDoc = new XmlDocument();
                    XmlElement metalinkNode = metalinkDoc.CreateElement("metalink","urn:ietf:params:xml:ns:metalink");
                    metalinkDoc.AppendChild(metalinkNode);
                    for (int i = 0; i < task.Jobs.Count; i++) {
                        Job job = task.Jobs[i];
                        if (!job.Publishes) continue;
                        job.ShowNodes = true;
                        job.GetStatus();
                        for (int j = 0; j < job.Nodes.Count; j++) {
                            XmlElement XmlResultNode = metalinkDoc.CreateElement("RawResult");
                            XmlResultNode.InnerXml = job.Nodes[j].ResultXml;
                            //metalinkNode.AppendChild(XmlResultNode);
                            XmlNodeList publishedFileNodes = XmlResultNode.SelectNodes("PublishedResults/Location/File");
                            foreach (XmlElement PublishedFileNode in publishedFileNodes) {
                                XmlElement fileNode = metalinkDoc.CreateElement("file");
                                metalinkNode.AppendChild(fileNode);
                                fileNode.SetAttribute("name", PublishedFileNode.InnerXml);
                                XmlElement sizeFileNode = metalinkDoc.CreateElement("size");
                                fileNode.AppendChild(sizeFileNode);
                                sizeFileNode.InnerText = PublishedFileNode.GetAttribute("size");
                                XmlElement urlFileNode = metalinkDoc.CreateElement("url");
                                fileNode.AppendChild(urlFileNode);
                                urlFileNode.InnerText = task.ResultUrl + PublishedFileNode.InnerXml;
                            }
                        }
                    }
                    context.ContentType="application/metalink4+xml";
                    context.StartXmlResponse();
                    metalinkDoc.WriteTo(context.XmlWriter);
                    
                } else {
                    context.ReturnError("Invalid request");
                }
            }
        }
        
        context.Close();

    } catch (Exception e) {
        context.Close(e);
    }
}

</script>
