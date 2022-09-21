// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Lasso;
    using Microsoft.Lasso.Telemetry;

    /// <summary>
    /// The start up program.
    /// </summary>
    public class Program
    {
        private static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication<CommandMain>();

            // We always instantiate and depend on telemetry services, but these defaults
            // mean telemetry is effectively disabled by using a nonsensical ingest token
            // and setting our output to standard out. Nothing will be sent remotely.
            string ingestionToken = "Not a real ingestion token, but Lasso requires this be non-empty or dependency injection will break.";
            TelemetryOutput backend = TelemetryOutput.StandardOut;

            // We will only send telemetry if we are given an ingestion token via
            // environment variable. We *have* to do this here, rather than in
            // `CommandMain.OnExecute`, because Lasso doesn't have a way of allowing a
            // `CommandLineApplication` to dynamically set telemetry configuration. Even if
            // it did, we can't guarantee that a failure in command line parsing wouldn't
            // trigger telemetry before we ever get to disable it.
            //
            // To disable telemetry a user need only leave this environment variable unset. It's off by default.
            string applicationInsightsIngestionToken = Environment.GetEnvironmentVariable(EnvVars.ApplicationInsightsIngestionTokenEnvVar);
            if (!string.IsNullOrEmpty(applicationInsightsIngestionToken))
            {
                ingestionToken = applicationInsightsIngestionToken;
                backend = TelemetryOutput.ApplicationInsights;
            }

            TelemetryConfig telemetryConfig = new TelemetryConfig()
            {
                Backend = backend,
                IngestionToken = ingestionToken,
                Async = true,
            };

            // We want redirect stdout to get just token output
            // while warnings and errors still go to stderr to be seen by a user.
            var stdErrLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning;

            LassoOptions options = new LassoOptions(
                telemetryConfig,
                sendExecuteEvent: false,
                sendCommandEvents: true,
                minStderrLoglevel: stdErrLogLevel);

            new Lasso(app, options).Execute(args);
        }
    }
}
