// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System.IO.Abstractions;
    using System.Reflection;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// The command <see cref="CommandInfo"/> shows debug information and system runtime.
    /// </summary>
    [Command(Name = "info", Description = "Show debug information of AzureAuth. Please provide when asking for help.")]
    internal class CommandInfo
    {
        /// <summary>
        /// This method executes the info process.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <returns>The error code: 0 is normal execution, and the rest means errors during execution.</returns>
        public int OnExecute(ILogger<CommandInfo> logger, IFileSystem fileSystem)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string azureauthVersion = assembly.GetName().Version.ToString();
            logger.LogInformation($"AzureAuth Version: {azureauthVersion}");

            string deviceID = TelemetryMachineIDHelper.GetRandomDeviceIDAsync(fileSystem).Result;
            logger.LogInformation($"Device ID: {deviceID}");

            logger.LogInformation($"Device Identifier File Location: {TelemetryMachineIDHelper.GetIdentifierLocation(fileSystem)}");
            logger.LogInformation($"To reset your device identifier, delete the file at {TelemetryMachineIDHelper.GetIdentifierLocation(fileSystem)}.");

            logger.LogInformation($"\nTo get the user's sid, use option --output=sid in normal authentication process.");

            return 0;
        }
    }
}
