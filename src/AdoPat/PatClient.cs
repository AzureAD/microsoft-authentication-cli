// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;

    /// <summary>
    /// TODO.
    /// </summary>
    public class PatClient
    {
        private ITokensHttpClientProvider client;
        private int pageSize = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatClient"/> class.
        /// </summary>
        /// <param name="client">TODO.</param>
        public PatClient(ITokensHttpClientProvider client)
        {
            this.client = client;
        }

        /// <summary>
        /// TODO: Summarize this method. Should we actually return PatTokenResults and take PatTokenCreateRequests or use our own types.
        /// </summary>
        /// <param name="patTokenCreateRequest">(Undocumented).</param>
        /// <param name="userState">Undocumented.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>A <see cref="Task"/> returning a <see cref="PatTokenResult"/>.</returns>
        public async Task<PatTokenResult> CreatePatAsync(PatTokenCreateRequest patTokenCreateRequest, object userState = null, CancellationToken cancellationToken = default)
        {
            return await this.client.CreatePatAsync(patTokenCreateRequest, userState, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// TODO: It might be better to yield results one page at a time instead of collecting them all up into a set.
        /// </summary>
        /// <param name="cancellationToken">Foo.</param>
        /// <returns>Bar.</returns>
        public async Task<ISet<PatToken>> GetActivePatsAsync(CancellationToken cancellationToken = default)
        {
            // Initialize a PagedPatTokens so that we can use the continuation token
            // in the scope of the conditional of the do while loop below.
            var pagedPatTokens = new PagedPatTokens(continuationToken: string.Empty, patTokens: new List<PatToken>());
            var pats = new HashSet<PatToken>();
            do
            {
                pagedPatTokens = await this.client.ListPatsAsync(
                    displayFilterOption: DisplayFilterOptions.Active,
                    top: this.pageSize,
                    continuationToken: pagedPatTokens.ContinuationToken,
                    sortByOption: SortByOptions.DisplayDate,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

                foreach (PatToken pat in pagedPatTokens.PatTokens)
                {
                    pats.Add(pat);
                }
            }
            while (!string.IsNullOrEmpty(pagedPatTokens.ContinuationToken));

            return pats;
        }
    }
}
