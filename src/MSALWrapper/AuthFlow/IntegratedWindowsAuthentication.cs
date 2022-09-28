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
        private TimeSpan integratedWindowsAuthTimeout = TimeSpan.FromSeconds(15);
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegratedWindowsAuthentication"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="cacheFilePath">The pca cache file path.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        public IntegratedWindowsAuthentication(ILogger logger, Guid clientId, Guid tenantId, IEnumerable<string> scopes, string cacheFilePath, string preferredDomain = null, IPCAWrapper pcaWrapper = null)
        {
            this.errors = new List<Exception>();
            this.logger = logger;
            this.scopes = scopes;
            this.preferredDomain = preferredDomain;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(logger, clientId, tenantId, null, cacheFilePath);
        }

        /// <summary>
        /// Get a jwt token for a resource.
        /// </summary>
        /// <returns>A <see cref="Task"/> of <see cref="TokenResult"/>.</returns>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            IAccount account = await this.pcaWrapper.TryToGetCachedAccountAsync(this.preferredDomain) ?? null;
            this.logger.LogDebug($"Using cached account '{account?.Username}'");
            TokenResult tokenResult = null;

            try
            {
                try
                {
                    tokenResult = await TaskExecutor.CompleteWithin(
                        this.logger,
                        this.integratedWindowsAuthTimeout,
                        "Get Token Silent",
                        (cancellationToken) => this.pcaWrapper.GetTokenSilentAsync(this.scopes, account, cancellationToken),
                        this.errors)
                        .ConfigureAwait(false);
                    tokenResult.SetSilent();
                    if (tokenResult == null)
                    {
                        this.errors.Add(new NullTokenResultException("IWA Get Token Silent returned null.(Not expected)"));
                    }
                }
                catch (MsalUiRequiredException ex)
                {
                    this.errors.Add(ex);
                    this.logger.LogDebug("Cached auth failed.");
                    this.logger.LogDebug(ex.Message);
                    tokenResult = await TaskExecutor.CompleteWithin(
                                  this.logger,
                                  this.integratedWindowsAuthTimeout,
                                  "Get Token Integrated Windows Authentication",
                                  (cancellationToken) => this.pcaWrapper.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, cancellationToken),
                                  this.errors)
                                  .ConfigureAwait(false);
                    tokenResult.SetSilent();
                }
            }
            catch (MsalUiRequiredException ex)
            {
                this.errors.Add(ex);
                if (ex.Classification == UiRequiredExceptionClassification.BasicAction
                      && ex.Message.StartsWith("AADSTS50076", StringComparison.OrdinalIgnoreCase))
                {
                    this.logger.LogWarning("IWA failed, 2FA is required.");
                    this.logger.LogWarning("IWA can pass this requirement if you log into Windows with either a Smart Card or Windows Hello.");
                    this.logger.LogWarning(ex.Message);
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

            return new AuthFlowResult(tokenResult, this.errors, this.GetType().Name);
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
                    enableDefaultPlatformLogging: true);

            return new PCAWrapper(this.logger, clientBuilder.Build(), this.errors, tenantId, null, cacheFilePath);
        }

        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }
    }
}
