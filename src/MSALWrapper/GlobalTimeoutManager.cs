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
        /// Determine if user should be warned as the timeout approaches.
        /// </summary>
        /// <returns>True if the user should be warned.</returns>
        public static bool WarnUser()
        {
            return GetRemainingTime() <= TimeSpan.FromSeconds(50);
        }

        /// <summary>
        ///  Set the global timeout.
        /// </summary>
        /// <param name="timeout"> Number of seconds for timeout.</param>
        public static void SetTimeout(float timeout)
        {
            globalTimeout = TimeSpan.FromSeconds(timeout);
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