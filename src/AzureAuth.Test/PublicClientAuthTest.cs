// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test
{
    using System;
    using System.Collections.Generic;

    using FluentAssertions;

    using Microsoft.Authentication.AzureAuth;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.IdentityModel.JsonWebTokens;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;

    using Moq;

    using NLog.Targets;

    using NUnit.Framework;

    internal class PublicClientAuthTest
    {
        private readonly Guid client = Fake.Client;
        private readonly Guid tenant = Fake.Tenant;
        private readonly IEnumerable<string> scopes = Fake.Scopes;
        private readonly string domain = Fake.Domain;
        private readonly string prompt = "AzuerAuth.Test";
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

        private ILogger logger;
        private MemoryTarget logTarget;
        private Mock<IEnv> env;
        private Mock<ITelemetryService> telemetryService;
        private Mock<IMsalWrapper> tokenFetcher;

        [SetUp]
        public void SetUp()
        {
            (this.logger, this.logTarget) = MemoryLogger.Create();
            this.env = new Mock<IEnv>(MockBehavior.Strict);
            this.telemetryService = new Mock<ITelemetryService>(MockBehavior.Strict);
            this.tokenFetcher = new Mock<IMsalWrapper>(MockBehavior.Strict);
        }

        public PublicClientAuth Subject() => new PublicClientAuth(this.logger, this.env.Object, this.telemetryService.Object, this.tokenFetcher.Object);

        [Test]
        public void Contructor_Works()
        {
            this.Subject().Should().NotBeNull();
        }

        [Test]
        public void Constructor_No_Nulls_Allowed()
        {
            Action nullLogger = () => new PublicClientAuth(null, null, null, null);
            nullLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");

            Action nullEnv = () => new PublicClientAuth(this.logger, null, null, null);
            nullEnv.Should().Throw<ArgumentNullException>().WithParameterName("env");

            Action nullTelemetryService = () => new PublicClientAuth(this.logger, this.env.Object, null, null);
            nullTelemetryService.Should().Throw<ArgumentNullException>().WithParameterName("telemetryService");

            Action nullTokenFetcher = () => new PublicClientAuth(this.logger, this.env.Object, this.telemetryService.Object, null);
            nullTokenFetcher.Should().Throw<ArgumentNullException>().WithParameterName("tokenFetcher");
        }

        [Test]
        public void Good_Auth_Returns_TokenResult()
        {
            /*
             * This is an Integration test. The components that it uses are all almost entirely unit tested already.
             * The goal of this test is to assert that how they come together and we use them.
             */

            // Arrange
            var correlationId = new Guid("6ed5394e-511d-4a45-b41d-f949bf7ec523");
            var authFlowName = "TestAuthFlow";
            var expected = new TokenResult(new JsonWebToken(Fake.Token), correlationId);
            var tokenFetcherResult = new MsalWrapper.Result()
            {
                Attempts = new List<AuthFlowResult>() { new AuthFlowResult(expected, Array.Empty<Exception>(), authFlowName) },
            };

            // We Expect TokenFetcher to be called with specific transformations on the arguments we are giving the subject.
            // * The AuthMode should be combined into 1 bit flag.
            // * The Prompt should be prefixed.
            var expectedMode = AuthMode.Web | AuthMode.DeviceCode; // These 2 auth modes chosen because they are x-plat.
            var expectedPrompt = $"AzureAuth: {this.prompt}";

            this.tokenFetcher.SetupSequence(
                t => t.AccessToken(
                    this.logger, this.client, this.tenant, this.scopes, expectedMode, this.domain, expectedPrompt, this.timeout))
                .Returns(tokenFetcherResult);

            // The AuthMode should be Combined, and run through the extension to disable interacive auth if needed.
            this.env.Setup(e => e.Get("AZUREAUTH_NO_USER")).Returns((string)null);
            this.env.Setup(e => e.Get("Corext_NonInteractive")).Returns((string)null);

            // One AuthFlow Telemetry event should be sent.
            // We don't need to assert the details of those events here because they are unit tested
            // separately in AuthFlowResultExtensionsTest for converting an AuthFlowResult to EventData.
            this.telemetryService.SetupSequence(t => t.SendEvent("authflow_TestAuthFlow", It.IsAny<EventData>()));

            EventData eventData = new EventData();

            // Act
            TokenResult subject = this.Subject().Token(
                this.client,
                this.tenant,
                this.scopes,
                new[] { AuthMode.Web, AuthMode.DeviceCode },
                this.domain,
                this.prompt,
                this.timeout,
                eventData);

            // Asserts
            subject.Should().Be(expected);

            // These logs are only those generated by this class/test.
            // What we do not see here are the debug and trace logs generated by the services that are mocked for the test.
            this.logTarget.Logs.Should().ContainInOrder("Acquired an AAD access token via TestAuthFlow in 0.00 sec");

            eventData.Properties.Should().Contain(new Dictionary<string, string>
            {
                { "silent", "False" },
                { "sid", null },
                { "succeeded_mode", "TestAuthFlow" },
            });

            eventData.Measures.Should().Contain(new Dictionary<string, double>()
            {
                { "error_count", 0 },
                { "authflow_count", 1 },
            });
        }
    }
}
