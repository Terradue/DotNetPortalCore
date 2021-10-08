using System;
using System.Web;
using Terradue.Portal;
using Terradue.Ldap;

namespace Terradue.Authentication.Ldap
{

    /// <summary>
    /// Authentication open identifier.
    /// </summary>
    public class LdapAuthenticationType : AuthenticationType {

        /// <summary>
        /// The client.
        /// </summary>
        protected Connect2IdClient client;

        /// <summary>
        /// Indicates whether the authentication type depends on external identity providers.
        /// </summary>
        /// <value><c>true</c> if uses external identity provider; otherwise, <c>false</c>.</value>
        public override bool UsesExternalIdentityProvider {
            get {
                return true;
            }
        }

        /// <summary>
        /// In a derived class, checks whether an session corresponding to the current web server session exists on the
        /// external identity provider.
        /// </summary>
        /// <returns><c>true</c> if this instance is external session active the specified context request; otherwise, <c>false</c>.</returns>
        /// <param name="context">Context.</param>
        /// <param name="request">Request.</param>
        public override bool IsExternalSessionActive(IfyWebContext context, HttpRequest request) {
            return true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the received email is trusted or not
        /// </summary>
        /// <value><c>true</c> if trust email; otherwise, <c>false</c>.</value>
        public bool TrustEmail { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the default user level at creation
        /// </summary>
        /// <value>default user level at creation</value>
        public int UserCreationDefaultLevel { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.OAuth.OAuth2AuthenticationType"/> class.
        /// </summary>
        public LdapAuthenticationType(IfyContext context) : base(context) {
            client = new Connect2IdClient(context.GetConfigValue("sso-configUrl"));
            client.SSOAuthEndpoint = context.GetConfigValue("sso-authEndpoint");
            client.SSOApiClient = context.GetConfigValue("sso-clientId");
            client.SSOApiSecret = context.GetConfigValue("sso-clientSecret");
            client.SSOApiToken = context.GetConfigValue("sso-apiAccessToken");
            client.SSOSessionStoreEndpoint = context.GetConfigValue("sso-sessionEndpoint");
            UserCreationDefaultLevel = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Authentication.OAuth.OAuth2AuthenticationType"/> class.
        /// </summary>
        /// <param name="c">C.</param>
        public void SetConnect2IdCLient(Connect2IdClient c) {
            this.client = c;
        }

        /// <summary>
        /// Gets the user profile.
        /// </summary>
        /// <returns>The user profile.</returns>
        /// <param name="context">Context.</param>
        /// <param name="request">Request.</param>
        public override User GetUserProfile(IfyWebContext context, HttpRequest request = null, bool strict = false) {
            User usr;
            AuthenticationType authType = IfyWebContext.GetAuthenticationType(typeof(LdapAuthenticationType));

            var tokenrefresh = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-refresh"));
            var tokenaccess = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-access"));

            context.LogDebug(this, string.Format("GetUserProfile -- tokenrefresh = {0} ; tokenaccess = {1}", tokenrefresh.Value, tokenaccess.Value));

            if (!string.IsNullOrEmpty(tokenrefresh.Value) && DateTime.UtcNow > tokenaccess.Expire) {
                // refresh the token
                try {
                    client.RefreshToken(tokenrefresh.Value);
                    tokenaccess = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-access"));
                    context.LogDebug(this, string.Format("GetUserProfile - refresh -- tokenrefresh = {0} ; tokenaccess = {1}", tokenrefresh.Value, tokenaccess.Value));
                } catch (Exception) {
                    return null;
                }
            }

            if (!string.IsNullOrEmpty(tokenaccess.Value)) {

                OauthUserInfoResponse usrInfo = client.GetUserInfo(tokenaccess.Value);

                context.LogDebug(this, string.Format("GetUserProfile -- usrInfo"));

                if (usrInfo == null) return null;

                context.LogDebug(this, string.Format("GetUserProfile -- usrInfo = {0}", usrInfo.sub));

                bool exists = User.DoesUserExist(context, usrInfo.sub, authType);
                usr = User.GetOrCreate(context, usrInfo.sub);

                if (usr.AccountStatus == AccountStatusType.Disabled) usr.AccountStatus = AccountStatusType.PendingActivation;
                
                if (!exists){
                    if (TrustEmail) usr.AccountStatus = AccountStatusType.Enabled;
                    usr.Level = UserCreationDefaultLevel;
                }

                //update user infos
                if (!string.IsNullOrEmpty(usrInfo.given_name))
                    usr.FirstName = usrInfo.given_name;
                if (!string.IsNullOrEmpty(usrInfo.family_name))
                    usr.LastName = usrInfo.family_name;
                if (!string.IsNullOrEmpty(usrInfo.email) && (TrustEmail || usrInfo.email_verifier))
                    usr.Email = usrInfo.email;
                if (!string.IsNullOrEmpty(usrInfo.zoneinfo))
                    usr.TimeZone = usrInfo.zoneinfo;
                if (!string.IsNullOrEmpty(usrInfo.locale))
                    usr.Language = usrInfo.locale;

                if (usr.Id == 0) {
                    usr.AccessLevel = EntityAccessLevel.Administrator;
                }
                usr.Store();
                if (!exists) usr.LinkToAuthenticationProvider(authType, usr.Username);
                return usr;
            } else {

            }

            context.LogDebug(this, string.Format("GetUserProfile -- return null"));

            return null;
        }

        public override void EndExternalSession(IfyWebContext context, HttpRequest request, HttpResponse response) {

            var sid = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-SID"));
            var tokenaccess = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-access"));
            try {
                client.DeleteSession(sid.Value);
            } catch (Exception) { }
            client.RevokeToken(tokenaccess.Value);
            DBCookie.RevokeSessionCookies(context);
        }

    }
}

