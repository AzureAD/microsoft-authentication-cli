// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The auth flows class.
    /// </summary>
    public class AuthFlowExecutor : IAuthFlow
    {
        private readonly IEnumerable<IAuthFlow> authflows;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthFlowExecutor"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="authFlows">The list of auth flows.</param>
        public AuthFlowExecutor(ILogger logger, IEnumerable<IAuthFlow> authFlows)
        {
            this.logger = logger;
            this.authflows = authFlows;
        }

        /// <summary>
        /// Gets the auth flow result.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            AuthFlowResult authFlowResult = new AuthFlowResult(null, new List<Exception>());
            foreach (var authFlow in this.authflows)
            {
                var result = await authFlow.GetTokenAsync();
                if (result == null)
                {
                    authFlowResult.Errors.Add(new Exception("This is a catastrophic failure. AuthFlow result is null!"));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        authFlowResult.Errors.Add(error);
                    }

                    if (result.Success)
                    {
                        authFlowResult.TokenResult = result.TokenResult;
                        break;
                    }
                }
            }

            return authFlowResult;
        }
    }
}
