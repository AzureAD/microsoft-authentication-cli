// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands
{
    using McMaster.Extensions.CommandLineUtils;

    /// <summary>
    /// Parent command for Azure Devops specific commands.
    /// </summary>
    [Command("ado", Description = "A collection of Azure Devops (ADO) specific authentication commands.")]
    [Subcommand(typeof(Ado.CommandPat))]
    [Subcommand(typeof(Ado.CommandToken))]
    public class CommandAdo
    {
        /// <summary>
        /// Execute the command, showing the help text since this is only a parent command. returns 0.
        /// </summary>
        /// <param name="app">The command app instance.</param>

        /// <returns>An Exit code of 0 since showingthe help should never fail.</returns>
        public int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}
