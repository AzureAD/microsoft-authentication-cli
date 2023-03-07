// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The ISilentAuthFlow interface.
    /// </summary>
    public interface ISilentAuthFlow
    {
        /// <summary>
        /// Get the account in cache.
        /// </summary>
        /// <returns>The account.</returns>
        Task<IAccount> GetCachedAccountAsync();

        /// <summary>
        /// Get a token for a resource with non-interactive mode.
        /// </summary>
        /// <param name="account">The account found in cache.</param>
        /// <returns>A <see cref="Task"/> of <see cref="TokenResult"/>.</returns>
        Task<AuthFlowResult> GetTokenSilentAsync(IAccount account);
    }
}
