// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.TestHelper
{
    using NLog;
    using NLog.Config;
    using NLog.Targets;

    using System.Diagnostics;

    public static class MemoryLogger
    {
        public static (Extensions.Logging.ILogger logger, MemoryTarget logTarget) Create()
        {
            // Use reflection to get the name of the class which declares our caller.
            // Expected to be the name of a Test class.
            StackFrame frame = new StackFrame(1);
            var method = frame.GetMethod();
            var type = method?.DeclaringType;

            var loggerFactory = new NLog.Extensions.Logging.NLogLoggerFactory();
            var logger = loggerFactory.CreateLogger(type?.FullName);

            // Setup in memory logging target with NLog
            var loggingConfig = new LoggingConfiguration();
            var logTarget = new MemoryTarget("memory_target");
            logTarget.Layout = "${message}";
            loggingConfig.AddTarget(logTarget);
            loggingConfig.AddRuleForAllLevels(logTarget);

            // Set the Config
            LogManager.Configuration = loggingConfig;

            return (logger, logTarget);
        }
    }
}
