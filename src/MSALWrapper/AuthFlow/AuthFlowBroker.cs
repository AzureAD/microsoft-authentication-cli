// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlows
{
#if NET472
    using Microsoft.Identity.Client.Desktop;
#endif
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The broker auth flow.
    /// </summary>
    public class AuthFlowBroker : IAuthFlow
    {
        private readonly ILogger logger;
        private readonly IEnumerable<string> scopes;
        private readonly string preferredDomain;
        private IPCAWrapper pcaWrapper;

        #region Public configurable properties

        /// <summary>
        /// The silent auth timeout.
        /// </summary>
        private TimeSpan silentAuthTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The interactive auth timeout.
        /// </summary>
        private TimeSpan interactiveAuthTimeout = TimeSpan.FromMinutes(15);
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthFlowBroker"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="osxKeyChainSuffix">The osx key chain suffix.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        public AuthFlowBroker(ILogger logger, Guid clientId, Guid tenantId, IEnumerable<string> scopes, string osxKeyChainSuffix = null, string preferredDomain = null, IPCAWrapper pcaWrapper = null)
        {
            this.ErrorsList = new List<Exception>();
            this.logger = logger;
            this.scopes = scopes;
            this.preferredDomain = preferredDomain;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(logger, clientId, tenantId, osxKeyChainSuffix);
        }

        /// <summary>
        /// Gets the errors list.
        /// </summary>
        public IList<Exception> ErrorsList { get; }

        /// <summary>
        /// Gets the jwt token for a resource.
        /// </summary>
        /// <returns>A <see cref="Task"/> of <see cref="TokenResult"/>.</returns>
        public async Task<TokenResult> GetTokenAsync()
        {
            IAccount account = await this.pcaWrapper.TryToGetCachedAccountAsync(this.preferredDomain)
                ?? Identity.Client.PublicClientApplication.OperatingSystemAccount;
            this.logger.LogDebug($"GetTokenNormalFlowAsync: Using account '{account.Username}'");

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
                            this.ErrorsList)
                            .ConfigureAwait(false);
                        tokenResult.SetAuthenticationType(AuthType.Silent);

                        return tokenResult;
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        this.ErrorsList.Add(ex);
                        this.logger.LogDebug($"Silent auth failed, re-auth is required.\n{ex.Message}");
                        var tokenResult = await TaskExecutor.CompleteWithin(
                            this.logger,
                            this.interactiveAuthTimeout,
                            "Interactive Auth",
                            (cancellationToken) => this.pcaWrapper.GetTokenInteractiveAsync(this.scopes, account, cancellationToken),
                            this.ErrorsList)
                            .ConfigureAwait(false);
                        tokenResult.SetAuthenticationType(AuthType.Interactive);
                        return tokenResult;
                    }
                }
                catch (MsalUiRequiredException ex)
                {
                    this.ErrorsList.Add(ex);
                    this.logger.LogDebug($"Silent auth failed, re-auth is required.\n{ex.Message}");
                    var tokenResult = await TaskExecutor.CompleteWithin(
                        this.logger,
                        this.interactiveAuthTimeout,
                        "Interactive Auth (with extra claims)",
                        (cancellationToken) => this.pcaWrapper.GetTokenInteractiveAsync(this.scopes, ex.Claims, cancellationToken),
                        this.ErrorsList)
                        .ConfigureAwait(false);
                    tokenResult.SetAuthenticationType(AuthType.Interactive);
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

        private IPCAWrapper BuildPCAWrapper(ILogger logger, Guid clientId, Guid tenantId, string osxKeyChainSuffix)
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

#if NETFRAMEWORK
            clientBuilder.WithWindowsBroker();
#else
            clientBuilder.WithBroker();
#endif
            return new PCAWrapper(this.logger, clientBuilder.Build(), this.ErrorsList, tenantId, osxKeyChainSuffix);
        }

        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }
    }
}
