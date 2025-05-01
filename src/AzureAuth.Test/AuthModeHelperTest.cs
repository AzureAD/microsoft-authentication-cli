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

namespace AzureAuth.Test
{
    public class AuthModeHelperTest
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

        [Test]
        public void ReadAuthModeFromEnvOrSetDefault_ReturnsDefault_WhenEnvVarIsEmpty()
        {
            // Arrange
            envMock.Setup(e => e.Get(It.IsAny<string>())).Returns(string.Empty);

            // Act
            var result = AuthModeHelper.ReadAuthModeFromEnvOrSetDefault(envMock.Object, new EventData(), logger);

            // Assert
            result.Should().BeEquivalentTo(new[] { AuthMode.Default });
        }

        [Test]
        public void ReadAuthModeFromEnvOrSetDefault_ReturnsParsedAuthModes_WhenEnvVarIsValid()
        {
            // Arrange
            envMock.Setup(e => e.Get(It.IsAny<string>())).Returns("Web,Broker");

            // Act
            var result = AuthModeHelper.ReadAuthModeFromEnvOrSetDefault(envMock.Object, new EventData(), logger);

            // Assert
            result.Should().BeEquivalentTo(new[] { AuthMode.Web, AuthMode.Broker });
        }

        [Test]
        public void ReadAuthModeFromEnvOrSetDefault_LogsErrorAndReturnsEmpty_WhenEnvVarIsInvalid()
        {
            // Arrange
            envMock.Setup(e => e.Get(It.IsAny<string>())).Returns("InvalidMode");

            // Act
            var result = AuthModeHelper.ReadAuthModeFromEnvOrSetDefault(envMock.Object, new EventData(), logger);

            // Assert
            result.Should().BeEmpty();
            this.logTarget.Logs.Should().ContainMatch("Invalid value specified for environment variable*");
        }

        [Test]
        public void ReadAuthModeFromEnvOrSetDefault_AddsEventData_WhenEnvVarIsValid()
        {
            // Arrange
            var eventData = new EventData();
            envMock.Setup(e => e.Get(It.IsAny<string>())).Returns("Web");

            // Act
            var result = AuthModeHelper.ReadAuthModeFromEnvOrSetDefault(envMock.Object, eventData, logger);

            // Assert
            var env_var = $"env_{EnvVars.AuthMode}";
            eventData.Properties[env_var.ToLower()].Should().Be("Web");
        }
    }
}
