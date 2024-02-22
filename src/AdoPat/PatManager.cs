// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;

    /// <summary>
    /// Manage the interaction between the PAT cache and Azure DevOps.
    /// </summary>
    /// <remarks>
    /// This class and its methods are not threadsafe. Locking must be handled externally.
    /// </remarks>
    public class PatManager
    {
        // The ValidToExtensionDays value cannot exceed 7. The Azure DevOps PAT API will not allow a PAT to be valid for more than 7 days, and may return an error if the value is higher.
        private const int ValidToExtensionDays = 7;
        private const int NearingExpirationDays = 2;

        // Note: Do NOT use this logger instance to log any fields on a PAT, ESPECIALLY the token.
        private ILogger logger;

        private IPatCache cache;
        private IPatClient client;
        private Func<DateTime> now;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatManager"/> class.
        /// </summary>
        /// <param name="logger">An instance of <see cref="ILogger"/>.</param>
        /// <param name="cache">Any class that implements the <see cref="IPatCache"/> interface.</param>
        /// <param name="client">Any class that implements the <see cref="IPatClient"/> interface.</param>
        /// <param name="now">A function for computing the current moment. Defaults to null, which
        /// uses <see cref="DateTime.UtcNow"/>. Overriding this should only be necessary in testing.</param>
        public PatManager(
            ILogger logger,
            IPatCache cache,
            IPatClient client,
            Func<DateTime> now = null
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.client = client;
            this.now = now ?? new Func<DateTime>(() => DateTime.UtcNow);
        }

        /// <summary>
        /// Fetches a PAT matching the given options from the cache, creating or regenerating a new PAT as necessary.
        /// The cache controlled by this PatManager is not a shared cache and is *only* updated by this method.
        /// </summary>
        /// <param name="options">Options used to match the PAT in the cache or when creating/regenerating a new one.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An Azure DevOps Personal Access Token.</returns>
        public async Task<PatToken> GetPatAsync(
            PatOptions options,
            CancellationToken cancellationToken = default
        )
        {
            var cacheKey = options.CacheKey();
            this.logger.LogDebug($"Checking for PAT in cache with key '{cacheKey}'");
            var pat = this.cache.Get(cacheKey);

            // If the PAT is null it means it wasn't present in the cache, so we must create one.
            // If the PAT was present in the cache, but is inactive we must also create a new one.
            // If the PAT was present in the cache, but will expire soon we must regenerate it.
            // Otherwise we can simply return the PAT as is.
            if (
                this.NullPat(pat)
                || await this.Inactive(pat, cancellationToken).ConfigureAwait(false)
            )
            {
                this.logger.LogDebug($"Creating new PAT with {options}");
                pat = await this.client
                    .CreateAsync(
                        displayName: options.DisplayName,
                        scope: string.Join(" ", options.Scopes),
                        validTo: this.now().AddDays(ValidToExtensionDays),
                        cancellationToken: cancellationToken
                    )
                    .ConfigureAwait(false);
                this.cache.Put(options.CacheKey(), pat);
            }
            else if (this.ExpiringSoon(pat))
            {
                this.logger.LogDebug($"Regenerating PAT with {options}");
                pat = await this.client
                    .RegenerateAsync(
                        pat,
                        this.now().AddDays(ValidToExtensionDays),
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                this.cache.Put(options.CacheKey(), pat);
            }

            return pat;
        }

        private bool NullPat(PatToken pat)
        {
            if (pat == null)
            {
                this.logger.LogDebug("No matching PAT found in cache");
                return true;
            }

            this.logger.LogDebug("Found PAT in cache");
            return false;
        }

        // Whether the given PAT is still considered active by Azure DevOps.
        private async Task<bool> Inactive(
            PatToken pat,
            CancellationToken cancellationToken = default
        )
        {
            var activePats = await this.client
                .ListActiveAsync(cancellationToken)
                .ConfigureAwait(false);
            var active = activePats.ContainsKey(pat.AuthorizationId);
            this.logger.LogDebug($"PAT active: {active}");
            return !active;
        }

        // Whether the given PAT will expire before `NearingExpirationDays`.
        private bool ExpiringSoon(PatToken pat)
        {
            var expiringSoon = pat.ValidTo < this.now().AddDays(NearingExpirationDays);
            this.logger.LogDebug($"PAT expiring soon: {expiringSoon}");
            return expiringSoon;
        }
    }
}
