// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;

    /// <summary>
    /// An interface for a transparent wrapper of <see cref="TokensHttpClient"/>.
    /// </summary>
    public interface ITokensHttpClientWrapper
    {
        /// <summary>
        /// [Preview API] Creates a new personal access token (PAT) for the requesting user.
        /// </summary>
        /// <param name="patTokenCreateRequest">(Undocumented).</param>
        /// <param name="userState">(Undocumented).</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>A <see cref="Task"/> returning a <see cref="PatTokenResult"/>.</returns>
        Task<PatTokenResult> CreatePatAsync(
            PatTokenCreateRequest patTokenCreateRequest,
            object userState = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///  [Preview API] Gets a paged list of personal access tokens (PATs) created in this organization. Subsequent calls to the API require the same filtering options to be supplied.
        /// </summary>
        /// <param name="displayFilterOption">(Optional) Refers to the status of the personal access token (PAT).</param>
        /// <param name="sortByOption">(Optional) Which field to sort by.</param>
        /// <param name="isSortAscending">(Optional) Ascending or descending.</param>
        /// <param name="continuationToken">(Optional) Where to start returning tokens from.</param>
        /// <param name="top">(Optional) How many tokens to return, limit of 100.</param>
        /// <param name="userState">(Undocumented).</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>A <see cref="Task"/> with <see cref="PagedPatTokens"/>.</returns>
        Task<PagedPatTokens> ListPatsAsync(
            DisplayFilterOptions? displayFilterOption = null,
            SortByOptions? sortByOption = null,
            bool? isSortAscending = null,
            string continuationToken = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// [Preview API] Revokes a personal access token (PAT) by authorizationId.
        /// </summary>
        /// <param name="authorizationId">The authorizationId identifying a single, unique personal access token (PAT).</param>
        /// <param name="userState">(Undocumented).</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>Void.</returns>
        Task RevokeAsync(
            Guid authorizationId,
            object userState = null,
            CancellationToken cancellationToken = default);
    }
}
