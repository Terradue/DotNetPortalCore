using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Xsl;
using MySql.Data.MySqlClient;
using Terradue.Util;





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





namespace Terradue.Portal {



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    public interface IResponseNonceStorage {
        bool Contains(DateTime nonceTime, string randomPart);
        void Add(DateTime nonceTime, string randomPart);
    }

    /// <summary>
    /// Ify web context.
    /// </summary>
    /// \ingroup Context
    public class IfyWebContext : IfyContext, IInputSource, IResponseNonceStorage {
        private static EntityDictionary<AuthenticationType> authenticationTypes;
        public static PasswordAuthenticationType passwordAuthenticationType;

        //---------------------------------------------------------------------------------------------------------------------

        public const string OperationParameterName = "_request";
        public const string ListTokenParameterName = "_list";
        public const string FormatParameterName = "_format";
        
        private static bool isWebSiteAvailable;
        private static string[] trustedHosts;
        private static bool allowExternalAuthentication;
        private static bool allowSelfRegistration;
        private static int accountActivationRule;
        private static RuleApplicationType allowPassword;
        private static int allowOpenId;
        private static RuleApplicationType allowSessionless;
        private static string userCertificateServerVariable;

        //---------------------------------------------------------------------------------------------------------------------

        public static new bool ConfigurationLoaded { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public override bool IsInteractive {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual bool IsWebSiteAvailable {
            get { return isWebSiteAvailable; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual string[] TrustedHosts {
            get { return trustedHosts; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual bool AllowExternalAuthentication {
            get { return allowExternalAuthentication; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string[] DisabledProfileFields {
            get { return disabledProfileFields; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual bool AllowSelfRegistration {
            get { return allowSelfRegistration; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual int AccountActivationRule {
            get { return accountActivationRule; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual RuleApplicationType AllowPassword {
            get { return allowPassword; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual int AllowOpenId {
            get { return allowOpenId; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual RuleApplicationType AllowSessionless {
            get { return allowSessionless; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string UserCertificateServerVariable {
            get { return userCertificateServerVariable; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string AdminRootUrl {
            get { return adminRootUrl; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string AccountRootUrl {
            get { return accountRootUrl; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string TaskWorkspaceRootUrl {
            get { return taskWorkspaceRootUrl; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string TaskWorkspaceJobDir {
            get { return taskWorkspaceJobDir; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string SchedulerWorkspaceRootUrl {
            get { return schedulerWorkspaceRootUrl; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string TaskFlowUrl {
            get { return taskFlowUrl; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual string DefaultXslFile {
            get { return defaultXslFile; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public int DefaultAccountStatus {
            get {
                int result = AccountStatusType.Disabled;
                switch (AccountActivationRule) {
                    case AccountActivationRuleType.ActiveAfterApproval :
                        result = AccountStatusType.PendingActivation;
                        break;
                    case AccountActivationRuleType.ActiveImmediately :
                        result = AccountStatusType.Enabled;
                        break;
                }
                return result;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected IDbConnection NewsDbConnection { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        //protected bool Certificate { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool SimplifiedGui { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the total credits of the logged or impersonated user.</summary>
        public double TotalCredits { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the unused credits of the logged or impersonated user. </summary>
        public double AvailableCredits { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the string representing the remaining proxy life time. </summary>
        public string RemainingProxyTime { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string Format { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public PagePrivileges Privileges { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool UsesUrlRewriting { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string ScriptRoot { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string ResourcePath { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string[] ResourcePathParts { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string ScriptName { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string HostName {
            get { return HttpContext.Current.Request.ServerVariables["SERVER_NAME"]; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string ScriptUrl { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string ContentType {
            get { return contentType; }
            set {
                xslFilename = null;
                if (responseStarted) return; //throw new InvalidOperationException("The response has already started"); // !!!
                contentType = value;
                customOutput = true;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string XslFilename {
            get { return xslFilename; }
            set { xslFilename = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public MonoXmlWriter XmlWriter {
            get { return xmlWriter; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public bool SkipChecks { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public bool CloseOnError { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool AllowViewTokenList { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public string RequestedOperation {
            get { return OperationIdentifier; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string OperationIdentifier {
            get { return GetParamValue(OperationParameterName); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string HttpMethod {
            get { return HttpContext.Current.Request.HttpMethod; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string ListToken {
            get { return GetParamValue(ListTokenParameterName); }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new IfyWebContext instance for interactive web applications.</summary>
        /// <param name="pagePrivileges">the set of privileges for the current page</param>
        public IfyWebContext(PagePrivileges privileges) : base(System.Configuration.ConfigurationManager.AppSettings["DatabaseConnection"]) {
            this.Privileges = privileges;
            if (HttpContext.Current != null) {
                // this.scriptName = HttpContext.Current.Request.ServerVariables["SCRIPT_NAME"];
                // this.scriptUrl = (HttpContext.Current.Request.ServerVariables["HTTPS"] == "on" ? "https://" : "http://") + HttpContext.Current.Request.ServerVariables["SERVER_NAME"] + this.scriptName;
                string hostname = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_HOST"];
                if (hostname == null || hostname == String.Empty)hostname = HttpContext.Current.Request.ServerVariables["HTTP_HOST"];

                //string port = HttpContext.Current.Request.ServerVariables["SERVER_PORT"];
                
                HostUrl = (HttpContext.Current.Request.IsSecureConnection ? "https://" : "http://") + hostname;

                //ScriptRoot = HttpContext.Current.Request.FilePath;
                ScriptRoot = HttpContext.Current.Request["T2_SCRIPT_ROOT"];
                if (ScriptRoot != null) {
                    UsesUrlRewriting = true;
                    //ResourcePath = HttpContext.Current.Request.PathInfo;
                    ResourcePath = HttpContext.Current.Request["T2_RESOURCE_PART"];
                    if (!String.IsNullOrEmpty(ResourcePath)) {
                        if (ResourcePath.Length == 0 || ResourcePath[0] != '/') ResourcePath = "/" + ResourcePath;
                        ResourcePath = Regex.Replace(ResourcePath, "//+", "/");
                        ResourcePath = Regex.Replace(ResourcePath, "/+$", String.Empty);
                    }
                    if (String.IsNullOrEmpty(ResourcePath)) {
                        ResourcePath = "/";
                        ResourcePathParts = new string[0];
                        ScriptName = ScriptRoot;
                    } else {
                        ResourcePathParts = ResourcePath.Substring(1).Split('/');
                        ScriptName = ScriptRoot + ResourcePath;
                    }
                } else {
                    ResourcePathParts = new string[0];
                    ScriptName = HttpContext.Current.Request.Path;
                    if (ScriptName.EndsWith("index.aspx")) ScriptName = ScriptName.Substring(0, ScriptName.Length - 10); // !!! ??? put into property with lazy evaluation?
                }
                ScriptUrl = HostUrl + ScriptName;
                queryString = HttpContext.Current.Request.ServerVariables["QUERY_STRING"];
                if (queryString != null) {
                    if (queryString.StartsWith("T2_")) queryString = "&" + queryString;
                    queryString = Regex.Replace(queryString, "[&\\?]T2_[^=]+=[^&]+", String.Empty);
                    if (queryString.StartsWith("&")) queryString = queryString.Substring(1);
                }
                Format = GetParamValue(FormatParameterName);
                bool noQuery = (queryString == String.Empty);
                if (noQuery || !queryString.StartsWith("?")) queryString = "?" + queryString;
                if (Format == null) queryString = queryString.Replace("?", "?" + FormatParameterName + "=xml" + (noQuery ? "" : "&"));
                //this.postRequest = (HttpContext.Current.Request.HttpMethod == "POST");
                //this.jsonResponse = (GetParamValue(FormatParameterName) == "json");
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Opens the database connection and performs the necessary checks on the user.</summary>
        public override void Open() {
            // In case of interactive web application, set XSL file if necessary and check whether the user session is valid (1) or whether a sessionless request is allowd (2)
            // In case of external application, read the application metadata from the database (3)
            bool open = false;
            
            if (!ConfigurationLoaded) {
                base.Open(); // this loads all configuration variables immediately
                open = true;
            }
            
            switch (Format) {
                case "xml" :
                    break;
                default :
                    if (contentType == null && DefaultXslFile != null) xslFilename = DefaultXslFile; // HttpContext.Current.Server.MapPath("/template/xsl/default.xsl");
                    break;
            }
            
            IsUserAuthenticated = CheckUserSession();

            if (!open) base.Open();

            int level = 0;
            Int32.TryParse(GetParamValue("_debug"), out level);
            
            // if (level != 0) DebugLevel = level; //!!!

            // Check web portal availability
            //if (passwordExpired && !NoRedirectIfExpired) RefreshPasswordExpiration(true, DateTime.MinValue);

            if (!IsUserAuthenticated) {
                User user = AuthenticateAutomatically();
                if (user != null) IsUserAuthenticated = true;
            }
            
            if (!SkipChecks) {
                CheckAvailability();
                if (!CheckAuthorization()) {
                    if (IsUserAuthenticated) RejectUnauthorizedRequest();
                    else RejectUnauthenticatedRequest();
                }

                /*if (!authenticated && UserLevel < Privileges.MinUserLevelView) RejectUnauthenticatedRequest();
                else if (Privileges.MinUserLevelView == Terradue.Portal.UserLevel.Administrator && UserLevel < Terradue.Portal.UserLevel.Administrator) RejectUnauthorizedRequest();*/
            }
            
            if (level != 0 && (UserLevel == Terradue.Portal.UserLevel.Administrator || HttpContext.Current.Request.UserHostAddress == "127.0.0.1")) DebugLevel = level;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected override void LoadAdditionalConfiguration() {
            base.LoadAdditionalConfiguration();
            string value;
            isWebSiteAvailable = GetConfigBooleanValue("Available");
            value = GetConfigValue("TrustedHosts");
            trustedHosts = (value == null ? new string[0] : value.Replace(" ", String.Empty).Split(','));
            allowExternalAuthentication = GetConfigBooleanValue("ExternalAuthentication");
            value = GetConfigValue("DisabledProfileFields");
            disabledProfileFields = (value == null ? new string[0] : value.Replace(" ", String.Empty).Split(','));
            allowSelfRegistration = GetConfigBooleanValue("AllowSelfRegistration");
            accountActivationRule = GetConfigIntegerValue("AccountActivation");
            allowPassword = ToRuleApplicationType(GetConfigIntegerValue("AllowPassword"));
            allowOpenId = GetConfigIntegerValue("AllowOpenId");
            allowSessionless = ToRuleApplicationType(GetConfigIntegerValue("AllowSessionless"));
            userCertificateServerVariable = GetConfigValue("ClientCertVerify");
            adminRootUrl = GetConfigValue("AdminRootUrl");
            accountRootUrl = GetConfigValue("AccountRootUrl");
            taskWorkspaceRootUrl = GetConfigValue("TaskWorkspaceRootUrl");
            taskWorkspaceJobDir = GetConfigValue("TaskWorkspaceJobDir");
            if (taskWorkspaceJobDir == null) taskWorkspaceJobDir = "jobs";
            schedulerWorkspaceRootUrl = GetConfigValue("SchedulerWorkspaceRootUrl");
            taskFlowUrl = GetConfigValue("TaskFlowUrl");
            defaultXslFile = GetConfigValue("DefaultXslFile");

            authenticationTypes = new EntityDictionary<AuthenticationType>(this);
            authenticationTypes.Load();
            foreach (AuthenticationType authenticationType in authenticationTypes) {
                if (authenticationType is PasswordAuthenticationType) {
                    passwordAuthenticationType = authenticationType as PasswordAuthenticationType;
                    break;
                }
            }

            ConfigurationLoaded = true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Closes all open database connection and releases other resources.</summary>
        public override void Close() {
            if (responseStarted) {
                if (textOutput) EndTextResponse(); else EndXmlResponse();
            }

            base.Close();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Closes all open database connection and releases other resources taking also into account the specified exception.</summary>
        /// <param name="e">The exception to take into account.</param>
        /// <remarks>The exception parameter is ignored in this class; derived classes, however, may have a different behaviour.</remarks>
        public virtual void Close(Exception e) {
            Close();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static AuthenticationType GetAuthenticationType(Type type) {
            foreach (AuthenticationType authenticationType in authenticationTypes) {
                if (type.IsAssignableFrom(authenticationType.GetType())) return authenticationType;
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the incoming request orginates from a sufficiently authenticated user by sequentially trying several methods</summary>
        /// <remarks>
        ///     The method loops over the authentication types and checks for each enabled authentication type whether user is recocgnised
        /// </remarks>
        /// <returns>true if the request is</returns>
        public User AuthenticateAutomatically() {
            AuthenticationType selectedAuthenticationType = null;
            User identifiedUser = null;

            foreach (AuthenticationType authenticationType in authenticationTypes) {
                if (!authenticationType.IsEnabled) continue;

                User user = authenticationType.GetUserProfile(this, HttpContext.Current.Request, false);
                if (user == null) continue;

                // Existing users recognised by external authentication providers must be already linked to that authentication provider
                // (avoiding account conflicts between different authentication providers)
                if (authenticationType.UsesExternalIdentityProvider && user.Exists && GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM usr_auth WHERE id_usr={0} AND id_auth={1};", user.Id, authenticationType.Id)) == 0) continue;

                selectedAuthenticationType = authenticationType;
                identifiedUser = user;
                break;
            }

            if (identifiedUser == null) {
                if (Privileges.MinUserLevelView > Terradue.Portal.UserLevel.Everybody) RejectUnauthenticatedRequest(true);
            } else {
                bool isNewUser = !identifiedUser.Exists;

                if (isNewUser || selectedAuthenticationType.AlwaysRefreshAccount) {
                    string authUsername = identifiedUser.Username;
                    identifiedUser.Store();
                    if (isNewUser && selectedAuthenticationType.UsesExternalIdentityProvider) {
                        identifiedUser.LinkToAuthenticationProvider(selectedAuthenticationType, authUsername);
                    }
                }

                if (selectedAuthenticationType.SetAuthorizations(identifiedUser, HttpContext.Current.Request, isNewUser)) {
                    // do nothing
                } else {
                    // Assign default groups to user if user has no groups assigned yet
                    int groupCount = (GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM usr_grp WHERE id_usr={0};", identifiedUser.Id)));
                    if (groupCount == 0) Execute(String.Format("INSERT INTO usr_grp (id_usr, id_grp) SELECT {0}, t.id FROM grp AS t WHERE is_default;", identifiedUser.Id));
                }

                if (isNewUser && AutomaticUserMails && identifiedUser.AccountStatus == AccountStatusType.PendingActivation) identifiedUser.SendMail(UserMailType.Registration, true);
                if (Privileges.MinUserLevelView > Terradue.Portal.UserLevel.Everybody) StartSession(selectedAuthenticationType, identifiedUser);
            }

            return identifiedUser;
        }

        //---------------------------------------------------------------------------------------------------------------------
/*
        /// <summary>Checks whether a user session is open and retrieves the relevant user information.</summary>
        /// <returns><b>true</b> if a session is active</returns>
        public User GetUserProfile_SingleSignOn(IfyWebContext context, HttpRequest request) {
            string ConfigurationFile = SiteConfigFolder + Path.DirectorySeparatorChar + "auth.xml";
            XmlDocument authDoc = new XmlDocument();
            
            try {
                authDoc.Load(ConfigurationFile);
                foreach (XmlNode typeNode in authDoc.SelectNodes("/externalAuthentication/method[@active='true']/accountType")) {
                    XmlElement methodElem = typeNode.ParentNode as XmlElement;
                    XmlElement typeElem = typeNode as XmlElement;
                    if (typeElem == null || methodElem == null) continue;

                    // The received "Host" header must match exactly the value of the "host" attribute.
                    if (methodElem.HasAttribute("host") && methodElem.Attributes["host"].Value != HttpContext.Current.Request.Headers["Host"]) continue;
                    
                    // The request origin ("REMOTE_HOST" server variable) must have (or be) the same IP address as the hostname (or IP address) in the value of the "remoteHost" attribute.
                    if (methodElem.HasAttribute("remoteHost") && !IsRequestFromHost(methodElem.Attributes["remoteHost"].Value)) continue;

                    bool match = true;
                    foreach (XmlNode conditionNode in typeElem.SelectNodes("condition")) {
                        XmlElement conditionElem = conditionNode as XmlElement;
                        if (conditionElem == null) continue;

                        string value = null, pattern = null;
                        if (conditionElem.HasAttribute("header")) value = HttpContext.Current.Request.Headers[conditionElem.Attributes["header"].Value];
                        else continue;

                        if (conditionElem.HasAttribute("pattern")) pattern = conditionElem.Attributes["pattern"].Value;
                        else continue;
                        
                        if (value == null || pattern == null) continue;

                        if (!Regex.Match(value, pattern).Success) {
                            match = false;
                            break;
                        }
                    }
                    if (!match) continue;
                    
                    XmlElement loginElem = typeElem["login"];
                    if (loginElem == null) continue;
                    
                    // Get username from <login> element
                    string externalUsername = null;
                    if (loginElem.HasAttribute("header")) externalUsername = HttpContext.Current.Request.Headers[loginElem.Attributes["header"].Value];

                    if (externalUsername == null) continue;
                    IsUserIdentified = true;

                    EntityType userEntityType = EntityType.GetEntityType(typeof(User));
                    User user = User.GetOrCreate(context, externalUsername);

                    bool register = !user.Exists && loginElem.HasAttribute("register") && loginElem.Attributes["register"].Value == "true";
                    bool refresh = user.Exists && loginElem.HasAttribute("refresh") && loginElem.Attributes["refresh"].Value == "true";

                    // If username was not found and automatic registration is configured, create new user
                    // If username was found return with success
                    if (register || refresh) {
                        if (register) {
                        } else {
                            IsUserAuthenticated = true; // TODO: REMOVE
                        }
                        
                        foreach (XmlElement elem in loginElem.ChildNodes) {
                            if (elem == null) continue;
                            string value = null;
                            if (elem.HasAttribute("header")) value = HttpContext.Current.Request.Headers[elem.Attributes["header"].Value];

                            switch (elem.Name) {
                                case "firstName" :
                                    user.FirstName = value;
                                    break;
                                case "lastName" :
                                    user.LastName = value;
                                    break;
                                case "email" :
                                    user.Email = value;
                                    break;
                                case "affiliation" :
                                    user.Affiliation = value;
                                    break;
                                case "credits" :
                                    int credits;
                                    Int32.TryParse(value, out credits);
                                    user.TotalCredits = credits;
                                    break;
                                case "proxyUsername" :
                                    //user.ProxyUsername = value;
                                    break;
                                case "proxyPassword" :
                                    //user.ProxyPassword = value;
                                    break;
                            }
                        }
                    }
                    return user;
                }
                return null;

            } catch (Exception e) {
                throw new Exception("Invalid authentication settings" + " " + e.Message + " " + e.StackTrace);
            }
        }
*/
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether an HTTP session for the specified user can be started according to the user's account status.</summary>
        /// <exception cref="UnauthorizedAccessException">If the account is deactivated or disabled.</exception>
        /// <exception cref="UnauthorizedAccessException">If the account is waiting for an activation  </exception>
        /// <param name="user">The user for whom the session is to be started.</param>
        public virtual void CheckCanStartSession(User user) {
            switch (user.AccountStatus) {
                case AccountStatusType.Disabled :
                    throw new UnauthorizedAccessException("The user account has been disabled");
                case AccountStatusType.Deactivated :
                    throw new UnauthorizedAccessException("The user account has been deactivated, most likely because of too many failed login attempts");
                case AccountStatusType.PendingActivation:
                    throw new PendingActivationException("The user account has not yet been activated");
                case AccountStatusType.PasswordReset :
                    throw new PendingActivationException("The user account has to be reactivated after a password reset");
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Starts an HTTP session for the specified user who has been authenticated with the specified authentication type.</summary>
        /// <param name="authenticationType">The used authentication type.</param>
        /// <param name="user">The user for whom the session is to be started.</param>
        /// <param name="check">Decides whether a check is performed. This parameter can be omitted, the default setting is <c>true</c>.</param>
        public void StartSession(AuthenticationType authenticationType, User user, bool check = true) {
            if (!authenticationType.SupportsSessions) throw new InvalidOperationException(String.Format("{0} does not support sessions", authenticationType.Name));

            SetUserInformation(authenticationType, user);

            if (check) CheckCanStartSession(user);

            user.StartNewSession();
            if (HttpContext.Current != null) {
                if (User.ProfileExtension != null) User.ProfileExtension.OnSessionStarting(this, user, HttpContext.Current.Request);
                HttpContext.Current.Session["user"] = UserInformation;
                HttpContext.Current.Session.Timeout = (authenticationType.UsesExternalIdentityProvider ? 5 : 1440);
            }
        }

        /// <summary>
        /// Starts the session.
        /// </summary>
        /// <param name="authenticationType">Authentication type.</param>
        /// <param name="user">User.</param>
        /// <param name="session">Session.</param>
        public void StartSession(AuthenticationType authenticationType, User user, string session) {
            UserInformation.ExternalSessionToken = session;
            StartSession(authenticationType, user);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Ends the current HTTP session.</summary>
        public void EndSession() {
            AuthenticationType authenticationType = null;
            int oldUserId = 0;
            if (UserInformation != null && HttpContext.Current != null) {
                oldUserId = UserInformation.UserId;
                authenticationType = UserInformation.AuthenticationType;
            }

            SetUserInformation(null, null);
            if (HttpContext.Current != null) {
                HttpContext.Current.Session["user"] = null;
                if (User.ProfileExtension != null && oldUserId != 0) {
                    if (User.ProfileExtension != null) User.ProfileExtension.OnSessionEnded(this, User.FromId(this, oldUserId), HttpContext.Current.Request);
                }
            }
            //if (HttpContext.Current != null) HttpContext.Current.Session.Abandon();

            if (authenticationType != null && authenticationType.UsesExternalIdentityProvider) authenticationType.EndExternalSession(this, HttpContext.Current.Request, HttpContext.Current.Response);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static bool IsStrongPassword(string value) {
            if (value == null || value.Length < 8) return false;
            if (!Regex.Match(value, "[A-Z]").Success) return false;
            if (!Regex.Match(value, "[a-z]").Success) return false;
            if (!Regex.Match(value, "[0-9]").Success) return false;
            if (!Regex.Match(value, "[^A-Za-z0-9]").Success) return false;
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool IsPasswordAllowedForUser() {
            return GetQueryBooleanValue(String.Format("SELECT normal_account AND {1} OR (!normal_account OR {2}) allow_password FROM usr WHERE id={0};", UserId, AllowPassword == RuleApplicationType.Always ? "true" : "false", AllowPassword == RuleApplicationType.NoRule ? "true" : "false"));
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        bool IResponseNonceStorage.Contains(DateTime nonceTime, string randomPart) {
            return GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM openidnonce WHERE time_part='{0}' AND random_part={1};", nonceTime.ToUniversalTime().ToString(@"yyyy\-MM\-dd\THH\:mm\:ss.fff\Z"), StringUtils.EscapeSql(randomPart))) != 0;
        }

        //---------------------------------------------------------------------------------------------------------------------

        void IResponseNonceStorage.Add(DateTime nonceTime, string randomPart) {
            Execute(String.Format("INSERT INTO openidnonce (time_part, random_part) VALUES ('{0}', {1});", nonceTime.ToUniversalTime().ToString(@"yyyy\-MM\-dd\THH\:mm\:ss.fff\Z"), StringUtils.EscapeSql(randomPart)));
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Ends the HTTP session for the active user account and reduces the web portal usage privileges to those of non-authenticated users.</summary>
        [Obsolete("EndSession or EndImpersonation should be called directly by the View component")]
        public void LogoutUser() {
            //HttpContext.Current.Session.Abandon(); // !!! Session.Abandon has serious bugs in Mono 2.10
            if (UserId == OriginalUserId) {
                EndSession();
            } else {
                EndImpersonation();
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void RefreshUser(int userId) {
            User user = User.FromId(this, userId);

            SetUserInformation(null, user);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void RefreshUserCredits() {
            AvailableCredits = TotalCredits - GetQueryDoubleValue(String.Format("SELECT SUM(resources * priority) FROM task AS t WHERE t.id_usr={0} AND t.status=20;", UserId));
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether a user session is open and retrieves the relevant user information.</summary>
        /// <returns><b>true</b> if a session is active.</returns>
        public bool CheckUserSession() {
            if (HttpContext.Current.Session == null) {
                throw new NullReferenceException("The current context session is null");
            } else {
                UserInformation = HttpContext.Current.Session["user"] as UserInformation;
                if (UserInformation == null) return false;
                SetUserFields();
                return true;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected bool GetUserData(string username) {
            bool result = false;
            if (username == null) throw new ArgumentNullException("Username missing");

            User user = User.FromUsername(this, username);

            if (user.AccountStatus != AccountStatusType.Enabled) throw new UnauthorizedAccessException("User account is not active");

            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void RejectUnauthenticatedRequest() {
            RejectUnauthenticatedRequest(false);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void RejectUnauthenticatedRequest(bool forceReject) {
            if (forceReject || !IsUserIdentified) throw new UnauthorizedAccessException("You are not logged in");
            /*if (RegistrationUrl != null) Redirect(RegistrationUrl, false, false); else */AddInfo("You are not registered");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void RejectUnauthorizedRequest() {
            throw new UnauthorizedAccessException("You are not authorized to view this information");
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public bool CheckAuthorization() {
            if (UserLevel >= Privileges.MinUserLevelView) return true;
            if (AllowViewTokenList && ListToken != null) return true;
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public bool IsRequestFromHost(string hostname) {
            string remoteHost = HttpContext.Current.Request.ServerVariables["REMOTE_HOST"];
            try {
                foreach (IPAddress ip in Dns.GetHostEntry(hostname).AddressList) if (ip.ToString() == remoteHost) return true;
            } catch (Exception) {}
            return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool IsTrustedUrl(string url) {
            Match match = Regex.Match(url, @"^([a-z0-9]+://)?([^:/\?]+).*$");
            if (!match.Success) throw new ArgumentException("Invalid URL");
            
            if (!match.Groups[1].Success) url = "http://" + url;
            string requestedHost = match.Groups[2].Value;
            
            return IsTrustedHost(requestedHost);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool IsTrustedHost(string requestedHost) {

            Match match = Regex.Match(requestedHost.Replace(" ", String.Empty), @"^10\..*$");
            if (match.Success) return true;

            foreach (string trustedHost in TrustedHosts) {
                if (trustedHost == requestedHost) return true;
                try {
                    foreach (IPAddress requestedIp in Dns.GetHostEntry(requestedHost).AddressList) {
                        foreach (IPAddress trustedIp in Dns.GetHostEntry(trustedHost).AddressList) if (trustedIp.ToString() == requestedIp.ToString()) return true;
                    }
                } catch (Exception) {}
            }
            return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static bool IsTrustedHost(string requestedHost, string trustedHostsStr) {
            if (trustedHostsStr == null) return false;
            string[] trustedHosts = trustedHostsStr.Replace(" ", String.Empty).Split(',');
            foreach (string trustedHost in trustedHosts) {
                if (trustedHost == requestedHost) return true;
                try {
                    foreach (IPAddress requestedIp in Dns.GetHostEntry(requestedHost).AddressList) {
                        foreach (IPAddress trustedIp in Dns.GetHostEntry(trustedHost).AddressList) if (trustedIp.ToString() == requestedIp.ToString()) return true;
                    }
                } catch (Exception) {}
            }
            return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks the availability of the web portal as defined in the Control Panel.</summary>
        /*!
            The exception is only thrown if caused by a fatal runtime error that has not been dealt with before by application-specific error handling (i.e. ReturnError() method).
        /// <param name="e">the exception to be thrown if considered fatal</param>
        */
        public void CheckAvailability() {
            if (IsWebSiteAvailable) return;
            if (UserLevel == Terradue.Portal.UserLevel.Administrator) {
                WriteWarning("The site is currently unavailable to the general public.", "siteUnavailable");
            } else {
                string message = GetConfigValue("UnavailabilityMessage");
                if (message == null) message = "This site is currently not available. Please retry later.";
                throw new SiteNotAvailableException(message);
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void ProcessProxyRequest(string url, string contentType, string xslFilename) {
            ProcessProxyRequest(url, contentType, xslFilename, false);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void ProcessProxyRequest(string url, string contentType, string xslFilename, bool trust) {
            if (!trust && !(IsTrustedUrl(url))) throw new UnauthorizedAccessException("Requested host cannot be accessed through this proxy");
            
            HttpWebResponse webResponse = null;
            Stream webResponseStream = null;
            byte[] buffer = new byte[4096];
            int size;
            try {
                HttpRequest inRequest = HttpContext.Current.Request; // incoming request
                HttpWebRequest outRequest = (HttpWebRequest)WebRequest.Create(url); // outgoing request
                outRequest.Method = inRequest.HttpMethod;
                if (HttpContext.Current.Request.ContentLength != 0) {
                    outRequest.ContentLength = inRequest.ContentLength;
                    Stream outStream = outRequest.GetRequestStream();
                    Stream inStream = inRequest.InputStream;
                    while ((size = inStream.Read(buffer, 0, 4096)) != 0) outStream.Write(buffer, 0, size);
                    inStream.Close();
                    outStream.Close();
                }
                webResponse = (HttpWebResponse)outRequest.GetResponse();
                //webResponseStream = webResponse.GetResponseStream();
            } catch (WebException e) {
                webResponse = (HttpWebResponse)e.Response;
            } catch (Exception e) {
                throw;
            }
            
            if (webResponse == null) {
                throw new Exception("Invalid request or response");
            }
            
            if (webResponse != null) {
                HttpContext.Current.Response.ContentType = (contentType != null ? contentType : webResponse.ContentType);
                int statusCode;
                switch (webResponse.StatusCode) {
                    case HttpStatusCode.OK :
                        statusCode = 200;
                        break;
                    case HttpStatusCode.Unauthorized :
                        statusCode = 401;
                        break;
                    case HttpStatusCode.Forbidden :
                        statusCode = 403;
                        break;
                    case HttpStatusCode.NotFound :
                        statusCode = 404;
                        break;
                    case HttpStatusCode.RequestTimeout :
                        statusCode = 408;
                        break;
                    default :
                        statusCode = 500;
                        break;
                }
                HttpContext.Current.Response.StatusCode = statusCode;
                webResponseStream = webResponse.GetResponseStream();
                
                if (xslFilename == null || !File.Exists(xslFilename)) while ((size = webResponseStream.Read(buffer, 0, 4096)) != 0) HttpContext.Current.Response.OutputStream.Write(buffer, 0, size);
                else TransformXml(webResponseStream, HttpContext.Current.Response.OutputStream, xslFilename);
                webResponseStream.Close();
                webResponse.Close();
            }
            
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void Redirect(string url) {
            HttpContext.Current.Response.Redirect(url, false);
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void Redirect(string url, bool format, bool privileges) {
            char separator = (url.IndexOf('?') == -1 ? '?' : '&');
            if (privileges && UserInformation != null && UserInformation.AuthenticationType is SessionlessAuthenticationType) {
                url += separator + "_user=" + UserId;
                separator = '&';
            }
            if (format && Format != null) url += separator + FormatParameterName + "=" + Format;
            HttpContext.Current.Response.Redirect(url, false);
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the web browser from which the request originates has inbuilt SVG display functionality.</summary>
        public bool IsSvgEnabledUserAgent() {
            // Firefox: Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10.5; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7
            // Safari:  Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_5_8; en-us) AppleWebKit/530.19.2 (KHTML, like Gecko) Version/4.0.2 Safari/530.19
            // Opera:   Opera/9.62 (Macintosh; Intel Mac OS X; U; en) Presto/2.1.1
            // IE       Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.2; Trident/4.0; .NET CLR 1.1.4322; .NET CLR 2.0.50727; .NET CLR 3.0.4506.2152; .NET CLR 3.5.30729)

            // "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10.6; en-US; rv:1.9.2.11) Gecko/20101012 Firefox/3.6.11 GTB7.1"
            
            string userAgent = HttpContext.Current.Request.ServerVariables["HTTP_USER_AGENT"];
            if (userAgent == null) return true;
            
            if (userAgent.StartsWith("Opera/") || userAgent.Contains("Firefox/") || userAgent.Contains("Safari/")) return true;
            
            // if (userAgent.Contains(" MSIE ")) return false;
            
            /*Match match = Regex.Match(userAgent, "^([^ /]+)/([0-9]+)");
            if (!match.Success) return false;
            string browser = match.Groups[1].Value;
            int version = Int32.Parse(match.Groups[2].Value);
            if (browser == "Opera" && version >= 9) return true;
            
            match = Regex.Match(userAgent, "([^ /]+)/([0-9]+)[^ ]*$");
            if (!match.Success) return false;

            browser = match.Groups[1].Value;
            version = Int32.Parse(match.Groups[2].Value);
            if (browser == "Firefox" && version >= 3) return true;*/
            
            return false;
        }



























































        
        private static string adminRootUrl;
        private static string accountRootUrl;
        private static string taskWorkspaceRootUrl;
        private static string taskWorkspaceJobDir;
        private static string schedulerWorkspaceRootUrl;
        private static string taskFlowUrl;
        private static string defaultXslFile;
        private static string[] disabledProfileFields;


        // public const string FormatParameterName = "_format"; // !!!

        //protected DateTime startTime;
        protected DateTime endTime;
        protected bool addRequestInfo = true;
        protected NameValueCollection requestParameters;
        private MemoryStream memStream;
        private string queryString;
        private string xslFilename = null;
        private string contentType = null;
        private bool customOutput = false;
        private bool xslTransformation = false;
        private string outputString;
        protected MonoXmlWriter xmlWriter, msgXmlWriter;
        protected bool responseStarted = false, responseEnded = false;

        //---------------------------------------------------------------------------------------------------------------------

        public IDbConnection GetNewsDbConnection() {
            string connectionString = System.Configuration.ConfigurationManager.AppSettings["NewsDatabaseConnection"];
            if (connectionString == null) throw new InvalidOperationException("News database not available");
            NewsDbConnection = GetNewDbConnection(connectionString);
            return NewsDbConnection;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void CloseNewsDbConnection() {
            if (NewsDbConnection == null) return;
            NewsDbConnection.Close();
            NewsDbConnection = null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Checks whether the web browser from which the request originates has inbuilt SVG display functionality.</summary>
        public void WriteXmlFile(string filename) {
            XmlDocument doc = LoadXmlFile(filename);
            WriteXmlFile(doc);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void WriteXmlFile(XmlDocument doc) {
            doc.WriteTo(xmlWriter);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void SetContentType(string value) {
            HttpContext.Current.Response.ContentType = value;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void AddDebug(int level, string message) {
            if (level > DebugLevel) return;
            if (msgStrWriter == null) {
                msgStrWriter = new StringWriter();
                msgXmlWriter = MonoXmlWriter.Create(msgStrWriter);
            } else if (responseEnded) {
                return;
            }
            msgXmlWriter.WriteStartElement("message");
            msgXmlWriter.WriteAttributeString("type", "debug");
            msgXmlWriter.WriteAttributeString("level", level.ToString());
            msgXmlWriter.WriteString(message);
            msgXmlWriter.WriteEndElement();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected override void AddMessage(MessageType type, string message, string messageClass) {
            if (msgStrWriter == null) {
                msgStrWriter = new StringWriter();
                msgXmlWriter = MonoXmlWriter.Create(msgStrWriter);
            } else if (responseEnded) {
                return;
            }
            msgXmlWriter.WriteStartElement("message");
            msgXmlWriter.WriteAttributeString("type", type.ToString().ToLower());
            if (messageClass != null) msgXmlWriter.WriteAttributeString("class", messageClass);
            msgXmlWriter.WriteString(message);
            msgXmlWriter.WriteEndElement();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override void WriteDebug(int level, string message) {
            if (HideMessages || level > DebugLevel) return;
            if (!responseStarted) StartXmlResponse();
            xmlWriter.WriteStartElement("message");
            xmlWriter.WriteAttributeString("type", "debug");
            //xmlWriter.WriteAttributeString("type", "info");
            //xmlWriter.WriteAttributeString("class", "debug");
            xmlWriter.WriteAttributeString("level", level.ToString());
            xmlWriter.WriteString(message);
            xmlWriter.WriteEndElement();
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected override void WriteMessage(MessageType type, string message, string messageClass) {
            if (HideMessages) return;
            if (!responseStarted) StartXmlResponse();
            xmlWriter.WriteStartElement("message");
            xmlWriter.WriteAttributeString("type", type.ToString().ToLower());
            if (messageClass != null) xmlWriter.WriteAttributeString("class", messageClass);
            xmlWriter.WriteString(message);
            xmlWriter.WriteEndElement();
        }

        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Throw exceptions directly and handle the exceptions in the View component of the web application")]
        public override void ReturnError(Exception exception, string messageClass) {
            lastErrorMessage = exception.Message;
            lastErrorTime = DateTime.UtcNow;
            if (exception == null) exception = new Exception("An error occurred");

            int statusCode;
            string statusDescription;
            
            if (exception is ArgumentException || exception is FormatException) {
                statusCode = 400;
                statusDescription = "Bad Request";
            } else if (exception is UnauthorizedAccessException) {
                statusCode = 403;
                statusDescription = "Forbidden";
            } else if (exception is FileNotFoundException || exception is DirectoryNotFoundException) {
                statusCode = 404;
                statusDescription = "Not Found";
            } else {
                statusCode = 500;
                statusDescription = "Internal Server Error";
            }

            // !!! TO DO use HTTP status codes in case of errors

            /* HttpContext.Current.Response.StatusCode = statusCode;
            HttpContext.Current.Response.StatusDescription = statusDescription; */
            if (InTransaction) Rollback();

            /*if (textOutput) {
                if (!responseStarted) StartTextResponse();
                SetContentType("text/html");
                streamWriter.WriteLine("<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\">");
                streamWriter.Write("<html><head><title>" + statusCode + " " + statusDescription + "</title></head>");
                streamWriter.Write("<body><h1>" + statusDescription + "</h1><p>" + exception.Message + "</p></body></html>");
                //if (!KeepOpen) EndTextResponse();
            } else if (customOutput) {
                if (!responseStarted) StartXmlResponse();
                WriteMessage(MessageType.Error, exception.Message, messageClass);
            } else {
                if (!responseStarted) StartXmlResponse();
                AddMessage(MessageType.Error, exception.Message, messageClass);
                //if (!KeepOpen) EndXmlResponse();
            }*/
            //if (!KeepOpen) Close();
            Error = true;
            throw exception;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override void WriteSeparator() {}
        
        // Extended function to include HTTP error code and description
        public void ReturnHttpError( Exception e, string messageClass, int statusCode, string statusDescription) {
           HttpContext.Current.Response.StatusCode = statusCode;
           HttpContext.Current.Response.StatusDescription = statusDescription;
           AddMessage(MessageType.Error, e.Message, messageClass);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual MonoXmlWriter StartXmlResponse() {
            if (responseStarted) return xmlWriter;

            xslTransformation = (xslFilename != null);

            if (customOutput && contentType != null) SetContentType(contentType);
            else if (customOutput || !xslTransformation) SetContentType("text/xml");

            strWriter = new UTF8StringWriter();
            xmlWriter = MonoXmlWriter.Create(strWriter);

            responseStarted = true;

            if (!customOutput) {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("content");
                if (!IsUserAuthenticated) xmlWriter.WriteAttributeString("allowPassword", AllowPassword == RuleApplicationType.Never ? "false" : "true");
                if (!addRequestInfo) xmlWriter.WriteAttributeString("version", CoreVersion);
                if (IsUserAuthenticated && UserId != 0) {
                    //RefreshUserState();
                    xmlWriter.WriteStartElement("user");
                    if (UserId != OriginalUserId) xmlWriter.WriteAttributeString("impersonated", "true");
                    if (AuthenticationMethod == AuthenticationMethod.CertificateSubject) xmlWriter.WriteAttributeString("certificate", "true");
                    xmlWriter.WriteElementString("id", UserId.ToString());
                    xmlWriter.WriteElementString("level", UserLevel.ToString());
                    xmlWriter.WriteElementString("caption", UserCaption);
                    xmlWriter.WriteElementString("proxyTimeLeft", RemainingProxyTime);
                    xmlWriter.WriteElementString("resources", AvailableCredits.ToString()); // !!! rename to "credits"
                    xmlWriter.WriteEndElement(); // </user>
                }
                xmlWriter.WriteRaw("<!--$MESSAGES$-->");
            }
            return xmlWriter;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>.</summary>
        public virtual MonoXmlWriter StartXmlResponse(string contentType) {
            ContentType = contentType;
            return StartXmlResponse();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void EndXmlResponse() {
            if (!responseStarted) return;
            responseStarted = false;
            responseEnded = true;

            if (!customOutput) {
                xmlWriter.WriteEndElement(); // </content>
            }

            xmlWriter.Close();
            outputString = strWriter.ToString();
            if (!customOutput) {
                string messages;
                if (msgStrWriter == null) {
                    messages = null;
                } else {
                    messages = msgStrWriter.ToString();
                    msgXmlWriter.Close();
                    msgStrWriter.Close();
                }
                //if (!MultipleErrors)
                if (addRequestInfo) {
                    endTime = DateTime.UtcNow;
                    TimeSpan requestDuration = endTime - Now;
                    ResolvePlaceholder(
                        "<content>",
                        String.Format("<content version=\"{0}\" started=\"{1}\" finished=\"{2}\" duration=\"{3} sec\" link=\"{4}\">",
                                  CoreVersion,
                                  Now.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss.fff\Z"),
                                  endTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss.fff\Z"),
                                  requestDuration.TotalSeconds.ToString("#0.000"),
                                  System.Security.SecurityElement.Escape(ScriptUrl + Regex.Replace(queryString, @"([\?\&])site\:[^=]+=[^\&]+", "$1"))
                                  ),
                        false
                        );
                }
                ResolvePlaceholder("<!--$MESSAGES$-->", messages, false);
            }
            memStream = new MemoryStream();
            byte[] bytes = new UTF8Encoding(false).GetBytes(outputString);
            memStream.Write(bytes, 0, bytes.Length);

            xslTransformation = (xslFilename != null);
            //xslTransformation = (HttpContext.Current.Request["format"] == null && xslFilename != null);

            if (xslTransformation) {
                //HttpContext.Current.Response.ContentEncoding = new UTF8Encoding(false); // not needed?

                // Get the media type of the XSL output and set the output stream's content type
                string contentType = GetXslMediaType(xslFilename);
                if (contentType != null) SetContentType(contentType);

                /*XmlDocument doc = new XmlDocument();
                doc.Load(xslFilename);
                XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);
                nsm.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
                XmlAttribute attr = doc.SelectSingleNode("//xsl:output/@media-type", nsm) as XmlAttribute;
                if (attr != null) SetContentType(attr.Value);*/

                // Perform the XSL transformation directly into the output stream
                TransformXml(memStream, HttpContext.Current.Response.OutputStream, xslFilename);

            } else if (bytes.Length != 0) {
                memStream.WriteTo(HttpContext.Current.Response.OutputStream);
            }

            memStream.Close();
            //            StreamWriter output = new StreamWriter(HttpContext.Current.Response.OutputStream);
            //            output.Write(outputString);
            //            output.Close();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static string GetXslMediaType(string xslFilename) {
            string result = null;
            using (FileStream fs = new FileStream(xslFilename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                XmlDocument doc = new XmlDocument();
                doc.Load(fs);
                XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);
                nsm.AddNamespace("xsl", "http://www.w3.org/1999/XSL/Transform");
                XmlAttribute attr = doc.SelectSingleNode("//xsl:output/@media-type", nsm) as XmlAttribute;
                if (attr != null) result = attr.Value;
                fs.Close();
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static void TransformXml(Stream source, Stream dest, string filename) {
            XmlReader xmlReader;
            XslCompiledTransform xslt;

            // Do transformation in memory and write to stdout
            if (source.CanSeek) {
                source.Flush();
                source.Seek(0, SeekOrigin.Begin);
            }

            xmlReader = XmlReader.Create(source);
            xmlReader.MoveToContent();

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;
            XmlReader xslReader = XmlReader.Create(filename, settings);

            try {
                xslt = new XslCompiledTransform();
                xslt.Load(xslReader, new XsltSettings(true, false), new XmlUrlResolver());
                //xslReader = XmlReader.Create(filename, settings);

                xslt.Transform(xmlReader, null, dest/*System.Xml.XmlWriter.Create(dest, xslt.OutputSettings)*/);
                xslReader.Close();
                xmlReader.Close();
            } catch (Exception e) {
                if (xslReader != null) xslReader.Close();
                if (xmlReader != null) xmlReader.Close();
                throw e;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual StreamWriter StartTextResponse() {
            if (responseStarted) return streamWriter;

            textOutput = true;

            SetContentType("text/plain");
            streamWriter = new StreamWriter(HttpContext.Current.Response.OutputStream, Encoding.UTF8);

            responseStarted = true;
            return streamWriter;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void EndTextResponse() {
            if (!responseStarted) return;

            responseStarted = false;
            streamWriter.Close();
        }


        //---------------------------------------------------------------------------------------------------------------------

        private void ResolvePlaceholder(string placeholder, string value, bool multiple) {
            if (multiple) {
                outputString = outputString.Replace(placeholder, value);
            } else {
                int pos = outputString.IndexOf(placeholder);
                if (pos != -1) outputString = outputString.Substring(0, pos) + value + outputString.Substring(pos + placeholder.Length);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public int GetIdFromRequest() {
            int id = 0;
            Int32.TryParse(GetParamValue("id"), out id);
            return id;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void GetRequestParameters() {
            requestParameters = (HttpContext.Current.Request.Form.Count == 0 ? HttpContext.Current.Request.QueryString : HttpContext.Current.Request.Form);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetParamName(int index) {
            if (requestParameters == null) GetRequestParameters();
            string name = requestParameters.GetKey(index);
            if (name == String.Empty) name = null;
            return name;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetParamValue(int index) {
            if (requestParameters == null) GetRequestParameters();
            string value = requestParameters.Get(index);
            if (value == String.Empty) value = null;
            return value;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetValue(string name) {
            string value = HttpContext.Current.Request[name];
            if (value == String.Empty) value = null;
            return value;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetParamValue(string name) {
            string value = HttpContext.Current.Request[name];
            if (value == String.Empty) value = null;
            return value;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetParamGetValue(string name) {
            string value = HttpContext.Current.Request.QueryString[name];
            if (value == String.Empty) value = null;
            return value;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetParamPostValue(string name) {
            string value = HttpContext.Current.Request.Form[name];
            if (value == String.Empty) value = null;
            return value;
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents a set of privileges for accessing entities in relation to the level of the web portal user.</summary>
    public class PagePrivileges {

        /// <summary>The minimum user level required for viewing an entity's item list</summary>
        public int MinUserLevelList;
        
        /// <summary>The minimum user level required for viewing an entity's single items</summary>
        public int MinUserLevelView;
        
        /// <summary>The minimum user level required for defining or creating new items</summary>
        public int MinUserLevelCreate;
        
        /// <summary>The minimum user level required for modifying existing items</summary>
        public int MinUserLevelModify;
        
        /// <summary>The minimum user level required for deleting existing items</summary>
        public int MinUserLevelDelete;

        /// <summary>Gets a predefined privilege set providing full access to administrators. </summary>
        public static PagePrivileges AdminOnly {
            get { return new PagePrivileges(UserLevel.Administrator, UserLevel.Administrator, UserLevel.Administrator, UserLevel.Administrator, UserLevel.Administrator); }
        }

        /// <summary>Gets a predefined privilege set providing full access to administrators. </summary>
        public static PagePrivileges ManagerOnly {
            get { return new PagePrivileges(UserLevel.Manager, UserLevel.Manager, UserLevel.Manager, UserLevel.Manager, UserLevel.Manager); }
        }

        /// <summary>Gets a predefined privilege set providing viewing access to developers and full access to administrators. </summary>
        public static PagePrivileges DeveloperView {
            get { return new PagePrivileges(UserLevel.Developer, UserLevel.Developer, UserLevel.Administrator, UserLevel.Administrator, UserLevel.Administrator); }
        }

        /// <summary>Gets a predefined privilege set providing full access to developers and administrators. </summary>
        public static PagePrivileges DeveloperEdit  {
            get { return new PagePrivileges(UserLevel.Developer, UserLevel.Developer, UserLevel.Developer, UserLevel.Developer, UserLevel.Developer); }
        }

        /// <summary>Gets a predefined privilege set providing viewing access to users and developers and full access to administrators. </summary>
        public static PagePrivileges UserView  {
            get { return new PagePrivileges(UserLevel.User, UserLevel.User, UserLevel.Administrator, UserLevel.Administrator, UserLevel.Administrator); }
        }

        /// <summary>Gets a predefined privilege set providing full access to registered users, developers and administrators. </summary>
        public static PagePrivileges UserEdit {
            get { return new PagePrivileges(UserLevel.User, UserLevel.User, UserLevel.User, UserLevel.User, UserLevel.User); }
        }

        /// <summary>Gets a predefined privilege set providing viewing access to unregistered users and full access to administrators. </summary>
        public static PagePrivileges EverybodyView  {
            get { return new PagePrivileges(UserLevel.Everybody, UserLevel.Everybody, UserLevel.Administrator, UserLevel.Administrator, UserLevel.Administrator); }
        }

        public PagePrivileges(int minUserLevelList, int minUserLevelView, int minUserLevelCreate, int minUserLevelModify, int minUserLevelDelete) {
            this.MinUserLevelList = minUserLevelList;
            this.MinUserLevelView = minUserLevelView;
            this.MinUserLevelCreate = minUserLevelCreate;
            this.MinUserLevelModify = minUserLevelModify;
            this.MinUserLevelDelete = minUserLevelDelete;
        }
    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public enum AuthenticationMethod {
        None = 0,
        Password = 1,
        OpenId = 2,
        Token = 3,
        CertificateSubject = 4,
        SingleSignOn = 5
    }

    
    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public enum RuleApplicationType {
        NoRule = 0,
        Never = 1,
        Always = 2,
    }


    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class SiteNotAvailableException : Exception { 
        public SiteNotAvailableException(string message) : base(message) {}
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class PendingActivationException : UnauthorizedAccessException {
        public PendingActivationException(string message) : base(message) {}
    }




}

