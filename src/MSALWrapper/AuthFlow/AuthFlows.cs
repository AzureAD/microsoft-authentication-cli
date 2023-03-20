// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// A list of <see cref="IAuthFlow"/> to be executed by an <see cref="AuthFlowExecutor"/>.
    /// </summary>
    public class AuthFlows : IEnumerable<IAuthFlow>
    {
        /// <summary>
        /// The Id of this group of auth flows.
        /// </summary>
        public readonly string LockName;

        private readonly IEnumerable<IAuthFlow> authFlows;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthFlows"/> class.
        /// </summary>
        /// <param name="client">The client for these <paramref name="authFlows"/>.</param>
        /// <param name="tenant">The tenant for these <paramref name="authFlows"/>.</param>
        /// <param name="authFlows">An <see cref="IEnumerable{IAuthFlow}"/> of auth flows.</param>
        public AuthFlows(Guid client, Guid tenant, IEnumerable<IAuthFlow> authFlows)
        {
            this.LockName = $"{client}_{tenant}";
            this.authFlows = authFlows ?? throw new ArgumentNullException(nameof(authFlows));
        }

        /// <inheritdoc/>
        public IEnumerator<IAuthFlow> GetEnumerator()
        {
            return this.authFlows.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.authFlows).GetEnumerator();
        }
    }
}
