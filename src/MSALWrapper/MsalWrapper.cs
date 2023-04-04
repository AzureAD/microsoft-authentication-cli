// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// A functional orchestrator of doing auth using the building blocks
    /// of <see cref="AuthFlowFactory"/> and <see cref="AuthFlowExecutor"/>.
    /// </summary>
    public class MsalWrapper : IMsalWrapper
    {
        /// <summary>
        /// The result of running <see cref="MsalWrapper"/>.
        /// </summary>
        public record Result
        {
            /// <summary>
            /// Gets the success <see cref="AuthFlowResult"/> from <see cref="Attempts"/> if one exists, null otherwise.
            /// </summary>
            public AuthFlowResult Success => this.Attempts.FirstOrDefault(result => result.Success);

            /// <summary>
            /// Gets all the attempts made to authenticate.
            /// </summary>
            public List<AuthFlowResult> Attempts { get; init; }
        }

        private static readonly TimeSpan MaxLockWaitTime = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Initializes a new instance of the <see cref="MsalWrapper"/> class.
        /// </summary>
        public MsalWrapper()
        {
        }

        /// <inheritdoc/>
        public Result AccessToken(
            ILogger logger,
            AuthParameters authParams,
            AuthMode mode,
            string domain,
            string prompt,
            TimeSpan timeout)
        {
            var authFlows = AuthFlowFactory.Create(
                logger: logger,
                authMode: mode,
                authParams: authParams,
                preferredDomain: domain,
                promptHint: prompt);

            List<AuthFlowResult> results = new List<AuthFlowResult>();
            var executor = new AuthFlowExecutor(logger, authFlows, new StopwatchTracker(timeout));

            // Prevent multiple calls to AzureAuth for the same client and tenant from prompting at the same time.
            string lockName = $"Local\\{authParams.Tenant}_{authParams.Client}";

            results.AddRange(Locked.Execute(logger, lockName, MaxLockWaitTime, async () => await executor.GetTokenAsync()));

            return new Result { Attempts = results };
        }
    }
}
