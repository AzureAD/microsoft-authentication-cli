// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// An abstract class that implements the <see cref="IAuthFlow"/> interface
    /// and enforces consistent behavior for naming AuthFlows.
    /// </summary>
    public abstract class AuthFlowBase : IAuthFlow
    {
        /// <inheritdoc/>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            (var result, var errors) = await this.GetTokenInnerAsync();
            return new AuthFlowResult(result, errors, this.Name());
        }

        /// <summary>
        /// The inner method required to be implemented by an AuthFlow.
        /// Performs token acquisition.
        /// </summary>
        /// <returns>a tuple of <see cref="TokenResult"/> and <see cref="IList{Exception}"/>.</returns>
        protected abstract Task<(TokenResult result, IList<Exception> errors)> GetTokenInnerAsync();

        /// <summary>
        /// The name of this AuthFlow.
        /// </summary>
        /// <returns>The <see cref="string"/> representation of the authflow name.</returns>
        protected abstract string Name();
    }
}
