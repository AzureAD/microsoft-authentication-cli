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

    public class AuthModeExtensionsTest
    {
        private Mock<IEnv> envMock;
        private ILogger logger;
        private MemoryTarget logTarget;

        [SetUp]
        public void SetUp()
        {
            this.envMock = new Mock<IEnv>();
            (this.logger, this.logTarget) = MemoryLogger.Create();
        }

#if PlatformWindows
        [Test]
        public void CombinedAuthMode_Allowed()
        {
            // Arrange
            this.envMock.Setup(e => e.Get(EnvVars.NoUser)).Returns(string.Empty);
            this.envMock.Setup(e => e.Get("Corext_NonInteractive")).Returns(string.Empty);

            var subject = new[] { AuthMode.IWA, AuthMode.Web, AuthMode.Broker };

            // Act + Assert
            subject.Combine().PreventInteractionIfNeeded(this.envMock.Object, this.logger).Should().Be(AuthMode.Default);
            this.logTarget.Logs.Should().BeEmpty();
        }

        [TestCase("AZUREAUTH_NO_USER")]
        [TestCase("Corext_NonInteractive")]
        public void Filterinteraction_Interactive_Auth_Disabled(string envVar)
        {
            // Arrange
            this.envMock.Setup(e => e.Get(envVar)).Returns("1");
            var subject = new[] { AuthMode.IWA, AuthMode.Web, AuthMode.Broker, AuthMode.DeviceCode };

            // Act + Assert
            subject.Combine().PreventInteractionIfNeeded(this.envMock.Object, this.logger).Should().Be(AuthMode.IWA);
            this.logTarget.Logs.Should().ContainInOrder("Interactive authentication is disabled.", "Only Integrated Windows Authentication will be attempted.");
        }
#else
        public void CombinedAuthMode_Allowed()
        {
            // Arrange
            this.envMock.Setup(e => e.Get(EnvVars.NoUser)).Returns(string.Empty);
            this.envMock.Setup(e => e.Get("Corext_NonInteractive")).Returns(string.Empty);

            var subject = new[] { AuthMode.Web, AuthMode.DeviceCode };

            // Act + Assert
            subject.Combine().PreventInteractionIfNeeded(this.envMock.Object, this.logger).Should().Be(AuthMode.Default);
            this.logTarget.Logs.Should().BeEmpty();
        }

        [TestCase("AZUREAUTH_NO_USER")]
        [TestCase("Corext_NonInteractive")]
        public void Filterinteraction_Interactive_Auth_Disabled(string envVar)
        {
            // Arrange
            this.envMock.Setup(e => e.Get(envVar)).Returns("1");
            var subject = new[] { AuthMode.Web, AuthMode.DeviceCode };

            // Act + Assert
            subject.Combine().PreventInteractionIfNeeded(this.envMock.Object, this.logger).Should().Be(AuthMode.None);
            this.logTarget.Logs.Should().ContainInOrder("Interactive authentication is disabled.");
        }
#endif
    }
}
