// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.TestHelper
{
    using Microsoft.Extensions.Logging;

    using NLog;
    using NLog.Config;
    using NLog.Targets;

    public static class MemoryLogger
    {
        public static (ILogger<T> logger, MemoryTarget logTarget) Create<T>()
        {
            var loggerFactory = new NLog.Extensions.Logging.NLogLoggerFactory();
            var logger = loggerFactory.CreateLogger<T>();

            // Setup in memory logging target with NLog - allows making assertions against what has been logged.
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
