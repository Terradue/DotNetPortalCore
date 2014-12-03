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



    public abstract class SoftwareItem {

        private int id = 0;

        //---------------------------------------------------------------------------------------------------------------------

        protected AdminTool Tool { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the database ID of this software item.</summary>
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

        /// <summary>Gets or sets the unique identifier string of this software item.</summary>
        public string Identifier { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public abstract string Caption { get; }

        //---------------------------------------------------------------------------------------------------------------------

        public abstract string ItemsCaption { get; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the base directory containing the files of this software item.</summary>
        public string BaseDir { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the current version of this software item or the version currently being installed or upgraded to.</summary>
        /// <remarks>The version is only made persistent after the successful execution of the installation or upgrade script via <see cref="RegisterVersion">RegisterVersion</see>.</remarks>
        public string Version { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the current version of this software has had before the run of the application.</summary>
        public string PreviousVersion { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the version of this software will have after the successful execution of the installation script.</summary>
        public string NewVersion { get; set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Gets or sets (protected) the version of the last cleanup of this software item.</summary>
        /// <remarks>
        ///     At the beginning of the processing, if there was no failure in the previous run, the last cleanup version is unset and assumed to be the same as the current version.
        ///     In case of a failure during the previous run, the value contains the last version of the last cleanup.
        /// </remarks>
        public string CleanupVersion { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether this software has already a database record representing it.</summary>
        public bool Exists {
            get { return Id != 0; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether this software item has just been installed.</summary>
        public bool IsInstalled {
            get { return Version != null; }
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Indicates whether this software item was already installed before the application started.</summary>
        public bool WasInstalledBefore { get; protected set; }

        //---------------------------------------------------------------------------------------------------------------------

        public SoftwareItem(AdminTool tool, string identifier, string baseDir) {
            this.Tool = tool;
            this.Identifier = identifier;
            this.BaseDir = baseDir;
            this.Id = 0;
        }

        //---------------------------------------------------------------------------------------------------------------------

        protected void SetId() {
            Id = Tool.GetQueryIntegerValue("SELECT LAST_INSERT_ID();");
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, persistently stores the current version of this software item.</summary>
        public abstract void RegisterVersion();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>In a derived class, persistently stores the current cleanup version of this software item.</summary>
        public abstract void RegisterCleanupVersion();

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Copies previous version information from another instance.</summary>
        public void RestoreFrom(SoftwareItem oldItem) {
            PreviousVersion = oldItem.PreviousVersion;
            WasInstalledBefore = oldItem.WasInstalledBefore;
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Runs the default database preparation script for this software item.</summary>
        public void PrepareChange() {
            if (Tool.LastFailurePhase > ProcessPhaseType.Prepare) return;

            string file = String.Format("{0}{1}db{1}db-prepare.sql", BaseDir, Path.DirectorySeparatorChar);
            if (!File.Exists(file)) return;

            AdminTool.WriteSeparator();
            Console.WriteLine("Prepare {1} of {0} ({2}):", ItemsCaption, WasInstalledBefore ? "upgrade" : "installation", file);
            Tool.ExecuteSqlScript(file, this);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Runs the database installation script for this software item.</summary>
        public virtual void Install() {
            if (Tool.LastFailurePhase > ProcessPhaseType.InstallAndUpgrade) return;
            if (Tool.LastFailurePhase == ProcessPhaseType.InstallAndUpgrade && this != Tool.LastFailureItem) return;

            bool isFailureItem = Tool.LastFailurePhase == ProcessPhaseType.InstallAndUpgrade && this == Tool.LastFailureItem;

            string file = String.Format("{0}{1}db{1}db-create.sql", BaseDir, Path.DirectorySeparatorChar);
            if (!File.Exists(file)) throw new FileNotFoundException(String.Format("Installation script for {0} not found at {1}", ItemsCaption, file));

            AdminTool.WriteSeparator();
            Console.WriteLine("Install {0}{1}\n({2}):", ItemsCaption, isFailureItem && !Tool.AfterFailureCheckpoint ? String.Format(" (recovering from {0})", Tool.CheckpointText) : String.Empty, file);
            Tool.ExecuteSqlScript(file, this);

            Version = NewVersion;
            CleanupVersion = Version;

            RegisterVersion();
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Runs the latest database upgrade scripts for this software item.</summary>
        public virtual bool Upgrade() {
            if (Tool.LastFailurePhase > ProcessPhaseType.InstallAndUpgrade) return false;
            if (Tool.LastFailurePhase == ProcessPhaseType.InstallAndUpgrade && (!Tool.AfterFailureCheckpoint || this != Tool.LastFailureItem)) return false;

            bool isFailureItem = Tool.LastFailurePhase == ProcessPhaseType.InstallAndUpgrade && this == Tool.LastFailureItem;

            string dir = String.Format("{0}{1}db", BaseDir, Path.DirectorySeparatorChar);
            if (!Directory.Exists(dir)) return false;

            List<string> files = new List<string>(Directory.GetFiles(dir, "db-*#.sql"));

            files.Sort(new FileNameComparer());
            int count = 0;

            // Execute only scripts with version > PreviousVersion or, in case of failure in the previous run for the current, item
            foreach (string file in files) {
                Match match = Regex.Match(file, @"db-([\d\.]+)#\.sql$");
                if (!match.Success) continue;

                string currentFileVersion = match.Groups[1].Value;
                if (FileNameComparer.Compare(currentFileVersion, Version) <= 0 && (!isFailureItem || currentFileVersion != Tool.LastFailureItemVersion)) continue;
                if (isFailureItem && Tool.AfterFailureCheckpoint) continue;

                Version = currentFileVersion;

                AdminTool.WriteSeparator();
                Console.WriteLine("Upgrade {0} to version {1}{2}\n({3}):", ItemsCaption, Version, isFailureItem && !Tool.AfterFailureCheckpoint ? String.Format(" (recovering from {0})", Tool.CheckpointText) : String.Empty, file);
                Tool.ExecuteSqlScript(file, this);

                RegisterVersion();
                count++;

                if (isFailureItem) break;
            }

            return (count != 0);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Runs the latest database cleanup scripts corresponding to the last upgrades for this software item.</summary>
        public virtual bool Cleanup() {
            if (Tool.LastFailurePhase > ProcessPhaseType.Cleanup) return false;
            if (Tool.LastFailurePhase == ProcessPhaseType.Cleanup && !Tool.AfterFailureCheckpoint) return false;

            bool isFailureItem = Tool.LastFailurePhase == ProcessPhaseType.Cleanup && this == Tool.LastFailureItem;

            string dir = String.Format("{0}{1}db", BaseDir, Path.DirectorySeparatorChar);
            if (!Directory.Exists(dir)) return false;

            string[] files = Directory.GetFiles(dir, "db-*c.sql");

            Array.Sort(files, new FileNameComparer());
            int count = 0;
            foreach (string file in files) {
                Match match = Regex.Match(file, @"db-([\d\.]+)c\.sql$");
                if (!match.Success) continue;

                string currentFileVersion = match.Groups[1].Value;

                // Process file only if current file version is greater than last CleanupVersion, otherwise skip file
                if (FileNameComparer.Compare(currentFileVersion, CleanupVersion) <= 0 && (!isFailureItem || currentFileVersion != Tool.LastFailureItemVersion)) continue;

                // Skip if current file is beyond Version
                if (FileNameComparer.Compare(currentFileVersion, Version) > 0) continue;

                CleanupVersion = currentFileVersion;

                AdminTool.WriteSeparator();
                Console.WriteLine("Run {0} cleanup for version {1}{2}\n({3}):", ItemsCaption, CleanupVersion, isFailureItem && !Tool.AfterFailureCheckpoint ? String.Format(" (recovering from {0})", Tool.CheckpointText) : String.Empty, file);
                Tool.ExecuteSqlScript(file, this);
                RegisterCleanupVersion();

                count++;
            }

            return (count != 0);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Runs the default database completion script for this software item.</summary>
        public virtual void CompleteChange() {
            if (Tool.LastFailurePhase > ProcessPhaseType.Complete) return;

            string file = String.Format("{0}{1}db{1}db-complete.sql", BaseDir, Path.DirectorySeparatorChar);
            if (!File.Exists(file)) return;

            AdminTool.WriteSeparator();
            Console.WriteLine("Complete {1} of {0} ({2}):", ItemsCaption, WasInstalledBefore ? "upgrade" : "installation", file);
            Tool.ExecuteSqlScript(file, this);
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes a line containing information on the last change of this software item to the standard output.</summary>
        public void WriteChange() {
            Console.WriteLine("{0,-40} :  version {1,-10}({2})",
                Caption, Version == null ? "<unknown>" : Version, PreviousVersion == Version ? "no change" : WasInstalledBefore ? "upgraded from " + PreviousVersion : "installed now"
            );
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
            Load();
            if (!Exists) {
                tool.Execute(String.Format("INSERT INTO $MAIN$.module (name) VALUES ({0});", StringUtils.EscapeSql(Identifier)));
                SetId();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        private void Load() {
            IDataReader reader = Tool.GetQueryResult(String.Format("SELECT id, version, c_version FROM $MAIN$.module WHERE name={0};", StringUtils.EscapeSql(Identifier)));
            if (reader.Read()) {
                this.Id = reader.GetInt32(0);
                this.Version = (reader.GetValue(1) == DBNull.Value ? null : reader.GetString(1));
                this.CleanupVersion = (reader.GetValue(2) == DBNull.Value ? this.Version : reader.GetString(2));
                this.PreviousVersion = Version;
                this.WasInstalledBefore = this.IsInstalled;
            }
            reader.Close();
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void RegisterVersion() {
            try {
                Tool.Execute(String.Format("UPDATE $MAIN$.module SET version={1} WHERE name={0};", StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(Version)));
            } catch (Exception) {}
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void RegisterCleanupVersion() {
            try {
                Tool.Execute(String.Format("UPDATE $MAIN$.module SET c_version={1} WHERE name={0};", StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(CleanupVersion)));
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

        public CoreModule(AdminTool tool, string baseDir) : base(tool, "core", baseDir) {}

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
                IDataReader reader = Tool.GetQueryResult(String.Format("SELECT id, version, c_version FROM $MAIN$.service WHERE identifier={0};", StringUtils.EscapeSql(Identifier)));
                if (reader.Read()) {
                    this.Id = reader.GetInt32(0); 
                    this.Version = (reader.GetValue(1) == DBNull.Value ? null : reader.GetString(1));
                    this.CleanupVersion = (reader.GetValue(2) == DBNull.Value ? this.Version : reader.GetString(2));
                    this.PreviousVersion = Version;
                    this.WasInstalledBefore = this.IsInstalled;
                }
                reader.Close();
                if (!this.Exists) {
                    tool.Execute(String.Format("INSERT INTO $MAIN$.service (id_type, available, identifier, name, description, version) VALUES ({0}, true, {1}, {1}, {1}, {2});", ScriptBasedServiceTypeId, StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(Version)));
                    SetId();
                    tool.Execute(String.Format("INSERT INTO $MAIN$.scriptservice (id, root) VALUES ({0}, {1});", Id, StringUtils.EscapeSql("$(SERVICEROOT)/" + Identifier)));
                }
            } catch (Exception e) {
                Console.Error.WriteLine("ERROR: Could not add service: {0}", e.Message);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------

        public static void GetScriptBasedServiceTypeId(AdminTool tool) {
            ScriptBasedServiceTypeId = tool.GetQueryIntegerValue("SELECT id FROM type WHERE class = 'Terradue.Portal.ScriptBasedService, Terradue.Portal';");
        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void RegisterVersion() {
            try {
                Tool.Execute(String.Format("UPDATE $MAIN$.service SET version={1} WHERE identifier={0};", StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(Version)));
            } catch (Exception) {}

        }

        //---------------------------------------------------------------------------------------------------------------------

        public override void RegisterCleanupVersion() {
            try {
                Tool.Execute(String.Format("UPDATE $MAIN$.service SET c_version={1} WHERE name={0};", StringUtils.EscapeSql(Identifier), StringUtils.EscapeSql(CleanupVersion)));
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

}
