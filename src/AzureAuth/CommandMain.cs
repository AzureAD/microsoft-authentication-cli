// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;

    using McMaster.Extensions.CommandLineUtils;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Extensions;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// The command main.
    /// </summary>
    [Command(Name = "azureauth", Description = "A CLI interface to MSAL authentication")]
    public class CommandMain
    {
        private const string ResourceOption = "--resource";
        private const string ClientOption = "--client";
        private const string TenantOption = "-t|--tenant";
        private const string ScopeOption = "--scope";
        private const string ClearOption = "-c|--clear";
        private const string DomainOption = "-d|--domain";
        private const string AuthModeOption = "-m|--auth-mode";
        private const string OutputOption = "-o|--output";
        private const string AliasOption = "--alias";
        private const string ConfigOption = "--config";

#if PlatformWindows
        private const string AuthModeHelperText = @"Authentication mode. Default: broker.
You can use any combination of modes with multiple instances of the -m flag.
Allowed values: [all, broker, web, devicecode]";

#else
        private const string AuthModeHelperText = @"Authentication mode. Default: web.
You can use any combination with multiple instances of the -m flag.
Allowed values: [all, web, devicecode]";
#endif

        private readonly EventData eventData;
        private readonly ILogger<CommandMain> logger;
        private readonly IFileSystem fileSystem;

        private Alias tokenFetcherOptions;
        private ITokenFetcher tokenFetcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandMain"/> class.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        public CommandMain(CommandExecuteEventData eventData, ILogger<CommandMain> logger, IFileSystem fileSystem)
        {
            this.eventData = eventData;
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandMain"/> class.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="tokenFetcher">An injected ITokenFetcher (defined for testability).</param>
        public CommandMain(CommandExecuteEventData eventData, ILogger<CommandMain> logger, IFileSystem fileSystem, ITokenFetcher tokenFetcher)
            : this(eventData, logger, fileSystem)
        {
            this.tokenFetcher = tokenFetcher;
        }

        /// <summary>
        /// Gets or sets the resource.
        /// </summary>
        [Option(ResourceOption, "The ID of the resource you are authenticating to.", CommandOptionType.SingleValue)]
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        [Option(ClientOption, "The ID of the App registration you are authenticating as.", CommandOptionType.SingleValue)]
        public string Client { get; set; }

        /// <summary>
        /// Gets or sets the tenant.
        /// </summary>
        [Option(TenantOption, "Tenant Name or AAD TenantId.\nAllowed values are \"microsoft\", \"torus\" or an AAD Tenant Guid\n", CommandOptionType.SingleValue)]
        public string Tenant { get; set; }

        /// <summary>
        /// Gets or sets the scopes.
        /// </summary>
        [Option(ScopeOption, "Scopes to request. By default, the only scope requested is <resource ID>\\.default.\nPassing in one or more values here will override the default.", CommandOptionType.MultipleValue)]
        public string[] Scopes { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether clear cache.
        /// </summary>
        [Option(ClearOption, "Clear the token cache for this AAD Application.\n", CommandOptionType.NoValue)]
        public bool ClearCache { get; set; }

        /// <summary>
        /// Gets or sets the preferred domain.
        /// </summary>
        [Option(DomainOption, "Preferred domain to filter cached accounts by. If a single account matching the preferred domain is in the cache it is used, otherwise an account picker will be launched.\n", CommandOptionType.SingleValue)]
        public string PreferredDomain { get; set; }

        /// <summary>
        /// Gets or sets the auth modes.
        /// </summary>
        [Option(AuthModeOption, AuthModeHelperText, CommandOptionType.MultipleValue)]
        public IEnumerable<AuthMode> AuthModes { get; set; } = new[] { AuthMode.Default };

        /// <summary>
        /// Gets or sets the output.
        /// </summary>
        [Option(OutputOption, "Output mode. Controls how the token information is printed to stdout.\nDefault: status (only print status, no token).\nAvailable: [status, token, json, none]\n", CommandOptionType.SingleValue)]
        public OutputMode Output { get; set; } = OutputMode.Status;

        /// <summary>
        /// Gets or sets the alias name.
        /// </summary>
        [Option(AliasOption, "The name of an alias for config options loaded from the config file.", CommandOptionType.SingleValue)]
        public string AliasName { get; set; }

        /// <summary>
        /// Gets or sets the config file path.
        /// </summary>
        [FileExists]
        [Option(ConfigOption, "The path to a configuration file.", CommandOptionType.SingleValue)]
        public string ConfigFilePath { get; set; }

        /// <summary>
        /// Gets the token fetcher options.
        /// </summary>
        public Alias TokenFetcherOptions
        {
            get { return this.tokenFetcherOptions; }
        }

        private AuthMode CombinedAuthMode => this.AuthModes.Aggregate((a1, a2) => a1 | a2);

        /// <summary>
        /// The evaluate options.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool EvaluateOptions()
        {
            // We start with the assumption that we only have command line options.
            Alias evaluatedOptions = new Alias
            {
                Resource = this.Resource,
                Client = this.Client,
                Domain = this.PreferredDomain,
                Tenant = this.Tenant,
                Scopes = this.Scopes?.ToList(),
            };

            // We only load options from a config file if an alias is given.
            if (!string.IsNullOrEmpty(this.AliasName))
            {
                if (string.IsNullOrEmpty(this.ConfigFilePath))
                {
                    // This is a fatal error. We can't load aliases without a config file.
                    this.logger.LogError($"The {AliasOption} field was given, but no {ConfigOption} was specified.");
                    return false;
                }

                string fullConfigPath = this.fileSystem.Path.GetFullPath(this.ConfigFilePath);

                try
                {
                    Config config = Config.FromFile(fullConfigPath, this.fileSystem);
                    if (config.Alias is null || !config.Alias.ContainsKey(this.AliasName))
                    {
                        // This is a fatal error. We can't load a missing alias.
                        this.logger.LogError($"Alias '{this.AliasName}' was not found in {this.ConfigFilePath}");
                        return false;
                    }

                    // Load the requested alias and merge it with any command line options.
                    Alias configFileOptions = config.Alias[this.AliasName];
                    evaluatedOptions = configFileOptions.Override(evaluatedOptions);
                }
                catch (Tomlyn.TomlException ex)
                {
                    this.logger.LogError($"Error parsing TOML in config file at '{fullConfigPath}':\n{ex.Message}");
                    return false;
                }
            }

            // Set the token fetcher options so they can be used later on.
            this.tokenFetcherOptions = evaluatedOptions;

            // Evaluation is a two-part task. Parse, then validate. Validation is complex, so we call a separate helper.
            return this.ValidateOptions();
        }

        /// <summary>
        /// The on execute.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public int OnExecute()
        {
            if (!this.EvaluateOptions())
            {
                return 1;
            }

            return this.ClearCache ? this.ClearLocalCache() : this.GetToken();
        }

        private bool ValidateOptions()
        {
            bool validOptions = true;
            if (string.IsNullOrEmpty(this.tokenFetcherOptions.Resource))
            {
                this.logger.LogError($"The {ResourceOption} field is required.");
                validOptions = false;
            }

            if (string.IsNullOrEmpty(this.tokenFetcherOptions.Client))
            {
                this.logger.LogError($"The {ClientOption} field is required.");
                validOptions = false;
            }

            if (string.IsNullOrEmpty(this.tokenFetcherOptions.Tenant))
            {
                this.logger.LogError($"The {TenantOption} field is required.");
                validOptions = false;
            }

            return validOptions;
        }

        private int ClearLocalCache()
        {
            this.TokenFetcher().ClearCacheAsync().Wait();
            return 0;
        }

        private int GetToken()
        {
            try
            {
                TokenResult tokenResult = null;
                ITokenFetcher tokenFetcher = null;
                using (new GlobalLock(this.Tenant + this.Resource))
                {
                    tokenFetcher = this.TokenFetcher();
                    tokenResult = (this.tokenFetcherOptions.Scopes == null
                    ? tokenFetcher.GetAccessTokenAsync(this.CombinedAuthMode)
                    : tokenFetcher.GetAccessTokenAsync(this.tokenFetcherOptions.Scopes, this.CombinedAuthMode))
                    .Result;
                }

                this.eventData.Add("error_list", ExceptionListToStringConverter.Execute(tokenFetcher.Errors()));

                if (tokenResult == null)
                {
                    this.logger.LogError("Authentication failed. Re-run with '--verbosity debug' to get see more info.");
                    return 1;
                }

                this.eventData.Add("auth_type", $"{tokenResult.AuthType}");

                switch (this.Output)
                {
                    case OutputMode.Status:
                        this.logger.LogSuccess(tokenResult.ToString());
                        break;
                    case OutputMode.Token:
                        this.logger.LogInformation(tokenResult.Token);
                        break;
                    case OutputMode.Json:
                        this.logger.LogInformation(tokenResult.ToJson());
                        break;
                    case OutputMode.None:
                        break;
                }
            }
            catch (Exception ex)
            {
                this.eventData.Add(ex);
                this.logger.LogCritical(ex.Message);
                return 1;
            }

            return 0;
        }

        private ITokenFetcher TokenFetcher()
        {
            if (this.tokenFetcher == null)
            {
                return new TokenFetcherPublicClient(
                    this.logger,
                    new Guid(this.tokenFetcherOptions.Resource),
                    new Guid(this.tokenFetcherOptions.Client),
                    new Guid(this.tokenFetcherOptions.Tenant),
                    osxKeyChainSuffix: Constants.AuthOSXKeyChainSuffix,
                    preferredDomain: this.tokenFetcherOptions.Domain);
            }

            return this.tokenFetcher;
        }
    }
}
