// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;

    /// <summary>
    /// Timeout manager interface.
    /// </summary>
    public interface ITimeoutManager
    {
        /// <summary>
        /// Determines if the timer has timed out.
        /// </summary>
        /// <returns>True if timer has timed out.</returns>
        bool HasTimedout();

        /// <summary>
        /// Get the remaining time before timeout.
        /// </summary>
        /// <returns>Remaining TimeSpan.</returns>
        TimeSpan GetRemainingTime();

        /// <summary>
        /// Get the time elapsed since timer started.
        /// </summary>
        /// <returns>Timespan elapsed for the timer.</returns>
        TimeSpan GetElapsedTime();

        /// <summary>
        /// Start the Timer.
        /// </summary>
        void StartTimer();

        /// <summary>
        /// Stop the timer.
        /// </summary>
        void StopTimer();
    }
}
