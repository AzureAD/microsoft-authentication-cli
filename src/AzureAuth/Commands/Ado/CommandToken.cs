// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands.Ado
{
    using System.Collections.Generic;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Authentication.MSALWrapper;
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
        /// Gets or sets the Azure Tenant ID to use for authentication.
        /// </summary>
        [Option(CommandAad.TenantOption, Description = "The Azure Tenant ID to use for authentication. Defaults to Microsoft.")]
        public string Tenant { get; set; } = AzureAuth.Ado.Constants.Tenant.Msft;

        /// <summary>
        /// Gets or sets the auth modes.
        /// </summary>
        [Option(CommandAad.ModeOption, CommandAad.AuthModeHelperText, CommandOptionType.MultipleValue)]
        public IEnumerable<AuthMode> AuthModes { get; set; } = new[] { AuthMode.Default };

        /// <summary>
        /// Gets or sets domain suffix to filter on.
        /// </summary>
        [Option(CommandAad.DomainOption, Description = CommandAad.DomainHelpText)]
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the global timeout option.
        /// </summary>
        [Option(CommandAad.TimeoutOption, CommandAad.TimeoutHelpText, CommandOptionType.SingleValue)]
        public double Timeout { get; set; } = CommandAad.GlobalTimeout.TotalMinutes;

        /// <summary>
        /// Gets or sets the prompt hint.
        /// </summary>
        [Option(CommandAad.PromptHintOption, CommandAad.PromptHintHelpText, CommandOptionType.SingleValue)]
        public string PromptHint { get; set; }

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
