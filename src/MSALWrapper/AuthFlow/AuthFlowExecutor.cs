// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The auth flows class.
    /// </summary>
    public class AuthFlowExecutor
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
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.authflows = authFlows ?? throw new ArgumentNullException(nameof(authFlows));
        }

        /// <summary>
        /// Get a auth flow result.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<IEnumerable<AuthFlowResult>> GetTokenAsync()
        {
            var resultList = new List<AuthFlowResult>();

            if (this.authflows.Count() == 0)
            {
                this.logger.LogWarning("Warning: There are 0 auth modes to execute!");
            }

            Stopwatch s;
            foreach (var authFlow in this.authflows)
            {
                var authFlowName = authFlow.GetType().Name;
                this.logger.LogDebug($"Starting {authFlowName}...");

                s = Stopwatch.StartNew();
                var attempt = await authFlow.GetTokenAsync();
                s.Stop();

                if (attempt == null)
                {
                    var oopsMessage = $"Auth flow '{authFlow.GetType().Name}' returned a null AuthFlowResult.";
                    this.logger.LogDebug(oopsMessage);

                    attempt = new AuthFlowResult(null, null, authFlowName);
                    attempt.Errors.Add(new NullTokenResultException(oopsMessage));
                }

                attempt.DurationInMs = s.ElapsedMilliseconds;
                resultList.Add(attempt);

                if (attempt.Success)
                {
                    this.logger.LogDebug($"{authFlowName} success: {attempt.Success}.");
                    break;
                }
            }

            return resultList;
        }
    }
}
