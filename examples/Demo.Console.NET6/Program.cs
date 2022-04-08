// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Demo.Console.NET6
{
    using System;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.Logging;
    using Microsoft.TeamFoundation.SourceControl.WebApi;
    using Microsoft.VisualStudio.Services.Common;
    using Microsoft.VisualStudio.Services.WebApi;
    using NLog.Config;
    using NLog.Extensions.Logging;
    using NLog.Targets;

    /// <summary>
    /// The startup program.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Setup dependencies
            Uri repoUri = new Uri("https://contoso.com/repo");
            LoggingConfiguration config = new LoggingConfiguration();
            var logconsole = new ConsoleTarget("logconsole");
            config.AddTarget(logconsole);
            config.AddRuleForAllLevels("logconsole");

            NLog.LogManager.Configuration = config;

            ILogger logger = LoggerFactory.Create((c) => c.AddNLog(config)).CreateLogger<Program>();

            logger.LogInformation("I am ExampleCLI");

            if (args.Length != 2)
            {
                logger.LogError("Usage: <resource> <client>");
                Environment.Exit(1);
            }

            Guid resource = new Guid(args[1]);
            Guid client = new Guid(args[2]);
            Guid tenant = new Guid(args[3]);

            // Create a token fetcher
            ITokenFetcher adoAuth = new TokenFetcherPublicClient(logger, resource, client, tenant);

            // Get a token
            TokenResult token;
            try
            {
                token = adoAuth.GetAccessTokenAsync().Result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                if (ex.InnerException != null)
                {
                    logger.LogError($"Inner Exception:\n{ex.InnerException.Message}");
                }

                Environment.Exit(1);
                return;
            }

            /*
             * token.User  -> the email that authenticated
             * token.Token -> the actual access token. Treat this like a password.
             * token.JWT   -> the JsonWebToken model instance. This class defined by the MSAL library.
             */
            logger.LogInformation($"Welcome, {token.User}\n");

            // Use the token!
            VssConnection connection = new VssConnection(
              repoUri,
              new VssBasicCredential(string.Empty, token.Token));

            var gitClient = connection.GetClient<GitHttpClient>();

            var prId = 0; // Note: This should be replaced by an actual PR number.
            var pr = gitClient.GetPullRequestByIdAsync("office", prId).Result;

            System.Console.WriteLine($"PR {prId}: {pr.Title}");
            System.Console.WriteLine($"Opened by: {pr.CreatedBy.DisplayName}\non {pr.CreationDate.ToLocalTime()}\nstatus: {pr.Status}");
        }
    }
}
