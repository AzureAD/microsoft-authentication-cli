// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using FluentAssertions;
    using Microsoft.Authentication.MSALWrapper;
    using NUnit.Framework;

    /// <summary>
    /// The auth mode test.
    /// </summary>
    internal class AuthModeTest
    {
#if !PlatformWindows
        /// <summary>
        /// All is all.
        /// </summary>
        [Test]
        public void AllIsAll()
        {
            (AuthMode.Web | AuthMode.DeviceCode).Should().Be(AuthMode.All);
        }

        /// <summary>
        /// The test for broker is expected.
        /// </summary>
        /// <param name="subject">
        /// The subject.
        /// </param>
        /// <param name="expected">
        /// The expected.
        /// </param>
        [TestCase(AuthMode.All, false)]
        [TestCase(AuthMode.Web, false)]
        [TestCase(AuthMode.DeviceCode, false)]
        public void BrokerIsExpected(AuthMode subject, bool expected)
        {
            subject.IsBroker().Should().Be(expected);
        }
#else

        /// <summary>
        /// All is all.
        /// </summary>
        [Test]
        public void AllIsAll()
        {
            (AuthMode.Broker | AuthMode.Web | AuthMode.DeviceCode).Should().Be(AuthMode.All);
        }

        /// <summary>
        /// The test for broker is expected.
        /// </summary>
        /// <param name="subject">
        /// The subject.
        /// </param>
        /// <param name="expected">
        /// The expected.
        /// </param>
        [TestCase(AuthMode.All, true)]
        [TestCase(AuthMode.Broker, true)]
        [TestCase(AuthMode.Web, false)]
        [TestCase(AuthMode.DeviceCode, false)]
        public void BrokerIsExpected(AuthMode subject, bool expected)
        {
            subject.IsBroker().Should().Be(expected);
        }

        /// <summary>
        /// The test for web is expected.
        /// </summary>
        /// <param name="subject">
        /// The subject.
        /// </param>
        /// <param name="expected">
        /// The expected.
        /// </param>
        [TestCase(AuthMode.All, true)]
        [TestCase(AuthMode.Broker, false)]
        [TestCase(AuthMode.Web, true)]
        [TestCase(AuthMode.DeviceCode, false)]
        public void WebIsExpected(AuthMode subject, bool expected)
        {
            subject.IsWeb().Should().Be(expected);
        }

        /// <summary>
        /// The test for device flow is expected.
        /// </summary>
        /// <param name="subject">
        /// The subject.
        /// </param>
        /// <param name="expected">
        /// The expected.
        /// </param>
        [TestCase(AuthMode.All, true)]
        [TestCase(AuthMode.Broker, false)]
        [TestCase(AuthMode.Web, false)]
        [TestCase(AuthMode.DeviceCode, true)]
        public void DeviceCodeIsExpected(AuthMode subject, bool expected)
        {
            subject.IsDeviceCode().Should().Be(expected);
        }

        /// <summary>
        /// The test for other combos.
        /// </summary>
        [Test]
        public void OtherCombos()
        {
            var subject = AuthMode.DeviceCode | AuthMode.Web;
            subject.IsDeviceCode().Should().BeTrue();
            subject.IsWeb().Should().BeTrue();

            subject = AuthMode.Broker | AuthMode.Web;
            subject.IsBroker().Should().BeTrue();
            subject.IsWeb().Should().BeTrue();
        }

        /// <summary>
        /// The test for Web or device code.
        /// </summary>
        [Test]
        public void WebOrDeviceCodeIsNotbroker()
        {
            var subject = AuthMode.Web | AuthMode.DeviceCode;

            subject.IsBroker().Should().BeFalse();
        }
#endif
    }
}
