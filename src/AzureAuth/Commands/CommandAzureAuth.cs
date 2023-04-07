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
    [Command(Name = "azureauth", Description = "A CLI interface to MSAL (Microsoft Authentication Library).")]
    [Subcommand(typeof(CommandAad))]
    [Subcommand(typeof(CommandAdo))]
    [Subcommand(typeof(CommandInfo))]
    public class CommandAzureAuth
    {
        /// <summary> Execute the command. </summary>
        /// <param name="app">The current command instance.</param>
        /// <returns>0.</returns>
        public int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}
