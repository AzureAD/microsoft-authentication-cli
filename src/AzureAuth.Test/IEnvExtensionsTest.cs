// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test
{
    using FluentAssertions;

    using Microsoft.Authentication.AzureAuth;
    using Microsoft.Office.Lasso.Interfaces;

    using Moq;

    using NUnit.Framework;

    public class IEnvExtensionsTest
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
    }
}
