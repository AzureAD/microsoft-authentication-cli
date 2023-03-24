// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System.Collections.Generic;
    using System.Linq;

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
            var strings = new List<string> { this.Organization, this.DisplayName };
            strings.AddRange(this.Scopes);
            return string.Join(" ", strings);
        }
    }
}
