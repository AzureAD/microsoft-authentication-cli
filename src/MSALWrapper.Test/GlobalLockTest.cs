// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Authentication.MSALWrapper;
    using NUnit.Framework;

    /// <summary>
    /// The <see cref="GlobalLockTest"/> test.
    /// </summary>
    internal class GlobalLockTest
    {
        /// <summary>
        /// The number of threads or tasks.
        /// </summary>
        private const int N = 10;

        /// <summary>
        /// The simulated lockname.
        /// </summary>
        private string lockName;

        /// <summary>
        /// The setup.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            this.lockName = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Test mutex in single task.
        /// </summary>
        [Test]
        public void TestSingleMutex()
        {
            int sum = 0;
            Task.Run(() =>
            {
                using (var mu = new GlobalLock(this.lockName))
                {
                    sum++;
                }
            }).Wait();
            Assert.AreEqual(1, sum);
        }

        /// <summary>
        /// Test mutex in tasks.
        /// </summary>
        [Test]
        public void TestMutexInTasks()
        {
            int cas = 0;
            int sum = 0;
            var tasks = new List<Task>();
            for (int i = 0; i < N; i++)
            {
                var task = new Task(() =>
                {
                    using (new GlobalLock(this.lockName))
                    {
                        if (Interlocked.CompareExchange(ref cas, 1, 0) == 1)
                        {
                            Assert.Fail($"The thread should be blocked by {nameof(Thread)}");
                        }

                        sum++;
                        Thread.Sleep(10);
                        Interlocked.Exchange(ref cas, 0);
                    }
                });
                task.Start();
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            Assert.AreEqual(N, sum);
        }

        /// <summary>
        /// Test mutex in threads.
        /// </summary>
        [Test]
        public void TestMutexInThreads()
        {
            int cas = 0;
            int sum = 0;
            var threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                var thread = new Thread(() =>
                {
                    using (new GlobalLock(this.lockName))
                    {
                        if (Interlocked.CompareExchange(ref cas, 1, 0) == 1)
                        {
                            Assert.Fail($"The thread should be blocked by {nameof(Thread)}");
                        }

                        sum++;
                        Thread.Sleep(50);
                        Interlocked.Exchange(ref cas, 0);
                    }
                });
                thread.Start();
                threads.Add(thread);
            }

            threads.ForEach(t => t.Join());
            Assert.AreEqual(N, sum);
        }
    }
}
