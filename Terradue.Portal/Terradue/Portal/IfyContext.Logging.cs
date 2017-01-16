using System;
using System.Web;
using log4net;
using log4net.Config;

namespace Terradue.Portal {

    public abstract partial class IfyContext {

        private static ILog log;// = LogManager.GetLogger(typeof(IfyContext));
        private static bool isLogActive;
        private static readonly log4net.Core.Level statLevel = new log4net.Core.Level(50000, "STAT"); // the first and second values must be unique values
        //---------------------------------------------------------------------------------------------------------------------

        /// <summary>Configures the logging object.</summary>
        /// \ingroup Context
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void CreateLogger() {
            log = LogManager.GetLogger(this.GetType().FullName);
            // adding a new log4net level (statistical level)
            LogManager.GetRepository().LevelMap.Add(statLevel);
            this.LoadLogConfig();
        }

        /// <summary>Create a new TerradueLog instance reading configuration from the default file </summary>
        public void LoadLogConfig() {
            try {
                System.Configuration.Configuration rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(null);
                System.IO.FileInfo fi = new System.IO.FileInfo(rootWebConfig.AppSettings.Settings["TerradueLogConfigurationFile"].Value);
                XmlConfigurator.Configure(fi);
                isLogActive = true;
            } catch (Exception) {
                isLogActive = false;
            }

        }

        /// <summary>Create a new TerradueLog instance reading configuration from a specfied file </summary>
        public void LoadLogConfig(string terradueConfigurationFilePath) {
            try {
                XmlConfigurator.Configure(new System.IO.FileInfo(terradueConfigurationFilePath));
                isLogActive = true;
            } catch (Exception) {
                isLogActive = false;
            }
        }

        /// <summary>add a custom property</summary>
        private void SetUserId() {
            log4net.GlobalContext.Properties["user"] = this.Username;
        }
        /// <summary>add a custom property</summary>
        public void SetUrl(string url) {
            /*
             * url (always a string) can be:
             * url: user authorization
             * search url : search request
             * dar url: data access
             * standing order url: standing order
             * download manager identifier: download manager installation
            */
            log4net.GlobalContext.Properties["url"] = url;
        }
        /// <summary>add a custom property</summary>
        public void SetAction(string action) {
            /*
             * action can be:
             * status (string): user authorization/data access/ standig order
             * action (string): download manager installation
             * # of result (int): search request 
            */
            log4net.GlobalContext.Properties["action"] = action;
        }

        /// <summary>add a custom property</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        private void SetReporter(string reporter) {
            log4net.GlobalContext.Properties["reporter"] = reporter;
        }

        /// <summary>add a custom property</summary>
        public void SetParameters(string parameters) {
            log4net.ThreadContext.Properties["parameters"] = parameters;
        }

        /// <summary>add a custom property</summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        private void SetOriginator(string originator) {
            log4net.ThreadContext.Properties["originator"] = originator;
        }

        /// <summary> Loging Debug level </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void LogDebug(object reporter, string message) {
            SetReporter(reporter.GetType().ToString());
            if (isLogActive) log.Debug(message);
        }

        /// <summary> Loging Debug level </summary>
        public virtual void LogDebug(object reporter, string message, Exception exception) {
            SetReporter(reporter.GetType().ToString());
            if (isLogActive) log.Debug(message,exception);
        }


        /// <summary> Loging Debug level </summary>
        public virtual void LogDebug(object reporter, string format, params object[] args) {
            SetReporter(reporter.GetType().ToString());
            if (isLogActive) log.DebugFormat(format,args);
        }


        /// <summary> Loging Error level </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void LogError(object reporter, string message) {
            SetReporter(reporter.GetType().ToString());
            if (isLogActive) log.Error(message);
        }

        /// <summary> Loging Error level </summary>
        public virtual void LogError(object reporter, string message, Exception exception) {
            SetReporter(reporter.GetType().ToString());
            if (isLogActive) log.Error(message,exception);
        }

        /// <summary> Loging Error level </summary>
        public virtual void LogError(object reporter, string format, params object[] args) {
            LogError(reporter.GetType(), format, args);
        }

        /// <summary> Loging Error level </summary>
        public virtual void LogError(Type reporter, string format, params object[] args) {
            SetReporter(reporter.ToString());
            if (isLogActive) log.ErrorFormat(format,args);
        }

        public virtual void LogHttpError(object reporter, HttpContext httpContext, string format, params object[] args) {
            SetOriginator(httpContext.Request.UserHostAddress);
            LogError(reporter, string.Format("[{0}]", httpContext.Request.RequestType) + string.Format(format, args));
        }

        /// <summary> Loging Fatal level </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void LogFatalError(object reporter, string message) {
            SetReporter(reporter.GetType().ToString());
            if (isLogActive) log.Fatal(message);
        }

        /// <summary> Loging Fatal level </summary>
        public virtual void LogFatalError(object reporter, string message, Exception exception) {
            SetReporter(reporter.GetType().ToString());
            if (isLogActive) log.Fatal(message,exception);
        }


        /// <summary> Loging Fatal level </summary>
        public virtual void LogFatalError(object reporter, string format, params object[] args) {
            LogFatalError(reporter.GetType(), format, args);
        }


        /// <summary> Loging Info level </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void LogInfo(object reporter, string message) {
            SetReporter(reporter.GetType().ToString());
            if (isLogActive) log.Info(message);
        }

        /// <summary> Loging Info level </summary>
        public virtual void LogInfo(object reporter, string format, params object[] args) {
            SetReporter(reporter.GetType().ToString());
            if (isLogActive) log.InfoFormat(format,args);
        }

        public virtual void LogHttpInfo(object reporter, string message, HttpContext httpContext) {
            SetOriginator(httpContext.Request.UserHostAddress);
            LogInfo(reporter, message);
        }

        /// <summary> Loging Warning level </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void LogWarning(object reporter, string message) {
            SetReporter(reporter.GetType().ToString());
            if (isLogActive) log.Warn(message);
        }

        /// <summary> Loging Warning level </summary>
        public virtual void LogWarning(object reporter, string message, Exception exception) {
            SetReporter(reporter.GetType().ToString());
            if (isLogActive) log.Warn(message,exception);
        }


        /// <summary> Loging Warning level </summary>
        public virtual void LogWarning(object reporter, string format, params object[] args) {
            LogWarning(reporter.GetType(), format, args);
        }

        /// <summary> Loging Warning level </summary>
        public virtual void LogWarning(Type reporter, string format, params object[] args) {
            SetReporter(reporter.ToString());
            if (isLogActive) log.WarnFormat(format,args);
        }


        /// <summary> Loging Statistical level </summary>
        /// \xrefitem rmodp "RM-ODP" "RM-ODP Documentation"
        public virtual void LogStat(object reporter) {
            SetReporter(reporter.GetType().ToString());
            SetUserId();
            //SetAction(action);
            //SetUrl(HttpContext.Current.Request.Url.AbsoluteUri);
            log.Logger.Log(this.GetType(), statLevel, "Used for statistical extraction", null);
        }
    }
}
