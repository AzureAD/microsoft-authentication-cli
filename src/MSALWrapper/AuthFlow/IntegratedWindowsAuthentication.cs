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
    /// The broker auth flow.
    /// </summary>
    public class IntegratedWindowsAuthentication : AuthFlowBase
    {
        private readonly IEnumerable<string> scopes;

        // TODO: We don't currently warn if the account returned from IWA doesn't match the preferred domain.
        private readonly string preferredDomain;
        private readonly IPCAWrapper pcaWrapper;

        /// <summary>
        /// The integrated windows auth flow timeout.
        /// </summary>
        private TimeSpan integratedWindowsAuthTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegratedWindowsAuthentication"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// /// <param name="authParameters">The authentication paramaters.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        public IntegratedWindowsAuthentication(ILogger logger, AuthParameters authParameters, string preferredDomain = null, IPCAWrapper pcaWrapper = null)
        {
            this.logger = logger;
            this.scopes = authParameters.Scopes;
            this.preferredDomain = preferredDomain;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(authParameters.Client, authParameters.Tenant);
        }

        /// <inheritdoc/>
        protected override string Name { get; } = Constants.AuthFlow.Iwa;

        /// <inheritdoc/>
        protected override async Task<TokenResult> GetTokenInnerAsync()
        {
            TokenResult tokenResult = null;

            try
            {
                tokenResult = await TaskExecutor.CompleteWithin(
                    this.logger,
                    this.integratedWindowsAuthTimeout,
                    "Integrated Windows Authentication",
                    this.Iwa,
                    this.errors).ConfigureAwait(false);

                // If IWA worked, it was 100% silent.
                tokenResult.SetSilent();
            }
            catch (MsalUiRequiredException ex) when (
                ex.Classification == UiRequiredExceptionClassification.BasicAction
                && ex.Message.StartsWith("AADSTS50076", StringComparison.OrdinalIgnoreCase))
            {
                this.logger.LogDebug("IWA failed, 2FA is required.");
                throw;
            }
            catch (MsalClientException ex) when (ex.Message.Contains("WS-Trust endpoint not found"))
            {
                this.logger.LogDebug($"IWA only works on corporate AD backed network, AzureAuth is trying to use other auth flows if applicable.");
                this.logger.LogDebug($"Turn on VPN for IWA mode to succeed.");
                throw;
            }

            return tokenResult;
        }

        private async Task<TokenResult> Iwa(CancellationToken cancellationToken)
        {
            return await this.pcaWrapper.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, cancellationToken);
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
    }
}
