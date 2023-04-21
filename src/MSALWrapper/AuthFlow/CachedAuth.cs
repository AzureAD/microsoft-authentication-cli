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
    /// This class provides an abstraction over how to use the MSAL Cached Auth (known as GetTokenSilent).
    /// </summary>
    public class CachedAuth : AuthFlowBase
    {
        private const string NameValue = "cache";
        private static readonly TimeSpan CachedAuthTimeout = TimeSpan.FromSeconds(30);
        private readonly IEnumerable<string> scopes;
        private readonly string preferredDomain;
        private readonly IPCAWrapper pcaWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedAuth"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        public CachedAuth(ILogger logger, Guid clientId, Guid tenantId, IEnumerable<string> scopes, string preferredDomain = null, IPCAWrapper pcaWrapper = null)
        {
            this.logger = logger;
            this.scopes = scopes;
            this.preferredDomain = preferredDomain;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(clientId, tenantId);
        }

        /// <inheritdoc/>
        protected override string Name() => NameValue;

        /// <inheritdoc/>
        protected override async Task<TokenResult> GetTokenInnerAsync()
        {
            IAccount account = await this.pcaWrapper.TryToGetCachedAccountAsync(this.preferredDomain);
            if (account == null)
            {
                this.logger.LogDebug("No cached account found!");
                return null;
            }

            this.logger.LogDebug($"Using cached account '{account.Username}'");

            TokenResult tokenResult = await TaskExecutor.CompleteWithin(
                this.logger,
                CachedAuthTimeout,
                "Get Token Silent",
                (cancellationToken) => this.pcaWrapper.GetTokenSilentAsync(this.scopes, account, cancellationToken),
                this.errors).ConfigureAwait(false);

            tokenResult.SetSilent();

            return tokenResult;
        }

        private IPCAWrapper BuildPCAWrapper(Guid clientId, Guid tenantId)
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

            return new PCAWrapper(this.logger, clientBuilder.Build(), this.errors, tenantId);
        }

        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }

        /// <summary>
        /// Try to get a token silently.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger"/>.</param>
        /// <param name="cachedAuthTimeout">The timeout for getting a cached token.</param>
        /// <param name="scopes">Auth Scopes.</param>
        /// <param name="account"><see cref="IAccount"/> object to use or null if no account has been found.</param>
        /// <param name="pcaWrapper">An <see cref="IPCAWrapper"/>.</param>
        /// <param name="errors">List of errors to report any Exceptions into.</param>
        /// <returns>A <see cref="Task{TResult}"/> or null if no cached token was acquired.</returns>
        public static async Task<TokenResult> GetTokenAsync(ILogger logger, IEnumerable<string> scopes, IAccount account, IPCAWrapper pcaWrapper, IList<Exception> errors)
        {
            if (account == null)
            {
                logger.LogDebug("No cached account");
                return null;
            }

            logger.LogDebug($"Using cached account '{account.Username}'");

            TokenResult tokenResult = null;
            try
            {
                tokenResult = await TaskExecutor.CompleteWithin(
                                logger,
                                CachedAuthTimeout,
                                "Get Token Silent",
                                (cancellationToken) => pcaWrapper.GetTokenSilentAsync(scopes, account, cancellationToken),
                                errors)
                                .ConfigureAwait(false);
                tokenResult.SetSilent();
            }
            catch (MsalUiRequiredException ex)
            {
                errors.Add(ex);
                logger.LogDebug($"Cached Auth failed:\n{ex.Message}");
            }

            return tokenResult;
        }
    }
}
