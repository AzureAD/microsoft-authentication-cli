// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Demo.Console.NETFramework472
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Gets or sets the resource.
        /// </summary>
        [Required]
        [Option("--resource", "The ID of the resource you are authenticating to.", CommandOptionType.SingleValue)]
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        [Required]
        [Option("--client", "The ID of the App registration you are authenticating as.", CommandOptionType.SingleValue)]
        public string Client { get; set; }

        /// <summary>
        /// Gets or sets the tenant.
        /// </summary>
        [Required]
        [Option("--tenant", "The ID of the tenant you are authenticating to.", CommandOptionType.SingleValue)]
        public string Tenant { get; set; }

        /// <summary>
        /// The on execute.
        /// </summary>
        /// <param name="logger">
        /// The logger.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public int OnExecute(ILogger<Program> logger)
        {
            ITokenFetcher fetcher = new TokenFetcherPublicClient(logger, new Guid(this.Resource), new Guid(this.Client), new Guid(this.Tenant));
            TokenResult result = fetcher.GetAccessTokenAsync().Result;
            logger.LogInformation($"Got token for {result.User} valid for {result.ValidFor}");
            return 0;
        }

        private static void Main(string[] args)
        {
            CommandLineApplication app = new CommandLineApplication<Program>();

            TelemetryConfig tConfig = new TelemetryConfig()
            {
                Backend = TelemetryOutput.StandardOut,
                Async = false,
            };

            LassoOptions options = new LassoOptions(tConfig);
            Lasso lassoApp = new Lasso(app, options);
            lassoApp.Execute(args);
        }
    }
}
