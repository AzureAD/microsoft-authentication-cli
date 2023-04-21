// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The web auth flow.
    /// </summary>
    public class Web : AuthFlowBase
    {
        private const string NameValue = "web";
        private readonly IEnumerable<string> scopes;
        private readonly string preferredDomain;
        private readonly string promptHint;
        private readonly IPCAWrapper pcaWrapper;

        /// <summary>
        /// The interactive auth timeout.
        /// </summary>
        private TimeSpan interactiveAuthTimeout = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Initializes a new instance of the <see cref="Web"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        /// <param name="promptHint">The customized header text in account picker for WAM prompts.</param>
        public Web(ILogger logger, Guid clientId, Guid tenantId, IEnumerable<string> scopes, string preferredDomain = null, IPCAWrapper pcaWrapper = null, string promptHint = null)
        {
            this.logger = logger;
            this.scopes = scopes;
            this.preferredDomain = preferredDomain;
            this.promptHint = promptHint;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(clientId, tenantId);
        }

        /// <inheritdoc/>
        protected override string Name() => NameValue;

        /// <inheritdoc/>
        protected override async Task<TokenResult> GetTokenInnerAsync()
        {
            var account = await this.pcaWrapper.TryToGetCachedAccountAsync(this.preferredDomain);
            TokenResult tokenResult = null;

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
                // It would be nice to use 'when (!string.IsNullOrEmpty(ex.Claims))' as a catch clause above
                // but we can't actually create an MsalUiRequiredException with a non-null Claims property.
                // It's a read only field and so we would not be able to orchestrate this usage under test :(.
                this.errors.Add(ex);
                this.logger.LogDebug($"Initial ${this.Name()} auth failed. Trying again with claims.\n{ex.Message}");

                tokenResult = await TaskExecutor.CompleteWithin(
                    this.logger,
                    this.interactiveAuthTimeout,
                    "Interactive Auth (with extra claims)",
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

        private IPCAWrapper BuildPCAWrapper(Guid clientId, Guid tenantId)
        {
            var httpFactoryAdaptor = new MsalHttpClientFactoryAdaptor();
            var clientBuilder =
                PublicClientApplicationBuilder
                .Create($"{clientId}")
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .WithLogging(
                    this.LogMSAL,
                    Identity.Client.LogLevel.Verbose,
                    enablePiiLogging: false,
                    enableDefaultPlatformLogging: true)
                .WithHttpClientFactory(httpFactoryAdaptor)
                .WithRedirectUri(Constants.AadRedirectUri.ToString());

            return new PCAWrapper(this.logger, clientBuilder.Build(), this.errors, tenantId);
        }

        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }
    }
}
