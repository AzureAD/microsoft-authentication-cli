// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test.Commands.Ado
{
    using System;
    using System.Collections.Generic;

    using FluentAssertions;
    using AzureAuth.Test;
    using Microsoft.Authentication.AzureAuth;
    using Microsoft.Authentication.AzureAuth.Commands.Ado;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.IdentityModel.JsonWebTokens;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;
    using Moq;
    using NLog.Extensions.Logging;
    using NLog.Targets;
    using NUnit.Framework;

    internal class CommandTokenTest
    {
        private Mock<IEnv> mockEnv;
        private Mock<ITelemetryService> mockTelemetry;
        private Mock<IPublicClientAuth> mockPublicClientAuth;
        private IServiceProvider serviceProvider;
        private MemoryTarget logTarget;
        private CommandExecuteEventData eventData;

        [SetUp]
        public void SetUp()
        {
            this.mockEnv = new Mock<IEnv>();
            this.mockEnv.Setup(e => e.Get(It.IsAny<string>())).Returns((string)null);
            this.mockTelemetry = new Mock<ITelemetryService>();
            this.mockPublicClientAuth = new Mock<IPublicClientAuth>();
            this.eventData = new CommandExecuteEventData();

            // Setup in memory logging target with NLog - allows making assertions against what has been logged.
            var loggingConfig = new NLog.Config.LoggingConfiguration();
            this.logTarget = new MemoryTarget("memory_target")
            {
                Layout = "${message}" // Define a simple layout so we don't get timestamps in messages.
            };
            loggingConfig.AddTarget(this.logTarget);
            loggingConfig.AddRuleForAllLevels(this.logTarget);

            // Setup Dependency Injection container to provide logger.
            this.serviceProvider = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                    loggingBuilder.AddNLog(loggingConfig);
                })
                .BuildServiceProvider();
        }

        [TestCase("foobar", CommandToken.OutputMode.Token, "foobar")]
        [TestCase("foobar", CommandToken.OutputMode.HeaderValue, "Basic OmZvb2Jhcg==")]
        [TestCase("foobar", CommandToken.OutputMode.Header, "Authorization: Basic OmZvb2Jhcg==")]
        public void FormatToken_Basic(string input, CommandToken.OutputMode mode, string expected)
        {
            CommandToken.FormatToken(input, mode, Microsoft.Authentication.AzureAuth.Ado.Authorization.Basic).Should().Be(expected);
        }

        [TestCase("foobar", CommandToken.OutputMode.Token, "foobar")]
        [TestCase("foobar", CommandToken.OutputMode.HeaderValue, "Bearer foobar")]
        [TestCase("foobar", CommandToken.OutputMode.Header, "Authorization: Bearer foobar")]
        public void FormatToken_Bearer(string input, CommandToken.OutputMode mode, string expected)
        {
            CommandToken.FormatToken(input, mode, Microsoft.Authentication.AzureAuth.Ado.Authorization.Bearer).Should().Be(expected);
        }

        [Test]
        public void OnExecute_AzureAuthAdoPat_AlwaysUsed()
        {
            this.mockEnv.Setup(e => e.Get(EnvVars.AdoPat)).Returns("my-explicit-pat");

            var command = new CommandToken();
            var result = command.OnExecute(
                this.serviceProvider.GetService<ILogger<CommandToken>>(),
                this.mockEnv.Object,
                this.mockTelemetry.Object,
                this.mockPublicClientAuth.Object,
                this.eventData);

            result.Should().Be(0);
            this.mockPublicClientAuth.Verify(
                p => p.Token(It.IsAny<AuthParameters>(), It.IsAny<IEnumerable<AuthMode>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<EventData>()),
                Times.Never);
        }

        [Test]
        public void OnExecute_AdoPipeline_UsesSystemAccessToken()
        {
            this.mockEnv.Setup(e => e.Get(EnvVars.TfBuild)).Returns("True");
            this.mockEnv.Setup(e => e.Get(EnvVars.SystemAccessToken)).Returns("pipeline-token");

            var command = new CommandToken();
            var result = command.OnExecute(
                this.serviceProvider.GetService<ILogger<CommandToken>>(),
                this.mockEnv.Object,
                this.mockTelemetry.Object,
                this.mockPublicClientAuth.Object,
                this.eventData);

            result.Should().Be(0);
            this.mockPublicClientAuth.Verify(
                p => p.Token(It.IsAny<AuthParameters>(), It.IsAny<IEnumerable<AuthMode>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<EventData>()),
                Times.Never);
        }

        [Test]
        public void OnExecute_AdoPipeline_NoSystemAccessToken_ReturnsError()
        {
            this.mockEnv.Setup(e => e.Get(EnvVars.TfBuild)).Returns("True");

            var command = new CommandToken();
            var result = command.OnExecute(
                this.serviceProvider.GetService<ILogger<CommandToken>>(),
                this.mockEnv.Object,
                this.mockTelemetry.Object,
                this.mockPublicClientAuth.Object,
                this.eventData);

            result.Should().Be(1);
            this.logTarget.Logs.Should().Contain(l => l.Contains($"{EnvVars.SystemAccessToken} is not set"));
        }

        [Test]
        public void OnExecute_NotAdoPipeline_SystemAccessTokenSet_WarnsAndContinues()
        {
            this.mockEnv.Setup(e => e.Get(EnvVars.SystemAccessToken)).Returns("stale-token");
            var fakeTokenResult = new TokenResult(new JsonWebToken(Fake.Token), Guid.NewGuid());
            this.mockPublicClientAuth
                .Setup(p => p.Token(It.IsAny<AuthParameters>(), It.IsAny<IEnumerable<AuthMode>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<EventData>()))
                .Returns(fakeTokenResult);

            var command = new CommandToken();
            var result = command.OnExecute(
                this.serviceProvider.GetService<ILogger<CommandToken>>(),
                this.mockEnv.Object,
                this.mockTelemetry.Object,
                this.mockPublicClientAuth.Object,
                this.eventData);

            result.Should().Be(0);
            this.logTarget.Logs.Should().Contain(l => l.Contains("does not appear to be an Azure DevOps Pipeline environment"));

            // Verify it fell through to AAD auth (ignored the SYSTEM_ACCESSTOKEN)
            this.mockPublicClientAuth.Verify(
                p => p.Token(It.IsAny<AuthParameters>(), It.IsAny<IEnumerable<AuthMode>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<EventData>()),
                Times.Once);
        }

        [Test]
        public void OnExecute_AdoPipeline_AzureAuthAdoPat_TakesPriority()
        {
            this.mockEnv.Setup(e => e.Get(EnvVars.AdoPat)).Returns("my-explicit-pat");
            this.mockEnv.Setup(e => e.Get(EnvVars.TfBuild)).Returns("True");
            this.mockEnv.Setup(e => e.Get(EnvVars.SystemAccessToken)).Returns("pipeline-token");

            var command = new CommandToken();
            var result = command.OnExecute(
                this.serviceProvider.GetService<ILogger<CommandToken>>(),
                this.mockEnv.Object,
                this.mockTelemetry.Object,
                this.mockPublicClientAuth.Object,
                this.eventData);

            result.Should().Be(0);
            this.logTarget.Logs.Should().Contain(l => l.Contains(EnvVars.AdoPat));
        }
    }
}
