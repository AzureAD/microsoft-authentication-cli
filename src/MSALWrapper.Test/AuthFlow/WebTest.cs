// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Identity.Client;

    using Moq;

    using NUnit.Framework;

    internal class WebTest : AuthFlowTestBase
    {
        public AuthFlow.Web Subject() => new AuthFlow.Web(
            this.logger,
            this.authParameters,
            pcaWrapper: this.mockPca.Object,
            promptHint: PromptHint);

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetTokenInteractive_Success(bool withAccount)
        {
            this.SetupCachedAccount(withAccount);
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveSuccess(withAccount: withAccount);

            // Act
            AuthFlow.Web deviceCode = this.Subject();
            var subject = await deviceCode.GetTokenAsync();

            // Assert
            var expected = new AuthFlowResult(this.testToken, Array.Empty<Exception>(), "web");
            subject.Should().BeEquivalentTo(expected);
            subject.TokenResult.IsSilent.Should().BeFalse();
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetTokenInteractive_Null(bool withAccount)
        {
            this.SetupCachedAccount(withAccount);
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveReturnsNull(withAccount: withAccount);

            // Act
            AuthFlow.Web web = this.Subject();
            var authFlowResult = await web.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("web");
        }

        [Test]
        public async Task General_Exceptions_Are_Thrown()
        {
            this.SetupCachedAccount(true);
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveGeneralException();

            // Act
            AuthFlow.Web web = this.Subject();
            Func<Task> subject = async () => await web.GetTokenAsync();

            // Assert
            await subject.Should().ThrowExactlyAsync<Exception>().WithMessage(GeneralExceptionMessage);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetTokenInteractiveWithClaims_Success(bool withAccount)
        {
            this.SetupCachedAccount(withAccount);
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveMsalUiRequiredException(withAccount);
            this.SetupGetTokenInteractiveWithClaimsSuccess();

            // Act
            AuthFlow.Web web = this.Subject();
            var authFlowResult = await web.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.testToken);
            authFlowResult.TokenResult.IsSilent.Should().BeFalse();
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors.First().Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("web");
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetTokenInteractiveWithClaims_ReturnsNull(bool withAccount)
        {
            this.SetupCachedAccount(withAccount);
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveMsalUiRequiredException(withAccount);
            this.SetupGetTokenInteractiveWithClaimsReturnsNull();

            // Act
            AuthFlow.Web web = this.Subject();
            var authFlowResult = await web.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors.First().Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("web");
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetTokenInteractiveWithClaims_ThrowsMsalServiceException(bool withAccount)
        {
            this.SetupCachedAccount(withAccount);
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveMsalUiRequiredException(withAccount);
            this.SetupGetTokenInteractiveWithClaimsThrowsServiceException();

            // Act
            AuthFlow.Web web = this.Subject();
            var authFlowResult = await web.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(2);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[1].Should().BeOfType(typeof(MsalServiceException));
            authFlowResult.AuthFlowName.Should().Be("web");
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetTokenInteractive_ThrowsMsalServiceException(bool withAccount)
        {
            this.SetupCachedAccount(withAccount);
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveMsalServiceException(withAccount);

            // Act
            AuthFlow.Web web = this.Subject();
            var authFlowResult = await web.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalServiceException));
            authFlowResult.AuthFlowName.Should().Be("web");
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetTokenInteractive_Timeout(bool withAccount)
        {
            this.SetupCachedAccount(withAccount);
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveTimeout(withAccount);

            // Act
            AuthFlow.Web web = this.Subject();
            var authFlowResult = await web.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(AuthenticationTimeoutException));
            authFlowResult.Errors[0].Message.Should().Be("web interactive auth timed out after 00:15:00");
            authFlowResult.AuthFlowName.Should().Be("web");
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetTokenInteractiveWithClaims_Timeout(bool withAccount)
        {
            this.SetupCachedAccount(withAccount);
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveMsalUiRequiredException(withAccount);
            this.SetupGetTokenInteractiveWithClaimsTimeout();

            // Act
            AuthFlow.Web web = this.Subject();
            var authFlowResult = await web.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(2);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[1].Should().BeOfType(typeof(AuthenticationTimeoutException));
            authFlowResult.AuthFlowName.Should().Be("web");
        }
    }
}
