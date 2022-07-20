// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.Office.Lasso.Extensions;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// The command main class parses commands and dispatches to the corresponding methods.
    /// </summary>
    [Command(Name = "azureauth", Description = "A CLI interface to MSAL authentication")]
    public class CommandMain
    {
        private const string ResourceOption = "--resource";
        private const string ClientOption = "--client";
        private const string TenantOption = "--tenant";
        private const string PromptHintOption = "--prompt-hint";
        private const string ScopeOption = "--scope";
        private const string ClearOption = "--clear";
        private const string DomainOption = "--domain";
        private const string ModeOption = "--mode";
        private const string OutputOption = "--output";
        private const string AliasOption = "--alias";
        private const string ConfigOption = "--config";
        private const string CacheOption = "--cache";
        private const string PromptHintPrefix = "AzureAuth";
        private const string TimeoutOption = "--timeout";

#if PlatformWindows
        private const string AuthModeHelperText = @"Authentication mode. Default: iwa (Integrated Windows Auth), then broker, then web.
You can use any combination of modes with multiple instances of the --mode flag.
Allowed values: [all, iwa, broker, web, devicecode]";
#else
        private const string AuthModeHelperText = @"Authentication mode. Default: web.
You can use any combination with multiple instances of the --mode flag.
Allowed values: [all, web, devicecode]";
#endif

        private readonly EventData eventData;
        private readonly ILogger<CommandMain> logger;
        private readonly IFileSystem fileSystem;
        private readonly IEnv env;
        private Alias authSettings;
        private IAuthFlow authFlow;
        private AuthFlowExecutor authFlowExecutor;
        private ITelemetryService telemetryService;

        /// <summary>
        /// The maximum time we will wait to acquire a mutex around prompting the user.
        /// </summary>
        private TimeSpan promptMutexTimeout = TimeSpan.FromMinutes(15);
        private string cacheFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandMain"/> class.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="telemetryService">The telemetry service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="env">The environment interface.</param>
        public CommandMain(CommandExecuteEventData eventData, ITelemetryService telemetryService, ILogger<CommandMain> logger, IFileSystem fileSystem, IEnv env)
        {
            this.eventData = eventData;
            this.telemetryService = telemetryService;
            this.logger = logger;
            this.fileSystem = fileSystem;
            this.env = env;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandMain"/> class.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="telemetryService">The telemetry service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="env">The environment interface.</param>
        /// <param name="authFlow">An injected <see cref="IAuthFlow"/> (defined for testability).</param>
        public CommandMain(CommandExecuteEventData eventData, ITelemetryService telemetryService, ILogger<CommandMain> logger, IFileSystem fileSystem, IEnv env, IAuthFlow authFlow)
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
        [Option(PromptHintOption, "The prompt hint text for WAM prompts and web mode.", CommandOptionType.SingleValue)]
        public string PromptHint { get; set; }

        /// <summary>
        /// Gets or sets global Timeout.
        /// </summary>
        [Option(TimeoutOption, "Number of seconds for which the CLI should run", CommandOptionType.SingleValue)]
        public double Timeout { get; set; } = Constants.GlobalTimeout.TotalSeconds;

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
        [Option(ModeOption, AuthModeHelperText, CommandOptionType.MultipleValue)]
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
        /// Gets or sets the cache file name. Only available on Windows.
        /// </summary>
        [Option(CacheOption, "Override the default cache file location. This option is only available on Windows.", CommandOptionType.SingleValue, ShowInHelpText = false)]
        [LegalFilePath]
        public string CacheFilePath
        {
            get
            {
                // Check command parameter first.
                if (!string.IsNullOrEmpty(this.cacheFilePath))
                {
                    return this.cacheFilePath;
                }

                // Check environment variable.
                string envCacheFile = this.env.Get(EnvVars.Cache);
                if (!string.IsNullOrEmpty(envCacheFile))
                {
                    return envCacheFile;
                }

                // Use default cache file path.
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string absolutePath = this.fileSystem.Path.Combine(appData, ".IdentityService", $"msal_{this.authSettings.Tenant}.cache");
                return absolutePath;
            }

            set
            {
                this.cacheFilePath = value;
            }
        }

        /// <summary>
        /// Gets the token fetcher options.
        /// </summary>
        public Alias TokenFetcherOptions
        {
            get { return this.authSettings; }
        }

        private AuthMode CombinedAuthMode => this.AuthModes.Aggregate((a1, a2) => a1 | a2);

        /// <summary>
        /// Combine the <see cref="PromptHintPrefix"/> with the caller provided prompt hint.
        /// </summary>
        /// <param name="promptHint">The provided prompt hint.</param>
        /// <returns>The combined prefix and prompt hint or just the prefix if no prompt hint was given.</returns>
        public static string PrefixedPromptHint(string promptHint)
        {
            if (string.IsNullOrEmpty(promptHint))
            {
                return PromptHintPrefix;
            }

            return $"{PromptHintPrefix}: {promptHint}";
        }

        /// <summary>
        /// Generates event data from the AuthFlowResult.
        /// </summary>
        /// <param name="result">The AuthFlowResult.</param>
        /// <returns>The event data.</returns>
        public EventData AuthFlowEventData(AuthFlowResult result)
        {
            if (result == null)
            {
                return null;
            }

            var eventData = new EventData();
            eventData.Add("authflow", result.AuthFlowName);
            eventData.Add("success", result.Success);
            eventData.Add("duration_milliseconds", (int)result.Duration.TotalMilliseconds);

            var correlationIDs = new List<string>();

            if (result.Errors.Any())
            {
                var error_messages = ExceptionListToStringConverter.Execute(result.Errors);
                eventData.Add("error_messages", error_messages);
                correlationIDs = ExceptionListToStringConverter.ExtractCorrelationIDsFromException(result.Errors);
            }

            if (result.Success)
            {
                correlationIDs.Add(result.TokenResult.CorrelationID.ToString());
                eventData.Add("token_validity_minutes", result.TokenResult.ValidFor.TotalMinutes);
                eventData.Add("silent", result.TokenResult.IsSilent);
            }

            if (correlationIDs.Any())
            {
                eventData.Add("msal_correlation_ids", correlationIDs);
            }

            return eventData;
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
                catch (Tomlyn.TomlException ex)
                {
                    this.logger.LogError($"Error parsing TOML in config file at '{fullConfigPath}':\n{ex.Message}");
                    return false;
                }
            }

            // Set the token fetcher options so they can be used later on.
            this.authSettings = evaluatedOptions;

            // Evaluation is a two-part task. Parse, then validate. Validation is complex, so we call a separate helper.
            return this.ValidateOptions();
        }

        /// <summary>
        /// This method executes the auth process.
        /// </summary>
        /// <returns>
        /// The error code: 0 is normal execution, and the rest means errors during execution.
        /// </returns>
        public int OnExecute()
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
            this.eventData.Add("settings_cachefile", this.CacheFilePath);

            // Small bug in Lasso - Add does not accept a null IEnumerable here.
            this.eventData.Add("settings_scopes", this.authSettings.Scopes ?? new List<string>());

            return this.ClearCache ? this.ClearLocalCache() : this.GetToken();
        }

        private bool ValidateOptions()
        {
            bool validOptions = true;
            if (string.IsNullOrEmpty(this.authSettings.Resource))
            {
                this.logger.LogError($"The {ResourceOption} field is required.");
                validOptions = false;
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

            if (!this.CacheFilePath.IsValidAbsoluteFilePath())
            {
                this.logger.LogError($"The option {CacheOption}=`{this.CacheFilePath}` " +
                    $"or environment varable {EnvVars.Cache}=`{this.env.Get(EnvVars.Cache)}` " +
                    $"is not a valid absolute file path.");
                validOptions = false;
            }

            return validOptions;
        }

        private int ClearLocalCache()
        {
            var pca = PublicClientApplicationBuilder.Create(this.authSettings.Client).Build();
            var pcaWrapper = new PCAWrapper(this.logger, pca, new List<Exception>(), new Guid(this.authSettings.Tenant), "azureauth", this.CacheFilePath);

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

        private int GetToken()
        {
            try
            {
                AuthFlowExecutor authFlowExecutor = this.AuthFlowExecutor();
                AuthFlowResult successfulResult = null;
                AuthFlowResult[] results = null;

                // When running multiple AzureAuth processes with the same resource, client, and tenant IDs,
                // They may prompt many times, which is annoying and unexpected.
                // Use Mutex to ensure that only one process can access the corresponding resource at the same time.
                string lockName = $"Local\\{this.Resource}_{this.Client}_{this.Tenant}";

                // First parameter InitiallyOwned indicated whether this lock is owned by current thread.
                // It should be false otherwise a dead lock could occur.
                using (Mutex mutex = new Mutex(false, lockName))
                {
                    bool lockAcquired = false;
                    try
                    {
                        // Wait for the other session to exit.
                        lockAcquired = mutex.WaitOne(this.promptMutexTimeout);
                    }

                    // An AbandonedMutexException could be thrown if another process exits without releasing the mutex correctly.
                    catch (AbandonedMutexException)
                    {
                        // If another process crashes or exits accidently, we can still acquire the lock.
                        lockAcquired = true;

                        // In this case, basicly we can just leave a log warning, because the worst side effect is propmting more than once.
                        this.logger.LogWarning("The authentication attempt mutex was abandoned. Another thread or process may have exited unexpectedly.");
                    }

                    if (!lockAcquired)
                    {
                        throw new TimeoutException("Authentication failed. The application did not gain access in the expected time, possibly because the resource handler was occupied by another process for a long time.");
                    }

                    try
                    {
                        results = authFlowExecutor.GetTokenAsync().Result.ToArray();
                        successfulResult = results.FirstOrDefault(result => result.Success);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }

                var errors = results.SelectMany(result => result.Errors).ToArray();
                this.eventData.Add("error_count", errors.Length);
                this.eventData.Add("authflow_count", results.Length);

                if (successfulResult == null)
                {
                    this.logger.LogError("Authentication failed. Re-run with '--verbosity debug' to get see more info.");
                    return 1;
                }

                var tokenResult = successfulResult.TokenResult;
                this.eventData.Add("silent", tokenResult.IsSilent);
                this.eventData.Add("succeeded_mode", successfulResult.AuthFlowName);

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

                // Send custom telemetry events for each authflow result.
                this.SendAuthFlowTelemetryEvents(results);
            }
            catch (Exception ex)
            {
                this.eventData.Add(ex);
                this.logger.LogCritical(ex.Message);
                return 1;
            }

            return 0;
        }

        private void SendAuthFlowTelemetryEvents(AuthFlowResult[] results)
        {
            Parallel.ForEach(results, result =>
            {
                var eventData = this.AuthFlowEventData(result);
                if (eventData != null)
                {
                    this.telemetryService.SendEvent($"authflow_{result.AuthFlowName}", eventData);
                }
            });
        }

        private AuthFlowExecutor AuthFlowExecutor()
        {
            if (this.authFlowExecutor == null)
            {
                // TODO: Really we need to get rid of Resource
                var scopes = this.Scopes ?? new string[] { $"{this.authSettings.Resource}/.default" };

                var authFlows = AuthFlowFactory.Create(
                    this.logger,
                    this.CombinedAuthMode,
                    new Guid(this.authSettings.Client),
                    new Guid(this.authSettings.Tenant),
                    scopes,
                    this.CacheFilePath,
                    this.PreferredDomain,
                    PrefixedPromptHint(this.authSettings.PromptHint),
                    Constants.AuthOSXKeyChainSuffix);

                this.authFlowExecutor = new AuthFlowExecutor(this.logger, authFlows, this.BuildGlobalTimeoutManager());
            }

            return this.authFlowExecutor;
        }

        private ITimeoutManager BuildGlobalTimeoutManager()
        {
            return new GlobalTimeoutManager(TimeSpan.FromSeconds(this.Timeout));
        }
    }
}
