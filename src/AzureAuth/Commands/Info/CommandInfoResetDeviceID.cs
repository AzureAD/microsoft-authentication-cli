// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands.Info
{
    using System.IO.Abstractions;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Extensions;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// The command <see cref="CommandResetDeviceID"/> reset the device ID in local storage.
    /// </summary>
    [Command(Name = "reset-device-id", Description = "Reset your AzureAuth telemetry device identifier.")]
    public class CommandResetDeviceID
    {
        /// <summary>
        /// This method executes the reset device ID process.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <returns>The error code: 0 is normal execution, and the rest means errors during execution.</returns>
        public int OnExecute(ILogger<CommandResetDeviceID> logger, IFileSystem fileSystem)
        {
            TelemetryDeviceID.Delete(fileSystem);
            logger.LogSuccess($"Telemetry Device ID was reset.");

            return 0;
        }
    }
}
