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
    /// The device code auth flow.
    /// </summary>
    public class DeviceCode : AuthFlowBase
    {
        private readonly IEnumerable<string> scopes;

        // TODO: We never check if the account returned from device code matches the preferred domain.
        private readonly string preferredDomain;
        private readonly string promptHint;
        private readonly IPCAWrapper pcaWrapper;

        /// <summary>
        /// The device code flow timeout.
        /// </summary>
        private TimeSpan deviceCodeFlowTimeout = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceCode"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="authParameters">The authentication paramaters.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        /// <param name="promptHint">The customized header text in account picker for WAM prompts.</param>
        public DeviceCode(ILogger logger, AuthParameters authParameters, string preferredDomain = null, IPCAWrapper pcaWrapper = null, string promptHint = null)
        {
            this.logger = logger;
            this.scopes = authParameters.Scopes;
            this.preferredDomain = preferredDomain;
            this.promptHint = promptHint;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(authParameters.Client, authParameters.Tenant);
        }

        /// <inheritdoc/>
        protected override string Name { get; } = Constants.AuthFlow.DeviceCode;

        /// <inheritdoc/>
        protected override async Task<TokenResult> GetTokenInnerAsync()
        {
            this.logger.LogWarning($"Device Code Authentication for: {this.promptHint}");
            TokenResult tokenResult = null;

            tokenResult = await TaskExecutor.CompleteWithin(
                this.logger,
                this.deviceCodeFlowTimeout,
                $"{this.Name} interactive auth",
                this.DeviceCodeAuth,
                this.errors).ConfigureAwait(false);

            return tokenResult;
        }

        private Task<TokenResult> DeviceCodeAuth(CancellationToken cancellationToken)
        {
            return this.pcaWrapper.GetTokenDeviceCodeAsync(
                this.scopes,
                this.ShowDeviceCodeInTty,
                cancellationToken);
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

        private Task ShowDeviceCodeInTty(DeviceCodeResult dcr)
        {
            this.logger.LogWarning(dcr.Message);
            return Task.CompletedTask;
        }
    }
}
