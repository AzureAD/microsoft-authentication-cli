// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;

    /// <summary>
    /// A thin wrapper for Stopwatch to manage timeout.
    /// </summary>
    public interface IStopwatch
    {
        /// <summary>
        /// Determines if the timer has timed out.
        /// </summary>
        /// <returns>True if timer has timed out.</returns>
        bool Timedout();

        /// <summary>
        /// Get the remaining time before timeout.
        /// </summary>
        /// <returns>Remaining TimeSpan.</returns>
        TimeSpan Remaining();

        /// <summary>
        /// Get the time elapsed since timer started.
        /// </summary>
        /// <returns>Timespan elapsed for the timer.</returns>
        TimeSpan Elapsed();

        /// <summary>
        /// Start the Timer.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the timer.
        /// </summary>
        void Stop();
    }
}
