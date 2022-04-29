// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;

    /// <summary>
    /// An Auth Flow for IWA - a silent integrated windows auth flow using ADFS - silently only.
    /// </summary>
    public class IntegratedWindowsAuthentication : IAuthFlow
    {
        private ILogger logger;
        private IPublicClientApplication pca;
        private IEnumerable<string> scopes;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegratedWindowsAuthentication"/> class.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger"/> to use.</param>
        /// <param name="clientId">ID of client application.</param>
        /// <param name="tenantId">Thje ID of the tenant.</param>
        /// <param name="scopes">The scopes to request.</param>
        public IntegratedWindowsAuthentication(ILogger logger, Guid clientId, Guid tenantId, IEnumerable<string> scopes)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.pca = PublicClientApplicationBuilder.Create(clientId.ToString())
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .Build();
            this.scopes = scopes;
        }

        /// <inheritdoc/>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            var result = new AuthFlowResult();
            try
            {
                var authResult = await this.pca.AcquireTokenByIntegratedWindowsAuth(this.scopes).ExecuteAsync().ConfigureAwait(false);
                result.TokenResult = new TokenResult(new JsonWebToken(authResult.AccessToken));
            }
            catch (Exception ex)
            {
                this.logger.LogDebug($"{this.GetType().Name} failed:\n{ex}");
            }

            return result;
        }
    }
}
