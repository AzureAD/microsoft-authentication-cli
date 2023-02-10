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
        private const string SystemAT = "SYSTEM_ACCESSTOKEN";
        private Mock<IEnv> mockEnv;

        [SetUp]
        public void SetUp()
        {
            this.mockEnv = new Mock<IEnv>(MockBehavior.Strict);
        }

        [Test]
        public void No_PAT()
        {
            this.mockEnv.Setup(e => e.Get(SystemAT)).Returns<string>(default);

            AdoToken.PatResult expected = new ()
            {
                Exists = false,
                EnvVarSource = null,
                Value = null,
            };

            AdoToken.PatFromEnv(this.mockEnv.Object).Should().Be(expected);
        }

        [Test]
        public void EmptyPAT()
        {
            this.mockEnv.Setup(e => e.Get(SystemAT)).Returns(string.Empty);

            AdoToken.PatResult expected = new ()
            {
                Exists = false,
                EnvVarSource = null,
                Value = null,
            };

            AdoToken.PatFromEnv(this.mockEnv.Object).Should().Be(expected);
        }

        [Test]
        public void Returns_PAT_From_EnvVar()
        {
            // Arrange
            string pat = "some long pat value";
            Mock<IEnv> mockEnv = new Mock<IEnv>(MockBehavior.Strict);
            mockEnv.Setup(e => e.Get(SystemAT)).Returns(pat);

            AdoToken.PatResult expected = new ()
            {
                Exists = true,
                EnvVarSource = SystemAT,
                Value = pat,
            };

            // Act + Assert.
            AdoToken.PatFromEnv(mockEnv.Object).Should().Be(expected);
        }
    }
}
