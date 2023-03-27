// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using FluentAssertions;
    using Microsoft.Authentication.MSALWrapper;
    using NUnit.Framework;

    public class StopwatchTrackerTest
    {
        [Test]
        public void RemainingTime_ShouldBe_LessThan_InitialTimeout()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(10);
            StopwatchTracker stopwatch = this.Subject(timeout);
            stopwatch.Start();
            stopwatch.Remaining().Should().BeLessThan(timeout);
        }

        [Test]
        public void TimedOut_Returns_True()
        {
            StopwatchTracker stopwatch = this.Subject(TimeSpan.FromSeconds(0));
            stopwatch.Start();
            stopwatch.TimedOut().Should().BeTrue();
        }

        [Test]
        public void TimedOut_Returns_False()
        {
            StopwatchTracker stopwatch = this.Subject(TimeSpan.FromMinutes(10));
            stopwatch.Start();
            stopwatch.TimedOut().Should().BeFalse();
        }

        [Test]
        public void Elapsed_ShouldBe_GreaterThan_Zero()
        {
            StopwatchTracker stopwatch = this.Subject(TimeSpan.FromSeconds(0));
            stopwatch.Start();
            stopwatch.Elapsed().Should().BeGreaterThan(TimeSpan.FromSeconds(0));
        }

        private StopwatchTracker Subject(TimeSpan timeout)
        {
            return new StopwatchTracker(timeout);
        }
    }
}
