using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Xml;
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

    

    /// <summary>Represents an authentication type.</summary>
    /// <remarks>
    ///     This class provides an abstract model for basic functionality of internal and external authentication methods.
    ///     Implementing classes must provide a method to recognise a user identity from an HTTP request (if automatic user detection is possible) and may implement a method that checks whether an external session is still active.
    ///     Implementing classes may also implement specific methods, especially for the initiation of an authentication process, such as an HTTP redirect to an external sign-in page.
    /// </remarks>
    /// \xrefitem uml "UML" "UML Diagram"
    [EntityTable("auth", EntityTableConfiguration.Custom, IdentifierField = "identifier", NameField = "name", TypeField = "type")]
    public abstract class AuthenticationType : Entity {

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether this authentication method is enabled.</summary>
        [EntityDataField("enabled")]
        public bool IsEnabled { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the rule for the activation of user accounts created by this .</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        [EntityDataField("activation_rule")]
        public int AccountActivationRule { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the rule whether and how this authentication type is applicable to new normal user accounts.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        [EntityDataField("normal_rule")]
        public RuleApplicationType NormalAccountRule { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the period in seconds after which the external session created with this authentication type must be refreshed.</summary>
        /// <remarks>
        ///     The value is only relevant for authentications that support sessions and use an external identity provider <see cref="SupportSessions"/>, <see cref="UsesExternalIdentityProvider"/>. If the value is <c>0</c>.
        ///     The external session is refreshed after the expiry time. This does not affect the web portal's own session unless the the external session cannot be refreshed. In that case also the web portal's session is closed.
        /// </remarks>
        /// \xrefitem uml "UML" "UML Diagram"
        [EntityDataField("refresh_period")]
        public int ExternalSessionRefreshPeriod { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the location of an optional configuration file for complex profile mapping settings.</summary>
        [EntityDataField("config")]
        public string ConfigurationFile { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the authentication type starts and maintains persistent sessions.</summary>
        /// <remarks>Since normally a successful authentication with a portal starts a session with this web server, the default value is <c>true</c>. Derived classes may override the property.</remarks>
        public virtual bool SupportsSessions {
            get { return true; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the user account information in the database is updated after a new successful authentication.</summary>
        /// <remarks>
        ///     The request used to authenticate the user may contain fields user information, such as e-mail address, real name etc.
        ///     Since normally this incoming information originates from a central user directory and is up to date, the default value is <c>true</c> so as to update the web server's local database. Derived classes may override the property.
        /// </remarks>
        public virtual bool AlwaysRefreshAccount {
            get { return true; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the authentication type depends on external identity providers.</summary>
        /// <remarks>Derived classes must provide the correct value that applies to the application type.</remarks>
        /// \xrefitem uml "UML" "UML Diagram"
        public abstract bool UsesExternalIdentityProvider { get; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new AuthenticationType instance.</summary>
        /// <remarks>The provided <c>IfyContext</c> is not used except for loading the information from the database, since instances of this class will be used within a static collection and other methods must use their own <c>IfyContext</c>.</remarks>
        /// <param name="context">The execution environment context.</param>
        public AuthenticationType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public static AuthenticationType FromId(IfyContext context, int id) {
            EntityType entityType = EntityType.GetEntityType(typeof(AuthenticationType));
            AuthenticationType result = (AuthenticationType)entityType.GetEntityInstanceFromId(context, id); 
            result.Id = id;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, obtains the user information from the current HTTP request's URL, headers or content.</summary>
        /// <remarks>
        ///     This method works only in an HTTP context, i.e. within a web application. It is called by <see cref="Terradue.Portal.IfyWebContext"/> when the current request is not yet authenticated with this web server, i.e. with the first contact.
        ///     The method returns a <see cref="Terradue.Portal.User"/> instance that is either an existing user (if recognised from the request information) or a new user where all properties are filled with the available information from the request.
        ///     For users that do not yet exist, the method should also specify whether a new user is created automatically and with what status by setting the <see cref="Terradue.Portal.User.AccountStatus"/> property.
        ///     If <c>strict</c> is set to <c>false<c>, the method should return <c>null</c> if the authentication type does not apply. A failed authentication with one authentication type is not a fatal error in this case. The calling method may try several authentication types and normally only one actually recognizes the user.
        ///     If <c>strict</c> is set to <c>true</c>, the method should always throw an exception to inform the calling method about the specific problem.
        ///     The calling IfyWebContext then deals with the further processing of the request, including the user creation or authentication (or rejection of the request).
        /// </remarks>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        /// <param name="strict">If set to <c>true</c> the method should never return null but throws always an exception even if the authentication type does not apply.</param>
        /// <returns>An instance of User representing the user profile information or <c>null</c> if no such information is available.</returns>
        /// \xrefitem uml "UML" "UML Diagram"
        public abstract User GetUserProfile(IfyWebContext context, HttpRequest request, bool strict);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, checks whether an session corresponding to the current web server session exists on the external identity provider.</summary>
        /// <remarks>
        ///     This method works only in an HTTP context, i.e. within a web application. It is called by <see cref="Terradue.Portal.IfyWebContext"/> when the current request is authenticated with this web server in order to determine whether this is also the case with the external identity provider.
        ///     The user might have authenticated with the portal, but in the meantime he might have signed from the identity provider.
        ///     If the user has no longer a session with the identity provider, his session with this web server should be closed too. This is taken care of by the calling IfyWebContext.
        ///     For derived classes without external identity provider, this method is not applicable and should always return <c>true</c>.
        /// </remarks>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        /// <returns><c>true</c> if there is still a session, <c>false</c> otherwise.</returns>
        /// \xrefitem uml "UML" "UML Diagram"
        public abstract bool IsExternalSessionActive(IfyWebContext context, HttpRequest request);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Performs additional operations for the account of a successfully authenticated user based on the current HTTP request information.</summary>
        /// <remarks>This method can be used to define additional and complex authorization settings.</remarks>
        /// <param name="user">The concerned user.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        /// <param name="isNewUser">Indicates whether the user has just been created. Usually there is nothing to do for an existing account.</param>
        /// <returns><c>true</c> if the authentication type handles additional fully autonomously.</returns>
        /// \xrefitem uml "UML" "UML Diagram"
        public virtual bool SetAuthorizations(User user, HttpRequest request, bool isNewUser) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Terminates the user's session with the external identity provider.</summary>
        /// <remarks>This method is called from <see cref="Terradue.Portal.IfyWebContext.EndSession"/> if the authentication type uses external identity providers.</remarks>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest for reference.</param>
        /// <param name="response">The current HttpResponse for reference.</param>
        /// \xrefitem uml "UML" "UML Diagram"
        public virtual void EndExternalSession(IfyWebContext context, HttpRequest request, HttpResponse response) {}

        //---------------------------------------------------------------------------------------------------------------------

        public int GetDefaultAccountStatus() {
            switch (AccountActivationRule) {
                case AccountActivationRuleType.ActiveAfterApproval :
                case AccountActivationRuleType.ActiveAfterMail :
                    return AccountStatusType.PendingActivation;
                case AccountActivationRuleType.ActiveImmediately :
                    return AccountStatusType.Enabled;
            }
            return AccountStatusType.Disabled;
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    /// <summary>
    /// Password authentication type.
    /// </summary>
    /// \xrefitem uml "UML" "UML Diagram"
    public class PasswordAuthenticationType : AuthenticationType {

        private static bool forceStrongPasswords;
        private static int passwordExpireTime;
        private static int maxFailedLogins;

        //---------------------------------------------------------------------------------------------------------------------

        public override bool UsesExternalIdentityProvider {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Terradue.Portal.PasswordAuthenticationType"/> force
        /// strong passwords.
        /// </summary>
        /// <value><c>true</c> if force strong passwords; otherwise, <c>false</c>.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public bool ForceStrongPasswords {
            get { return forceStrongPasswords; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the password expire time.
        /// </summary>
        /// <value>The password expire time.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public int PasswordExpireTime {
            get { return passwordExpireTime; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------


        /// <summary>
        /// Gets or sets the max failed logins.
        /// </summary>
        /// <value>The max failed logins.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public int MaxFailedLogins {
            get { return maxFailedLogins; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public PasswordAuthenticationType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public override void Load() {
            base.Load();
            forceStrongPasswords = context.GetConfigBooleanValue("ForceStrongPasswords");
            passwordExpireTime = StringUtils.StringToSeconds(context.GetConfigValue("PasswordExpireTime"));
            maxFailedLogins = context.GetConfigIntegerValue("MaxFailedLogins");
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override User GetUserProfile(IfyWebContext context, HttpRequest request, bool strict) {
            if (strict) throw new InvalidOperationException("Password authentication not possible without explicit initiation");
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override bool IsExternalSessionActive(IfyWebContext context, HttpRequest request) {
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Authenticates the user.
        /// </summary>
        /// <returns>The user.</returns>
        /// <param name="context">Context.</param>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        /// \xrefitem uml "UML" "UML Diagram"
        public User AuthenticateUser(IfyWebContext context, string username, string password) {
            if (!IsEnabled) throw new InvalidOperationException("Password authentication is not enabled");

            if (username == null || password == null) throw new ArgumentNullException("Username and/or password missing", "emptyLogin");

            User user = User.GetInstance(context);
            user.Username = username;
            user.Load();

            if (!user.PasswordAuthenticationAllowed) throw new UnauthorizedAccessException("This user account cannot be accessed with a password");

            bool correctPassword = user.CheckPassword(password);

            if (!correctPassword) {
                user.FailedLogins++;
                if (MaxFailedLogins != 0 && user.FailedLogins > MaxFailedLogins) user.AccountStatus = AccountStatusType.Deactivated;
                user.Store();
                throw new UnauthorizedAccessException("Wrong username or password");
            }
            user.PasswordExpired = (PasswordExpireTime > 0 && user.LastPasswordChangeTime.AddSeconds(PasswordExpireTime) < context.Now);

            context.StartSession(this, user);
            return user;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static void ExecutePasswordExpirationCheck(IfyContext context) {
            IfyWebContext webContext = new IfyWebContext(null);

            if (context.DebugLevel >= 3) context.WriteDebug(3, "PasswordExpireTime seconds: " + passwordExpireTime);

            if (passwordExpireTime <= 0) return;
            DateTime earliestTime = context.Now.AddSeconds(- passwordExpireTime);

            string sql = String.Format("UPDATE usr SET status={0} WHERE status={1} AND allow_password AND (last_password_change_time IS NULL OR last_password_change_time<'{2}');", AccountStatusType.Deactivated, AccountStatusType.Enabled, earliestTime.ToString(@"yyyy\-MM\-dd HH\:mm\:ss"));
            context.WriteDebug(3, sql);
            int count = context.Execute(sql);

            context.WriteInfo("Deactivated user accounts: " + (count <= 0 ? "0" : count.ToString()));
        }

    }
    


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    /// <summary>
    /// Token authentication type.
    /// </summary>
    /// \xrefitem uml "UML" "UML Diagram"
    public class TokenAuthenticationType : AuthenticationType {

        public override bool UsesExternalIdentityProvider {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public TokenAuthenticationType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public override User GetUserProfile(IfyWebContext context, HttpRequest request, bool strict) {
            if (strict) throw new InvalidOperationException("Token authentication not possible without explicit initiation");
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override bool IsExternalSessionActive(IfyWebContext context, HttpRequest request) {
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Authenticates the user.
        /// </summary>
        /// <returns>The user.</returns>
        /// <param name="context">Context.</param>
        /// <param name="token">Token.</param>
        /// \xrefitem uml "UML" "UML Diagram"
        public User AuthenticateUser(IfyWebContext context, string token) {
            if (token == null) throw new ArgumentNullException("No account key or token specified");
            int userId = context.GetQueryIntegerValue(String.Format("SELECT t.id_usr FROM usrreg AS t WHERE t.token={0};", StringUtils.EscapeSql(token)));
            if (userId == 0) throw new UnauthorizedAccessException("Invalid account key");

            User user = User.FromId(context, userId);

            if (user.AccountStatus == AccountStatusType.PendingActivation || user.AccountStatus == AccountStatusType.PasswordReset || user.FailedLogins != 0) {
                user.AccountStatus = AccountStatusType.Enabled;
                user.NeedsEmailConfirmation = false;
                user.FailedLogins = 0;
                user.Store();
                context.SetUserInformation(this, user);
            }

            context.StartSession(this, user);

            return user;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    /// <summary>
    /// Certificate authentication type.
    /// </summary>
    /// \xrefitem uml "UML" "UML Diagram"
    public class CertificateAuthenticationType : AuthenticationType {

        public override bool UsesExternalIdentityProvider {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public CertificateAuthenticationType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public override User GetUserProfile(IfyWebContext context, HttpRequest request, bool strict) {
            if (request == null || request.ClientCertificate == null) {
                if (strict) throw new ArgumentNullException("Missing client certificate information");
                return null;
            }

            int userId = context.GetQueryIntegerValue(String.Format("SELECT id FROM usr AS t WHERE t.cert_subject={0};", StringUtils.EscapeSql(request.ClientCertificate.Subject)));
            if (userId == 0) {
                if (false /*CreateUserIfDoesNotExistFromSpeficConfiguration*/) {
                    User user = User.GetInstance(context);
                } else if (strict) {
                    throw new UnauthorizedAccessException("User not found");
                }
                return null;
            } else {
                return User.FromId(context, userId);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override bool IsExternalSessionActive(IfyWebContext context, HttpRequest request) {
            return true;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class SessionlessAuthenticationType : AuthenticationType {

        public override bool SupportsSessions {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override bool UsesExternalIdentityProvider {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override bool AlwaysRefreshAccount {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public SessionlessAuthenticationType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        public override User GetUserProfile(IfyWebContext context, HttpRequest request, bool strict) {
            if (!IsEnabled) throw new InvalidOperationException("Sessionless requests are not allowed");

            if (request == null) throw new ArgumentNullException("Missing request information");

            string username = GetUsername(request);

            if (username == null) {
                if (strict) throw new ArgumentNullException("No user account specified");
                return null;
            }

            string remoteHost = request.ServerVariables["REMOTE_HOST"];
            if (remoteHost != null && Array.IndexOf(context.TrustedHosts, remoteHost) != -1) {
                throw new UnauthorizedAccessException(String.Format("You are not connecting from a trusted host ({0})", remoteHost));
            }

            User user = User.FromString(context, username);

            if (!user.SessionlessRequestsAllowed) throw new InvalidOperationException("Sessionless requests are not allowed");

            return user;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override bool IsExternalSessionActive(IfyWebContext context, HttpRequest request) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        private string GetUsername(HttpRequest request) {
            return request.QueryString["_user"];
        }

    }

}

