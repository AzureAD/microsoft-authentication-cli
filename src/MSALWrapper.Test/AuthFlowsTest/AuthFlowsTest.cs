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
    public class AuthFlowsTest
    {
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
             .AddTransient<AuthFlows>((provider) =>
             {
                 var logger = provider.GetService<ILogger<AuthFlows>>();
                 return new AuthFlows(logger, ResourceId, ClientId, TenantId);
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
        /// The broker auth flow is expected.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        [Test]
        public async Task BrokerAuthFlowIsExpected()
        {
            var brokerPcaTokenFetcher = this.GetBrokerPCATokenFetcherMock();
            this.GetAccountsMock();

            // Act
            var authFlows = this.Subject();
            authFlows.Authflows.Add(brokerPcaTokenFetcher);
            var result = await authFlows.GetTokenAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(this.tokenResult);
        }

        private AuthFlows Subject() => this.serviceProvider.GetService<AuthFlows>();

        private BrokerPCATokenFetcher Subject1() => this.serviceProvider.GetService<BrokerPCATokenFetcher>();

        private BrokerPCATokenFetcher GetBrokerPCATokenFetcherMock()
        {
            var brokerPcaTokenFetcher = this.Subject1();
            brokerPcaTokenFetcher.PCAWrapper = this.pcaWrapper.Object;
            brokerPcaTokenFetcher.PublicClientApplication = this.pcaClient.Object;
            brokerPcaTokenFetcher.AccountsProvider = this.serviceProvider.GetService<IAccountsProvider>();

            this.pcaWrapper
               .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
               .ReturnsAsync(this.tokenResult);

            return brokerPcaTokenFetcher;
        }

        private void GetAccountsMock()
        {
            this.pcaClient
                .Setup(pca => pca.GetAccountsAsync())
                .ReturnsAsync(new List<IAccount>() { this.testAccount.Object });
        }
    }
}
