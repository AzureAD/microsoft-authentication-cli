// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test
{
    using System.Collections.Generic;

    using FluentAssertions;

    using Microsoft.Authentication.AzureAuth;
    using Microsoft.Authentication.AzureAuth.Commands;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Office.Lasso.Interfaces;

    using Moq;

    using NUnit.Framework;

    using ModeExtensions = Microsoft.Authentication.AzureAuth.AuthModeExtensions;

    public class AuthModeExtensionsTest
    {
        private Mock<IEnv> envMock;

        [SetUp]
        public void SetUp()
        {
            this.envMock = new Mock<IEnv>();
        }

        [TestCase("1", true)]
        [TestCase("non-empty-string", false)]
        [TestCase("true", false)]
        [TestCase("", false)]
        public void InteractiveAuth_IsDisabledOnCorextEnvVar(string corextNonInteractive, bool expected)
        {
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);
            this.envMock.Setup(e => e.Get("Corext_NonInteractive")).Returns(corextNonInteractive);

            ModeExtensions.InteractiveAuthDisabled(this.envMock.Object).Should().Be(expected);
        }

        [TestCase("1", true)]
        [TestCase("non-empty-string", true)]
        [TestCase("true", true)]
        [TestCase("", false)]
        public void InteractiveAuth_IsDisabledOnNoUserEnvVar(string noUser, bool expected)
        {
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);
            this.envMock.Setup(e => e.Get("AZUREAUTH_NO_USER")).Returns(noUser);
            ModeExtensions.InteractiveAuthDisabled(this.envMock.Object).Should().Be(expected);
        }

        [Test]
        public void InteractiveAuth_IsEnabledIfEnvVarsAreNotSet()
        {
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);
            ModeExtensions.InteractiveAuthDisabled(this.envMock.Object).Should().BeFalse();
        }

#if PlatformWindows
        [Test]
        public void FilterInteraction_Allowed()
        {
            // Arrange
            this.envMock.Setup(e => e.Get(EnvVars.NoUser)).Returns(string.Empty);
            this.envMock.Setup(e => e.Get("Corext_NonInteractive")).Returns(string.Empty);

            var subject = new[] { AuthMode.IWA, AuthMode.Web, AuthMode.Broker };

            // Act + Assert
            subject.FilterInteraction(this.envMock.Object).Should().Be(AuthMode.Default);
        }

        [TestCase("AZUREAUTH_NO_USER")]
        [TestCase("Corext_NonInteractive")]
        public void Filterinteraction_Interactive_Auth_Disabled(string envVar)
        {
            // Arrange
            this.envMock.Setup(e => e.Get(envVar)).Returns("1");
            var subject = new[] { AuthMode.IWA, AuthMode.Web, AuthMode.Broker, AuthMode.DeviceCode };

            // Act + Assert
            subject.FilterInteraction(this.envMock.Object).Should().Be(AuthMode.IWA);
        }
#else

        public void FilterInteraction_Allowed()
        {
            // Arrange
            this.envMock.Setup(e => e.Get(EnvVars.NoUser)).Returns(string.Empty);
            this.envMock.Setup(e => e.Get("Corext_NonInteractive")).Returns(string.Empty);

            var subject = new[] { AuthMode.Web, AuthMode.DeviceCode };

            // Act + Assert
            subject.FilterInteraction(this.envMock.Object).Should().Be(AuthMode.Default);
        }

        [TestCase("AZUREAUTH_NO_USER")]
        [TestCase("Corext_NonInteractive")]
        public void Filterinteraction_Interactive_Auth_Disabled(string envVar)
        {
            // Arrange
            this.envMock.Setup(e => e.Get(envVar)).Returns("1");
            var subject = new[] { AuthMode.Web, AuthMode.DeviceCode };

            // Act + Assert
            subject.FilterInteraction(this.envMock.Object).Should().Be((AuthMode)0);
        }
#endif
    }
}
