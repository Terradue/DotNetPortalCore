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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Xsl;
using MySql.Data.MySqlClient;
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

    

    /// <summary>
    /// Ify local context.
    /// </summary>
    /// \ingroup core_Context
    public class IfyLocalContext : IfyContext {

        private int logLevel = -1, debugLevel = -1;
        private string baseUrl;
        
        //---------------------------------------------------------------------------------------------------------------------

        public override int LogLevel {
            get {
                if (logLevel == -1) {
                    if (Application == null) {
                        logLevel = GetConfigIntegerValue("AgentLogLevel");
                        debugLevel = (logLevel <= 3 ? 0 : logLevel - 3);
                    } else {
                        logLevel = Application.LogLevel;
                        if (logLevel < 3 && debugLevel != -1) debugLevel = 0;
                    }
                }
                return logLevel;
            }
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

        //---------------------------------------------------------------------------------------------------------------------*/

        public override int DebugLevel {
            get {
                if (debugLevel == -1) {
                    if (Application == null) {
                        logLevel = GetConfigIntegerValue("AgentLogLevel");
                        debugLevel = (logLevel <= 3 ? 0 : logLevel - 3);
                    } else {
                        debugLevel = Application.DebugLevel;
                        //AddWarning("DEBUGLEVEL(A) " + debugLevel);
                        if (debugLevel < 0) debugLevel = 0;
                        else if (debugLevel > 0) logLevel = 3;
                    }
                }
                return debugLevel;
            }
            set {
                debugLevel = (value < 0 ? 0 : value);
                if (value > 0) logLevel = 3;
            }
        }
        
        //---------------------------------------------------------------------------------------------------------------------*/

        /// <summary>Creates a new IfyLocalContext instance for client applications, such as web services</summary>
        /*!
        /// <param name="the">name of the client application</param>
        */
        public IfyLocalContext(string connectionString, bool console) : base(connectionString) {
            this.console = console;
        }
        
        //---------------------------------------------------------------------------------------------------------------------*/

        /// <summary>Creates a new IfyLocalContext instance for client applications, such as web services</summary>
        /*!
        /// <param name="the">name of the client application</param>
        */
        public IfyLocalContext(string connectionString, string baseUrl, string applicationName) : base(connectionString) {
            this.baseUrl = baseUrl;
            this.ApplicationName = applicationName;
            this.BufferedLogging = true;
        }
        
        //---------------------------------------------------------------------------------------------------------------------*/

        public override void Open() {
            base.Open();
            string username = GetConfigValue("AgentUser");
            if (username != null) UserId = GetQueryIntegerValue(String.Format("SELECT id FROM usr WHERE username={0};", StringUtils.EscapeSql(username)));
            UserLevel = Terradue.Portal.UserLevel.Administrator;
            RestrictedMode = false;
            //AdminMode = true;
            if (Application == null) {
                logLevel = GetConfigIntegerValue("AgentLogLevel");
                if (logLevel < 0) logLevel = 0;
                debugLevel = (logLevel > 3 ? logLevel - 3 : 0);
            }
            
            if (BaseUrl == null) BaseUrl = this.baseUrl;
        }

        //---------------------------------------------------------------------------------------------------------------------*/

        /*public override void LoadConfiguration() {
            base.LoadConfiguration();
            HostUrl = BaseUrl;
        }*/

    }
    
}


