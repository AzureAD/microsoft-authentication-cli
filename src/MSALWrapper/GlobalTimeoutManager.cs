// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Manages global timer for the CLI.
    /// </summary>
    public static class GlobalTimeoutManager
    {
        private static readonly Stopwatch GlobalTimer = new Stopwatch();
        private static TimeSpan globalTimeout;

        /// <summary>
        /// Get Elapsed Time for global timer.
        /// </summary>
        /// <returns> Elapsed Timespan.</returns>
        public static TimeSpan GetElapsedTime()
        {
            return GlobalTimer.Elapsed;
        }

        /// <summary>
        /// Get number of seconds remaining before CLI times out.
        /// </summary>
        /// <returns>Remaining Time for global timeout.</returns>
        public static TimeSpan GetRemainingTime()
        {
            return globalTimeout.Subtract(GetElapsedTime());
        }

        /// <summary>
        /// Check if CLI has timedout.
        /// </summary>
        /// <returns>True if CLI has timedout.</returns>
        public static bool HasTimedout()
        {
            return globalTimeout <= GetElapsedTime();
        }

        /// <summary>
        ///  Set the global timeout.
        /// </summary>
        /// <param name="timeout"> Timeout period.</param>
        public static void SetTimeout(TimeSpan timeout)
        {
            globalTimeout = timeout;
        }

        /// <summary>
        /// Starts the global timer.
        /// </summary>
        public static void StartTimer()
        {
            GlobalTimer.Start();
        }

        /// <summary>
        /// Stops the globat timer.
        /// </summary>
        public static void StopTimer()
        {
            GlobalTimer.Stop();
        }

        /// <summary>
        /// Reset the timer.
        /// </summary>
        public static void ResetTimer()
        {
            GlobalTimer.Reset();
        }
    }
}
