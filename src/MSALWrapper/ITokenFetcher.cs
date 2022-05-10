// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// The TokenFetcher interface.
    /// </summary>
    public interface ITokenFetcher
    {
        /// <summary>
        /// A number of exceptions are thrown and this method stores the list of exception messages.
        /// </summary>
        /// <returns> Returns a list of exceptions.</returns>
        IEnumerable<Exception> Errors();

        /// <summary>
        /// The get access token async.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<TokenResult> GetAccessTokenAsync();

        /// <summary>
        /// The get access token async.
        /// </summary>
        /// <param name="authMode">/// The auth mode.</param>
        /// <returns>/// The <see cref="Task"/>.</returns>
        Task<TokenResult> GetAccessTokenAsync(AuthMode authMode);

        /// <summary>
        /// The get access token async.
        /// </summary>
        /// <param name="scopes">/// The scopes.</param>
        /// <param name="authMode">/// The auth mode.</param>
        /// <returns>/// The <see cref="Task"/>.
        /// </returns>
        Task<TokenResult> GetAccessTokenAsync(IEnumerable<string> scopes, AuthMode authMode);

        /// <summary>
        /// The get access token async.
        /// </summary>
        /// <param name="scopes">/// The scopes.</param>
        /// <returns>/// The <see cref="Task"/>.</returns>
        Task<TokenResult> GetAccessTokenAsync(IEnumerable<string> scopes);

        /// <summary>
        /// Clear the local token cache for the current public client application.
        /// </summary>
        /// <returns>/// The <see cref="Task"/>.</returns>
        Task ClearCacheAsync();
    }
}
