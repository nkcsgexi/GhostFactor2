using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace warnings.util
{
    public class NLoggerUtil
    {
        /* The path to the desktop. */
        private static string desktopPath = @"C:\Users\xige\Desktop\";

        private static bool toDesktop = true;

        /* Configure the logger programmatically. */
        static NLoggerUtil()
        {
            var config = new LoggingConfiguration();
            string path = "GhostFactor.log";
            if (toDesktop)
                path = desktopPath + path;

            var target = new FileTarget
            {
                FileName = path
            };

            // Add a file target where all the log shall go to.
            config.AddTarget("file", target);

            // Add a rule that any information higher than debug goes to the file.
            var rule = new LoggingRule("*", LogLevel.Debug, target);
            config.LoggingRules.Add(rule);

            // Also log fatal logs to another file.
            string fatalPath = "GhostFactorFatal.log";
            if (toDesktop)
                fatalPath = desktopPath + fatalPath;
            var fatalTarget = new FileTarget()
            {
                FileName = fatalPath
            };
            var fatalRule = new LoggingRule("*", LogLevel.Fatal, fatalTarget);
            config.LoggingRules.Add(fatalRule);

            LogManager.Configuration = config;
        }

        /* get a logger using the specified RefactoringType. */
        static public Logger GetNLogger(Type t)
        {
            return LogManager.GetLogger(t.FullName);
        }
    }

    public interface ILoggerKeeper
    {
        Logger GetLogger();
    }
}
