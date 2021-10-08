using System;
using System.Web;
using Terradue.Portal;
using Terradue.Ldap;

namespace Terradue.Authentication.OAuth
{

    /// <summary>
    /// Authentication open identifier.
    /// </summary>
    public class OAuth2AuthenticationType : AuthenticationType
    {

        /// <summary>
        /// The client.
        /// </summary>
        private Connect2IdClient client;
        
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
        public override bool IsExternalSessionActive (IfyWebContext context, HttpRequest request)
        {
            return true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.OAuth.OAuth2AuthenticationType"/> class.
        /// </summary>
        public OAuth2AuthenticationType (IfyContext context) : base (context)
        {
            client = new Connect2IdClient (context.GetConfigValue ("sso-configUrl"));
            client.SSOAuthEndpoint = context.GetConfigValue ("sso-authEndpoint");
            client.SSOApiClient = context.GetConfigValue ("sso-clientId");
            client.SSOApiSecret = context.GetConfigValue ("sso-clientSecret");
            client.SSOApiToken = context.GetConfigValue ("sso-apiAccessToken");
            client.SSOSessionStoreEndpoint = context.GetConfigValue ("sso-sessionEndpoint");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Terradue.Authentication.OAuth.OAuth2AuthenticationType"/> class.
        /// </summary>
        /// <param name="c">C.</param>
        public void SetConnect2IdCLient (Connect2IdClient c)
        {
            this.client = c;
        }

        /// <summary>
        /// Gets the user profile.
        /// </summary>
        /// <returns>The user profile.</returns>
        /// <param name="context">Context.</param>
        /// <param name="request">Request.</param>
        public override User GetUserProfile (IfyWebContext context, HttpRequest request = null, bool strict = false)
        {
            User usr;
            AuthenticationType authType = IfyWebContext.GetAuthenticationType (typeof (OAuth2AuthenticationType));

            var tokenrefresh = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-refresh"));
            var tokenaccess = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-access"));

            if (!string.IsNullOrEmpty (tokenrefresh.Value) && DateTime.UtcNow > tokenaccess.Expire) {
                // refresh the token
                try {
                    client.RefreshToken (tokenrefresh.Value);
                    tokenaccess = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-access"));
                } catch (Exception) {
                    return null;
                }
            }

            if (!string.IsNullOrEmpty (tokenaccess.Value)) {

                OauthUserInfoResponse usrInfo = client.GetUserInfo (tokenaccess.Value);

                if (usrInfo == null) return null;

                bool exists = User.DoesUserExist (context, usrInfo.sub, authType);
                usr = User.GetOrCreate (context, usrInfo.sub);

                if (usr.AccountStatus == AccountStatusType.Disabled) usr.AccountStatus = AccountStatusType.PendingActivation;

                //update user infos
                if (!string.IsNullOrEmpty (usrInfo.given_name))
                    usr.FirstName = usrInfo.given_name;
                if (!string.IsNullOrEmpty (usrInfo.family_name))
                    usr.LastName = usrInfo.family_name;
                if (!string.IsNullOrEmpty (usrInfo.email) && usrInfo.email_verifier)
                    usr.Email = usrInfo.email;
                if (!string.IsNullOrEmpty (usrInfo.zoneinfo))
                    usr.TimeZone = usrInfo.zoneinfo;
                if (!string.IsNullOrEmpty (usrInfo.locale))
                    usr.Language = usrInfo.locale;

                usr.Store ();
                if (!exists) usr.LinkToAuthenticationProvider (authType, usr.Username);
                return usr;
            } else {

            }

            return null;
        }

        public override void EndExternalSession (IfyWebContext context, HttpRequest request, HttpResponse response)
        {

            var sid = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-SID"));
            var tokenaccess = DBCookie.LoadDBCookie(context, context.GetConfigValue("cookieID-token-access"));
            try {
                client.DeleteSession (sid.Value);
            } catch (Exception) { }
            client.RevokeToken (tokenaccess.Value);
            DBCookie.RevokeSessionCookies(context);
        }

    }
}

