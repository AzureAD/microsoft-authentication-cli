// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;

    using McMaster.Extensions.CommandLineUtils;

    using Microsoft.Authentication.AzureAuth;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Office.Lasso.Extensions;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// Command class for authenticating with AAD.
    /// </summary>
    [Command("aad", Description = "Acquire an Azure Access Token")]
    public class CommandAad
    {
        /// <summary>
        /// The Domain option.
        /// </summary>
        public const string DomainOption = "--domain";

        /// <summary>
        /// The Tenant option.
        /// </summary>
        public const string TenantOption = "--tenant";

        /// <summary>
        /// The Timeout option.
        /// </summary>
        public const string TimeoutOption = "--timeout";

        /// <summary>
        /// The Mode option.
        /// </summary>
        public const string ModeOption = "--mode";

        /// <summary>
        /// The Prompt Hint option.
        /// </summary>
        public const string PromptHintOption = "--prompt-hint";

        /// <summary>
        /// The Prompt Hint help text.
        /// </summary>
        public const string PromptHintHelpText = "A prompt hint to contextualize prompts and identify uses in telemetry, when captured.";

        /// <summary>
        /// The help text for the <see cref="ModeOption"/> option.
        /// </summary>
#if PlatformWindows
        public const string AuthModeHelperText = $@"Authentication mode. Repeated invocations allowed.
[default: broker, then web]
[possible values: {AuthModeAllowedValues}]";
#else
        public const string AuthModeHelperText = $@"Authentication mode. Repeated invocations allowed
[default: web]
[possible values: {AuthModeAllowedValues}]";
#endif

        /// <summary>
        /// The help text for the <see cref="DomainOption"/> option.
        /// </summary>
        public const string DomainHelpText = "Preferred domain for filtering cached accounts.\nSkips launching an account picker if only one cached account matches the preferred domain.";

        /// <summary>
        /// The help text for the <see cref="TimeoutOption"/> option.
        /// </summary>
        public const string TimeoutHelpText = "The number of minutes before authentication times out.\n[default: 15 minutes]";

        /// <summary>
        /// The default number of minutes CLI is allowed to run.
        /// </summary>
        public static readonly TimeSpan GlobalTimeout = TimeSpan.FromMinutes(15);

        /// <summary>
        /// The allowed values for the <see cref="AuthMode"/> option.
        /// </summary>
#if PlatformWindows
        public const string AuthModeAllowedValues = "all, iwa, broker, web, devicecode";
#else
        public const string AuthModeAllowedValues = "all, web, devicecode";
#endif

        private const string ResourceOption = "--resource";
        private const string ClientOption = "--client";

        private const string ScopeOption = "--scope";
        private const string ClearOption = "--clear";

        private const string OutputOption = "--output";
        private const string AliasOption = "--alias";
        private const string ConfigOption = "--config";

        private readonly EventData eventData;
        private readonly ILogger<CommandAzureAuth> logger;
        private readonly IFileSystem fileSystem;
        private readonly IEnv env;
        private Alias authSettings;
        private ITelemetryService telemetryService;
        private IAuthFlow authFlow;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAad"/> class.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="telemetryService">The telemetry service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="env">The environment interface.</param>
        public CommandAad(CommandExecuteEventData eventData, ITelemetryService telemetryService, ILogger<CommandAzureAuth> logger, IFileSystem fileSystem, IEnv env)
        {
            this.eventData = eventData;
            this.telemetryService = telemetryService;
            this.logger = logger;
            this.fileSystem = fileSystem;
            this.env = env;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAad"/> class.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="telemetryService">The telemetry service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="env">The environment interface.</param>
        /// <param name="authFlow">An injected <see cref="IAuthFlow"/> (defined for testability).</param>
        public CommandAad(CommandExecuteEventData eventData, ITelemetryService telemetryService, ILogger<CommandAzureAuth> logger, IFileSystem fileSystem, IEnv env, IAuthFlow authFlow)
            : this(eventData, telemetryService, logger, fileSystem, env)
        {
            this.authFlow = authFlow;
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
        [Option(TenantOption, "The ID of the Tenant where the client and resource entities exist in", CommandOptionType.SingleValue)]
        public string Tenant { get; set; }

        /// <summary>
        /// Gets or sets the customized prompt hint text for WAM prompts and web mode.
        /// </summary>
        [Option(PromptHintOption, PromptHintHelpText, CommandOptionType.SingleValue)]
        public string PromptHint { get; set; }

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
        [Option(DomainOption, DomainHelpText, CommandOptionType.SingleValue)]
        public string PreferredDomain { get; set; }

        /// <summary>
        /// Gets or sets the auth modes.
        /// </summary>
        [Option(ModeOption, AuthModeHelperText, CommandOptionType.MultipleValue)]
        public IEnumerable<AuthMode> AuthModes { get; set; }

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
        /// Gets or sets global Timeout.
        /// </summary>
        [Option(TimeoutOption, TimeoutHelpText, CommandOptionType.SingleValue)]
        public double Timeout { get; set; } = GlobalTimeout.TotalMinutes;

        /// <summary>
        /// Gets the token fetcher options.
        /// </summary>
        public Alias TokenFetcherOptions
        {
            get { return this.authSettings; }
        }

        /// <summary>
        /// This method evaluates whether the options are valid or not.
        /// </summary>
        /// <returns>
        /// Whether the option is valid.
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
                PromptHint = this.PromptHint,
                Scopes = this.Scopes?.ToList(),
            };

            // We only load options from a config file if an alias is given.
            if (!string.IsNullOrEmpty(this.AliasName))
            {
                this.ConfigFilePath = this.ConfigFilePath ?? this.env.Get(EnvVars.Config);
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
                catch (System.IO.FileNotFoundException)
                {
                    this.logger.LogError($"The file '{fullConfigPath}' does not exist.");
                    return false;
                }
                catch (Tomlyn.TomlException ex)
                {
                    this.logger.LogError($"Error parsing TOML in config file at '{fullConfigPath}':\n{ex.Message}");
                    return false;
                }
            }

            // If command line options for mode are not specified, then use the environment variables.
            this.AuthModes ??= env.ReadAuthModeFromEnvOrSetDefault();
            if (!this.AuthModes.Any())
            {
                this.logger.LogError($"Invalid value specified for environment variable {EnvVars.AuthMode}. Allowed values are: {CommandAad.AuthModeHelperText}");
                return false;
            }

            // Handle Resource Shorthand for Default Scope
            if (evaluatedOptions.Scopes.IsNullOrEmpty() && !string.IsNullOrEmpty(evaluatedOptions.Resource))
            {
                evaluatedOptions.Scopes = new List<string>() { $"{evaluatedOptions.Resource}/.default" };
                evaluatedOptions.Resource = null;
            }

            // Set the token fetcher options so they can be used later on.
            this.authSettings = evaluatedOptions;

            // Evaluation is a two-part task. Parse, then validate. Validation is complex, so we call a separate helper.
            return this.ValidateOptions();
        }

        /// <summary>
        /// This method executes the auth process.
        /// </summary>
        /// <param name="publicClientAuth">An <see cref="IPublicClientAuth"/> to handle authentication.</param>
        /// <returns>
        /// The error code: 0 is normal execution, and the rest means errors during execution.
        /// </returns>
        public int OnExecute(IPublicClientAuth publicClientAuth)
        {
            if (!this.EvaluateOptions())
            {
                this.eventData.Add("validargs", false);
                return 1;
            }

            this.eventData.Add("validargs", true);
            this.eventData.Add("settings_client", this.authSettings.Client);
            this.eventData.Add("settings_resource", this.authSettings.Resource);
            this.eventData.Add("settings_tenant", this.authSettings.Tenant);
            this.eventData.Add("settings_prompthint", this.authSettings.PromptHint);

            // Small bug in Lasso - Add does not accept a null IEnumerable here.
            this.eventData.Add("settings_scopes", this.authSettings.Scopes ?? new List<string>());

            return this.ClearCache ? this.ClearLocalCache() : this.GetToken(publicClientAuth);
        }

        private bool ValidateOptions()
        {
            bool validOptions = true;

            int scopesCount = this.authSettings.Scopes?.Count ?? 0;

            if (string.IsNullOrEmpty(this.authSettings.Resource) && scopesCount == 0)
            {
                this.logger.LogError($"The {ResourceOption} field or the {ScopeOption} field is required.");
                validOptions = false;
            }

            if (!string.IsNullOrEmpty(this.authSettings.Resource) && scopesCount > 0)
            {
                this.logger.LogWarning($"The {ScopeOption} option was provided with the {ResourceOption} option. Only {ScopeOption} will be used.");
            }

            if (string.IsNullOrEmpty(this.authSettings.Client))
            {
                this.logger.LogError($"The {ClientOption} field is required.");
                validOptions = false;
            }

            if (string.IsNullOrEmpty(this.authSettings.Tenant))
            {
                this.logger.LogError($"The {TenantOption} field is required.");
                validOptions = false;
            }

            return validOptions;
        }

        private int ClearLocalCache()
        {
            var pca = PublicClientApplicationBuilder.Create(this.authSettings.Client).Build();
            var pcaWrapper = new PCAWrapper(this.logger, pca, new List<Exception>(), new Guid(this.authSettings.Tenant));

            var accounts = pcaWrapper.TryToGetCachedAccountsAsync().Result;
            while (accounts.Any())
            {
                var account = accounts.First();
                this.logger.LogInformation($"Removing {account.Username} from the cache...");
                pcaWrapper.RemoveAsync(account).Wait();
                accounts = pcaWrapper.TryToGetCachedAccountsAsync().Result;
                this.logger.LogInformation("Cleared.");
            }

            return 0;
        }

        private int GetToken(IPublicClientAuth publicClientAuth)
        {
            try
            {
                TokenResult tokenResult = publicClientAuth.Token(
                    authParams: new AuthParameters(this.authSettings.Client, this.authSettings.Tenant, this.authSettings.Scopes),
                    authModes: this.AuthModes,
                    domain: this.authSettings.Domain,
                    prompt: this.authSettings.PromptHint,
                    timeout: TimeSpan.FromMinutes(this.Timeout),
                    this.eventData);

                if (tokenResult == null)
                {
                    this.logger.LogError("Authentication failed. Re-run with '--verbosity debug' to get see more info.");
                    return 1;
                }

                switch (this.Output)
                {
                    case OutputMode.Status:
                        this.logger.LogSuccess(tokenResult.ToString());
                        break;
                    case OutputMode.Token:
                        Console.Write(tokenResult.Token);
                        break;
                    case OutputMode.Json:
                        Console.Write(tokenResult.ToJson());
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
    }
}
