// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Tracks the timer for timeout.
    /// </summary>
    public class StopwatchTracker : IStopwatch
    {
        private readonly Stopwatch stopwatch;
        private readonly TimeSpan timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="StopwatchTracker"/> class.
        /// </summary>
        /// <param name="timeout"> Timeout period.</param>
        public StopwatchTracker(TimeSpan timeout)
        {
            this.stopwatch = new Stopwatch();
            this.timeout = timeout;
        }

        /// <summary>
        /// Get Elapsed time for timer.
        /// </summary>
        /// <returns> Elapsed Timespan.</returns>
        public TimeSpan Elapsed()
        {
            return this.stopwatch.Elapsed;
        }

        /// <summary>
        /// Time remaining before the timer times out.
        /// </summary>
        /// <returns>Remaining time for timeout.</returns>
        public TimeSpan Remaining()
        {
            return this.timeout - this.stopwatch.Elapsed;
        }

        /// <summary>
        /// Check if the timer has timed out.
        /// </summary>
        /// <returns>True if the timer has timed out.</returns>
        public bool TimedOut()
        {
            return this.timeout <= this.stopwatch.Elapsed;
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start()
        {
            this.stopwatch.Start();
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            this.stopwatch.Stop();
        }
    }
}
