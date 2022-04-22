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
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;
    using Moq;
    using NLog.Extensions.Logging;
    using NLog.Targets;
    using NUnit.Framework;

    internal class DeviceCodeTest
    {
        private const string TestUser = "user@microsoft.com";

        // These Guids were randomly generated and do not refer to a real resource or client
        // as we don't need either for our testing.
        private static readonly Guid ResourceId = new Guid("6e979987-a7c8-4604-9b37-e51f06f08f1a");
        private static readonly Guid ClientId = new Guid("5af6def2-05ec-4cab-b9aa-323d75b5df40");
        private static readonly Guid TenantId = new Guid("8254f6f7-a09f-4752-8bd6-391adc3b912e");

        private string promptHint = "test prompt hint";

        private IServiceProvider serviceProvider;
        private MemoryTarget logTarget;

        // MSAL Specific Mocks
        private Mock<IPCAWrapper> pcaWrapperMock;
        private IEnumerable<string> scopes = new string[] { $"{ResourceId}/.default" };
        private TokenResult tokenResult;

        /// <summary>
        /// The test setup.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Setup in memory logging target with NLog - allows making assertions against what has been logged.
            var loggingConfig = new NLog.Config.LoggingConfiguration();
            this.logTarget = new MemoryTarget("memory_target");
            loggingConfig.AddTarget(this.logTarget);
            loggingConfig.AddRuleForAllLevels(this.logTarget);

            this.pcaWrapperMock = new Mock<IPCAWrapper>(MockBehavior.Strict);

            // Setup Dependency Injection container to provide logger and out class under test (the "subject")
            this.serviceProvider = new ServiceCollection()
             .AddLogging(loggingBuilder =>
             {
                 // configure Logging with NLog
                 loggingBuilder.ClearProviders();
                 loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                 loggingBuilder.AddNLog(loggingConfig);
             })
             .AddTransient<AuthFlow.DeviceCode>((provider) =>
             {
                 var logger = provider.GetService<ILogger<AuthFlow.DeviceCode>>();
                 return new AuthFlow.DeviceCode(logger, ClientId, TenantId, this.scopes, pcaWrapper: this.pcaWrapperMock.Object, promptHint: this.promptHint);
             })
             .BuildServiceProvider();

            // Mock successful token result
            this.tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken));
        }

        /// <summary>
        /// Get a new instance of the class under test.
        /// </summary>
        /// <returns>The <see cref="AuthFlow.DeviceCode"/> registered in the <see cref="Setup"/> method.</returns>
        public AuthFlow.DeviceCode Subject() => this.serviceProvider.GetService<AuthFlow.DeviceCode>();

        [Test]
        public async Task DeviceCodeAuthFlow_HappyPath()
        {
            this.DeviceCodeAuthResult();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.TokenResult.AuthType.Should().Be(AuthType.DeviceCodeFlow);
            authFlowResult.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task DeviceCodeAuthFlow_Returns_Null()
        {
            this.DeviceCodeAuthReturnsNull();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task DeviceCodeAuthFlow_MsalException()
        {
            this.DeviceCodeAuthMsalException();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalException));
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
    }
}
