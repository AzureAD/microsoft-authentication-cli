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
    /// The web auth flow.
    /// </summary>
    public class Web : IAuthFlow
    {
        private const string NameValue = "web";
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
        /// The interactive auth timeout.
        /// </summary>
        private TimeSpan interactiveAuthTimeout = TimeSpan.FromMinutes(15);
        #endregion

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
            this.errors = new List<Exception>();
            this.logger = logger;
            this.scopes = scopes;
            this.preferredDomain = preferredDomain;
            this.promptHint = promptHint;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(logger, clientId, tenantId);
        }

        /// <inheritdoc/>
        public string Name() => NameValue;

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
                    try
                    {
                        var tokenResult = await TaskExecutor.CompleteWithin(
                            this.logger,
                            this.silentAuthTimeout,
                            "Get Token Silent",
                            (cancellationToken) => this.pcaWrapper.GetTokenSilentAsync(this.scopes, account, cancellationToken),
                            this.errors)
                            .ConfigureAwait(false);
                        tokenResult.SetSilent();

                        return new AuthFlowResult(tokenResult, this.errors, this.GetType().Name);
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        this.errors.Add(ex);
                        this.logger.LogDebug($"Silent auth failed, re-auth is required.\n{ex.Message}");
                        var tokenResult = await TaskExecutor.CompleteWithin(
                            this.logger,
                            this.interactiveAuthTimeout,
                            "Interactive Auth",
                            (cancellationToken) => this.pcaWrapper
                            .WithPromptHint(this.promptHint)
                            .GetTokenInteractiveAsync(this.scopes, account, cancellationToken),
                            this.errors)
                            .ConfigureAwait(false);

                        return new AuthFlowResult(tokenResult, this.errors, this.GetType().Name);
                    }
                }
                catch (MsalUiRequiredException ex)
                {
                    this.errors.Add(ex);
                    this.logger.LogDebug($"Silent auth failed, re-auth is required.\n{ex.Message}");
                    var tokenResult = await TaskExecutor.CompleteWithin(
                        this.logger,
                        this.interactiveAuthTimeout,
                        "Interactive Auth (with extra claims)",
                        (cancellationToken) => this.pcaWrapper
                        .WithPromptHint(this.promptHint)
                        .GetTokenInteractiveAsync(this.scopes, ex.Claims, cancellationToken),
                        this.errors)
                        .ConfigureAwait(false);

                    return new AuthFlowResult(tokenResult, this.errors, this.GetType().Name);
                }
            }
            catch (MsalServiceException ex)
            {
                this.logger.LogWarning($"MSAL Service Exception! (Not expected)\n{ex.Message}");
                this.errors.Add(ex);
            }
            catch (MsalClientException ex)
            {
                this.logger.LogWarning($"MSAL Client Exception! (Not expected)\n{ex.Message}");
                this.errors.Add(ex);
            }
            catch (NullReferenceException ex)
            {
                this.logger.LogWarning($"MSAL unexpected null reference! (Not Expected)\n{ex.Message}");
                this.errors.Add(ex);
            }

            return new AuthFlowResult(null, this.errors, this.GetType().Name);
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
    }
}
