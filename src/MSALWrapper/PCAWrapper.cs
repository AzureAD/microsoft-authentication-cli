// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;

    /// <summary>
    /// Not for use outside of Microsoft.Office.Auth.ForConsole.
    /// This wrapper enables us to mock pca behavior which we cannot do with the MSAL
    /// provided IPublicClientApplication interface since all there methods return instances of non-mockable param builders
    /// that must be called to get the actual authentication result objects.
    /// </summary>
    public class PCAWrapper : IPCAWrapper
    {
        private ILogger logger;
        private IPublicClientApplication pca;

        /// <summary>
        /// Initializes a new instance of the <see cref="PCAWrapper"/> class without caching.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="pca">The public client application instance.</param>
        public PCAWrapper(ILogger logger, IPublicClientApplication pca)
        {
            this.logger = logger;
            this.pca = pca;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PCAWrapper"/> class with x-plat caching configured.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="pca">The public client application instance.</param>
        /// <param name="errors">The errors list to append error encountered to.</param>
        /// <param name="tenantId">The tenant ID to help key the cache off of.</param>
        /// <param name="osxKeyChainSuffix">An optional (can be null) suffix to further customize key chain token caches on OSX.</param>
        public PCAWrapper(ILogger logger, IPublicClientApplication pca, IList<Exception> errors, Guid tenantId, string osxKeyChainSuffix)
            : this(logger, pca)
        {
            var cacher = new PCACache(logger, tenantId, osxKeyChainSuffix);
            cacher.SetupTokenCache(this.pca.UserTokenCache, errors);
        }

        /// <summary>
        /// Gets or sets, The prompt hint displayed in the title bar.
        /// </summary>
        public string PromptHint { get; set; }

        /// <inheritdoc/>
        public IPCAWrapper WithPromptHint(string promptHint)
        {
            this.PromptHint = promptHint;
            return this;
        }

        /// <inheritdoc/>
        public async Task<TokenResult> GetTokenSilentAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await this.pca.AcquireTokenSilent(scopes, account).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return this.TokenResultOrThrow(result);
        }

        /// <inheritdoc/>
        public async Task<TokenResult> GetTokenInteractiveAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await this.pca
                .AcquireTokenInteractive(scopes)
                .WithEmbeddedWebViewOptions(new EmbeddedWebViewOptions()
                {
                    Title = this.PromptHint,
                })
                .WithAccount(account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            return this.TokenResultOrThrow(result);
        }

        /// <inheritdoc/>
        public async Task<TokenResult> GetTokenInteractiveAsync(IEnumerable<string> scopes, string claims, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await this.pca
                .AcquireTokenInteractive(scopes)
                .WithEmbeddedWebViewOptions(new EmbeddedWebViewOptions()
                {
                    Title = this.PromptHint,
                })
                .WithClaims(claims)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            return this.TokenResultOrThrow(result);
        }

        /// <inheritdoc/>
        public async Task<TokenResult> GetTokenDeviceCodeAsync(IEnumerable<string> scopes, Func<DeviceCodeResult, Task> callback, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await this.pca.AcquireTokenWithDeviceCode(scopes, callback).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return this.TokenResultOrThrow(result);
        }

        /// <inheritdoc/>
        public async Task<IAccount> TryToGetCachedAccountAsync(string preferredDomain = null)
        {
            var accounts = await this.TryToGetCachedAccountsAsync(preferredDomain);
            var account = (accounts == null || accounts.Count() > 1) ? null : accounts.FirstOrDefault();
            return account;
        }

        /// <inheritdoc/>
        public async Task<IList<IAccount>> TryToGetCachedAccountsAsync(string preferredDomain = null)
        {
            IEnumerable<IAccount> accounts = await this.pca.GetAccountsAsync().ConfigureAwait(false);
            if (accounts == null)
            {
                return null;
            }

            this.logger.LogDebug($"Accounts found in cache: ({accounts.Count()}):");
            this.logger.LogDebug(string.Join("\n", accounts.Select(a => a.Username)));

            if (!string.IsNullOrWhiteSpace(preferredDomain))
            {
                this.logger.LogDebug($"Filtering cached accounts with preferred domain '{preferredDomain}'");
                accounts = accounts.Where(eachAccount => eachAccount.Username.EndsWith(preferredDomain, StringComparison.OrdinalIgnoreCase));

                this.logger.LogDebug($"Accounts found in cache after filtering: ({accounts.Count()}):");
                this.logger.LogDebug(string.Join("\n", accounts.Select(a => a.Username)));
            }

            return accounts.ToList();
        }

        private TokenResult TokenResultOrThrow(AuthenticationResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.AccessToken))
            {
                return null;
            }

            return new TokenResult(new JsonWebToken(result.AccessToken));
        }
    }
}
