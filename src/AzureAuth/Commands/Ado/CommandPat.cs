// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands.Ado
{
    using McMaster.Extensions.CommandLineUtils;

    using Microsoft.Applications.Events;
    using Microsoft.Extensions.Logging;

    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// Command class for creating or fetching, and returning Azure Devops PATs.
    /// </summary>
    [Command("pat", Description = "Create and locally cache Azure Devops Personal Access Tokens (PATs) using encrypted local storage.")]
    public class CommandPat
    {
        /// <summary>
        /// Executes the command and returns a status code indicating the success or failure of the execution.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{T}"/> instance that is used for logging.</param>
        /// <param name="eventData">Lasso injected command event data.</param>
        /// <returns>An integer status code. 0 for success and non-zero for failure.</returns>
        public int OnExecute(ILogger<CommandPat> logger, CommandExecuteEventData eventData)
        {
            logger.LogInformation("coming soon");
            return 0;
        }
    }
}
