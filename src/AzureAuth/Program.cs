// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.Text;

    using McMaster.Extensions.CommandLineUtils;

    using Microsoft.Authentication.AzureAuth.Commands;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// The start up program.
    /// </summary>
    public class Program
    {
        private static void Main(string[] args)
        {
            // Use UTF-8 output encoding.
            // This will impact the NLog Console Target as well as any other Console usage.
            Console.OutputEncoding = Encoding.UTF8;

            CommandLineApplication app = new CommandLineApplication<CommandAzureAuth>();

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

            var envVarsToCollect = new[]
            {
                Ado.Constants.SystemDefinitionId,
                EnvVars.CloudBuild,
                EnvVars.NoUser,
                EnvVars.CorextNonInteractive,
            };

            TelemetryConfig telemetryConfig = new TelemetryConfig(
                eventNamespace: "azureauth",
                backend: backend,
                ingestionToken: ingestionToken,
                useAsync: true,
                envVarsToCollect: envVarsToCollect,
                hideAlias: true,
                hideMachineName: true);

            // We want redirect stdout to get just token output
            // while warnings and errors still go to stderr to be seen by a user.
            var stdErrLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning;

            LassoOptions options = new LassoOptions(
                telemetryConfig,
                sendExecuteEvent: false,
                sendCommandEvents: true,
                minStderrLoglevel: stdErrLogLevel);

            var loggerFactory = new NLog.Extensions.Logging.NLogLoggerFactory();
            var logger = loggerFactory.CreateLogger("AzureAuth");

            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IMsalWrapper, MsalWrapper>();
            services.AddSingleton<IPublicClientAuth, PublicClientAuth>();
            services.AddSingleton<ILogger>(logger);

            new Lasso(app, options, services).Execute(args);
        }
    }
}
