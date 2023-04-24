// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// A grouping of common options necessary to create or renew a PAT.
    /// Used as a key for the PAT cache.
    /// </summary>
    public record PatOptions
    {
        /// <summary>
        /// Gets the PAT organization.
        /// </summary>
        public string Organization { get; init; } = default;

        /// <summary>
        /// Gets the PAT display name.
        /// </summary>
        public string DisplayName { get; init; } = default;

        /// <summary>
        /// Gets the PAT scopes.
        /// </summary>
        public string[] Scopes { get; init; } = default;

        /// <summary>
        /// Gets the string used as a cache key which corresponds to these options.
        /// </summary>
        /// <returns>The cache key.</returns>
        public string CacheKey()
        {
            // We concatenate scopes with the empty string after sorting and
            // deduplicating them to ensure the cache key is always the same.
            var sortedScopes = string.Concat(this.Scopes.ToImmutableSortedSet());

            using (SHA256 sha256 = SHA256.Create())
            {
                var organization = sha256.ComputeHash(Encoding.UTF8.GetBytes(this.Organization));
                var displayName = sha256.ComputeHash(Encoding.UTF8.GetBytes(this.DisplayName));
                var scopes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sortedScopes));

                var hashBytes = new[] { organization, displayName, scopes };
                var hashes = hashBytes
                    .Select(bytes => bytes.Select(b => b.ToString("x2")))
                    .Select(bytes => string.Concat(bytes));

                return string.Join('-', hashes);
            }
        }
    }
}
