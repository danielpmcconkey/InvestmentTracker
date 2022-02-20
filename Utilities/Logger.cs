using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace Utilities
{
    public enum LogLevel
    {
        DEBUG,
        INFO,
        WARN,
        ERROR,
        FATAL,
    }
    public static class Logger
    {
        private static ILog _logger;
        private static string _logName;



        static Logger()
        {

        }

        public static void initialize(string logName)
        {
            XmlConfigurator.Configure();
            _logger = LogManager.GetLogger(logName);
            _logName = logName;
        }

        private static void log(LogLevel level, string message, Exception ex = null)
        {

            switch (level)
            {
                case LogLevel.DEBUG:
                    _logger.Debug(message);
                    break;
                case LogLevel.INFO:
                    _logger.Info(message);
                    break;
                case LogLevel.WARN:
                    _logger.Warn(message);
                    break;
                case LogLevel.ERROR:
                    if (ex == null) _logger.Error(message);
                    _logger.Error(message, ex);
                    break;
                case LogLevel.FATAL:
                    if (ex == null) _logger.Fatal(message);
                    _logger.Fatal(message, ex);
                    break;
            }

        }
        public static void debug(string message, Exception ex = null)
        {
            log(LogLevel.DEBUG, message, ex);
        }
        public static void info(string message, Exception ex = null)
        {
            log(LogLevel.INFO, message, ex);
        }
        public static void warn(string message, Exception ex = null)
        {
            log(LogLevel.WARN, message, ex);
        }
        public static void error(string message, Exception ex = null)
        {
            log(LogLevel.ERROR, message, ex);
        }
        public static void fatal(string message, Exception ex = null)
        {
#if !DEBUG
            string subject = String.Format("Error in {0}", _logName);
            string body = message + Environment.NewLine;
            if(ex != null)
            {
                body += ex.Message + Environment.NewLine;
                body += ex.StackTrace;
            }
            Emailer.sendEmail_async(subject, body); 
#endif
            log(LogLevel.FATAL, message, ex);
        }


    }
}
