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
    using Microsoft.Office.Lasso.Telemetry;
    using Moq;
    using NLog.Targets;
    using NUnit.Framework;

    public class IEnvExtensionsTest
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

        [TestCase("1", true)]
        [TestCase("non-empty-string", false)]
        [TestCase("true", false)]
        [TestCase("", false)]
        public void InteractiveAuth_IsDisabledOnCorextEnvVar(string corextNonInteractive, bool expected)
        {
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);
            this.envMock.Setup(e => e.Get("Corext_NonInteractive")).Returns(corextNonInteractive);

            IEnvExtensions.InteractiveAuthDisabled(this.envMock.Object).Should().Be(expected);
        }

        [TestCase("1", true)]
        [TestCase("non-empty-string", true)]
        [TestCase("true", true)]
        [TestCase("", false)]
        public void InteractiveAuth_IsDisabledOnNoUserEnvVar(string noUser, bool expected)
        {
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);
            this.envMock.Setup(e => e.Get("AZUREAUTH_NO_USER")).Returns(noUser);
            IEnvExtensions.InteractiveAuthDisabled(this.envMock.Object).Should().Be(expected);
        }

        [Test]
        public void InteractiveAuth_IsEnabledIfEnvVarsAreNotSet()
        {
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);
            IEnvExtensions.InteractiveAuthDisabled(this.envMock.Object).Should().BeFalse();
        }

        [Test]
        public void ReadAuthModeFromEnvOrSetDefault_ReturnsDefault_WhenEnvVarIsEmpty()
        {
            // Arrange
            envMock.Setup(e => e.Get(It.IsAny<string>())).Returns(string.Empty);

            // Act
            var result = IEnvExtensions.ReadAuthModeFromEnvOrSetDefault(envMock.Object);

            // Assert
            result.Should().BeEquivalentTo(new[] { AuthMode.Default });
        }

        [Test]
        public void ReadAuthModeFromEnvOrSetDefault_ReturnsParsedAuthModes_WhenEnvVarIsValid()
        {
            // Arrange
            envMock.Setup(e => e.Get(It.IsAny<string>())).Returns("Web,DeviceCode");

            // Act
            var result = IEnvExtensions.ReadAuthModeFromEnvOrSetDefault(envMock.Object);

            // Assert
            result.Should().BeEquivalentTo(new[] { AuthMode.Web, AuthMode.DeviceCode });
        }

        [Test]
        public void ReadAuthModeFromEnvOrSetDefault_ReturnsEmpty_WhenEnvVarIsInvalid()
        {
            // Arrange
            envMock.Setup(e => e.Get(It.IsAny<string>())).Returns("InvalidMode");

            // Act
            var result = IEnvExtensions.ReadAuthModeFromEnvOrSetDefault(envMock.Object);

            // Assert
            result.Should().BeEmpty();
        }
    }
}
