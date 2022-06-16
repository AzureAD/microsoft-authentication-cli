// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using Moq;

    using NLog.Extensions.Logging;
    using NLog.Targets;

    using NUnit.Framework;

    internal class AuthFlowFactoryTest
    {
        private static readonly Guid ResourceId = new Guid("6e979987-a7c8-4604-9b37-e51f06f08f1a");
        private static readonly Guid ClientId = new Guid("5af6def2-05ec-4cab-b9aa-323d75b5df40");
        private static readonly Guid TenantId = new Guid("8254f6f7-a09f-4752-8bd6-391adc3b912e");

        private Mock<IPCAWrapper> pcaWrapperMock;
        private Mock<IPlatformUtils> platformUtilsMock;
        private MemoryTarget logTarget;
        private ServiceProvider serviceProvider;
        private ILogger logger;
        private IEnumerable<string> scopes;
        private string osxKeyChainSuffix;
        private string preferredDomain;
        private string promptHint;

        [SetUp]
        public void Setup()
        {
            // Setup in memory logging target with NLog - allows making assertions against what has been logged.
            var loggingConfig = new NLog.Config.LoggingConfiguration();
            this.logTarget = new MemoryTarget("memory_target");
            loggingConfig.AddTarget(this.logTarget);
            loggingConfig.AddRuleForAllLevels(this.logTarget);

            this.serviceProvider = new ServiceCollection()
             .AddLogging(loggingBuilder =>
             {
                 // configure Logging with NLog
                 loggingBuilder.ClearProviders();
                 loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                 loggingBuilder.AddNLog(loggingConfig);
             })
             .BuildServiceProvider();

            // Always setup Mock with behavior strict - which fails tests on first use of non-mocked behavior.
            // Reminder: If adding a new Mock - also call VerifyAll() in the TearDown method below to assert that
            // all mocked calls were called.
            this.pcaWrapperMock = new Mock<IPCAWrapper>(MockBehavior.Strict);
            this.platformUtilsMock = new Mock<IPlatformUtils>(MockBehavior.Strict);

            this.logger = this.serviceProvider.GetService<ILogger<AuthFlowFactory>>();
            this.scopes = new[] { $"{ResourceId}/.default" };
            this.osxKeyChainSuffix = "azureauth";
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
                mode,
                ClientId,
                TenantId,
                this.scopes,
                this.preferredDomain,
                this.promptHint,
                this.osxKeyChainSuffix,
                pcaWrapper: this.pcaWrapperMock.Object,
                platformUtils: this.platformUtilsMock.Object);

        [Test]
        public void Web_Only()
        {
            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Web);

            subject.Should().HaveCount(1);
            subject.First().GetType().Name.Should().Be(typeof(Web).Name);
        }

#if PlatformWindows
        [Test]
        public void IntegratedWindowsAuthentication_Only()
        {
            this.MockIsWindows(true);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.IWA);

            subject.Should().HaveCount(1);
            subject.First().GetType().Name.Should().Be(typeof(IntegratedWindowsAuthentication).Name);
        }

        [Test]
        public void Broker_Only()
        {
            this.MockIsWindows10Or11(true);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Broker);

            subject.Should().HaveCount(1);
            subject.First().GetType().Name.Should().Be(typeof(Broker).Name);
        }

        [Test]
        public void Windows10Or11_Defaults()
        {
            this.MockIsWindows(true);
            this.MockIsWindows10Or11(true);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Default);

            subject.Should().HaveCount(3);

            // BeEquivalentTo doesn't assert order for a list :(
            // so explicitly assert the first and second item names.
            var names = subject.Select(a => a.GetType().Name).ToList();
            names[0].Should().Be(typeof(IntegratedWindowsAuthentication).Name);
            names[1].Should().Be(typeof(Broker).Name);
            names[2].Should().Be(typeof(Web).Name);
        }

        [Test]
        public void Windows_Defaults()
        {
            this.MockIsWindows(true);
            this.MockIsWindows10Or11(false);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.Default);

            subject.Should().HaveCount(2);
            var names = subject.Select(a => a.GetType().Name).ToList();
            names[0].Should().Be(typeof(IntegratedWindowsAuthentication).Name);
            names[1].Should().Be(typeof(Web).Name);
        }

        [Test]
        public void Windows10Or11_All()
        {
            this.MockIsWindows(true);
            this.MockIsWindows10Or11(true);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.All);

            subject.Should().HaveCount(4);

            // BeEquivalentTo doesn't assert order for a list :(
            // so explicitly assert the first and second item names.
            var names = subject.Select(a => a.GetType().Name).ToList();
            names[0].Should().Be(typeof(IntegratedWindowsAuthentication).Name);
            names[1].Should().Be(typeof(Broker).Name);
            names[2].Should().Be(typeof(Web).Name);
            names[3].Should().Be(typeof(DeviceCode).Name);
        }

        [Test]
        public void Windows_All()
        {
            this.MockIsWindows(true);
            this.MockIsWindows10Or11(false);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.All);

            subject.Should().HaveCount(3);

            // BeEquivalentTo doesn't assert order for a list :(
            // so explicitly assert the first and second item names.
            var names = subject.Select(a => a.GetType().Name).ToList();
            names[0].Should().Be(typeof(IntegratedWindowsAuthentication).Name);
            names[1].Should().Be(typeof(Web).Name);
            names[2].Should().Be(typeof(DeviceCode).Name);
        }
#endif

        [Test]
        public void DeviceCode_Only()
        {
            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.DeviceCode);

            this.pcaWrapperMock.VerifyAll();
            subject.Should().HaveCount(1);
            subject.First().GetType().Name.Should().Be(typeof(DeviceCode).Name);
        }

        [Test]
        [Platform("Win")]
        public void AllModes_Windows()
        {
            this.MockIsWindows(true);
            this.MockIsWindows10Or11(false);

            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.All);

            this.pcaWrapperMock.VerifyAll();
            subject.Should().HaveCount(3);
            subject
                .Select(flow => flow.GetType().Name)
                .Should()
                .BeEquivalentTo(new[]
                {
                    typeof(IntegratedWindowsAuthentication).Name,
                    typeof(Web).Name,
                    typeof(DeviceCode).Name,
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
                .Select(flow => flow.GetType().Name)
                .Should()
                .BeEquivalentTo(new[]
                {
                    typeof(IntegratedWindowsAuthentication).Name,
                    typeof(Broker).Name,
                    typeof(Web).Name,
                    typeof(DeviceCode).Name,
                });
        }

        [Test]
        [Platform("MacOsX")]
        public void AllModes_Mac()
        {
            IEnumerable<IAuthFlow> subject = this.Subject(AuthMode.All);

            this.pcaWrapperMock.VerifyAll();
            subject.Should().HaveCount(2);
            subject
                .Select(flow => flow.GetType().Name)
                .Should()
                .BeEquivalentTo(new[]
                {
                    typeof(Web).Name,
                    typeof(DeviceCode).Name,
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
            subject.Select(async => async.GetType().Name).Should().BeEquivalentTo(new[]
            {
                typeof(Web).Name,
            });
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
