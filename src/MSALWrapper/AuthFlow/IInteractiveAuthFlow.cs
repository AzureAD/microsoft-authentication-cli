// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The IInteractiveAuthFlow interface.
    /// </summary>
    public interface IInteractiveAuthFlow
    {
        /// <summary>
        /// Get the account in cache.
        /// </summary>
        /// <returns>The account.</returns>
        Task<IAccount> GetCachedAccountAsync();

        /// <summary>
        /// Get a token for a resource with interactive mode.
        /// </summary>
        /// <param name="account">The account found in cache.</param>
        /// <returns>A <see cref="Task"/> of <see cref="TokenResult"/>.</returns>
        Task<AuthFlowResult> GetTokenInteractiveAsync(IAccount account);
    }
}
