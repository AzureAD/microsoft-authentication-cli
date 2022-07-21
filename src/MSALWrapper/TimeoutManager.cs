// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Manages timer for the CLI.
    /// </summary>
    public class TimeoutManager : ITimeoutManager
    {
        private readonly Stopwatch timer;
        private readonly TimeSpan timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutManager"/> class.
        /// </summary>
        /// <param name="timeout"> Timeout period.</param>
        public TimeoutManager(TimeSpan timeout)
        {
            this.timer = new Stopwatch();
            this.timeout = timeout;
        }

        /// <summary>
        /// Get Elapsed Time for timer.
        /// </summary>
        /// <returns> Elapsed Timespan.</returns>
        public TimeSpan GetElapsedTime()
        {
            return this.timer.Elapsed;
        }

        /// <summary>
        /// Get number of time remaining before CLI times out.
        /// </summary>
        /// <returns>Remaining time for timeout.</returns>
        public TimeSpan GetRemainingTime()
        {
            return this.timeout.Subtract(this.timer.Elapsed);
        }

        /// <summary>
        /// Check if the timer has timed out.
        /// </summary>
        /// <returns>True if CLI has timedout.</returns>
        public bool HasTimedout()
        {
            return this.timeout <= this.timer.Elapsed;
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void StartTimer()
        {
            this.timer.Start();
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void StopTimer()
        {
            this.timer.Stop();
        }
    }
}
