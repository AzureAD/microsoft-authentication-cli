// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The accounts provider.
    /// </summary>
    internal class AccountsProvider : IAccountsProvider
    {
        private readonly IPublicClientApplication publicClientApplication;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountsProvider"/> class.
        /// </summary>
        /// <param name="publicClientApplication">The public client application.</param>
        /// <param name="logger">The logger.</param>
        internal AccountsProvider(IPublicClientApplication publicClientApplication, ILogger logger)
        {
            this.publicClientApplication = publicClientApplication;
            this.logger = logger;
        }

        /// <summary>
        /// The try get account async.
        /// </summary>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<IAccount> TryGetAccountAsync(string preferredDomain = null)
        {
            var accounts = await this.TryGetAccountsAsync(preferredDomain);
            var account = (accounts == null || accounts.Count() > 1) ? null : accounts.FirstOrDefault();
            return account;
        }

        /// <summary>
        /// The try get accounts async.
        /// </summary>
        /// <param name="preferredDomain">The preferred domain.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<IList<IAccount>> TryGetAccountsAsync(string preferredDomain = null)
        {
            IEnumerable<IAccount> accounts = await this.publicClientApplication.GetAccountsAsync().ConfigureAwait(false);
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
    }
}
