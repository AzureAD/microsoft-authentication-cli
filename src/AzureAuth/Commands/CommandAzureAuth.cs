// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using McMaster.Extensions.CommandLineUtils;
    using Microsoft.Authentication.AzureAuth;

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
    public class CommandAzureAuth
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

        /// <summary>
        /// The default number of minutes CLI is allowed to run.
        /// </summary>
        private static readonly TimeSpan GlobalTimeout = TimeSpan.FromMinutes(15);

        private readonly EventData eventData;
        private readonly ILogger<CommandAzureAuth> logger;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAzureAuth"/> class.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="telemetryService">The telemetry service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="env">The environment interface.</param>
        public CommandAzureAuth(CommandExecuteEventData eventData, ITelemetryService telemetryService, ILogger<CommandAzureAuth> logger, IFileSystem fileSystem, IEnv env)
        {
            this.eventData = eventData;
            this.telemetryService = telemetryService;
            this.logger = logger;
            this.fileSystem = fileSystem;
            this.env = env;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAzureAuth"/> class.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="telemetryService">The telemetry service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="env">The environment interface.</param>
        /// <param name="authFlow">An injected <see cref="IAuthFlow"/> (defined for testability).</param>
        public CommandAzureAuth(CommandExecuteEventData eventData, ITelemetryService telemetryService, ILogger<CommandAzureAuth> logger, IFileSystem fileSystem, IEnv env, IAuthFlow authFlow)
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
        /// Gets or sets global Timeout.
        /// </summary>
        [Option(TimeoutOption, "The number of minutes before authentication times out.\nDefault: 10 minutes.", CommandOptionType.SingleValue)]
        public double Timeout { get; set; } = GlobalTimeout.TotalMinutes;

        /// <summary>
        /// Gets the token fetcher options.
        /// </summary>
        public Alias TokenFetcherOptions
        {
            get { return authSettings; }
        }

        /// <summary>
        /// Gets the CombinedAuthMode depending on env variables to disable interactive auth modes.
        /// </summary>
        public AuthMode CombinedAuthMode
        {
            get
            {
                if (InteractiveAuthDisabled())
                {
#if PlatformWindows
                    return AuthMode.IWA;
#else
                    return 0;
#endif
                }

                return AuthModes.Aggregate((a1, a2) => a1 | a2);
            }
        }

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

            if (result.Errors.Any())
            {
                var error_messages = ExceptionListToStringConverter.Execute(result.Errors);
                eventData.Add("error_messages", error_messages);
            }

            if (result.Success)
            {
                eventData.Add("msal_correlation_id", result.TokenResult.CorrelationID.ToString());
                eventData.Add("token_validity_minutes", result.TokenResult.ValidFor.TotalMinutes);
                eventData.Add("silent", result.TokenResult.IsSilent);
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
                Resource = Resource,
                Client = Client,
                Domain = PreferredDomain,
                Tenant = Tenant,
                PromptHint = PromptHint,
                Scopes = Scopes?.ToList(),
            };

            // We only load options from a config file if an alias is given.
            if (!string.IsNullOrEmpty(AliasName))
            {
                ConfigFilePath = ConfigFilePath ?? env.Get(EnvVars.Config);
                if (string.IsNullOrEmpty(ConfigFilePath))
                {
                    // This is a fatal error. We can't load aliases without a config file.
                    logger.LogError($"The {AliasOption} field was given, but no {ConfigOption} was specified.");
                    return false;
                }

                string fullConfigPath = fileSystem.Path.GetFullPath(ConfigFilePath);

                try
                {
                    Config config = Config.FromFile(fullConfigPath, fileSystem);
                    if (config.Alias is null || !config.Alias.ContainsKey(AliasName))
                    {
                        // This is a fatal error. We can't load a missing alias.
                        logger.LogError($"Alias '{AliasName}' was not found in {ConfigFilePath}");
                        return false;
                    }

                    // Load the requested alias and merge it with any command line options.
                    Alias configFileOptions = config.Alias[AliasName];
                    evaluatedOptions = configFileOptions.Override(evaluatedOptions);
                }
                catch (System.IO.FileNotFoundException)
                {
                    logger.LogError($"The file '{fullConfigPath}' does not exist.");
                    return false;
                }
                catch (Tomlyn.TomlException ex)
                {
                    logger.LogError($"Error parsing TOML in config file at '{fullConfigPath}':\n{ex.Message}");
                    return false;
                }
            }

            // Set the token fetcher options so they can be used later on.
            authSettings = evaluatedOptions;

            // Evaluation is a two-part task. Parse, then validate. Validation is complex, so we call a separate helper.
            return ValidateOptions();
        }

        /// <summary>
        /// This method executes the auth process.
        /// </summary>
        /// <returns>
        /// The error code: 0 is normal execution, and the rest means errors during execution.
        /// </returns>
        public int OnExecute()
        {
            if (!EvaluateOptions())
            {
                eventData.Add("validargs", false);
                return 1;
            }

            eventData.Add("validargs", true);
            eventData.Add("settings_client", authSettings.Client);
            eventData.Add("settings_resource", authSettings.Resource);
            eventData.Add("settings_tenant", authSettings.Tenant);
            eventData.Add("settings_prompthint", authSettings.PromptHint);

            // Small bug in Lasso - Add does not accept a null IEnumerable here.
            eventData.Add("settings_scopes", authSettings.Scopes ?? new List<string>());

            if (InteractiveAuthDisabled())
            {
                eventData.Add(EnvVars.CorextNonInteractive, env.Get(EnvVars.CorextNonInteractive));
                eventData.Add(EnvVars.NoUser, env.Get(EnvVars.NoUser));
                logger.LogWarning($"Interactive authentication is disabled.");
#if PlatformWindows
                logger.LogWarning($"Supported auth mode is Integrated Windows Authentication");
#endif
            }

            return ClearCache ? ClearLocalCache() : GetToken();
        }

        /// <summary>
        /// Determines whether Public Client Authentication (PCA) is disabled or not.
        /// </summary>
        /// <returns>A boolean to indicate PCA is disabled.</returns>
        public bool InteractiveAuthDisabled()
        {
            return !string.IsNullOrEmpty(env.Get(EnvVars.NoUser)) ||
                string.Equals("1", env.Get(EnvVars.CorextNonInteractive));
        }

        private bool ValidateOptions()
        {
            bool validOptions = true;

            int scopesCount = authSettings.Scopes?.Count ?? 0;

            if (string.IsNullOrEmpty(authSettings.Resource) && scopesCount == 0)
            {
                logger.LogError($"The {ResourceOption} field or the {ScopeOption} field is required.");
                validOptions = false;
            }

            if (!string.IsNullOrEmpty(authSettings.Resource) && scopesCount > 0)
            {
                logger.LogWarning($"The {ScopeOption} option was provided with the {ResourceOption} option. Only {ScopeOption} will be used.");
            }

            if (string.IsNullOrEmpty(authSettings.Client))
            {
                logger.LogError($"The {ClientOption} field is required.");
                validOptions = false;
            }

            if (string.IsNullOrEmpty(authSettings.Tenant))
            {
                logger.LogError($"The {TenantOption} field is required.");
                validOptions = false;
            }

            return validOptions;
        }

        private int ClearLocalCache()
        {
            var pca = PublicClientApplicationBuilder.Create(authSettings.Client).Build();
            var pcaWrapper = new PCAWrapper(logger, pca, new List<Exception>(), new Guid(authSettings.Tenant));

            var accounts = pcaWrapper.TryToGetCachedAccountsAsync().Result;
            while (accounts.Any())
            {
                var account = accounts.First();
                logger.LogInformation($"Removing {account.Username} from the cache...");
                pcaWrapper.RemoveAsync(account).Wait();
                accounts = pcaWrapper.TryToGetCachedAccountsAsync().Result;
                logger.LogInformation("Cleared.");
            }

            return 0;
        }

        private int GetToken()
        {
            try
            {
                AuthFlowExecutor authFlowExecutor = AuthFlowExecutor();
                AuthFlowResult successfulResult = null;
                AuthFlowResult[] results = null;

                // When running multiple AzureAuth processes with the same resource, client, and tenant IDs,
                // They may prompt many times, which is annoying and unexpected.
                // Use Mutex to ensure that only one process can access the corresponding resource at the same time.
                string lockName = $"Local\\{Resource}_{Client}_{Tenant}";

                // First parameter InitiallyOwned indicated whether this lock is owned by current thread.
                // It should be false otherwise a dead lock could occur.
                using (Mutex mutex = new Mutex(false, lockName))
                {
                    bool lockAcquired = false;
                    try
                    {
                        // Wait for the other session to exit.
                        lockAcquired = mutex.WaitOne(promptMutexTimeout);
                    }

                    // An AbandonedMutexException could be thrown if another process exits without releasing the mutex correctly.
                    catch (AbandonedMutexException)
                    {
                        // If another process crashes or exits accidently, we can still acquire the lock.
                        lockAcquired = true;

                        // In this case, basicly we can just leave a log warning, because the worst side effect is propmting more than once.
                        logger.LogWarning("The authentication attempt mutex was abandoned. Another thread or process may have exited unexpectedly.");
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
                eventData.Add("error_count", errors.Length);
                eventData.Add("authflow_count", results.Length);

                // Send custom telemetry events for each authflow result.
                SendAuthFlowTelemetryEvents(results);

                if (successfulResult == null)
                {
                    logger.LogError("Authentication failed. Re-run with '--verbosity debug' to get see more info.");
                    return 1;
                }

                var tokenResult = successfulResult.TokenResult;
                eventData.Add("silent", tokenResult.IsSilent);
                eventData.Add("sid", tokenResult.SID);
                eventData.Add("succeeded_mode", successfulResult.AuthFlowName);

                switch (Output)
                {
                    case OutputMode.Status:
                        logger.LogSuccess(tokenResult.ToString());
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
                eventData.Add(ex);
                logger.LogCritical(ex.Message);
                return 1;
            }

            return 0;
        }

        private void SendAuthFlowTelemetryEvents(AuthFlowResult[] results)
        {
            Parallel.ForEach(results, result =>
            {
                var eventData = AuthFlowEventData(result);
                if (eventData != null)
                {
                    telemetryService.SendEvent($"authflow_{result.AuthFlowName}", eventData);
                }
            });
        }

        private AuthFlowExecutor AuthFlowExecutor()
        {
            // TODO: Really we need to get rid of Resource
            var scopes = Scopes ?? new string[] { $"{authSettings.Resource}/.default" };

            IEnumerable<IAuthFlow> authFlows = null;
            if (authFlow != null)
            {
                // if this.authFlow has been injected - use that.
                authFlows = new[] { authFlow };
            }
            else
            {
                // Normal production flow
                authFlows = AuthFlowFactory.Create(
                logger,
                CombinedAuthMode,
                new Guid(authSettings.Client),
                new Guid(authSettings.Tenant),
                scopes,
                PreferredDomain,
                PrefixedPromptHint(authSettings.PromptHint));
            }

            authFlowExecutor = new AuthFlowExecutor(logger, authFlows, StopwatchTracker());

            return authFlowExecutor;
        }

        private IStopwatch StopwatchTracker()
        {
            return new StopwatchTracker(TimeSpan.FromMinutes(Timeout));
        }
    }
}
