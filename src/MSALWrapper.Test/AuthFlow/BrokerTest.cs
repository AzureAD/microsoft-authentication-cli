// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;

    using Moq;

    using NLog.Targets;

    using NUnit.Framework;

    internal class BrokerTest : AuthFlowTestBase
    {
        public AuthFlow.Broker Subject() => new AuthFlow.Broker(this.logger, this.authParameters, pcaWrapper: this.mockPca.Object, promptHint: PromptHint);

        [Test]
        public async Task GetTokenSilent_Success()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentSuccess();

            // Act
            AuthFlowResult authFlowResult = await this.Subject().GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.testToken);
            authFlowResult.TokenResult.IsSilent.Should().BeTrue();
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task GetTokenSilent_ReturnsNull_GetTokeninteractive_Success()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentReturnsNull();
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveSuccess(withAccount: true);

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.testToken);
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task GetTokenSilent_Throws_GetTokenInteractive_Success()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentMsalUiRequiredException();
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveSuccess(withAccount: true);

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.testToken);
            authFlowResult.TokenResult.IsSilent.Should().BeFalse();
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task NoAccount_GetTokenInteractive_RetriesWithClaims_Success()
        {
            this.SetupCachedAccount(false);

            // No account username mock because the account will be the built in OS account
            // which has a null username.

            // special mock for OS account
            this.mockPca
                .Setup(pca => pca.GetTokenSilentAsync(Scopes, PublicClientApplication.OperatingSystemAccount, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TokenResult)null);

            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveMsalUiRequiredException(PublicClientApplication.OperatingSystemAccount);
            this.SetupGetTokenInteractiveWithClaimsSuccess();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.testToken);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task General_Exceptions_Are_ReThrown()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentMsalUiRequiredException();
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveGeneralException(withAccount: true);

            // Act
            AuthFlow.Broker broker = this.Subject();
            Func<Task> subject = async () => await broker.GetTokenAsync();

            // Assert
            await subject.Should().ThrowExactlyAsync<Exception>().WithMessage(GeneralExceptionMessage);
        }

        [Test]
        public async Task GetTokenSilent_MsalServiceException()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentMsalServiceException();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalServiceException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task GetTokenSilent_Timeout_GetTokenInteractive_Success()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentTimeout();
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveSuccess(withAccount: true);

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.testToken);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(AuthenticationTimeoutException));
            authFlowResult.Errors[0].Message.Should().Be("Get Token Silent timed out after 00:00:30");
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task GetTokenSilent_MsalClientException()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentMsalClientException();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalClientException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task GetTokenSilent_NullReferenceException()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentSilentNullReferenceException();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().BeNull();
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(NullReferenceException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task GetTokenInteractiveWithClaims_MsalException()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentMsalUiRequiredException();
            this.SetupGetTokenInteractiveMsalUiRequiredException(this.mockAccount.Object);
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveWithClaimsThrowsServiceException();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().BeNull();
            authFlowResult.Errors.Should().HaveCount(3);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[1].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[2].Should().BeOfType(typeof(MsalServiceException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task GetTokenInteractiveWithClaims_ReturnsNull()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentMsalUiRequiredException();
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveMsalUiRequiredException(this.mockAccount.Object);
            this.SetupGetTokenInteractiveWithClaimsReturnsNull();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().BeNull();
            authFlowResult.Errors.Should().HaveCount(2);
            authFlowResult.Errors.Should().AllBeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task GetTokenInteractive_Timeout()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentMsalUiRequiredException();
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveTimeout(withAccount: true);

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(2);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[1].Should().BeOfType(typeof(AuthenticationTimeoutException));
            authFlowResult.Errors[1].Message.Should().Be("broker interactive auth timed out after 00:15:00");
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task GetTokenInteractiveWithClaims_Timeout()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentMsalUiRequiredException();
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveMsalUiRequiredException(this.mockAccount.Object);
            this.SetupGetTokenInteractiveWithClaimsTimeout();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(3);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[1].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[2].Should().BeOfType(typeof(AuthenticationTimeoutException));
            authFlowResult.Errors[2].Message.Should().Be("broker interactive auth (with extra claims) timed out after 00:15:00");
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task GetTokenInteractiveAsync_Calls_WithPromptHint()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentMsalUiRequiredException();
            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveSuccess(withAccount: true);

            // Act
            AuthFlow.Broker broker = this.Subject();
            await broker.GetTokenAsync();

            // Assert
            // yes the inherited TearDown does this, but we do it here since it's the explicit assertion for this test
            this.mockPca.VerifyAll();
        }

        private void SetupGetTokenSilentMsalServiceException()
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenSilentAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MsalServiceException(MsalExceptionErrorCode, MsalExceptionMessage));
        }

        private void SetupGetTokenSilentMsalClientException()
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenSilentAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalClientException("1", "Could not find a WAM account for the silent request."));
        }

        private void SetupGetTokenSilentSilentNullReferenceException()
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenSilentAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new NullReferenceException("There was a null reference excpetion. This should absolutly never happen and if it does it is a bug."));
        }
    }
}
