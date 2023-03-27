// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal abstract class AuthFlowBase : IAuthFlow
    {
        /// <inheritdoc/>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            IList<Exception> errors = new List<Exception>();
            var result = await this.GetTokenInnerAsync(errors);
            return new AuthFlowResult(result, errors, this.Name());
        }

        /// <summary>
        /// The inner method required to be implemented by an AuthFlow.
        /// Performs token acquisition.
        /// </summary>
        /// <returns>a tuple of <see cref="TokenResult"/> and <see cref="IList{Exception}"/>.</returns>
        protected abstract Task<TokenResult> GetTokenInnerAsync(IList<Exception> errors);

        /// <summary>
        /// The name of this AuthFlow.
        /// </summary>
        /// <returns>The <see cref="string"/> representation of the authflow name.</returns>
        protected abstract string Name();
    }
}
