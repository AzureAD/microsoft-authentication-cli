// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;

    /// <summary>
    /// Manage the interaction between the PAT cache and Azure DevOps.
    /// </summary>
    public class PatManager
    {
        private const int ValidToExtensionDays = 7;
        private const int NearingExpirationDays = 2;

        private IPatCache cache;
        private IPatClient client;
        private Func<DateTime> now;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatManager"/> class.
        /// </summary>
        /// <param name="cache">Any class that implements the <see cref="IPatCache"/> interface.</param>
        /// <param name="client">Any class that implements the <see cref="IPatClient"/> interface.</param>
        /// <param name="now">A function for computing the current moment. Defaults to null, which
        /// uses <see cref="DateTime.UtcNow"/>. Overriding this should only be necessary in testing.</param>
        public PatManager(IPatCache cache, IPatClient client, Func<DateTime> now = null)
        {
            this.cache = cache;
            this.client = client;
            this.now = now ?? new Func<DateTime>(() => DateTime.UtcNow);
        }

        /// <summary>
        /// Fetches a PAT matching the given options from the cache, creating or regenerating a new PAT as necessary.
        /// </summary>
        /// <param name="options">Options used to match the PAT in the cache or when creating/regenerating a new one.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An Azure DevOps Personal Access Token.</returns>
        public async Task<PatToken> GetPatAsync(
            PatOptions options,
            CancellationToken cancellationToken = default)
        {
            bool writeBack = false;
            var pat = this.cache.GetPat(options.ToString());

            if (pat == null || await this.Inactive(pat, cancellationToken).ConfigureAwait(false))
            {
                var patTokencreateRequest = new PatTokenCreateRequest
                {
                    DisplayName = options.DisplayName,
                    Scope = string.Join(" ", options.Scopes),
                    ValidTo = this.now().AddDays(ValidToExtensionDays),
                    AllOrgs = default,
                };
                pat = await this.client.CreatePatAsync(patTokencreateRequest, cancellationToken).ConfigureAwait(false);
                writeBack = true;
            }

            if (this.ExpiringSoon(pat))
            {
                pat = await this.client.RegeneratePatAsync(
                    pat,
                    this.now().AddDays(ValidToExtensionDays),
                    cancellationToken)
                .ConfigureAwait(false);
                writeBack = true;
            }

            if (writeBack)
            {
                this.cache.PutPat(options.ToString(), pat);
            }

            return pat;
        }

        // Whether the given PAT is still considered active by Azure DevOps.
        private async Task<bool> Inactive(PatToken pat, CancellationToken cancellationToken = default)
        {
            var activePats = await this.client.GetActivePatsAsync(cancellationToken).ConfigureAwait(false);
            return !activePats.ContainsKey(pat.AuthorizationId);
        }

        // Whether the given PAT will expire before `NearingExpirationDays`.
        private bool ExpiringSoon(PatToken pat)
        {
            return pat.ValidTo < this.now().AddDays(NearingExpirationDays);
        }
    }
}
