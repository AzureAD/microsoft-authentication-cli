// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;

    /// <summary>
    /// An abstraction over common operations with the Azure DevOps PAT Lifecycle Management REST API.
    /// </summary>
    public class PatClient
    {
        private const int PageSize = 100; // Using the maximum allowable page size allows us to reduce HTTP calls.
        private const bool AllOrgs = false; // Azure DevOps have recommended we always set this to false.

        private ITokensHttpClientWrapper client;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatClient"/> class.
        /// </summary>
        /// <param name="client">Any class which implements the <see cref="ITokensHttpClientWrapper"/> interface.</param>
        public PatClient(ITokensHttpClientWrapper client)
        {
            this.client = client;
        }

        /// <summary>
        /// Creates a new PAT.
        /// </summary>
        /// <param name="patTokenCreateRequest">
        /// XXX: Not documented by Visual Studio client libraries. Should we depend on this or
        /// should this method use the corresponding primitives as arguments? Not yet clear.
        /// </param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> returning a <see cref="PatTokenResult"/>.</returns>
        public async Task<PatToken> CreatePatAsync(
            PatTokenCreateRequest patTokenCreateRequest,
            CancellationToken cancellationToken = default)
        {
            var patTokenResult = await this.client.CreatePatAsync(
                patTokenCreateRequest,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

            if (patTokenResult.PatToken == null)
            {
                throw new PatClientException($"Failed to create PAT during regeneration: {patTokenResult.PatTokenError}");
            }

            return patTokenResult.PatToken;
        }

        /// <summary>
        /// Gets all active PATs.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The set of active PATs.</returns>
        public async Task<ISet<PatToken>> GetActivePatsAsync(CancellationToken cancellationToken = default)
        {
            // Initialize a PagedPatTokens so that we can use the continuation token
            // in the scope of the conditional of the do while loop below. Azure
            // DevOps will return the empty string when there are no more pages.
            var pagedPatTokens = new PagedPatTokens(continuationToken: string.Empty, patTokens: new List<PatToken>());
            var pats = new HashSet<PatToken>();
            do
            {
                pagedPatTokens = await this.client.ListPatsAsync(
                    displayFilterOption: DisplayFilterOptions.Active,
                    top: PageSize,
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

        /// <summary>
        /// Creates a new PAT with a new 'valid to' date by replicating an existing one.
        /// </summary>
        /// <param name="patToken">An existing <see cref="PatToken"/>.</param>
        /// <param name="validTo">The new expiration date for the regenerated token.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> returning a <see cref="PatToken"/>.</returns>
        public async Task<PatToken> RegeneratePatAsync(
            PatToken patToken,
            DateTime validTo,
            CancellationToken cancellationToken = default)
        {
            // Regeneration is an option for existing PATs in the Azure DevOps UI,
            // but from the API's viewpoint it is an abstraction over 3 operations.
            //
            // 1. Understand the metadata of the PAT using a GET call.
            // 2. Create a new PAT with the old PAT's metadata using a POST call.
            // 3. Revoke the old PAT using a DELETE call.
            //
            // We skip the initial GET call because the given patToken should have
            // its own metadata. It risks being out of date depending on where it
            // was fetched from.
            //
            // For more info see:
            // https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/manage-personal-access-tokens-via-api?view=azure-devops#q-how-can-i-regeneraterotate-pats-through-the-api-i-saw-that-option-in-the-ui-but-i-dont-see-a-similar-method-in-the-api
            PatTokenCreateRequest patTokenCreateRequest = new PatTokenCreateRequest
            {
                DisplayName = patToken.DisplayName,
                Scope = patToken.Scope,
                ValidTo = validTo,
                AllOrgs = AllOrgs,
            };

            var renewedPatToken = await this.CreatePatAsync(
                patTokenCreateRequest,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

            // XXX: If we create a new PAT, but fail to revoke the old PAT, should
            // we continue or should we try to undo the creation and bail? It's
            // possible this could fail because the token has already been revoked.
            // Is that an error we should allow?
            await this.client.RevokeAsync(
                patToken.AuthorizationId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

            return renewedPatToken;
        }
    }
}
