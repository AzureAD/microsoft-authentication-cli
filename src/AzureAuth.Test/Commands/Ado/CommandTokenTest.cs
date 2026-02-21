// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test.Commands.Ado
{
    using System;
    using System.Collections.Generic;

    using FluentAssertions;
    using AzureAuth.Test;
    using Microsoft.Authentication.AzureAuth;
    using Microsoft.Authentication.AzureAuth.Ado;
    using Microsoft.Authentication.AzureAuth.Commands.Ado;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.IdentityModel.JsonWebTokens;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;
    using Moq;
    using NLog.Targets;
    using NUnit.Framework;

    internal class CommandTokenTest
    {
        private Mock<IEnv> mockEnv;
        private Mock<ITelemetryService> mockTelemetry;
        private Mock<IPublicClientAuth> mockPublicClientAuth;
        private ILogger logger;
        private MemoryTarget logTarget;
        private CommandExecuteEventData eventData;

        [SetUp]
        public void SetUp()
        {
            this.mockEnv = new Mock<IEnv>();
            this.mockEnv.Setup(e => e.Get(It.IsAny<string>())).Returns((string)null);
            this.mockTelemetry = new Mock<ITelemetryService>();
            this.mockPublicClientAuth = new Mock<IPublicClientAuth>();
            (this.logger, this.logTarget) = MemoryLogger.Create();
            this.eventData = new CommandExecuteEventData();
        }

        [TestCase("foobar", CommandToken.OutputMode.Token, "foobar")]
        [TestCase("foobar", CommandToken.OutputMode.HeaderValue, "Basic OmZvb2Jhcg==")]
        [TestCase("foobar", CommandToken.OutputMode.Header, "Authorization: Basic OmZvb2Jhcg==")]
        public void FormatToken_Basic(string input, CommandToken.OutputMode mode, string expected)
        {
            CommandToken.FormatToken(input, mode, Authorization.Basic).Should().Be(expected);
        }

        [TestCase("foobar", CommandToken.OutputMode.Token, "foobar")]
        [TestCase("foobar", CommandToken.OutputMode.HeaderValue, "Bearer foobar")]
        [TestCase("foobar", CommandToken.OutputMode.Header, "Authorization: Bearer foobar")]
        public void FormatToken_Bearer(string input, CommandToken.OutputMode mode, string expected)
        {
            CommandToken.FormatToken(input, mode, Authorization.Bearer).Should().Be(expected);
        }

        [Test]
        public void OnExecute_AzureAuthAdoPat_AlwaysUsed()
        {
            this.mockEnv.Setup(e => e.Get("AZUREAUTH_ADO_PAT")).Returns("my-explicit-pat");

            var command = new CommandToken();
            var result = command.OnExecute(
                (ILogger<CommandToken>)this.logger,
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
            this.mockEnv.Setup(e => e.Get("TF_BUILD")).Returns("True");
            this.mockEnv.Setup(e => e.Get("SYSTEM_ACCESSTOKEN")).Returns("pipeline-token");

            var command = new CommandToken();
            var result = command.OnExecute(
                (ILogger<CommandToken>)this.logger,
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
            this.mockEnv.Setup(e => e.Get("TF_BUILD")).Returns("True");

            var command = new CommandToken();
            var result = command.OnExecute(
                (ILogger<CommandToken>)this.logger,
                this.mockEnv.Object,
                this.mockTelemetry.Object,
                this.mockPublicClientAuth.Object,
                this.eventData);

            result.Should().Be(1);
            this.logTarget.Logs.Should().Contain(l => l.Contains("SYSTEM_ACCESSTOKEN is not set"));
        }

        [Test]
        public void OnExecute_NotAdoPipeline_SystemAccessTokenSet_WarnsAndContinues()
        {
            this.mockEnv.Setup(e => e.Get("SYSTEM_ACCESSTOKEN")).Returns("stale-token");
            var fakeTokenResult = new TokenResult(new JsonWebToken(Fake.Token), Guid.NewGuid());
            this.mockPublicClientAuth
                .Setup(p => p.Token(It.IsAny<AuthParameters>(), It.IsAny<IEnumerable<AuthMode>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<EventData>()))
                .Returns(fakeTokenResult);

            var command = new CommandToken();
            var result = command.OnExecute(
                (ILogger<CommandToken>)this.logger,
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
            this.mockEnv.Setup(e => e.Get("AZUREAUTH_ADO_PAT")).Returns("my-explicit-pat");
            this.mockEnv.Setup(e => e.Get("TF_BUILD")).Returns("True");
            this.mockEnv.Setup(e => e.Get("SYSTEM_ACCESSTOKEN")).Returns("pipeline-token");

            var command = new CommandToken();
            var result = command.OnExecute(
                (ILogger<CommandToken>)this.logger,
                this.mockEnv.Object,
                this.mockTelemetry.Object,
                this.mockPublicClientAuth.Object,
                this.eventData);

            result.Should().Be(0);
            this.logTarget.Logs.Should().Contain(l => l.Contains("AZUREAUTH_ADO_PAT"));
        }
    }
}
