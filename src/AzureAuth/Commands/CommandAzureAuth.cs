// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands
{
    using System.IO.Abstractions;

    using McMaster.Extensions.CommandLineUtils;

    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// The command main class parses commands and dispatches to the corresponding methods.
    /// </summary>
    [Command(Name = "azureauth", Description = "A CLI interface to MSAL (Microsoft Authentication Library)." +
        "\n\n⚠️ NOTICE: Use `azureauth aad [options]`.\n⚠️ The top-level azureauth command is deprecated and will print help text soon." +
        "\n\n\u001b[31m-- azureauth [options]\u001b[0m" +
        "\n\u001b[32m++ azureauth aad [options]\u001b[0m\n")]
    [Subcommand(typeof(CommandAad))]
    //[Subcommand(typeof(CommandAdo))] // TODO: Add ado command once sub-commands are ready
    [Subcommand(typeof(CommandInfo))]
    public class CommandAzureAuth : CommandAad
    {
#pragma warning disable SA1648 // inheritdoc should be used with inheriting class
        /// <inheritdoc/>
        public CommandAzureAuth(CommandExecuteEventData eventData, ITelemetryService telemetryService, ILogger<CommandAzureAuth> logger, IFileSystem fileSystem, IEnv env)
#pragma warning restore SA1648 // inheritdoc should be used with inheriting class
            : base(eventData, telemetryService, logger, fileSystem, env)
        {
        }
    }
}
