<%@ Import Namespace="System" %>
<%@ Import Namespace="Terradue.Portal" %>
<%@ Import Namespace="Terradue.Umsso" %>

<script language="C#" runat="server">

void Page_Load(object sender, EventArgs ea) {
	IfyWebContext context = IfyContext.GetWebContext(PagePrivileges.EverybodyView);
	try {
		context.Open();
		context.StartXmlResponse();
//    for (int i = 0; i < Request.Headers.Count; i++) context.AddInfo(Request.Headers.GetKey(i) + " = " + Request.Headers.Get(i));
		if (context.IsUserAuthenticated) context.ReturnError("You are already a registered user.");
		if (!context.IsUserIdentified) context.ReturnError("Your have to sign in at the Single Sign-on service before registering.");
		
		context.XslFilename = Server.MapPath("/template/xsl/account.xsl");
		Terradue.Portal.User user = Terradue.Portal.User.GetInstance(context);
		user.OwnAccount = true;
		user.NoRedirect = true;
		user.ProcessGenericRequest();
		context.AddInfo("ID = " + context.UserId);
		if (context.UserId != 0) {
            if (UmssoUtils.BindToUmssoIdp(context, context.ExternalUsername, context.ExternalUsername, Request.Headers["Umsso-Transaction-Id"])) user.EnableUser();
		}

		context.Close();
		
	} catch (Exception e) { 
		context.Close(e); 
	}

}
</script>
