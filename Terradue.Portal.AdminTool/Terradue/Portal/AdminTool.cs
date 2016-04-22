using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.Diagnostics;
using Terradue.Util;

namespace Terradue.Portal {

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class AdminTool {
        
        private static string toolVersion;

        public const int DB_MYSQL = 1;
        public const int DB_POSTGRES = 2;

        private const int DB_INSERT = 1;
        private const int DB_UPDATE = 2;
        
        protected string dbMainSchema;
        protected string dbNewsSchema;

        private int type = 1;
        private string mainConnectionString, currentConnectionString;
        private IDbConnection mainDbConn, currentDbConn;
        private List<DbConnData> dbConns = new List<DbConnData>();
        private IDbCommand dbCommand;
        private int totalRowCount = -1;
        private bool newLine = true;
        private string toDoList;
        private int toDoCount;
        private string toDoSpace = "    ";

        protected bool schemaExists;
        protected string currentSchema;

        private CoreModule core;
        private Site site;
        private List<Module> modules;
        private List<ServiceItem> services;
        private int itemId;

        //---------------------------------------------------------------------------------------------------------------------

        public bool Interactive { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public DataDefinitionMode DefinitionMode { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the base directory of the Terradue.Portal installation.</summary>
        /// <remarks>This directory is something like <em>/usr/local/terradue/portal</em>. The directory must contain the folder <em>core/db</em> and the database scripts.</remarks>
        public string InstallationBaseDirectory { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the internal short name of the application for which this tool is to run.</summary>
        public string SiteName { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the directory containing specific files of the web application for which this tool is to run.</summary>
        /// <remarks>This directory is a directory named after the SiteName under the directory <em>sites</em> under the InstallationBaseDirectory. It contains, among others, application-specific database items (directory <em>db</em>) and the web application code (directory <em>root</em> or <em>www</em>).</remarks>
        public string SiteBaseDirectory { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the server name of the database host.</summary>
        public string DbHost { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the port on which the database server is listening.</summary>
        public int DbPort { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the username for the connection to the database server.</summary>
        public string DbUsername { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the password for the connection to the database server.</summary>
        public string DbPassword { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the database schema or database containing the core items.</summary>
        public string DbMainSchema {
            get { return dbMainSchema; }
            set { dbMainSchema = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the name of the database schema or database containing the items that regard shareable content such as news articles.</summary>
        /// <remarks>If there is no news schema, the tables etc. foreseen for it are created in the main schema (<see cref="DbMainSchema"/>).</remarks>
        public string DbNewsSchema {
            get { return dbNewsSchema == null ? dbMainSchema : dbNewsSchema; }
            set { dbNewsSchema = value; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public int CurrentPhase { get; private set; }
    
        //---------------------------------------------------------------------------------------------------------------------

        public int LastFailurePhase { get; private set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool LastFailureInService { get; private set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string LastFailureItemIdentifier { get; private set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string LastFailureItemVersion { get; private set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string LastFailureItemCheckpoint { get; private set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string CheckpointText {
            get {
                if (LastFailurePhase == ProcessPhaseType.None) return null;
                return String.Format(LastFailureItemCheckpoint == null ? "start" : "checkpoint {0}", LastFailureItemCheckpoint);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public SoftwareItem LastFailureItem { get; private set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool AfterFailureCheckpoint { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string CurrentSchema {
            get { return currentSchema; } 
            set { currentSchema = value; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool NewLine {
            get { return newLine; } 
            set { newLine = value; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool Verbose { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string DbResult { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public char NameQuote { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool ProtectedStatement { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public int TotalRowCount {
            get { return totalRowCount; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new instance of <see cref="Terradue.Portal.AdminTool"/> based on the specified connection string and other parameters necessary for running the tool.</summary>
        protected AdminTool() {}

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new instance of <see cref="Terradue.Portal.AdminTool"/> based on the specified connection string and other parameters necessary for running the tool.</summary>
        /// <param name="definitionMode">The database definition mode (create, upgrade or automatic).</param>
        /// <param name="installationBaseDirectory">The Terradue.Portal installation base directory, e.g. "/usr/local/terradue/portal".</param>
        /// <param name="siteName">The internal short name of the application (only required if there are application-specific database items).</param>
        /// <param name="connectionString">The ADO.NET connection string for the database connection.</param>
        public AdminTool(DataDefinitionMode definitionMode, string installationBaseDirectory, string siteName, string connectionString) {
            this.DefinitionMode = definitionMode;
            this.InstallationBaseDirectory = installationBaseDirectory;
            this.SiteName = siteName;
            MatchCollection matches = Regex.Matches(connectionString, "([A-Za-z ]+) *= *([^;]+)");
            foreach (Match match in matches) {
                string value = match.Groups[2].Value;
                switch (match.Groups[1].Value.Trim().ToLower()) {
                    case "server":
                        this.DbHost = value;
                        break;
                    case "port":
                        int dbPort;
                        Int32.TryParse(value, out dbPort);
                        this.DbPort = dbPort;
                        break;
                    case "user id":
                        this.DbUsername = value;
                        break;
                    case "password":
                        this.DbPassword = value;
                        break;
                    case "database":
                        this.DbMainSchema = value;
                        break;
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Creates a new instance of <see cref="Terradue.Portal.AdminTool"/> based on the specified parameters necessary for running the tool.</summary>
        /// <param name="definitionMode">The database definition mode (create, upgrade or automatic).</param>
        /// <param name="installationBaseDirectory">The Terradue.Portal installation base directory, e.g. "/usr/local/terradue/portal".</param>
        /// <param name="siteName">The internal short name of the application (only required if there are application-specific database items).</param>
        /// <param name="dbHost">The server name of the database host.</param>
        /// <param name="dbPort">The port on which the database server is listening.</param>
        /// <param name="dbUsername">The username for the connection to the database server.</param>
        /// <param name="dbPassword">The password for the connection to the database server.</param>
        /// <param name="dbMainSchema">The name of the database schema or database containing (or to contain) core items.</param>
        public AdminTool(DataDefinitionMode definitionMode, string installationBaseDirectory, string siteName, string dbHost, int dbPort, string dbUsername, string dbPassword, string dbMainSchema, string dbNewsSchema) {
            this.DefinitionMode = definitionMode;
            this.InstallationBaseDirectory = installationBaseDirectory;
            this.SiteName = siteName;
            this.DbHost = dbHost;
            this.DbPort = dbPort;
            this.DbUsername = dbUsername;
            this.DbPassword = dbPassword;
            this.DbMainSchema = dbMainSchema;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static void Main(string[] args) {

            toolVersion = FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(typeof(Terradue.Portal.IfyContext)).Location).FileVersion;

            Console.WriteLine("portal-admin-tool (v{0}) - Tool for portal database maintenance - (c) Terradue S.r.l.", toolVersion);
            Console.WriteLine();
    
            AdminTool tool = new AdminTool();
            tool.Interactive = true;
            if (!tool.GetArgs(args)) {
                PrintUsage();
                Environment.ExitCode = 1;
                return;
            }
            
            try {
                tool.Process();
            } catch (Exception e) {
                if (!tool.NewLine) Console.Error.WriteLine("ERROR");
                tool.WriteErrorSeparator();
                Console.Error.WriteLine("ERROR: " + e.Message + " " + e.StackTrace);
                if (tool != null) tool.CloseConnection();
                //Console.Error.WriteLine(String.Format("The database schema version is {0}{1}", tool.SchemaVersion, tool.Checkpoint == null ? String.Empty : " (checkpoint " + tool.Checkpoint + ")"));
                tool.WriteErrorSeparator();
                Environment.ExitCode = 1;
                return;
            }
            
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool GetArgs(string[] args) {
            if (args.Length == 0) return false;
            switch (args[0]) {
                case "create": 
                    DefinitionMode = DataDefinitionMode.Create;
                    break;
                case "upgrade" :
                    DefinitionMode = DataDefinitionMode.Upgrade;
                    break;
                case "auto" :
                    DefinitionMode = DataDefinitionMode.Automatic;
                    break;
                default :
                    return false;
            }

            int argpos = 1;
            while (argpos <= args.Length - 1) {
                switch (args[argpos]) {
                    case "-v" :
                        Verbose = true;
                        break;
                    case "-b":
                        if (argpos == args.Length - 1) return false;
                        InstallationBaseDirectory = args[++argpos];
                        break;
                    case "-s":
                        if (argpos == args.Length - 1) return false;
                        SiteName = args[++argpos];
                        break;
                    case "-r" :
                        if (argpos == args.Length - 1) return false;
                        InstallationBaseDirectory = args[++argpos];
                        break;
                    case "-H" : 
                        if (argpos == args.Length - 1) return false;
                        DbHost = args[++argpos];
                        break;
                    case "-P" : 
                        if (argpos == args.Length - 1) return false;
                        int dbPort;
                        Int32.TryParse(args[++argpos], out dbPort);
                        DbPort = dbPort;
                        break;
                    case "-u" :
                        if (argpos == args.Length - 1) return false;
                        DbUsername = args[++argpos];
                        break;
                    case "-p" : 
                        if (argpos == args.Length - 1) return false;
                        DbPassword = args[++argpos];
                        break;
                    case "-S" : 
                        if (argpos == args.Length - 1) return false;
                        DbMainSchema = args[++argpos];
                        break;
                    case "-Sn" : 
                        if (argpos == args.Length - 1) return false;
                        DbNewsSchema = args[++argpos];
                        break;
                    default: 
                        return false;
                }
                argpos++;
            }

            if (InstallationBaseDirectory == null && SiteBaseDirectory == null) Console.Error.WriteLine("ERROR: Missing argument for -b or -r");
            //else if (!File.Exists(SiteRootDirectory + Path.DirectorySeparatorChar + "web.config") && dbMainSchema == null) Console.Error.WriteLine("ERROR: No web.config found in root directory, specify database schema name g using -S");
            //else if (dbMainSchema == null) Console.Error.WriteLine("ERROR: Specify database schema name using -S");
            else return true;

            return false;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static void PrintUsage() {
            Console.Error.WriteLine(String.Format("Usage:        {0} <mode> <arguments>", Path.GetFileName(Environment.GetCommandLineArgs()[0])));
            Console.Error.WriteLine();
            Console.Error.WriteLine("Modes:");
            Console.Error.WriteLine("    create  : (re-)creates the portal database schemas");
            Console.Error.WriteLine("    upgrade : upgrades the portal database schemas to the latest version");
            Console.Error.WriteLine("    auto    : either creates or upgrades the database schemas (if exist)");
            Console.Error.WriteLine("    create    [-v] (-b [-s] | -r) [-H] [-P] [-u] [-p] -S [-Sn]");
            Console.Error.WriteLine("    upgrade   [-v] (-b [-s] | -r) [-H] [-P] [-u] [-p] -S [-Sn]");
            Console.Error.WriteLine("    auto      [-v] (-b [-s] | -r) [-H] [-P] [-u] [-p] -S [-Sn]");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Arguments:");
            Console.Error.WriteLine("    -v             : verbose mode, displays all queries");
            Console.Error.WriteLine("    -b <dir>       : local path to installation base directory");
            Console.Error.WriteLine("    -s <name>      : name of installed site");
            Console.Error.WriteLine("    -r <dir>       : local path to site root directory");
            Console.Error.WriteLine("    -H <hostname>  : name of database server host (default: localhost)");
            Console.Error.WriteLine("    -P <port>      : port of database server (optional)");
            Console.Error.WriteLine("    -u <name>      : database username (optional); in create mode, database root privileges are required");
            Console.Error.WriteLine("    -p <password>  : database password (optional)");
            Console.Error.WriteLine("    -S <name>      : name of main portal schema");
            Console.Error.WriteLine("    -Sn <name>     : name of news schema (optional)");
            Console.Error.WriteLine();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void Process() {
            AfterFailureCheckpoint = true;

            currentSchema = DbMainSchema;

            GetDirectories();
            string connectionString = GetConnectionString();
            if (connectionString == null) return;

            if (Interactive) Console.WriteLine("Connecting to: " + Regex.Replace(connectionString, "(Password)=[^;]+", "$1=********", RegexOptions.IgnoreCase));
    
            try {
                OpenConnection(connectionString);
                schemaExists = true;
            } catch (Exception e) {
                if (!e.Message.Contains("Unknown database")) throw;
            }

            if (schemaExists) {
                if (DefinitionMode == DataDefinitionMode.Create) {
                    if (Interactive) Console.Write("WARNING: Main portal schema \"{0}\" already exists. Recreating it will delete contained data. Recreate? ", DbMainSchema);
                    if (!GetYes("Answer yes to delete and recreate: ")) {
                        CloseConnection();
                        if (Interactive) Console.WriteLine("Aborted (no change)");
                        return;
                    }
                } else if (DefinitionMode == DataDefinitionMode.Automatic) {
                    DefinitionMode = DataDefinitionMode.Upgrade;
                }
            } else if (DefinitionMode == DataDefinitionMode.Automatic) {
                DefinitionMode = DataDefinitionMode.Create;
            } else if (DefinitionMode != DataDefinitionMode.Create) {
                if (Interactive) Console.Error.WriteLine("Upgrade not possible, schema \"{0}\" does not exist", DbMainSchema);
                return;
            }

            if (DefinitionMode == DataDefinitionMode.Create) CreateSchemas();
            CheckState();

            ProcessScripts();
            if (LastFailurePhase == ProcessPhaseType.InstallAndUpgrade || LastFailurePhase == ProcessPhaseType.Cleanup) {
                CoreModule oldCore = core;
                List<Module> oldModules = new List<Module>(modules);
                List<ServiceItem> oldServices = new List<ServiceItem>(services);
                Site oldSite = site;

                ProcessScripts();

                core.RestoreFrom(oldCore);
                foreach (Module module in modules) {
                    foreach (Module oldModule in oldModules) {
                        if (oldModule.Identifier == module.Identifier) {
                            module.RestoreFrom(oldModule);
                            break;
                        }
                    }
                }
                foreach (ServiceItem service in services) {
                    foreach (ServiceItem oldService in oldServices) {
                        if (oldService.Identifier == service.Identifier) {
                            service.RestoreFrom(oldService);
                            break;
                        }
                    }
                }
                if (site != null) site.RestoreFrom(oldSite);
            }

            WriteSeparator();
    
            if (DefinitionMode == DataDefinitionMode.Create || schemaExists) {
                if (Interactive) {
                    Console.WriteLine("SUMMARY");
                    WriteSeparator();
                    core.WriteChange();
                    foreach (Module module in modules) module.WriteChange();
                    foreach (ServiceItem service in services) service.WriteChange();
                    if (site != null) site.WriteChange();
                    WriteSeparator();
                    Console.WriteLine("Portal database {0} successfully.", DefinitionMode == DataDefinitionMode.Create ? "installed" : "upgraded");
                }
            }
            
            WriteToDoList();
            WriteSeparator();
                
            CloseConnection();
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void ProcessScripts() {

            core = new CoreModule(this, String.Format("{0}{1}core", InstallationBaseDirectory, Path.DirectorySeparatorChar));
            modules = new List<Module>();
            services = new List<ServiceItem>();
            string siteBaseDir = String.Format("{0}{2}sites{2}{1}", InstallationBaseDirectory, SiteName, Path.DirectorySeparatorChar);
            if (SiteName != null && File.Exists(String.Format("{0}{1}db{1}db-create.sql", siteBaseDir, Path.DirectorySeparatorChar))) site = new Site(this, siteBaseDir);

            RememberPreviousModuleVersions();
            DetectModules();

            FindFailureItem();
            AfterFailureCheckpoint = LastFailureItem == null;

            // Phase 1 (db-prepare), in inverse order
            CurrentPhase = ProcessPhaseType.Prepare;
            if (site != null && site.IsInstalled) site.PrepareChange();
            for (int i = modules.Count - 1; i >= 0; i--) if (modules[i].IsInstalled) modules[i].PrepareChange();
            if (core.IsInstalled) core.PrepareChange();

            // Phase 2a (db-create, db-<version>#), only for core
            CurrentPhase = ProcessPhaseType.InstallAndUpgrade;
            if (CurrentPhase >= LastFailurePhase) {
                if (!core.IsInstalled) core.Install();
                core.Upgrade();
            }

            RememberPreviousServiceVersions();
            DetectServices();
            ServiceItem.GetScriptBasedServiceTypeId(this);

            // Phase 2b (db-create, db-<version>#), for modules, services and site
            if (CurrentPhase >= LastFailurePhase) {
                for (int i = 0; i < modules.Count; i++) {
                    if (!modules[i].IsInstalled) modules[i].Install();
                    modules[i].Upgrade();
                }

                for (int i = 0; i < services.Count; i++) {
                    if (!services[i].IsInstalled) services[i].Install();
                    services[i].Upgrade();
                }

                if (site != null) {
                    if (!site.IsInstalled) site.Install();
                    site.Upgrade();
                }
            }

            // Phase 3 (db-<version>c) in inverse order
            CurrentPhase = ProcessPhaseType.Cleanup;
            if (site != null) site.Cleanup();
            for (int i = modules.Count - 1; i >= 0; i--) modules[i].Cleanup();
            core.Cleanup();

            // Phase 4 (db-complete)
            CurrentPhase = ProcessPhaseType.Complete;
            if (CurrentPhase >= LastFailurePhase) {
                if (core.IsInstalled) core.CompleteChange();
                for (int i = 0; i < modules.Count; i++) if (modules[i].IsInstalled) modules[i].CompleteChange();
                if (site != null && site.IsInstalled) site.CompleteChange();
            }

            ForgetPreviousModuleVersions();
            ForgetPreviousServiceVersions();
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void GetDirectories() {
            if (InstallationBaseDirectory == null) {
                throw new Exception("No directory information specified");
            }
            if (InstallationBaseDirectory != null) {
                if (SiteName != null) {
                    SiteBaseDirectory = String.Format("{0}{2}sites{2}{1}", InstallationBaseDirectory, SiteName, Path.DirectorySeparatorChar);
                }

            } else {
                DirectoryInfo dirInfo = new DirectoryInfo(SiteBaseDirectory);
                if (dirInfo.Exists) SiteBaseDirectory = dirInfo.FullName;
                Match match = Regex.Match(SiteBaseDirectory, @"^((.+)[\\/]+sites[\\/]+([^\\/]+))([\\/]+(root|www))?[\\/]*$");
                if (match.Success) {
                    SiteBaseDirectory = match.Groups[1].Value; // "/root" must be removed from site base directory if present
                    InstallationBaseDirectory = match.Groups[2].Value;
                    SiteName = match.Groups[3].Value;
                } else {
                    throw new Exception("Invalid site root directory");
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        private string GetConnectionString() {
            string connectionString = null;

            // Get connection string (from web.config or build it from command line argument)
            if (DbMainSchema == null) {

                string configFile = String.Format("{0}{1}root{1}web.config", SiteBaseDirectory, Path.DirectorySeparatorChar);
                if (!File.Exists(configFile)) {
                    if (Interactive) {
                        Console.Error.WriteLine(
                                String.Format("ERROR: The configuration file {0} does not exist.",
                                        configFile,
                                        File.Exists(configFile + ".tmpl") ? " You may create it using the .tmpl file." : String.Empty
                                )
                        );
                    }
                    return null;
                }
                XmlDocument configDoc = new XmlDocument();
                configDoc.Load(configFile);

                XmlElement key = configDoc.SelectSingleNode("/configuration/appSettings/add[@key='DatabaseConnection']") as XmlElement;
                if (key == null || !key.HasAttribute("value") || (connectionString = key.Attributes["value"].Value) == String.Empty) {
                    if (Interactive) Console.Error.WriteLine(String.Format("ERROR: No connection string found in web.config.{0}       Make sure the key \"DatabaseConnection\" exists under <appSettings>", Environment.NewLine));
                    return null;
                }

            } else {
                connectionString = String.Format("Server={0}{1}{2}{3}; Database={4}; Allow User Variables=True",
                    DbHost,
                    DbPort == 0 ? "" : "; Port=" + DbPort,
                    DbUsername == null ? "" : "; User Id=" + DbUsername,
                    DbPassword == null ? "" : "; Password=" + DbPassword,
                    DbMainSchema
                );
            }

            return connectionString;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void CheckState() {
            bool moduleOk = false;

            // Create "module" table if it does not exist at all
            try {
                GetQueryIntegerValue("SELECT id FROM $MAIN$.module LIMIT 0;");
            } catch (Exception) {
                Execute(@"CREATE TABLE $MAIN$.module (
    id int unsigned NOT NULL auto_increment,
    name varchar(50) NOT NULL COMMENT 'Unique identifier',
    version varchar(10) COMMENT 'Installed version',
    c_version varchar(10) COMMENT 'Cleanup version (in case of interrupted upgrade)',
    CONSTRAINT pk_module PRIMARY KEY (id),
    UNIQUE INDEX (name)
) Engine=InnoDB COMMENT 'Installed modules';"
                );
                moduleOk = true;
            }

            // Create "install" table if it does not exist at all
            int count = 0;
            try {
                count = GetQueryIntegerValue("SELECT COUNT(*) FROM $MAIN$.install;");
            } catch (Exception) {
                Execute(@"CREATE TABLE $MAIN$.install (
    phase tinyint,
    is_service boolean,
    name varchar(50),
    version varchar(10),
    checkpoint varchar(20)
) Engine=InnoDB COMMENT 'Internal installation information';"
                );
            }

            // Make sure there exactly only one record in "install" table
            if (count != 1) {
                Execute("DELETE FROM $MAIN$.install;");
                Execute("INSERT INTO $MAIN$.install () VALUES ();");
            }

            try {
                if (!moduleOk) {
                    GetQueryStringValue("SELECT version_checkpoint FROM $MAIN$.module LIMIT 0;");
                    Execute("ALTER TABLE $MAIN$.module ADD COLUMN c_version varchar(10) COMMENT 'Cleanup version (in case of interrupted upgrade)', DROP COLUMN version_checkpoint;");
                    moduleOk = true;
                }
            } catch (Exception) {}

            try {
                if (!moduleOk) {
                    GetQueryStringValue("SELECT checkpoint FROM $MAIN$.module LIMIT 0;");
                    Execute("ALTER TABLE $MAIN$.module DROP COLUMN checkpoint, CHANGE COLUMN c_version c_version varchar(10) COMMENT 'Cleanup version (in case of interrupted upgrade)', DROP COLUMN c_checkpoint;");
                    moduleOk = true;
                }
            } catch (Exception) {}

            GetLastFailure();

            try {
                if (DefinitionMode != DataDefinitionMode.Create && (LastFailurePhase > ProcessPhaseType.InstallAndUpgrade || LastFailureInService || LastFailureItemIdentifier != "core")) GetQueryStringValue("SELECT c_version FROM $MAIN$.service LIMIT 0;");
            } catch (Exception) {
                Execute("ALTER TABLE $MAIN$.service ADD COLUMN c_version varchar(10) COMMENT 'Cleanup version (in case of interrupted upgrade)' AFTER version;");
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void FindFailureItem() {
            GetLastFailure();

            if (LastFailurePhase == ProcessPhaseType.None) {
                LastFailureItem = null;
                return;
            }

            if (LastFailureInService) {
                foreach (ServiceItem service in services) {
                    if (service.Identifier == LastFailureItemIdentifier) {
                        LastFailureItem = service;
                        break;
                    }
                }
            } else if (LastFailureItemIdentifier == core.Identifier) {
                LastFailureItem = core;
            } else if (LastFailureItemIdentifier == site.Identifier) {
                LastFailureItem = site;
            } else {
                foreach (Module module in modules) {
                    if (module.Identifier == LastFailureItemIdentifier) {
                        LastFailureItem = module;
                        break;
                    }
                }
            }
            if (LastFailureItem == null) LastFailurePhase = ProcessPhaseType.None;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        private void GetLastFailure() {
            LastFailurePhase = ProcessPhaseType.None;
            IDataReader reader = GetQueryResult("SELECT phase, is_service, name, version, checkpoint FROM $MAIN$.install;");
            if (reader.Read() && reader.GetValue(0) != DBNull.Value && reader.GetInt32(0) != ProcessPhaseType.None) {
                LastFailurePhase = reader.GetInt32(0);
                LastFailureInService = reader.GetValue(1) != DBNull.Value && reader.GetBoolean(1);
                LastFailureItemIdentifier = reader.GetValue(2) == DBNull.Value ? null : reader.GetString(2);
                LastFailureItemVersion = reader.GetValue(3) == DBNull.Value ? null : reader.GetString(3);
                LastFailureItemCheckpoint = reader.GetValue(4) == DBNull.Value ? null : reader.GetString(4);
            }
            reader.Close();
        }

        //---------------------------------------------------------------------------------------------------------------------

        private bool GetYes(string hint) {
            if (Interactive) {
                string answer = Console.ReadLine().ToLower();
                if (answer == "no" || answer == "n") return false;
                if (answer != "yes") {
                    Console.Write(hint);
                    answer = Console.ReadLine().ToLower();
                    return (answer == "yes"); 
                }
            }
            return true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        protected void CreateSchemas() {
        // Create main portal database schema (drop first if exists)
            if (schemaExists) Execute("DROP DATABASE $MAIN$;");
            
            if (!schemaExists) ReopenConnection();
            
            /*AddToDoTask(
                    String.Format("Rename the file \"{0}{1}web.config.TO-BE-CHANGED\"\\nto \"{0}{1}web.config\" and open it.\\nSet the value of the element <DatabaseConnection> to the ADO.NET connection string for the portal database access from ASP.NET/Mono web pages.", 
                            SiteRootDirectory,
                            Path.DirectorySeparatorChar
                    )
            );*/

            if (Interactive) Console.WriteLine("Creating main portal schema \"{0}\"", DbMainSchema);
            Execute("CREATE DATABASE $MAIN$ DEFAULT CHARACTER SET utf8 DEFAULT COLLATE utf8_general_ci;");
            schemaExists = true;
            
            if (DbNewsSchema != DbMainSchema) {
                if (Interactive) Console.WriteLine("Creating news schema \"{0}\"", DbNewsSchema);
                try {
                    Execute("USE $NEWS$;");
                    Execute("DROP DATABASE $NEWS$;");
                } catch (Exception) {}
                Execute("CREATE DATABASE $NEWS$ DEFAULT CHARACTER SET utf8 DEFAULT COLLATE utf8_general_ci;");
            }

            WriteSeparator();
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void RememberPreviousModuleVersions() {
            Execute("UPDATE $MAIN$.module SET c_version=version;");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        private void RememberPreviousServiceVersions() {
            Execute("UPDATE $MAIN$.service SET c_version=version;");
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void ForgetPreviousModuleVersions() {
            Execute("UPDATE $MAIN$.module SET c_version=NULL;");
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void ForgetPreviousServiceVersions() {
            Execute("UPDATE $MAIN$.service SET c_version=NULL;");
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void DetectModules() {
            string modulesBaseDir = String.Format("{0}{1}modules", InstallationBaseDirectory, Path.DirectorySeparatorChar);
            if (!Directory.Exists(modulesBaseDir)) return;
                
            string[] moduleDirs = Directory.GetDirectories(modulesBaseDir, "*");
            foreach (string moduleDir in moduleDirs) {
                if (moduleDir.StartsWith(".")) continue;
                
                Match match = Regex.Match(moduleDir, @"([^\\/]+)$");
                if (!match.Success) continue;
                
                string moduleIdentifier = match.Groups[1].Value;
                string dbDir = String.Format("{0}{1}db", moduleDir, Path.DirectorySeparatorChar);
                if (!Directory.Exists(dbDir) || Directory.GetFiles(dbDir, "db-create.sql").Length == 0 /*|| !Directory.Exists(String.Format("{0}{2}{1}", SiteRootDirectory, moduleIdentifier, Path.DirectorySeparatorChar))*/) continue;
                modules.Add(new Module(this, moduleIdentifier, moduleDir));
            }
            
            // Reorder the modules according to their dependencies
            foreach (Module module in modules) {
                string dependencyFile = String.Format("{0}{1}dependency", module.BaseDir, Path.DirectorySeparatorChar);
                if (File.Exists(dependencyFile)) {
                    StreamReader sr = new StreamReader(dependencyFile);
                    string moduleIdentifier;
                    while ((moduleIdentifier = sr.ReadLine()) != null) {
                        foreach (Module module2 in modules) {
                            if (module2.Identifier == moduleIdentifier) {
                                module.Dependencies.Add(module2);
                                break;
                            }
                        }
                    }
                    sr.Close();
                }
            }
            
            modules.Sort(new ModuleComparer<Module>());
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        private void DetectServices() {
            string servicesBaseDir = String.Format("{0}{1}services", InstallationBaseDirectory, Path.DirectorySeparatorChar);
            if (!Directory.Exists(servicesBaseDir)) return;

            string[] serviceDirs = Directory.GetDirectories(servicesBaseDir, "*");

            foreach (string serviceDir in serviceDirs) {
                if (serviceDir.StartsWith(".")) continue;
                Match match = Regex.Match(serviceDir, @"([^\\/]+)$");
                if (!match.Success) continue;
                string serviceIdentifier = match.Groups[1].Value;
                string wwwDir = String.Format("{0}{1}www", serviceDir, Path.DirectorySeparatorChar);
                if (!Directory.Exists(wwwDir) || !File.Exists(String.Format("{0}{1}index.aspx", wwwDir, Path.DirectorySeparatorChar)) || !Directory.Exists(String.Format("{0}{2}root{2}services{2}{1}", SiteBaseDirectory, serviceIdentifier, Path.DirectorySeparatorChar))) continue;
                services.Add(new ServiceItem(this, serviceIdentifier, serviceDir));
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ExecuteSpecificAction(string identifier) {
            //switch (identifier) {
            //}
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void SetItemId(int value) {
            this.itemId = value;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void ExecuteSqlScript(string filename, SoftwareItem item) {
            SetItemId(item.Id);
            if (AfterFailureCheckpoint) SetCheckpoint(item, null);
            else if (CurrentPhase == LastFailurePhase && item == LastFailureItem && LastFailureItemCheckpoint == null) AfterFailureCheckpoint = true;
            ResetResult();
            ChangeSchema(DbMainSchema);
            StreamReader fileReader = new StreamReader(filename);
            string line, command = String.Empty;
            bool routine = false;
            while ((line = fileReader.ReadLine()) != null) {
                string tline = line.Trim();
                if (command == String.Empty && Regex.Match(tline, "^CREATE (FUNCTION|PROCEDURE|TRIGGER) ").Success) routine = true;

                if (tline.StartsWith("--") && command == String.Empty && CurrentSchema != null) {
                    ProcessComment(tline.Substring(2).Trim(), item);
                    continue;
                } else if (tline.StartsWith("DELIMITER ")) {
                    if (AfterFailureCheckpoint) Execute(tline);
                    command = String.Empty;
                    routine = false;
                    continue;
                }

                if (tline != String.Empty && !Regex.Match(tline, @"^/\*\**\*/$").Success) command += (command == String.Empty ? String.Empty : "\n") + tline;

                if (tline.EndsWith(";") && (!routine || tline.StartsWith("END;"))) {
                    Match match = Regex.Match(command, "^USE +([^ ]+);$");
                    bool execute = AfterFailureCheckpoint;
                    if (match.Success) {
                        ChangeSchema(match.Groups[1].Value);
                        execute = false;
                    }
                    if (execute && CurrentSchema != null) Execute(command);

                    command = String.Empty;
                    routine = false;
                }
            }
            fileReader.Close();
            ChangeSchema(DbMainSchema);
            if (AfterFailureCheckpoint) ClearCheckpoint();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ChangeSchema(string schema) {
            switch (schema) {
                case "$MAIN$":
                    schema = DbMainSchema;
                    break;
                case "$NEWS$":
                    schema = DbNewsSchema;
                    break;
            }
            CurrentSchema = schema;
            Execute(String.Format("USE {0};", schema));
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ProcessComment(string comment, SoftwareItem item) {
            bool write = false;

            if (comment.StartsWith("VERSION ")) {
                item.NewVersion = comment.Substring(8);

            } else if (comment.StartsWith("CHECKPOINT")) {
                string newCheckpoint = comment.Substring(11);
                if (AfterFailureCheckpoint) {
                    SetCheckpoint(item, newCheckpoint);
                } else if (CurrentPhase == LastFailurePhase && item == LastFailureItem && newCheckpoint == LastFailureItemCheckpoint) {
                    AfterFailureCheckpoint = true;
                }
            } else if (AfterFailureCheckpoint) {
                if (comment == "TRY START") ProtectedStatement = true;
                else if (comment == "TRY END") ProtectedStatement = false;
                else if (comment == "NORESULT") ResetResult();
                else if (comment == "RESULT") WriteResult();
                else if (comment.StartsWith("TODO ")) AddToDoTask(comment.Substring(5));
                else if (comment == "SEPARATOR") WriteSeparator();
                else write = true;
            }

            if (write) {
                Match match = Regex.Match(comment, @"^EXECUTE\((.+)\)$");
                if (match.Success) {
                    ExecuteSpecificAction(match.Groups[1].Value);
                } else if (Interactive) {
                    NewLine = !comment.EndsWith(@"\");
                    if (!NewLine) comment = Regex.Replace(comment, @"\\$", "");
                    if (Verbose) comment = "-- " + comment;
                    Console.Write(comment);
                    if (NewLine || Verbose) Console.WriteLine();
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void SetCheckpoint(SoftwareItem item, string checkpoint) {
            Execute(String.Format("UPDATE $MAIN$.install SET phase={0}, is_service={1}, name={2}, version={3}, checkpoint={4};",
                CurrentPhase,
                (item is ServiceItem).ToString().ToLower(),
                StringUtils.EscapeSql(item.Identifier),
                StringUtils.EscapeSql(item.Version),
                StringUtils.EscapeSql(checkpoint)
            ));
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void ClearCheckpoint() {
            Execute("UPDATE $MAIN$.install SET phase=NULL, is_service=NULL, name=NULL, version=NULL, checkpoint=NULL;");
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void WriteSeparator() {
            if (Interactive) Console.WriteLine("----------------------------------------------------------------------------------------------------");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void WriteErrorSeparator() {
            if (Interactive) Console.Error.WriteLine("----------------------------------------------------------------------------------------------------");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void AddToDoTask(string text) {
            text = text.Replace(@"\n", Environment.NewLine + toDoSpace);
            if (toDoCount == 0) toDoList = String.Empty; else toDoList += Environment.NewLine + Environment.NewLine; 
            toDoList += String.Format("({0}) {1}", ++toDoCount, text);
            toDoSpace = Regex.Replace(toDoCount.ToString(), ".", " ") + "   ";
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        private void WriteToDoList() {
            if (!Interactive || toDoCount == 0) return;
            WriteSeparator();
            Console.WriteLine("TO DO: Follow the instruction{0} below before using the web portal and its services", toDoCount == 1 ? "" : "s");
            Console.WriteLine();
            Console.WriteLine(toDoList);
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        public string DoSubstitutions(string sql) {
            sql = sql.Trim();
            
            MatchCollection matches = Regex.Matches(sql, @"\$[A-Z]+\$");
            for (int i = 0; i < matches.Count; i++) {
                string value = matches[i].Value;
                switch (value) {
                    case "$MAIN$" :
                        if (DbMainSchema == null) return null;
                        sql = sql.Replace(value, NameQuote + DbMainSchema + NameQuote);
                        break;
                    case "$NEWS$" :
                        if (DbNewsSchema == null) return null;
                        sql = sql.Replace(value, NameQuote + DbNewsSchema + NameQuote);
                        break;
                    case "$DBHOSTNAME$" :
                        sql = sql.Replace(value, DbHost);
                        break;
                    case "$DBPORT$" :
                        sql = sql.Replace(value, DbPort.ToString());
                        break;
                    case "$DBUSERNAME$" :
                        sql = sql.Replace(value, DbUsername);
                        break;
                    case "$DBPASSWORD$" :
                        sql = sql.Replace(value, DbPassword);
                        break;
                    case "$SERVICEBASE$" :
                        sql = sql.Replace(value, String.Format("{0}/root/services", SiteBaseDirectory.Replace(@"\", "/")));
                        break;
                    case "$ID$" :
                        sql = sql.Replace(value, itemId.ToString());
                        break;
                }
            }
            return sql;
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        public void WriteResult() {
            if (DbResult == null) DbResult = String.Format("OK{0}", totalRowCount == -1 ? "" : " (" + totalRowCount + " record" + (totalRowCount == 1 ? "" : "s") + ")");
            if (Verbose) DbResult = "-- " + DbResult;
            if (Interactive) Console.WriteLine(DbResult);
            ResetResult();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void ResetResult() {
            DbResult = null;
            totalRowCount = -1;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void OpenConnection(string connectionString) {
            mainConnectionString = connectionString;
            try {
                if (type == 1) mainDbConn = new MySqlConnection(mainConnectionString);
                /*else mainDbConn = new NpgsqlConnection(mainConnectionString);*/
            } catch (Exception) {
                throw new Exception("Could not connect to database");
            }
            if (type == 1) NameQuote = '`';
            currentConnectionString = mainConnectionString;
            currentDbConn = mainDbConn;
            mainDbConn.Open();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void ReopenConnection() {
            if (mainDbConn != null) {
                mainDbConn.Close();
                string newConnectionString = Regex.Replace(mainConnectionString, "Database=[^ ;]+;?", "Database=information_schema;");
                if (type == 1) mainDbConn = new MySqlConnection(newConnectionString);
                /*else mainDbConn = new NpgsqlConnection(newConnectionString);*/
                mainDbConn.Open();
                currentDbConn = mainDbConn;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void CloseConnection() {
            while (dbConns.Count != 0) CloseLastDbConnection();
            if (mainDbConn != null) mainDbConn.Close();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void OpenNewDbConnection() {
            dbConns.Add(new DbConnData(currentConnectionString));
            currentDbConn = dbConns[dbConns.Count - 1].DbConn;
            currentDbConn.Open();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void CloseLastDbConnection() {
            if (dbConns.Count != 0) dbConns[dbConns.Count - 1].DbConn.Close();
            dbConns.RemoveAt(dbConns.Count - 1);
            if (dbConns.Count == 0) {
                currentDbConn = mainDbConn;
                currentConnectionString = mainConnectionString;
            } else {
                currentDbConn = dbConns[dbConns.Count - 1].DbConn;
                currentConnectionString = dbConns[dbConns.Count - 1].ConnectionString;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void Execute(string sql) {
            string name = null;
            bool message = false;
            int changeData = 0;
            if ((sql = DoSubstitutions(sql)) == null) return;
            Match match = Regex.Match(sql, "^(CREATE TABLE|CREATE FUNCTION|CREATE PROCEDURE|ALTER TABLE|INSERT INTO|UPDATE|CALL) +([^ \\(]*)");
            if (match.Success) {
                name = match.Groups[2].Value;
                Match match2;
                switch (match.Groups[1].Value) {
                    case "CREATE TABLE" :
                        match2 = Regex.Match(sql, "COMMENT (('([^']+)')+);");
                        message = (!Verbose && match2.Success);
                        if (Interactive && message) Console.Write("Creating table \"{0}\" ({1}) ... ", name, StringUtils.UnescapeSql(match2.Groups[1].Value));
                        newLine = false;
                        break;
                    case "CREATE FUNCTION" :
                        match2 = Regex.Match(sql, "COMMENT (('([^']+)')+)");
                        message = (!Verbose && match2.Success);
                        if (Interactive && message) Console.Write("Creating stored function \"{0}\" ({1}) ... ", name, StringUtils.UnescapeSql(match2.Groups[1].Value));
                        newLine = false;
                        break;
                    case "CREATE PROCEDURE" :
                        match2 = Regex.Match(sql, "COMMENT (('([^']+)')+)");
                        message = (!Verbose && match2.Success);
                        if (Interactive && message) Console.Write("Creating stored procedure \"{0}\" ({1}) ... ", name, StringUtils.UnescapeSql(match2.Groups[1].Value));
                        newLine = false;
                        break;
                    case "ALTER TABLE" :
                        match2 = Regex.Match(sql, "COMMENT '(.+)';$");
                        break;
                    case "INSERT INTO" :
                        changeData = DB_INSERT;
                        break;
                    case "UPDATE" :
                        changeData = DB_UPDATE;
                        break;
                    case "CALL" :
                        changeData = DB_INSERT;
                        break;
                }
                if (Interactive && Verbose) Console.WriteLine(sql);
            }
    
            dbCommand = currentDbConn.CreateCommand();
            dbCommand.CommandText = sql;
            int rowCount = 0;
            if (ProtectedStatement) {
                try {
                    rowCount = dbCommand.ExecuteNonQuery();
                } catch (Exception) {}
            } else {
                rowCount = dbCommand.ExecuteNonQuery();
            }
            if (Interactive && Verbose) Console.WriteLine("-- rows: " + rowCount); 
    
            if (changeData != 0 && rowCount != -1) totalRowCount = (totalRowCount == -1 ? 0 : totalRowCount) + rowCount;
    
            if (Interactive && message) Console.WriteLine("OK");
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        public IDataReader GetQueryResult(string sql) {
            if ((sql = DoSubstitutions(sql)) == null) return null;
            dbCommand = currentDbConn.CreateCommand();
            dbCommand.CommandText = sql;
            IDataReader dbReader = dbCommand.ExecuteReader();
            return dbReader;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string GetQueryStringValue(string sql) {
            string result = null;
            if ((sql = DoSubstitutions(sql)) == null) return null;
            dbCommand =  currentDbConn.CreateCommand();
            dbCommand.CommandText = sql;
            IDataReader dbReader = dbCommand.ExecuteReader(); 
            if (dbReader.Read()) result = (dbReader.GetValue(0) == DBNull.Value ? null : dbReader.GetString(0));
            dbReader.Close();
            return result;
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        public int GetQueryIntegerValue(string sql) {
            int result = 0;
            if ((sql = DoSubstitutions(sql)) == null) return 0;
            dbCommand =  currentDbConn.CreateCommand();
            dbCommand.CommandText = sql;
            IDataReader dbReader = dbCommand.ExecuteReader(); 
            if (dbReader.Read()) result = (dbReader.GetValue(0) == DBNull.Value ? 0 : dbReader.GetInt32(0));
            dbReader.Close();
            return result;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public double GetQueryDoubleValue(string sql) {
            double result = 0;
            if ((sql = DoSubstitutions(sql)) == null) return 0;
            dbCommand =  currentDbConn.CreateCommand();
            dbCommand.CommandText = sql;
            IDataReader dbReader = dbCommand.ExecuteReader(); 
            if (dbReader.Read()) result = (dbReader.GetValue(0) == DBNull.Value ? 0 : dbReader.GetDouble(0));
            dbReader.Close();
            return result;
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        public string GetValue(IDataReader reader, int index) {
            if (reader.GetValue(index) == DBNull.Value) return null;
            return reader.GetValue(index).ToString();
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        public bool GetBoolValue(IDataReader reader, int index) {
            if (reader.GetValue(index) == DBNull.Value) return false;
            return reader.GetBoolean(index);
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        public int GetIntegerValue(IDataReader reader, int index) {
            if (reader.GetValue(index) == DBNull.Value) return 0;
            return reader.GetInt32(index);
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
    
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class DbConnData {
        private string connectionString;
        private IDbConnection dbConn;
    
        //---------------------------------------------------------------------------------------------------------------------

        public string ConnectionString {
            get { return connectionString; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public IDbConnection DbConn {
            get { return dbConn; }
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        public DbConnData(string connectionString) {
            this.connectionString = connectionString;
            this.dbConn = new MySqlConnection(connectionString);
        }
    }

    

    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ProcessPhaseType {
        public static readonly int None = 0;
        public static readonly int Prepare = 1;
        public static readonly int InstallAndUpgrade = 2;
        public static readonly int Cleanup = 3;
        public static readonly int Complete = 4;
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------

    

    public class ModuleComparer<Module> : IComparer<Module> {
        int IComparer<Module>.Compare(Module x, Module y) {
            // IComparer<T> seems to have a problem, therefore the cast
            Terradue.Portal.Module xm = x as Terradue.Portal.Module;
            Terradue.Portal.Module ym = y as Terradue.Portal.Module;
            if (xm.Distance == ym.Distance) return String.Compare(xm.Identifier, ym.Identifier);
            return xm.Distance - ym.Distance;
        }
    }


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class FileNameComparer : IComparer<string> {
        private static readonly Regex regex = new Regex(@"^(db-)?([\d\.]+)([#c]\.sql)?$");

        int IComparer<string>.Compare(string x, string y) {
            return Compare(x, y);
        }

        public static int Compare(string x, string y) {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            Match match;
            match = regex.Match(x);
            if (!match.Success) return 0;
            x = match.Groups[2].Value;
            match = regex.Match(y);
            if (!match.Success) return 0;
            y = match.Groups[2].Value;

            string[] xa = x.Split('.');
            string[] ya = y.Split('.');
            int[] xai = new int[xa.Length];
            int[] yai = new int[ya.Length];
            for (int i = 0; i < xai.Length; i++) Int32.TryParse(xa[i], out xai[i]);
            for (int i = 0; i < yai.Length; i++) Int32.TryParse(ya[i], out yai[i]);

            //for (int i = 0; i < (xa.Length > ya.Length ? xa.Length : ya.Length); i++) Console.WriteLine("    {0} : {1}", i < xa.Length ? xa[i] : "-", i < ya.Length ? ya[i] : "-");
            //for (int i = 0; i < (xa.Length > ya.Length ? xa.Length : ya.Length); i++) Console.WriteLine("  I {0} : {1}", i < xa.Length ? xai[i].ToString() : "-", i < ya.Length ? yai[i].ToString() : "-");

            int index = 0;
            while (index < xai.Length && index < yai.Length) {
                if (xai[index] != yai[index]) return xai[index] - yai[index];
                index++;
            }
            return (xai.Length - yai.Length);
        }

    }


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    /// <summary>Enumaration for the data definition modes of the AdminTool.</summary>
    public enum DataDefinitionMode {

        /// <summary>Create mode (new database is created in the latest version).</summary>
        Create,

        /// <summary>Upgrade mode (existing database is upgraded to a new version).</summary>
        Upgrade,

        /// <summary>Automatic mode determiniation (create if database does not exist yet, upgrade otherwise).</summary>
        Automatic
    }

}

