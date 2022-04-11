// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlows
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The auth flows class.
    /// </summary>
    public class AuthFlows : IAuthFlows
    {
        /// <summary>
        /// The list of Auth flows to try.
        /// </summary>
        internal readonly List<IAuthFlows> Authflows;

        private readonly ILogger logger;
        private readonly Guid resourceId;
        private readonly Guid clientId;
        private readonly Guid tenantId;
        private readonly string osxKeyChainSuffix;
        private readonly bool verifyPersistence;
        private readonly string preferredDomain;
        private IEnumerable<string> scopes;

        #region Public configurable properties

        /// <summary>
        /// The global auth timeout.
        /// </summary>
        private TimeSpan globalAuthTimeout = TimeSpan.FromMinutes(15);
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthFlows"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="osxKeyChainSuffix">The osx key chain suffix.</param>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <param name="verifyPersistence">The verify persistence.</param>
        public AuthFlows(ILogger logger, Guid resourceId, Guid clientId, Guid tenantId, IEnumerable<string> scopes = null, string osxKeyChainSuffix = null, string preferredDomain = null, bool verifyPersistence = false)
        {
            this.Authflows = new List<IAuthFlows>();
            this.logger = logger;
            this.resourceId = resourceId;
            this.clientId = clientId;
            this.tenantId = tenantId;
            this.osxKeyChainSuffix = osxKeyChainSuffix;
            this.preferredDomain = preferredDomain;
            this.verifyPersistence = verifyPersistence;
            this.scopes = scopes ?? new string[] { $"{this.resourceId}/.default" };

            this.BuildRequiredAuthFlows();
        }

        /// <summary>
        /// The get token async.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<TokenResult> GetTokenAsync()
        {
            var token = await TaskExecutor.CompleteWithin(
                    this.globalAuthTimeout,
                    "Try auth flows",
                    (cancellationToken) => this.TryGetToken());
            return token;
        }

        private async Task<TokenResult> TryGetToken()
        {
            TokenResult token = null;
            foreach (var authFlow in this.Authflows)
            {
                try
                {
                    token = await authFlow.GetTokenAsync();
                    if (token != null)
                    {
                        return token;
                    }
                }
                catch (MsalException ex)
                {
                    this.logger.LogError(ex.Message);
                }
            }

            return token;
        }

        private void BuildRequiredAuthFlows()
        {
            if (PlatformUtils.IsWindows10(this.logger))
            {
                this.Authflows.Add(new BrokerPCATokenFetcher(this.logger, this.clientId, this.tenantId, this.scopes, this.osxKeyChainSuffix, this.preferredDomain, this.verifyPersistence));
            }

            // COMING SOON
            // this.authFlows.Add(new WebPCATokenFetcher(this.logger, this.clientId, this.tenantId, this.scopes, this.osxKeyChainSuffix, this.preferredDomain, this.verifyPersistence));
            // this.authFlows.Add(new DeviceCodePCATokenFetcher(this.logger, this.clientId, this.tenantId, this.scopes, this.osxKeyChainSuffix, this.verifyPersistence));
        }
    }
}
