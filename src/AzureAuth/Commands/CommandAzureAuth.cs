// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands
{
    using McMaster.Extensions.CommandLineUtils;

    /// <summary>
    /// The command main class parses commands and dispatches to the corresponding methods.
    /// </summary>
    [Command(Name = "azureauth", Description = "A CLI interface to MSAL (Microsoft Authentication Library)")]
    [Subcommand(typeof(CommandAad), typeof(CommandAdo))]
    public class CommandAzureAuth
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAzureAuth"/> class.
        /// </summary>
        public CommandAzureAuth()
        {
        }

        /// <summary>
        /// Execute.
        /// </summary>
        /// <param name="app"><see cref="CommandLineApplication"/> instance of the current command.</param>
        /// <returns>0 - this command only shows help.</returns>
        public int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}
