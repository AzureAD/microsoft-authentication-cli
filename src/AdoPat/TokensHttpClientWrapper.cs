// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization.Client;
    using Microsoft.VisualStudio.Services.WebApi;

    /// <summary>
    /// A thin wrapper around a <see cref="TokensHttpClient"/> that implements the <see cref="ITokensHttpClientWrapper"/> interface.
    /// </summary>
    public class TokensHttpClientWrapper : ITokensHttpClientWrapper
    {
        private TokensHttpClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokensHttpClientWrapper"/> class.
        /// </summary>
        /// <param name="connection">An instance of <see cref="VssConnection"/>.</param>
        public TokensHttpClientWrapper(VssConnection connection)
        {
            this.client = connection.GetClient<TokensHttpClient>();
        }

        /// <inheritdoc/>
        public async Task<PatTokenResult> CreatePatAsync(
            PatTokenCreateRequest patTokenCreateRequest,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            return await this.client.CreatePatAsync(
                patTokenCreateRequest,
                userState,
                cancellationToken)
            .ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PagedPatTokens> ListPatsAsync(
            DisplayFilterOptions? displayFilterOption = null,
            SortByOptions? sortByOption = null,
            bool? isSortAscending = null,
            string continuationToken = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            return await this.client.ListPatsAsync(
                displayFilterOption,
                sortByOption,
                isSortAscending,
                continuationToken,
                top,
                userState,
                cancellationToken)
            .ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task RevokeAsync(
            Guid authorizationId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            await this.RevokeAsync(
                authorizationId,
                userState,
                cancellationToken)
            .ConfigureAwait(false);
        }
    }
}
