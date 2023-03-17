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
            this.mockLogger = new Mock<ILogger>();
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
            var shortTimeOut = TimeSpan.FromMilliseconds(1);

            AutoResetEvent hasLock = new AutoResetEvent(false);
            AutoResetEvent assertionMade = new AutoResetEvent(false);

            Func<int> longFunc = () => Locked.Execute(this.mockLogger.Object, lockName, TenSec, () =>
            {
                // Signal that we have the lock, and wait for the assertion to be made.
                hasLock.Set();
                assertionMade.WaitOne();
                return Task.FromResult(42);
            });

            Action subject = () => Locked.Execute(this.mockLogger.Object, lockName, shortTimeOut, () => Task.FromResult(0));

            // Start longFunc, and wait for the lock to be acquired
            Task<int> longTask = Task.Run(longFunc);
            hasLock.WaitOne();
            subject.Should().Throw<TimeoutException>();

            // Release the long Task by signaling we've made our assertion.
            // This prevents us from abandoning the longTask which has the lock and would not actually release
            // the Mutex correctly. This could be a problem if another test accidentally re-used a lock name.
            assertionMade.Set();
            longTask.Result.Should().Be(42);
        }

        [Test]
        public void Abandon_The_Mutex()
        {
            var lockName = "this mutex is going to be poisoned (abandoned)";
            var tenSeconds = TimeSpan.FromMilliseconds(10_000);

            AutoResetEvent hasLock = new AutoResetEvent(false);
            Mutex m = new Mutex(false, lockName);

            // acquire the same mutex that our Subject will attempt to acquire.
            new Thread(() =>
            {
                m.WaitOne();
                hasLock.Set();
            }).Start();

            // Once lock is acquired, we can start our second task which waits for the lock.
            hasLock.WaitOne();
            int subject = Locked.Execute(this.mockLogger.Object, lockName, tenSeconds, () => Task.FromResult(13));
            subject.Should().Be(13);
        }

        // https://stackoverflow.com/questions/1976007/what-characters-are-forbidden-in-windows-and-linux-directory-names
        [TestCase("c99d8886-3cf5-4d44-b04c-6789bec9e1c8\\.default")]
        [TestCase("Local\\c99d8886-3cf5-4d44-b04c-6789bec9e1c8/.default")]
        [TestCase("LPT1.txt")]
        [TestCase("Local\\LPT1.txt")]
        [TestCase("bad_windows_<")]
        [TestCase("bad_windows_>")]
        [TestCase("bad_windows_\"")]
        [TestCase("bad_windows_|")]
        [TestCase("bad_windows_?")]
        [TestCase("bad_windows_*")]
        [TestCase("bad_windows_ ")]
        [TestCase("bad_mac_n_win_:")]
        [TestCase("bad_on_nix/.default")]
        [TestCase("CON")]
        [TestCase("CON.txt")]
        public void LockNames_Are_Made_Safe(string lockName, Locked.Visibility visibility = Locked.Visibility.Local)
        {
            var timeout = TimeSpan.FromMilliseconds(10);
            var subject = Locked.Execute(this.mockLogger.Object, lockName, timeout, () => Task.FromResult(0));
            subject.Should().Be(0);
        }

        [TestCase("foobar", Locked.Visibility.Local, "Local\\c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2")]
        [TestCase("foobar", Locked.Visibility.Global, "Global\\c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2")]
        public void LockName_Contains_Visibility(string lockName, Locked.Visibility visibility, string expected)
        {
            Locked.LockName(lockName, visibility).Should().Be(expected);
        }
    }
}
