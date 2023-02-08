// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands.Ado
{
    using McMaster.Extensions.CommandLineUtils;

    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// ADO Command for using either an ADO PAT or acquiring an AAD Access Token.
    /// </summary>
    [Command("token", Description = @"Get a PAT from env vars or an AAD AT for Azure Devops.
For use by short-lived processes. More info at https://aka.ms/AzureAuth")]
    public class CommandToken
    {
        /// <summary>
        /// Executes the command and returns a status code indicating the success or failure of the execution.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{T}"/> instance that is used for logging.</param>
        /// <param name="eventData">Lasso injected command event data.</param>
        /// <returns>An integer status code. 0 for success and non-zero for failure.</returns>
        public int OnExecute(ILogger<CommandToken> logger, CommandExecuteEventData eventData)
        {
            logger.LogInformation("coming soon");
            return 0;
        }
    }
}
