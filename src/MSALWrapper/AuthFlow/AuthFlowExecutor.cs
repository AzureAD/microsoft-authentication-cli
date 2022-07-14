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

        #region Public configurable properties

        /// <summary>
        /// The time we want to wait before polling.
        /// </summary>
        private TimeSpan delayPeriodForPolling = TimeSpan.FromMinutes(1);
        #endregion

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

            foreach (var authFlow in this.authflows)
            {
                var authFlowName = authFlow.GetType().Name;
                this.logger.LogDebug($"Starting {authFlowName}...");
                Stopwatch watch = Stopwatch.StartNew();
                var attempt = await this.GetTokenAndPoll(authFlow);
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
        private async Task<AuthFlowResult> GetTokenAndPoll(IAuthFlow authFlow)
        {
            var flowResult = Task.Run(() => authFlow.GetTokenAsync());
            var authFlowName = authFlow.GetType().Name;
            var oneMinute = TimeSpan.FromMinutes(1);
            while (!flowResult.IsCompleted)
            {
                if (GlobalTimeoutManager.GetElapsedTime() >= oneMinute)
                {
                    this.logger.LogWarning($"Waiting on {authFlowName} authentication." +
                        $"Timeout in {GlobalTimeoutManager.GetRemainingTime():hh\\:mm\\:ss}");
                }

                if (GlobalTimeoutManager.HasTimedout())
                {
                    GlobalTimeoutManager.StopTimer();
                    this.logger.LogWarning("AzureAuth has timed out!");
                    AuthFlowResult timeoutResult = new AuthFlowResult(null, null, authFlow.GetType().Name);
                    timeoutResult.Errors.Add(new TimeoutException($"The application has timed out while waiting on {authFlowName}"));
                    /* Note that though the task running the auth flow will be killed once we return from this method,
                     * the interactive auth prompt will be killed as we exit the application (possibly due to the way GC works).
                     */
                    return timeoutResult;
                }

                await Task.WhenAny(Task.Delay(this.DetermineDelayPeriod()), flowResult);
            }

            return await flowResult;
        }

        /// <summary>
        /// Determines amount of time to wait before polling.
        /// If global timeout is in less than default delayPeriodForPolling, we would want to wait for a period equal to
        /// timeout rather than the delayPeriodForPolling.
        /// </summary>
        /// <returns>Time to wait before polling.</returns>
        private TimeSpan DetermineDelayPeriod()
        {
            return GlobalTimeoutManager.GetRemainingTime() < this.delayPeriodForPolling ?
                GlobalTimeoutManager.GetRemainingTime() : this.delayPeriodForPolling;
        }
    }
}
