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
    /// GlobalLock is used for preventing code from entering critical sections in different processes at the same time.
    /// </summary>
    public class GlobalLock : IDisposable
    {
        private Mutex mutex;
        private bool disposedValue;
        private bool lockAccqired;

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

            this.mutex = new Mutex(false, lockName);
            try
            {
                this.mutex.WaitOne();
                this.lockAccqired = true;
            }
            catch (AbandonedMutexException)
            {
                // lock eventually acquired
                this.lockAccqired = true;
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
                    if (this.lockAccqired)
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
