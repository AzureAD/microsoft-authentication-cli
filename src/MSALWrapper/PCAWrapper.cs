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
    /// This interface is what our token fetcher uses for it's core business logic.
    /// </summary>
    public interface IPCAWrapper
    {
        /// <summary>
        /// The get token silent async.
        /// </summary>
        /// <param name="scopes">
        /// The scopes.
        /// </param>
        /// <param name="account">
        /// The account.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<TokenResult> GetTokenSilentAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken);

        /// <summary>
        /// The get token interactive async.
        /// </summary>
        /// <param name="scopes">
        /// The scopes.
        /// </param>
        /// <param name="account">
        /// The account.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<TokenResult> GetTokenInteractiveAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken);

        /// <summary>
        /// The get token interactive async.
        /// </summary>
        /// <param name="scopes">
        /// The scopes.
        /// </param>
        /// <param name="claims">
        /// The claims.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<TokenResult> GetTokenInteractiveAsync(IEnumerable<string> scopes, string claims, CancellationToken cancellationToken);

        /// <summary>
        /// The get token device code async.
        /// </summary>
        /// <param name="scopes">
        /// The scopes.
        /// </param>
        /// <param name="callback">
        /// The callback.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        Task<TokenResult> GetTokenDeviceCodeAsync(IEnumerable<string> scopes, Func<DeviceCodeResult, Task> callback, CancellationToken cancellationToken);

        /// <summary>
        /// Tries to return a cached account when the list returns only one account using the preferred domain if provided.
        /// A null return indicates one of the following.
        /// No accounts were found in cache.
        /// No accounts match the domain.
        /// More than one account with the same domain was found in the list.
        /// </summary>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<IAccount> TryToGetCachedAccountAsync(string preferredDomain = null);

        /// <summary>
        /// Tries to get a list of cached accounts using the preferred domain if provided.
        /// It returns null if no accounts are returned from the PCA.
        /// </summary>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<IList<IAccount>> TryToGetCachedAccountsAsync(string preferredDomain = null);
    }

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
        /// Initializes a new instance of the <see cref="PCAWrapper"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="pca">The public client application instance.</param>
        public PCAWrapper(ILogger logger, IPublicClientApplication pca)
        {
            this.logger = logger;
            this.pca = pca;
        }

        /// <summary>
        /// The get token silent async.
        /// </summary>
        /// <param name="scopes">
        /// The scopes.
        /// </param>
        /// <param name="account">
        /// The account.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<TokenResult> GetTokenSilentAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await this.pca.AcquireTokenSilent(scopes, account).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return this.TokenResultOrThrow(result);
        }

        /// <summary>
        /// The get token interactive async.
        /// </summary>
        /// <param name="scopes">
        /// The scopes.
        /// </param>
        /// <param name="account">
        /// The account.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<TokenResult> GetTokenInteractiveAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await this.pca
                .AcquireTokenInteractive(scopes)
                .WithAccount(account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            return this.TokenResultOrThrow(result);
        }

        /// <summary>
        /// The get token interactive async.
        /// </summary>
        /// <param name="scopes">
        /// The scopes.
        /// </param>
        /// <param name="claims">
        /// The claims.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<TokenResult> GetTokenInteractiveAsync(IEnumerable<string> scopes, string claims, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await this.pca
                .AcquireTokenInteractive(scopes)
                .WithClaims(claims)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            return this.TokenResultOrThrow(result);
        }

        /// <summary>
        /// The get token device code async.
        /// </summary>
        /// <param name="scopes">
        /// The scopes.
        /// </param>
        /// <param name="callback">
        /// The callback.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<TokenResult> GetTokenDeviceCodeAsync(IEnumerable<string> scopes, Func<DeviceCodeResult, Task> callback, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await this.pca.AcquireTokenWithDeviceCode(scopes, callback).ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return this.TokenResultOrThrow(result);
        }

        /// <summary>
        /// Tries to return a cached account when the list has only one account using the preferred domain if provided.
        /// A null return indicates one of the following.
        /// No accounts were found in cache.
        /// No accounts match the domain.
        /// More than one account with the same domain was found in the list.
        /// </summary>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<IAccount> TryToGetCachedAccountAsync(string preferredDomain = null)
        {
            var accounts = await this.TryToGetCachedAccountsAsync(preferredDomain);
            var account = (accounts == null || accounts.Count() > 1) ? null : accounts.FirstOrDefault();
            return account;
        }

        /// <summary>
        /// Tries to get a list of cached accounts using the preferred domain if provided.
        /// It returns null if no accounts are returned from the PCA.
        /// </summary>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <returns>The <see cref="Task"/>.</returns>
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
                throw new NullAuthenticationResultException();
            }

            return new TokenResult(new JsonWebToken(result.AccessToken));
        }
    }
}
