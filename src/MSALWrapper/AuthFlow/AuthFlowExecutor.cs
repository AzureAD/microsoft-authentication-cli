// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The auth flows class.
    /// </summary>
    public static class AuthFlowExecutor
    {
        /// <summary>
        /// The result of running <see cref="AuthFlowExecutor"/>.
        /// </summary>
        public record Result
        {
            /// <summary>
            /// Gets the success <see cref="AuthFlowResult"/> from <see cref="Attempts"/> if one exists, null otherwise.
            /// </summary>
            public AuthFlowResult Success => this.Attempts?.FirstOrDefault(result => result.Success);

            /// <summary>
            /// Gets all the attempts made to authenticate.
            /// </summary>
            public List<AuthFlowResult> Attempts { get; init; } = new List<AuthFlowResult>();
        }

        /// <summary>
        /// The amount of time to wait before we start warning on stderr about waiting for auth.
        /// </summary>
        public static TimeSpan WarningDelay = TimeSpan.FromSeconds(20);

        private static readonly TimeSpan MaxLockWaitTime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Get a auth flow result.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="authFlows">The list of auth flows.</param>
        /// <param name="stopwatch">The stopwatch to handle timeout.</param>
        /// <param name="lockName">The name to use when locking this series of auth flow executions.</param>
        /// <returns>A <see cref="Result"/>.</returns>
        public static Result GetToken(ILogger logger, IEnumerable<IAuthFlow> authFlows, IStopwatch stopwatch, string lockName)
        {
            logger = logger ?? throw new ArgumentNullException(nameof(logger));
            authFlows = authFlows ?? throw new ArgumentNullException(nameof(authFlows));
            stopwatch = stopwatch ?? throw new ArgumentNullException(nameof(stopwatch));
            lockName = string.IsNullOrWhiteSpace(lockName) ? throw new ArgumentException($"Parameter '{nameof(lockName)}' cannot be null, empty, or whitespace") : lockName;

            if (authFlows.Count() == 0)
            {
                logger.LogWarning("Warning: There are 0 auth flows to execute!");
                return new Result();
            }

            return LockedExecute(logger, lockName, async () => await ExecuteAuthFlowsAsync(logger, authFlows, stopwatch));
        }

        private static async Task<Result> ExecuteAuthFlowsAsync(ILogger logger, IEnumerable<IAuthFlow> authFlows, IStopwatch stopwatch)
        {
            List<AuthFlowResult> results = new List<AuthFlowResult>();
            stopwatch.Start();
            foreach (var authFlow in authFlows)
            {
                var authFlowName = authFlow.GetType().Name;
                logger.LogDebug($"Starting {authFlowName}...");

                Stopwatch timer = Stopwatch.StartNew();
                var attempt = await GetTokenAndPollAsync(logger, authFlow, stopwatch);
                timer.Stop();

                if (attempt == null)
                {
                    var oopsMessage = $"Auth flow '{authFlowName}' returned a null AuthFlowResult.";
                    logger.LogDebug(oopsMessage);

                    attempt = new AuthFlowResult(null, null, authFlowName);
                    attempt.Errors.Add(new NullTokenResultException(oopsMessage));
                }

                attempt.Duration = timer.Elapsed;
                results.Add(attempt);

                if (attempt.Errors.OfType<TimeoutException>().Any())
                {
                    break;
                }

                if (attempt.Success)
                {
                    logger.LogDebug($"{authFlowName} success: {attempt.Success}.");
                    break;
                }
            }

            return new Result() { Attempts = results };
        }

        private static T LockedExecute<T>(ILogger logger, string lockName, Func<Task<T>> subject)
        {
            T result = default(T);

            // The first parameter 'initiallyOwned' indicates whether this lock is owned by current thread.
            // It should be false otherwise a deadlock could occur.
            using (Mutex mutex = new Mutex(initiallyOwned: false, name: lockName))
            {
                bool lockAcquired = false;
                try
                {
                    // Wait for other sessions to exit.
                    lockAcquired = mutex.WaitOne(MaxLockWaitTime);
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
                    result = subject().Result;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }

            return result;
        }

        // Run the auth mode in a separate task and poll to see if we hit global timeout before the auth flow completes.
        private static async Task<AuthFlowResult> GetTokenAndPollAsync(ILogger logger, IAuthFlow authFlow, IStopwatch stopwatch)
        {
            var flowResult = Task.Run(() => authFlow.GetTokenAsync());
            var authFlowName = authFlow.GetType().Name;

            while (!flowResult.IsCompleted)
            {
                if (stopwatch.TimedOut())
                {
                    stopwatch.Stop();
                    logger.LogError($"Timed out while waiting for {authFlowName} authentication!");
                    AuthFlowResult timeoutResult = new AuthFlowResult(null, null, authFlow.GetType().Name);
                    timeoutResult.Errors.Add(new TimeoutException($"Global timeout hit during {authFlowName}"));

                    // Note that though the task running the auth flow will be killed once we return from this method,
                    // the interactive auth prompt will be killed as we exit the application (possibly due to the way GC works).
                    return timeoutResult;
                }

                if (stopwatch.Elapsed() >= WarningDelay)
                {
                    logger.LogWarning($"Waiting for {authFlowName} authentication. Look for an auth prompt.");
                    logger.LogWarning($"Timeout in {stopwatch.Remaining():mm}m {stopwatch.Remaining():ss}s!");
                }

                await Task.WhenAny(Task.Delay(Delay(stopwatch)), flowResult);
            }

            return await flowResult;
        }

        /// <summary>
        /// Helps in determining right polling interval which can be different from the default
        /// at the beginning of timer and at the end of timeout period.
        /// </summary>
        /// <returns>Time to wait before polling.</returns>
        private static TimeSpan Delay(IStopwatch stopwatch)
        {
            if (stopwatch.Elapsed() < WarningDelay)
            {
                return WarningDelay;
            }
            else
            {
                return stopwatch.Remaining() < PollingInterval ?
                stopwatch.Remaining() : PollingInterval;
            }
        }
    }
}
