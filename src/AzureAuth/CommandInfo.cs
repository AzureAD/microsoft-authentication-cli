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
        private const string OptionResetDeviceID = "--reset-device-id";

        /// <summary>
        /// Gets or sets a value indicating whether reset device id.
        /// </summary>
        [Option(OptionResetDeviceID, "Reset Device ID", CommandOptionType.NoValue)]
        public bool ResetDeviceID { get; set; }

        /// <summary>
        /// This method executes the info process.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <returns>The error code: 0 is normal execution, and the rest means errors during execution.</returns>
        public int OnExecute(ILogger<CommandInfo> logger, IFileSystem fileSystem)
        {
            if (this.ResetDeviceID)
            {
                return this.ResetID(logger, fileSystem);
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            string azureauthVersion = assembly.GetName().Version.ToString();
            string deviceID = TelemetryMachineIDHelper.GetRandomDeviceIDAsync(fileSystem).Result;
            string deviceIDLocation = TelemetryMachineIDHelper.GetIdentifierLocation(fileSystem);

            logger.LogInformation(
                $"AzureAuth Version: {azureauthVersion} \n" +
                $"Device ID: {deviceID} \n" +
                $"Device ID Path: {deviceIDLocation} \n" +
                $"To reset your device identifier, Run `azureauth info {OptionResetDeviceID}` \n" +
                $"\n" +
                $"To get the user's sid, use option --output=sid. For example:\n" +
                $"azureauth --client <client> --scope <scope> --tenant <tenant> --output sid");

            return 0;
        }

        private int ResetID(ILogger<CommandInfo> logger, IFileSystem fileSystem)
        {
            fileSystem.File.Delete(TelemetryMachineIDHelper.GetIdentifierLocation(fileSystem));
            logger.LogInformation($"Device ID was reset.");

            return 0;
        }
    }
}
