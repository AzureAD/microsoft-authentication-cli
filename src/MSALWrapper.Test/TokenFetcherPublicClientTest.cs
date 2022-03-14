// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;
    using Moq;
    using NLog.Extensions.Logging;
    using NLog.Targets;
    using NUnit.Framework;

    /// <summary>
    /// The token fetcher public client test.
    /// </summary>
    public class TokenFetcherPublicClientTest
    {
        private const string MsalServiceExceptionErrorCode = "1";
        private const string MsalServiceExceptionMessage = "MSAL Service Exception: Something bad has happened!";
        private const string TestUser = "user@microsoft.com";

        // These Guids were randomly generated and do not refer to a real resource or client
        // as we don't need either for our testing.
        private static readonly Guid ResourceId = new Guid("6e979987-a7c8-4604-9b37-e51f06f08f1a");
        private static readonly Guid ClientId = new Guid("5af6def2-05ec-4cab-b9aa-323d75b5df40");
        private static readonly Guid TenantId = new Guid("8254f6f7-a09f-4752-8bd6-391adc3b912e");

        private readonly IAccount userLive = new MockAccount("first@live.com");
        private readonly IAccount userMicrosoft1 = new MockAccount("second@microsoft.com");
        private readonly IAccount userMicrosoft2 = new MockAccount("third@microsoft.com");
        private readonly IAccount userUppercase = new MockAccount("fourth@MICROSOFT.com");
        private readonly string authority = $"https://login.microsoftonline.com/{TenantId.ToString()}";

        private IServiceProvider serviceProvider;
        private MemoryTarget logTarget;

        // MSAL Specific Mocks
        private Mock<IPCAWrapper> pcaMock;
        private Mock<IAccount> testAccount;
        private IEnumerable<string> scopes = new string[] { $"{ResourceId}/.default" };
        private TokenResult tokenResult;

        /// <summary>
        /// The setup.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Setup in memory logging target with NLog - allows making assertions against what has been logged.
            var loggingConfig = new NLog.Config.LoggingConfiguration();
            this.logTarget = new MemoryTarget("memory_target");
            loggingConfig.AddTarget(this.logTarget);
            loggingConfig.AddRuleForAllLevels(this.logTarget);

            // Setup Dependency Injection container to provide logger and out class under test (the "subject")
            this.serviceProvider = new ServiceCollection()
             .AddLogging(loggingBuilder =>
             {
                 // configure Logging with NLog
                 loggingBuilder.ClearProviders();
                 loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                 loggingBuilder.AddNLog(loggingConfig);
             })
             .AddTransient<TokenFetcherPublicClient>((provider) =>
             {
                 var logger = provider.GetService<ILogger<TokenFetcherPublicClient>>();
                 return new TokenFetcherPublicClient(logger, ResourceId, ClientId, TenantId);
             })
             .BuildServiceProvider();

            // MSAL Mocks
            this.testAccount = new Mock<IAccount>(MockBehavior.Strict);
            this.testAccount.Setup(a => a.Username).Returns(TestUser);

            this.pcaMock = new Mock<IPCAWrapper>(MockBehavior.Strict);

            // Mock successful token result
            this.tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken));
        }

        /// <summary>
        /// The get token normal flow async_ happy path.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task GetTokenNormalFlowAsync_HappyPath()
        {
            this.SilentAuthResult();

            // Act
            var tokenFetcher = this.Subject();
            var result = await tokenFetcher.GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert
            this.pcaMock.VerifyAll();
            result.Should().Be(this.tokenResult);
            result.AuthType.Should().Be(AuthType.Silent);
            tokenFetcher.ErrorsList.Should().BeEmpty();
        }

        /// <summary>
        /// The get token normal flow async_ msal ui exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task GetTokenNormalFlowAsync_MsalUIException()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthResult();

            // Act
            var tokenFetcher = this.Subject();
            var result = await tokenFetcher.GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert
            this.pcaMock.VerifyAll();
            result.Should().Be(this.tokenResult);
            result.AuthType.Should().Be(AuthType.Interactive);
            tokenFetcher.ErrorsList.Should().HaveCount(1);
            tokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalUiRequiredException));
        }

        /// <summary>
        /// The get token normal flow async_ general_ exceptions_ are_ re thrown.
        /// </summary>
        [Test]
        public void GetTokenNormalFlowAsync_General_Exceptions_Are_ReThrown()
        {
            var message = "Something somwhere has gone terrible wrong!";
            this.pcaMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new Exception(message));

            // Act
            Func<Task> subject = async () => await this.Subject().GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert
            subject.Should().ThrowExactlyAsync<Exception>().WithMessage(message);

            // This VerifyAll must come after the assert, since the assert is what execute the lambda
            this.pcaMock.VerifyAll();
        }

        /// <summary>
        /// The get token normal flow async_ get token silent_ throws_ msal service exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task GetTokenNormalFlowAsync_GetTokenSilent_Throws_MsalServiceException()
        {
            this.SilentAuthServiceException();

            // Act
            var tokenFetcher = this.Subject();
            var result = await tokenFetcher.GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert - this method should not throw for known types of excpeptions, instead return null, so
            // our caller can retry auth another way.
            this.pcaMock.VerifyAll();
            result.Should().Be(null);
            tokenFetcher.ErrorsList.Should().HaveCount(1);
            tokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalServiceException));
        }

        /// <summary>
        /// The get token normal flow async_ get token silent_ throws_ operation canceled exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task GetTokenNormalFlowAsync_GetTokenSilent_Throws_OperationCanceledException()
        {
            this.SilentAuthTimeout();

            // Act
            var tokenFetcher = this.Subject();
            var result = await tokenFetcher.GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert
            this.pcaMock.VerifyAll();
            result.Should().Be(null);
            tokenFetcher.ErrorsList.Should().HaveCount(1);
            tokenFetcher.ErrorsList[0].Should().BeOfType(typeof(AuthenticationTimeoutException));
            tokenFetcher.ErrorsList[0].Message.Should().Be("Get Token Silent timed out after 5 minutes.");
        }

        /// <summary>
        /// The get token normal flow async_ get token silent_ throws_ msal client exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task GetTokenNormalFlowAsync_GetTokenSilent_Throws_MsalClientException()
        {
            this.SilentAuthClientException();

            // Act
            var tokenFetcher = this.Subject();
            var result = await tokenFetcher.GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert
            this.pcaMock.VerifyAll();
            result.Should().Be(null);
            tokenFetcher.ErrorsList.Should().HaveCount(1);
            tokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalClientException));
        }

        /// <summary>
        /// The get token normal flow async_ get token silent_ throws_ null reference exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task GetTokenNormalFlowAsync_GetTokenSilent_Throws_NullReferenceException()
        {
            this.SilentAuthNullReferenceException();

            // Act
            var tokenFetcher = this.Subject();
            var result = await tokenFetcher.GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert
            this.pcaMock.VerifyAll();
            result.Should().Be(null);
            tokenFetcher.ErrorsList.Should().HaveCount(1);
            tokenFetcher.ErrorsList[0].Should().BeOfType(typeof(NullReferenceException));
        }

        /// <summary>
        /// The get token normal flow async_ get token interactive_ throws_ msal ui exception_ for_ claims.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task GetTokenNormalFlowAsync_GetTokenInteractive_Throws_MsalUIException_For_Claims()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthExtraClaimsRequired();
            this.InteractiveAuthWithClaimsResult();

            // Act
            var tokenFetcher = this.Subject();
            var result = await tokenFetcher.GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert
            this.pcaMock.VerifyAll();
            result.Should().Be(this.tokenResult);
            result.AuthType.Should().Be(AuthType.Interactive);
            tokenFetcher.ErrorsList.Should().HaveCount(2);
            tokenFetcher.ErrorsList.Should().AllBeOfType(typeof(MsalUiRequiredException));
        }

        /// <summary>
        /// The get token normal flow async_ get token interactive_ throws_ msal service exception after using claims.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task GetTokenNormalFlowAsync_GetTokenInteractive_Throws_MsalServiceException_After_Using_Claims()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthExtraClaimsRequired();
            this.InteractiveAuthWithClaimsServiceException();

            // Act
            var tokenFetcher = this.Subject();
            var result = await tokenFetcher.GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert
            this.pcaMock.VerifyAll();
            result.Should().Be(null);
            tokenFetcher.ErrorsList.Should().HaveCount(3);
            tokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalUiRequiredException));
            tokenFetcher.ErrorsList[1].Should().BeOfType(typeof(MsalUiRequiredException));
            tokenFetcher.ErrorsList[2].Should().BeOfType(typeof(MsalServiceException));
        }

        /// <summary>
        /// The get token normal flow async_ get token interactive_ throws_ msal service exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task GetTokenNormalFlowAsync_GetTokenInteractive_Throws_MsalServiceException()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthServiceException();

            // Act
            var tokenFetcher = this.Subject();
            var result = await tokenFetcher.GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert
            this.pcaMock.VerifyAll();
            result.Should().Be(null);
            tokenFetcher.ErrorsList.Should().HaveCount(2);
            tokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalUiRequiredException));
            tokenFetcher.ErrorsList[1].Should().BeOfType(typeof(MsalServiceException));
        }

        /// <summary>
        /// The get token normal flow async_ get token interactive_ throws_ operation canceled exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task GetTokenNormalFlowAsync_GetTokenInteractive_Throws_OperationCanceledException()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthTimeout();

            // Act
            var tokenFetcher = this.Subject();
            var result = await tokenFetcher.GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert
            this.pcaMock.VerifyAll();
            result.Should().Be(null);
            tokenFetcher.ErrorsList.Should().HaveCount(2);
            tokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalUiRequiredException));
            tokenFetcher.ErrorsList[1].Should().BeOfType(typeof(AuthenticationTimeoutException));
            tokenFetcher.ErrorsList[1].Message.Should().Be("Interactive Auth timed out after 15 minutes.");
        }

        /// <summary>
        /// The get token normal flow async_ get token interactive_ throws_ operation canceled exception_ for_ claims.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task GetTokenNormalFlowAsync_GetTokenInteractive_Throws_OperationCanceledException_For_Claims()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthExtraClaimsRequired();
            this.InteractiveAuthWithClaimsTimeout();

            // Act
            var tokenFetcher = this.Subject();
            var result = await tokenFetcher.GetTokenNormalFlowAsync(this.pcaMock.Object, this.scopes, this.testAccount.Object);

            // Assert
            this.pcaMock.VerifyAll();
            result.Should().Be(null);
            tokenFetcher.ErrorsList.Should().HaveCount(3);
            tokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalUiRequiredException));
            tokenFetcher.ErrorsList[1].Should().BeOfType(typeof(MsalUiRequiredException));
            tokenFetcher.ErrorsList[2].Should().BeOfType(typeof(AuthenticationTimeoutException));
            tokenFetcher.ErrorsList[2].Message.Should().Be("Interactive Auth (with extra claims) timed out after 15 minutes.");
        }

        /// <summary>
        /// The get token_ device code_ flow_ happy path.
        /// </summary>
        [Test]
        [Ignore("Not implemented")]
        public void GetToken_DeviceCode_Flow_HappyPath()
        {
        }

        /// <summary>
        /// The get token_ device code_ msal service exception.
        /// </summary>
        [Test]
        [Ignore("Not implemented")]
        public void GetToken_DeviceCode_MsalServiceException()
        {
        }

        /// <summary>
        /// The try to get cached account async_ no accounts.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task TryToGetCachedAccountAsync_NoAccounts()
        {
            Mock<IPublicClientApplication> pcaMock = GetAccountsMock(new List<IAccount>());

            // Act
            IAccount result = await this.Subject().TryToGetCachedAccountAsync(pcaMock.Object);

            // Assert
            pcaMock.VerifyAll();
            result.Should().BeNull();
        }

        /// <summary>
        /// The try to get cached account async_ null.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task TryToGetCachedAccountAsync_Null()
        {
            Mock<IPublicClientApplication> pcaMock = GetAccountsMock(null);

            // Act
            IAccount result = await this.Subject().TryToGetCachedAccountAsync(pcaMock.Object);

            // Assert
            pcaMock.VerifyAll();
            result.Should().BeNull();
        }

        /// <summary>
        /// The try to get cached account async_ one account.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task TryToGetCachedAccountAsync_OneAccount()
        {
            var joe = new MockAccount("joe@microsoft.com");
            IList<IAccount> accounts = new List<IAccount>() { joe };

            Mock<IPublicClientApplication> pcaMock = GetAccountsMock(accounts);

            // Act
            IAccount result = await this.Subject().TryToGetCachedAccountAsync(pcaMock.Object);

            // Assert
            pcaMock.VerifyAll();
            result.Should().BeSameAs(joe);
        }

        /// <summary>
        /// The try to get cached account async_ two accounts.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task TryToGetCachedAccountAsync_TwoAccounts()
        {
            var first = new MockAccount("first@live.com");
            var second = new MockAccount("second@microsoft.com");
            IList<IAccount> accounts = new List<IAccount>() { first, second };

            Mock<IPublicClientApplication> pcaMock = GetAccountsMock(accounts);

            // Act
            IAccount result = await this.Subject().TryToGetCachedAccountAsync(pcaMock.Object);

            // Assert
            pcaMock.VerifyAll();
            result.Should().BeNull();
        }

        /// <summary>
        /// The try to get cached account async_ with preferred domain_ two accounts.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task TryToGetCachedAccountAsync_WithPreferredDomain_TwoAccounts()
        {
            IList<IAccount> accounts = new List<IAccount>() { this.userLive, this.userMicrosoft1 };

            Mock<IPublicClientApplication> pcaMock = GetAccountsMock(accounts);

            // Act
            IAccount result = await this.Subject().TryToGetCachedAccountAsync(pcaMock.Object, "microsoft.com");

            // Assert
            pcaMock.VerifyAll();
            result.Should().BeSameAs(this.userMicrosoft1);
        }

        /// <summary>
        /// The try to get cached account async_ with preferred domain_ multiple accounts.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task TryToGetCachedAccountAsync_WithPreferredDomain_MultipleAccounts()
        {
            IList<IAccount> accounts = new List<IAccount>() { this.userLive, this.userMicrosoft1, this.userMicrosoft2 };

            Mock<IPublicClientApplication> pcaMock = GetAccountsMock(accounts);

            // Act
            IAccount result = await this.Subject().TryToGetCachedAccountAsync(pcaMock.Object, "microsoft.com");

            // Assert
            pcaMock.VerifyAll();
            result.Should().BeNull();
        }

        /// <summary>
        /// The token fetcher_ with microsoft authority.
        /// </summary>
        [Test]
        public void TokenFetcher_WithAuthority()
        {
            // Act
            // Tenant Id is set to Default Microsoft AAD Tenant
            var tokenFetcher = this.Subject();

            // Assert
            tokenFetcher.Authority.Should().Be(this.authority);
        }

        private static Mock<IPublicClientApplication> GetAccountsMock(IEnumerable<IAccount> accounts)
        {
            Mock<IPublicClientApplication> pcaMock = new Mock<IPublicClientApplication>(MockBehavior.Strict);
            pcaMock
                .Setup(pca => pca.GetAccountsAsync())
                .ReturnsAsync(accounts);
            return pcaMock;
        }

        private void SilentAuthResult()
        {
            this.pcaMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.tokenResult);
        }

        private void SilentAuthTimeout()
        {
            this.pcaMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private void SilentAuthUIRequired()
        {
            this.pcaMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "UI is required"));
        }

        private void SilentAuthServiceException()
        {
            this.pcaMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalServiceExceptionErrorCode, MsalServiceExceptionMessage));
        }

        private void SilentAuthClientException()
        {
            this.pcaMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalClientException("1", "Could not find a WAM account for the silent request."));
        }

        private void SilentAuthNullReferenceException()
        {
            this.pcaMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new NullReferenceException("There was a null reference excpetion. This should absolutly never happen and if it does it is a bug."));
        }

        private void InteractiveAuthResult()
        {
            this.pcaMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.tokenResult);
        }

        private void InteractiveAuthWithClaimsResult()
        {
            this.pcaMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.tokenResult);
        }

        private void InteractiveAuthTimeout()
        {
            this.pcaMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private void InteractiveAuthExtraClaimsRequired()
        {
            this.pcaMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "Extra Claims are required."));
        }

        private void InteractiveAuthServiceException()
        {
            this.pcaMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalServiceExceptionErrorCode, MsalServiceExceptionMessage));
        }

        private void InteractiveAuthWithClaimsServiceException()
        {
            this.pcaMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalServiceExceptionErrorCode, MsalServiceExceptionMessage));
        }

        private void InteractiveAuthWithClaimsTimeout()
        {
            this.pcaMock
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private TokenFetcherPublicClient Subject() => this.serviceProvider.GetService<TokenFetcherPublicClient>();
    }
}
