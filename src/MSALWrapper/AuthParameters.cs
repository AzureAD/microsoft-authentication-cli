// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Core authentication parameters needed
    /// </summary>
    public record AuthParameters
    {
        /// <summary>Gets the Client Id.</summary>
        public Guid Client { get; init; }

        /// <summary>Gets the Tenant Id.</summary>
        public string Tenant { get; init; }

        /// <summary>Gets the Scopes.</summary>
        public IEnumerable<string> Scopes { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthParameters"/> class.
        /// </summary>
        /// <param name="client">Client Id.</param>
        /// <param name="tenant">Tenant Id.</param>
        /// <param name="scopes">Scopes.</param>
        public AuthParameters(string client, string tenant, IEnumerable<string> scopes)
        {
            this.Client = new Guid(client);
            this.Tenant = tenant;
            this.Scopes = scopes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthParameters"/> class.
        /// </summary>
        /// <param name="client">Client Id.</param>
        /// <param name="tenant">Tenant Id.</param>
        /// <param name="scopes">Scopes.</param>
        public AuthParameters(Guid client, Guid tenant, IEnumerable<string> scopes)
        {
            this.Client = client;
            this.Tenant = tenant.ToString();
            this.Scopes = scopes;
        }
    }
}
