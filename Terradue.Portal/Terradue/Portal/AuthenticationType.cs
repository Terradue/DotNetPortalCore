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


/*!
\defgroup Security Security
@{


@}

\defgroup Authentication Authentication
@{
It provides with the functions to identify a user through a generic interface for implementing multiple authentication mechanism.

\xrefitem mvc_c "Controller" "Controller components"

\ingroup Security

\xrefitem dep "Dependencies" "Dependencies" \ref Persistence reads/writes the user information

\startuml "Authentication mechanism Activity Diagram"

start
:read session information;
if (valid session) then (yes)
    :Load user info from session;
else (No) 
    while (user not authenticated & next authentication method ?) is (yes)
        if (Authentication successful) then (yes)
            if (user exists) then (no)
                if (authentication method allows user creation) then (yes)
                    :create user account;
                endif
            endif
         endif
    endwhile (no)
    if (user authenticated) then (yes)
        if (user is enabled) then (no)
            if ( configuration needs user activation ) then (yes)
                :Throw User Activation Exception;
                stop
            endif
        endif
        :Load user info and create session;
    endif
endif
stop


\enduml

@}
*/


namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents an authentication type.</summary>
    /// <remarks>
    ///     <para>This class provides an abstract model for basic functionality of internal and external authentication methods.</para>
    ///     <para>Implementing classes must provide a method to recognise a user identity from an HTTP request (if automatic user detection is possible) and may implement a method that checks whether an external session is still active.</para>
    ///     <para>Implementing classes may also implement specific methods, especially for the initiation of an authentication process, such as an HTTP redirect to an external sign-in page.</para>
    /// </remarks>
    /// \xrefitem uml "UML" "UML Diagram"
    [EntityTable("auth", EntityTableConfiguration.Custom, IdentifierField = "identifier", NameField = "name", TypeField = "type")]
    public abstract class AuthenticationType : Entity {

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether this authentication method is enabled.</summary>
        [EntityDataField("enabled")]
        public bool IsEnabled { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the rule for how new user accounts are created by this authentication type are activated.</summary>
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

        /// <summary>In a derived class, indicates whether the authentication type depends on external identity providers.</summary>
        /// <remarks>Derived classes must provide the correct value that applies to the application type.</remarks>
        /// \xrefitem uml "UML" "UML Diagram"
        public abstract bool UsesExternalIdentityProvider { get; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new AuthenticationType instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <remarks>The provided <c>IfyContext</c> is not used except for loading the information from the database, since instances of this class will be used within a static collection and other methods must use their own <c>IfyContext</c>.</remarks>
        public AuthenticationType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new AuthenticationType instance representing the authentication type with the specified database ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">The database ID of the authentication type.</param>
        /// <returns>The created AuthenticationType object.</returns>
        public static AuthenticationType FromId(IfyContext context, int id) {
            EntityType entityType = EntityType.GetEntityType(typeof(AuthenticationType));
            AuthenticationType result = (AuthenticationType)entityType.GetEntityInstanceFromId(context, id); 
            result.Id = id;
            result.Load();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, obtains the user information from the current HTTP request's URL, headers or content.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        /// <param name="strict">If set to <c>true</c> the method should never return null but throws always an exception even if the authentication type does not apply.</param>
        /// <returns>An instance of User representing the user profile information or <c>null</c> if no such information is available.</returns>
        /// <remarks>
        ///     This method works only in an HTTP context, i.e. within a web application. It is called by <see cref="Terradue.Portal.IfyWebContext"/> when the current request is not yet authenticated with this web server, i.e. with the first contact.
        ///     The method returns a <see cref="Terradue.Portal.User"/> instance that is either an existing user (if recognised from the request information) or a new user where all properties are filled with the available information from the request.
        ///     For users that do not yet exist, the method should also specify whether a new user is created automatically and with what status by setting the <see cref="Terradue.Portal.User.AccountStatus"/> property.
        ///     If <c>strict</c> is set to <c>false<c>, the method should return <c>null</c> if the authentication type does not apply. A failed authentication with one authentication type is not a fatal error in this case. The calling method may try several authentication types and normally only one actually recognizes the user.
        ///     If <c>strict</c> is set to <c>true</c>, the method should always throw an exception to inform the calling method about the specific problem.
        ///     The calling IfyWebContext then deals with the further processing of the request, including the user creation or authentication (or rejection of the request).
        /// </remarks>
        /// \xrefitem uml "UML" "UML Diagram"
        public abstract User GetUserProfile(IfyWebContext context, HttpRequest request, bool strict);

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, checks whether a session corresponding to the current web server session exists on the external identity provider.</summary>
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
        /// <param name="user">The concerned user.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        /// <param name="isNewUser">Indicates whether the user has just been created. Usually there is nothing to do for an existing account.</param>
        /// <returns><c>true</c> if the authentication type handles additional fully autonomously.</returns>
        /// <remarks>This method can be used to define additional and complex authorization settings.</remarks>
        /// \xrefitem uml "UML" "UML Diagram"
        public virtual bool SetAuthorizations(User user, HttpRequest request, bool isNewUser) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Terminates the user's session with the external identity provider.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest for reference.</param>
        /// <param name="response">The current HttpResponse for reference.</param>
        /// \xrefitem uml "UML" "UML Diagram"
        /// <remarks>This method is called from <see cref="Terradue.Portal.IfyWebContext.EndSession"/> if the authentication type uses external identity providers.</remarks>
        public virtual void EndExternalSession(IfyWebContext context, HttpRequest request, HttpResponse response) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the account status a newly created user account.</summary>
        /// <returns>The account status (<see cref="AccountStatusType"/>) based on this authentication type's activation rule.</returns>
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


    /// <summary>Defines settings for authentication via username and password where the user accounts are maintained by the Terradue.Portal installation.</summary>
    /// \xrefitem uml "UML" "UML Diagram"
    public class PasswordAuthenticationType : AuthenticationType {

        private static bool forceStrongPasswords;
        private static int passwordExpireTime;
        private static int maxFailedLogins;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates that PasswordAuthenticationType instances do not depend on an external identity provider.</summary>
        /// <value>Always <c>false</c> for PasswordAuthenticationType since the Terradue.Portal installation is its own identity provider.</value>
        public override bool UsesExternalIdentityProvider {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether this password authentication type requires the user to have strong passwords.</summary>
        /// <remarks>Strong passwords follow certain rules about the type characters that have to be used.</remarks>
        /// \xrefitem uml "UML" "UML Diagram"
        public bool ForceStrongPasswords {
            get { return forceStrongPasswords; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the password expiratation time in seconds.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public int PasswordExpireTime {
            get { return passwordExpireTime; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------


        /// <summary>Gets or sets the maximum possible number of failed logins before the account is blocked.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public int MaxFailedLogins {
            get { return maxFailedLogins; }
            protected set { throw new GlobalConfigurationReadOnlyException(); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new PasswordAuthenticationType instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <remarks>The provided <c>IfyContext</c> is not used except for loading the information from the database, since instances of this class will be used within a static collection and other methods must use their own <c>IfyContext</c>.</remarks>
        public PasswordAuthenticationType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Load this instance.</summary>
        public override void Load() {
            base.Load();
            forceStrongPasswords = context.GetConfigBooleanValue("ForceStrongPasswords");
            passwordExpireTime = StringUtils.StringToSeconds(context.GetConfigValue("PasswordExpireTime"));
            maxFailedLogins = context.GetConfigIntegerValue("MaxFailedLogins");
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Does nothing or throws an exception (method is not applicable to this authentication type).</summary>
        /// <returns><c>null</c> if strict is <c>false</c>.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        /// <param name="strict">If set to <c>true</c> the method throws an exception since the user identity is not obtained from an HTTP request.</param>
        public override User GetUserProfile(IfyWebContext context, HttpRequest request, bool strict) {
            if (strict) throw new InvalidOperationException("Password authentication not possible without explicit initiation");
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Always returns true (method is not applicable to this authentication type).</summary>
        /// <returns><c>true</c> as is the expected behaviour for authentication types with no external identity provider.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        public override bool IsExternalSessionActive(IfyWebContext context, HttpRequest request) {
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Authenticates the user via the specified username and password.</summary>
        /// <returns>An instance of User representing the authenticated user if the authentication was successful.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
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

            context.StartSession(this, user, false);

            return user;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Performs the background action for the password expiration check.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <remarks>
        ///     The background agent action <em>Password expiration check</em> deactivates user accounts for which the password has not been set for longer than the password expiration time (see <see cref="PasswordExpireTime"/>.
        ///     Users can still use their accounts after setting a new password.
        /// </remarks>
        public static void ExecutePasswordExpirationCheck(IfyContext context) {
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


    /// <summary>Defines settings for authentication via a secret token.</summary>
    /// \xrefitem uml "UML" "UML Diagram"
    public class TokenAuthenticationType : AuthenticationType {

        /// <summary>Indicates that TokenAuthenticationType instances do not depend on an external identity provider.</summary>
        /// <value>Always <c>false</c> for TokenAuthenticationType since in this case the Terradue.Portal installation acts as its own identity provider.</value>
        public override bool UsesExternalIdentityProvider {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new TokenAuthenticationType instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <remarks>The provided <c>IfyContext</c> is not used except for loading the information from the database, since instances of this class will be used within a static collection and other methods must use their own <c>IfyContext</c>.</remarks>
        public TokenAuthenticationType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Does nothing or throws an exception (method is not applicable to this authentication type).</summary>
        /// <returns><c>null</c> if strict is <c>false</c>.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        /// <param name="strict">If set to <c>true</c> the method throws an exception since the user identity is not obtained from an HTTP request.</param>
        public override User GetUserProfile(IfyWebContext context, HttpRequest request, bool strict) {
            if (strict) throw new InvalidOperationException("Token authentication not possible without explicit initiation");
            return null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Always returns true (method is not applicable to this authentication type).</summary>
        /// <returns><c>true</c> as is the expected behaviour for authentication types with no external identity provider.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        public override bool IsExternalSessionActive(IfyWebContext context, HttpRequest request) {
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Authenticates the user via the specified username and password.</summary>
        /// <returns>An instance of User representing the authenticated user if the authentication was successful.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="token">The secret token (e.g. a UUID a user received via e-mail).</param>
        /// <summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public User AuthenticateUser(IfyWebContext context, string token) {
            if (token == null) throw new ArgumentNullException("token", "No account key or token specified");
            int userId = context.GetQueryIntegerValue(String.Format("SELECT t.id_usr FROM usrreg AS t WHERE t.token={0};", StringUtils.EscapeSql(token)));
            if (userId == 0) throw new UnauthorizedAccessException("Invalid account key");

            User user = User.ForceFromId(context, userId);
            if (user.AccountStatus == AccountStatusType.Enabled) throw new InvalidOperationException("Account already enabled");

            if (user.AccountStatus == AccountStatusType.PendingActivation || user.AccountStatus == AccountStatusType.PasswordReset || user.FailedLogins != 0) {
                user.AccountStatus = AccountStatusType.Enabled;
                user.NeedsEmailConfirmation = false;
                user.FailedLogins = 0;
                user.Store();
                context.SetUserInformation(this, user);
            }

            context.StartSession(this, user, false);

            return user;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------


    /// <summary>Defines settings for authentication via a certificate subject.</summary>
    /// \xrefitem uml "UML" "UML Diagram"
    public class CertificateAuthenticationType : AuthenticationType {

        /// <summary>Indicates that CertificateAuthenticationType instances do not depend on an external identity provider.</summary>
        /// <value>Always <c>false</c> for CertificateAuthenticationType since in this case the Terradue.Portal installation acts as its own identity provider.</value>
        public override bool UsesExternalIdentityProvider {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new CertificateAuthenticationType instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <remarks>The provided <c>IfyContext</c> is not used except for loading the information from the database, since instances of this class will be used within a static collection and other methods must use their own <c>IfyContext</c>.</remarks>
        public CertificateAuthenticationType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Obtains the user information from the X509 certificate in the current HTTP request.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        /// <param name="strict">If set to <c>true</c> the method should never return null but throws always an exception even if the authentication type does not apply.</param>
        /// <returns>An instance of User representing the user profile information or <c>null</c> if no such information is available.</returns>
        public override User GetUserProfile(IfyWebContext context, HttpRequest request, bool strict) {
            if (request == null || request.ClientCertificate == null) {
                if (strict) throw new ArgumentNullException("request", "Missing client certificate information");
                return null;
            }

            int userId = context.GetQueryIntegerValue(String.Format("SELECT id FROM usr AS t WHERE t.cert_subject={0};", StringUtils.EscapeSql(request.ClientCertificate.Subject)));
            if (userId == 0) {
                /*if (false CreateUserIfDoesNotExistFromSpeficConfiguration) {
                    User user = User.GetInstance(context);
                } else */if (strict) {
                    throw new UnauthorizedAccessException("User not found");
                }
                return null;
            } else {
                return User.ForceFromId(context, userId);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Always returns true (method is not applicable to this authentication type).</summary>
        /// <returns><c>true</c> as is the expected behaviour for authentication types with no external identity provider.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        public override bool IsExternalSessionActive(IfyWebContext context, HttpRequest request) {
            return true;
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Defines settings for one-time authentication that is valid only for the current request.</summary>
    public class SessionlessAuthenticationType : AuthenticationType {

        /// <summary>Indicates that the SessionlessAuthenticationType instances do not support sessions.</summary>
        /// <value>Always <c>false</c> for SessionlessAuthenticationType as the name suggests.</value>
        public override bool SupportsSessions {
            get {
                return false;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates that SessionlessAuthenticationType instances do not depend on an external identity provider.</summary>
        /// <value>Always <c>false</c> for SessionlessAuthenticationType since in this case the Terradue.Portal installation acts as its own identity provider.</value>
        public override bool UsesExternalIdentityProvider {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates that the user account information in the database has not to be updated after a new successful authentication.</summary>
        /// <value>Always <c>false</c> for SessionlessAuthenticationType since there is usually no additional user infomation in the request.</value>
        public override bool AlwaysRefreshAccount {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new SessionlessAuthenticationType instance.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <remarks>The provided <c>IfyContext</c> is not used except for loading the information from the database, since instances of this class will be used within a static collection and other methods must use their own <c>IfyContext</c>.</remarks>
        public SessionlessAuthenticationType(IfyContext context) : base(context) {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Obtains the user information from the current HTTP request.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        /// <param name="strict">If set to <c>true</c> the method should never return null but throws always an exception even if the authentication type does not apply.</param>
        /// <returns>An instance of User representing the user profile information or <c>null</c> if no such information is available.</returns>
        public override User GetUserProfile(IfyWebContext context, HttpRequest request, bool strict) {
            if (!IsEnabled) throw new InvalidOperationException("Sessionless requests are not allowed");

            if (request == null) throw new ArgumentNullException("request", "Missing request information");

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

        /// <summary>Always returns false (method is not applicable to this authentication type).</summary>
        /// <returns><c>false</c> unlike the expected behaviour for authentication types with no external identity provider (<c>true</c>) because SessionlessAuthenticationType instances do not support essions at all.</returns>
        /// <param name="context">The execution environment context.</param>
        /// <param name="request">The current HttpRequest to analyse.</param>
        public override bool IsExternalSessionActive(IfyWebContext context, HttpRequest request) {
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        private string GetUsername(HttpRequest request) {
            return request.QueryString["_user"];
        }

    }

}

