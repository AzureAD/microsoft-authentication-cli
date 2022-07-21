// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MSALWrapper.Test
{
    using System;
    using FluentAssertions;
    using Microsoft.Authentication.MSALWrapper;
    using NUnit.Framework;

    public class TimeoutManagerTest
    {
        [Test]
        public void RemainingTime_ShouldBe_LessThan_InitialTimeout()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(10);
            TimeoutManager timeoutManager = this.Subject(timeout);
            timeoutManager.StartTimer();
            timeoutManager.GetRemainingTime().Should().BeLessThan(timeout);
        }

        [Test]
        public void HasTimedout_Returns_True()
        {
            TimeoutManager timeoutManager = this.Subject(TimeSpan.FromSeconds(0));
            timeoutManager.StartTimer();
            timeoutManager.HasTimedout().Should().BeTrue();
        }

        [Test]
        public void HasTimedout_Returns_False()
        {
            TimeoutManager timeoutManager = this.Subject(TimeSpan.FromMinutes(10));
            timeoutManager.StartTimer();
            timeoutManager.HasTimedout().Should().BeFalse();
        }

        [Test]
        public void ElapsedTime_ShouldBe_GreaterThan_Zero()
        {
            TimeoutManager timeoutManager = this.Subject(TimeSpan.FromSeconds(0));
            timeoutManager.StartTimer();
            timeoutManager.GetElapsedTime().Should().BeGreaterThan(TimeSpan.FromSeconds(0));
        }

        private TimeoutManager Subject(TimeSpan timeout)
        {
            return new TimeoutManager(timeout);
        }
    }
}
