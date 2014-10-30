<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>

<script language="C#" runat="server">
/*
# Module:       sugar/portal-core
# Author:       Terradue Srl
# Last update:  2013-04-26
# File:         /account.aspx
# Version:      2.4
# Description:  Provides the user account interface
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
    context = IfyContext.GetWebContext(PagePrivileges.EverybodyView);
    try {
        context.Open();
        parts = context.ResourcePathParts;
        if (!context.IsUserAuthenticated && parts.Length >= 1) {
            if (parts[0] != "register" && parts[0] != "activate" && parts[0] != "signin" && parts[0] != "recover") context.RejectUnauthenticatedRequest();
        }
        
        string profileTarget = (context.IsUserAuthenticated ? "profile" : "register");

        if (parts.Length == 0 || (parts[0] == "profile" || parts[0] == "register") && parts[0] != profileTarget) { // Control panel home page
            context.Redirect(String.Format("{0}/{1}", context.AccountRootUrl, profileTarget), true, false);

        } else if (parts.Length >= 1) {
            context.XslFilename = Server.MapPath("/template/xsl/account.xsl");
            string key;
            Terradue.Portal.User user;
            
            switch (parts[0]) {

                case "register" :
                    if (parts.Length >= 2 && Request.QueryString["_state"] == "created") {
                        context.WriteInfo("Your account has been created");
                        break;
                    }
                    user = Terradue.Portal.User.GetInstance(context);
                    user.OwnAccount = true;
                    user.ViewingState = ViewingState.DefiningItem;
                    user.ProcessGenericRequest();
                    break;

                case "activate" :
                    key = Request["key"];
                    context.AuthenticateWithToken(key);
                    context.Redirect("/account/profile", true, false);
                    break;

                case "recover" :
                    key = Request["key"];
                    if (String.IsNullOrEmpty(key)) {
                        Terradue.Portal.User.GetInstance(context).RecoverPassword();
                    } else {
                        context.AuthenticateWithToken(key);
                        if (context.IsPasswordAllowedForUser()) context.AddInfo("Please enter a new password now");
                        user = Terradue.Portal.User.GetInstance(context);
                        user.OwnAccount = true;
                        user.ItemUrl = "/account/profile";
                        user.ProcessGenericRequest();
                    }
                    break;

                case "signin" :
                    string username = Request["username"];
                    string password = Request["password"];
                    if (!String.IsNullOrEmpty(username) != null && !String.IsNullOrEmpty(password) != null) {
                        if (context.LoginUser(username, password)) {
                            context.CheckAvailability(false);
                            context.WriteInfo("You are now logged in");
                        }
                    }
                    break;

                case "signout" :
                    if (true) {
                        context.LogoutUser();
                        context.WriteInfo("You are now logged out", "userLogOut");
                    }
                    break;

                case "profile" :
                    context.XslFilename = Server.MapPath("/template/xsl/account.xsl");
                    user = Terradue.Portal.User.GetInstance(context);
                    user.OwnAccount = true;
                    user.ProcessGenericRequest();
                    break;
                    
                case "filters" :
                    ProcessAccountFilterRequest();
                    break;

                case "publish-servers" :
                    ProcessAccountPublishServerRequest();
                    break;

                case "open-ids" :
                    ProcessAccountOpenIdRequest();
                    break;
                    
                default :
                    context.ReturnError("Invalid request");
                    break;
                
            }

        }
        
        context.Close();

    } catch (Exception e) {
        context.Close(e);
    }

}

//---------------------------------------------------------------------------------------------------------------------

void ProcessAccountFilterRequest() {
    if (parts.Length < 2) context.ReturnError("Invalid request");
    
    EntityType entityType = IfyContext.GetEntityTypeFromKeyword(parts[1]);
    if (entityType == null) context.ReturnError("Invalid request");
    
    context.ContentType = "text/xml";
    Filter filter = Filter.ForEntityType(context, entityType);
    
    filter.ProcessRequest();
}

//---------------------------------------------------------------------------------------------------------------------

void ProcessAccountPublishServerRequest() {
    int id = 0;
    if (parts.Length >= 2) Int32.TryParse(parts[1], out id);
    context.Privileges = PagePrivileges.UserEdit;
    context.XslFilename = Server.MapPath("/template/xsl/") + "pubserver.xsl";
    Terradue.Portal.PublishServer publishServer;
    publishServer = Terradue.Portal.PublishServer.GetInstance(context);
    publishServer.Restricted = false;
    publishServer.SetOpenSearchDescription("Publish Servers", "Publish server search", "Search publish servers by keyword or any of the specific fields defined in the OpenSearch URL.");
    publishServer.ProcessGenericRequest(id);
}

//---------------------------------------------------------------------------------------------------------------------

void ProcessAccountOpenIdRequest() {
    Terradue.Portal.UserOpenId userOpenId;
    userOpenId = Terradue.Portal.UserOpenId.GetInstance(context);
    
    context.Privileges = PagePrivileges.UserEdit;
    if (!userOpenId.ProcessRequest()) {
        if (context.IsUserAuthenticated && !userOpenId.MissingIdentifier) {
            userOpenId.WriteItemList();
        } else {
            context.Privileges = PagePrivileges.EverybodyView;
            userOpenId.WriteProviderList();
        }
    }
    
}

</script>
