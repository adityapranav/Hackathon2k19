namespace onenotelink
{
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class LogProvider
    {
        public static Logger logger = null; 

        public static Logger getLogInstance()
        {
            if (logger == null)
            {
                createLoggingConfiguration();
                logger = NLog.LogManager.GetCurrentClassLogger();
            }

            return logger;
        }

        private static void createLoggingConfiguration()
        {
            var config = new NLog.Config.LoggingConfiguration();
            var time   = DateTime.Now.ToString("yyyyMMddHHmmss");
            // Targets where to log to: File and Console
            var debugLogFile = new NLog.Targets.FileTarget("debugLogFile") { FileName = $"brokenLogFixer_Debug_{time}.LOG" };
            var logFile = new NLog.Targets.FileTarget("logfile") { FileName = $"brokenLogFixer_{time}.LOG" };

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logFile);
            config.AddRule(LogLevel.Trace, LogLevel.Debug, debugLogFile);

            // Apply config           
            NLog.LogManager.Configuration = config;
        }

        private LogProvider()
        {

        }
    }
}
