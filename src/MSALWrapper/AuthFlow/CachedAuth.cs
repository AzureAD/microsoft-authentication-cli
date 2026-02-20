// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    /// <summary>
    /// This class provides an abstraction over how to use the MSAL Cached Auth (known as GetTokenSilent).
    /// </summary>
    public class CachedAuth : AuthFlowBase
    {
        private static readonly TimeSpan CachedAuthTimeout = TimeSpan.FromSeconds(30);
        private readonly IEnumerable<string> scopes;
        private readonly string preferredDomain;
        private readonly IPCAWrapper pcaWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedAuth"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="authParameters">The authentication paramaters.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        public CachedAuth(ILogger logger, AuthParameters authParameters, string preferredDomain = null, IPCAWrapper pcaWrapper = null)
        {
            this.logger = logger;
            this.scopes = authParameters.Scopes;
            this.preferredDomain = preferredDomain;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(authParameters.Client, authParameters.Tenant);
        }

        /// <inheritdoc/>
        protected override string Name { get; } = Constants.AuthFlow.CachedAuth;

        /// <summary>
        /// Try to get a token silently.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger"/>.</param>
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

        /// <inheritdoc/>
        protected override async Task<TokenResult> GetTokenInnerAsync()
        {
            IAccount account = await this.pcaWrapper.TryToGetCachedAccountAsync(this.preferredDomain);
            return await GetTokenAsync(this.logger, this.scopes, account, this.pcaWrapper, this.errors);
        }

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

            return new PCAWrapper(this.logger, clientBuilder.Build(), this.errors, tenantId);
        }

        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }
    }
}
