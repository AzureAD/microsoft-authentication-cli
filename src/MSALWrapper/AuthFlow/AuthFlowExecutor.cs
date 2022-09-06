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
        /// <summary>
        /// The amount of time to wait before we start warning on stderr about waiting for auth.
        /// </summary>
        public static TimeSpan WarningDelay = TimeSpan.FromSeconds(20);

        private readonly IEnumerable<IAuthFlow> authflows;
        private readonly ILogger logger;
        private readonly IStopwatch stopwatch;

        private TimeSpan pollingInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthFlowExecutor"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="authFlows">The list of auth flows.</param>
        /// <param name="stopwatch">The stopwatch to handle timeout.</param>
        public AuthFlowExecutor(ILogger logger, IEnumerable<IAuthFlow> authFlows, IStopwatch stopwatch)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.authflows = authFlows ?? throw new ArgumentNullException(nameof(authFlows));
            this.stopwatch = stopwatch ?? throw new ArgumentNullException(nameof(stopwatch));
        }

        /// <summary>
        /// Get a auth flow result.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<IEnumerable<AuthFlowResult>> GetTokenAsync()
        {
            this.stopwatch.Start();
            var resultList = new List<AuthFlowResult>();

            if (this.authflows.Count() == 0)
            {
                this.logger.LogWarning("Warning: There are 0 auth modes to execute!");
            }

            foreach (var authFlow in this.authflows)
            {
                var authFlowName = authFlow.GetType().Name;
                this.logger.LogDebug($"Starting {authFlowName}...");
                Stopwatch watch = Stopwatch.StartNew();
                var attempt = await this.GetTokenAndPollAsync(authFlow);
                watch.Stop();

                if (attempt == null)
                {
                    var oopsMessage = $"Auth flow '{authFlowName}' returned a null AuthFlowResult.";
                    this.logger.LogDebug(oopsMessage);

                    attempt = new AuthFlowResult(null, null, authFlowName);
                    attempt.Errors.Add(new NullTokenResultException(oopsMessage));
                }

                attempt.Duration = watch.Elapsed;
                resultList.Add(attempt);

                if (attempt.Errors.OfType<TimeoutException>().Any())
                {
                    break;
                }

                if (attempt.Success)
                {
                    this.logger.LogDebug($"{authFlowName} success: {attempt.Success}.");
                    break;
                }
            }

            return resultList;
        }

        /// <summary>
        /// Run the auth mode in a separate task and
        /// poll to see if we hit global timeout before the auth flow completes.
        /// </summary>
        /// <param name="authFlow">Auth Flow that we are executing.</param>
        /// <returns>AuthFlowResult containing Error if CLI times out; else actual result of the AuthFlow.</returns>
        private async Task<AuthFlowResult> GetTokenAndPollAsync(IAuthFlow authFlow)
        {
            var flowResult = Task.Run(() => authFlow.GetTokenAsync());
            var authFlowName = authFlow.GetType().Name;

            while (!flowResult.IsCompleted)
            {
                if (this.stopwatch.TimedOut())
                {
                    this.stopwatch.Stop();
                    this.logger.LogError($"Timed out while waiting for {authFlowName} authentication!");
                    AuthFlowResult timeoutResult = new AuthFlowResult(null, null, authFlow.GetType().Name);
                    timeoutResult.Errors.Add(new TimeoutException($"Global timeout hit during {authFlowName}"));

                    // Note that though the task running the auth flow will be killed once we return from this method,
                    // the interactive auth prompt will be killed as we exit the application (possibly due to the way GC works).
                    return timeoutResult;
                }

                if (this.stopwatch.Elapsed() >= WarningDelay)
                {
                    this.logger.LogWarning($"Waiting for {authFlowName} authentication. Look for an auth prompt.");
                    this.logger.LogWarning($"Timeout in {this.stopwatch.Remaining():mm}m {this.stopwatch.Remaining():ss}s!");
                }

                await Task.WhenAny(Task.Delay(this.Delay()), flowResult);
            }

            return await flowResult;
        }

        /// <summary>
        /// Helps in determining right polling interval which can be different from the default
        /// at the beginning of timer and at the end of timeout period.
        /// </summary>
        /// <returns>Time to wait before polling.</returns>
        private TimeSpan Delay()
        {
            if (this.stopwatch.Elapsed() < WarningDelay)
            {
                return WarningDelay;
            }
            else
            {
                return this.stopwatch.Remaining() < this.pollingInterval ?
                this.stopwatch.Remaining() : this.pollingInterval;
            }
        }
    }
}
