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
    using Microsoft.Authentication.MSALWrapper.Test;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;
    using Moq;
    using NLog.Extensions.Logging;
    using NLog.Targets;
    using NUnit.Framework;

    /// <summary>
    /// The main auth flow test.
    /// </summary>
    public class AuthFlowExecutorTest
    {
        private const string TestUser = "user@microsoft.com";

        private IServiceProvider serviceProvider;
        private MemoryTarget logTarget;

        // MSAL Specific Mocks
        private Mock<IPCAWrapper> pcaWrapperMock;
        private Mock<IAccount> testAccount;
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

            // MSAL Mocks
            this.testAccount = new Mock<IAccount>(MockBehavior.Strict);
            this.testAccount.Setup(a => a.Username).Returns(TestUser);

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
             .BuildServiceProvider();

            // Mock successful token result
            this.tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken));
        }

        [Test]
        public async Task SingleAuthFlow_Returns_TokenResult()
        {
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            var authFlowResult = new AuthFlowResult(this.tokenResult, null);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            result.Should().NotBeNull();
            result.TokenResult.Should().Be(this.tokenResult);
            result.Success.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task SingleAuthFlow_Returns_Null()
        {
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
        }

        [Test]
        public async Task HasTwoAuthFlows_Returns_Null_TokenResult()
        {
            var authFlowResult1 = new AuthFlowResult(null, null);
            var authFlowResult2 = new AuthFlowResult(this.tokenResult, null);

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            result.TokenResult.Should().Be(this.tokenResult);
            result.Success.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task HasTwoAuthFlows_Returns_Null_TokenResult_With_Errors()
        {
            var errors1 = new[]
            {
                new Exception("Exception 1."),
            };
            var authFlowResult1 = new AuthFlowResult(null, errors1);
            var authFlowResult2 = new AuthFlowResult(this.tokenResult, null);

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            result.TokenResult.Should().Be(this.tokenResult);
            result.Success.Should().BeTrue();
            result.Errors.Should().BeEquivalentTo(errors1);
        }

        [Ignore("Not yet!")]
        [Test]
        public async Task AuthFlowExecutor_HasTwoAuthFlows_Returns_Null_AuthFlowResult()
        {
            var authFlowResult = new AuthFlowResult(this.tokenResult, null);

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            result.TokenResult.Should().Be(this.tokenResult);
            result.Success.Should().BeTrue();

            // result.Errors.Should().NotBeNullOrEmpty();
        }

        private AuthFlowExecutor Subject(IEnumerable<IAuthFlow> authFlows)
        {
            var logger = this.serviceProvider.GetService<ILogger<AuthFlowExecutor>>();
            return new AuthFlowExecutor(logger, authFlows);
        }
    }
}
