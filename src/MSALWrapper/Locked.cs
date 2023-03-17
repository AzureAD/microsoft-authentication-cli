// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// A method for executing a <see cref="Func{T}"/> under an inter-process lock.
    /// </summary>
    public static class Locked
    {
        /// <summary>
        /// <see cref="Mutex"/> visibility levels between terminal server sessions.
        /// </summary>
        public enum Visibility
        {
            Local,
            Global,
        }

        /// <summary>
        /// Execute the given <paramref name="subject"/> under the <paramref name="lockName"/> as a "Local\" inter-process lock.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> you want to return in a locked blocked.</typeparam>
        /// <param name="logger">An <see cref="ILogger"/> to use for logging.</param>
        /// <param name="lockName">A lock name. This will be treated like file on *nix systems and will be path sanitized.</param>
        /// <param name="maxLockWaitTime">The max amount of time to wait to acquire the lock.</param>
        /// <param name="subject">A <see cref="Func{TResult}"/> representing the action to take while holding the lock.</param>
        /// <param name="visibility">The <see cref="Visibility"/> of the <see cref="Mutex"/>.</param>
        /// <returns>A <typeparamref name="T"/> result.</returns>
        /// <exception cref="TimeoutException">This lock will attempt to wait for the <paramref name="maxLockWaitTime"/>, and if not acquired throws a <see cref="TimeoutException"/>.</exception>
        public static T Execute<T>(ILogger logger, string lockName, TimeSpan maxLockWaitTime, Func<Task<T>> subject, Visibility visibility = Visibility.Local)
        {
            T result = default;

            lockName = LockName(lockName, visibility);

            // The first parameter 'initiallyOwned' indicates whether this lock is owned by current thread.
            // It should be false otherwise a deadlock could occur.
            using (Mutex mutex = new Mutex(initiallyOwned: false, name: lockName))
            {
                bool lockAcquired = false;
                try
                {
                    // Wait for other sessions to exit.
                    lockAcquired = mutex.WaitOne(maxLockWaitTime);
                }
                catch (AbandonedMutexException)
                {
                    // An AbandonedMutexException could be thrown if another process exits without releasing the mutex correctly.
                    // If another process crashes or exits accidentally, we can still acquire the lock.
                    lockAcquired = true;

                    // In this case, basically we can just leave a log warning, because the worst side effect is prompting more than once.
                    logger.LogWarning("Another thread or process may have exited unexpectedly, while holding AzureAuth resources.");
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

        /// <summary>
        /// Create a safe lockname.
        /// </summary>
        /// <param name="lockName">The lock name.</param>
        /// <param name="visibility">The Mutex <see cref="Visibility"/>.</param>
        /// <returns>A safe lock name derived from <paramref name="visibility"/> and <paramref name="lockName"/>.</returns>
        public static string LockName(string lockName, Visibility visibility)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(lockName));
                return string.Concat(new[] { $"{visibility}\\" }.Concat(hash.Select(b => b.ToString("x2"))));
            }
        }
    }
}
