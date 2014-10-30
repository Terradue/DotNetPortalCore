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
        private const int DB_INSERT = 1;
        private const int DB_UPDATE = 2;
        
        protected static bool create;
        private static bool auto;
        private static bool verbose;
        // private static bool didYouKnow;
        private static bool newLine = true;
        protected static string siteRootDir, siteBaseDir, installationBaseDir;
        private static string dbHostname = "localhost";
        private static int dbPort;
        private static string dbUsername;
        private static string dbPassword;
        protected static string dbMainSchema;
        private static string dbNewsSchema;
        private static string dbOldSchema;
        private static string dbOldNewsSchema;
        private static string oldSiteRootDir;
        private static string catalogueBaseUrl = "$(CATALOGUE)/";
        
        protected bool schemaExists;
        protected string currentSchema;

        private Module core, site;
        private List<Module> modules;
        private List<ServiceItem> services;
        private int itemId;

        private string toDoList;
        private int toDoCount;
        private string toDoSpace = "    ";

        private IDataReader dbReader;
    
        //---------------------------------------------------------------------------------------------------------------------

        public string CurrentSchema {
            get { return currentSchema; } 
            set { currentSchema = GetSchemaName(value); }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool NewLine {
            get { return newLine; } 
            set { newLine = value; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public bool Verbose { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public static void Main(string[] args) {

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            toolVersion = fvi.FileVersion;

            Console.WriteLine("portal-admin-tool (v{0}) - Tool for portal database maintenance - (c) Terradue S.r.l.", toolVersion);
            Console.WriteLine();
    
            if (!GetArgs(args)) {
                PrintUsage();
                Environment.ExitCode = 1;
                return;
            }
            
            AdminTool tool = null;
            try {
                tool = new AdminTool();
                tool.Process();
            } catch (Exception e) {
                if (!newLine) Console.Error.WriteLine("ERROR");
                WriteErrorSeparator();
                Console.Error.WriteLine("ERROR: " + e.Message + " " + e.StackTrace);
                if (tool != null) tool.CloseConnection();
                //Console.Error.WriteLine(String.Format("The database schema version is {0}{1}", tool.SchemaVersion, tool.Checkpoint == null ? String.Empty : " (checkpoint " + tool.Checkpoint + ")"));
                WriteErrorSeparator();
                Environment.ExitCode = 1;
                return;
            }
            
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void Process() {
            currentSchema = dbMainSchema;
            Verbose = verbose;
            
            DirectoryInfo dirInfo;
            if (siteRootDir != null) {
                dirInfo = new DirectoryInfo(siteRootDir);
                if (dirInfo.Exists) siteRootDir = dirInfo.FullName;
            }
            if (oldSiteRootDir != null) {
                dirInfo = new DirectoryInfo(oldSiteRootDir);
                if (dirInfo.Exists) oldSiteRootDir = dirInfo.FullName;
            }
            Match match = Regex.Match(siteRootDir, @"^((.+)[\\/]+sites[\\/]+([^\\/]+))[\\/]+root[\\/]*$");
            siteBaseDir = match.Groups[1].Value;
            installationBaseDir = match.Groups[2].Value;
            
            string connectionString = null;

            // Get connection string (from web.config or build it from command line argument)
            if (dbMainSchema == null) {
                
                string configFile = String.Format("{0}{1}web.config", siteRootDir, Path.DirectorySeparatorChar);
                if (!File.Exists(configFile)) {
                    Console.Error.WriteLine(
                            String.Format("ERROR: The configuration file {0} does not exist.",
                                    configFile,
                                    File.Exists(configFile + ".tmpl") ? " You may create it using the .tmpl file." : String.Empty
                            )
                    );
                    return;
                }
                XmlDocument configDoc = new XmlDocument();
                configDoc.Load(configFile);
                
                XmlElement key = configDoc.SelectSingleNode("/configuration/appSettings/add[@key='DatabaseConnection']") as XmlElement;
                if (key == null || !key.HasAttribute("value") || (connectionString = key.Attributes["value"].Value) == String.Empty) {
                    Console.Error.WriteLine(String.Format("ERROR: No connection string found in web.config.{0}       Make sure the key \"DatabaseConnection\" exists under <appSettings>", Environment.NewLine));
                    return;
                }
                
            } else {
                
                connectionString = String.Format("Server={0}{1}{2}{3}; Database={4};",
                        dbHostname,
                        dbPort == 0 ? "" : "; Port=" + dbPort,
                        dbUsername == null ? "" : "; User Id=" + dbUsername,
                        dbPassword == null ? "" : "; Password=" + dbPassword,
                        dbMainSchema
                );
            }
                
            
            Console.WriteLine("Connecting to: " + Regex.Replace(connectionString, "(Password)=[^;]+", "$1=********", RegexOptions.IgnoreCase));
    
            try {
                OpenConnection(connectionString);
                schemaExists = true;
            } catch (Exception e) {
                if (!e.Message.Contains("Unknown database")) throw e;
            }
            
            if (schemaExists) {
                if (create) {
                    Console.Write("WARNING: Main portal schema \"{0}\" already exists. Recreating it will delete contained data. Recreate? ", dbMainSchema);
                    create = GetYes("Answer yes to delete and recreate: ");
                    if (!create) {
                        CloseConnection();
                        Console.WriteLine("Aborted (no change)");
                        return;
                    }
                }
            } else if (auto) {
                create = true;
            }
            
            if (create) CreateSchemas();
            
            if (schemaExists) {
                core = new CoreModule(this, String.Format("{0}{1}core", installationBaseDir, Path.DirectorySeparatorChar));
                if (File.Exists(String.Format("{0}{1}db{1}db-create.sql", siteBaseDir, Path.DirectorySeparatorChar))) site = new Site(this, siteBaseDir);
    
                if (create) {
                    core.Install();
                    
                    GetInstalledModules();
                    foreach (Module module in modules) {
                        module.Install();
                        module.CompleteChange();
                    }

                    ServiceItem.GetScriptBasedServiceTypeId(this);                    
                    GetInstalledServices();
                    foreach (ServiceItem service in services) service.Install();
                    
                    if (site != null) {
                        site.Install();
                        site.CompleteChange();
                    }
                } else {
                    if (site != null) site.PrepareChange();
                    
                    GetInstalledModules();
                    for (int i = modules.Count - 1; i >= 0; i--) modules[i].PrepareChange();

                    core.Upgrade();
                    
                    for (int i = 0; i < modules.Count; i++) {
                        Module module = modules[i];
                        if (module.IsNew) {
                            // Special case module "reporting": if its tables (e.g. usr_log) exist, installation is not necessary 
                            bool install = true;
                            switch (module.Identifier) {
                                case "reporting" :
                                    try {
                                        GetQueryIntegerValue("SELECT COUNT(*) FROM $MAIN$.usr_log;");
                                        module.SetVersion(String.Compare(core.PreviousVersion, "2.3") < 0 ? core.PreviousVersion : "1.0.3");
                                        module.PreviousVersion = module.Version;
                                        install = false;
                                    } catch (Exception) {}
                                    break;
                            }
                            if (install) module.Install();
                        }
                        module.Upgrade();
                        module.CompleteChange();
                    }

                    ServiceItem.GetScriptBasedServiceTypeId(this);                    
                    GetInstalledServices();
                    foreach (ServiceItem service in services) {
                        if (service.IsNew) service.Install();
                        service.Upgrade();
                    }
                    
                    // Install or upgrade site module if exists
                    if (site != null) {
                        if (site.IsNew) site.Install();
                        site.Upgrade();
                        site.CompleteChange();
                    }
                    
                    for (int i = modules.Count - 1; i >= 0; i--) {
                        modules[i].CompleteUpgrade();
                    }
                    core.CompleteUpgrade();
                }
                
            }

            WriteSeparator();
    
            if (create || schemaExists) {
                core.WriteChange();
                foreach (Module module in modules) module.WriteChange();
                foreach (ServiceItem service in services) service.WriteChange();
                if (site != null) site.WriteChange();
                WriteSeparator();
                Console.WriteLine("Portal database {0} successfully.", create ? "installed" : "upgraded");
            }
            
            WriteToDoList();
            WriteSeparator();
                
            CloseConnection();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        private bool GetYes(string hint) {
            string answer = Console.ReadLine().ToLower();
            if (answer == "no" || answer == "n") return false;
            if (answer != "yes") {
                Console.Write(hint);
                answer = Console.ReadLine().ToLower();
                return (answer == "yes"); 
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
                            siteRootDir,
                            Path.DirectorySeparatorChar
                    )
            );*/

            Console.WriteLine("Creating main portal schema \"{0}\"", dbMainSchema);
            Execute("CREATE DATABASE $MAIN$ DEFAULT CHARACTER SET utf8 DEFAULT COLLATE utf8_general_ci;");
            schemaExists = true;
            
            if (dbNewsSchema != dbMainSchema) {
                Console.WriteLine("Creating news schema \"{0}\"", dbNewsSchema);
                try {
                    Execute("USE $NEWS$;");
                    Execute("DROP DATABASE $NEWS$;");
                } catch (Exception) {}
                Execute("CREATE DATABASE $NEWS$ DEFAULT CHARACTER SET utf8 DEFAULT COLLATE utf8_general_ci;");
            }

            WriteSeparator();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        private void GetInstalledModules() {
            string[] moduleDirs = Directory.GetDirectories(String.Format("{0}{1}modules", installationBaseDir, Path.DirectorySeparatorChar), "*");
            modules = new List<Module>();
            foreach (string moduleDir in moduleDirs) {
                if (moduleDir.StartsWith(".")) continue;
                
                Match match = Regex.Match(moduleDir, @"([^\\/]+)$");
                if (!match.Success) continue;
                
                string moduleIdentifier = match.Groups[1].Value;
                string dbDir = String.Format("{0}{1}db", moduleDir, Path.DirectorySeparatorChar);
                if (!Directory.Exists(dbDir) || Directory.GetFiles(dbDir, "db-create.sql").Length == 0 /*|| !Directory.Exists(String.Format("{0}{2}{1}", siteRootDir, moduleIdentifier, Path.DirectorySeparatorChar))*/) continue;
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

        private void GetInstalledServices() {
            string[] serviceDirs = Directory.GetDirectories(String.Format("{0}{1}services", installationBaseDir, Path.DirectorySeparatorChar), "*");
            services = new List<ServiceItem>();
            foreach (string serviceDir in serviceDirs) {
                if (serviceDir.StartsWith(".")) continue;
                Match match = Regex.Match(serviceDir, @"([^\\/]+)$");
                if (!match.Success) continue;
                string serviceIdentifier = match.Groups[1].Value;
                string wwwDir = String.Format("{0}{1}www", serviceDir, Path.DirectorySeparatorChar);
                if (!Directory.Exists(wwwDir) || !File.Exists(String.Format("{0}{1}index.aspx", wwwDir, Path.DirectorySeparatorChar)) || !Directory.Exists(String.Format("{0}{2}services{2}{1}", siteRootDir, serviceIdentifier, Path.DirectorySeparatorChar))) continue;
                services.Add(new ServiceItem(this, serviceIdentifier, serviceDir));
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public void ExecuteSpecificAction(string identifier) {
            if (dbOldSchema == null) return;
            
            string incompleteServices = null;
            int seriesTotalCount = 0, seriesFoundCount = 0;
            
            switch (identifier) {
                case "import cedir" :
                    // Loop through the computing elements to obtain the working and result directories
                    dbReader = GetQueryResult(String.Format("SELECT id, UNESCAPE(wdir), UNESCAPE(rdir) FROM {1}{0}{1}.ce ORDER BY id", dbOldSchema, NameQuote));
                    while (dbReader.Read()) {
                        int id = dbReader.GetInt32(0);
                        string wdirStr = dbReader.GetString(1);
                        string rdirStr = dbReader.GetString(2);
                        string[] dirs;
                        OpenNewDbConnection();
                        if (wdirStr != null) {
                            dirs = wdirStr.Split(';');
                            for (int i = 0; i < dirs.Length; i++) {
                                Match match = Regex.Match(dirs[i], "(#)?(.*)");
                                string path = match.Groups[2].Value;
                                if (path == "") continue;
                                int available = (match.Groups[1].Value == "#" ? 0 : 1);
                                Execute("INSERT INTO cedir (id_ce, available, dir_type, path) VALUES (" + id + ", " + available + ", 'W', '" + path + "');");
                            }
                        }
                        if (rdirStr != null) {
                            dirs = rdirStr.Split(';');
                            for (int i = 0; i < dirs.Length; i++) {
                                Match match = Regex.Match(dirs[i], "(#)?(.*)");
                                string path = match.Groups[2].Value;
                                if (path == "") continue;
                                int available = (match.Groups[1].Value == "#" ? 0 : 1) ;
                                Execute("INSERT INTO cedir (id_ce, available, dir_type, path) VALUES (" + id + ", " + available + ", 'R', '" + path + "');");
                            }
                        }
                        CloseLastDbConnection();
                    }
                    dbReader.Close();
                    break;
                    
                case "import service_series" :
    
                    // Loop through services and extract dataset series and parameter information
                    if (oldSiteRootDir == null) {
                        DbResult = "Skipped (no site root directory specified)";
                        break;
                    }
                    
                    if (!Directory.Exists(oldSiteRootDir)) {
                        DbResult = "Skipped (invalid site root directory)";
                        seriesFoundCount = 0;
                        break;
                    }
    
                    oldSiteRootDir = Regex.Replace(oldSiteRootDir, @"[\\/]+$", "");
                    
                    dbReader = GetQueryResult(String.Format("SELECT s.id, UNESCAPE(s.name), os.href FROM service AS s INNER JOIN {1}{0}{1}.srv AS os ON s.id=os.id ORDER BY os.id",  dbOldSchema, NameQuote));
                    
                    while (dbReader.Read()) {
                        seriesTotalCount++;
                        int id = dbReader.GetInt32(0);
                        string serviceIdentifier = dbReader.GetString(1);
                        string filename = oldSiteRootDir + Path.DirectorySeparatorChar + dbReader.GetString(2);
                        bool seriesAdded = false;
                        
                        OpenNewDbConnection();
                        
                        if (File.Exists(filename)) {
                            seriesFoundCount++;
                            string datasetCollection = null;
                            string[] series = null;
                            string line, sql;
            
                            StreamReader sr = new StreamReader(filename);
                            Match match;
                            while ((line = sr.ReadLine()) != null) {
            
                                // Check for Dataset Series (Collection)
                                match = Regex.Match(line, "^[ \t]*datasetcollection[ \t]*=[ \t]*\"([^\"]*)\"", RegexOptions.IgnoreCase);
                                if (match.Success & match.Groups[1].Value.Trim() != "") {
                                    datasetCollection = match.Groups[1].Value.Trim();
                                }
            
                                // Check for default dataset
                                match = Regex.Match(line, "^[ \t]*defaultdataset[ \t]*=[ \t]*\"([^\"]*)\"", RegexOptions.IgnoreCase);
                                if (match.Success) {
                                    series = match.Groups[1].Value.Split('|');
                                }
                            }
            
                            if (datasetCollection != null) {
                                datasetCollection = Regex.Replace(datasetCollection, "collectionname", "t1.collectionname", RegexOptions.IgnoreCase); 
                                datasetCollection = Regex.Replace(datasetCollection, "satellite", "t1.satellite", RegexOptions.IgnoreCase); 
                                datasetCollection = Regex.Replace(datasetCollection, "sensor", "t1.sensor", RegexOptions.IgnoreCase); 
                                datasetCollection = Regex.Replace(datasetCollection, "name", "t1.name", RegexOptions.IgnoreCase); 
                                datasetCollection = Regex.Replace(datasetCollection, "t1.collectiont1.name", "t1.collectionname", RegexOptions.IgnoreCase); 
                                datasetCollection = Regex.Replace(datasetCollection, @"datasettype\.", "", RegexOptions.IgnoreCase); 
                                
                                sql = String.Format("INSERT INTO service_series (id_service, id_series) SELECT {0}, t.id FROM series AS t INNER JOIN $OLD$.datasettype AS t1 ON t.id=t1.id WHERE {1};", id, datasetCollection);
                                Execute(sql);
                            }
            
                            if (series != null) {
                                if (series.Length == 1) {
                                    if (datasetCollection == null) sql = "INSERT INTO service_series (id_service, id_series, is_default) SELECT {0}, id, 1 FROM series WHERE identifier='{1}';";
                                    else sql = "UPDATE service_series SET is_default=1 WHERE id_service={0} AND id_series=(SELECT MIN(id) FROM series WHERE identifier='{1}');";
                                    Execute(String.Format(sql, id, series[0].Replace("'", "\\'")));
                                    seriesAdded = true;
    
                                } else if (datasetCollection == null) {
                                    for (int i = 0; i < series.Length; i++) {
                                        sql = String.Format("INSERT INTO service_series (id_service, id_series) SELECT {0}, id FROM series WHERE identifier='{1}';", id, series[i].Replace("'", "\\'"));
                                        Execute(sql);
                                        seriesAdded = true;
                                    }
                                }
                            }
                            //string definition = filename.Replace(@"\", "/");
                            string definition = Regex.Replace(filename.Replace(oldSiteRootDir + Path.DirectorySeparatorChar + "services", "$(SERVICEROOT)"), @"/[^\\/]+\.asp$", "").Replace(@"\", "/");
                            Execute(String.Format("UPDATE service SET root='{1}' WHERE id={0};", definition.Replace("'", "''"), id));
                        }
                        
                        if (!seriesAdded) {
                            if (incompleteServices == null) incompleteServices = ""; else incompleteServices += ", ";
                            incompleteServices += serviceIdentifier;
                        }
                        CloseLastDbConnection();
                    }
                    dbReader.Close();
                    DbResult = String.Format("OK (imported {0} of {1})", seriesFoundCount, seriesTotalCount);
                    break;
                    
                case "import jobdependency" :
                    List<int> jobIds = new List<int>();
                    List<string> jobDependencies = new List<string>();
                    dbReader = GetQueryResult(String.Format("SELECT t.id, t1.dependency FROM job AS t INNER JOIN {1}{0}{1}.job AS t1 ON t.id=t1.id LEFT JOIN jobdependency AS t2 ON t.id=t2.id_job WHERE t1.dependency IS NOT NULL AND t1.dependency!='' AND t2.id_job IS NULL ORDER BY t.id;", dbOldSchema, NameQuote));
                    while (dbReader.Read()) {
                        jobIds.Add(dbReader.GetInt32(0));
                        jobDependencies.Add(dbReader.GetString(1));
                    }
                    dbReader.Close();
                    
                    for (int i = 0; i < jobIds.Count; i++) {
                        if (i % 1000 == 0) Console.Write(".");
                        string[] dependencies = jobDependencies[i].Split('|');
                        for (int j = 0; j < dependencies.Length; j++) Execute(String.Format("INSERT INTO jobdependency (id_job, id_job_input) SELECT j.id, ji.id FROM job AS j INNER JOIN task AS t ON j.id_task=t.id AND j.id={0} INNER JOIN job AS ji ON t.id=ji.id_task AND ji.name='{1}';", jobIds[i], dependencies[j])); 
                    }
                    jobIds.Clear();
                    jobDependencies.Clear();
                    Console.Write(" ");
                    break;
                    
                case "change service" :
                    List<int> serviceIds = new List<int>();
                    List<string> serviceLogoUrls = new List<string>();
                    dbReader = GetQueryResult("SELECT t.id, t.logo_url FROM service AS t;");
                    while (dbReader.Read()) {
                        serviceIds.Add(dbReader.GetInt32(0));
                        serviceLogoUrls.Add(dbReader.GetString(1));
                    }
                    dbReader.Close();
                    
                    for (int i = 0; i < serviceIds.Count; i++) {
                        string logoUrl = serviceLogoUrls[i];
                        
                        if (logoUrl == null) continue;
                        
                        //Console.WriteLine("File " + logoFullFilename + " " + (File.Exists(logoFullFilename) ? " exists" : "DOES NOT EXIST!"));*/
                        string logoFilename = Regex.Replace(logoUrl, @"[\\/]+$", "");
                        
                        Execute(String.Format("UPDATE service SET icon_url=CONCAT('/images/', '{1}') WHERE id={0};", logoFilename.Replace("'", "''"), serviceIds[i]));
                    }
                    
                    serviceIds.Clear();
                    serviceLogoUrls.Clear();
                    break;
            }
                    
            if (identifier == "import service_series" && incompleteServices != null) {
                if (seriesFoundCount == 0) AddToDoTask("In the Control Panel > Services, select the compatible dataset series for all processing services");
                else AddToDoTask("In the Control Panel > Services, select the compatible dataset series for the following processing services:\\n" + incompleteServices);
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public void SetItemId(int value) {
            this.itemId = value;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static void WriteSeparator() {
            Console.WriteLine("----------------------------------------------------------------------------------------------------");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static void WriteErrorSeparator() {
            Console.Error.WriteLine("----------------------------------------------------------------------------------------------------");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string GetSchemaName(string placeholder) {
            switch (placeholder) {
                case "$MAIN$" :
                    return dbMainSchema;
                case "$NEWS$" :
                    return dbNewsSchema;
                case "$OLD$" :
                    return dbOldSchema;
                case "$OLDNEWS$" :
                    return dbOldNewsSchema;
            }
            return null;
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
            if (toDoCount == 0) return;
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
                        if (dbMainSchema == null) return null;
                        sql = sql.Replace(value, NameQuote + dbMainSchema + NameQuote);
                        break;
                    case "$NEWS$" :
                        if (dbNewsSchema == null) return null;
                        sql = sql.Replace(value, NameQuote + dbNewsSchema + NameQuote);
                        break;
                    case "$OLD$" :
                        if (dbOldSchema == null) return null;
                        sql = sql.Replace(value, NameQuote + dbOldSchema + NameQuote);
                        break;
                    case "$OLDNEWS$" :
                        if (dbOldNewsSchema == null) return null;
                        sql = sql.Replace(value, NameQuote + dbOldNewsSchema + NameQuote);
                        break;
                    case "$DBHOSTNAME$" :
                        sql = sql.Replace(value, dbHostname);
                        break;
                    case "$DBPORT$" :
                        sql = sql.Replace(value, dbPort.ToString());
                        break;
                    case "$DBUSERNAME$" :
                        sql = sql.Replace(value, dbUsername);
                        break;
                    case "$DBPASSWORD$" :
                        sql = sql.Replace(value, dbPassword);
                        break;
                    case "$SERVICEROOT$" :
                        sql = sql.Replace(value, String.Format("{0}/services", siteRootDir.Replace(@"\", "/")));
                        break;
                    case "$CATALOGUE$" :
                        sql = sql.Replace(value, catalogueBaseUrl.Replace("'", "''"));
                        break;
                    case "$ID$" :
                        sql = sql.Replace(value, itemId.ToString());
                        break;
                }
            }
            return sql;
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        public static bool GetArgs(string[] args) {
            if (args.Length == 0) return false;
            switch (args[0]) {
                case "create" : 
                    create = true;
                    break;
                case "upgrade" :
                    create = false;
                    break;
                case "auto" :
                    auto = true;
                    break;
                default :
                    return false;
            }
            
            int argpos = 1;
            while (argpos <= args.Length - 1) {
                switch (args[argpos]) {
                    case "-v" :
                        verbose = true;
                        break;
                    case "-r" :
                        siteRootDir = args[++argpos];;
                        break;
                    case "-k" :
                        //didYouKnow = true;
                        break;
                    case "-H" : 
                        if (argpos < args.Length - 1) {
                            dbHostname = args[++argpos];
                        } else return false;
                        break;
                    case "-P" : 
                        if (argpos < args.Length - 1) {
                            Int32.TryParse(args[++argpos], out dbPort);
                        } else return false;
                        break;
                    case "-u" :
                        if (argpos < args.Length - 1) {
                            dbUsername = args[++argpos];
                        } else return false;
                        break;
                    case "-p" : 
                        if (argpos < args.Length - 1) {
                            dbPassword = args[++argpos];
                        } else return false;
                        break;
                    case "-S" : 
                        if (argpos < args.Length - 1) {
                            dbMainSchema = args[++argpos];
                        } else return false;
                        break;
                    case "-Sn" : 
                        if (argpos < args.Length - 1) {
                            dbNewsSchema = args[++argpos];
                        } else return false;
                        break;
                    case "-O" : 
                        if (argpos < args.Length - 1) {
                            dbOldSchema = args[++argpos];
                        } else return false;
                        break;
                    case "-On" : 
                        if (argpos < args.Length - 1) {
                            dbOldNewsSchema = args[++argpos];
                        } else return false;
                        break;
                    case "-Or" : 
                        if (argpos < args.Length - 1) {
                            oldSiteRootDir = args[++argpos];
                        } else return false;
                        break;
                    case "-c" : 
                        if (argpos < args.Length - 1) {
                            catalogueBaseUrl = args[++argpos];
                            if (!catalogueBaseUrl.EndsWith("/")) catalogueBaseUrl += "/";
                        } else return false;
                        break;
                    default: 
                        return false;
                }
                argpos++;
            }
            
            if (dbNewsSchema == null) dbNewsSchema = dbMainSchema; 
            
            if (siteRootDir == null) Console.Error.WriteLine("ERROR: Missing argument for -r");
            //else if (!File.Exists(siteRootDir + Path.DirectorySeparatorChar + "web.config") && dbMainSchema == null) Console.Error.WriteLine("ERROR: No web.config found in root directory, specify database schema name g using -S");
            else if (dbMainSchema == null) Console.Error.WriteLine("ERROR: Specify database schema name using -S");
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
            Console.Error.WriteLine("    create    [-v] -r [-H] [-P] [-u] [-p] -S [-Sn] [-O] [-On] [-Or] [-c]");
            Console.Error.WriteLine("    upgrade   [-v] -r [-H] [-P] [-u] [-p] -S [-Sn]");
            Console.Error.WriteLine("    auto      [-v] -r [-H] [-P] [-u] [-p] -S [-Sn]");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Arguments:");
            Console.Error.WriteLine("    -v             : verbose mode, displays all queries");
            Console.Error.WriteLine("    -r <name>      : local path to site root directory");
            Console.Error.WriteLine("    -H <hostname>  : name of database server host (default: localhost)");
            Console.Error.WriteLine("    -P <port>      : port of database server (optional)");
            Console.Error.WriteLine("    -u <name>      : database username (optional); in create mode, database root privileges are required");
            Console.Error.WriteLine("    -p <password>  : database password (optional)");
            Console.Error.WriteLine("    -S <name>      : name of main portal schema");
            Console.Error.WriteLine("    -Sn <name>     : name of news schema (optional)");
            Console.Error.WriteLine("    -O <name>      : name of main schema of portal ASP version (optional for data import during creation)");
            Console.Error.WriteLine("    -On <name>     : name of news schema of portal ASP version (optional for news import during creation)");
            Console.Error.WriteLine("    -Or <dir>      : site root directory of portal ASP version (optional for service import during creation)");
            Console.Error.WriteLine("    -c <url>       : catalogue host and virtual folder common to all dataset series description URLs");
            Console.Error.WriteLine("                     (optional for dataset series import during creation)");
            Console.Error.WriteLine();
        }
    


        public static int DB_MYSQL = 1;
        public static int DB_POSTGRES = 2;
        
        private int type = 1;
        private string mainConnectionString, currentConnectionString;
        private IDbConnection mainDbConn, currentDbConn;
        private List<DbConnData> dbConns = new List<DbConnData>();
        private IDbCommand dbCommand;
        private int totalRowCount = -1;
    
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

        public void WriteResult() {
            if (DbResult == null) DbResult = String.Format("OK{0}", totalRowCount == -1 ? "" : " (" + totalRowCount + " record" + (totalRowCount == 1 ? "" : "s") + ")");
            if (Verbose) DbResult = "-- " + DbResult;
            Console.WriteLine(DbResult);
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
                string newConnectionString = Regex.Replace(mainConnectionString, "Database=[^ ]+$", "Database=information_schema");
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
                        if (message) Console.Write("Creating table \"{0}\" ({1}) ... ", name, StringUtils.UnescapeSql(match2.Groups[1].Value));
                        newLine = false;
                        break;
                    case "CREATE FUNCTION" :
                        match2 = Regex.Match(sql, "COMMENT (('([^']+)')+)");
                        message = (!Verbose && match2.Success);
                        if (message) Console.Write("Creating stored function \"{0}\" ({1}) ... ", name, StringUtils.UnescapeSql(match2.Groups[1].Value));
                        newLine = false;
                        break;
                    case "CREATE PROCEDURE" :
                        match2 = Regex.Match(sql, "COMMENT (('([^']+)')+)");
                        message = (!Verbose && match2.Success);
                        if (message) Console.Write("Creating stored procedure \"{0}\" ({1}) ... ", name, StringUtils.UnescapeSql(match2.Groups[1].Value));
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
                if (Verbose) {
                    Console.WriteLine(sql);
                }
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
            if (Verbose) Console.WriteLine("-- rows: " + rowCount); 
    
            if (changeData != 0 && rowCount != -1) totalRowCount = (totalRowCount == -1 ? 0 : totalRowCount) + rowCount;
    
            if (message) Console.WriteLine("OK");
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



    public abstract class SoftwareItem {
    
        private int id = 0;
        private string version = null;
        private string newVersion;
        private bool execute = true;
        private bool afterCheckpoint;
        private string importingFrom;
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public int Id { 
            get {
                return id;
            }
            
            protected set {
                this.id = value;
                Tool.SetItemId(Id); 
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public bool Valid { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        protected AdminTool Tool { get; set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public string Identifier { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public abstract string Caption { get; }

        //---------------------------------------------------------------------------------------------------------------------

        public abstract string ItemsCaption { get; }

        //---------------------------------------------------------------------------------------------------------------------

        public string BaseDir { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------
        
        public string Version {
            get {
                return version;
            }

            protected set {
                if (version == null) {
                    CompletedVersion = value;
                    PreviousVersion = value;
                }
                version = value;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public string Checkpoint { get; protected set; }
    
        //---------------------------------------------------------------------------------------------------------------------

        public string CompletedVersion { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string CompletedCheckpoint { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public string PreviousVersion { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool Completing { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        public bool Exists {
            get { return Id != 0; }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether the software item has just been installed.</summary>
        public bool IsNew { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public SoftwareItem(AdminTool tool, string identifier, string baseDir) {
            this.Tool = tool;
            this.Identifier = identifier;
            this.BaseDir = baseDir;
            tool.SetItemId(0);
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        protected void SetId() {
            IsNew = true;
            Id = Tool.GetQueryIntegerValue("SELECT LAST_INSERT_ID();");
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void SetVersion(string version) {
            this.version = version;
            Checkpoint = null;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void SetCheckpoint(string checkpoint) {
            Checkpoint = checkpoint;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public virtual void SetCompletedVersion(string completedVersion) {
            Checkpoint = null;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void SetCompletedCheckpoint(string completedCheckpoint) {
            CompletedCheckpoint = completedCheckpoint;
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual void Install() {
            afterCheckpoint = true;
            string file = String.Format("{0}{1}db{1}db-create.sql", BaseDir, Path.DirectorySeparatorChar);
            if (!File.Exists(file)) return;

            AdminTool.WriteSeparator();
            Console.WriteLine("Install {0} ({1}):", ItemsCaption, file);
            ExecuteSqlScript(file);

            SetVersion(newVersion);
        }

        //---------------------------------------------------------------------------------------------------------------------

        public virtual bool Upgrade() {
            afterCheckpoint = (Checkpoint == null);
            string dir = String.Format("{0}{1}db", BaseDir, Path.DirectorySeparatorChar);
            if (!Directory.Exists(dir)) return false;
            
            string[] files = Directory.GetFiles(dir, "db-*#.sql");
            
            Array.Sort(files, new FileNameComparer());
            int count = 0;
            foreach (string file in files) {
                string fileVersion = null;
                Match match = Regex.Match(file, @"db-([0-9\.]+)\#\.sql$");
                if (match.Success) fileVersion = match.Groups[1].Value;
                if (fileVersion == null || String.Compare(fileVersion, Version) <= 0) continue;
                
                AdminTool.WriteSeparator();
                Console.WriteLine("Upgrade {0} to version {1}{2}{3}({4}):", ItemsCaption, fileVersion, Checkpoint == null ? String.Empty : " (recovering from checkpoint " + Checkpoint + ")", Environment.NewLine, file);
                ExecuteSqlScript(file);
                
                SetVersion(fileVersion);
                count++;
            }
            
            return (count != 0);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual bool CompleteUpgrade() {
            Completing = true;

            afterCheckpoint = (CompletedCheckpoint == null);
            string dir = String.Format("{0}{1}db", BaseDir, Path.DirectorySeparatorChar);
            if (!Directory.Exists(dir)) return false;
            
            string[] files = Directory.GetFiles(dir, "db-*c.sql");
            
            Array.Sort(files, new FileNameComparer());
            int count = 0;
            foreach (string file in files) {
                string fileVersion = null;
                Match match = Regex.Match(file, @"db-([0-9\.]+)c\.sql$");
                if (match.Success) fileVersion = match.Groups[1].Value;
                if (fileVersion == null || String.Compare(fileVersion, CompletedVersion) <= 0) continue;
                
                AdminTool.WriteSeparator();
                Console.WriteLine("Complete upgrade of {0} for version {1}{2}({3}):", ItemsCaption, fileVersion, Environment.NewLine, file);
                ExecuteSqlScript(file);
                SetCompletedVersion(fileVersion);

                count++;
            }
            SetCompletedVersion(Version);
            Completing = false;

            return (count != 0);
        }
        
        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual void PrepareChange() {
            afterCheckpoint = true;
            string file = String.Format("{0}{1}db{1}db-prepare.sql", BaseDir, Path.DirectorySeparatorChar);
            if (!File.Exists(file)) return;

            AdminTool.WriteSeparator();
            Console.WriteLine("Prepare {1} of {0} ({2}):", ItemsCaption, IsNew ? "installation" : "upgrade", file);
            ExecuteSqlScript(file);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public virtual void CompleteChange() {
            afterCheckpoint = true;
            string file = String.Format("{0}{1}db{1}db-complete.sql", BaseDir, Path.DirectorySeparatorChar);
            if (!File.Exists(file)) return;

            AdminTool.WriteSeparator();
            Console.WriteLine("Complete {1} of {0} ({2}):", ItemsCaption, IsNew ? "installation" : "upgrade", file);
            ExecuteSqlScript(file);
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        protected virtual void ExecuteSqlScript(string filename) {
            Tool.SetItemId(Id);
            Tool.ResetResult();
            Tool.Execute("USE $MAIN$;");
            StreamReader fileReader = new StreamReader(filename);
            string line, command = String.Empty;
            bool routine = false;
            while ((line = fileReader.ReadLine()) != null) {
                string tline = line.Trim();
                if (command == String.Empty && Regex.Match(tline, "^CREATE (FUNCTION|PROCEDURE|TRIGGER) ").Success) routine = true;
    
                if (tline.StartsWith("--") && command == String.Empty && Tool.CurrentSchema != null) {
                    WriteComment(tline.Substring(2).Trim());
                    continue;
                } else if (tline.StartsWith("DELIMITER ")) {
                    if (execute && afterCheckpoint) Tool.Execute(tline);
                    command = String.Empty;
                    routine = false;
                    continue;
                }
                
                if (tline != String.Empty && !Regex.Match(tline, @"^/\*\**\*/$").Success) command += (command == String.Empty ? String.Empty : "\n") + tline;
                
                if (tline.EndsWith(";") && (!routine || tline.StartsWith("END;"))) {
                    if (execute && afterCheckpoint) {
                        Match match = Regex.Match(command, "^USE +([^ ]+);$");
                        if (match.Success) Tool.CurrentSchema = match.Groups[1].Value;
                        
                        if (Tool.CurrentSchema != null) Tool.Execute(command);
                    }
                    command = String.Empty;
                    routine = false;
                }
            }
            fileReader.Close();
            Tool.Execute("USE $MAIN$;");
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        public void WriteChange() {
            Console.WriteLine("{0,-40} :  version {1,-10}({2})",
                    Caption, Version == null ? "<unknown>" : Version, PreviousVersion == Version ? "no change" : IsNew ? "installed now" : "upgraded from " + PreviousVersion
            );
        }

        //---------------------------------------------------------------------------------------------------------------------
        
        private void WriteComment(string comment) {
            bool write = false;
            
            if (comment.StartsWith("VERSION ")) {
                newVersion = comment.Substring(8);
    
            } else if (comment.StartsWith("CHECKPOINT")) {
                string newCheckpoint = comment.Substring(11);
                if (afterCheckpoint) {
                    if (Completing) SetCompletedCheckpoint(newCheckpoint);
                    else SetCheckpoint(newCheckpoint);
                } else if (Checkpoint == newCheckpoint) {
                    afterCheckpoint = true;
                }
            } else if (comment.StartsWith("IMPORT START ")) {
                importingFrom = Tool.GetSchemaName(comment.Substring(13).Trim());
                execute = (importingFrom != null);
                
            } else if (comment == "IMPORT END") {
                importingFrom = null;
                execute = true;
            } else if (execute && afterCheckpoint) {
                if (comment == "TRY START") Tool.ProtectedStatement = true;
                else if (comment == "TRY END") Tool.ProtectedStatement = false;
                else if (comment == "NORESULT") Tool.ResetResult();
                else if (comment == "RESULT") Tool.WriteResult();
                else if (comment.StartsWith("TODO ")) Tool.AddToDoTask(comment.Substring(5));
                else if (comment == "SEPARATOR") AdminTool.WriteSeparator();
                else write = true;
            }
            
            if (write) {
                Match match = Regex.Match(comment, @"^EXECUTE\((.+)\)$");
                if (match.Success) Tool.ExecuteSpecificAction(match.Groups[1].Value);
                else /*if (!importing || importingFrom != null)*/ {
                    Tool.NewLine = !comment.EndsWith(@"\");
                    if (!Tool.NewLine) comment = Regex.Replace(comment, @"\\$", "");
                    if (Tool.Verbose) comment = "-- " + comment;
                    Console.Write(comment);
                    if (Tool.NewLine || Tool.Verbose) Console.WriteLine();
                }
            }
        }
        
    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class Module : SoftwareItem {
        
        private int distance;

        //---------------------------------------------------------------------------------------------------------------------

        public override string Caption {
            get { return String.Format("Module \"{0}\"", Identifier); } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override string ItemsCaption {
            get { return String.Format("database items of module \"{0}\"", Identifier); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public List<Module> Dependencies { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public int Distance {
            get {
                if (distance != -1) return distance;
                distance = 0;
                if (Dependencies == null || Dependencies.Count == 0) return distance;
                foreach (Module module in Dependencies) {
                    int newDistance = module.Distance + 1;
                    if (newDistance > distance) distance = newDistance;
                }
                return distance;
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public Module(AdminTool tool, string identifier, string baseDir) : base(tool, identifier, baseDir) {
            Dependencies = new List<Module>();
            distance = -1;
            try {
                Load();
                if (!Exists) {
                    tool.Execute(String.Format("INSERT INTO $MAIN$.module (name) VALUES ({0});", StringUtils.EscapeSql(Identifier)));
                    SetId();
                }
            } catch (Exception) {
                IDataReader reader = Tool.GetQueryResult("SHOW TABLES IN $MAIN$ LIKE 'module';");
                bool moduleTableExists = reader.Read();
                reader.Close();
                if (moduleTableExists) {
                    Tool.Execute("ALTER TABLE $MAIN$.module\n" +
                                 "    CHANGE COLUMN version_checkpoint checkpoint varchar(10) COMMENT 'Last checkpoint reached during failed creation/upgrade',\n" +
                                 "    ADD COLUMN c_version varchar(10) COMMENT 'Installed version (including cleanup)' AFTER version,\n" +
                                 "    ADD COLUMN c_checkpoint varchar(10) COMMENT 'Last checkpoint reached during failed creation/upgrade' AFTER c_version\n" +
                                 ";"
                    );
                    Tool.Execute("UPDATE $MAIN$.module SET c_version = version;");
                    Load();

                } else {
                    Tool.Execute("CREATE TABLE $MAIN$.module (\n" +
                                 "    id int unsigned NOT NULL auto_increment,\n" +
                                 "    name varchar(50) NOT NULL COMMENT 'Unique identifier',\n" +
                                 "    version varchar(10) COMMENT 'Installed version',\n" +
                                 "    checkpoint varchar(10) COMMENT 'Last checkpoint reached during failed creation/upgrade',\n" +
                                 "    c_version varchar(10) COMMENT 'Installed version (including cleanup)',\n" +
                                 "    c_checkpoint varchar(10) COMMENT 'Last checkpoint reached during failed cleanup',\n" +
                                 "    CONSTRAINT pk_module PRIMARY KEY (id),\n" +
                                 "    UNIQUE INDEX (name)\n" +
                                 ") Engine=InnoDB COMMENT 'Installed modules';"
                                 );
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void Load() {
            IDataReader reader = Tool.GetQueryResult(String.Format("SELECT id, version, checkpoint, c_version, c_checkpoint FROM $MAIN$.module WHERE name={0};", StringUtils.EscapeSql(Identifier)));
            if (reader.Read()) {
                this.Id = reader.GetInt32(0);
                this.Version = (reader.GetValue(1) == DBNull.Value ? null : reader.GetString(1));
                this.Checkpoint = (reader.GetValue(2) == DBNull.Value ? null : reader.GetString(2));
                this.CompletedVersion = (reader.GetValue(3) == DBNull.Value ? null : reader.GetString(3));
                this.CompletedCheckpoint = (reader.GetValue(4) == DBNull.Value ? null : reader.GetString(4));
                if (this.Version == null) this.IsNew = true;
            }
            reader.Close();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override void SetVersion(string version) {
            base.SetVersion(version);
            try {
                Tool.Execute(String.Format("UPDATE $MAIN$.module SET version={1}, checkpoint=NULL WHERE name={0};", StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(Version)));
            } catch (Exception) {}
            
        }
    
        //---------------------------------------------------------------------------------------------------------------------

        public override void SetCheckpoint(string checkpoint) {
            base.SetCheckpoint(checkpoint);
            try {
                Tool.Execute(String.Format("UPDATE $MAIN$.module SET checkpoint={1} WHERE name={0};", StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(Checkpoint)));
            } catch (Exception) {}
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void SetCompletedVersion(string version) {
            base.SetCompletedVersion(version);
            try {
                Tool.Execute(String.Format("UPDATE $MAIN$.module SET c_version={1}, c_checkpoint=NULL WHERE name={0};", StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(CompletedVersion)));
            } catch (Exception) {}

        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void SetCompletedCheckpoint(string checkpoint) {
            base.SetCompletedCheckpoint(checkpoint);
            try {
                Tool.Execute(String.Format("UPDATE $MAIN$.module SET c_checkpoint={1} WHERE name={0};", StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(CompletedCheckpoint)));
            } catch (Exception) {}
        }

    }



    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class CoreModule : Module {
        
        //---------------------------------------------------------------------------------------------------------------------

        public override string Caption {
            get { return "Core module"; } 
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override string ItemsCaption {
            get { return "core database items"; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public CoreModule(AdminTool tool, string baseDir) : base(tool, "core", baseDir) {
            if (this.Exists) {

            } else {
                try {
                    this.Version = Tool.GetQueryStringValue("SELECT value FROM $MAIN$.config WHERE name='DbVersion';");
                    this.Checkpoint = Tool.GetQueryStringValue("SELECT value FROM $MAIN$.config WHERE name='DbVersionCheckpoint';");
                        
                } catch (Exception) {}
                
                tool.Execute(String.Format("INSERT INTO $MAIN$.module (name, version, checkpoint) VALUES ({0}, {1}, {2});", StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(Version), StringUtils.EscapeSql(Checkpoint)));
                SetId();
            }

        }
    
    }
    

    
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class ServiceItem : SoftwareItem {
        
        //---------------------------------------------------------------------------------------------------------------------

        public static int ScriptBasedServiceTypeId { get; protected set; }
        
        //---------------------------------------------------------------------------------------------------------------------

        public override string Caption {
            get { return String.Format("Service \"{0}\"", Identifier); } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override string ItemsCaption {
            get { return String.Format("database items of service \"{0}\"", Identifier); }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public ServiceItem(AdminTool tool, string identifier, string baseDir) : base(tool, identifier, baseDir) {
            try {
                IDataReader reader = Tool.GetQueryResult(String.Format("SELECT id, version FROM $MAIN$.service WHERE identifier={0};", StringUtils.EscapeSql(Identifier)));
                if (reader.Read()) {
                    this.Id = reader.GetInt32(0); 
                    this.Version = (reader.GetValue(1) == DBNull.Value ? null : reader.GetString(1));
                    if (this.Version == null) this.IsNew = true;
                }
                reader.Close();
                if (!this.Exists) {
                    tool.Execute(String.Format("INSERT INTO $MAIN$.service (id_type, available, identifier, name, description, version) VALUES ({0}, true, {1}, {1}, {1}, {2});", ScriptBasedServiceTypeId, StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(Version)));
                    SetId();
                    tool.Execute(String.Format("INSERT INTO $MAIN$.scriptservice (id, root) VALUES ({0}, {1});", Id, StringUtils.EscapeSql("$(SERVICEROOT)/" + Identifier)));
                }
            } catch (Exception e) {
                Console.Error.WriteLine("ERROR: Could not add service: " + e.Message);
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        public static void GetScriptBasedServiceTypeId(AdminTool tool) {
            ScriptBasedServiceTypeId = tool.GetQueryIntegerValue("SELECT id FROM type WHERE class = 'Terradue.Portal.ScriptBasedService, Terradue.Portal';");
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void SetVersion(string version) {
            base.SetVersion(version);
            try {
                Tool.Execute(String.Format("UPDATE $MAIN$.service SET version={1} WHERE identifier={0};", StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(Version)));
            } catch (Exception) {}
            
        }
    
    }
        


    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------



    public class Site : Module {

        //---------------------------------------------------------------------------------------------------------------------

        public override string Caption {
            get { return "Site module"; } 
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override string ItemsCaption {
            get { return "site-specific database items"; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public Site(AdminTool tool, string baseDir) : base(tool, "site", baseDir) {}
        
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

    

    public class ModuleComparer1 : IComparer {
        int IComparer.Compare(object x, object y) {
            Module xm = x as Module;
            Module ym = y as Module;
            if (xm.Dependencies.Contains(ym)) return 1;
            else if (ym.Dependencies.Contains(xm)) return -1;
            return 0;
        }
    }

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



    public class FileNameComparer : IComparer {
        int IComparer.Compare(object x, object y) {
            string xs = (x as string);
            string ys = (y as string);
            return String.Compare(xs.Replace(".sql", String.Empty), ys.Replace(".sql", String.Empty));
        }
    }
}

