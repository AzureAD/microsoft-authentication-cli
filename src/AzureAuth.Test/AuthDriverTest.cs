// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test
{
    using FluentAssertions;

    using Microsoft.Authentication.AzureAuth;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Interfaces;

    using Moq;

    using NLog.Targets;
    using NUnit.Framework;

    internal class AuthDriverTest
    {
        private ILogger logger;
        private MemoryTarget logTarget;
        private Mock<IEnv> env;
        private Mock<ITelemetryService> telemetryService;
        private Mock<ITokenFetcher> tokenFetcher;

        [SetUp]
        public void SetUp()
        {
            (this.logger, this.logTarget) = MemoryLogger.Create();
            this.env = new Mock<IEnv>(MockBehavior.Strict);
            this.telemetryService = new Mock<ITelemetryService>(MockBehavior.Strict);
            this.tokenFetcher = new Mock<ITokenFetcher>(MockBehavior.Strict);
        }

        public AuthDriver Subject() => new AuthDriver(this.logger, this.env.Object, this.telemetryService.Object, this.tokenFetcher.Object);

        [Test]
        public void Contructor_Works()
        {
            this.Subject().Should().NotBeNull();
        }
    }
}
