// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;

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
            this.MockIsMacOS(false);

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
            this.MockIsMacOSBrokerAvailable(false);
            this.MockIsLinuxBrokerAvailable(false);
            this.MockIsMacOS(false);
            this.MockIsLinux(false);

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
            this.MockIsMacOS(false);

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
            this.MockIsMacOSBrokerAvailable(false);
            this.MockIsLinuxBrokerAvailable(false);
            this.MockIsMacOS(false);
            this.MockIsLinux(false);

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
            this.MockIsMacOSBrokerAvailable(false);
            this.MockIsLinuxBrokerAvailable(false);
            this.MockIsMacOS(false);
            this.MockIsLinux(false);

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
            this.MockIsMacOS(false);

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
            this.MockIsMacOSBrokerAvailable(true);
            this.MockIsWindows10Or11(false);
            this.MockIsMacOS(true);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.All);

            this.pcaWrapperMock.VerifyAll();
            subject.Should().HaveCount(4);
            subject
                .Select(flow => flow.GetType())
                .Should()
                .BeEquivalentTo(new[]
                {
                    typeof(CachedAuth),
                    typeof(Broker),
                    typeof(Web),
                    typeof(DeviceCode),
                });
        }

        [Test]
        [Platform("MacOsx")]
        public void DefaultModes_Not_Windows()
        {
            // On macOS, default mode is Web only (broker is opt-in via --mode broker).
            var subject = this.Subject(AuthMode.Default);

            subject.Should().HaveCount(2);
            subject
                .Select(a => a.GetType())
                .Should()
                .ContainInOrder(
                    typeof(CachedAuth),
                    typeof(Web));
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        [Platform("MacOsX")]
        public async Task BrokerAndWeb_Mac_UsesCacheThenBrokerThenInteractiveWeb(bool cacheHasToken, bool brokerSucceeds)
        {
            var platform = new Mock<IPlatformUtils>();
            platform.Setup(p => p.IsMacOSBrokerAvailable()).Returns(true);
            platform.Setup(p => p.IsMacOS()).Returns(true);
            var account = new Mock<IAccount>();
            account.Setup(a => a.Username).Returns("test@contoso.com");
            var token = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken), Guid.NewGuid());

            this.pcaWrapperMock
                .Setup(p => p.TryToGetCachedAccountAsync(this.preferredDomain))
                .ReturnsAsync(account.Object);
            var silentCalls = this.pcaWrapperMock
                .SetupSequence(p => p.GetTokenSilentAsync(this.authParams.Scopes, account.Object, It.IsAny<CancellationToken>()));
            if (cacheHasToken)
            {
                silentCalls.ReturnsAsync(token);
            }
            else
            {
                silentCalls.ThrowsAsync(new MsalUiRequiredException("interaction_required", "Cache miss"));
                if (brokerSucceeds)
                {
                    silentCalls.ReturnsAsync(token);
                }
                else
                {
                    silentCalls.ThrowsAsync(new MsalServiceException("redirect_uri_mismatch", "Broker redirect URI mismatch"));
                }
            }

            if (!cacheHasToken && !brokerSucceeds)
            {
                this.pcaWrapperMock.Setup(p => p.WithPromptHint(this.promptHint)).Returns(this.pcaWrapperMock.Object);
                this.pcaWrapperMock
                    .Setup(p => p.GetTokenInteractiveAsync(this.authParams.Scopes, account.Object, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(token);
            }

            var flows = AuthFlowFactory.Create(
                this.logger, this.authParams, AuthMode.Broker | AuthMode.Web,
                this.preferredDomain, this.promptHint, this.pcaWrapperMock.Object, platform.Object);
            var executor = new AuthFlowExecutor(this.logger, flows, new StopwatchTracker(TimeSpan.FromSeconds(5)));
            var results = (await executor.GetTokenAsync()).ToList();

            results.Select(r => r.AuthFlowName).Should().Equal(
                cacheHasToken ? new[] { "cache" } : brokerSucceeds ? new[] { "cache", "broker" } : new[] { "cache", "broker", "web" });
            results.Last().TokenResult.Should().BeSameAs(token);
            results.Last().TokenResult.IsSilent.Should().Be(cacheHasToken || brokerSucceeds);
            this.pcaWrapperMock.Verify(
                p => p.GetTokenSilentAsync(this.authParams.Scopes, account.Object, It.IsAny<CancellationToken>()),
                Times.Exactly(cacheHasToken ? 1 : 2));
            this.pcaWrapperMock.Verify(
                p => p.GetTokenInteractiveAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IAccount>(), It.IsAny<CancellationToken>()),
                cacheHasToken || brokerSucceeds ? Times.Never() : Times.Once());
        }

        [Test]
        [Platform("MacOsX")]
        public void BrokerRequested_Mac_CP_Unavailable_SkipsBroker()
        {
            this.MockIsWindows10Or11(false);
            this.MockIsMacOS(true);
            this.MockIsMacOSBrokerAvailable(false);
            this.MockIsLinuxBrokerAvailable(false);

            // Broker is silently skipped; only CachedAuth remains when no other modes are requested.
            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Broker);

            subject.Should().HaveCount(1);
            subject.First().Should().BeOfType<CachedAuth>();
        }

        [Test]
        [Platform("MacOsX")]
        public void BrokerAndWeb_Mac_CP_Unavailable_FallsThrough()
        {
            this.MockIsWindows10Or11(false);
            this.MockIsMacOS(true);
            this.MockIsMacOSBrokerAvailable(false);
            this.MockIsLinuxBrokerAvailable(false);

            // Broker is skipped but web is still added — fall-through pattern.
            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Broker | AuthMode.Web);

            subject.Should().HaveCount(2);
            subject
                .Select(flow => flow.GetType())
                .Should()
                .ContainInOrder(
                    typeof(CachedAuth),
                    typeof(Web));
        }

        [Test]
        [Platform("Linux")]
        public void BrokerRequested_Linux_BrokerAvailable()
        {
            this.MockIsWindows10Or11(false);
            this.MockIsMacOSBrokerAvailable(false);
            this.MockIsLinuxBrokerAvailable(true);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Broker);

            subject.Should().ContainSingle().Which.Should().BeOfType<Broker>();
        }

        [Test]
        [Platform("Linux")]
        public void BrokerRequested_Linux_BrokerUnavailable_SkipsBroker()
        {
            this.MockIsWindows10Or11(false);
            this.MockIsMacOSBrokerAvailable(false);
            this.MockIsLinuxBrokerAvailable(false);
            this.MockIsMacOS(false);
            this.MockIsLinux(true);

            // Broker is silently skipped; only CachedAuth remains when no other modes are requested.
            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Broker);

            subject.Should().ContainSingle().Which.Should().BeOfType<CachedAuth>();
        }

        [Test]
        [Platform("Linux")]
        public void DefaultModes_Linux()
        {
            // On Linux, default mode is Web only (broker is opt-in via --mode broker).
            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Default);

            subject.Should().HaveCount(2);
            subject
                .Select(a => a.GetType())
                .Should()
                .ContainInOrder(typeof(CachedAuth), typeof(Web));
        }

        private void MockIsWindows10Or11(bool value)
        {
            this.platformUtilsMock.Setup(p => p.IsWindows10Or11()).Returns(value);
        }

        private void MockIsWindows(bool value)
        {
            this.platformUtilsMock.Setup(p => p.IsWindows()).Returns(value);
        }

        private void MockIsMacOS(bool value)
        {
            this.platformUtilsMock.Setup(p => p.IsMacOS()).Returns(value);
        }

        private void MockIsMacOSBrokerAvailable(bool value)
        {
            this.platformUtilsMock.Setup(p => p.IsMacOSBrokerAvailable()).Returns(value);
        }

        private void MockIsLinux(bool value)
        {
            this.platformUtilsMock.Setup(p => p.IsLinux()).Returns(value);
        }

        private void MockIsLinuxBrokerAvailable(bool value)
        {
            this.platformUtilsMock.Setup(p => p.IsLinuxBrokerAvailable()).Returns(value);
        }
    }
}
