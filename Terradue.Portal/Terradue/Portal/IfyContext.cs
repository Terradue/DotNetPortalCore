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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Xsl;
using MySql.Data.MySqlClient;
using Terradue.Util;



/*!
\defgroup core_Context Context
@{
This component manages the general context for the entire system for a given session. It manages the authentication of the user with a generic username/password scheme or with an external authentication mechanism (e.g. SSO) based on HTTP headers.
It interacts with the database to store and read the persistent configuration data.
When the system is a web application, it provides an HTTP context (\ref Terradue.Portal#IfyWebContext with session, request, content-type) extended with application specific information (e.g. current user information)
 
\ingroup core
 
\section sec_core_ContextDependencies Dependencies
 
- \ref core_UserGroupACL, used to load/store users information
- \ref core_Configuration, used to manage configuration data
 
\section sec_core_ContextInterfaces Abstract Interfaces

Here all the interfaces that this components implements in abstract way. It means that the interfaces is not (yet) implemented as such but represent an interface for a dedicated function in the system.

| Interface ID | Type | Description |
| ------------ | ---- | ----------- |
| \ref IExternalAuthentication "IExternalAuthentication" | Sub-system internal | Provides an interface to plug a trusted external authentication system that maps on internal user representation |

@}
 */





//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------------------------------------------





/// \ingroup core
namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Supported relational database management systems</summary>
    public enum DatabaseSystemType {
        
        /// <summary>PostgreSQL (not supported yet)</summary>
        Postgres,

        /// <summary>MySQL</summary>
        MySql
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Levels of log messages</summary>
    public enum MessageType {

        /// <summary>Information messages (level 3)  </summary>
        Info,

        /// <summary>Warning messages (level 2)  </summary>
        Warning,

        /// <summary>Error messages (level 1)  </summary>
        Error
    };

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Levels of web portal users</summary>
    /// \ingroup core_Context
    public class UserLevel {
        
        /// <summary>Unregistered or unauthenticated users</summary>
        public const int Everybody = 0;
        
        /// <summary>Authenticated users</summary>
        public const int User = 1;

        /// <summary>Authenticated web portal developers</summary>
        public const int Developer = 2;

        /// <summary>Authenticated domain managers</summary>
        public const int Manager = 3;

        /// <summary>Authenticated web portal administrators</summary>
        public const int Administrator = 4;
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Options for the configuration of self-defined user accounts</summary>
    /// \ingroup core_Context
    public class AccountActivationRuleType {
        
        /// <summary>Unauthenticated users cannot create their own user accounts</summary>
        public const int Disabled = 0;

        /// <summary>Self-defined user accounts must activated by an administrator</summary>
        public const int ActiveAfterApproval = 1;

        /// <summary>Self-defined user accounts are activated after confirmation via e-mail link</summary>
        public const int ActiveAfterMail = 2;

        /// <summary>Self-defined user accounts are activated immediately</summary>
        public const int ActiveImmediately = 3;
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    /// <summary>Represents the execution environment context and manages the client connection, database access and other resources.</summary>
    /// \ingroup core_Context
    /// \xrefitem uml "UML" "UML Diagram"
    public abstract partial class IfyContext {

        private object lockObject = new Object();

        // Static fields backing public configuration properties
        private static bool dynamicDbConnectionsGlobal;
        private static string siteName;
        private static string installationRoot;
        private static string siteFolder;
        private static string siteRootFolder;
        private static string siteConfigFolder;
        private static string serviceFileRoot;
        private static string serviceParamFileRoot;
        private static string unprotectedBaseUrl;
        private static string hostCertificateFile;
        private static string hostCertificatePassword;
        private static string serviceWebRoot;
        private static bool synchronousTaskOperations;
        private static int taskSubmissionRetrying;
        private static TimeSpan defaultTaskSubmissionRetryingPeriod;
        private static string priorityValueString;
        private static PriorityValue[] priorityValues;
        private static FixedValueSet compressionValues;
        private static TimeSpan computingResourceStatusValidity;
        private static int defaultMaxNodesPerJob;
        private static int defaultMinArgumentsPerNode;

        public bool dynamicDbConnections = false;

        public const int MaxUserLevel = 4;
        public const string CertSubjectVariable = "CERT_SUBJECT";
        public const string CertPemContentVariable = "HTTP_SSL_CLIENT_CERT";
        
        private bool isUserIdentified;
        private string resultMetadataUploadUrl, resultMetadataDownloadUrl;

        
        // Fields for alert sending
        private string smtpHostname, smtpUsername, smtpPassword;
        private string mailSender, mailReceivers, mailCcReceivers, mailBccReceivers, mailMessageSubject, mailMessageBody;
        protected string lastErrorMessage;
        protected DateTime lastErrorTime;
        private string applicationName;
        private string inputContent;
        private bool sameLine;
        
        private DateTime now = DateTime.UtcNow;
        private TimeZoneInfo userTimeZone;

        private const string databaseSchemaVersion = "2.3";

        protected string mainConnectionString, currentConnectionString;
        protected IDbConnection mainDbConnection;
        protected bool nestedTransactions = false;
        private DatabaseSystemType databaseSystem = DatabaseSystemType.MySql;

        private List<IDbConnection> dbConnections = new List<IDbConnection>();

        private Application application;
        protected bool console;
        protected bool logging, debugging;
        private string logFilename, debugFilename;
        private bool bufferedLogging, logFileExists, debugFileExists;
        private TextWriter logFile, debugFile;

        private string username;
        protected string proxyUsername = null, proxyPassword = null;
        protected StringWriter strWriter, msgStrWriter;
        protected StreamWriter streamWriter;
        
        protected bool textOutput;
        protected bool noDatabase = false;
        
        private int logLevel = 3, debugLevel = 0;

        protected bool passwordExpired;

        public static bool DefaultConsoleDebug {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool ConsoleDebug { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets a value indicating whether this instance is interactive.
        /// </summary>
        /// <value><c>true</c> if this instance is interactive; otherwise, <c>false</c>.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public virtual bool IsInteractive {
            get { return false; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public string CoreVersion {
            get { return coreVersion; }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public string DatabaseSchemaVersion {
            get { return databaseSchemaVersion; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool DynamicDbConnectionsGlobal {
            get { return dynamicDbConnectionsGlobal; }
            protected set { 
                dynamicDbConnectionsGlobal = value;
                dynamicDbConnections = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool DynamicDbConnections {
            // BUG: This causes an error.
            get { return dynamicDbConnections && ConfigurationLoaded && !InTransaction; }
            //get { return true; }
            set { dynamicDbConnections = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool BufferedLogging {
            get { return bufferedLogging; }
            set {
                if (value == bufferedLogging) return;
                CloseLogFile();
                CloseDebugFile();
                bufferedLogging = value;
                OpenLogFile();
                OpenDebugFile();
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        /// <value>The log level.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public virtual int LogLevel {
            get { return logLevel; }
            set {
                if (value > 3) {
                    logLevel = 3;
                    debugLevel = value - 3;
                } else {
                    logLevel = (value < 0 ? 0 : value);
                    if (debugLevel != -1) debugLevel = 0;
                }
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the debug level.
        /// </summary>
        /// <value>The debug level.</value>
        /// \xrefitem uml "UML" "UML Diagram"
        public virtual int DebugLevel {
            get { return debugLevel; }
            set {
                debugLevel = (value < 0 ? 0 : value);
                if (value > 0) logLevel = 3;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public DateTime Now {
            get { return now; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public DatabaseSystemType DatabaseSystem {
            get { return databaseSystem; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected string ApplicationName {
            get { return applicationName; }
            set { applicationName = value; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public Application Application {
            get {
                if (application == null && ApplicationName != null) application = Application.FromIdentifier(this, ApplicationName);
                //bufferedLogging = true;
                return application;
            }
            protected set {
                application = value;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string LogFilename {
            get { return logFilename; }
            set { SetLogFile(value); }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string DebugFilename {
            get { return debugFilename; }
            set { SetDebugFile(value); }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public UserInformation UserInformation { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the ID of the logged or impersonated user.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public int UserId { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the ID of the user for whom new items are created.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public int OwnerId { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether administrator mode is used.</summary>
        /// <remarks>
        ///     In administrator mode, the entity's privilege properties are not set and user authorisation checks are not performed for default operations.
        ///     The administrator mode is only available for administrator and manager users. The default value is <i>false</i>.
        /// </remarks>
        public bool AdminMode { get; set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Indicates or determines whether normal user restrictions apply.</summary>
        /// <summary>
        ///     If AdminMode is set to <i>true</i>, that property takes precedence and RestrictedMode has no effect.
        ///     Otherwise, with RestrictedMode set to <i>true</i> restrictions are enforced already when the loading of an item is attempted. The restrictions are based on the value of UserId (usually the ID of the current user).
        ///     Setting RestrictedMode to <i>false</i> allows obtaining an item instances even if the current user has no privileges on the item at all.
        ///     The privilege properties (e.g. CanView) reflect the user's actual privileges and the calling code has to react accordingly. The default value is <i>true</i>.
        /// </summary>
        public bool RestrictedMode { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public int OriginalUserId { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the request originates from a registered user.</summary>
        /// \ingroup core_Context
        /// \xrefitem uml "UML" "UML Diagram"
        public bool IsUserIdentified {
            get {
                return isUserIdentified || IsUserAuthenticated;
            }
            protected set {
                isUserIdentified = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the request originates from a registered user.</summary>
        /// <remarks>Being <c>IsUserAuthenticated</c> true implies that also <c>IsUserIdentified</c> is true.</remarks>
        /// \ingroup core_Context
        /// \xrefitem uml "UML" "UML Diagram"
        public bool IsUserAuthenticated { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the authentication method.
        /// </summary>
        /// <value>The authentication method.</value>
        /// \ingroup core_Context
        /// \xrefitem uml "UML" "UML Diagram"
        public AuthenticationMethod AuthenticationMethod { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the level of the logged or impersonated user.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public int UserLevel { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the displayed name of the logged or impersonated user.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public string UserCaption { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the login name of the logged or impersonated user.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public string Username {
            get {
                if (username == null) username = GetQueryStringValue("SELECT username FROM usr WHERE id=" + UserId + ";");
                return username;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string ExternalUsername { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the user is allowed to use sessionless requests or is allowed to use task scheduling with at least one service.</summary>
        public bool UserSessionlessEnabled {
            get {
                //return UserInformation == null ? false : UserInformation.SessionlessRequestsEnabled;
                bool result = GetQueryBooleanValue(String.Format("SELECT allow_sessionless FROM usr WHERE id={0};", UserId));
                if (result) return true;
                return GetQueryIntegerValue(String.Format("SELECT COUNT(*) FROM service AS t INNER JOIN service_priv AS p ON t.id=p.id_service LEFT JOIN usr_grp AS ug ON p.id_grp=ug.id_grp INNER JOIN usr AS u ON (p.id_usr=u.id OR ug.id_usr=u.id) AND u.id={0} WHERE t.available=true AND p.allow_scheduling;", UserId)) != 0;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the time zone ID of the user.</summary>
        public string UserTimeZoneId { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the time zone ID of the user.</summary>
        public TimeZoneInfo UserTimeZone {
            get {
                if (userTimeZone != null) return userTimeZone;
                if (UserTimeZoneId == null || UserTimeZoneId == "UTC") {
                    userTimeZone = TimeZoneInfo.Utc;
                } else {
                    try {
                        userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(UserTimeZoneId);
                    } catch (Exception) {
                        userTimeZone = TimeZoneInfo.Utc;
                    }
                }
                return userTimeZone;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string ProxyUsername {
            get { return proxyUsername; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string ProxyPassword {
            get { return proxyPassword; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected Dictionary<string, string> UserFields { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string GetUserField(string name) {
            if (UserFields == null || !UserFields.ContainsKey(name)) return null;
            return UserFields[name];
            
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool NeedsEmailConfirmation {
            get { return (UserInformation != null && UserInformation.NeedsEmailConfirmation); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool AutomaticUserMails {
            get { return true; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool TextOutput {
            get { return textOutput; }
            set { textOutput = value; } // !!! problem if response already started
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates or determines whether messages are sent to the client.</summary>
        public bool HideMessages { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool Error { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool NoDatabase {
            get { return noDatabase; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected int TransactionLevel { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        protected bool InTransaction {
            get { return TransactionLevel > 0; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string HostUrl { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public static bool ConfigurationLoaded { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual string SiteName {
            get { return siteName; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the root folder of the Ifynet installation (the folder containing the <i>base</i> and <i>sites</i> folders).</summary>
        public virtual string InstallationRoot {
            get { return installationRoot; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the root folder of the currently used site (i.e. the folder directly below <i>sites</i>).</summary>
        public virtual string SiteFolder {
            get { return siteFolder; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the root folder (<i>root</i>) of the currently used site.</summary>
        public virtual string SiteRootFolder {
            get { return siteRootFolder; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the configuration folder (<i>config</i>) of the currently used site.</summary>
        public virtual string SiteConfigFolder {
            get { return siteConfigFolder; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the service root folder (<i>root/services</i>) of the currently used site.</summary>
        public virtual string ServiceFileRoot {
            get { return serviceFileRoot; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the service root folder (<i>root/services</i>) of the currently used site.</summary>
        public virtual string ServiceParamFileRoot {
            get { return serviceParamFileRoot; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the local unprotected base URL of the portal.</summary>
        public virtual string UnprotectedBaseUrl {
            get { return unprotectedBaseUrl; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the local base URL of the portal.</summary>
        public virtual string BaseUrl { get; set; } 

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the location of the PKCS#12-formatted host certificate file</summary>
        public virtual string HostCertificateFile {
            get { return hostCertificateFile; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Gets the password for the host certificate.</summary>
        public virtual string HostCertificatePassword {
            get { return hostCertificatePassword; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the absolute or relative URL of the service root folder for HTTP access.</summary>
        public virtual string ServiceWebRoot {
            get { return serviceWebRoot; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether task operations such as submission or abortion are performed immediately (synchronously) with the request processing.</summary>
        public virtual bool SynchronousTaskOperations {
            get { return synchronousTaskOperations; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual int TaskSubmissionRetrying {
            get { return taskSubmissionRetrying; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the default length of the period during which a task is submitted again if the submission fails because of capacity problems</summary>
        public virtual TimeSpan DefaultTaskSubmissionRetryingPeriod {
            get { return defaultTaskSubmissionRetryingPeriod; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual string PriorityValueString {
            get { return priorityValueString; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual PriorityValue[] PriorityValues {
            get { return priorityValues; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual FixedValueSet CompressionValues {
            get { return compressionValues; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual TimeSpan ComputingResourceStatusValidity {
            get { return computingResourceStatusValidity; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual int DefaultMaxNodesPerJob {
            get { return defaultMaxNodesPerJob; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual int DefaultMinArgumentsPerNode {
            get { return defaultMinArgumentsPerNode; } 
            protected set { throw new GlobalConfigurationReadOnlyException(); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string CustomBaseUrl {
            get {
                return BaseUrl;
            }
            protected set {
                this.BaseUrl = value;
            }
                    
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the base URL for the publishing of result information files, such as image previews.</summary>
        public string ResultMetadataUploadUrl {
            get {
                if (resultMetadataUploadUrl == null) resultMetadataUploadUrl = GetQueryStringValue("SELECT CASE WHEN t.upload_url IS NOT NULL THEN t.upload_url ELSE CONCAT(t.protocol, '://', CASE WHEN t.username IS NULL THEN '' ELSE CONCAT(t.username, CASE WHEN t.password IS NULL THEN '' ELSE CONCAT(':', t.password) END, '@') END, t.hostname, CASE WHEN t.port IS NULL THEN '' ELSE CONCAT(':', CAST(t.port AS char)) END, CASE WHEN t.path IS NULL THEN '' ELSE CONCAT('/', t.path) END) END FROM pubserver AS t WHERE metadata;");
                return resultMetadataUploadUrl;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets the base URL for the publishing of result information files, such as image previews.</summary>
        public string ResultMetadataDownloadUrl {
            get {
                if (resultMetadataDownloadUrl == null) resultMetadataDownloadUrl = GetQueryStringValue("SELECT CASE WHEN t.download_url IS NOT NULL THEN t.download_url WHEN t.upload_url IS NOT NULL THEN t.upload_url ELSE CONCAT(t.protocol, '://', t.hostname, CASE WHEN t.port IS NULL THEN '' ELSE CONCAT(':', CAST(t.port AS char)) END, CASE WHEN t.path IS NULL THEN '' ELSE CONCAT('/', t.path) END) END FROM pubserver AS t WHERE t.metadata;");
                return resultMetadataDownloadUrl;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public bool MultipleErrors { get; set; }
                
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new generic IfyContext instance with the spcified database connection string.</summary>
        /*!
        /// <param name="mainConnectionString">the database connection string</param>
        /// <param name="console">determines whether the IfyContext is used from a console application </param>
        */
        public IfyContext(string connectionString) {
            this.mainConnectionString = connectionString;
            this.RestrictedMode = true;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new domain-specific local context.</summary>
        public static IfyLocalContext GetLocalContext(string connectionString, bool console) {
            // e.g. "Terradue.Ify.Service, Ify, Version=2, Culture=neutral, PublicKeyToken=8e323390df8d9ed4"
            // e.g. "Terradue.Ify.IfyContext, Ify, Version=null, Culture=neutral, PublicKeyToken=null"
            string className = System.Configuration.ConfigurationManager.AppSettings["LocalContextClass"];
            if (className == null) return new IfyLocalContext(connectionString, console);
            Type type = Type.GetType(className, true);
            System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[]{typeof(string), typeof(bool)});
            return (IfyLocalContext)ci.Invoke(new object[] {connectionString, console});
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new domain-specific local context.</summary>
        public static IfyLocalContext GetLocalContext(string className, string connectionString, bool console) {
            // e.g. "Terradue.Ify.Service, Ify, Version=2, Culture=neutral, PublicKeyToken=8e323390df8d9ed4"
            // e.g. "Terradue.Ify.IfyContext, Ify, Version=null, Culture=neutral, PublicKeyToken=null"
            if (className == null) return new IfyLocalContext(connectionString, console);
            Type type = Type.GetType(className, true);
            System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[]{typeof(string), typeof(bool)});
            return (IfyLocalContext)ci.Invoke(new object[] {connectionString, console});
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new domain-specific local context.</summary>
        public static IfyLocalContext GetLocalContext(string connectionString, string baseUrl, string applicationName) {
            // e.g. "Terradue.Ify.Service, Ify, Version=2, Culture=neutral, PublicKeyToken=8e323390df8d9ed4"
            // e.g. "Terradue.Ify.IfyContext, Ify, Version=null, Culture=neutral, PublicKeyToken=null"
            string className = System.Configuration.ConfigurationManager.AppSettings["LocalContextClass"];
            if (className == null) return new IfyLocalContext(connectionString, baseUrl, applicationName);
            Type type = Type.GetType(className, true);
            System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[]{typeof(string), typeof(string), typeof(string)});
            return (IfyLocalContext)ci.Invoke(new object[] {connectionString, baseUrl, applicationName});
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new domain-specific local context.</summary>
        public static IfyLocalContext GetLocalContext(string className, string connectionString, string baseUrl, string applicationName) {
            // e.g. "Terradue.Ify.Service, Ify, Version=2, Culture=neutral, PublicKeyToken=8e323390df8d9ed4"
            // e.g. "Terradue.Ify.IfyContext, Ify, Version=null, Culture=neutral, PublicKeyToken=null"
            if (className == null) return new IfyLocalContext(connectionString, baseUrl, applicationName);
            Type type = Type.GetType(className, true);
            System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[]{typeof(string), typeof(string), typeof(string)});
            return (IfyLocalContext)ci.Invoke(new object[] {connectionString, baseUrl, applicationName});
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        // ! Creates a new domain-specific web context.
        /*public static IfyWebContext GetWebContext() {
            // e.g. "Terradue.Ify.Service, Ify, Version=2, Culture=neutral, PublicKeyToken=8e323390df8d9ed4"
            // e.g. "Terradue.Ify.IfyContext, Ify, Version=null, Culture=neutral, PublicKeyToken=null"
            Type type = Type.GetType(System.Configuration.ConfigurationManager.AppSettings["WebContextClass"], true);
            System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[0]);
            return (IfyWebContext)ci.Invoke(new object[] {});
        }*/
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Creates a new domain-specific web context.</summary>
        public static IfyWebContext GetWebContext(PagePrivileges privileges) {
            // e.g. "Terradue.Ify.Service, Ify, Version=2, Culture=neutral, PublicKeyToken=8e323390df8d9ed4"
            // e.g. "Terradue.Ify.IfyContext, Ify, Version=null, Culture=neutral, PublicKeyToken=null"
            string className = System.Configuration.ConfigurationManager.AppSettings["WebContextClass"];
            if (className == null) return new IfyWebContext(privileges);
            Type type = Type.GetType(className, true);
            System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[] {typeof(PagePrivileges)});
            if (ci == null) return new IfyWebContext(privileges);
            return (IfyWebContext)ci.Invoke(new object[] {privileges});
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static IfyWebContext GetServiceWebContext(PagePrivileges privileges) {
            // e.g. "Terradue.Ify.Service, Ify, Version=2, Culture=neutral, PublicKeyToken=8e323390df8d9ed4"
            // e.g. "Terradue.Ify.IfyContext, Ify, Version=null, Culture=neutral, PublicKeyToken=null"
            string className = System.Configuration.ConfigurationManager.AppSettings["WebContextClass"];
            if (className == null) return new IfyWebContext(privileges);
            Type type = Type.GetType(className, true);
            System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[] {typeof(PagePrivileges)});
            IfyWebContext result;
            if (ci == null) result = new IfyWebContext(privileges);
            else result = (IfyWebContext)ci.Invoke(new object[] {privileges});
            if (result.RequestedOperation == "dyndef") privileges.MinUserLevelView = Terradue.Portal.UserLevel.Everybody;
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Opens the database connection.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public virtual void Open() {
            dynamicDbConnections = dynamicDbConnectionsGlobal;
            if (!dynamicDbConnections || !ConfigurationLoaded) {
                mainDbConnection = new MySqlConnection(mainConnectionString);
                nestedTransactions = false;
                try {
                    mainDbConnection = GetNewDbConnection();
                } catch (Exception e) {
                    if (e.Message.Contains("Unknown database")) {
                        noDatabase = true;
                        throw new Exception("Application database not available");
                    } else {
                        throw e;
                    }
                }
            }
            if (!ConfigurationLoaded) {
                LoadConfiguration();
                ConfigurationLoaded = true;
                if (DynamicDbConnections) CloseDbConnection(mainDbConnection);
                CreateLogger();
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Closes all open database connection and releases other resources.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public virtual void Close() {
            foreach (IDbConnection dbConnection in dbConnections) {
                dbConnection.Close();
            }
            if (mainDbConnection != null) mainDbConnection.Close();
            if (logging) CloseLogFile();
            if (debugging) CloseDebugFile();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// \ingroup core_Context
        /// \xrefitem uml "UML" "UML Diagram"
        public void LoadConfiguration() {
            bool newConnection = (mainDbConnection == null);
            if (newConnection) mainDbConnection = GetNewDbConnection();
            TransactionLevel = 1;

            siteName = GetConfigValue("SiteName");
            if (HttpContext.Current == null) {
                DirectoryInfo dirInfo = new DirectoryInfo(Environment.GetCommandLineArgs()[0]);
                string binDir = Regex.Replace(dirInfo.FullName, @"[^\\/]*$", String.Empty);
                installationRoot = Regex.Replace(binDir, @"[\\/][^\\/]+[\\/][^\\/]+[\\/][^\\/]+[\\/]bin[\\/]$", "");
                siteFolder = Regex.Replace(binDir, @"[\\/][^\\/]+[\\/]bin[\\/]$", String.Empty);
                if (serviceParamFileRoot == null) serviceParamFileRoot = Regex.Replace(binDir, @"[\\/]bin[\\/]$", "/services");
            } else {
                installationRoot = Regex.Replace(HttpContext.Current.Request.PhysicalApplicationPath, @"[\\/][^\\/]+[\\/][^\\/]+[\\/][^\\/]+/?$", String.Empty);
                siteFolder = Regex.Replace(HttpContext.Current.Request.PhysicalApplicationPath, @"[\\/][^\\/]+/?$", String.Empty);
                if (serviceParamFileRoot == null) serviceParamFileRoot = Regex.Replace(HttpContext.Current.Request.PhysicalApplicationPath, @"/?$", "/services");
            }
            siteRootFolder = SiteFolder + "/root";
            siteConfigFolder = SiteFolder + "/config";
            serviceFileRoot = GetConfigValue("ServiceFileRoot");
            serviceParamFileRoot = GetConfigValue("ServiceParamFileRoot");

            unprotectedBaseUrl = GetConfigValue("BaseUrl");
            BaseUrl = unprotectedBaseUrl;
            hostCertificateFile = GetConfigValue("HostCertFile");
            hostCertificatePassword = GetConfigValue("HostCertPassword");
            serviceWebRoot = GetConfigValue("ServiceWebRoot");
            if (serviceWebRoot == null) serviceWebRoot = "/services";
            else serviceWebRoot = Regex.Replace(serviceWebRoot, "/$", String.Empty);
            synchronousTaskOperations = GetConfigBooleanValue("SyncTaskOperations");
            taskSubmissionRetrying = StringUtils.StringToSeconds(GetConfigValue("TaskRetry"));
            defaultTaskSubmissionRetryingPeriod = StringUtils.StringToTimeSpan(GetConfigValue("TaskRetryPeriod"));
            GetPriorityValues();
            GetCompressionValues();
            computingResourceStatusValidity = StringUtils.StringToTimeSpan(GetConfigValue("ComputingResourceStatusValidity"));
            defaultMaxNodesPerJob = GetConfigIntegerValue("DefaultMaxNodesPerJob");
            defaultMinArgumentsPerNode = GetConfigIntegerValue("DefaultMinArgumentsPerNode");

            EntityType.LoadEntityTypes(this);

            LoadAdditionalConfiguration();

            TransactionLevel = 0;
            if (newConnection) {
                mainDbConnection.Close();
                mainDbConnection = null;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected virtual void LoadAdditionalConfiguration() {}
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected void GetPriorityValues() {
            priorityValueString = GetConfigValue("PriorityValues");
            if (priorityValueString == null) priorityValueString = "0.25:Very low;0.5:Low;1:Normal;2:High;4:Very high";

            string[] priorityTerms = priorityValueString.Split(';');
            PriorityValue[] values = new PriorityValue[priorityTerms.Length];
            
            int count = 0;
            for (int i = 0; i < priorityTerms.Length; i++) {
                Match match = Regex.Match(priorityTerms[i], @"^(\*)?([^:]+):(.+)");
                if (!match.Success) continue;
                
                double value;
                if (!Double.TryParse(match.Groups[2].Value, out value) || value <= 0) continue;
                
                values[i] = new PriorityValue(value, match.Groups[3].Value);
                count++;
            }
            
            if (count != priorityTerms.Length) Array.Resize(ref values, count);
            priorityValues = values;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        protected void GetCompressionValues() {
            string compressionList = GetConfigValue("CompressionValues");
            if (compressionList == null) compressionList = "NONE:No compression;GZ:Compressed individually;TGZ:Compressed in one file;TAR:TAR archive";
            compressionValues = new FixedValueSet(compressionList);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void AddUserToGroup(string groupName) {
            AddUserToGroup(null, groupName);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void AddUserToGroup(string username, string groupName) {
            int userId = (username == null ? UserId : GetQueryIntegerValue(String.Format("SELECT id FROM usr WHERE username={0};", StringUtils.EscapeSql(username))));
            if (userId == 0) throw new InvalidOperationException(String.Format("User \"{0}\" does not exist", username));
            int groupId = GetQueryIntegerValue(String.Format("SELECT id FROM grp WHERE name={0};", StringUtils.EscapeSql(groupName)));
            if (groupId == 0) throw new InvalidOperationException(String.Format("Group \"{0}\" does not exist", groupName));
            AddUserToGroup(userId, groupId);
        }
            
        //---------------------------------------------------------------------------------------------------------------------

        public void AddUserToGroup(int groupId) {
            if (UserId == 0) throw new InvalidOperationException("No user currently logged in");
            AddUserToGroup(UserId, groupId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void AddUserToGroup(int userId, int groupId) {
            if (!GetQueryBooleanValue(String.Format("SELECT id_usr IS NOT NULL FROM usr_grp WHERE id_usr={0} AND id_grp={1};", userId, groupId))) {
                Execute(String.Format("INSERT INTO usr_grp (id_usr, id_grp) VALUES ({0}, {1});", userId, groupId));
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void RemoveUserFromGroup(string groupName) {
            RemoveUserFromGroup(null, groupName);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void RemoveUserFromGroup(string username, string groupName) {
            int userId = (username == null ? UserId : GetQueryIntegerValue(String.Format("SELECT id FROM usr WHERE username={0};", StringUtils.EscapeSql(username))));
            int groupId = GetQueryIntegerValue(String.Format("SELECT id FROM grp WHERE name={0};", StringUtils.EscapeSql(groupName)));
            if (userId == 0 || groupId == 0) return; 
            RemoveUserFromGroup(userId, groupId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void RemoveUserFromGroup(int groupId) {
            RemoveUserFromGroup(UserId, groupId);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void RemoveUserFromGroup(int userId, int groupId) {
            Execute(String.Format("DELETE FROM usr_grp WHERE id_usr={0} AND id_grp={1};", userId, groupId));
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Returns the constructor for the correct scheduler subclass for the specified scheduler type or scheduler item.</summary>
        /// <remarks></remarks>
        public System.Reflection.ConstructorInfo GetConstructor(int extensionTypeId, Type[] args) {
            string fullClassName = GetQueryStringValue(String.Format("SELECT t.class FROM type AS t WHERE t.id={0};", extensionTypeId));
            if (fullClassName != null) {
                Type type = Type.GetType(fullClassName, true);
                if (type != null) return type.GetConstructor(args);
            }
            return null;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the constructor for the correct scheduler subclass for the specified scheduler type or scheduler item.</summary>
        /// <remarks></remarks>
        public System.Reflection.ConstructorInfo GetConstructor(string table, string condition, Type[] args) {
            string fullClassName = GetQueryStringValue(String.Format("SELECT t1.class FROM {0} AS t INNER JOIN type AS t1 ON t.id_type=t1.id WHERE {1};", table, condition));
            if (fullClassName != null) {
                Type type = Type.GetType(fullClassName, true);
                if (type != null) return type.GetConstructor(args);
            }
            return null;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public IDataReader GetExtensionTypeQuery(Type type) {
            EntityType entityType = EntityType.GetEntityType(type);
            return GetQueryResult(String.Format("SELECT id, caption_sg FROM type WHERE id_super={0};", entityType == null ? 0 : entityType.TopTypeId));
        }

        //---------------------------------------------------------------------------------------------------------------------
        
		public byte[] GetCertificatePkcs12Content(int userId) {
            byte[] result = null;
            bool error = false;
            
            //IDataReader reader = GetQueryResult(String.Format("SELECT t.proxy_username, t.proxy_password, LENGTH(t1.cert_content), CAST(t1.cert_content AS BINARY) FROM usr AS t LEFT JOIN usrcert AS t1 ON t.id=t1.id_usr WHERE t.id={0};", UserId));
            IDbConnection dbConnection = GetDbConnection();
			IDataReader reader = GetQueryResult(String.Format("SELECT t.proxy_username, t.proxy_password, t1.cert_content_base64 FROM usr AS t LEFT JOIN usrcert AS t1 ON t.id=t1.id_usr WHERE t.id={0};", userId), dbConnection);
            if (reader.Read()) {
                if (reader.GetValue(2) == DBNull.Value) {
                    reader.Close();
                    throw new Exception("No certificate information available");
                } else {
                    /*int length = reader.GetInt32(2);
                    result = new byte[length];
                    reader.GetBytes(3, 0, result, 0, length);*/
                    // !!! TODO RESOLVE THIS
                    try {
                        result = Convert.FromBase64String(reader.GetString(2));
                    } catch (Exception) {
                        error = true;
                    }
                }
            }
            CloseQueryResult(reader, dbConnection);
            if (error) throw new Exception("Invalid certificate information");
            
            // !!! TODO: check root certificate or import it into keystore
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string GetCertificatePemContent(int userId) {
            string result = null;
            
            //IDataReader reader = GetQueryResult(String.Format("SELECT t.proxy_username, t.proxy_password, LENGTH(t1.cert_content), CAST(t1.cert_content AS BINARY) FROM usr AS t LEFT JOIN usrcert AS t1 ON t.id=t1.id_usr WHERE t.id={0};", UserId));
            IDbConnection dbConnection = GetDbConnection();
            IDataReader reader = GetQueryResult(String.Format("SELECT t1.cert_content_pem FROM usr AS t LEFT JOIN usrcert AS t1 ON t.id=t1.id_usr WHERE t.id={0};", userId), dbConnection);
            if (reader.Read()) {
                if (reader.GetValue(0) == DBNull.Value) {
                    reader.Close();
                    throw new Exception("No certificate information available");
                } else {
                    result = reader.GetString(0);
                    //result = GetBytesFromHexString("308209590201033082091F06092A864886F70D010701A08209100482090C30820908308203BF06092A864886F70D010706A08203B0308203AC020100308203A506092A864886F70D010701301C060A2A864886F70D010C0106300E0408B7175B85F6D5EA1702020800808203786E16613711942DAEC9CDBAFD113BA6A6F6FBD5EE65A2299246F2DC59C68E7AC9DF983FD669C2546CA41900D1AA6C87F07DA613345811CA01B578C027FD409EDBE15195EE46E36CF56BB268CBC67223E5F87F2966B580EA7C04E0E8DB93E654E21457D152968B2CF222122FC84D659AF283BECD6CDA143C089260B7E26FFBE50D91526F4933ADA1FF9A79B4F9E0CA45CB9F3C3BC7B6FF82CA965453965E4AD281677079C93CBACA840D0A5598FF9E1910ABA7D96018403888ADC32F93883C58052EF00C67BCBE6922A941B70125DA0C6176CE04831B5D4FEFEEEAF6D44DD6961B87CCCB06A02F25E03754790083BF10B47513D4D3D83DA8CA1A960F854F64FB23FF41E5284B159C34A72E1C19008311CF2FBB9C31FAE82071A81CD5C5C38037B41BBE03C00F99315B81D7B1F21422C39674C93EE6124D12596B5E310655352F3A2D02CB10AB2B5E9BD529D89A5A4C6BF9137A13A758062D55D4A249877210AC49288B2062ABBA796E11466F2A4BFD0741EFF2981DE07635201B0FD2B444E48F63CE784CC257031991A7FC9F44DD6C7CD03B4E72419C571006EBA8475D850616156BE63A90B23BB33DAC5FA532E372FB73AF231479230979A6CE2E2BD5073C990D934324C38257F44CEBF07E5C5E3310F619BE545AC17350007B5071A8DCF27634523A84E177A9D6A96597BC857AC0B0CBDAFA49469E5ABE2DC739CF9BBED37596B072E79503D2A72FF78ACDA81BE09E60436F2F0DE3612FDD5BC28D4226C81593DBAFCC2E07B12FC74C8F043D9F1DC474D0CDC325E2E48D4F8DC0D666CFE05DC248CA4F9F04C04DFD9E07B96A7ABFFE4C7D0AB6A8B564EDA4BD16FA486380552C077317231D2D3115D3EC99FA142398D5A8CDB715AD3AED2C8F5642BB6F016193857C368A4827EE14462C9148AA0872EE1EC4F7B30DD291E45F89115B979DA113DCB6B9CBAF198026265EC500A36E9B595278EBCBFB41E99E0C330E943714056A7550D0A6C2D37E7E3E2A933C10272B2DB0D79E84E046935ACD9F1155FEF1CCBE50CD64328B8FF44EDA3119394143212B18B4484136AC71E55785C5C22B4EA85263D40CC048D3FEB64869DC1020EDDA67DAE1A961C508E3FD64FAC88611A43B83EC937B2CDD1FACBF37336C7490847D2BEC38AE2E52CFC617A8AD85A45092257B2B479298BB7FCAE7B3FF584F72466851BBD8928158ADFD3667254E86EF4885113E6BBBE2346689BE7A6507BCCEF6653ED0EF6672404349FA3082054106092A864886F70D010701A08205320482052E3082052A30820526060B2A864886F70D010C0A0102A08204EE308204EA301C060A2A864886F70D010C0103300E0408C2B69BCCC05FA36702020800048204C80F2909F7B1C0B121B1AFCDBF9B032F6B8E31EEF0CC1339E90B80979CB6E81459B457A378F364786381FC03574D40D7F688833D265734F83244DCBCBAAB3D75DEE7154170C69248241A8AEC6E7B76B97936F7C96B45515036ABA36C7B2EEEC06EED58016C23B44F085638EB63CB97695ABCA25F5181DAB5926EDEF01A89C44F9E32AC425B5322228C3B3ECA7112434168E890AB079A9C564518D14411228F7D11152A4D68BD31422971936F0C024809716E92494069C6BA913DCCACDF0B6CF2A9FC3FF7D599EF1607A53DF6F93C44EE697B8AD8F73032462C5A3411216FD6C75DA4DA64294B94DB37962B61C097F648B21B2A40DCA6973E579048EECAE5395DD74492130987C56223E36BB497921B9291D82C5CCE6DAF247E5267E085C7FADCD39FD490223BC8A06213942393C1551FCC6DA53BFE7EC184DD73A7E343226528DFCD27FDF1318DE3727D987525BE6CA4B15BF0CA5254E0C30323DDBD301A4012CD02C322734A31F06DB92BCD205D049E01DF0C3D9FF5539B38881D554BE73B15F2EC0E49973B1B673BDCA0ADA36E20344BC676C43DA266B01DC29E04936E720CF99FF5AF1B15D04C642A4D6AD34E9979F361DD9AE1DF06DAE444DCA9C8302F6CEC3D13FBBE3F9A1D10429673A94812623B535FA903C0D6E0C57841DD904A086A7B1318FAA53DC5DE8270909F891880A54CEB1CFD02E3E0B8D4AC4F024D70E4680A73793492C03A05C7D241F63EE34DEA08264099F623776CEB7BC09956BCCBF13F87FE19F5EDF34CD12249AD25FCB53F3791645C00986B585738AAEA0407F2E06549B1EC829586AE0307C197B043E34F6F0F4498C8BFC5965F855CCA91C65A9E36DBB2B923ACFE45BA9827DBF799608A36D6662883E7EDDF8CF2F85B969D2A8906AD582FD57111A3D1709B96F4A1F40B35C80A26A5FE0267A4EA547709D1EE9464F88ACF7A10B3A752ED6DFCD7A18D7E416B548F3767834B1B485F02DC9A7E523E3643EEE3E8FB327A808BA1DC93A28616F7EDE29063F31DD6EE6E5139D5A4CDA63316502ED30E0BE01C07F05354CC9BD571B66E954660BD93178A7F2A0AD70DAF4CC46535CBF9459B1D6F51C989EC7DDA25349093263DDE43453B50377105D60E6477452386ECB1ECE92B367B8452ABA5E1A424172E751CBE6E4528644334C5071272BA18C1ABF967B46B1F08E418541DA0C06840E91CEADEE85151B477A5220EEC351F03DFEAEB2B932384AEE632618F2B52FE1A7A3FB7E48CF43E264B08943E42BBB87F964795F1C784EE2D2D308B16A7AE1E028A97991CE20FC4DBB4C2C66443DEEE731D7A085E20A2F02AEFCF0D147FBB7267BE7955FB9E84FB04B7DC3D73ED4991E85A9518AD8856EEE7AA0F65C1A067BE1583A14BD62D167E354C9B10A41AB96B45B8FC7A5B9C451F063AB01AB8C98B1E22309EF656BCD9CDA576315CF418079AA5038A59E91625B49243AD4A15523585490C8215E376D4D347BDCC4D13AD56E1DB7349F406EB466E94A24F557821313BD0E2C640C3E50DD9852E86702B0ADA77F23C452E07F6DA8854D6B81BACA89D681B7D50AA8723391769F89B5A6FD593F0D35EC4083069A80E8F2339B5CCCE7702399C2C671B4AA32760E1DD83F01D71CD62C7E8078DDCDE9E130517447A40AD6D9A710EAB09EB051357179A1FEFD9934CCD68FC0CEA52FF8CFDC5FB7E20C284096B31AE2409F9C78B06E033D2C5B4B19D1B8CD0A1F93125302306092A864886F70D01091531160414D597C42C87A65FAA961A272C5423E928A0461E1430313021300906052B0E03021A0500041427F7D68A84168B332FA6A28B592ED4B6115DE7EC040880547ADD5A28BC9002020800");
                    
                }
            }
            reader.Close();
            CloseQueryResult(reader, dbConnection);

            // !!! TODO: check root certificate or import it into keystore
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected byte[] GetBytesFromHexString(string s) {
            byte[] input = Encoding.ASCII.GetBytes(s);
            int length = s.Length / 2;
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++) {
                int index = 2 * i;
                byte v = (byte)(16 * (input[index] >= 48 && input[index] < 58 ? input[index] - 48 : input[index] >= 65 && input[index] < 71 ? input[index] - 55 : 0));
                index++;
                v += (byte)(input[index] >= 48 && input[index] < 58 ? input[index] - 48 : input[index] >= 65 && input[index] < 71 ? input[index] - 55 : 0);
                result[i] = v;
            }
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

		public virtual HttpWebRequest GetSslRequest(string url, string method, string contentType, int userId) {
			AddDebug(3, HostCertificateFile);
			return GetSslRequest(url, method, contentType, HostCertificateFile, userId);
		}

        //---------------------------------------------------------------------------------------------------------------------

		public virtual HttpWebRequest GetSslRequest(string url, string method, string contentType) {
			AddDebug(3, HostCertificateFile);
			return GetSslRequest(url, method, contentType, HostCertificateFile, OwnerId == 0 ? UserId : OwnerId);
		}

        //---------------------------------------------------------------------------------------------------------------------

		public HttpWebRequest GetSslRequest(string url, string method, string contentType, string certFileName, int userId) {
            HttpWebRequest request = null;
            try {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method;
                if (contentType != null) request.ContentType = contentType;
                if (certFileName == null) {
					byte[] certContent = GetCertificatePkcs12Content(userId);
                    if (certContent != null) request.ClientCertificates.Add(new X509Certificate2(certContent, String.Empty, X509KeyStorageFlags.DefaultKeySet));
                } else {
                    string certPassword = GetConfigValue("HostCertPassword");
                    request.ClientCertificates.Add(new X509Certificate2(certFileName, certPassword == null ? String.Empty : certPassword, X509KeyStorageFlags.DefaultKeySet));
                    string userCertContent = GetCertificatePemContent(userId);
                    if (userCertContent != null) request.Headers.Add("SSL_CLIENT_CERT_PROXY", userCertContent);
                }
            } catch (Exception e) {
                throw;
            }

            ServicePointManager.ServerCertificateValidationCallback = delegate(object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) {
                return true;
            };
            
            return request;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual bool ExecuteAgentActionMethod(string className, string methodName) {
            if (className == null || methodName == null) return false;
            
            Type type = Type.GetType(className, false);
            if (type == null) {
                AddWarning(String.Format("Class not found: {0}", className));
                return false;
            }
            
            System.Reflection.MethodInfo methodInfo = type.GetMethod(methodName, new Type[]{typeof(IfyContext)});
            if (methodInfo == null) {
                AddWarning(String.Format("Method not found: {0}", methodName));
                return false;
            }
            
            methodInfo.Invoke(null, new object[]{this});
            
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public DateTime RefreshNow() {
            now = DateTime.UtcNow;
            return now;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected bool SetLogFile(string filename) {
            if (filename == logFilename) return false;
            CloseLogFile();
            logFilename = filename;
            if (logFilename != null) OpenLogFile();
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected void OpenLogFile() {
            if (logging || logFilename == null) return;
            logFileExists = File.Exists(logFilename);
            if (bufferedLogging) logFile = new StringWriter();
            else logFile = File.AppendText(logFilename);
            logging = true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected void CloseLogFile() {
            if (!logging) return;
            logging = false;
            if (bufferedLogging) {
                StreamWriter file = File.AppendText(logFilename);
                file.Write(logFile.ToString());
                file.Close();
            }
            logFile.Close();
            if (!logFileExists) ExecuteShellCommand("chmod", "644 " + logFilename);
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected bool SetDebugFile(string filename) {
            if (filename == debugFilename) return false;
            CloseDebugFile();
            debugFilename = filename;
            if (debugFilename != null) OpenDebugFile();
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected void OpenDebugFile() {
            if (debugging || debugFilename == null) return;
            debugFileExists = File.Exists(debugFilename);
            if (bufferedLogging) debugFile = new StringWriter();
            else debugFile = File.AppendText(debugFilename);
            debugging = true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected void CloseDebugFile() {
            if (!debugging) return;
            debugging = false;
            if (bufferedLogging) {
                StreamWriter file = File.AppendText(debugFilename);
                file.Write(debugFile.ToString());
                file.Close();
            }
            debugFile.Close();
            if (!debugFileExists) ExecuteShellCommand("chmod", "644 " + debugFilename);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool GetLogFilenameFromConfig(string name, DateTime date) {
            logFilename = GetQueryStringValue("SELECT value FROM config WHERE name=" + StringUtils.EscapeSql(name) + ";");
            return SetLogFile(logFilename == null ? null : logFilename.Replace("$(DATE)", date.ToString("yyyyMMdd")));
            //return false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public int ExecuteShellCommand(string fileName, string arguments) {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public Process GetShellCommandProcess(string fileName, string arguments) {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            process.WaitForExit();
            return process;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Determines whether this instance is database available.
        /// </summary>
        /// <returns><c>true</c> if this instance is database available; otherwise, <c>false</c>.</returns>
        public bool IsDatabaseAvailable() {
            try {
                mainDbConnection.Open();
            } catch (Exception e) {
                if (e.Message.Contains("Unknown database")) {
                    noDatabase = true;
                    return false;
                } else {
                    throw e;
                }
            }
            return true;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the db connection.
        /// </summary>
        /// <returns>The db connection.</returns>
        public IDbConnection GetDbConnection() {
            IDbConnection result;
            if (DynamicDbConnections) {
                result = GetNewDbConnection();
                lock (lockObject) {
                    dbConnections.Add(result);
                }
            } else {
                result = mainDbConnection;
            }
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public IDbConnection GetNewDbConnection() {
            return GetNewDbConnection(mainConnectionString);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public IDbConnection GetNewDbConnection(string connectionString) {
            // distinguish by underlying DB engine
            IDbConnection result = new MySqlConnection(connectionString);
            result.Open();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void CloseDbConnection(IDbConnection dbConnection) {
            if (DynamicDbConnections) {
                lock (lockObject) {
                    dbConnections.Remove(dbConnection);
                }
                dbConnection.Close();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Starts a database transaction.</summary>
        /// <remarks>
        ///     Nested transactions are not supported by all database manangement systems. If this is the case, nested transactions are ignored but taken into account to keep awareness of the nesting level for the correct closing of transactions.
        ///     Throughout a transaction, the same connection must be used. What connection is used, depends on the setting for the dynamic database connections (<see cref="Terradue.Portal.IfyContext.DynamicDbConnections"/>: 
        ///     <list type="bullet">
        ///         <item>If DynamicDbConnections is <c>false</c> (one database connection used as long as the IfyContext instance is open): The main database connection, opened at the beginning by IfyContext.Open (DynamicDbConnections is <c>false</c>) or</item>
        ///         <item>If DynamicDbConnections is <c>true</c> (new short-lived database connections whenever they are needed): A new database connection (if DynamicDbConnections is <c>true</c>) is opened in this method and is used for all database accesses until the transaction end, i.e. a call to the matching Commit or Rollback.</item>
        ///     </list>
        ///     Note: if a transaction is active, IfyContext.DynamicDbTransactions returns always <c>false</c>, even if it has been set to <c>true</c> originally.
        /// </remarks>
        /// \xrefitem uml "UML" "UML Diagram"
        public void StartTransaction() {
            bool exit = (InTransaction && !nestedTransactions);
            if (!exit && DynamicDbConnections) mainDbConnection = GetNewDbConnection();
            TransactionLevel++;
            if (exit) return;
            Execute("START TRANSACTION;");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Commits a database transaction.</summary>
        /// <remarks>
        ///     If nested transactions are not supported, the transaction is only committed if it is the root transaction.
        ///     After committing, the behaviour of dynamic database connections returns to normal.
        /// </remarks>
        /// <seealso cref="Terradue.Portal.IfyContext.StartTransaction"/>
        /// \xrefitem uml "UML" "UML Diagram"
        public void Commit() {
            if (InTransaction && (nestedTransactions || TransactionLevel == 1)) Execute("COMMIT;");
            TransactionLevel--;
            if (InTransaction) return;
            if (DynamicDbConnections && mainDbConnection != null) {
                CloseDbConnection(mainDbConnection);
                mainDbConnection = null;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Discards the changes of a database transaction.</summary>
        /// <remarks>
        ///     If nested transactions are not supported, the root transaction is rolled back, even if the request originates from an innner transaction.
        ///     After rolling back the transaction, the behaviour of dynamic database connections returns to normal.
        /// </remarks>
        /// <seealso cref="Terradue.Portal.IfyContext.StartTransaction"/>
        /// \xrefitem uml "UML" "UML Diagram"
        public void Rollback() {
            while (InTransaction) {
                if (InTransaction && (nestedTransactions || TransactionLevel == 1)) Execute("ROLLBACK;");
                TransactionLevel--;
                if (InTransaction) continue;
                if (DynamicDbConnections && mainDbConnection != null) {
                    CloseDbConnection(mainDbConnection);
                    mainDbConnection = null;
                }
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the value of the specified configuration variable.</summary>
        /// <param name="name">The name of the configuration variable.</param>
        /// <returns>The value of the configuration variable.</returns>
        /// \ingroup core_Context
        /// \xrefitem uml "UML" "UML Diagram"
        public string GetConfigValue(string name) {
            string result = null;
            IDbConnection dbConnection = GetDbConnection();
            IDbCommand dbCommand = dbConnection.CreateCommand(); // always use main database connection
            dbCommand.CommandText = String.Format("SELECT value FROM config WHERE name={0};", StringUtils.EscapeSql(name));
            IDataReader dbReader = dbCommand.ExecuteReader();
            if (dbReader.Read() && dbReader.GetValue(0) != DBNull.Value) result = dbReader.GetValue(0).ToString();
            CloseQueryResult(dbReader, dbConnection);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public int GetConfigIntegerValue(string name) {
            return GetQueryIntegerValue(String.Format("SELECT value FROM config WHERE name={0};", StringUtils.EscapeSql(name)));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool GetConfigBooleanValue(string name) {
            return GetQueryBooleanValue(String.Format("SELECT value FROM config WHERE name={0};", StringUtils.EscapeSql(name)));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void SetConfigValue(string name, string value) {
            Execute(String.Format("UPDATE config SET value={1} WHERE name={0};", StringUtils.EscapeSql(name), StringUtils.EscapeSql(value)));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void SetConfigValue(string name, int value) {
            Execute(String.Format("UPDATE config SET value={1} WHERE name={0};", StringUtils.EscapeSql(name), value));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void SetConfigValue(string name, bool value) {
            Execute(String.Format("UPDATE config SET value={1} WHERE name={0};", StringUtils.EscapeSql(name), value.ToString().ToLower()));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public int GetInsertId(IDbConnection dbConnection) {
            int result = 0;
            IDbCommand dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = "SELECT LAST_INSERT_ID();";
            IDataReader dbReader = dbCommand.ExecuteReader();
            if (dbReader.Read()) result = dbReader.GetInt32(0);
            dbReader.Close();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public IDataReader GetQueryResult(string sql) {
            return GetQueryResult(sql, mainDbConnection);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public IDataReader GetQueryResult(string sql, IDbConnection dbConnection) {
            AddDebug(3, sql);
            if (ConsoleDebug) Console.WriteLine("**** " + sql);
            IDbCommand dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = sql;
            IDataReader dbReader = dbCommand.ExecuteReader();
            return dbReader;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void CloseQueryResult(IDataReader dbReader) {
            dbReader.Close();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void CloseQueryResult(IDataReader dbReader, IDbConnection dbConnection) {
            dbReader.Close();
            if (DynamicDbConnections) dbConnection.Close();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public int Execute(string sql) {
            IDbConnection dbConnection = GetDbConnection();
            int result = Execute(sql, dbConnection);
            CloseDbConnection(dbConnection);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public int Execute(string sql, IDbConnection dbConnection) {
            IDbCommand dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = sql;
            AddDebug(3, sql);
            int result = dbCommand.ExecuteNonQuery();
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetQueryStringValue(string sql) {
            string result = null;
            IDbConnection dbConnection = GetDbConnection();
            IDbCommand dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = sql;
            IDataReader dbReader = dbCommand.ExecuteReader();
            if (dbReader.Read()) result = dbReader.GetString(0);
            CloseQueryResult(dbReader, dbConnection);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool GetQueryBooleanValue(string sql) {
            bool result = false;
            IDbConnection dbConnection = GetDbConnection();
            IDbCommand dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = sql;
            IDataReader dbReader = dbCommand.ExecuteReader();
            if (dbReader.Read()) result = (dbReader.GetValue(0) != DBNull.Value && (dbReader.GetString(0).ToLower() == "true" || dbReader.GetString(0) == "1"));
            CloseQueryResult(dbReader, dbConnection);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public int GetQueryIntegerValue(string sql) {
            int result = 0;
            IDbConnection dbConnection = GetDbConnection();
            IDbCommand dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = sql;
            IDataReader dbReader = dbCommand.ExecuteReader();
            if (dbReader.Read()) result = dbReader.GetInt32(0);
            CloseQueryResult(dbReader, dbConnection);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public double GetQueryDoubleValue(string sql) {
            double result = 0;
            IDbConnection dbConnection = GetDbConnection();
            IDbCommand dbCommand = dbConnection.CreateCommand();
            dbCommand.CommandText = sql;
            IDataReader dbReader = dbCommand.ExecuteReader();
            if (dbReader.Read()) {
                if (dbReader.GetValue(0) == DBNull.Value) result = 0;
                else result = dbReader.GetDouble(0);
            }
            CloseQueryResult(dbReader, dbConnection);
            return result;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetValue(IDataReader reader, int index) {
            if (reader.GetValue(index) == DBNull.Value) return null;
            return reader.GetValue(index).ToString();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public bool GetBooleanValue(IDataReader reader, int index) {
            if (reader.GetValue(index) == DBNull.Value) return false;
            return reader.GetBoolean(index);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public int GetIntegerValue(IDataReader reader, int index) {
            if (reader.GetValue(index) == DBNull.Value) return 0;
            return reader.GetInt32(index);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public long GetLongIntegerValue(IDataReader reader, int index) {
            if (reader.GetValue(index) == DBNull.Value) return 0;
            return reader.GetInt64(index);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public double GetDoubleValue(IDataReader reader, int index) {
            if (reader.GetValue(index) == DBNull.Value) return 0;
            return reader.GetDouble(index);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public DateTime GetDateTimeValue(IDataReader reader, int index) {
            if (reader.GetValue(index) == DBNull.Value) return DateTime.MinValue;
            return reader.GetDateTime(index);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string GetDateTimeValue(IDataReader reader, int index, string format) {
            if (reader.GetValue(index) == DBNull.Value) return null;
            return reader.GetDateTime(index).ToString(format);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns the username of the user with the specified database ID</summary>
        /// <param name="userId">User identifier.</param>
        /// <returns>The username.</returns>
        public string GetUsername(int userId) {
            if (userId == UserId) return Username;
            return GetQueryStringValue(String.Format("SELECT username FROM usr WHERE id={0};", userId));
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Changes the current HTTP session information in order to impersonate another user.</summary>
        /// <param name="userId">the ID of the user to be impersonated</param>
        /// \xrefitem uml "UML" "UML Diagram"
        public void StartImpersonation(int userId) {
            if (UserInformation == null || UserLevel < Terradue.Portal.UserLevel.Administrator) throw new UnauthorizedAccessException("You are not authorized to impersonate other users");

            User user = User.FromId(this, userId);
            UserInformation.Update(user);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Returns to the HTTP session information of the original user account.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public void EndImpersonation() {
            if (UserInformation == null || OriginalUserId == UserId) throw new InvalidOperationException("You are not impersonating another user");

            User user = User.FromId(this, UserInformation.OriginalUserId);
            UserInformation.Update(user);

            // If impersonating user is authenticated through an authentication method different from a session cookie,
            // the current session (using the impersonated user's account) must be closed
            //if (AuthenticateAutomatically()) HttpContext.Current.Session.Abandon();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void SetUserInformation(AuthenticationType authenticationType, User user) {
            if (authenticationType == null && user == null) UserInformation = null;
            else if (UserInformation == null) UserInformation = new UserInformation(authenticationType, user);
            else UserInformation.Update(user);
            SetUserFields();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void SetUserFields() {
            if (UserInformation == null) {
                UserId = 0;
                UserLevel = 0;
                UserCaption = null;
                return;
            }

            UserId = UserInformation.UserId;
            UserLevel = UserInformation.UserLevel;
            UserCaption = UserInformation.UserCaption;
            DebugLevel = UserInformation.UserDebugLevel;
            UserTimeZoneId = UserInformation.UserTimeZone;
            OriginalUserId = UserInformation.OriginalUserId;
            passwordExpired = UserInformation.PasswordExpired;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void GetUserProxyInformation() {
            GetUserProxyInformation(UserId);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void GetUserProxyInformation(int customUserId) {
            //if (proxyUsername != null && proxyPassword != null) return;
            
            if (customUserId == 0) return;
            string sql = "SELECT proxy_username, proxy_password FROM usr WHERE id=" + customUserId + ";" ;
            IDbConnection dbConnection = GetDbConnection();
            IDataReader reader = GetQueryResult(sql, dbConnection);
            if (reader.Read()) {
                proxyUsername = GetValue(reader, 0);
                proxyPassword = GetValue(reader, 1);
                reader.Close();
            } else {
                reader.Close();
                throw new UnauthorizedAccessException("No proxy user information");
            }
            CloseQueryResult(reader, dbConnection);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string GetRemainingProxyTime() {
            //GetUserProxyInformation(UserId);
            return ""; // !!!
            /*try {
                gridEngineWebService = GetGridEngineWebService();
                int seconds = Int32.Parse(gridEngineWebService.timeLeft(GetConfigValue("MyProxyServer"), proxyUsername, proxyPassword));
                
                return (seconds >= 86400 ? seconds / 86400 + "d " : "") +
                        (seconds >= 3600 ? (seconds % 86400) / 3600 + "h " : "") +
                        (seconds >= 60 ? (seconds % 3600) / 60 + "m " : "") +
                        (seconds % 60) + "s";

            } catch (Exception e) {
                WriteWarning("Could not receive remaining proxy lifetime: " + e.Message);
                return "";
            }*/
            
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void AddDebug(int level, string message) {
            if (level > DebugLevel) return;
            WriteDebug(level, message);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void AddInfo(string message) {
            if (LogLevel < 3) return;
            AddMessage(MessageType.Info, message, null);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void AddInfo(string message, string messageClass) {
            if (LogLevel < 3) return;
            AddMessage(MessageType.Info, message, messageClass);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void AddWarning(string message) {
            if (LogLevel < 2) return;
            AddMessage(MessageType.Warning, message, null);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void AddWarning(string message, string messageClass) {
            if (LogLevel < 2) return;
            AddMessage(MessageType.Warning, message, messageClass);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void AddError(string message) {
            Error = true;
            lastErrorMessage = message;
            lastErrorTime = DateTime.UtcNow;
            if (LogLevel < 1) return;
            AddMessage(MessageType.Error, message, null);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void AddError(string message, string messageClass) {
            Error = true;
            lastErrorMessage = message;
            lastErrorTime = DateTime.UtcNow;
            if (LogLevel < 1) return;
            AddMessage(MessageType.Error, message, messageClass);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected virtual void AddMessage(MessageType type, string message, string messageClass) {
            WriteMessage(type, message, messageClass);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void WriteDebug(int level, string message) {
            if (HideMessages || level > DebugLevel) return;
            if (!console && !logging && !debugging) return;
            string messageStr = (sameLine ? Environment.NewLine : String.Empty) + DateTime.UtcNow.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\Z") + " ";
            messageStr += "[DEBUG] " + message;
            if (console) Console.WriteLine(messageStr);
            if (debugging) debugFile.WriteLine(messageStr);
            else if (logging) logFile.WriteLine(messageStr);
            sameLine = false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteInfo(string message) {
            if (HideMessages || LogLevel < 3) return;
            WriteMessage(MessageType.Info, message, null);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteInfo(string message, string messageClass) {
            if (HideMessages || LogLevel < 3) return;
            WriteMessage(MessageType.Info, message, messageClass);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteWarning(string message) {
            if (HideMessages || LogLevel < 2) return;
            WriteMessage(MessageType.Warning, message, null);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteWarning(string message, string messageClass) {
            if (HideMessages || LogLevel < 2) return;
            WriteMessage(MessageType.Warning, message, messageClass);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteError(string message) {
            lastErrorMessage = message;
            lastErrorTime = DateTime.UtcNow;
            if (HideMessages || LogLevel < 1) return;
            WriteMessage(MessageType.Error, message, null);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteError(string message, string messageClass) {
            lastErrorMessage = message;
            lastErrorTime = DateTime.UtcNow;
            if (HideMessages || LogLevel < 1) return;
            WriteMessage(MessageType.Error, message, messageClass);
        }

        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Throw exceptions directly and handle them in the calling code")]
        public void ReturnError(string message) {
            ReturnError(new Exception(message), null);
        }

        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Throw exceptions directly and handle them in the calling code")]
        public void ReturnError(string message, string messageClass) {
            ReturnError(new Exception(message), messageClass);
        }

        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Throw exceptions directly and handle them in the calling code")]
        public virtual void ReturnError(Exception exception) {
            ReturnError(exception, null);
        }

        //---------------------------------------------------------------------------------------------------------------------

        [Obsolete("Throw exceptions directly and handle them in the calling code")]
        public virtual void ReturnError(Exception exception, string messageClass) {
            lastErrorMessage = exception.Message;
            lastErrorTime = DateTime.UtcNow;
            WriteMessage(MessageType.Error, exception.Message, messageClass);
            throw exception;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected virtual void WriteMessage(MessageType type, string message, string messageClass) {
            if (HideMessages) return;
            if (!console && !logging && !debugging) return;
            string messageStr = String.Empty;
            if (sameLine && (type == MessageType.Error || type == MessageType.Warning)) {
                switch (type) {
                    case MessageType.Warning :
                        messageStr = "WARNING" + Environment.NewLine;
                        break;
                    default :
                        messageStr = "ERROR" + Environment.NewLine;
                        break;
                }
                sameLine = false;
            }
            if (!sameLine) {
                messageStr += DateTime.UtcNow.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\Z") + " ";
                switch (type) {
                    case MessageType.Error :
                        messageStr += "[ERROR] ";
                        break;
                    case MessageType.Warning :
                        messageStr += "[WARN]  ";
                        break;
                    default :
                        messageStr += "[INFO]  ";
                        break;
                }
            }
            sameLine = message.EndsWith(@"\");
            if (sameLine) message = message.Substring(0, message.Length - 1);
            messageStr += message + (sameLine ? String.Empty : Environment.NewLine);
            if (console) Console.Write(messageStr);
            if (logging) logFile.Write(messageStr);
            if (debugging) debugFile.WriteLine(messageStr);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void WriteSeparator() {
            if (sameLine) Console.WriteLine();
            sameLine = false;
            if (logLevel <= 0) return;
            if (console) Console.WriteLine("--------------------");
            if (logging) {
                logFile.WriteLine("--------------------");
                logFile.Flush();
                //Process process = Process.Start("chmod 744 " + logFilename);
                //process.WaitForExit();
            }
            if (debugging) {
                debugFile.WriteLine("--------------------");
                debugFile.Flush();
                //Process process = Process.Start("chmod 744 " + debugFilename);
                //process.WaitForExit();
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string FormatUserDateTime(DateTime dt, string format) {
            return TimeZoneInfo.ConvertTimeFromUtc(dt, UserTimeZone).ToString(format) + (userTimeZone == TimeZoneInfo.Utc ? " (UTC)" : String.Empty);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool ConvertUserDateTime(string value, out DateTime dt) {
            if (!DateTime.TryParse(value, out dt)) return false;
            AddDebug(3, "TIME " + dt.ToString(@"yyyy\-MM\-dd HH\:mm\:ss") + " " + dt.Kind);
            //dt = TimeZoneInfo.ConvertTime(dt, UserTimeZone, TimeZoneInfo.Utc);
            try {
                dt = TimeZoneInfo.ConvertTimeToUtc(dt, UserTimeZone);
            } catch (Exception) {}
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public XmlDocument LoadXmlFile(string filename) {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);
            return doc;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Calculates the credits that the specified user has left for task submissions.</summary>
        public double GetAvailableCredits(int userId) {
            return GetQueryIntegerValue("SELECT credits FROM usr WHERE id=" + userId + ";") - GetQueryDoubleValue("SELECT SUM(resources * priority) FROM task AS t WHERE t.id_usr=" + userId + " AND t.status=20;");
        }


        //---------------------------------------------------------------------------------------------------------------------
        
        public void SetAlertInformation(string applicationName, XmlElement alertElem) {
            this.applicationName = applicationName;
            for (int i = 0; i < alertElem.ChildNodes.Count; i++) {
                XmlElement elem = alertElem.ChildNodes[i] as XmlElement;
                if (elem == null) continue;
                switch (elem.Name) {
                    case "smtp" :
                        if (elem.HasAttribute("hostname")) smtpHostname = elem.Attributes["hostname"].Value;
                        if (elem.HasAttribute("username")) smtpUsername = elem.Attributes["username"].Value;
                        if (elem.HasAttribute("password")) smtpPassword = elem.Attributes["password"].Value;
                        break;
                    case "from" :
                        mailSender = elem.InnerXml;
                        break;
                    case "to" :
                        mailReceivers = elem.InnerXml;
                        break;
                    case "cc" :
                        mailCcReceivers = elem.InnerXml;
                        break;
                    case "bcc" :
                        mailBccReceivers = elem.InnerXml;
                        break;
                    case "subject" :
                        mailMessageSubject = elem.InnerXml;
                        break;
                    case "body" :
                        mailMessageBody = elem.InnerXml;
                        break;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public void SetInputStream(Stream stream, bool format) {
            inputContent = StringUtils.GetXmlFromStream(stream, format);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Sends a mail alert to an administrator.</summary>
        /// \xrefitem uml "UML" "UML Diagram"
        public bool SendMailAlert() {
            /*bool result = false;*/
            string subject = (mailMessageSubject == null ? "No subject" : mailMessageSubject.Replace("$(IDENTIFIER)", applicationName));
            string body = "", logLines = ""/*, line*/;

/*            logWriter.Flush();
            log.Seek(0, SeekOrigin.Begin);
            StreamReader logReader = new StreamReader(log);
            try {
                while ((line = logReader.ReadLine()) != null) logLines += line + Environment.NewLine;
            } catch (Exception) { }
            logReader.Close();*/

            if (mailMessageBody != null) {
                body = mailMessageBody;
                body = body.Replace(@"\n", Environment.NewLine);

                string rnd = StringUtils.CreateRandomString(32);
                body = body.Replace("$", "$" + rnd + "(");
                body = body.Replace("$" + rnd + "(IDENTIFIER)", (applicationName == "" ? "[No name]" : applicationName));
                body = body.Replace("$" + rnd + "(ERROR)", (lastErrorMessage == "" ? "[Not available]" : lastErrorMessage));
                body = body.Replace("$" + rnd + "(TIME)", (lastErrorMessage == "" ? "[Not available]" : lastErrorTime.ToString(@"yyyy\-MM\-dd HH\:mm\:ss\Z")));
                //body = body.Replace("$" + rnd + "(CONTEXT)", (contextInformation == "" ? "[Not available]" : contextInformation));
                body = body.Replace("$" + rnd + "(LOG)", (logLines == "" ? "[Not available]" + Environment.NewLine : logLines));
                body = body.Replace("$" + rnd + "(INPUT)", (inputContent == "" ? "[Not available]" : inputContent));
                body = body.Replace(rnd, "");
            } else {
                body = logLines;
            }

            MailMessage message = new MailMessage();
            
            /*message.Fields.Add("http://schemas.microsoft.com/cdo/configuration/smtpauthenticate", "1");
            message.Fields.Add("http://schemas.microsoft.com/cdo/configuration/sendusername", );
            message.Fields.Add("http://schemas.microsoft.com/cdo/configuration/sendpassword", );*/

            SmtpClient client = new SmtpClient(smtpHostname);
            // Add credentials if the SMTP server requires them.
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            
            message.From = new MailAddress(mailSender);
            message.To.Add(mailReceivers);
            message.CC.Add(mailCcReceivers);
            message.Bcc.Add(mailBccReceivers);
            message.Subject = subject;
            message.Body = body;
            
            try {
                client.Send(message);
            } catch (Exception e) {
                if (e.Message.IndexOf("CDO.Message") != -1) throw new Exception("Error mail could not be sent, this is probably caused by an invalid SMTP hostname or wrong SMTP server credentials");
                else throw new Exception("Error mail could not be sent: " + e.Message);
            }
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Starts a session on the Ify web portal.</summary>
        /// <remarks>This method is only needed for external applications that need to maintain a session state.</remarks>
        /// <returns>The session token.</returns>
        public string StartClientSession(string username, string password) {

            // Get a new cookie
            Cookie sessionCookie = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(BaseUrl);
            request.CookieContainer = new CookieContainer();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.Cookies.Count != 0) sessionCookie = response.Cookies[0];
            response.Close();
            
            if (sessionCookie == null) return null;
            //string sessionToken = Regex.Replace(sessionCookie.Name, "^ASPSESSIONID", "") + "/" + sessionCookie.Value;
            string sessionToken = sessionCookie.Name + "=" + sessionCookie.Value;
            
            // Encode POST data
            string dataStr;
            /*if (externalAuthentication) dataStr = "extid=" + portalUsername;
            else */dataStr = "username=" + Uri.EscapeDataString(username) + "&password=" + Uri.EscapeDataString(password);
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] data = encoding.GetBytes(dataStr);
    
            // Create request
            request = (HttpWebRequest)WebRequest.Create(BaseUrl + "/account/login.aspx?_format=xml");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            
            // Add cookie
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(sessionCookie);
            
            // Add POST data
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();
            
            ParseResponseForError(request);
            
            return sessionToken;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Ends a session on the Ify web portal.</summary>
        /// \ingroup core_Context
        /*!
            This method is only needed for external applications that need to maintain a session state.
            /param the session token
        */
        public bool EndClientSession(string sessionToken) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(BaseUrl + "/account/logout.aspx?_format=xml");
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(CreateCookie(sessionToken));
            ParseResponseForError(request);
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        /// <summary>Checks whether a session on the Ify web portal is still active.</summary>
        /*!
            This method is only needed for external applications that need to maintain a session state.
            /param the session token
        */
        public bool IsClientSessionActive(string sessionToken) {
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(BaseUrl + "/account/check.aspx?_format=xml");
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(CreateCookie(sessionToken));
                return ParseResponseForUser(request);
            } catch (Exception e) {
                AddError(e.Message);
                return false;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        //!
        protected void ParseResponseForError(HttpWebRequest request) {
            HttpWebResponse response = null;
            XmlElement errorElem;
            try {
                response = (HttpWebResponse)request.GetResponse();
                XmlDocument doc = new XmlDocument();
                doc.Load(response.GetResponseStream());
                response.Close();
                errorElem = doc.SelectSingleNode("/content/message[@type='error']") as XmlElement;

            } catch (WebException e) {
                if (response != null) response.Close();
                throw new Exception(e.Message);
            }
            if (errorElem != null) throw new Exception(errorElem.InnerXml);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        //!
        protected bool ParseResponseForUser(HttpWebRequest request) {
            HttpWebResponse response = null;
            //XmlElement errorElem;
            try {
                response = (HttpWebResponse)request.GetResponse();
                XmlDocument doc = new XmlDocument();
                doc.Load(response.GetResponseStream());
                response.Close();
                XmlElement userIdElem;
                if ((userIdElem = doc.SelectSingleNode("/content/user/id") as XmlElement) != null) {
                    int userId = 0;
                    if (Int32.TryParse(userIdElem.InnerText, out userId)) {
                        UserId = userId;
                        return true;
                    }
                }

            } catch (WebException e) {
                if (response != null) response.Close();
                throw new Exception(e.Message);
            }
            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        private Cookie CreateCookie(string sessionToken) {
            if (sessionToken == null || sessionToken == String.Empty) sessionToken = "EMPTY=";
            
            string cookieDomain = BaseUrl;
            Match match = Regex.Match(BaseUrl, "^[^:]+://([^:/]+).*$");
            if (match.Success) cookieDomain = match.Groups[1].Value;
            
            match = Regex.Match(sessionToken, @"^([^=]+)=(.*)$");
            
            if (match.Success) return new Cookie(match.Groups[1].Value, match.Groups[2].Value, "/", cookieDomain);
            else return new Cookie("EMPTY", "", "/", cookieDomain);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public class DbConnData {
            private string connectionString;
            private IDbConnection dbConn;
    
            public string ConnectionString {
                get { return connectionString; }
            }
            public IDbConnection DbConn {
                get { return dbConn; }
            }
    
            public DbConnData(string connectionString) {
                this.connectionString = connectionString;
                this.dbConn = new MySqlConnection(connectionString);
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static RuleApplicationType ToRuleApplicationType(int value) {
            switch (value) {
                case 1:
                    return RuleApplicationType.Never;
                case 2:
                    return RuleApplicationType.Always;
                default:
                    return RuleApplicationType.NoRule;
            }
        }
        
    }

    public class GlobalConfigurationReadOnlyException : InvalidOperationException { 
        public GlobalConfigurationReadOnlyException() : base("Global configuration value cannot be changed") {}
    }

    public class AccountStatusType {
        public const int Disabled = 0;
        public const int PendingActivation = 1;
        public const int PasswordReset = 2;
        public const int Deactivated = 3;
        public const int Enabled = 4;
    }
    
    public class AccountFlags {
        public const int NeedsEmailConfirmation = 8;
    }


    public class SubmissionRetryingType {
        public const int Never = 1;
        public const int AskUser = 2;
        public const int Always = 3;
    }
    
    public class PriorityValue {
        public double Value { get; protected set; }
        public string Caption { get; protected set; }
        public PriorityValue(double value, string caption) {
            this.Value = value;
            this.Caption = caption;
        }
    }

}

