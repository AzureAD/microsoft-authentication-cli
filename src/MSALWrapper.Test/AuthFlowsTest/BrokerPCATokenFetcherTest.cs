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
    using Microsoft.Authentication.MSALWrapper.AuthFlows;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;
    using Moq;
    using NLog.Extensions.Logging;
    using NLog.Targets;
    using NUnit.Framework;

    /// <summary>
    /// The broker public client test.
    /// </summary>
    public class BrokerPCATokenFetcherTest
    {
        private const string MsalServiceExceptionErrorCode = "1";
        private const string MsalServiceExceptionMessage = "MSAL Service Exception: Something bad has happened!";
        private const string TestUser = "user@microsoft.com";

        // These Guids were randomly generated and do not refer to a real resource or client
        // as we don't need either for our testing.
        private static readonly Guid ResourceId = new Guid("6e979987-a7c8-4604-9b37-e51f06f08f1a");
        private static readonly Guid ClientId = new Guid("5af6def2-05ec-4cab-b9aa-323d75b5df40");
        private static readonly Guid TenantId = new Guid("8254f6f7-a09f-4752-8bd6-391adc3b912e");

        private IServiceProvider serviceProvider;
        private MemoryTarget logTarget;

        // MSAL Specific Mocks
        private Mock<IPCAWrapper> pcaWrapper;
        private Mock<IPublicClientApplication> pcaClient;
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

            // MSAL Mocks
            this.testAccount = new Mock<IAccount>(MockBehavior.Strict);
            this.testAccount.Setup(a => a.Username).Returns(TestUser);

            this.pcaWrapper = new Mock<IPCAWrapper>(MockBehavior.Strict);
            this.pcaClient = new Mock<IPublicClientApplication>(MockBehavior.Strict);

            // Setup Dependency Injection container to provide logger and out class under test (the "subject")
            this.serviceProvider = new ServiceCollection()
             .AddLogging(loggingBuilder =>
             {
                 // configure Logging with NLog
                 loggingBuilder.ClearProviders();
                 loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                 loggingBuilder.AddNLog(loggingConfig);
             })
             .AddTransient<BrokerPCATokenFetcher>((provider) =>
             {
                 var logger = provider.GetService<ILogger<BrokerPCATokenFetcher>>();
                 return new BrokerPCATokenFetcher(logger, ClientId, TenantId, this.scopes);
             })
             .AddTransient<IAccountsProvider>((provider) =>
             {
                 var logger = provider.GetService<ILogger<BrokerPCATokenFetcher>>();
                 return new AccountsProvider(this.pcaClient.Object, logger);
             })
             .BuildServiceProvider();

            // Mock successful token result
            this.tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken));
        }

        /// <summary>
        /// The broker auth flow happy path.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task BrokerAuthFlow_HappyPath()
        {
            this.SilentAuthResult();

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            var result = await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert
            this.pcaWrapper.VerifyAll();
            result.Should().Be(this.tokenResult);
            result.AuthType.Should().Be(AuthType.Silent);
            brokerPcaTokenFetcher.ErrorsList.Should().BeEmpty();
        }

        /// <summary>
        /// The broker auth flow throws MSAL UI exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task BrokerAuthFlow_MsalUIException()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthResult();

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            var result = await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert
            this.pcaWrapper.VerifyAll();
            result.Should().Be(this.tokenResult);
            result.AuthType.Should().Be(AuthType.Interactive);
            brokerPcaTokenFetcher.ErrorsList.Should().HaveCount(1);
            brokerPcaTokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalUiRequiredException));
        }

        /// <summary>
        /// The broker auth flow throws general exception.
        /// </summary>
        [Test]
        public void BrokerAuthFlow_General_Exceptions_Are_ReThrown()
        {
            var message = "Something somwhere has gone terribly wrong!";
            this.pcaWrapper
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new Exception(message));

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            Func<Task> subject = async () => await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert
            subject.Should().ThrowExactlyAsync<Exception>().WithMessage(message);

            // This VerifyAll must come after the assert, since the assert is what execute the lambda
            this.pcaWrapper.VerifyAll();
        }

        /// <summary>
        /// The broker auth flow throws MSAL Service exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task BrokerAuthFlow_GetTokenSilent_MsalServiceException()
        {
            this.SilentAuthServiceException();

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            var result = await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert - this method should not throw for known types of excpeptions, instead return null, so
            // our caller can retry auth another way.
            this.pcaWrapper.VerifyAll();
            result.Should().Be(null);
            brokerPcaTokenFetcher.ErrorsList.Should().HaveCount(1);
            brokerPcaTokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalServiceException));
        }

        /// <summary>
        /// The broker auth flow get token silent throws operation canceled exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task BrokerAuthFlow_GetTokenSilent_OperationCanceledException()
        {
            this.SilentAuthTimeout();

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            var result = await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert
            this.pcaWrapper.VerifyAll();
            result.Should().Be(null);
            brokerPcaTokenFetcher.ErrorsList.Should().HaveCount(1);
            brokerPcaTokenFetcher.ErrorsList[0].Should().BeOfType(typeof(AuthenticationTimeoutException));
            brokerPcaTokenFetcher.ErrorsList[0].Message.Should().Be("Get Token Silent timed out after 5 minutes.");
        }

        /// <summary>
        /// The broker auth flow get token silent throws Msal Client exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task BrokerAuthFlow_GetTokenSilent_MsalClientException()
        {
            this.SilentAuthClientException();

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            var result = await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert
            this.pcaWrapper.VerifyAll();
            result.Should().Be(null);
            brokerPcaTokenFetcher.ErrorsList.Should().HaveCount(1);
            brokerPcaTokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalClientException));
        }

        /// <summary>
        /// The broker auth flow get token silent throws null reference exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task BrokerAuthFlow_GetTokenSilent_NullReferenceException()
        {
            this.SilentAuthNullReferenceException();

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            var result = await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert
            this.pcaWrapper.VerifyAll();
            result.Should().Be(null);
            brokerPcaTokenFetcher.ErrorsList.Should().HaveCount(1);
            brokerPcaTokenFetcher.ErrorsList[0].Should().BeOfType(typeof(NullReferenceException));
        }

        /// <summary>
        /// The broker auth flow get token interactive throws MSAL UI exception for Claims.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task BrokerAuthFlow_GetTokenInteractive_MsalUIException_For_Claims()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthExtraClaimsRequired();
            this.InteractiveAuthWithClaimsResult();

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            var result = await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert
            this.pcaWrapper.VerifyAll();
            result.Should().Be(this.tokenResult);
            result.AuthType.Should().Be(AuthType.Interactive);
            brokerPcaTokenFetcher.ErrorsList.Should().HaveCount(2);
            brokerPcaTokenFetcher.ErrorsList.Should().AllBeOfType(typeof(MsalUiRequiredException));
        }

        /// <summary>
        /// The broker auth flow get token interactive throws MSAL service exception after using Claims.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task BrokerAuthFlow_GetTokenInteractive_MsalServiceException_After_Using_Claims()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthExtraClaimsRequired();
            this.InteractiveAuthWithClaimsServiceException();

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            var result = await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert
            this.pcaWrapper.VerifyAll();
            result.Should().Be(null);
            brokerPcaTokenFetcher.ErrorsList.Should().HaveCount(3);
            brokerPcaTokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalUiRequiredException));
            brokerPcaTokenFetcher.ErrorsList[1].Should().BeOfType(typeof(MsalUiRequiredException));
            brokerPcaTokenFetcher.ErrorsList[2].Should().BeOfType(typeof(MsalServiceException));
        }

        /// <summary>
        /// The broker auth flow get token interactive throws MSAL service exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task BrokerAuthFlow_GetTokenInteractive_MsalServiceException()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthServiceException();

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            var result = await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert
            this.pcaWrapper.VerifyAll();
            result.Should().Be(null);
            brokerPcaTokenFetcher.ErrorsList.Should().HaveCount(2);
            brokerPcaTokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalUiRequiredException));
            brokerPcaTokenFetcher.ErrorsList[1].Should().BeOfType(typeof(MsalServiceException));
        }

        /// <summary>
        /// The broker auth flow get token interactive throws operation canceled exception.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task BrokerAuthFlow_GetTokenInteractive_OperationCanceledException()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthTimeout();

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            var result = await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert
            this.pcaWrapper.VerifyAll();
            result.Should().Be(null);
            brokerPcaTokenFetcher.ErrorsList.Should().HaveCount(2);
            brokerPcaTokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalUiRequiredException));
            brokerPcaTokenFetcher.ErrorsList[1].Should().BeOfType(typeof(AuthenticationTimeoutException));
            brokerPcaTokenFetcher.ErrorsList[1].Message.Should().Be("Interactive Auth timed out after 15 minutes.");
        }

        /// <summary>
        /// The broker auth flow get token interactive throws operation canceled exception for claims.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Test]
        public async Task BrokerAuthFlow_GetTokenInteractive_OperationCanceledException_For_Claims()
        {
            this.SilentAuthUIRequired();
            this.InteractiveAuthExtraClaimsRequired();
            this.InteractiveAuthWithClaimsTimeout();

            this.GetAccountsMock();

            // Act
            BrokerPCATokenFetcher brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            var result = await brokerPcaTokenFetcher.GetTokenAsync();

            // Assert
            this.pcaWrapper.VerifyAll();
            result.Should().Be(null);
            brokerPcaTokenFetcher.ErrorsList.Should().HaveCount(3);
            brokerPcaTokenFetcher.ErrorsList[0].Should().BeOfType(typeof(MsalUiRequiredException));
            brokerPcaTokenFetcher.ErrorsList[1].Should().BeOfType(typeof(MsalUiRequiredException));
            brokerPcaTokenFetcher.ErrorsList[2].Should().BeOfType(typeof(AuthenticationTimeoutException));
            brokerPcaTokenFetcher.ErrorsList[2].Message.Should().Be("Interactive Auth (with extra claims) timed out after 15 minutes.");
        }

        private void GetAccountsMock()
        {
            this.pcaClient
                .Setup(pca => pca.GetAccountsAsync())
                .ReturnsAsync(new List<IAccount>() { this.testAccount.Object });
        }

        private void SilentAuthResult()
        {
            this.pcaWrapper
               .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
               .ReturnsAsync(this.tokenResult);
        }

        private void SilentAuthUIRequired()
        {
            this.pcaWrapper
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "UI is required"));
        }

        private void SilentAuthServiceException()
        {
            this.pcaWrapper
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalServiceExceptionErrorCode, MsalServiceExceptionMessage));
        }

        private void SilentAuthTimeout()
        {
            this.pcaWrapper
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private void SilentAuthClientException()
        {
            this.pcaWrapper
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalClientException("1", "Could not find a WAM account for the silent request."));
        }

        private void SilentAuthNullReferenceException()
        {
            this.pcaWrapper
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new NullReferenceException("There was a null reference excpetion. This should absolutly never happen and if it does it is a bug."));
        }

        private void InteractiveAuthResult()
        {
            this.pcaWrapper
               .Setup((pca) => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
               .ReturnsAsync(this.tokenResult);
        }

        private void InteractiveAuthWithClaimsResult()
        {
            this.pcaWrapper
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.tokenResult);
        }

        private void InteractiveAuthTimeout()
        {
            this.pcaWrapper
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private void InteractiveAuthExtraClaimsRequired()
        {
            this.pcaWrapper
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "Extra Claims are required."));
        }

        private void InteractiveAuthServiceException()
        {
            this.pcaWrapper
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalServiceExceptionErrorCode, MsalServiceExceptionMessage));
        }

        private void InteractiveAuthWithClaimsServiceException()
        {
            this.pcaWrapper
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalServiceExceptionErrorCode, MsalServiceExceptionMessage));
        }

        private void InteractiveAuthWithClaimsTimeout()
        {
            this.pcaWrapper
                .Setup(pca => pca.GetTokenInteractiveAsync(this.scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private BrokerPCATokenFetcher Subject() => this.serviceProvider.GetService<BrokerPCATokenFetcher>();

        private BrokerPCATokenFetcher GetBrokerPCATokenFetcherMock()
        {
            var brokerPcaTokenFetcher = this.Subject();
            brokerPcaTokenFetcher.PCAWrapper = this.pcaWrapper.Object;
            brokerPcaTokenFetcher.PublicClientApplication = this.pcaClient.Object;
            brokerPcaTokenFetcher.AccountsProvider = this.serviceProvider.GetService<IAccountsProvider>();
            return brokerPcaTokenFetcher;
        }
    }
}
