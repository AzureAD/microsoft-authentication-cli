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

    public class BrokerTest
    {
        private const string MsalServiceExceptionErrorCode = "1";
        private const string MsalServiceExceptionMessage = "MSAL Service Exception: Something bad has happened!";
        private const string TestUser = "user@microsoft.com";

        // These Guids were randomly generated and do not refer to a real resource or client
        // as we don't need either for our testing.
        private static readonly Guid ResourceId = new Guid("6e979987-a7c8-4604-9b37-e51f06f08f1a");
        private static readonly Guid ClientId = new Guid("5af6def2-05ec-4cab-b9aa-323d75b5df40");
        private static readonly Guid TenantId = new Guid("8254f6f7-a09f-4752-8bd6-391adc3b912e");

        private string promptHint = "test prompt hint";

        private MemoryTarget logTarget;
        private ILogger logger;

        // MSAL Specific Mocks
        private Mock<IPCAWrapper> pcaWrapperMock;
        private Mock<IAccount> testAccount;
        private IEnumerable<string> scopes = new string[] { $"{ResourceId}/.default" };
        private TokenResult tokenResult;

        [SetUp]
        public void Setup()
        {
            (this.logger, this.logTarget) = MemoryLogger.Create();

            // MSAL Mocks
            this.testAccount = new Mock<IAccount>(MockBehavior.Strict);
            this.testAccount.Setup(a => a.Username).Returns(TestUser);

            this.pcaWrapperMock = new Mock<IPCAWrapper>(MockBehavior.Strict);

            // Mock successful token result
            this.tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken), Guid.NewGuid());
        }

        [TearDown]
        public void TearDown()
        {
            this.pcaWrapperMock.VerifyAll();
        }

        public AuthFlow.Broker Subject() => new AuthFlow.Broker(this.logger, ClientId, TenantId, this.scopes, pcaWrapper: this.pcaWrapperMock.Object, promptHint: this.promptHint);

        [Test]
        public async Task BrokerAuthFlow_HappyPath()
        {
            this.SilentAuthResult();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.TokenResult.IsSilent.Should().BeTrue();
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_GetTokenSilent_ReturnsNull()
        {
            this.MockAccount();
            this.SilentAuthReturnsNull();
            this.SetupInteractiveAuthWithPromptHint();
            this.InteractiveAuthResult();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_MsalUIException()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthResult();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.TokenResult.IsSilent.Should().BeFalse();
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_MsalUIException_InteractiveAuthResultReturnsNullWithoutClaims()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthResultReturnsNullWithoutClaims();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public void BrokerAuthFlow_General_Exceptions_Are_ReThrown()
        {
            var message = "Something somwhere has gone terribly wrong!";
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new Exception(message));

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            Func<Task> subject = async () => await broker.GetTokenAsync();

            // Assert
            subject.Should().ThrowExactlyAsync<Exception>().WithMessage(message);

            // This VerifyAll must come after the assert, since the assert is what execute the lambda
        }

        [Test]
        public async Task BrokerAuthFlow_GetTokenSilent_MsalServiceException()
        {
            this.SilentAuthServiceException();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert - this method should not throw for known types of excpeptions, instead return null, so
            // our caller can retry auth another way.
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalServiceException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_GetTokenSilent_OperationCanceledException_ThenSucceeds()
        {
            this.SilentAuthTimeout();
            this.MockAccount();
            this.SetupInteractiveAuthWithPromptHint();
            this.InteractiveAuthResult();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(AuthenticationTimeoutException));
            authFlowResult.Errors[0].Message.Should().Be("Get Token Silent timed out after 00:00:20");
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_GetTokenSilent_MsalClientException()
        {
            this.SilentAuthClientException();

            this.MockAccount();

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
        public async Task BrokerAuthFlow_GetTokenSilent_NullReferenceException()
        {
            this.SilentAuthNullReferenceException();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(NullReferenceException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_GetTokenInteractive_MsalUIException_For_Claims()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthExtraClaimsRequired();
            this.InteractiveAuthWithClaimsResult();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.TokenResult.IsSilent.Should().BeFalse();
            authFlowResult.Errors.Should().HaveCount(2);
            authFlowResult.Errors.Should().AllBeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_MsalUIException_InteractiveAuthResultReturnsNullWithClaims()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthExtraClaimsRequired();
            this.InteractiveAuthResultReturnsNullWithClaims();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(2);
            authFlowResult.Errors.Should().AllBeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_GetTokenInteractive_MsalServiceException_After_Using_Claims()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthExtraClaimsRequired();
            this.InteractiveAuthWithClaimsServiceException();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(3);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[1].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[2].Should().BeOfType(typeof(MsalServiceException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_GetTokenInteractive_MsalServiceException()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthServiceException();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(2);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[1].Should().BeOfType(typeof(MsalServiceException));
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_GetTokenInteractive_OperationCanceledException()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthTimeout();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(2);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[1].Should().BeOfType(typeof(AuthenticationTimeoutException));
            authFlowResult.Errors[1].Message.Should().Be("Interactive Auth timed out after 00:15:00");
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_GetTokenInteractive_OperationCanceledException_For_Claims()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthExtraClaimsRequired();
            this.InteractiveAuthWithClaimsTimeout();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var authFlowResult = await broker.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(3);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[1].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[2].Should().BeOfType(typeof(AuthenticationTimeoutException));
            authFlowResult.Errors[2].Message.Should().Be("Interactive Auth (with extra claims) timed out after 00:15:00");
            authFlowResult.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task BrokerAuthFlow_GetTokenInteractiveAsync_WithPromptHint()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthResult();

            this.MockAccount();

            // Act
            AuthFlow.Broker broker = this.Subject();
            var result = await broker.GetTokenAsync();

            // Verify
            this.pcaWrapperMock.Verify((pca) => pca.WithPromptHint(this.promptHint), Times.Once());
        }

        private void SilentAuthResult()
        {
            this.pcaWrapperMock
               .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
               .ReturnsAsync(this.tokenResult);
        }

        private void SilentAuthReturnsNull()
        {
            this.pcaWrapperMock
               .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
               .ReturnsAsync((TokenResult)null);
        }

        private void SilentAuthUIRequired()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "UI is required"));
            this.SetupInteractiveAuthWithPromptHint();
        }

        private void SilentAuthServiceException()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalServiceExceptionErrorCode, MsalServiceExceptionMessage));
        }

        private void SilentAuthTimeout()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private void SilentAuthClientException()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalClientException("1", "Could not find a WAM account for the silent request."));
        }

        private void SilentAuthNullReferenceException()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new NullReferenceException("There was a null reference excpetion. This should absolutly never happen and if it does it is a bug."));
        }

        private void InteractiveAuthResult()
        {
            this.pcaWrapperMock
               .Setup((pca) => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
               .ReturnsAsync(this.tokenResult);
        }

        private void InteractiveAuthResultReturnsNullWithoutClaims()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TokenResult)null);
        }

        private void InteractiveAuthWithClaimsResult()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.tokenResult);
        }

        private void InteractiveAuthResultReturnsNullWithClaims()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TokenResult)null);
        }

        private void InteractiveAuthTimeout()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private void InteractiveAuthExtraClaimsRequired()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "Extra Claims are required."));
        }

        private void InteractiveAuthServiceException()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalServiceExceptionErrorCode, MsalServiceExceptionMessage));
        }

        private void InteractiveAuthWithClaimsServiceException()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalServiceExceptionErrorCode, MsalServiceExceptionMessage));
        }

        private void InteractiveAuthWithClaimsTimeout()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private void MockAccount()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync(this.testAccount.Object);
        }

        private void SetupInteractiveAuthWithPromptHint()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.WithPromptHint(It.IsAny<string>()))
                .Returns((string s) => this.pcaWrapperMock.Object);
        }
    }
}
