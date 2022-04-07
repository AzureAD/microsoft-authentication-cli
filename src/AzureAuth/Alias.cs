// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System.Collections.Generic;

    /// <summary>
    /// The alias contains information that the auth process needs.
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
        /// Gets or sets the customized caller name (WAM prompts and web mode only).
        /// </summary>
        public string Caller { get; set; }

        /// <summary>
        /// Gets or sets the scopes.
        /// </summary>
        public List<string> Scopes { get; set; }

        /// <summary>
        /// The override method creates a new <see cref="Alias"/> instance which merges two Alias fields.
        /// Non-null fields in the given parameter will replace original fields.
        /// </summary>
        /// <param name="other">
        /// The given instance with fields to be replaced.
        /// </param>
        /// <returns>
        /// The merged <see cref="Alias"/>.
        /// </returns>
        public Alias Override(Alias other)
        {
            return new Alias
            {
                Resource = other.Resource ?? this.Resource,
                Client = other.Client ?? this.Client,
                Domain = other.Domain ?? this.Domain,
                Tenant = other.Tenant ?? this.Tenant,
                Caller = other.Caller ?? this.Caller,
                Scopes = other.Scopes ?? this.Scopes,
            };
        }
    }
}
