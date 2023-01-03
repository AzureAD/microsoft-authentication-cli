// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;

    /// <summary>
    /// Not for use outside of Microsoft.Authentication.MSALWrapper.
    /// This interface is what our Authflows use for it's core business logic.
    /// </summary>
    public interface IPCAWrapper
    {
        /// <summary>
        /// Get token silent async.
        /// </summary>
        /// <param name="scopes">The scopes.</param>
        /// <param name="account">The account.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<TokenResult> GetTokenSilentAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken);

        /// <summary>
        /// Get token interactive async.
        /// </summary>
        /// <param name="scopes">The scopes.</param>
        /// <param name="account">The account.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<TokenResult> GetTokenInteractiveAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken);

        /// <summary>
        /// Get token interactive async.
        /// </summary>
        /// <param name="scopes">The scopes.</param>
        /// <param name="claims">The claims.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<TokenResult> GetTokenInteractiveAsync(IEnumerable<string> scopes, string claims, CancellationToken cancellationToken);

        /// <summary>
        /// Get token device code async.
        /// </summary>
        /// <param name="scopes">The scopes.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<TokenResult> GetTokenDeviceCodeAsync(IEnumerable<string> scopes, Func<DeviceCodeResult, Task> callback, CancellationToken cancellationToken);

        /// <summary>
        /// Get token integrated windows authentication async.
        /// </summary>
        /// <param name="scopes">The scopes.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<TokenResult> GetTokenIntegratedWindowsAuthenticationAsync(IEnumerable<string> scopes, CancellationToken cancellationToken);

        /// <summary>
        /// Customize the title bar by prompt hint(Web mode only).
        /// </summary>
        /// <param name="promptHint">The prompt hint text.</param>
        /// <returns>This.</returns>
        IPCAWrapper WithPromptHint(string promptHint);

        /// <summary>
        /// Enable or disable using an embedded web view for web mode prompts.
        /// Embedded web view is inherently unreliable for passing CA conditions and is off by default.
        /// </summary>
        /// <param name="enabled">Whether or not to use the embedded web view for web mode prompts.</param>
        /// <returns>This.</returns>
        IPCAWrapper WithEmbeddedWebView(bool enabled);

        /// <summary>
        /// Tries to return a cached account when the list has only one account using the preferred domain if provided.
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

        /// <summary>
        /// Remove an account from the file cache.
        /// </summary>
        /// <param name="account">IAccount to remove.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RemoveAsync(IAccount account);
    }
}
