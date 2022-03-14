// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
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
    }

    /// <summary>
    /// Not for use outside of Microsoft.Office.Auth.ForConsole.
    /// This wrapper enables us to mock pca behavior which we cannot do with the MSAL
    /// provided IPublicClientApplication interface since all there methods return instances of non-mockable param builders
    /// that must be called to get the actual authentication result objects.
    /// </summary>
    public class PCAWrapper : IPCAWrapper
    {
        private IPublicClientApplication pca;

        /// <summary>
        /// Initializes a new instance of the <see cref="PCAWrapper"/> class.
        /// </summary>
        /// <param name="pca">
        /// The pca.
        /// </param>
        public PCAWrapper(IPublicClientApplication pca)
        {
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
