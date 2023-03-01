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
    /// <remarks>
    /// This class and its methods are not threadsafe. Locking must be handled externally.
    /// </remarks>
    public class PatClient : IPatClient
    {
        private const int PageSize = 100; // Using the maximum allowable page size allows us to reduce HTTP calls.

        // The AllOrgs field controls whether a PAT token create request is for all of a user's accessible organizations.
        // The default of false means that the token is for a specific organization. The Azure DevOps team have
        // recommended we always set this to false.
        private const bool AllOrgs = false;

        private ITokensHttpClientWrapper client;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatClient"/> class.
        /// </summary>
        /// <param name="client">Any class which implements the <see cref="ITokensHttpClientWrapper"/> interface.</param>
        public PatClient(ITokensHttpClientWrapper client)
        {
            this.client = client;
        }

        /// <inheritdoc/>
        public async Task<PatToken> CreateAsync(
            string displayName,
            string scope,
            DateTime validTo,
            CancellationToken cancellationToken = default)
        {
            var patTokenCreateRequest = new PatTokenCreateRequest
            {
                DisplayName = displayName,
                Scope = scope,
                ValidTo = validTo,
                AllOrgs = AllOrgs,
            };
            var patTokenResult = await this.client.CreatePatAsync(
                patTokenCreateRequest,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

            if (patTokenResult.PatToken == null)
            {
                throw new PatClientException($"Failed to create PAT: {patTokenResult.PatTokenError}");
            }

            return patTokenResult.PatToken;
        }

        /// <inheritdoc/>
        public async Task<IDictionary<Guid, PatToken>> ListActiveAsync(CancellationToken cancellationToken = default)
        {
            // Initialize a PagedPatTokens so that we can use the continuation token
            // in the scope of the conditional of the do while loop below. Azure
            // DevOps will return the empty string when there are no more pages.
            var pagedPatTokens = new PagedPatTokens(continuationToken: string.Empty, patTokens: new List<PatToken>());
            var pats = new Dictionary<Guid, PatToken>();
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
                    pats.Add(pat.AuthorizationId, pat);
                }
            }
            while (!string.IsNullOrEmpty(pagedPatTokens.ContinuationToken));

            return pats;
        }

        /// <inheritdoc/>
        public async Task<PatToken> RegenerateAsync(
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
            var renewedPatToken = await this.CreateAsync(
                patToken.DisplayName,
                patToken.Scope,
                validTo: validTo,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

            // Note: This method will not try to undo creation of a PAT if the
            // revocation fails.
            await this.client.RevokeAsync(
                patToken.AuthorizationId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

            return renewedPatToken;
        }
    }
}
