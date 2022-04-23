// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MSALWrapper.Test.AuthFlow
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

        private MemoryTarget logTarget;
        private ServiceProvider serviceProvider;
        private Mock<IPCAWrapper> pcaWrapperMock;
        private ILogger logger;
        private AuthMode authMode;
        private IEnumerable<string> scopes;
        private string osxKeyChainSuffix;
        private string preferredDomain;
        private IPCAWrapper pcaWrapper;
        private string promptHint;

        [SetUp]
        public void Setup()
        {
            // Setup in memory logging target with NLog - allows making assertions against what has been logged.
            var loggingConfig = new NLog.Config.LoggingConfiguration();
            this.logTarget = new MemoryTarget("memory_target");
            loggingConfig.AddTarget(this.logTarget);
            loggingConfig.AddRuleForAllLevels(this.logTarget);

            // Setup Dependency Injection container to provide logger and out class under test (the "subject")
            this.serviceProvider = new ServiceCollection()
             .AddLogging(loggingBuilder =>
             {
                 // configure Logging with NLog
                 loggingBuilder.ClearProviders();
                 loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                 loggingBuilder.AddNLog(loggingConfig);
             })
             .BuildServiceProvider();

            this.pcaWrapperMock = new Mock<IPCAWrapper>();

            this.logger = this.serviceProvider.GetService<ILogger<AuthFlowFactory>>();
            this.authMode = AuthMode.Broker;
            this.scopes = new[] { $"{ResourceId}/.default" };
            this.osxKeyChainSuffix = "azureauth";
            this.preferredDomain = "contoso.com";
            this.pcaWrapper = this.pcaWrapperMock.Object;
            this.promptHint = "Log into Contoso!";
        }

        [Test]
        public void JustBroker()
        {
            IEnumerable<IAuthFlow> subject = AuthFlowFactory.Create(
                this.logger,
                this.authMode,
                ClientId,
                TenantId,
                this.scopes,
                this.osxKeyChainSuffix,
                this.preferredDomain,
                this.pcaWrapper,
                this.promptHint);

            subject.Should().HaveCount(1);
            subject.First().GetType().Name.Should().Be(typeof(Broker).Name);
        }
    }
}
