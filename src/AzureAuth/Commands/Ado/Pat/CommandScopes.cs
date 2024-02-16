
using McMaster.Extensions.CommandLineUtils;

using Microsoft.Authentication.AdoPat;
using Microsoft.Extensions.Logging;

using System;
using System.Linq;

namespace Microsoft.Authentication.AzureAuth.Commands.Ado.Pat
{
    /// <summary>
    /// Command to print the list of available scopes

    /// </summary>
    [Command("scopes", Description = "List the valid ado pat scopes")]
    public class CommandScopes
    {
        private readonly ILogger logger;

        private readonly string UrlMessage = $"See {AdoPat.Constants.PatListURL} for details.";

        /// <summary>
        /// Create a CommandScopes
        /// </summary>
        /// <param name="logger"></param>
        public CommandScopes(ILogger<CommandScopes> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Executes the Scopes command listing the available scopes
        /// </summary>
        /// <returns></returns>
        public int OnExecute()
        {
            var scopes = Scopes.ValidScopes.ToList();
            scopes.Sort();

            // Print URL at the top and bottom because it's a long list of scopes.
            logger.LogInformation(UrlMessage + "\n");
            logger.LogInformation(string.Join(Environment.NewLine, scopes));
            logger.LogInformation("\n" + UrlMessage);
            return 0;
        }
    }
}
