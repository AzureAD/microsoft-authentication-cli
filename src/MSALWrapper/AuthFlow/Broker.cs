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

    /// <summary>
    /// The broker auth flow.
    /// </summary>
    public class Broker : AuthFlowBase
    {
        private const string NameValue = "broker";
        private readonly IEnumerable<string> scopes;
        private readonly string preferredDomain;
        private readonly string promptHint;
        private readonly IPCAWrapper pcaWrapper;

        /// <summary>
        /// The interactive auth timeout.
        /// </summary>
        private TimeSpan interactiveAuthTimeout = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Initializes a new instance of the <see cref="Broker"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        /// <param name="promptHint">The customized header text in account picker for WAM prompts.</param>
        public Broker(ILogger logger, Guid clientId, Guid tenantId, IEnumerable<string> scopes, string preferredDomain = null, IPCAWrapper pcaWrapper = null, string promptHint = null)
        {
            this.logger = logger;
            this.scopes = scopes;
            this.preferredDomain = preferredDomain;
            this.promptHint = promptHint;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(logger, clientId, tenantId);
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

        /// <inheritdoc/>
        protected override string Name() => NameValue;

        /// <inheritdoc/>
        protected override async Task<TokenResult> GetTokenInnerAsync()
        {
            IAccount account = await this.pcaWrapper.TryToGetCachedAccountAsync(this.preferredDomain)
                 ?? PublicClientApplication.OperatingSystemAccount;

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
                    $"{this.Name()} interactive auth",
                    this.GetTokenInteractive(account),
                    this.errors).ConfigureAwait(false);
            }
            catch (MsalUiRequiredException ex)
            {
                this.errors.Add(ex);
                this.logger.LogDebug($"initial {this.Name()} auth failed. Trying again with claims from exception.\n{ex.Message}");

                tokenResult = await TaskExecutor.CompleteWithin(
                    this.logger,
                    this.interactiveAuthTimeout,
                    $"{this.Name()} interactive auth (with extra claims)",
                    this.GetTokenInteractiveWithClaims(ex.Claims),
                    this.errors).ConfigureAwait(false);
            }

            return tokenResult;
        }

        private Func<CancellationToken, Task<TokenResult>> GetTokenInteractive(IAccount account)
        {
            return (CancellationToken cancellationToken) => this.pcaWrapper
                .WithPromptHint(this.promptHint)
                .GetTokenInteractiveAsync(this.scopes, account, cancellationToken);
        }

        private Func<CancellationToken, Task<TokenResult>> GetTokenInteractiveWithClaims(string claims)
        {
            return (CancellationToken cancellationToken) => this.pcaWrapper
                .WithPromptHint(this.promptHint)
                .GetTokenInteractiveAsync(this.scopes, claims, cancellationToken);
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

        private IPCAWrapper BuildPCAWrapper(ILogger logger, Guid clientId, Guid tenantId)
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
                .WithParentActivityOrWindow(() => this.GetParentWindowHandle()) // Pass parent window handle to MSAL so it can parent the authentication dialogs.
                .WithBrokerPreview(); // Use native broker mode.

            return new PCAWrapper(this.logger, clientBuilder.Build(), this.errors, tenantId);
        }

        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }
    }
}
