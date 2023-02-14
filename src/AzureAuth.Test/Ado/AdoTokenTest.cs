// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Test
{
    using FluentAssertions;

    using Microsoft.Authentication.AzureAuth.Ado;
    using Microsoft.Office.Lasso.Interfaces;

    using Moq;

    using NUnit.Framework;

    public class AdoTokenTest
    {
        private const string NotARealPat = "<imagine PAT here>";
        private const string SystemAT = "SYSTEM_ACCESSTOKEN";
        private const string AzureAuthADOPAT = "AZUREAUTH_ADO_PAT";
        private Mock<IEnv> mockEnv;

        [SetUp]
        public void SetUp()
        {
            this.mockEnv = new Mock<IEnv>(MockBehavior.Strict);
        }

        [TearDown]
        public void TearDown()
        {
            this.mockEnv.VerifyAll();
        }

        [Test]
        public void No_PAT()
        {
            this.mockEnv.Setup(e => e.Get(AzureAuthADOPAT)).Returns<string>(default);
            this.mockEnv.Setup(e => e.Get(SystemAT)).Returns<string>(default);

            AdoToken.PatResult expected = new()
            {
                Exists = false,
                EnvVarSource = null,
                Value = null,
            };

            AdoToken.PatFromEnv(this.mockEnv.Object).Should().Be(expected);
        }

        [Test]
        public void Empty_PAT()
        {
            this.mockEnv.Setup(e => e.Get(AzureAuthADOPAT)).Returns(string.Empty);
            this.mockEnv.Setup(e => e.Get(SystemAT)).Returns(string.Empty);

            AdoToken.PatResult expected = new()
            {
                Exists = false,
                EnvVarSource = null,
                Value = null,
            };

            AdoToken.PatFromEnv(this.mockEnv.Object).Should().Be(expected);
        }

        [Test]
        public void AzureAuth_EnvVar_Is_Checked_First()
        {
            // Note: this.mockEnv is in strict mode and the TearDown method verifies the mock.
            // This means that by not mocking a response for any other env vars,
            // we are also asserting that no other env vars are checked.
            // Meaning AzureAuth's env var always takes priority.
            this.mockEnv.Setup(e => e.Get(AzureAuthADOPAT)).Returns(NotARealPat);

            AdoToken.PatResult expected = new()
            {
                Exists = true,
                EnvVarSource = AzureAuthADOPAT,
                Value = NotARealPat,
            };

            AdoToken.PatFromEnv(this.mockEnv.Object).Should().Be(expected);
        }

        [Test]
        public void FallBack_To_SystemAccessToken()
        {
            // Arrange
            this.mockEnv.Setup(e => e.Get(AzureAuthADOPAT)).Returns<string>(default);
            this.mockEnv.Setup(e => e.Get(SystemAT)).Returns(NotARealPat);

            AdoToken.PatResult expected = new()
            {
                Exists = true,
                EnvVarSource = SystemAT,
                Value = NotARealPat,
            };

            // Act + Assert.
            AdoToken.PatFromEnv(this.mockEnv.Object).Should().Be(expected);
        }
    }
}
