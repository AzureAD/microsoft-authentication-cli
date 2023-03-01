// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// A functional orchestrator of doing auth using the building blocks
    /// of <see cref="AuthFlowFactory"/> and <see cref="AuthFlowExecutor"/>.
    /// </summary>
    public static class TokenFetcher
    {
        /// <summary>
        /// The result of running <see cref="TokenFetcher"/>.
        /// </summary>
        public record TokenFetcherResult
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

        /// <summary>
        /// Run the authenticatuion process.
        /// </summary>
        /// <param name="logger">A logger.</param>
        /// <param name="client">Client.</param>
        /// <param name="tenant">Tenant.</param>
        /// <param name="scopes">Scopes.</param>
        /// <param name="mode">Modes.</param>
        /// <param name="domain">Domain.</param>
        /// <param name="prompt">Prompt Hint.</param>
        /// <param name="timeout">Timeout.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<TokenFetcherResult> AccessTokenAsync(ILogger logger, Guid client, Guid tenant, IEnumerable<string> scopes, AuthMode mode, string domain, string prompt, TimeSpan timeout)
        {
            var authFlows = AuthFlowFactory.Create(
                logger: logger,
                authMode: mode,
                clientId: client,
                tenantId: tenant,
                scopes: scopes,
                preferredDomain: domain,
                promptHint: prompt);

            var executor = new AuthFlowExecutor(logger, authFlows, new StopwatchTracker(timeout));

            List<AuthFlowResult> results = new List<AuthFlowResult>();

            // When running multiple AzureAuth processes with the same resource, client, and tenant IDs,
            // They may prompt many times, which is annoying and unexpected.
            // Use Mutex to ensure that only one process can access the corresponding resource at the same time.
            string resource = string.Join(' ', scopes);
            string lockName = $"Local\\{resource}_{client}_{tenant}";

            // First parameter initiallyOwned indicates whether this lock is owned by current thread.
            // It should be false otherwise a dead lock could occur.
            using (Mutex mutex = new Mutex(initiallyOwned: false, name: lockName))
            {
                bool lockAcquired = false;
                try
                {
                    // Wait for the other session to exit.
                    lockAcquired = mutex.WaitOne(TimeSpan.FromMinutes(15)); // TODO: Inject mutex timeout?
                }
                catch (AbandonedMutexException)
                {
                    // An AbandonedMutexException could be thrown if another process exits without releasing the mutex correctly.
                    // If another process crashes or exits accidentally, we can still acquire the lock.
                    lockAcquired = true;

                    // In this case, basically we can just leave a log warning, because the worst side effect is prompting more than once.
                    logger.LogWarning("The authentication attempt mutex was abandoned. Another thread or process may have exited unexpectedly.");
                }

                if (!lockAcquired)
                {
                    throw new TimeoutException("Authentication failed. The application did not gain access in the expected time, possibly because the resource handler was occupied by another process for a long time.");
                }

                try
                {
                    // GetTokenAsync returns an empty list instead of null so no null check required here.
                    results.AddRange(await executor.GetTokenAsync());
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }

            return new TokenFetcherResult { Attempts = results };
        }
    }
}
