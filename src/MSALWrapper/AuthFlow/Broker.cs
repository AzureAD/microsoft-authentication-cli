// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
#if NET472
    using Microsoft.Identity.Client.Desktop;
#endif
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The broker auth flow.
    /// </summary>
    public class Broker : IAuthFlow
    {
        private readonly ILogger logger;
        private readonly IEnumerable<string> scopes;
        private readonly string preferredDomain;
        private readonly string promptHint;
        private readonly IList<Exception> errors;
        private IPCAWrapper pcaWrapper;

        #region Public configurable properties

        /// <summary>
        /// The silent auth timeout.
        /// </summary>
        private TimeSpan silentAuthTimeout = TimeSpan.FromSeconds(20);

        /// <summary>
        /// The interactive auth timeout.
        /// </summary>
        private TimeSpan interactiveAuthTimeout = TimeSpan.FromMinutes(15);
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Broker"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="cacheFilePath">The cache file path.</param>
        /// <param name="osxKeyChainSuffix">The osx key chain suffix.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        /// <param name="promptHint">The customized header text in account picker for WAM prompts.</param>
        public Broker(ILogger logger, Guid clientId, Guid tenantId, IEnumerable<string> scopes, string cacheFilePath, string osxKeyChainSuffix = null, string preferredDomain = null, IPCAWrapper pcaWrapper = null, string promptHint = null)
        {
            this.errors = new List<Exception>();
            this.logger = logger;
            this.scopes = scopes;
            this.preferredDomain = preferredDomain;
            this.promptHint = promptHint;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(logger, clientId, tenantId, osxKeyChainSuffix, cacheFilePath);
        }

        private enum GetAncestorFlags
        {
            /// <summary>
            /// Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
            /// </summary>
            GetParent = 1,

            /// <summary>
            /// Retrieves the root window by walking the chain of parent windows.
            /// </summary>
            GetRoot = 2,

            /// <summary>
            /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
            /// </summary>
            GetRootOwner = 3,
        }

        /// <summary>
        /// Get a jwt token for a resource.
        /// </summary>
        /// <returns>A <see cref="Task"/> of <see cref="TokenResult"/>.</returns>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            IAccount account = await this.pcaWrapper.TryToGetCachedAccountAsync(this.preferredDomain)
                ?? Identity.Client.PublicClientApplication.OperatingSystemAccount;
            this.logger.LogDebug($"Using cached account '{account.Username}'");

            try
            {
                try
                {
                    try
                    {
                        var tokenResult = await TaskExecutor.CompleteWithin(
                            this.logger,
                            this.silentAuthTimeout,
                            "Get Token Silent",
                            (cancellationToken) => this.pcaWrapper.GetTokenSilentAsync(this.scopes, account, cancellationToken),
                            this.errors)
                            .ConfigureAwait(false);
                        tokenResult.SetSilent();

                        return new AuthFlowResult(tokenResult, this.errors, this.GetType().Name);
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        this.errors.Add(ex);
                        this.logger.LogDebug($"Silent auth failed, re-auth is required.\n{ex.Message}");
                        var tokenResult = await TaskExecutor.CompleteWithin(
                            this.logger,
                            this.interactiveAuthTimeout,
                            "Interactive Auth",
                            (cancellationToken) => this.pcaWrapper
                            .WithPromptHint(this.promptHint)
                            .GetTokenInteractiveAsync(this.scopes, account, cancellationToken),
                            this.errors)
                            .ConfigureAwait(false);

                        return new AuthFlowResult(tokenResult, this.errors, this.GetType().Name);
                    }
                }
                catch (MsalUiRequiredException ex)
                {
                    this.errors.Add(ex);
                    this.logger.LogDebug($"Silent auth failed, re-auth is required.\n{ex.Message}");
                    var tokenResult = await TaskExecutor.CompleteWithin(
                        this.logger,
                        this.interactiveAuthTimeout,
                        "Interactive Auth (with extra claims)",
                        (cancellationToken) => this.pcaWrapper
                        .WithPromptHint(this.promptHint)
                        .GetTokenInteractiveAsync(this.scopes, ex.Claims, cancellationToken),
                        this.errors)
                        .ConfigureAwait(false);

                    return new AuthFlowResult(tokenResult, this.errors, this.GetType().Name);
                }
            }
            catch (MsalServiceException ex)
            {
                this.logger.LogWarning($"MSAL Service Exception! (Not expected)\n{ex.Message}");
                this.errors.Add(ex);
            }
            catch (MsalClientException ex)
            {
                this.logger.LogWarning($"Msal Client Exception! (Not expected)\n{ex.Message}");
                this.errors.Add(ex);
            }
            catch (NullReferenceException ex)
            {
                this.logger.LogWarning($"Msal unexpected null reference! (Not Expected)\n{ex.Message}");
                this.errors.Add(ex);
            }

            return new AuthFlowResult(null, this.errors, this.GetType().Name);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        /// <summary>
        /// Retrieves the handle to the ancestor of the specified window.
        /// </summary>
        /// <param name="windowsHandle">A handle to the window whose ancestor is to be retrieved.
        /// If this parameter is the desktop window, the function returns NULL. </param>
        /// <param name="flags">The ancestor to be retrieved.</param>
        /// <returns>The return value is the handle to the ancestor window.</returns>[DllImport("user32.dll", ExactSpelling = true)]
        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetAncestor(IntPtr windowsHandle, GetAncestorFlags flags);

        // MSAL will be providing a similar helper in the future that we can use to simplify this(AzureAD/microsoft-authentication-library-for-dotnet#3590).
        private IntPtr GetParentWindowHandle()
        {
            IntPtr consoleHandle = GetConsoleWindow();
            IntPtr ancestorHandle = GetAncestor(consoleHandle, GetAncestorFlags.GetRootOwner);
            return ancestorHandle;
        }

        private IPCAWrapper BuildPCAWrapper(ILogger logger, Guid clientId, Guid tenantId, string osxKeyChainSuffix, string cacheFilePath)
        {
            var clientBuilder =
                PublicClientApplicationBuilder
                .Create($"{clientId}")
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithLogging(
                    this.LogMSAL,
                    Identity.Client.LogLevel.Verbose,
                    enablePiiLogging: false,
                    enableDefaultPlatformLogging: true)
                .WithWindowsBrokerOptions(new WindowsBrokerOptions
                {
                    HeaderText = this.promptHint,
                })
                .WithParentActivityOrWindow(() => this.GetParentWindowHandle()); // Pass parent window handle to MSAL so it can parent the authentication dialogs.
#if NETFRAMEWORK
            clientBuilder.WithWindowsBroker();
#else
            clientBuilder.WithBroker();
#endif
            return new PCAWrapper(this.logger, clientBuilder.Build(), this.errors, tenantId, osxKeyChainSuffix, cacheFilePath);
        }

        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }
    }
}
