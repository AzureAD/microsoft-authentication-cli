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
    /// The web auth flow.
    /// </summary>
    public class Web : IAuthFlow
    {
        private readonly ILogger logger;
        private readonly IEnumerable<string> scopes;
        private readonly string preferredDomain;
        private readonly string promptHint;
        private readonly IList<Exception> errors;
        private readonly EventData eventData;
        private readonly IList<string> correlationIDs;
        private IPCAWrapper pcaWrapper;
        private int interactivePromptsCount;

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
        /// Initializes a new instance of the <see cref="Web"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="osxKeyChainSuffix">The osx key chain suffix.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="pcaWrapper">Optional: IPCAWrapper to use.</param>
        /// <param name="promptHint">The customized header text in account picker for WAM prompts.</param>
        public Web(ILogger logger, Guid clientId, Guid tenantId, IEnumerable<string> scopes, string osxKeyChainSuffix = null, string preferredDomain = null, IPCAWrapper pcaWrapper = null, string promptHint = null)
        {
            this.errors = new List<Exception>();
            this.logger = logger;
            this.scopes = scopes;
            this.preferredDomain = preferredDomain;
            this.promptHint = promptHint;
            this.pcaWrapper = pcaWrapper ?? this.BuildPCAWrapper(logger, clientId, tenantId, osxKeyChainSuffix);
            this.eventData = new EventData();
            this.correlationIDs = new List<string>();
        }

        /// <summary>
        /// Get a token for a resource.
        /// </summary>
        /// <returns>A <see cref="Task"/> of <see cref="TokenResult"/>.</returns>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            this.eventData.Add("auth_mode", "Web");
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
                        tokenResult.SetAuthenticationType(AuthType.Silent);
                        this.correlationIDs.Add(tokenResult.CorrelationID.ToString());
                        this.PopulateEventData();

                        return new AuthFlowResult(tokenResult, this.errors);
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        this.errors.Add(ex);
                        this.correlationIDs.Add(ex.CorrelationId?.ToString());
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
                        tokenResult.SetAuthenticationType(AuthType.Interactive);
                        this.interactivePromptsCount += 1;
                        this.PopulateEventData();

                        return new AuthFlowResult(tokenResult, this.errors);
                    }
                }
                catch (MsalUiRequiredException ex)
                {
                    this.errors.Add(ex);
                    this.correlationIDs.Add(ex.CorrelationId?.ToString());
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
                    tokenResult.SetAuthenticationType(AuthType.Interactive);
                    this.interactivePromptsCount += 1;
                    this.PopulateEventData();

                    return new AuthFlowResult(tokenResult, this.errors);
                }
            }
            catch (MsalServiceException ex)
            {
                this.logger.LogWarning($"MSAL Service Exception! (Not expected)\n{ex.Message}");
                this.errors.Add(ex);
                this.PopulateEventData();
            }
            catch (MsalClientException ex)
            {
                this.logger.LogWarning($"MSAL Client Exception! (Not expected)\n{ex.Message}");
                this.errors.Add(ex);
                this.PopulateEventData();
            }
            catch (NullReferenceException ex)
            {
                this.logger.LogWarning($"MSAL unexpected null reference! (Not Expected)\n{ex.Message}");
                this.errors.Add(ex);
                this.PopulateEventData();
            }

            return new AuthFlowResult(null, this.errors);
        }

        private void PopulateEventData()
        {
            this.eventData.Add("errors", ExceptionListToStringConverter.SerializeExceptions(this.errors));
            this.eventData.Add("msal_correlation_ids", this.correlationIDs);
            this.eventData.Measures.Add("no_of_interactive_prompts", this.interactivePromptsCount);
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
    }
}
