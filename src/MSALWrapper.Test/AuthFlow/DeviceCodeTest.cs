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
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;

    using Moq;

    using NLog.Targets;

    using NUnit.Framework;

    internal class DeviceCodeTest
    {
        private const string TestUser = "user@microsoft.com";
        private const string PromptHint = "test prompt hint";

        // These Guids were randomly generated and do not refer to a real resource or client
        // as we don't need either for our testing.
        private static readonly Guid ResourceId = new Guid("6e979987-a7c8-4604-9b37-e51f06f08f1a");
        private static readonly Guid ClientId = new Guid("5af6def2-05ec-4cab-b9aa-323d75b5df40");
        private static readonly Guid TenantId = new Guid("8254f6f7-a09f-4752-8bd6-391adc3b912e");

        private ILogger logger;

        // MSAL Specific Mocks
        private Mock<IPCAWrapper> pcaWrapperMock;
        private Mock<IAccount> testAccount;
        private IEnumerable<string> scopes = new string[] { $"{ResourceId}/.default" };
        private TokenResult tokenResult;

        [SetUp]
        public void Setup()
        {
            (this.logger, _) = MemoryLogger.Create();

            // MSAL Mocks
            this.testAccount = new Mock<IAccount>(MockBehavior.Strict);
            this.pcaWrapperMock = new Mock<IPCAWrapper>(MockBehavior.Strict);

            // Mock successful token result
            this.tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken), Guid.NewGuid());
        }

        [TearDown]
        public void TearDown()
        {
            this.pcaWrapperMock.VerifyAll();
            this.testAccount.VerifyAll();
        }

        public AuthFlow.DeviceCode Subject() => new AuthFlow.DeviceCode(this.logger, ClientId, TenantId, this.scopes, pcaWrapper: this.pcaWrapperMock.Object, promptHint: PromptHint);

        [Test]
        public async Task DeviceCodeAuthFlow_CachedToken()
        {
            this.MockAccount();
            this.SilentAuthResult();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.TokenResult.IsSilent.Should().BeTrue();
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("devicecode");
        }

        [Test]
        public async Task DeviceCodeAuthFlow_NoAccount()
        {
            this.MockAccountNull();
            this.DeviceCodeAuthResult();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.TokenResult.IsSilent.Should().BeFalse();
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("devicecode");
        }

        [Test]
        public async Task DeviceCodeAuthFlow_GetTokenSilent_ReturnsNull_ThenSuccess()
        {
            this.MockAccount();
            this.SilentAuthReturnsNull();
            this.DeviceCodeAuthResult();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("devicecode");
        }

        [Test]
        public async Task DeviceCodeAuthFlow_GetTokenSilent_OperationCanceledException_ThenSuccess()
        {
            this.MockAccount();
            this.SilentAuthTimeout();
            this.DeviceCodeAuthResult();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(AuthenticationTimeoutException));
            authFlowResult.Errors[0].Message.Should().Be("Get Token Silent timed out after 00:00:15");
            authFlowResult.AuthFlowName.Should().Be("devicecode");
        }

        [Test]
        public async Task DeviceCodeAuthFlow_SilentMsalUIException_ThenSuccess()
        {
            this.MockAccount();
            this.SilentAuthUIRequired();
            this.DeviceCodeAuthResult();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.TokenResult.IsSilent.Should().BeFalse();
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("devicecode");
        }

        [Test]
        public async Task DeviceCodeAuthFlow_MsalUIException_Returns_Null()
        {
            this.MockAccount();
            this.SilentAuthUIRequired();
            this.DeviceCodeAuthReturnsNull();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("devicecode");
        }

        [Test]
        public async Task DeviceCodeAuthFlow_MsalException()
        {
            this.MockAccount();
            this.SilentAuthUIRequired();
            this.DeviceCodeAuthMsalException();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(2);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[1].Should().BeOfType(typeof(MsalException));
            authFlowResult.AuthFlowName.Should().Be("devicecode");
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
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, It.IsAny<IAccount>(), It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "UI is required"));
        }

        private void SilentAuthTimeout()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private void DeviceCodeAuthResult()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenDeviceCodeAsync(this.scopes, It.IsAny<Func<DeviceCodeResult, Task>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.tokenResult);
        }

        private void DeviceCodeAuthReturnsNull()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenDeviceCodeAsync(this.scopes, It.IsAny<Func<DeviceCodeResult, Task>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TokenResult)null);
        }

        private void DeviceCodeAuthMsalException()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenDeviceCodeAsync(this.scopes, It.IsAny<Func<DeviceCodeResult, Task>>(), It.IsAny<CancellationToken>()))
                .Throws(new MsalException("1", "Msal Exception."));
        }

        private void MockAccount()
        {
            this.testAccount.Setup(a => a.Username).Returns(TestUser);
            this.pcaWrapperMock
                .Setup(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync(this.testAccount.Object);
        }

        private void MockAccountNull()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync((IAccount)null);
        }
    }
}
