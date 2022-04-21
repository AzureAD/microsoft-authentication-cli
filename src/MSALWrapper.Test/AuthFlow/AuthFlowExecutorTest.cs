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
        public async Task SingleAuthFlow_Returns_Null_TokenResult()
        {
            var authFlowResult = new AuthFlowResult(null, null);
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().BeNullOrEmpty();
        }

        [Test]
        public async Task SingleAuthFlow_Returns_Null_TokenResult_With_Errors()
        {
            var errors1 = new[]
            {
                new Exception("Exception 1."),
            };
            var authFlowResult = new AuthFlowResult(null, errors1);
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.Should().BeEquivalentTo(errors1);
        }

        [Test]
        public async Task SingleAuthFlow_Returns_Null_AuthFlowResult()
        {
            var errors1 = new[]
            {
                new Exception("This is a catastrophic failure. AuthFlow result is null!"),
            };
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
            result.Errors.Should().BeEquivalentTo(errors1);
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

        [Test]
        public async Task HasTwoAuthFlows_Returns_Null_AuthFlowResult()
        {
            var errors1 = new[]
            {
                new Exception("This is a catastrophic failure. AuthFlow result is null!"),
            };
            var authFlowResult = new AuthFlowResult(null, null);

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
            result.TokenResult.Should().BeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.Should().BeEquivalentTo(errors1);
        }

        [Test]
        public async Task HasTwoAuthFlows_Returns_Null_AuthFlowResult_With_Errors()
        {
            var errors1 = new[]
            {
                new Exception("This is a catastrophic failure. AuthFlow result is null!"),
            };

            var errors2 = new[]
            {
                new Exception("Exception 1"),
                new Exception("Exception 2"),
            };

            var authFlowResult = new AuthFlowResult(null, errors2);

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
            result.TokenResult.Should().BeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().HaveCount(3);
            result.Errors[0].Should().BeEquivalentTo(errors1[0]);
            result.Errors[1].Should().BeEquivalentTo(errors2[0]);
            result.Errors[2].Should().BeEquivalentTo(errors2[1]);
        }

        [Test]
        public async Task HasTwoAuthFlows_Returns_Null_AuthFlowResult_With_TokenResultAndErrors()
        {
            var errors1 = new[]
            {
                new Exception("This is a catastrophic failure. AuthFlow result is null!"),
            };

            var errors2 = new[]
            {
                new Exception("Exception 1"),
                new Exception("Exception 2"),
            };

            var authFlowResult = new AuthFlowResult(this.tokenResult, errors2);

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
            result.Errors.Should().HaveCount(3);
            result.Errors[0].Should().BeEquivalentTo(errors1[0]);
            result.Errors[1].Should().BeEquivalentTo(errors2[0]);
            result.Errors[2].Should().BeEquivalentTo(errors2[1]);
        }

        [Test]
        public async Task HasThreeAuthFlows_Returns_Null_TokenResult()
        {
            var authFlowResult1 = new AuthFlowResult(null, null);
            var authFlowResult2 = new AuthFlowResult(null, null);
            var authFlowResult3 = new AuthFlowResult(this.tokenResult, null);

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult3);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            result.TokenResult.Should().Be(this.tokenResult);
            result.Success.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task HasThreeAuthFlows_Returns_Null_TokenResult_With_Errors()
        {
            var errors1 = new[]
            {
                new Exception("Exception 1."),
            };

            var errors2 = new[]
            {
                new Exception("Exception 2."),
            };

            var errors3 = new[]
            {
                new Exception("Exception 3."),
                new Exception("Exception 4."),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1);
            var authFlowResult2 = new AuthFlowResult(null, errors2);
            var authFlowResult3 = new AuthFlowResult(this.tokenResult, errors3);

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult3);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            result.TokenResult.Should().Be(this.tokenResult);
            result.Success.Should().BeTrue();
            result.Errors.Should().HaveCount(4);
            result.Errors[0].Should().BeOfType(typeof(Exception));
            result.Errors[1].Should().BeOfType(typeof(Exception));
            result.Errors[2].Should().BeOfType(typeof(Exception));
            result.Errors[3].Should().BeOfType(typeof(Exception));
            result.Errors[0].Should().BeEquivalentTo(errors1[0]);
            result.Errors[1].Should().BeEquivalentTo(errors2[0]);
            result.Errors[2].Should().BeEquivalentTo(errors3[0]);
            result.Errors[3].Should().BeEquivalentTo(errors3[1]);
        }

        [Test]
        public async Task HasThreeAuthFlows_Returns_Null_AuthFlowResult()
        {
            var errors1 = new[]
            {
                new Exception("This is a catastrophic failure. AuthFlow result is null!"),
            };

            var authFlowResult1 = new AuthFlowResult(null, null);
            var authFlowResult2 = new AuthFlowResult(null, null);

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            result.TokenResult.Should().BeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.Should().BeEquivalentTo(errors1);
        }

        [Test]
        public async Task HasThreeAuthFlows_Returns_Null_AuthFlowResult_With_Errors()
        {
            var errors1 = new[]
            {
                new Exception("Exception 1"),
            };

            var errors2 = new[]
            {
                new Exception("Exception 2"),
            };

            var errors3 = new[]
            {
                new Exception("This is a catastrophic failure. AuthFlow result is null!"),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1);
            var authFlowResult2 = new AuthFlowResult(null, errors2);

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            result.TokenResult.Should().BeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().HaveCount(3);
            result.Errors[0].Should().BeEquivalentTo(errors1[0]);
            result.Errors[1].Should().BeEquivalentTo(errors2[0]);
            result.Errors[2].Should().BeEquivalentTo(errors3[0]);
        }

        [Test]
        public async Task HasThreeAuthFlows_Returns_Null_AuthFlowResult_With_Errors_InTheSecondAuthFlow()
        {
            var errors1 = new[]
            {
                new Exception("Exception 1"),
            };

            var errors2 = new[]
            {
                new Exception("This is a catastrophic failure. AuthFlow result is null!"),
            };

            var errors3 = new[]
            {
                new Exception("Exception 2"),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1);
            var authFlowResult3 = new AuthFlowResult(null, errors3);

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult3);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            result.TokenResult.Should().BeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().HaveCount(3);
            result.Errors[0].Should().BeEquivalentTo(errors1[0]);
            result.Errors[1].Should().BeEquivalentTo(errors2[0]);
            result.Errors[2].Should().BeEquivalentTo(errors3[0]);
        }

        [Test]
        public async Task HasThreeAuthFlows_Returns_Null_AuthFlowResult_With_TokenResultAndErrors()
        {
            var errors1 = new[]
            {
                new Exception("Exception 1"),
            };

            var errors2 = new[]
            {
                new Exception("This is a catastrophic failure. AuthFlow result is null!"),
            };

            var errors3 = new[]
            {
                new Exception("Exception 2"),
                new Exception("Exception 3"),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1);
            var authFlowResult3 = new AuthFlowResult(this.tokenResult, errors3);

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult3);

            // Act
            var authFlow = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var result = await authFlow.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            result.TokenResult.Should().Be(this.tokenResult);
            result.Success.Should().BeTrue();
            result.Errors.Should().HaveCount(4);
            result.Errors[0].Should().BeEquivalentTo(errors1[0]);
            result.Errors[1].Should().BeEquivalentTo(errors2[0]);
            result.Errors[2].Should().BeEquivalentTo(errors3[0]);
            result.Errors[3].Should().BeEquivalentTo(errors3[1]);
        }

        private AuthFlowExecutor Subject(IEnumerable<IAuthFlow> authFlows)
        {
            var logger = this.serviceProvider.GetService<ILogger<AuthFlowExecutor>>();
            return new AuthFlowExecutor(logger, authFlows);
        }
    }
}
