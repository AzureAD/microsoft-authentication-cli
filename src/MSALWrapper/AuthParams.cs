// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Core authentication parameters needed 
    /// </summary>
    public record AuthParams
    {
        /// <summary>Gets the Client Id.</summary>
        public Guid Client { get; init; }

        /// <summary>Gets the Tenant Id.</summary>
        public Guid Tenant { get; init; }

        /// <summary>Gets the Scopes.</summary>
        public IEnumerable<string> Scopes { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthParams"/> class.
        /// </summary>
        /// <param name="client">Client Id.</param>
        /// <param name="tenant">Tenant Id.</param>
        /// <param name="scopes">Scopes.</param>
        public AuthParams(string client, string tenant, IEnumerable<string> scopes)
        {
            this.Client = new Guid(client);
            this.Tenant = new Guid(tenant);
            this.Scopes = scopes;
        }
    }
}
