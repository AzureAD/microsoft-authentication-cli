// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    using Moq;

    using NUnit.Framework;

    internal class CachedAuthTest
    {
        private ILogger logger;

        [SetUp]
        public void Setup()
        {
            (this.logger, _) = MemoryLogger.Create();
        }

        [Test]
        public void Null_Account_Returns_Null_Without_Errors()
        {
            IList<Exception> errors = new List<Exception>();
            var subject = CachedAuth.GetTokenAsync(
                this.logger,
                new[] { "scope" },
                null,
                new Mock<IPCAWrapper>(MockBehavior.Strict).Object,
                errors).Result;

            subject.Should().BeNull();
            errors.Should().BeEmpty();
        }

        [Test]
        public void MsalUiRequiredException_Results_In_Null_With_Error()
        {
            // Setup
            Mock<IPCAWrapper> pcaWrapper = new Mock<IPCAWrapper>(MockBehavior.Strict);
            Mock<IAccount> account = new Mock<IAccount>(MockBehavior.Strict);
            IList<Exception> errors = new List<Exception>();
            var scopes = new[] { "scope" };

            account.Setup(account => account.Username).Returns("user@contoso.com");
            pcaWrapper.Setup(pca => pca.GetTokenSilentAsync(scopes, account.Object, It.IsAny<System.Threading.CancellationToken>()))
                .ThrowsAsync(new MsalUiRequiredException("1", "2fa is required", new Exception("inner 2fa exception"), UiRequiredExceptionClassification.AcquireTokenSilentFailed));

            // Act
            var subject = CachedAuth.GetTokenAsync(
                this.logger,
                scopes,
                account.Object,
                pcaWrapper.Object,
                errors).Result;

            // Assert
            subject.Should().BeNull();
            errors.Should().HaveCount(1);
            errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
        }

        [Test]
        public void SuccessfulCachedAuth_IsSilent()
        {
            // Setup
            Mock<IPCAWrapper> pcaWrapper = new Mock<IPCAWrapper>(MockBehavior.Strict);
            Mock<IAccount> account = new Mock<IAccount>(MockBehavior.Strict);
            IList<Exception> errors = new List<Exception>();
            var scopes = new[] { "scope" };
            var tokenResult = new TokenResult(new IdentityModel.JsonWebTokens.JsonWebToken(TokenResultTest.FakeToken), Guid.NewGuid());

            account.Setup(account => account.Username).Returns("user@contoso.com");
            pcaWrapper.Setup(pca => pca.GetTokenSilentAsync(scopes, account.Object, It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(tokenResult);

            // Act
            var subject = CachedAuth.GetTokenAsync(
                this.logger,
                scopes,
                account.Object,
                pcaWrapper.Object,
                errors).Result;

            // Assert
            subject.Should().Be(tokenResult);
            subject.IsSilent.Should().BeTrue();
        }
    }
}
