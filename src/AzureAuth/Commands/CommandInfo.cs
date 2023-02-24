// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands
{
    using System.IO.Abstractions;
    using System.Reflection;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Authentication.AzureAuth.Commands.Info;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// The command <see cref="CommandInfo"/> shows debug information and system runtime.
    /// </summary>
    [Command(Name = "info", Description = "Shows AzureAuth debug information. Please provide when asking for help.")]
    [Subcommand(typeof(CommandResetDeviceID))]
    public class CommandInfo
    {
        /// <summary>
        /// This method executes the info process.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="telemetryService">The telemetry service.</param>
        /// <returns>The error code: 0 is normal execution, and the rest means errors during execution.</returns>
        public int OnExecute(ILogger<CommandInfo> logger, ITelemetryService telemetryService)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string azureauthVersion = assembly.GetName().Version.ToString();
            string deviceID = (telemetryService as TelemetryServiceBase)?.MachineGUID;

            logger.LogInformation($"AzureAuth Version: {azureauthVersion}");
            logger.LogInformation($"Telemetry Device ID: {deviceID}");
            logger.LogInformation(string.Empty);

            logger.LogInformation("To reset your device identifier run the following command:");
            logger.LogInformation("  azureauth info reset-device-id");

            return 0;
        }
    }
}
