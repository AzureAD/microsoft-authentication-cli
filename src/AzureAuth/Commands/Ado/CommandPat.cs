// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands.Ado
{
    using McMaster.Extensions.CommandLineUtils;

    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// An ADO Command for creating or fetching, and returning Azure Devops PATs.
    /// </summary>
    [Command("pat", Description = "Create and cache Azure Devops Personal Access Tokens (PATs) using encrypted local storage.")]
    public class CommandPat
    {
        private const string OrganizationOption = "--organization";
        private const string OrganizationHelp = "The name of the Azure DevOps organization.";

        private const string DisplayNameOption = "--display-name";
        private const string DisplayNameHelp = "The Personal Access Token name.";

        private const string ScopeOption = "--scope";
        private const string ScopeHelp = "A token scope for accessing Azure DevOps resources. Repeated invocations allowed.";

        private const string OutputOption = "--output";
        private const string OutputHelp = "How PAT information is displayed. [default: token]\n[possible values: none, status, token, base64, header, headervalue, json]";

        // The possible PAT output modes.
        private enum OutputMode
        {
            // No output whatsoever.
            None,

            // Text indicating that a PAT was created/fetched and cached.
            Status,

            // Just the PAT, nothing more.
            Token,

            // A Base64-encoded version of the PAT.
            Base64,

            // The full `Authorization Basic` HTTP header.
            Header,

            // Just the value of the `Authorization Basic` header.
            HeaderValue,

            // The JSON value for the PAT, exactly as it was returned by the Azure DevOps API.
            Json,
        }

        [Option(OrganizationOption, OrganizationHelp, CommandOptionType.SingleValue)]
        private string Organization { get; set; } = null;

        [Option(DisplayNameOption, DisplayNameHelp, CommandOptionType.SingleValue)]
        private string DisplayName { get; set; } = null;

        [Option(ScopeOption, ScopeHelp, CommandOptionType.MultipleValue)]
        private string[] Scopes { get; set; } = null;

        [Option(OutputOption, OutputHelp, CommandOptionType.SingleValue)]
        private OutputMode Output { get; set; } = OutputMode.Token;

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
