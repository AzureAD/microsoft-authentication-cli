// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
#if NET472
    using Microsoft.Identity.Client.Desktop;
#endif
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Extensions.Msal;
    using Microsoft.Identity.Client.Utils.Windows;

    /// <summary>
    /// The token fetcher public client.
    /// </summary>
    public class TokenFetcherPublicClient : ITokenFetcher
    {
        /// <summary>
        /// The oeaut h_ msa l_ disabl e_ cache.
        /// </summary>
        public const string OEAUTH_MSAL_DISABLE_CACHE = "OEAUTH_MSAL_DISABLE_CACHE";

        /// <summary>
        /// The _errors.
        /// </summary>
        public List<Exception> ErrorsList;

        #region Token Cache Configuration

        // OSX
        private const string MacOSAccountName = "MSALCache";
        private const string MacOSServiceName = "Microsoft.Developer.IdentityService";

        // Linux
        private const string LinuxKeyRingSchema = "com.microsoft.identity.tokencache";
        private const string LinuxKeyRingCollection = "default";
        private const string LinuxKeyRingLabel = "MSAL token cache";
        private static KeyValuePair<string, string> linuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", "1");
        private static KeyValuePair<string, string> linuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "Microsoft Develoepr Tools");
        #endregion

        private readonly ILogger logger;

        private readonly string cacheDir;
        private readonly string cacheFileName = "msal.cache";
        private readonly bool cacheDisabled;
        private readonly string osxKeyChainSuffix;
        private readonly bool verifyPersistence;
        private readonly string preferredDomain;
        private readonly bool windows;
        private readonly bool windows10;

        private readonly string promptHint;

        #region Required MSAL GUIDs

        /// <summary>
        /// The Authority.
        /// </summary>
        public readonly string Authority;
        private readonly Guid resourceId;
        private readonly Guid clientId;
        #endregion

        #region Public configurable properties

        /// <summary>
        /// The silent auth timeout.
        /// </summary>
        private TimeSpan silentAuthTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The interactive auth timeout.
        /// </summary>
        private TimeSpan interactiveAuthTimeout = TimeSpan.FromMinutes(15);

        /// <summary>
        /// The device code flow timeout.
        /// </summary>
        private TimeSpan deviceCodeFlowTimeout = TimeSpan.FromMinutes(15);
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenFetcherPublicClient"/> class.
        /// Create an OAuth token fetcher for an AAD public client application.
        /// </summary>
        /// <param name="logger">
        /// A Microsoft.Extensions.Logging.ILogger.
        /// </param>
        /// <param name="resourceId">
        /// The AAD Resource you want to authenticate to (e.g. Azure Devops).
        /// </param>
        /// <param name="clientId">
        /// The AAD App Registration representing the application making the authentication request (e.g. Visual Studio).
        /// </param>
        /// <param name="tenantId">
        /// The tenant Id.
        /// </param>
        /// <param name="osxKeyChainSuffix">
        /// Optionally add a suffix to the OSX keychain token cache to avoid token cache sharing on Mac.
        /// </param>
        /// <param name="preferredDomain">
        /// The preferred Domain.
        /// </param>
        /// <param name="verifyPersistence">
        /// Optionally choose to verify the cache persistence layer when setting up the token cache.
        /// </param>
        /// <param name="promptHint">
        /// The customized header text in account picker for WAM prompts.
        /// </param>
        public TokenFetcherPublicClient(ILogger logger, Guid resourceId, Guid clientId, Guid tenantId, string osxKeyChainSuffix = null, string preferredDomain = null, bool verifyPersistence = false, string promptHint = null)
        {
            this.windows = PlatformUtils.IsWindows(logger);
            this.windows10 = PlatformUtils.IsWindows10(logger);

            this.logger = logger;
            this.Authority = this.GetAuthority(tenantId);
            this.resourceId = resourceId;
            this.clientId = clientId;

            this.promptHint = promptHint;

            this.osxKeyChainSuffix = osxKeyChainSuffix;
            this.verifyPersistence = verifyPersistence;
            this.preferredDomain = preferredDomain;
            this.ErrorsList = new List<Exception>();

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            this.cacheDir = Path.Combine(appData, ".IdentityService");
            this.cacheFileName = $"msal_{tenantId}.cache";
            this.osxKeyChainSuffix = string.IsNullOrWhiteSpace(this.osxKeyChainSuffix) ? $"{tenantId}" : $"{this.osxKeyChainSuffix}.{tenantId}";

            this.cacheDisabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OEAUTH_MSAL_DISABLE_CACHE));

            // Workaround for: WAM interactive dialog (the Account Picker) is immediately canceled when shown in an elevated app
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2560
            if (this.windows10 && WindowsNativeUtils.IsElevatedUser())
            {
                try
                {
                    // This might fail, but we have found WAM to still work in some cases, so let's not throw here. WAM
                    // should in theory fall back to web auth on it's own.
                    WindowsNativeUtils.InitializeProcessSecurity();
                }
                catch (MsalClientException ex)
                {
                    this.logger.LogDebug("TokenFetcherPublicClient: Detected we are running in an elevated process but failed to upgrade process security.");
                    this.logger.LogDebug(ex.Message);
                }
            }
        }

        #endregion

        #region IAdoTokenFetcher Interface Methods

        /// <summary>
        /// The get access token async.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public Task<TokenResult> GetAccessTokenAsync()
        {
            return this.GetAccessTokenAsync(this.DefaultScopes(), AuthMode.Default);
        }

        /// <summary>
        /// The get access token async.
        /// </summary>
        /// <param name="authMode">
        /// The auth mode.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public Task<TokenResult> GetAccessTokenAsync(AuthMode authMode)
        {
            return this.GetAccessTokenAsync(this.DefaultScopes(), authMode);
        }

        /// <summary>
        /// The get access token async.
        /// </summary>
        /// <param name="scopes">
        /// The scopes.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public Task<TokenResult> GetAccessTokenAsync(IEnumerable<string> scopes)
        {
            return this.GetAccessTokenAsync(this.DefaultScopes(), AuthMode.Default);
        }

        /// <summary>
        /// The ErrorsList.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        public IEnumerable<Exception> Errors() => this.ErrorsList;

        /// <summary>
        /// The get access token async.
        /// </summary>
        /// <param name="scopes">
        /// The scopes.
        /// </param>
        /// <param name="authMode">
        /// The auth mode.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<TokenResult> GetAccessTokenAsync(IEnumerable<string> scopes, AuthMode authMode)
        {
            TokenResult result = null;
            if (this.windows10 && authMode.IsBroker())
            {
                this.logger.LogDebug("Trying WAM Broker flow.");
                var pca = this.PCABroker();
                var pcaWrapper = new PCAWrapper(this.logger, pca);
                IAccount account = await this.TryToGetCachedAccountAsync(pca, this.preferredDomain).ConfigureAwait(false) ?? PublicClientApplication.OperatingSystemAccount;
                result = await this.GetTokenNormalFlowAsync(pcaWrapper, scopes, account);
            }

            if (result is null && authMode.IsWeb())
            {
                this.logger.LogDebug("Trying Web Auth flow.");
                var pca = this.PCAWeb();
                var pcaWrapper = new PCAWrapper(this.logger, pca);
                IAccount account = await this.TryToGetCachedAccountAsync(pca, this.preferredDomain).ConfigureAwait(false) ?? null;
                result = await this.GetTokenNormalFlowAsync(pcaWrapper, scopes, account);
            }

            if (result is null && authMode.IsDeviceCode())
            {
                var pca = this.PCAWeb();
                var pcaWrapper = new PCAWrapper(this.logger, pca);
                this.logger.LogTrace("Trying Device Code flow.");
                try
                {
                    result = await this.CompleteWithin(
                        this.deviceCodeFlowTimeout,
                        "device code flow",
                        (cancellationToken) => pcaWrapper.GetTokenDeviceCodeAsync(
                            scopes,
                            this.ShowDeviceCodeInTty,
                            cancellationToken))
                        .ConfigureAwait(false);
                    this.SetAuthenticationType(result, AuthType.DeviceCodeFlow);
                }
                catch (MsalException ex)
                {
                    this.logger.LogError(ex.Message);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task ClearCacheAsync()
        {
            // Implementation copied from the WAM docs
            // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/clearing-token-cache

            // You cannot clear OS accounts (used by the broker) so when clearing the cache, we always want the web broker.
            var pca = this.PCAWeb();
            var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            while (accounts.Any())
            {
                var account = accounts.First();
                this.logger.LogInformation($"Clearing cache for {account.Username}...");
                await pca.RemoveAsync(account);
                accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                this.logger.LogInformation("Cleared.");
            }
        }

        #endregion

        /// <summary>
        /// The get token normal flow async.
        /// </summary>
        /// <param name="pcaWrapper">
        /// The pca wrapper.
        /// </param>
        /// <param name="scopes">
        /// The scopes.
        /// </param>
        /// <param name="account">
        /// The account.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<TokenResult> GetTokenNormalFlowAsync(IPCAWrapper pcaWrapper, IEnumerable<string> scopes, IAccount account)
        {
            if (account != null)
            {
                this.logger.LogDebug($"GetTokenNormalFlowAsync: Using account '{account.Username}'");
            }
            else
            {
                this.logger.LogDebug("GetTokenNormalFlowAsync: No account given, will prompt");
            }

            try
            {
                try
                {
                    try
                    {
                        var tokenResult = await this.CompleteWithin(
                            this.silentAuthTimeout,
                            "Get Token Silent",
                            (cancellationToken) => pcaWrapper.GetTokenSilentAsync(scopes, account, cancellationToken))
                            .ConfigureAwait(false);
                        this.SetAuthenticationType(tokenResult, AuthType.Silent);

                        return tokenResult;
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        this.ErrorsList.Add(ex);
                        this.logger.LogDebug($"Silent auth failed, re-auth is required.\n{ex.Message}");
                        var tokenResult = await this.CompleteWithin(
                            this.interactiveAuthTimeout,
                            "Interactive Auth",
                            (cancellationToken) => pcaWrapper
                            .WithPromptHint(this.promptHint)
                            .GetTokenInteractiveAsync(scopes, account, cancellationToken)) // TODO: Need to pass account here
                            .ConfigureAwait(false);
                        this.SetAuthenticationType(tokenResult, AuthType.Interactive);
                        return tokenResult;
                    }
                }
                catch (MsalUiRequiredException ex)
                {
                    this.ErrorsList.Add(ex);
                    this.logger.LogDebug($"Silent auth failed, re-auth is required.\n{ex.Message}");
                    var tokenResult = await this.CompleteWithin(
                        this.interactiveAuthTimeout,
                        "Interactive Auth (with extra claims)",
                        (cancellationToken) => pcaWrapper
                        .WithPromptHint(this.promptHint)
                        .GetTokenInteractiveAsync(scopes, ex.Claims, cancellationToken))
                        .ConfigureAwait(false);
                    this.SetAuthenticationType(tokenResult, AuthType.Interactive);
                    return tokenResult;
                }
            }
            catch (MsalServiceException ex)
            {
                this.logger.LogWarning($"MSAL Service Exception! (Not expected)\n{ex.Message}");
                this.ErrorsList.Add(ex);
                return null;
            }
            catch (MsalClientException ex)
            {
                this.logger.LogWarning($"Msal Client Exception! (Not expected)\n{ex.Message}");
                this.ErrorsList.Add(ex);
                return null;
            }
            catch (NullReferenceException ex)
            {
                this.logger.LogWarning($"Msal unexpected null reference! (Not Expected)\n{ex.Message}");
                this.ErrorsList.Add(ex);
                return null;
            }
        }

        /// <summary>
        /// The try to get cached account async.
        /// </summary>
        /// <param name="pca">
        /// The pca.
        /// </param>
        /// <param name="preferredDomain">
        /// The preferred domain.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<IAccount> TryToGetCachedAccountAsync(IPublicClientApplication pca, string preferredDomain = null)
        {
            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            if (accounts == null)
            {
                return null;
            }

            this.logger.LogDebug($"Accounts found in cache: ({accounts.Count()}):");
            this.logger.LogDebug(string.Join("\n", accounts.Select(a => a.Username)));

            if (!string.IsNullOrWhiteSpace(preferredDomain))
            {
                this.logger.LogDebug($"Filtering cached accounts with preferred domain '{preferredDomain}'");
                accounts = accounts.Where(account => account.Username.EndsWith(preferredDomain, StringComparison.OrdinalIgnoreCase));

                this.logger.LogDebug($"Accounts found in cache after filtering: ({accounts.Count()}):");
                this.logger.LogDebug(string.Join("\n", accounts.Select(a => a.Username)));
            }

            return accounts.Count() > 1 ? null : accounts.FirstOrDefault();
        }

        /// <summary>
        /// The method that returns authority.
        /// </summary>
        /// <param name="tenantId">
        /// THe tenant.
        /// </param>
        /// <returns>
        /// The authority is returned.
        /// </returns>
        public string GetAuthority(Guid tenantId)
        {
            return $"https://login.microsoftonline.com/{tenantId}";
        }

        /// <summary>
        /// THe default scope.
        /// </summary>
        /// <returns>
        /// Return an array of scopes.
        /// </returns>
        protected virtual string[] DefaultScopes() => new string[] { $"{this.resourceId}/.default" };

        /// <summary>
        /// The build scope.
        /// </summary>
        /// <param name="scope">
        /// The scope.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        protected string BuildScope(string scope) => $"{this.resourceId}/{scope}";

        private static HttpClient CreateHttpClient()
        {
            HttpClientHandler handler = new HttpClientHandler();

            var client = new HttpClient(handler);

            // Add default headers
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
            };

            return client;
        }

        private IPublicClientApplication PCAWeb()
        {
            var httpFactoryAdaptor = new MsalHttpClientFactoryAdaptor(CreateHttpClient());
            var pca = this.PCABase()
                .WithHttpClientFactory(httpFactoryAdaptor)
                .WithRedirectUri(Constants.AadRedirectUri.ToString())
                .Build();

            this.SetupTokenCache(pca);
            return pca;
        }

        private IPublicClientApplication PCABroker()
        {
            var pcaBuilder = this.PCABase();
            pcaBuilder.WithWindowsBrokerOptions(new WindowsBrokerOptions
            {
                HeaderText = this.promptHint,
            });

#if NETFRAMEWORK
            pcaBuilder.WithWindowsBroker();
#else
            pcaBuilder.WithBroker();
#endif
            var pca = pcaBuilder.Build();
            this.SetupTokenCache(pca);
            return pca;
        }

        private PublicClientApplicationBuilder PCABase()
        {
            return PublicClientApplicationBuilder
                .Create(this.clientId.ToString())
                .WithAuthority(this.Authority)
                .WithLogging(
                    this.LogMSAL,
                    Identity.Client.LogLevel.Verbose,
                    enablePiiLogging: false,
                    enableDefaultPlatformLogging: true);
        }

        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }

        private void SetupTokenCache(IPublicClientApplication pca)
        {
            if (this.cacheDisabled)
            {
                return;
            }

            var osxKeychainItem = MacOSServiceName + (string.IsNullOrWhiteSpace(this.osxKeyChainSuffix) ? string.Empty : $".{this.osxKeyChainSuffix}");
            var storageProperties = new StorageCreationPropertiesBuilder(this.cacheFileName, this.cacheDir)
            .WithLinuxKeyring(LinuxKeyRingSchema, LinuxKeyRingCollection, LinuxKeyRingLabel, linuxKeyRingAttr1, linuxKeyRingAttr2)
            .WithMacKeyChain(osxKeychainItem, MacOSAccountName)
            .Build();

            try
            {
                MsalCacheHelper cacher = MsalCacheHelper.CreateAsync(storageProperties).Result;
                if (this.verifyPersistence)
                {
                    cacher.VerifyPersistence();
                }

                cacher.RegisterCache(pca.UserTokenCache);
            }
            catch (MsalCachePersistenceException ex)
            {
                this.logger.LogWarning($"MSAL token cache verification failed.\n{ex.Message}\n");
                this.ErrorsList.Add(ex);
            }
            catch (AggregateException ex) when (ex.InnerException.Message.Contains("Could not get access to the shared lock file"))
            {
                var exceptionMessage = ex.ToFormattedString();
                this.ErrorsList.Add(ex);

                this.logger.LogError("An unexpected error occured creating the cache.");
                throw new Exception(exceptionMessage);
            }
        }

        private Task ShowDeviceCodeInTty(DeviceCodeResult dcr)
        {
            this.logger.LogWarning(dcr.Message);
            return Task.CompletedTask;
        }

        private async Task<T> CompleteWithin<T>(TimeSpan timeout, string taskName, Func<CancellationToken, Task<T>> getTask)
            where T : class
        {
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeout);
            try
            {
                this.logger.LogDebug($"{taskName} has {timeout.TotalMinutes} minutes to complete before timeout.");
                return await getTask(source.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                var warningMessage = $"{taskName} timed out after {timeout.TotalMinutes} minutes.";
                this.logger.LogWarning(warningMessage);
                this.ErrorsList.Add(new AuthenticationTimeoutException(warningMessage));
                return null;
            }
        }

        private void SetAuthenticationType(TokenResult result, AuthType authType)
        {
            if (result != null)
            {
                result.AuthType = authType;
            }
        }

        private class MsalHttpClientFactoryAdaptor : IMsalHttpClientFactory
        {
            private HttpClient instance;

            public MsalHttpClientFactoryAdaptor(HttpClient instance)
            {
                if (instance == null)
                {
                    throw new ArgumentNullException(nameof(instance));
                }

                this.instance = instance;
            }

            public HttpClient GetHttpClient()
            {
                // MSAL calls this method each time it wants to use an HTTP client.
                // We ensure we only create a single instance to avoid socket exhaustion.
                return this.instance;
            }
        }
    }
}
