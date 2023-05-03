// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
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
            ClientId,
            TenantId,
            Scopes,
            pcaWrapper: this.mockPca.Object,
            promptHint: PromptHint);

        [Test]
        public async Task WebAuthFlow_NoAccount_Success()
        {
            this.SetupNoCachedAccount();
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveSuccess(withAccount: false);

            // Act
            AuthFlow.Web deviceCode = this.Subject();
            var subject = await deviceCode.GetTokenAsync();

            // Assert
            var expected = new AuthFlowResult(this.testToken, Array.Empty<Exception>(), "web");
            subject.Should().BeEquivalentTo(expected);
            subject.TokenResult.IsSilent.Should().BeFalse();
        }

        [Test]
        public async Task WebAuthFlow_WithAccount_Success()
        {
            this.SetupCachedAccount();
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveSuccess(withAccount: true);

            // Act
            AuthFlow.Web web = this.Subject();
            var subject = await web.GetTokenAsync();

            // Assert
            var expected = new AuthFlowResult(this.testToken, Array.Empty<Exception>(), "web");
            subject.Should().BeEquivalentTo(expected);
            subject.TokenResult.IsSilent.Should().BeFalse();
        }

        [Test]
        public async Task WebAuthFlow_NoAccount_Null()
        {
            this.SetupNoCachedAccount();
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveReturnsNull(withAccount: false);

            // Act
            AuthFlow.Web web = this.Subject();
            var authFlowResult = await web.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("web");
        }

        //[Test]
        //public void WebAuthFlow_General_Exceptions_Are_ReThrown()
        //{
        //    var message = "Something somwhere has gone terribly wrong!";
        //    this.MockAccount();
        //    this.mockPca
        //        .Setup((pca) => pca.GetTokenSilentAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
        //        .Throws(new Exception(message));

        //    // Act
        //    AuthFlow.Web web = this.Subject();
        //    Func<Task> subject = async () => await web.GetTokenAsync();

        //    // Assert
        //    subject.Should().ThrowExactlyAsync<Exception>().WithMessage(message);
        //}

        //[Test]
        //public async Task WebAuthFlow_GetTokenSilent_MsalServiceException()
        //{
        //    this.SilentAuthServiceException();

        //    this.MockAccount();

        //    // Act
        //    AuthFlow.Web web = this.Subject();
        //    var authFlowResult = await web.GetTokenAsync();

        //    // Assert - this method should not throw for known types of excpeptions, instead return null, so
        //    // our caller can retry auth another way.
        //    authFlowResult.TokenResult.Should().Be(null);
        //    authFlowResult.Errors.Should().HaveCount(1);
        //    authFlowResult.Errors[0].Should().BeOfType(typeof(MsalServiceException));
        //    authFlowResult.AuthFlowName.Should().Be("web");
        //}

        //[Test]
        //public async Task WebAuthFlow_GetTokenSilent_OperationCanceledException()
        //{
        //    this.MockAccount();
        //    this.SilentAuthTimeout();
        //    this.SetupWithPromptHint();
        //    this.SetupWebSuccess();

        //    // Act
        //    AuthFlow.Web web = this.Subject();
        //    var authFlowResult = await web.GetTokenAsync();

        //    // Assert
        //    authFlowResult.TokenResult.Should().Be(this.testToken);
        //    authFlowResult.Errors.Should().HaveCount(1);
        //    authFlowResult.Errors[0].Should().BeOfType(typeof(AuthenticationTimeoutException));
        //    authFlowResult.Errors[0].Message.Should().Be("Get Token Silent timed out after 00:00:30");
        //    authFlowResult.AuthFlowName.Should().Be("web");
        //}

        //[Test]
        //public async Task WebAuthFlow_GetTokenSilent_MsalClientException()
        //{
        //    this.MockAccount();
        //    this.SilentAuthClientException();

        //    // Act
        //    AuthFlow.Web web = this.Subject();
        //    var authFlowResult = await web.GetTokenAsync();

        //    // Assert
        //    authFlowResult.TokenResult.Should().Be(null);
        //    authFlowResult.Errors.Should().HaveCount(1);
        //    authFlowResult.Errors[0].Should().BeOfType(typeof(MsalClientException));
        //    authFlowResult.AuthFlowName.Should().Be("web");
        //}

        //[Test]
        //public async Task WebAuthFlow_GetTokenSilent_NullReferenceException()
        //{
        //    this.MockAccount();
        //    this.SilentAuthNullReferenceException();

        //    // Act
        //    AuthFlow.Web web = this.Subject();
        //    var authFlowResult = await web.GetTokenAsync();

        //    // Assert
        //    authFlowResult.TokenResult.Should().Be(null);
        //    authFlowResult.Errors.Should().HaveCount(1);
        //    authFlowResult.Errors[0].Should().BeOfType(typeof(NullReferenceException));
        //    authFlowResult.AuthFlowName.Should().Be("web");
        //}

        //[Test]
        //public async Task WebAuthFlow_GetTokenInteractive_MsalUIException_For_Claims()
        //{
        //    this.MockAccount();
        //    this.SilentAuthUIRequired();
        //    this.SetupWithPromptHint();
        //    this.InteractiveAuthExtraClaimsRequired();
        //    this.InteractiveAuthWithClaimsResult();

        //    // Act
        //    AuthFlow.Web web = this.Subject();
        //    var authFlowResult = await web.GetTokenAsync();

        //    // Assert
        //    authFlowResult.TokenResult.Should().Be(this.testToken);
        //    authFlowResult.TokenResult.IsSilent.Should().BeFalse();
        //    authFlowResult.Errors.Should().HaveCount(2);
        //    authFlowResult.Errors.Should().AllBeOfType(typeof(MsalUiRequiredException));
        //    authFlowResult.AuthFlowName.Should().Be("web");
        //}

        //[Test]
        //public async Task WebAuthFlow_MsalUIException_InteractiveAuthResultReturnsNullWithClaims()
        //{
        //    this.MockAccount();
        //    this.SilentAuthUIRequired();
        //    this.SetupWithPromptHint();
        //    this.InteractiveAuthExtraClaimsRequired();
        //    this.InteractiveAuthResultReturnsNullWithClaims();

        //    // Act
        //    AuthFlow.Web web = this.Subject();
        //    var authFlowResult = await web.GetTokenAsync();

        //    // Assert
        //    authFlowResult.TokenResult.Should().Be(null);
        //    authFlowResult.Errors.Should().HaveCount(2);
        //    authFlowResult.Errors.Should().AllBeOfType(typeof(MsalUiRequiredException));
        //    authFlowResult.AuthFlowName.Should().Be("web");
        //}

        //[Test]
        //public async Task WebAuthFlow_GetTokenInteractive_MsalServiceException_After_Using_Claims()
        //{
        //    this.MockAccount();
        //    this.SilentAuthUIRequired();
        //    this.SetupWithPromptHint();
        //    this.InteractiveAuthExtraClaimsRequired();
        //    this.InteractiveAuthWithClaimsServiceException();

        //    // Act
        //    AuthFlow.Web web = this.Subject();
        //    var authFlowResult = await web.GetTokenAsync();

        //    // Assert
        //    authFlowResult.TokenResult.Should().Be(null);
        //    authFlowResult.Errors.Should().HaveCount(3);
        //    authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
        //    authFlowResult.Errors[1].Should().BeOfType(typeof(MsalUiRequiredException));
        //    authFlowResult.Errors[2].Should().BeOfType(typeof(MsalServiceException));
        //    authFlowResult.AuthFlowName.Should().Be("web");
        //}

        //[Test]
        //public async Task WebAuthFlow_GetTokenInteractive_MsalServiceException()
        //{
        //    this.MockAccount();
        //    this.SilentAuthUIRequired();
        //    this.SetupWithPromptHint();
        //    this.InteractiveAuthServiceException();

        //    // Act
        //    AuthFlow.Web web = this.Subject();
        //    var authFlowResult = await web.GetTokenAsync();

        //    // Assert
        //    authFlowResult.TokenResult.Should().Be(null);
        //    authFlowResult.Errors.Should().HaveCount(2);
        //    authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
        //    authFlowResult.Errors[1].Should().BeOfType(typeof(MsalServiceException));
        //    authFlowResult.AuthFlowName.Should().Be("web");
        //}

        //[Test]
        //public async Task WebAuthFlow_GetTokenInteractive_OperationCanceledException()
        //{
        //    this.MockAccount();
        //    this.SilentAuthUIRequired();
        //    this.SetupWithPromptHint();
        //    this.InteractiveAuthTimeout();

        //    // Act
        //    AuthFlow.Web web = this.Subject();
        //    var authFlowResult = await web.GetTokenAsync();

        //    // Assert
        //    authFlowResult.TokenResult.Should().Be(null);
        //    authFlowResult.Errors.Should().HaveCount(2);
        //    authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
        //    authFlowResult.Errors[1].Should().BeOfType(typeof(AuthenticationTimeoutException));
        //    authFlowResult.Errors[1].Message.Should().Be("web interactive auth timed out after 00:15:00");
        //    authFlowResult.AuthFlowName.Should().Be("web");
        //}

        //[Test]
        //public async Task WebAuthFlow_GetTokenInteractive_OperationCanceledException_For_Claims()
        //{
        //    this.MockAccount();
        //    this.SilentAuthUIRequired();
        //    this.SetupWithPromptHint();
        //    this.InteractiveAuthExtraClaimsRequired();
        //    this.InteractiveAuthWithClaimsTimeout();

        //    // Act
        //    AuthFlow.Web web = this.Subject();
        //    var authFlowResult = await web.GetTokenAsync();

        //    // Assert
        //    authFlowResult.TokenResult.Should().Be(null);
        //    authFlowResult.Errors.Should().HaveCount(3);
        //    authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
        //    authFlowResult.Errors[1].Should().BeOfType(typeof(MsalUiRequiredException));
        //    authFlowResult.Errors[2].Should().BeOfType(typeof(AuthenticationTimeoutException));
        //    authFlowResult.Errors[2].Message.Should().Be("Interactive Auth (with extra claims) timed out after 00:15:00");
        //    authFlowResult.AuthFlowName.Should().Be("web");
        //}

        private void SetupInteractiveAuthWithClaimsSuccess()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, Claims, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.testToken);
        }

        private void SetupInteractiveAuthWithClaimsReturnsNull()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, Claims, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TokenResult)null);
        }

        private void InteractiveAuthTimeout()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private void InteractiveAuthExtraClaimsRequired()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "Extra Claims are required."));
        }

        private void InteractiveAuthServiceException()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalExceptionErrorCode, MsalExceptionMessage));
        }

        private void InteractiveAuthWithClaimsServiceException()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalExceptionErrorCode, MsalExceptionMessage));
        }

        private void InteractiveAuthWithClaimsTimeout()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private void MockAccount()
        {
            this.mockPca
                .Setup(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync(this.mockAccount.Object);
        }
    }
}
