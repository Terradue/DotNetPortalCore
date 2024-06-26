using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
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

    

    /// <summary>Provides the core functionality of the background agent.</summary>
    public class ActionLoop {
    
        private IfyLocalContext context;
        private AutoResetEvent autoEvent;
        private Timer timer;
        private IDataReader dbReader;

        // private string databaseType = "mysql";
        private string className;
        private string connectionString;
        private int interval = 30;
    
        private bool console, log;
        private bool running;
        private bool startup = true;

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Initializes a new instance of the <see cref="Terradue.Portal.ActionLoop"/> class.</summary>
        /// <param name="console">Determines whether the output goes to the console (if <c>true</c>) or a log (otherwise).</param>
        public ActionLoop(bool console) {
            this.console = console;
            this.log = !console;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Initiates the background agent's action loop.</summary>
        public void Process() {
            try {
                Open();
                if (connectionString == null) throw new Exception("Could not connect to database");
                autoEvent.WaitOne();
            } catch (Exception e) {
                if (context == null) {
                    Console.Error.WriteLine("FATAL ERROR: " + e.Message + " " + e.StackTrace);
                    Console.Error.WriteLine("Execution terminated because of fatal error (see above)");
                } else {
                    context.WriteError("FATAL ERROR: " + e.Message + " " + e.StackTrace);
                    context.WriteError("Execution terminated because of fatal error (see above)");
                }
                Close();
                Environment.ExitCode = 1;
                return;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Opens the execution environment context and other resources required by the background agent.</summary>
        /// <remarks>The <see cref="Terradue.Portal.IfyLocalContext"/> subclass and the database connection string are obtained from the web application's web.config file.</remarks>
        public void Open() {
            // Configuration file is ../web.config
            string binDir;
            DirectoryInfo dirInfo = new DirectoryInfo(Environment.GetCommandLineArgs()[0]);
            binDir = Regex.Replace(dirInfo.FullName, @"[^\\/]*$", "");
            string configFile = Regex.Replace(binDir, @"([\\/])bin[\\/]$", @"$1web.config");
            if (!File.Exists(configFile)) throw new Exception("File not found: " + configFile);
            XmlDocument configDoc = new XmlDocument();
            configDoc.Load(configFile);

            XmlElement key;
            key = configDoc.SelectSingleNode("/configuration/appSettings/add[@key='LocalContextClass']") as XmlElement;
            if (key != null && key.HasAttribute("value")) className = key.Attributes["value"].Value;

            key = configDoc.SelectSingleNode("/configuration/appSettings/add[@key='DatabaseConnection']") as XmlElement;
            if (key != null && key.HasAttribute("value")) connectionString = key.Attributes["value"].Value;
            else throw new Exception("No connection string found in web.config");
            
            context = IfyContext.GetLocalContext(className, connectionString, console);
            context.Open();
            context.AccessLevel = EntityAccessLevel.Administrator;
            autoEvent = new AutoResetEvent(false);
            timer = new Timer(new TimerCallback(OnTimer), autoEvent, 0, 1000 * interval);
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Stops the background agent.</summary>
        public void End() {
            Close();
            autoEvent.Set();
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Stops the background agent.</summary>
        /// <param name="message">A message to be written before stopping the agent.</param>
        public void End(string message) {
            context.WriteSeparator();
            context.WriteInfo(message);
            End();            
        }

        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Closes the execution environment context and other resources no longer required by the background agent.</summary>
        public void Close() {
            if (autoEvent != null) autoEvent.Close();
            if (timer != null) timer.Dispose();
            if (context != null) {
                context.WriteSeparator();
                context.Close();
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Executes the agent actions that are due in the current execution run.</summary>
        /// <param name="state">State.</param>
        protected void OnTimer(object state) {
            if (running) return;
    
            running = true;
            DateTime now = context.RefreshNow();
            if (log && context.GetLogFilenameFromConfig("AgentLogFile", now)) WriteStartupInfo();
            SetInterval();
            if (startup) WriteStartupInfo();
            IDbConnection dbConnection = context.GetDbConnection();
            try {
                // Force the reload of the configuration if it was changed (by another process, such as the web server component)
                if (context.GetConfigBooleanValue("ForceReload")) {
                    context.LoadConfiguration();
                    context.SetConfigValue("ForceReload", false);
                }

                List<ActionExecution> executions = new List<ActionExecution>();
                dbReader = context.GetQueryResult(
                        String.Format(
                                "SELECT t.id, t.identifier, t.name, t.class, t.method, t.time_interval, t.next_execution_time, t.immediate FROM action AS t WHERE t.immediate OR (t.enabled AND (t.next_execution_time IS NULL OR t.next_execution_time<='{0}')) ORDER BY t.immediate DESC, pos;",
                                now.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\.fff")
                        ),
                        dbConnection
                );
                while (dbReader.Read()) {
                    DateTime nextExecutionTime = DateTime.MinValue;
                    // Workaround for MySQL error of 0000-00-00 dates that appear as next_execution_time out of nowhere
                    try {
                        if (dbReader.GetValue(6) == DBNull.Value) nextExecutionTime = dbReader.GetDateTime(6);
                    } catch (Exception e) {
                        context.AddWarning("Error converting next execution time in DB: {0}", e.Message);
                    }
                    ActionExecution exec = new ActionExecution(
                            dbReader.GetInt32(0),
                            dbReader.GetString(1),
                            dbReader.GetString(2),
                            dbReader.GetString(3),
                            dbReader.GetString(4),
                            dbReader.GetValue(5) == DBNull.Value ? 300 : StringUtils.StringToSeconds(dbReader.GetString(5)),
                            nextExecutionTime,
                            dbReader.GetValue(7) != DBNull.Value && dbReader.GetBoolean(7)
                    );
                            
                    executions.Add(exec);
                }
                context.CloseQueryResult(dbReader, dbConnection);

                foreach (ActionExecution exec in executions) {
                    try {
                        context.WriteInfo(
                                String.Format("Executing \"{0}\"{1}",
                                        exec.Name, 
                                        exec.Immediate ? " (immediate execution requested)" : String.Empty
                                )
                        );
                        bool result = context.ExecuteAgentActionMethod(exec.ClassName, exec.MethodName);
                        //if (!result) result = context.ExecuteAgentAction(dbReader.GetString(1));
                        if (!result) context.WriteError("No action handler found.");
                        exec.NextExecutionTime = (exec.NextExecutionTime == DateTime.MinValue ? now.AddSeconds(exec.Interval) : exec.NextExecutionTime.AddSeconds((Math.Floor((now - exec.NextExecutionTime).TotalSeconds / exec.Interval) + 1) * exec.Interval));
                        context.Execute(String.Format("UPDATE action SET next_execution_time=CASE WHEN enabled THEN '{1}' ELSE next_execution_time END, immediate=false WHERE id={0};", exec.Id, exec.NextExecutionTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss")));
                        context.WriteInfo(String.Format("Next planned execution: {0}", exec.NextExecutionTime.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\Z")));
                        context.WriteSeparator();
                    } catch (Exception e) {
                        context.WriteError(String.Format("{0} - {1}", e.Message, e.StackTrace));
                        if (e.InnerException != null) context.WriteError(String.Format("{0} - {1}", e.InnerException.Message, e.InnerException.StackTrace));
                        context.WriteSeparator();
                    }
                }
    
            } catch (Exception e) {
                context.WriteError(e.Message + " " + e.StackTrace);
                context.WriteError(String.Format("{0} - {1}", e.Message, e.StackTrace));
                if (e.InnerException != null) context.WriteError(String.Format("Caused by: {0} - {1}", e.InnerException.Message, e.InnerException.StackTrace));
                context.WriteSeparator();
                if (dbReader != null && !dbReader.IsClosed) dbReader.Close();
                dbConnection.Close();
                running = false;
            }
            running = false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Writes an initial overview about agent actions and their execution schedule to the console or the log.</summary>
        protected void WriteStartupInfo() {
            IDbConnection dbConnection = context.GetDbConnection();
            try {
                // Enabled actions ordered by:
                // 1. next execution time in the past (i.e overdue),
                // 2. without next execution time (i.e. also immediately),
                // 3. with next execution time in the future
                dbReader = context.GetQueryResult(
                        String.Format(
                                "SELECT t.name, t.next_execution_time, t.enabled, t.next_execution_time IS NULL OR t.next_execution_time<='{0}' OR t.immediate FROM action AS t ORDER BY t.enabled OR immediate DESC, immediate DESC, next_execution_time>'{0}' OR next_execution_time IS NULL OR immediate, pos, next_execution_time;",
                                context.Now.ToString(@"yyyy\-MM\-dd\THH\:mm\:ss\.fff")
                        ),
                        dbConnection
                );
                        
                if (startup) {
                    if (console) context.WriteInfo("Execution started");
                    else context.WriteInfo("Service started");
                }
                context.WriteInfo("Next action executions:");
                while (dbReader.Read()) {
                    bool enabled = dbReader.GetValue(2) != DBNull.Value && dbReader.GetBoolean(2);
                    context.WriteInfo((enabled ? "+ " : "- ") + (dbReader.GetString(0) + ": ").PadRight(36) + 
                            (enabled ? (dbReader.GetBoolean(3) ? "immediately" : dbReader.GetDateTime(1).ToString(@"yyyy\-MM\-dd HH\:mm\:ss")) : "disabled")
                    );
                }
                context.CloseQueryResult(dbReader, dbConnection);
                context.WriteSeparator();
                if (startup) {
                    context.WriteInfo("Setting timer interval to " + interval + " seconds");
                    context.WriteSeparator();
                }
    
            } catch (Exception e) {
                context.WriteError(e.Message);
                if (dbReader != null && !dbReader.IsClosed) dbReader.Close();
                dbConnection.Close();
            }
            startup = false;
        }
        
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Resets the interval to the value configured in the database if changed.</summary>
        protected void SetInterval() {
            int newInterval = context.GetQueryIntegerValue("SELECT value FROM config WHERE name='AgentInterval';");
            if (newInterval > 0 && (startup || newInterval != interval)) {
                if (!startup) {
                    context.WriteInfo("Changing timer interval to " + newInterval + " seconds");
                    context.WriteSeparator();
                }
                timer.Change(1000 * newInterval, 1000 * newInterval);
                interval = newInterval;
            }
        }
        
    }
    




    //-----------------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------------





    /// <summary>Helper class representing an agent action and its execution settings.</summary>
    public class ActionExecution {

        /// <summary>Gets or sets the action's database ID.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the action's unique identifier.</summary>
        public string Identifier { get; set; }

        /// <summary>Gets or sets the action's human-readable name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the fully qualified name of the class containing the action's execution method.</summary>
        public string ClassName { get; set; }

        /// <summary>Gets or sets the name of the method for the execution of the action.</summary>
        public string MethodName { get; set; }

        /// <summary>Gets or sets the action's execution interval in seconds.</summary>
        public int Interval { get; set; }

        /// <summary>Gets or sets the action's next execution time.</summary>
        public DateTime NextExecutionTime { get; set; }

        /// <summary>Indicates or decides whether the action is executed immediately with the next cycle of the agent.</summary>
        public bool Immediate { get; set; }

        /// <summary>Creats a new instance of an ActionExecution.</summary>
        /// <param name="id">The action's database ID.</param>
        /// <param name="identifier">The action's unique identifier.</param>
        /// <param name="name">The action's human-readable name.</param>
        /// <param name="className">The fully qualified name of the class containing the action's execution method.</param>
        /// <param name="methodName">The name of the method for the execution of the action.</param>
        /// <param name="interval">The action's execution interval in seconds.</param>
        /// <param name="nextExecutionTime">The action's next execution time.</param>
        /// <param name="immediate">Whether or not the action is executed immediately with the next cycle of the agent.</param>
        public ActionExecution(int id, string identifier, string name, string className, string methodName, int interval, DateTime nextExecutionTime, bool immediate) {
            this.Id = id;
            this.Identifier = identifier;
            this.Name = name;
            this.ClassName = className;
            this.MethodName = methodName;
            this.Interval = interval;
            this.NextExecutionTime = nextExecutionTime;
            this.Immediate = immediate;
        }
    }

}

