// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The device code auth flow.
    /// </summary>
    public class DeviceCode : AuthFlowBase
    {
        private const string NameValue = "devicecode";
        private readonly ILogger logger;
        private readonly IEnumerable<string> scopes;
        private readonly string preferredDomain;
        private readonly string promptHint;
        private readonly IList<Exception> errors;
        private IPCAWrapper pcaWrapper;

        #region Public configurable properties

        /// <summary>
        /// The silent auth timeout.
        /// </summary>
        private TimeSpan silentAuthTimeout = TimeSpan.FromSeconds(15);

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
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        /// <param name="promptHint">The customized header text in account picker for WAM prompts.</param>
        public DeviceCode(ILogger logger, Guid clientId, Guid tenantId, IEnumerable<string> scopes, string preferredDomain = null, IPCAWrapper pcaWrapper = null, string promptHint = null)
        {
            this.errors = new List<Exception>();
            this.logger = logger;
            this.scopes = scopes;
            this.preferredDomain = preferredDomain;
            this.promptHint = promptHint;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(logger, clientId, tenantId);
        }

        /// <inheritdoc/>
        protected override string Name() => NameValue;

        /// <inheritdoc/>
        protected override async Task<(TokenResult, IList<Exception>)> GetTokenInnerAsync()
        {
            IAccount account = await this.pcaWrapper.TryToGetCachedAccountAsync(this.preferredDomain);
            TokenResult tokenResult = null;

            try
            {
                tokenResult = await CachedAuth.GetTokenAsync(
                    this.logger,
                    this.scopes,
                    account,
                    this.pcaWrapper,
                    this.errors);

                if (tokenResult != null)
                {
                    return (tokenResult, this.errors);
                }

                this.logger.LogWarning($"Device Code Authentication for: {this.promptHint}");

                Func<System.Threading.CancellationToken, Task<TokenResult>> deviceCodeAuth = (cancellationToken) =>
                    this.pcaWrapper.GetTokenDeviceCodeAsync(
                        this.scopes,
                        this.ShowDeviceCodeInTty,
                        cancellationToken);

                tokenResult = await TaskExecutor.CompleteWithin(
                    this.logger,
                    this.deviceCodeFlowTimeout,
                    $"{this.Name()} interactive auth",
                    deviceCodeAuth,
                    this.errors).ConfigureAwait(false);
            }
            catch (MsalException ex)
            {
                this.errors.Add(ex);
                this.logger.LogError(ex.Message);
            }

            return (tokenResult, this.errors);
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

        private IPCAWrapper BuildPCAWrapper(ILogger logger, Guid clientId, Guid tenantId)
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

        private Task ShowDeviceCodeInTty(DeviceCodeResult dcr)
        {
            this.logger.LogWarning(dcr.Message);
            return Task.CompletedTask;
        }
    }
}
