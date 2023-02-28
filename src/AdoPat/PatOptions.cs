// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
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
        /// Gets the string representation of these PAT options.
        /// </summary>
        /// <returns>The string representation of these PAT options.</returns>
        public override string ToString()
        {
            string output = $"{this.Organization} {this.DisplayName}";

            foreach (var scope in this.Scopes)
            {
                output += $" {scope.ToString()}";
            }

            return output;
        }
    }
}
