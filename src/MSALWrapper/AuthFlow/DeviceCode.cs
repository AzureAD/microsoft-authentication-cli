// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
#if NET472
    using Microsoft.Identity.Client.Desktop;
#endif
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// The device code auth flow.
    /// </summary>
    public class DeviceCode : IAuthFlow
    {
        private readonly ILogger logger;
        private readonly IEnumerable<string> scopes;
        private readonly string preferredDomain;
        private readonly string promptHint;
        private readonly IList<Exception> errors;
        private IPCAWrapper pcaWrapper;
        private int interactivePromptsCount;

        #region Public configurable properties

        /// <summary>
        /// The silent auth timeout.
        /// </summary>
        private TimeSpan silentAuthTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The device code flow timeout.
        /// </summary>
        private TimeSpan deviceCodeFlowTimeout = TimeSpan.FromMinutes(15);
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceCode"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="osxKeyChainSuffix">The osx key chain suffix.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        /// <param name="promptHint">The customized header text in account picker for WAM prompts.</param>
        public DeviceCode(ILogger logger, Guid clientId, Guid tenantId, IEnumerable<string> scopes, string osxKeyChainSuffix = null, string preferredDomain = null, IPCAWrapper pcaWrapper = null, string promptHint = null)
        {
            this.errors = new List<Exception>();
            this.logger = logger;
            this.scopes = scopes;
            this.preferredDomain = preferredDomain;
            this.promptHint = promptHint;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(logger, clientId, tenantId, osxKeyChainSuffix);
        }

        /// <summary>
        /// Get a token for a resource.
        /// </summary>
        /// <returns>A <see cref="Task"/> of <see cref="TokenResult"/>.</returns>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            IAccount account = await this.pcaWrapper.TryToGetCachedAccountAsync(this.preferredDomain) ?? null;

            if (account != null)
            {
                this.logger.LogDebug($"Using cached account '{account.Username}'");
            }

            try
            {
                try
                {
                    var tokenResult = await TaskExecutor.CompleteWithin(
                        this.logger,
                        this.silentAuthTimeout,
                        "Get Token Silent",
                        (cancellationToken) => this.pcaWrapper.GetTokenSilentAsync(
                            this.scopes,
                            account,
                            cancellationToken),
                        this.errors)
                        .ConfigureAwait(false);
                    tokenResult.SetAuthenticationType(AuthType.Silent);

                    return new AuthFlowResult(tokenResult, this.errors, this.interactivePromptsCount);
                }
                catch (MsalUiRequiredException ex)
                {
                    this.errors.Add(ex);
                    this.logger.LogDebug($"Silent auth failed, re-auth is required.\n{ex.Message}");
                    this.interactivePromptsCount += 1;
                    var tokenResult = await TaskExecutor.CompleteWithin(
                        this.logger,
                        this.deviceCodeFlowTimeout,
                        "Get Token using Device Code",
                        (cancellationToken) => this.pcaWrapper.GetTokenDeviceCodeAsync(
                        this.scopes,
                        this.ShowDeviceCodeInTty,
                        cancellationToken),
                        this.errors)
                        .ConfigureAwait(false);
                    tokenResult.SetAuthenticationType(AuthType.DeviceCodeFlow);

                    return new AuthFlowResult(tokenResult, this.errors, this.interactivePromptsCount);
                }
            }
            catch (MsalException ex)
            {
                this.errors.Add(ex);
                this.logger.LogError(ex.Message);
            }

            return new AuthFlowResult(null, this.errors, this.interactivePromptsCount);
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClientHandler handler = new HttpClientHandler();

            var client = new HttpClient(handler);

            // Add default headers
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
            };

            return client;
        }

        private IPCAWrapper BuildPCAWrapper(ILogger logger, Guid clientId, Guid tenantId, string osxKeyChainSuffix)
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

            return new PCAWrapper(this.logger, clientBuilder.Build(), this.errors, tenantId, osxKeyChainSuffix);
        }

        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }

        private Task ShowDeviceCodeInTty(DeviceCodeResult dcr)
        {
            this.logger.LogWarning(dcr.Message);
            return Task.CompletedTask;
        }
    }
}
