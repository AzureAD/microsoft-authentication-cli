// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
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
        private const int NumberOfThreads = 10;

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
                using (new GlobalLock(this.lockName))
                {
                    sum++;
                }
            }).Wait();
            sum.Should().Be(1);
        }

        /// <summary>
        /// Test mutex in tasks.
        /// </summary>
        [Test]
        public void TestMutexInTasks()
        {
            Semaphore semaphore = new Semaphore(1, 1);
            int sum = 0;
            var tasks = new List<Task>();
            for (int i = 0; i < NumberOfThreads; i++)
            {
                var task = new Task(() =>
                {
                    using (new GlobalLock(this.lockName))
                    {
                        semaphore.WaitOne(0).Should().BeTrue($"The thread should be blocked by {nameof(Thread)}");

                        sum++;
                        Thread.Sleep(1);
                        semaphore.Release();
                    }
                });
                task.Start();
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            sum.Should().Be(NumberOfThreads);
        }

        /// <summary>
        /// Test mutex in threads.
        /// </summary>
        [Test]
        public void TestMutexInThreads()
        {
            Semaphore semaphore = new Semaphore(1, 1);
            int sum = 0;
            var threads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                var thread = new Thread(() =>
                {
                    using (new GlobalLock(this.lockName))
                    {
                        semaphore.WaitOne(0).Should().BeTrue($"The thread should be blocked by {nameof(Thread)}");

                        sum++;
                        Thread.Sleep(1);
                        semaphore.Release();
                    }
                });
                thread.Start();
                threads.Add(thread);
            }

            threads.ForEach(t => t.Join());
            sum.Should().Be(NumberOfThreads);
        }
    }
}
