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

#if PlatformWindows
        [Test]
        public void CombinedAuthMode_Allowed()
        {
            // Arrange
            this.envMock.Setup(e => e.Get(EnvVars.NoUser)).Returns(string.Empty);
            this.envMock.Setup(e => e.Get("Corext_NonInteractive")).Returns(string.Empty);

            var subject = new[] { AuthMode.IWA, AuthMode.Web, AuthMode.Broker };

            // Act + Assert
            subject.Combine().PreventInteractionIfNeeded(this.envMock.Object).Should().Be(AuthMode.Default);
        }

        [TestCase("AZUREAUTH_NO_USER")]
        [TestCase("Corext_NonInteractive")]
        public void Filterinteraction_Interactive_Auth_Disabled(string envVar)
        {
            // Arrange
            this.envMock.Setup(e => e.Get(envVar)).Returns("1");
            var subject = new[] { AuthMode.IWA, AuthMode.Web, AuthMode.Broker, AuthMode.DeviceCode };

            // Act + Assert
            subject.Combine().PreventInteractionIfNeeded(this.envMock.Object).Should().Be(AuthMode.IWA);
        }
#else
        public void CombinedAuthMode_Allowed()
        {
            // Arrange
            this.envMock.Setup(e => e.Get(EnvVars.NoUser)).Returns(string.Empty);
            this.envMock.Setup(e => e.Get("Corext_NonInteractive")).Returns(string.Empty);

            var subject = new[] { AuthMode.Web, AuthMode.DeviceCode };

            // Act + Assert
            subject.Combine().PreventInteractionIfNeeded(this.envMock.Object).Should().Be(AuthMode.Default);
        }

        [TestCase("AZUREAUTH_NO_USER")]
        [TestCase("Corext_NonInteractive")]
        public void Filterinteraction_Interactive_Auth_Disabled(string envVar)
        {
            // Arrange
            this.envMock.Setup(e => e.Get(envVar)).Returns("1");
            var subject = new[] { AuthMode.Web, AuthMode.DeviceCode };

            // Act + Assert
            subject.Combined().PreventInteractionIfNeeded(this.envMock.Object).Should().Be(AuthMode.None);
        }
#endif
    }
}
