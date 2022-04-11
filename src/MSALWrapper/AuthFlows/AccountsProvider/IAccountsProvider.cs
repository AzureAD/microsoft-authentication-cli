// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlows
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The AccountsProvider interface.
    /// </summary>
    internal interface IAccountsProvider
    {
        /// <summary>
        /// The try get account async.
        /// </summary>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<IAccount> TryGetAccountAsync(string preferredDomain = null);

        /// <summary>
        /// The try get accounts async.
        /// </summary>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<IList<IAccount>> TryGetAccountsAsync(string preferredDomain = null);
    }
}
