// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    /// <summary>
    /// An abstract class that implements the <see cref="IAuthFlow"/> interface
    /// and enforces consistent behavior for naming AuthFlows.
    /// </summary>
    public abstract class AuthFlowBase : IAuthFlow
    {
        /// <summary>The errors encountered during token acquisition.</summary>
        protected IList<Exception> errors = new List<Exception>();

        /// <summary>A <see cref="ILogger"/> to use for logging.</summary>
        protected ILogger logger;
        private static readonly HashSet<Type> ExceptionsToCatch = new HashSet<Type>()
        {
            typeof(MsalUiRequiredException),
            typeof(MsalClientException),
            typeof(MsalServiceException),
            typeof(MsalException),
            typeof(NullReferenceException),
        };

        /// <summary>
        /// Gets the name of this AuthFlow.
        /// </summary>
        /// <returns>The <see cref="string"/> representation of the authflow name.</returns>
        protected abstract string Name { get; }

        /// <inheritdoc/>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            this.errors = new List<Exception>();
            TokenResult result = null;
            try
            {
                result = await this.GetTokenInnerAsync();
            }
            catch (Exception ex) when (ExceptionsToCatch.Contains(ex.GetType()))
            {
                this.errors.Add(ex);
                this.logger.LogDebug($"Exception caught during {this.Name} token acquisition");
                this.logger.LogDebug(ex.Message);
            }

            return new AuthFlowResult(result, this.errors, this.Name);
        }

        /// <summary>
        /// The inner method required to be implemented by an AuthFlow.
        /// Performs token acquisition.
        /// </summary>
        /// <param name="account">The <see cref="IAccount"/> to attempt to use.</param>
        /// <returns>a tuple of <see cref="TokenResult"/> and <see cref="IList{Exception}"/>.</returns>
        protected abstract Task<TokenResult> GetTokenInnerAsync();
    }
}
