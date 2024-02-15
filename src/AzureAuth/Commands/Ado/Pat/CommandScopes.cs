
using McMaster.Extensions.CommandLineUtils;

using Microsoft.Authentication.AdoPat;
using Microsoft.Extensions.Logging;
using Microsoft.Office.Lasso.Telemetry;

using System.Linq;

namespace Microsoft.Authentication.AzureAuth.Commands.Ado.Pat
{
    /// <summary>
    /// Command to print the list of avaialable scopes
    /// </summary>
    [Command("scopes", Description = "List the valid ado pat scopes")]
    public class CommandScopes
    {
        private readonly ILogger logger;

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

            foreach (var scope in scopes)
            {
                logger.LogInformation(scope);
            }

            return 0;
        }
    }
}
