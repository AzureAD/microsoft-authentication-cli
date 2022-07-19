// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Manages global timer for the CLI.
    /// </summary>
    public class GlobalTimeoutManager : ITimeoutManager
    {
        private readonly Stopwatch globalTimer;
        private readonly TimeSpan globalTimeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalTimeoutManager"/> class.
        /// </summary>
        /// <param name="timeout"> Timeout period.</param>
        public GlobalTimeoutManager(TimeSpan timeout)
        {
            this.globalTimer = new Stopwatch();
            this.globalTimeout = timeout;
        }

        /// <summary>
        /// Get Elapsed Time for global timer.
        /// </summary>
        /// <returns> Elapsed Timespan.</returns>
        public TimeSpan GetElapsedTime()
        {
            return this.globalTimer.Elapsed;
        }

        /// <summary>
        /// Get number of time remaining before CLI times out.
        /// </summary>
        /// <returns>Remaining Time for global timeout.</returns>
        public TimeSpan GetRemainingTime()
        {
            return this.globalTimeout.Subtract(this.globalTimer.Elapsed);
        }

        /// <summary>
        /// Check if the timer has timed out.
        /// </summary>
        /// <returns>True if CLI has timedout.</returns>
        public bool HasTimedout()
        {
            return this.globalTimeout <= this.globalTimer.Elapsed;
        }

        /// <summary>
        /// Starts the global timer.
        /// </summary>
        public void StartTimer()
        {
            this.globalTimer.Start();
        }

        /// <summary>
        /// Stops the global timer.
        /// </summary>
        public void StopTimer()
        {
            this.globalTimer.Stop();
        }
    }
}
