// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Broker;
    using Microsoft.Identity.Client.Utils;

    /// <summary>
    /// The broker auth flow. Supports Windows (WAM) and macOS (Enterprise SSO Extension).
    /// </summary>
    public class Broker : AuthFlowBase
    {
        private readonly IEnumerable<string> scopes;
        private readonly string preferredDomain;
        private readonly string promptHint;
        private readonly IPCAWrapper pcaWrapper;
        private readonly AuthParameters authParameters;
        private readonly IPlatformUtils platformUtils;

        /// <summary>
        /// The interactive auth timeout.
        /// </summary>
        private TimeSpan interactiveAuthTimeout = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Initializes a new instance of the <see cref="Broker"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="authParameters">The authentication paramaters.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        /// <param name="promptHint">The customized header text in account picker for WAM prompts.</param>
        /// <param name="platformUtils">Optional: IPlatformUtils for platform detection (defaults to runtime detection).</param>
        public Broker(ILogger logger, AuthParameters authParameters, string preferredDomain = null, IPCAWrapper pcaWrapper = null, string promptHint = null, IPlatformUtils platformUtils = null)
        {
            this.logger = logger;
            this.authParameters = authParameters;
            this.scopes = authParameters.Scopes;
            this.preferredDomain = preferredDomain;
            this.promptHint = promptHint;
            this.platformUtils = platformUtils ?? new PlatformUtils(logger);
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(authParameters.Client, authParameters.Tenant);
        }

#if PlatformWindows
        private enum GetAncestorType
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
#endif

        /// <inheritdoc/>
        protected override string Name { get; } = Constants.AuthFlow.Broker;

        /// <inheritdoc/>
        protected override async Task<TokenResult> GetTokenInnerAsync()
        {
            IAccount account = await this.ResolveAccountAsync();

            TokenResult tokenResult = await CachedAuth.GetTokenAsync(
                this.logger,
                this.scopes,
                account,
                this.pcaWrapper,
                this.errors);

            if (tokenResult != null)
            {
                return tokenResult;
            }

            try
            {
                tokenResult = await TaskExecutor.CompleteWithin(
                    this.logger,
                    this.interactiveAuthTimeout,
                    $"{this.Name} interactive auth",
                    this.GetTokenInteractive(account),
                    this.errors).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException ex)
            {
                this.errors.Add(ex);
                this.logger.LogDebug($"initial {this.Name} auth failed. Trying again with claims from exception.\n{ex.Message}");

                tokenResult = await TaskExecutor.CompleteWithin(
                    this.logger,
                    this.interactiveAuthTimeout,
                    $"{this.Name} interactive auth (with extra claims)",
                    this.GetTokenInteractiveWithClaims(ex.Claims),
                    this.errors).ConfigureAwait(false);
            }

            return tokenResult;
        }

        /// <summary>
        /// Resolves the account to use for token acquisition.
        /// On Windows, falls back to OperatingSystemAccount if no cached account.
        /// On macOS, returns null to trigger interactive auth if no cached account.
        /// </summary>
        private async Task<IAccount> ResolveAccountAsync()
        {
            // Try the MSAL cache filtered by preferred domain.
            IAccount account = await this.pcaWrapper.TryToGetCachedAccountAsync(this.preferredDomain);
            if (account != null)
            {
                return account;
            }

            if (this.platformUtils.IsMacOS())
            {
                // On macOS, OperatingSystemAccount is not supported.
                // If MSAL cache has no single matching account, trigger interactive auth.
                return null;
            }

            // On Windows, fall back to OperatingSystemAccount sentinel for WAM resolution.
            return PublicClientApplication.OperatingSystemAccount;
        }

#if PlatformWindows
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
#endif

        private Func<CancellationToken, Task<TokenResult>> GetTokenInteractive(IAccount account)
        {
            return async (CancellationToken cancellationToken) =>
            {
                if (this.platformUtils.IsMacOS() && MacMainThreadScheduler.Instance().IsRunning())
                {
                    TokenResult result = null;
                    await MacMainThreadScheduler.Instance().RunOnMainThreadAsync(async () =>
                    {
                        result = await this.pcaWrapper
                            .WithPromptHint(this.promptHint)
                            .GetTokenInteractiveAsync(this.scopes, account, cancellationToken);
                    });
                    return result;
                }

                return await this.pcaWrapper
                    .WithPromptHint(this.promptHint)
                    .GetTokenInteractiveAsync(this.scopes, account, cancellationToken);
            };
        }

        private Func<CancellationToken, Task<TokenResult>> GetTokenInteractiveWithClaims(string claims)
        {
            return async (CancellationToken cancellationToken) =>
            {
                if (this.platformUtils.IsMacOS() && MacMainThreadScheduler.Instance().IsRunning())
                {
                    TokenResult result = null;
                    await MacMainThreadScheduler.Instance().RunOnMainThreadAsync(async () =>
                    {
                        result = await this.pcaWrapper
                            .WithPromptHint(this.promptHint)
                            .GetTokenInteractiveAsync(this.scopes, claims, cancellationToken);
                    });
                    return result;
                }

                return await this.pcaWrapper
                    .WithPromptHint(this.promptHint)
                    .GetTokenInteractiveAsync(this.scopes, claims, cancellationToken);
            };
        }

#if PlatformWindows
        /// <summary>
        /// Retrieves the handle to the ancestor of the specified window.
        /// </summary>
        /// <param name="windowsHandle">A handle to the window whose ancestor is to be retrieved.
        /// If this parameter is the desktop window, the function returns NULL. </param>
        /// <param name="flags">The ancestor to be retrieved.</param>
        /// <returns>The return value is the handle to the ancestor window.</returns>
        [DllImport("user32.dll", ExactSpelling = true)]
#pragma warning disable SA1204 // Static elements should appear before instance elements
        private static extern IntPtr GetAncestor(IntPtr windowsHandle, GetAncestorType flags);
#pragma warning restore SA1204 // Static elements should appear before instance elements

        private IntPtr GetParentWindowHandle()
        {
            IntPtr consoleHandle = GetConsoleWindow();
            IntPtr ancestorHandle = GetAncestor(consoleHandle, GetAncestorType.GetRootOwner);
            return ancestorHandle;
        }
#endif

        private IPCAWrapper BuildPCAWrapper(Guid clientId, string tenantId)
        {
            var clientBuilder =
                PublicClientApplicationBuilder
                .Create($"{clientId}")
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithLogging(
                    this.LogMSAL,
                    Identity.Client.LogLevel.Verbose,
                    enablePiiLogging: false,
                    enableDefaultPlatformLogging: true);

            if (this.platformUtils.IsMacOS())
            {
                clientBuilder
                    .WithRedirectUri(Constants.MacOSBrokerRedirectUri.ToString())
                    .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.OSX));
            }
            else
            {
#if PlatformWindows
                clientBuilder
                    .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows)
                    {
                        Title = this.promptHint,
                    })
                    .WithParentActivityOrWindow(() => this.GetParentWindowHandle());
#endif
            }

            return new PCAWrapper(this.logger, clientBuilder.Build(), this.errors, tenantId);
        }

        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }
    }
}
