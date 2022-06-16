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
    /// The broker auth flow.
    /// </summary>
    public class IntegratedWindowsAuthentication : IAuthFlow
    {
        private readonly ILogger logger;
        private readonly IEnumerable<string> scopes;
        private readonly string preferredDomain;
        private readonly IList<Exception> errors;
        private IPCAWrapper pcaWrapper;

        #region Public configurable properties

        /// <summary>
        /// The integrated windows auth flow timeout.
        /// </summary>
        private TimeSpan integratedWindowsAuthTimeout = TimeSpan.FromSeconds(6);
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegratedWindowsAuthentication"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        public IntegratedWindowsAuthentication(ILogger logger, Guid clientId, Guid tenantId, IEnumerable<string> scopes, string preferredDomain = null, IPCAWrapper pcaWrapper = null)
        {
            this.errors = new List<Exception>();
            this.logger = logger;
            this.scopes = scopes;
            this.preferredDomain = preferredDomain;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(logger, clientId, tenantId, null);
        }

        /// <summary>
        /// Get a jwt token for a resource.
        /// </summary>
        /// <returns>A <see cref="Task"/> of <see cref="TokenResult"/>.</returns>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            IAccount account = await this.pcaWrapper.TryToGetCachedAccountAsync(this.preferredDomain) ?? null;

            if (account != null)
            {
                this.logger.LogDebug($"Using cached account '{account.Username}'");
                try
                {
                    try
                    {
                        var tokenResult = await TaskExecutor.CompleteWithin(
                            this.logger,
                            this.integratedWindowsAuthTimeout,
                            "Get Token Silent",
                            (cancellationToken) => this.pcaWrapper.GetTokenSilentAsync(this.scopes, account, cancellationToken),
                            this.errors)
                            .ConfigureAwait(false);
                        tokenResult.SetAuthenticationType(AuthType.Silent);

                        return new AuthFlowResult(tokenResult, this.errors);
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        this.errors.Add(ex);
                        this.logger.LogDebug($"Silent auth failed, re-auth is required.\n{ex.Message}");
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
            }
            else
            {
                try
                {
                    var tokenResult = await TaskExecutor.CompleteWithin(
                                    this.logger,
                                    this.integratedWindowsAuthTimeout,
                                    "Get Token Integrated Windows Authentication",
                                    (cancellationToken) => this.pcaWrapper.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, cancellationToken),
                                    this.errors)
                                    .ConfigureAwait(false);
                    tokenResult.SetAuthenticationType(AuthType.IntegratedWindowsAuthenticationFlow);

                    return new AuthFlowResult(tokenResult, this.errors);
                }
                catch (MsalUiRequiredException ex) when (
                             ex.Classification == UiRequiredExceptionClassification.BasicAction
                          && ex.Message.StartsWith("AADSTS50076", StringComparison.OrdinalIgnoreCase))
                {
                    this.errors.Add(ex);
                    this.logger.LogDebug($"IWA failed, 2FA is required.\n{ex.Message}");
                }
                catch (MsalUiRequiredException ex) when (
                             ex.Classification == UiRequiredExceptionClassification.None
                          && ex.Message.StartsWith("AADSTS500083", StringComparison.OrdinalIgnoreCase))
                {
                    this.errors.Add(ex);
                    this.logger.LogDebug($"IWA failed, unable to verify token signature with identifier 'urn:federation:MSFT'.\n{ex.Message}");
                }
                catch (MsalServiceException ex)
                {
                    this.logger.LogWarning($"MSAL Service Exception! (Not expected)\n{ex.Message}");
                    this.errors.Add(ex);
                }
                catch (MsalClientException ex)
                {
                    this.logger.LogWarning($"Msal Client Exception! Could not identify logged in user.\n{ex.Message}");
                    this.errors.Add(ex);
                }
            }

            return new AuthFlowResult(null, this.errors);
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

            return new PCAWrapper(this.logger, clientBuilder.Build(), this.errors, tenantId, null);
        }

        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }
    }
}
