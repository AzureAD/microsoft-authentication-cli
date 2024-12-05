// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System.Collections.Generic;
    using System.Linq;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;

    using Moq;

    using NLog.Targets;

    using NUnit.Framework;

    internal class AuthFlowFactoryTest
    {
        private readonly AuthParameters authParams = new AuthParameters(
            "5af6def2-05ec-4cab-b9aa-323d75b5df40",
            "8254f6f7-a09f-4752-8bd6-391adc3b912e",
            new[] { "6e979987-a7c8-4604-9b37-e51f06f08f1a/.default" });

        private MemoryTarget logTarget;
        private ILogger logger;
        private Mock<IPCAWrapper> pcaWrapperMock;
        private Mock<IPlatformUtils> platformUtilsMock;
        private string preferredDomain;
        private string promptHint;

        [SetUp]
        public void Setup()
        {
            (this.logger, this.logTarget) = MemoryLogger.Create();

            // Always setup Mock with behavior strict - which fails tests on first use of non-mocked behavior.
            // Reminder: If adding a new Mock - also call VerifyAll() in the TearDown method below to assert that
            // all mocked calls were called.
            this.pcaWrapperMock = new Mock<IPCAWrapper>(MockBehavior.Strict);
            this.platformUtilsMock = new Mock<IPlatformUtils>(MockBehavior.Strict);

            this.preferredDomain = "contoso.com";
            this.promptHint = "Log into Contoso!";
        }

        [TearDown]
        public void TearDown()
        {
            // Verify all mocks used by the test here to prevent any individual test from
            // forgetting to do this.
            // Reminder: Add a call to VerifyAll() for any new mocks used.
            this.pcaWrapperMock.VerifyAll();
            this.platformUtilsMock.VerifyAll();
        }

        public IEnumerable<IAuthFlow> Subject(AuthMode mode) => AuthFlowFactory.Create(
                this.logger,
                this.authParams,
                mode,
                this.preferredDomain,
                this.promptHint,
                pcaWrapper: this.pcaWrapperMock.Object,
                platformUtils: this.platformUtilsMock.Object);

        [Test]
        public void Web_Only()
        {
            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Web);

            subject.Should().HaveCount(2);
            subject
                .Select(a => a.GetType())
                .Should()
                .ContainInOrder(typeof(CachedAuth), typeof(Web));
        }

#if PlatformWindows
        [Test]
        public void IntegratedWindowsAuthentication_Only()
        {
            this.MockIsWindows(true);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.IWA);

            subject.Should().HaveCount(2);
            subject
                .Select(a => a.GetType())
                .Should()
                .ContainInOrder(typeof(CachedAuth), typeof(IntegratedWindowsAuthentication));
        }

        [Test]
        public void Broker_Only()
        {
            this.MockIsWindows10Or11(true);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Broker);

            subject.Should().HaveCount(1);
            subject
                .Select(a => a.GetType())
                .Should()
                .Contain(typeof(Broker));
        }

        [Test]
        public void Windows10Or11_Defaults()
        {
            this.MockIsWindows10Or11(true);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Default);

            subject.Should().HaveCount(2);
            subject
                .Select(a => a.GetType())
                .Should()
                .ContainInOrder(
                    typeof(Broker),
                    typeof(Web));
        }

        [Test]
        public void Windows_Defaults()
        {
            this.MockIsWindows10Or11(false);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Default);

            subject.Should().HaveCount(2);
            subject
                .Select(a => a.GetType())
                .Should()
                .ContainInOrder(
                    typeof(CachedAuth),
                    typeof(Web));
        }

        [Test]
        public void Windows10Or11_All()
        {
            this.MockIsWindows(true);
            this.MockIsWindows10Or11(true);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.All);

            subject.Should().HaveCount(4);
            subject
                .Select(a => a.GetType())
                .Should()
                .ContainInOrder(
                    typeof(Broker),
                    typeof(Web),
                    typeof(DeviceCode));
        }

        [Test]
        public void Windows_All()
        {
            this.MockIsWindows(true);
            this.MockIsWindows10Or11(false);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.All);

            subject.Should().HaveCount(4);
            subject
                .Select(a => a.GetType())
                .Should()
                .ContainInOrder(
                    typeof(CachedAuth),
                    typeof(IntegratedWindowsAuthentication),
                    typeof(Web),
                    typeof(DeviceCode));
        }
#endif

        [Test]
        public void DeviceCode_Only()
        {
            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.DeviceCode);

            this.pcaWrapperMock.VerifyAll();
            subject.Should().HaveCount(2);
            subject
                .Select(a => a.GetType())
                .Should()
                .ContainInOrder(
                    typeof(CachedAuth),
                    typeof(DeviceCode));
        }

        [Test]
        [Platform("Win")]
        public void AllModes_Windows()
        {
            this.MockIsWindows(true);
            this.MockIsWindows10Or11(false);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.All);

            this.pcaWrapperMock.VerifyAll();
            subject.Should().HaveCount(4);
            subject
                .Select(flow => flow.GetType())
                .Should()
                .BeEquivalentTo(new[]
                {
                    typeof(CachedAuth),
                    typeof(IntegratedWindowsAuthentication),
                    typeof(Web),
                    typeof(DeviceCode),
                });
        }

        [Test]
        [Platform("Win")]
        public void AllModes_Windows10Or11()
        {
            this.MockIsWindows(true);
            this.MockIsWindows10Or11(true);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.All);

            this.pcaWrapperMock.VerifyAll();
            subject.Should().HaveCount(4);
            subject
                .Select(flow => flow.GetType())
                .Should()
                .BeEquivalentTo(new[]
                {
                    typeof(IntegratedWindowsAuthentication),
                    typeof(Broker),
                    typeof(Web),
                    typeof(DeviceCode),
                });
        }

        [Test]
        [Platform("MacOsX")]
        public void AllModes_Mac()
        {
            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.All);

            this.pcaWrapperMock.VerifyAll();
            subject.Should().HaveCount(3);
            subject
                .Select(flow => flow.GetType())
                .Should()
                .BeEquivalentTo(new[]
                {
                    typeof(CachedAuth),
                    typeof(Web),
                    typeof(DeviceCode),
                });
        }

        [Test]
        [Platform("MacOsx")]
        public void DefaultModes_Not_Windows()
        {
            // On non-windows platforms the Default Authmode doesn't contain "Broker" as an option to start with.
            // so we short circuit checking the platform and expect it to not be called.
            var subject = this.Subject(AuthMode.Default);

            this.platformUtilsMock.VerifyAll();
            subject.Should().HaveCount(2);
            subject
                .Select(a => a.GetType())
                .Should()
                .ContainInOrder(
                    typeof(CachedAuth),
                    typeof(Web));
        }

        private void MockIsWindows10Or11(bool value)
        {
            this.platformUtilsMock.Setup(p => p.IsWindows10Or11()).Returns(value);
        }

        private void MockIsWindows(bool value)
        {
            this.platformUtilsMock.Setup(p => p.IsWindows()).Returns(value);
        }
    }
}
