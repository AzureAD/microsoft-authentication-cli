// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands
{
    using McMaster.Extensions.CommandLineUtils;

    [Command("ado", Description = "todo")]
    [Subcommand(typeof(Ado.CommandPat), typeof(Ado.CommandToken))]
    public class CommandAdo
    {
        public CommandAdo() { }

        /// <summary>
        /// Execute.
        /// </summary>
        /// <param name="app">The app insntace of the current command.</param>
        /// <returns>Exit code</returns>
        public int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}
