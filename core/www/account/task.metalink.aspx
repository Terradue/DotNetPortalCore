<%@ Import Namespace="System" %>
<%@ Import Namespace="System.Xml" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-03-19
# File:         /account/task.metalink.aspx
# Version:      2.3
# Description:  Returns a metalink document of a task's results
#
# This document is the property of Terradue and contains information directly
# resulting from knowledge and experience of Terradue.
# Any changes to this code is forbidden without written consent from Terradue Srl
#
# Contact       info@terradue.com
*/

void Page_Load(object sender, EventArgs ea) {

    IfyWebContext context = IfyContext.GetWebContext(PagePrivileges.UserView);
    XmlDocument MetalinkDoc = new XmlDocument();
    XmlElement metalinkNode = MetalinkDoc.CreateElement("metalink","urn:ietf:params:xml:ns:metalink");
    MetalinkDoc.AppendChild(metalinkNode);
    try {
        
        context.Open();
        context.ContentType="application/metalink4+xml";
        context.StartXmlResponse();
        
        // Make script directory current directory
        string scriptName = Request.ServerVariables["PATH_TRANSLATED"];
        Environment.CurrentDirectory = scriptName.Substring(0, scriptName.LastIndexOf(Path.DirectorySeparatorChar));
        
        string uid = context.GetParamValue("uid");
        //context.XslFilename = Server.MapPath("/template/xsl/") + "task.xsl";
        
        // Init XML doc
        
        
        XmlElement published = MetalinkDoc.CreateElement("published");
        metalinkNode.AppendChild(published);
        
        Task task;
        
        // If no ID and no UID was specified in request, write the user-specific task list,
        // otherwise write the task details
        if (uid == null) {
           MetalinkDoc.WriteTo(context.XmlWriter);
           context.Close();
           return;
        }
        
        task = Task.FromUid(context, uid);
        task.NoRestriction = true;
        
        if( task == null ){
           MetalinkDoc.WriteTo(context.XmlWriter);
           context.Close();
           return;
        }
        
        //task.ProcessRequest();
        for (int i=0; i < task.Jobs.Count; i++){
           Job job = task.Jobs[i];
           if (job.Publishes) {
              job.ShowProcessings=true;
              job.GetStatus();
              for (int j=0; j < job.Processings.Count; j++){
                 XmlElement XmlResultNode = MetalinkDoc.CreateElement("RawResult");
                 XmlResultNode.InnerXml = job.Processings[j].ResultXml;
                 //metalinkNode.AppendChild(XmlResultNode);
                 XmlNodeList PublishedFileNodes = XmlResultNode.SelectNodes("PublishedResults/Location/File");
                 foreach (XmlElement PublishedFileNode in PublishedFileNodes){
                    XmlElement fileNode = MetalinkDoc.CreateElement("file");
                    metalinkNode.AppendChild(fileNode);
                       fileNode.SetAttribute("name",PublishedFileNode.InnerXml);
                       XmlElement sizeFileNode = MetalinkDoc.CreateElement("size");
                       fileNode.AppendChild(sizeFileNode);
                       sizeFileNode.InnerText = PublishedFileNode.GetAttribute("size");
                       XmlElement urlFileNode = MetalinkDoc.CreateElement("url");
                       fileNode.AppendChild(urlFileNode);
                       urlFileNode.InnerText = task.ResultUrl+PublishedFileNode.InnerXml;
                 }
              }
           }
        }

        MetalinkDoc.WriteTo(context.XmlWriter);
        context.Close();
        return;

    } catch (Exception e) { 
        context.Close(e);
    }
}

</script>
