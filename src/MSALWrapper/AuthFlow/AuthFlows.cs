// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A list of <see cref="IAuthFlow"/> to be executed by an <see cref="AuthFlowExecutor"/>.
    /// </summary>
    public class AuthFlows
    {
        /// <summary>
        /// The Id of this group of auth flows.
        /// </summary>
        public readonly string Id;

        private readonly IEnumerable<IAuthFlow> authFlows;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthFlows"/> class.
        /// </summary>
        /// <param name="client">The client for these <paramref name="authFlows"/>.</param>
        /// <param name="tenant">The tenant for these <paramref name="authFlows"/>.</param>
        /// <param name="authFlows">An <see cref="IEnumerable{IAuthFlow}"/> of auth flows.</param>
        public AuthFlows(Guid client, Guid tenant, IEnumerable<IAuthFlow> authFlows)
        {
            this.Id = $"{client}_{tenant}";
            this.authFlows = authFlows;
        }
    }
}
