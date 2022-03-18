// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// GlobalLock is a wrapper for named mutex, used for preventing code from entering critical sections in different processes at the same time.
    /// <seealso cref="Mutex"/>
    /// </summary>
    public class GlobalLock : IDisposable
    {
        /// <summary>
        /// InitiallyOwned indicated whether this lock is owned by current thread.
        /// It should be false otherwise a dead lock could occur.
        /// </summary>
        private const bool InitiallyOwnedByCurrentThread = false;

        private Mutex mutex;
        private bool disposedValue;
        private bool lockAcquired;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalLock"/> class.
        /// </summary>
        /// <param name="lockName">Unique lock ID.</param>
        /// <exception cref="ArgumentNullException">The <see cref="ArgumentNullException"/>.</exception>
        public GlobalLock(string lockName)
        {
            if (lockName == null)
            {
                throw new ArgumentNullException($"{nameof(lockName)} must not be null");
            }

            this.mutex = new Mutex(InitiallyOwnedByCurrentThread, lockName);
            try
            {
                this.mutex.WaitOne();
                this.lockAcquired = true;
            }
            catch (AbandonedMutexException)
            {
                // lock eventually acquired
                this.lockAcquired = true;
            }
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        /// <param name="disposing">disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    if (this.lockAcquired)
                    {
                        this.mutex.ReleaseMutex();
                    }

                    this.mutex.Dispose();
                }

                this.mutex = null;
                this.disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
