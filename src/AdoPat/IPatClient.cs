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
    public interface IPatClient
    {
        /// <summary>
        /// Creates a new PAT.
        /// </summary>
        /// <param name="displayName">The token name.</param>
        /// <param name="scope">The token scopes for accessing Azure DevOps resources.</param>
        /// <param name="validTo">The token expiration date.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> returning a <see cref="PatTokenResult"/>.</returns>
        Task<PatToken> CreateAsync(string displayName, string scope, DateTime validTo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active PATs.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A mapping of authorization ID to active PATs.</returns>
        Task<IDictionary<Guid, PatToken>> ListActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new PAT with a new 'valid to' date by replicating an existing one.
        /// </summary>
        /// <param name="patToken">An existing <see cref="PatToken"/>.</param>
        /// <param name="validTo">The new expiration date for the regenerated token.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> returning a <see cref="PatToken"/>.</returns>
        Task<PatToken> RegenerateAsync(PatToken patToken, DateTime validTo, CancellationToken cancellationToken = default);
    }
}
