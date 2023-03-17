// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MSALWrapper.Test
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.Logging;

    using Moq;

    using NUnit.Framework;

    internal class LockedTest
    {
        private static readonly TimeSpan TenSec = TimeSpan.FromSeconds(10);

        private Mock<ILogger> mockLogger;

        [SetUp]
        public void SetUp()
        {
            this.mockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Test]
        public void Get_Int()
        {
            var lockName = "get_an_int";
            var subject = Locked.Execute(this.mockLogger.Object, lockName, TenSec, () => Task.FromResult(42));
            subject.Should().Be(42);
        }

        [Test]
        public void Get_String()
        {
            var lockName = "get_a_string";
            var subject = Locked.Execute(this.mockLogger.Object, lockName, TenSec, () => Task.FromResult("hi there"));
            subject.Should().Be("hi there");
        }

        [Test]
        public void Execute_TimeOut()
        {
            var lockName = "short task times out while long task is running";

            // this int will be used to signal across threads
            int lockAcquired = 0;
            var oneMs = TimeSpan.FromMilliseconds(0.25);

            Func<int> longFunc = () => Locked.Execute(this.mockLogger.Object, lockName, TenSec, async () =>
            {
                // signal that we have acquired the lock
                Interlocked.Increment(ref lockAcquired);

                // wait for the signal that our test has made it's assertion
                while (lockAcquired == 1)
                {
                    await Task.Delay(oneMs);
                }

                return 42;
            });

            Action subject = () => Locked.Execute(this.mockLogger.Object, lockName, oneMs, () => Task.FromResult(0));

            Task<int> longTask = Task.Run(longFunc);

            // wait for our signal
            while (lockAcquired == 0)
            {
                Thread.Sleep(oneMs);
            }

            subject.Should().Throw<TimeoutException>();

            // Release the long Task by signaling we've made our assertion.
            // This prevents us from abandoning the longTask which has the lock and would not actually release
            // the Mutex correctly. This could be a problem if another test accidentally re-used a lock name.
            Interlocked.Increment(ref lockAcquired);
            longTask.Result.Should().Be(42);
        }
    }
}
