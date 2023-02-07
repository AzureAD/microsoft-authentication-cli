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
    [Command(Name = "info", Description = "Shows AzureAuth debug information. Please provide when asking for help.")]
    public class CommandInfo
    {
        private const string OptionResetDeviceID = "--reset-device-id";
        private readonly ILogger<CommandInfo> logger;
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandInfo"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        public CommandInfo(ILogger<CommandInfo> logger, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Gets or sets a value indicating whether reset device id.
        /// </summary>
        [Option(OptionResetDeviceID, "Reset Device ID", CommandOptionType.NoValue)]
        public bool ResetDeviceID { get; set; }

        /// <summary>
        /// This method executes the info process.
        /// </summary>
        /// <returns>The error code: 0 is normal execution, and the rest means errors during execution.</returns>
        public int OnExecute()
        {
            if (this.ResetDeviceID)
            {
                return this.ExecuteResetDeviceID();
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            string azureauthVersion = assembly.GetName().Version.ToString();
            string deviceID = TelemetryDeviceID.GetAsync(this.fileSystem).Result;

            this.logger.LogInformation(
                $"AzureAuth Version: {azureauthVersion} \n" +
                $"Device ID: {deviceID} \n" +
                $"To reset your device identifier, Run `azureauth info {OptionResetDeviceID}` \n");

            return 0;
        }

        /// <summary>
        /// Reset the current device ID.
        /// </summary>
        /// <returns>The error code: 0 is normal execution, and the rest means errors during execution.</returns>
        public int ExecuteResetDeviceID()
        {
            TelemetryDeviceID.Delete(this.fileSystem);
            this.logger.LogInformation($"Device ID was reset.");

            return 0;
        }
    }
}
