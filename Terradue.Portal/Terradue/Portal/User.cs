using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Terradue.OpenSearch.Result;
using Terradue.Portal.OpenSearch;
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

    

    /// <summary>User</summary>
    /// <description>
    /// Basic object representing a user in the system.
    /// </description>
    /// \ingroup Core
    /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
    [EntityTable("usr", EntityTableConfiguration.Custom, IdentifierField = "username", AutoCorrectDuplicateIdentifiers = true, AllowsKeywordSearch = true)]
    public class User : EntitySearchable {

        private bool emailChanged;
        
        private string accessibleResourcesString;

        public override bool CanView {
            get { return base.CanView || UserId == Id; }
        }
        
        public override bool CanChange {
            get { return base.CanChange || UserId == Id; }
        }

        public override bool CanDelete {
            get { return base.CanDelete || UserId == Id; }
        }

        private string activationtoken;
        public string ActivationToken {
            get {
                if (string.IsNullOrEmpty(activationtoken)) {
                    activationtoken = GetActivationToken();
                    if (string.IsNullOrEmpty(activationtoken)) activationtoken = CreateActivationToken();
                }
                return activationtoken;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the username.</summary>
        /// \ingroup Authorisation
        public string Username {
            get { return Identifier; }
            set { Identifier = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Activation status of the user account.</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation" 
        [EntityDataField("status")]
        public int AccountStatus { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the user of this account needs to confirm his email address.</summary>
        public bool NeedsEmailConfirmation { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the permission level of the user account.</summary>
        [EntityDataField("level")]
        public int Level { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the e-mail address of the user.</summary>
        [EntityDataField("email", IsUsedInKeywordSearch = true)]
        public string Email { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the first name of the user.</summary>
        [EntityDataField("firstname", IsUsedInKeywordSearch = true)]
        public string FirstName { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the last name of the user.</summary>
        [EntityDataField("lastname", IsUsedInKeywordSearch = true)]
        public string LastName { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the affiliation (company, organsation or similar) of the user.</summary>
        [EntityDataField("affiliation", IsUsedInKeywordSearch = true)]
        public string Affiliation { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the country of the user.</summary>
        [EntityDataField("country")]
        public string Country { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the preferred language of the user.</summary>
        [EntityDataField("language")]
        public string Language { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the time zone identifier of the user.</summary>
        [EntityDataField("time_zone")]
        public string TimeZone { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the rules for normal user accounts apply to the user account.</summary>
        [EntityDataField("normal_account")]
        public bool IsNormalAccount { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether username/password authentication is allowed for the user account.</summary>
        [EntityDataField("allow_password")]
        public bool PasswordAuthenticationAllowed { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether sessionless requests from trusted hosts are allowed for the user account.</summary>
        [EntityDataField("allow_sessionless")]
        public bool SessionlessRequestsAllowed { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the user must connect from a trusted host, regardless of the chosen authentication type.</summary>
        [EntityDataField("force_trusted")]
        public bool ForceTrustedHosts { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or decides whether the user must connect via SSL.</summary>
        [EntityDataField("force_ssl")]
        public bool ForceSecureConnection { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the degree of debug information shown to the user.</summary>
        [EntityDataField("debug_level")]
        public int DebugLevel { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the total processing credits of the user.</summary>
        [EntityDataField("credits")]
        public int TotalCredits { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("last_password_change_time")]
        public DateTime LastPasswordChangeTime { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        [EntityDataField("failed_logins")]
        public int FailedLogins { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the first and last name or, if unavailable, the username of the user.</summary>
        public string Caption {
            get { return FirstName == null || LastName == null ? Username : String.Format("{0} {1}", FirstName, LastName); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool PasswordExpired { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        protected bool UseOpenId { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public static IUserProfileExtension ProfileExtension { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool IgnoreExtensions { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the registration origin.
        /// </summary>
        /// <value>The registration origin.</value>
        public string RegistrationOrigin { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the registration date.
        /// </summary>
        /// <value>The registration date.</value>
        public DateTime RegistrationDate { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance.</summary>
        /// <param name="context">The execution environment context.</param>
        public User(IfyContext context) : base(context) {}
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance.</summary>
        /// \ingroup Authorisation
        /// <param name="context">The execution environment context.</param>
        /// <returns>The created User object.</returns>
        public static User GetInstance(IfyContext context) {
            EntityType entityType = EntityType.GetEntityType(typeof(User));
            return (User)entityType.GetEntityInstance(context); 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the existing user with the specified username or creates that user with default account settings.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="username">The username.</param>
        /// <returns>The User instance. New users are not stored in the database, this must be done by the calling code.</returns>
        public static User GetOrCreate(IfyContext context, string username) {
            return GetOrCreate(context, username, null);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the existing user with the specified username or creates that user with default account settings.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="username">The username.</param>
        /// <param name="authenticationType">The used authentication type. This parameter can be null. If specified, the username is looked for only among the external usernames of the specified authentication type, otherwise among the web portal's own usernames.</param>
        /// <returns>The User instance. New users are not stored in the database, this must be done by the calling code.</returns>
        public static User GetOrCreate(IfyContext context, string username, AuthenticationType authenticationType) {
            User result = null;
            Activity activity = null;
            int userId = GetUserId(context, username, authenticationType);
            if (userId != 0) {
                var oldAccessLevel = context.AccessLevel;
                context.AccessLevel = EntityAccessLevel.Administrator;
                result = FromId(context, userId);
                context.AccessLevel = oldAccessLevel;
                return result;
            } else {
                IfyWebContext webContext = context as IfyWebContext;
                result = User.GetInstance(context);
                result.Username = username;
                result.AccountStatus = (authenticationType == null ? webContext == null ? AccountStatusType.Deactivated : webContext.DefaultAccountStatus : authenticationType.GetDefaultAccountStatus());
                result.Level = UserLevel.User;
                result.TimeZone = "UTC";
                result.IsNormalAccount = true;
                AuthenticationType authType;
                authType = IfyWebContext.GetAuthenticationType(typeof(PasswordAuthenticationType));
                result.PasswordAuthenticationAllowed = (authType != null && authType.NormalAccountRule == RuleApplicationType.Always);
                authType = IfyWebContext.GetAuthenticationType(typeof(PasswordAuthenticationType));
                result.SessionlessRequestsAllowed = (authType != null && authType.NormalAccountRule == RuleApplicationType.Always);

                activity = new Activity(context, result, EntityOperationType.Create);
                activity.Store();

                return result;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static bool DoesUserExist(IfyContext context, string username, AuthenticationType authenticationType) {
            return GetUserId(context, username, authenticationType) != 0;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static int GetUserId(IfyContext context, string username, AuthenticationType authenticationType) {
            return context.GetQueryIntegerValue(String.Format(authenticationType == null ? "SELECT id FROM usr WHERE username={0};" : "SELECT id_usr FROM usr_auth WHERE id_auth={1} AND username={0};", 
                    StringUtils.EscapeSql(username), 
                    authenticationType == null ? 0 : authenticationType.Id
            ));
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Links a user to the specified authentication type by the specified username.</summary>
        /// <param name="authenticationType">The authentication type.</param>
        /// <param name="username">The user's username on the external authentication provider.</param>
        public void LinkToAuthenticationProvider(AuthenticationType authenticationType, string username) {
            context.Execute(String.Format("INSERT INTO usr_auth (id_usr, id_auth, username) VALUES ({0}, {1}, {2});", 
                Id,
                authenticationType.Id,
                StringUtils.EscapeSql(username)
            ));
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance representing the user with the specified ID.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the user ID</param>
        /// <returns>the created User object</returns>
        public static User FromId(IfyContext context, int id) {
            User result = GetInstance(context);
            result.Id = id;
            result.Load();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance representing the user with the specified ID, ignoring permission- and privliege-based restrictions.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="id">the user ID</param>
        /// <returns>the created User object</returns>
        public static User ForceFromId(IfyContext context, int id) {
            User result = GetInstance(context);
            result.Id = id;
            result.Load(EntityAccessLevel.Administrator);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new User instance representing the user with the specified username, ignoring permission- and privliege-based restrictions.</summary>
        /// <param name="context">The execution environment context.</param>
        /// <param name="username">the user username</param>
        /// <returns>the created User object</returns>
        public static User ForceFromUsername(IfyContext context, string username) {
            User result = GetInstance(context);
            result.Username= username;
            result.Load(EntityAccessLevel.Administrator);
            return result;
        }

		//---------------------------------------------------------------------------------------------------------------------

		/// <summary>Creates a new User instance representing the user with the specified unique name, ignoring permission- and privliege-based restrictions.</summary>
		/// <param name="context">The execution environment context.</param>
		/// <param name="name">the unique user username</param>
		/// <returns>the created User object</returns>
		public static User FromUsername(IfyContext context, string username) {
			User result = GetInstance(context);
			result.Identifier = username;
			result.Load(EntityAccessLevel.Administrator);
			return result;
		}

		//---------------------------------------------------------------------------------------------------------------------

		/// <summary>Creates a new User instance representing the user with the specified unique email, ignoring permission- and privliege-based restrictions.</summary>
		/// <param name="context">The execution environment context.</param>
		/// <param name="email">the unique user email</param>
		/// <returns>the created User object</returns>
		public static User FromEmail(IfyContext context, string email) {
            User result = GetInstance(context);
            result.Email = email;
            result.Load(EntityAccessLevel.Administrator);
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns a User instance representing the user with the specified ID or username, ignoring permission- and privliege-based restrictions.</summary>
        /*!
        /// <param name="context">The execution environment context.</param>
        /// <param name="s">a search value that must match the User ID (preferred) or username</param>
        */
        public static User FromString(IfyContext context, string s) {
            int id = 0;
            Int32.TryParse(s, out id);
            User result = GetInstance(context);
            result.Id = id;
            result.Identifier = s;
            result.Load(EntityAccessLevel.Administrator);
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Get User from the activation token.
        /// </summary>
        /// <returns>The activation token.</returns>
        /// <param name="context">Context.</param>
        /// <param name="token">Token.</param>
        public static User FromActivationToken(IfyContext context, string token) {
            if (token == null) throw new ArgumentNullException("token", "No account key or token specified");
            int userId = context.GetQueryIntegerValue(String.Format("SELECT t.id_usr FROM usrreg AS t WHERE t.token={0};", StringUtils.EscapeSql(token)));
            if (userId == 0) throw new UnauthorizedAccessException("Invalid account key");

            return User.ForceFromId(context, userId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static void SetAccountStatus(IfyContext context, int[] ids, int accountStatus) {
            if (context.AccessLevel != EntityAccessLevel.Administrator) throw new EntityUnauthorizedException("You are not authorized to enable or disable user accounts", null, null, 0);
            string idsStr = "";
            for (int i = 0; i < ids.Length; i++) idsStr += (idsStr == "" ? "" : ",") + ids[i]; 
            string sql = String.Format("UPDATE usr SET status={0}{1} WHERE id{2};", 
                    accountStatus.ToString(),
                    accountStatus == AccountStatusType.Enabled ? ", failed_logins=0" : String.Empty,
                    ids.Length == 1 ? "=" + idsStr : " IN (" + idsStr + ")"
            );
            int count = context.Execute(sql);
            if (count > 0) context.WriteInfo(count + " user account" + (count == 1 ? " was " : "s were ") + (accountStatus == AccountStatusType.Enabled ? "enabled" : "disabled"));
            else context.WriteWarning("No user account has been " + (accountStatus == AccountStatusType.Enabled ? "enabled" : "disabled"));
            //OnItemProcessed(OperationType.Other, 0); // TODO
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void LoadRegistrationInfo() { 
            string sql = String.Format ("SELECT reg_origin, reg_date FROM usrreg WHERE id_usr={0};", this.Id);
            System.Data.IDbConnection dbConnection = context.GetDbConnection ();
            System.Data.IDataReader reader = context.GetQueryResult (sql, dbConnection);
            if (reader.Read ()) {
                if(reader.GetValue(0) != DBNull.Value) 
                    RegistrationOrigin = reader.GetString (0);
                if (reader.GetValue (1) != DBNull.Value)
                    RegistrationDate = reader.GetDateTime (1);
            }
            context.CloseQueryResult (reader, dbConnection);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Load() {
            base.Load();
            if (AccountStatus > AccountStatusType.Enabled) {
                NeedsEmailConfirmation = ((AccountStatus & AccountFlags.NeedsEmailConfirmation) != 0);
                AccountStatus = (AccountStatus & 7);
            }
            LoadRegistrationInfo();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override void Store() {
            bool isNew = !Exists;
            if (ProfileExtension != null && !IgnoreExtensions) {
                if (isNew) ProfileExtension.OnCreating(context, this); ProfileExtension.OnChanging(context, this);
            }
            //int appendix = 0;
            if (emailChanged) {
                NeedsEmailConfirmation = true;
                if (context.AutomaticUserMails) SendMail(UserMailType.EmailChanged, true);
            }
            if (NeedsEmailConfirmation) AccountStatus = AccountStatus | AccountFlags.NeedsEmailConfirmation;
            base.Store();
            AccountStatus = (AccountStatus & 7);
            if (ProfileExtension != null && !IgnoreExtensions) {
                if (isNew) ProfileExtension.OnCreated(context, this); ProfileExtension.OnChanged(context, this);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void StorePassword(string oldPassword, string newPassword) {
            if (!CheckPassword(oldPassword)) throw new UnauthorizedAccessException("The old password is incorrect");
            StorePassword(newPassword);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void StorePassword(string newPassword) {
            if (ProfileExtension != null && !IgnoreExtensions) ProfileExtension.OnPasswordChanging(context, this, newPassword);
            context.Execute(String.Format("UPDATE usr SET password=PASSWORD({1}) WHERE id={0}", Id, StringUtils.EscapeSql(newPassword)));
            if (ProfileExtension != null && !IgnoreExtensions) ProfileExtension.OnPasswordChanged(context, this, newPassword);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ChangeEmail(string newEmail) {
            emailChanged = true;
            Email = newEmail;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void Delete() {
            if (ProfileExtension != null && !IgnoreExtensions) ProfileExtension.OnDeleting(context, this);
            base.Delete();
            if (ProfileExtension != null && !IgnoreExtensions) ProfileExtension.OnDeleted(context, this);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void EnableAccount() {
            AccountStatus = AccountStatusType.Enabled;
            FailedLogins = 0;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        private string CreateActivationToken() {
            var token = Guid.NewGuid().ToString();
            context.LogInfo(this,"Create Activation token for user " + this.Username + " -- id_usr = " + Id + " ; token = " + token + " ; reg_date = " + DateTime.UtcNow.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss") + " ; reg_origin = " + (string.IsNullOrEmpty(RegistrationOrigin) ? "NULL" : StringUtils.EscapeSql(RegistrationOrigin)));
            try {
                context.Execute(String.Format("DELETE FROM usrreg WHERE id_usr={0};", Id));
                var sql = String.Format("INSERT INTO usrreg (id_usr, token, reg_date, reg_origin) VALUES ({0}, {1}, {2}, {3});",
                                              Id,
                                              StringUtils.EscapeSql(token),
                                              StringUtils.EscapeSql(DateTime.UtcNow.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss")),
                                         string.IsNullOrEmpty(RegistrationOrigin) ? "NULL" : StringUtils.EscapeSql(RegistrationOrigin));
                context.LogDebug(this, "Activation token sql : " + sql);
                context.Execute(sql);
                context.LogDebug(this, "Activation token created");
            }catch(Exception e){
                context.LogError(this, e.Message);
                throw e;
            }
            return token;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Sends a mail to a user.</summary>
        /// \ingroup Authorisation
        public bool SendMail(UserMailType type, bool forAuthenticatedUser) {
            IfyWebContext webContext = context as IfyWebContext; // TODO: replace

            string smtpHostname = context.GetConfigValue("SmtpHostname");
            string smtpUsername = context.GetConfigValue("SmtpUsername");
            string smtpPassword = context.GetConfigValue("SmtpPassword");
            int smtpPort = context.GetConfigIntegerValue("SmtpPort");
            bool smtpSsl = context.GetConfigBooleanValue("SmtpSSL");
            string mailSenderAddress = context.GetConfigValue("MailSenderAddress");
            string mailSender = context.GetConfigValue("MailSender");
            string emailConfirmationUrl = context.GetConfigValue("EmailConfirmationUrl");
            if (String.IsNullOrEmpty(emailConfirmationUrl)) {
                emailConfirmationUrl = String.Format(webContext.AccountRootUrl == null ? "{4}?_request={2}&key={3}" : "{0}{1}/{2}?key={3}",
                    webContext.BaseUrl,
                    webContext.AccountRootUrl,
                    type == UserMailType.Registration ? "activate" : "recover",
                    ActivationToken,
                    webContext.ScriptUrl
                );
            }

            if (mailSender == null) mailSender = mailSenderAddress;
            
            if (smtpHostname == null || mailSenderAddress == null) {
                string errorMessage;
                if (type == UserMailType.Registration && !forAuthenticatedUser) errorMessage = "Your account could not be created due to a server misconfiguration (registration mail cannot be sent)";
                else errorMessage = "Mail cannot be sent, missing values in SMTP account configuration (hostname or sender address)" + (context.UserLevel < UserLevel.Administrator ? ", this is a site administration issue" : String.Empty);
                throw new Exception(errorMessage);
            }

            Load();

            string subject = null, body = null;
            bool html = false;
            switch (type) {
                case UserMailType.Registration :
                    subject = context.GetConfigValue("RegistrationMailSubject");
                    body = context.GetConfigValue("RegistrationMailBody");
                    html = context.GetConfigBooleanValue("RegistrationMailHtml");
                    if (subject == null) subject = "Accout registration"; 
                    if (body == null) body = String.Format("Dear sir/madam,\n\nThank you for registering on {0}.\n\nYour username is: {1}\n\nBest regards,\nThe team of {0}\n\nP.S. Please do not reply to this mail, it has been generated automatically. If you think you received this mail by mistake, please ignore it.", context.GetConfigValue("SiteName"), Username);
                    break;
                    
                case UserMailType.PasswordReset :
                    subject = context.GetConfigValue("PasswordResetMailSubject");
                    body = context.GetConfigValue("PasswordResetMailBody");
                    html = context.GetConfigBooleanValue("PasswordResetMailHtml");
                    if (subject == null) subject = "Password reset"; 
                    if (body == null) body = String.Format("Dear sir/madam,\n\nYour password for your user account on {0} has been changed.\n\nYour username is: {1}\n\nBest regards,\nThe team of {0}\n\nP.S. Please do not reply to this mail, it has been generated automatically. If you think you received this mail by mistake, please take into account that your password has changed.", context.GetConfigValue("SiteName"), Username);
                    break;

                case UserMailType.EmailChanged :
                    subject = context.GetConfigValue("EmailChangedMailSubject");
                    body = context.GetConfigValue("EmailChangedMailBody");
                    html = context.GetConfigBooleanValue("EmailChangedMailHtml");
                    if (subject == null) subject = "E-mail changed"; 
                    if (body == null) body = String.Format("Dear sir/madam,\n\nYou changed your e-mail address linked to your user account on {0}.\n\nPlease confirm the email by clicking on the following link:\n{1}\n\nBest regards,\nThe team of {0}\n\nP.S. Please do not reply to this mail, it has been generated automatically. If you think you received this mail by mistake, please take into account that your e-mail address has changed.", context.GetConfigValue("SiteName"), ActivationToken);
                    break;
            }

            var baseurl = webContext.GetConfigValue ("BaseUrl");

            // activationToken also used here to avoid endless nested replacements
            subject = subject.Replace("$(SITENAME)", context.SiteName);
            body = body.Replace(@"\n", Environment.NewLine);
            body = body.Replace("$(", "$" + ActivationToken + "(");
            body = body.Replace("$" + ActivationToken + "(USERCAPTION)", Caption);
            body = body.Replace("$" + ActivationToken + "(USERNAME)", Username);
            body = body.Replace("$" + ActivationToken + "(SITENAME)", context.SiteName);
            body = body.Replace("$" + ActivationToken + "(SITEURL)", baseurl);
            body = body.Replace("$" + ActivationToken + "(ACTIVATIONURL)", emailConfirmationUrl.Replace("$(BASEURL)", baseurl).Replace("$(TOKEN)", ActivationToken));
            if (body.Contains("$" + ActivationToken + "(SERVICES)")) {
                body = body.Replace("$" + ActivationToken + "(SERVICES)", GetUserAccessibleResourcesString(Service.GetInstance(context), html));
            }
            if (body.Contains("$" + ActivationToken + "(SERIES)")) {
                body = body.Replace("$" + ActivationToken + "(SERIES)", GetUserAccessibleResourcesString(Series.GetInstance(context), html));
            }

            MailMessage message = new MailMessage();
            message.From = new MailAddress(mailSenderAddress, mailSender);
            message.To.Add(new MailAddress(Email, Email));
            message.Subject = subject;

            if (html) {
                AlternateView alternate = AlternateView.CreateAlternateViewFromString(body, new System.Net.Mime.ContentType("text/html"));
                message.AlternateViews.Add(alternate);
            } else {
                message.Body = body;
            }

            SmtpClient client = new SmtpClient(smtpHostname);
            
            // Add credentials if the SMTP server requires them.
            if (string.IsNullOrEmpty(smtpUsername)) {
                smtpUsername = null;
                client.Credentials = null;
                client.UseDefaultCredentials = false;
            } else if (smtpUsername != null) {
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            }
            if (smtpPassword == String.Empty) smtpPassword = null;

            if (smtpPort > 0)
                client.Port = smtpPort;

            client.EnableSsl = smtpSsl;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        
            try {
                client.Send(message);
            } catch (Exception e) {
                if (e.Message.Contains("CDO.Message") || e.Message.Contains("535")) context.AddError("Mail could not be sent, this is a site administration issue (probably caused by an invalid SMTP hostname or wrong SMTP server credentials)");
                else context.AddError("Mail could not be sent, this is a site administration issue: " + e.Message);
                throw;
            }
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Starts the new session.</summary>
        /// \ingroup Authorisation
        public virtual void StartNewSession() {
            context.Execute(String.Format("INSERT INTO usrsession (id_usr, log_time) VALUES ({0}, '{1}');", Id, context.Now.ToString(@"yyyy\-MM\-dd HH\:mm\:ss")));
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the user accessible resources string.
        /// </summary>
        /// <returns>The user accessible resources string.</returns>
        /// <param name="resource">Resource.</param>
        /// <param name="html">If set to <c>true</c> html.</param>
        /// \ingroup Authorisation
        public string GetUserAccessibleResourcesString(Entity resource, bool html) {
            return null;

            // TODO:
            /* 
            resource.UserId = Id;
            resource.GetValues(new ProcessValueSetElementCallbackType(ProcessUserResourceValueSetItem));
            if (accessibleResourcesString == null) accessibleResourcesString = String.Empty;
            if (html) {
                accessibleResourcesString = Regex.Replace(accessibleResourcesString, "([^" + Environment.NewLine + "]*)", "\t<li>$1</li>");
                accessibleResourcesString = "<ul>" + Environment.NewLine + accessibleResourcesString + Environment.NewLine + "</ul>"; 
            } else {
                accessibleResourcesString = Regex.Replace(accessibleResourcesString, "([^" + Environment.NewLine + "]*)", "* $1");
            }
            return accessibleResourcesString;*/
            
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        private void ProcessUserResourceValueSetItem(string value, string caption, int isDefault) {
            if (accessibleResourcesString == null) accessibleResourcesString = String.Empty; else accessibleResourcesString += Environment.NewLine;
            accessibleResourcesString += caption;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool CheckPassword(string password) {
            return context.GetQueryBooleanValue(String.Format("SELECT password=PASSWORD({1}) FROM usr WHERE id={0};", Id, StringUtils.EscapeSql(password)));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ValidateActivationToken(string token){
            if (token == null) throw new ArgumentNullException("No account key or token specified");
            int userId = context.GetQueryIntegerValue(String.Format("SELECT t.id_usr FROM usrreg AS t WHERE t.token={0};", StringUtils.EscapeSql(token)));
            if (userId != this.Id) throw new UnauthorizedAccessException("Invalid account key");
        }

        //---------------------------------------------------------------------------------------------------------------------

        private string GetActivationToken(){
            return context.GetQueryStringValue(String.Format("SELECT t.token FROM usrreg AS t WHERE t.id_usr={0};", this.Id));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void GenerateNewActivationToken() {
            var token = Guid.NewGuid().ToString();
            context.LogInfo(this, "Generate new Activation token for user " + this.Username + " -- id_usr = " + Id + " ; token = " + token);
            context.Execute(string.Format("UPDATE usrreg SET token='{0}' WHERE id_usr={1};", token, this.Id));
            activationtoken = token;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override AtomItem ToAtomItem(NameValueCollection parameters) {
            throw new NotImplementedException();
        }

    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public interface IUserProfileExtension {

        void OnCreating(IfyContext context, User user);

        void OnCreated(IfyContext context, User user);

        void OnChanging(IfyContext context, User user);

        void OnChanged(IfyContext context, User user);

        void OnPasswordChanging(IfyContext context, User user, string newPassword);

        void OnPasswordChanged(IfyContext context, User user, string newPassword);

        void OnDeleting(IfyContext context, User user);

        void OnDeleted(IfyContext context, User user);

        void OnSessionStarting(IfyContext context, User user, HttpRequest request);

        void OnSessionEnded(IfyContext context, User user, HttpRequest request);

    }


        
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public enum UserMailType {
        Registration,
        PasswordReset,
        EmailChanged
    }



    [Serializable]
    public class UserInformation {

        // public AuthenticationType AuthenticationType { get; protected set; }
        // public List<AuthenticationType> AllAuthenticationTypes { get; protected set; }
        public string AuthIdentifier { get; protected set; }
        public DateTime NextSessionRefreshTime { get; protected set; }
        public string ExternalUsername { get; protected set; }
        public bool NeedsEmailConfirmation { get; protected set; }

        public int OriginalUserId { get; protected set; }
        public int UserId { get; protected set; }
        public int UserLevel { get; protected set; }
        public string UserCaption { get; protected set; }
        public int UserDebugLevel { get; protected set; }
        public string UserTimeZone { get; protected set; }
        public bool PasswordExpired { get; protected set; }
        public string ExternalSessionToken { get; set; }
        //HttpContext.Current.Session["Credits"] = AvailableCredits;
        //HttpContext.Current.Session["RemainingProxyTime"] = RemainingProxyTime;

        public UserInformation(){}
        public UserInformation(AuthenticationType authenticationType, User user) {
            if (user == null) throw new ArgumentNullException("user", "User cannot be null");            
            this.OriginalUserId = user.Id;
            Update(authenticationType, user);
        }

        public void Update(AuthenticationType authenticationType, User user) {            
            UserId = user.Id;
            UserLevel = user.Level;
            UserDebugLevel = user.DebugLevel;
            //SimplifiedGui = user.SimplifiedGui;
            UserCaption = user.Caption;
            UserTimeZone = user.TimeZone;
            PasswordExpired = user.PasswordExpired;
            ExternalUsername = user.Username;
            NeedsEmailConfirmation = user.NeedsEmailConfirmation;            
            if (authenticationType != null) {
                this.AuthIdentifier = authenticationType.Identifier;
            //  if (AllAuthenticationTypes == null) AllAuthenticationTypes = new List<AuthenticationType>();
            //  if (!AllAuthenticationTypes.Contains(authenticationType)) AllAuthenticationTypes.Add(authenticationType);
            }
        }

/*        public void SetNewSessionRefreshTime(IfyContext context, HttpRequest request) {
            if (AuthenticationType.IsExternalSessionActive(context, request) && AuthenticationType.ExternalSessionRefreshPeriod != 0) {
                NextSessionRefreshTime = DateTime.UtcNow.AddSeconds(AuthenticationType.ExternalSessionRefreshPeriod);
            }
        }
*/
    }

}

