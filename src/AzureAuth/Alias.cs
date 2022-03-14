// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System.Collections.Generic;

    /// <summary>
    /// The alias.
    /// </summary>
    public class Alias
    {
        /// <summary>
        /// Gets or sets the resource.
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        public string Client { get; set; }

        /// <summary>
        /// Gets or sets the domain.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the tenant.
        /// </summary>
        public string Tenant { get; set; }

        /// <summary>
        /// Gets or sets the scopes.
        /// </summary>
        public List<string> Scopes { get; set; }

        /// <summary>
        /// The override.
        /// </summary>
        /// <param name="other">
        /// The other.
        /// </param>
        /// <returns>
        /// The <see cref="Alias"/>.
        /// </returns>
        public Alias Override(Alias other)
        {
            return new Alias
            {
                Resource = other.Resource ?? this.Resource,
                Client = other.Client ?? this.Client,
                Domain = other.Domain ?? this.Domain,
                Tenant = other.Tenant ?? this.Tenant,
                Scopes = other.Scopes ?? this.Scopes,
            };
        }
    }
}
