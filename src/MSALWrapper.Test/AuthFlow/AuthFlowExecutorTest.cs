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

    public class AuthFlowExecutorTest
    {
        private const string NullAuthFlowResultExceptionMessage = "Auth flow 'IAuthFlowProxy' returned a null AuthFlowResult.";

        private IServiceProvider serviceProvider;
        private MemoryTarget logTarget;
        private TokenResult tokenResult;
        private IEnumerable<IAuthFlow> authFlows;

        [SetUp]
        public void Setup()
        {
            // Setup in memory logging target with NLog - allows making assertions against what has been logged.
            var loggingConfig = new NLog.Config.LoggingConfiguration();
            this.logTarget = new MemoryTarget("memory_target");
            loggingConfig.AddTarget(this.logTarget);
            loggingConfig.AddRuleForAllLevels(this.logTarget);
            this.authFlows = new List<IAuthFlow>();

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
        public void ConstructorWith_BothNullArgs()
        {
            Action authFlowExecutor = () => new AuthFlowExecutor(null, null);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ConstructorWith_Null_Logger()
        {
            Action authFlowExecutor = () => new AuthFlowExecutor(null, this.authFlows);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ConstructorWith_Null_AuthFlows()
        {
            var logger = this.serviceProvider.GetService<ILogger<AuthFlowExecutor>>();
            Action authFlowExecutor = () => new AuthFlowExecutor(logger, null);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ConstructorWith_Valid_Arguments()
        {
            var logger = this.serviceProvider.GetService<ILogger<AuthFlowExecutor>>();
            Action authFlowExecutor = () => new AuthFlowExecutor(logger, this.authFlows);

            // Assert
            authFlowExecutor.Should().NotThrow<ArgumentNullException>();
        }

        [Test]
        public async Task SingleAuthFlow_Returns_TokenResult()
        {
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            var authFlowName = "authFlowName1";
            var authFlowResult = new AuthFlowResult(this.tokenResult, null, authFlowName);
            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);
            resultList.Should().Contain(authFlowResult);

            var result = resultList.FirstOrDefault(x => x.AuthFlowName == authFlowName);
            result.Should().BeEquivalentTo(authFlowResult);
            result.TokenResult.Should().Be(this.tokenResult);
            result.Success.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task SingleAuthFlow_Returns_Null_TokenResult()
        {
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            var authFlowName = "authFlowName1";
            var authFlowResult = new AuthFlowResult(null, null, authFlowName);
            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);
            resultList.Should().Contain(authFlowResult);

            var result = resultList.FirstOrDefault(x => x.AuthFlowName == authFlowName);
            result.Should().BeEquivalentTo(authFlowResult);
            result.TokenResult.Should().BeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task SingleAuthFlow_Returns_Null_TokenResult_With_Errors()
        {
            var errors1 = new[]
            {
                new Exception("Exception 1."),
            };
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            var authFlowName = "authFlowName1";
            var authFlowResult = new AuthFlowResult(null, errors1, authFlowName);
            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);
            resultList.Should().Contain(authFlowResult);

            var result = resultList.FirstOrDefault(x => x.AuthFlowName == authFlowName);
            result.Should().BeEquivalentTo(authFlowResult);
            result.TokenResult.Should().BeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.Should().BeEquivalentTo(errors1);
        }

        [Test]
        public async Task SingleAuthFlow_Returns_Null_AuthFlowResult()
        {
            var errors1 = new[]
            {
                new NullTokenResultException(NullAuthFlowResultExceptionMessage),
            };
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlowResult = new AuthFlowResult(null, errors1, "IAuthFlowProxy");
            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result = resultList.FirstOrDefault(x => x.AuthFlowName == "IAuthFlowProxy");
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.Should().BeEquivalentTo(errors1);
            result.Should().BeEquivalentTo(authFlowResult);
        }

        [Test]
        public async Task HasTwoAuthFlows_Returns_Null_TokenResult()
        {
            var authFlowResult1 = new AuthFlowResult(null, null, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(this.tokenResult, null, "authFlow2");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result1 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow1");
            result1.Should().NotBeNull();
            result1.Success.Should().BeFalse();
            result1.Errors.Should().HaveCount(0);

            var result2 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow2");
            result2.Should().NotBeNull();
            result2.Success.Should().BeTrue();
            result2.Errors.Should().HaveCount(0);
            result2.TokenResult.Should().Be(this.tokenResult);
        }

        [Test]
        public async Task HasTwoAuthFlows_Returns_Null_TokenResult_With_Errors()
        {
            var errors1 = new[]
            {
                new Exception("Exception 1."),
            };
            var authFlowResult1 = new AuthFlowResult(null, errors1, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(this.tokenResult, null, "authFlow2");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result1 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow1");
            result1.Should().NotBeNull();
            result1.Success.Should().BeFalse();
            result1.Errors.Should().HaveCount(1);
            result1.Errors.Should().BeEquivalentTo(errors1);

            var result2 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow2");
            result2.Should().NotBeNull();
            result2.Success.Should().BeTrue();
            result2.Errors.Should().HaveCount(0);
            result2.TokenResult.Should().Be(this.tokenResult);
        }

        [Test]
        public async Task HasTwoAuthFlows_Returns_Null_AuthFlowResult()
        {
            var errors1 = new[]
            {
                new NullTokenResultException(NullAuthFlowResultExceptionMessage),
            };
            var authFlowResult1 = new AuthFlowResult(null, errors1, "IAuthFlowProxy");
            var authFlowResult2 = new AuthFlowResult(null, null, "authFlow2");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result1 = resultList.FirstOrDefault(x => x.AuthFlowName == "IAuthFlowProxy");
            result1.Should().NotBeNull();
            result1.Success.Should().BeFalse();
            result1.Errors.Should().HaveCount(1);
            result1.Errors.Should().BeEquivalentTo(errors1);

            var result2 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow2");
            result2.Should().NotBeNull();
            result2.Success.Should().BeFalse();
            result2.Errors.Should().HaveCount(0);
            result2.TokenResult.Should().BeNull();
        }

        [Test]
        public async Task HasTwoAuthFlows_Returns_Null_AuthFlowResult_With_Errors()
        {
            var errors1 = new[]
            {
                new NullTokenResultException(NullAuthFlowResultExceptionMessage),
            };

            var errors2 = new[]
            {
                new Exception("Exception 1"),
                new Exception("Exception 2"),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1, "IAuthFlowProxy");
            var authFlowResult2 = new AuthFlowResult(null, errors2, "authFlow2");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result1 = resultList.FirstOrDefault(x => x.AuthFlowName == "IAuthFlowProxy");
            result1.Should().NotBeNull();
            result1.Success.Should().BeFalse();
            result1.Errors.Should().HaveCount(1);
            result1.Errors.Should().BeEquivalentTo(errors1);

            var result2 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow2");
            result2.Should().NotBeNull();
            result2.Success.Should().BeFalse();
            result2.Errors.Should().HaveCount(2);
            result2.TokenResult.Should().BeNull();
        }

        [Test]
        public async Task HasTwoAuthFlows_Returns_Null_AuthFlowResult_With_TokenResultAndErrors()
        {
            var errors1 = new[]
            {
                new NullTokenResultException(NullAuthFlowResultExceptionMessage),
            };

            var errors2 = new[]
            {
                new Exception("Exception 1"),
                new Exception("Exception 2"),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1, "IAuthFlowProxy");
            var authFlowResult2 = new AuthFlowResult(this.tokenResult, errors2, "authFlow2");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result1 = resultList.FirstOrDefault(x => x.AuthFlowName == "IAuthFlowProxy");
            result1.Should().NotBeNull();
            result1.Success.Should().BeFalse();
            result1.Errors.Should().HaveCount(1);
            result1.Errors.Should().BeEquivalentTo(errors1);

            var result2 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow2");
            result2.Should().NotBeNull();
            result2.Success.Should().BeTrue();
            result2.Errors.Should().HaveCount(2);
            result2.TokenResult.Should().BeEquivalentTo(this.tokenResult);

            resultList.SelectMany(x => x.Errors).ToList().Should().BeEquivalentTo(new[] { errors1[0], errors2[0], errors2[1] });
        }

        [Test]
        public async Task HasThreeAuthFlows_Returns_Null_TokenResult()
        {
            var authFlowResult1 = new AuthFlowResult(null, null, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(null, null, "authFlow2");
            var authFlowResult3 = new AuthFlowResult(this.tokenResult, null, "authFlow3");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult3);

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);
            authFlowResultList.Add(authFlowResult3);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result1 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow1");
            result1.Should().NotBeNull();
            result1.Success.Should().BeFalse();
            result1.Errors.Should().HaveCount(0);

            var result2 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow2");
            result2.Should().NotBeNull();
            result2.Success.Should().BeFalse();
            result2.Errors.Should().HaveCount(0);

            var result3 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow3");
            result3.Should().NotBeNull();
            result3.Success.Should().BeTrue();
            result3.Errors.Should().HaveCount(0);
            result3.TokenResult.Should().BeEquivalentTo(this.tokenResult);
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

            var authFlowResult1 = new AuthFlowResult(null, errors1, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(null, errors2, "authFlow2");
            var authFlowResult3 = new AuthFlowResult(this.tokenResult, errors3, "authFlow3");

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);
            authFlowResultList.Add(authFlowResult3);

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult3);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1);
            resultList[1].Should().BeEquivalentTo(authFlowResult2);
            resultList[2].Should().BeEquivalentTo(authFlowResult3);
        }

        [Test]
        public async Task HasThreeAuthFlows_Returns_Null_AuthFlowResult()
        {
            var expectedError = new[]
            {
                new NullTokenResultException(NullAuthFlowResultExceptionMessage),
            };

            var authFlowResult1 = new AuthFlowResult(null, null, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(null, null, "authFlow2");
            var authFlowResult3 = new AuthFlowResult(null, expectedError, "IAuthFlowProxy");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);
            authFlowResultList.Add(authFlowResult3);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result1 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow1");
            result1.Should().NotBeNull();
            result1.Success.Should().BeFalse();
            result1.Errors.Should().HaveCount(0);

            var result2 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow2");
            result2.Should().NotBeNull();
            result2.Success.Should().BeFalse();
            result2.Errors.Should().HaveCount(0);

            var result3 = resultList.FirstOrDefault(x => x.AuthFlowName == "IAuthFlowProxy");
            result3.Should().NotBeNull();
            result3.Success.Should().BeFalse();
            result3.Errors.Should().HaveCount(1);
            result3.Errors.Should().BeEquivalentTo(expectedError);
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
                new NullTokenResultException(NullAuthFlowResultExceptionMessage),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(null, errors2, "authFlow2");
            var authFlowResult3 = new AuthFlowResult(null, errors3, "IAuthFlowProxy");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);
            authFlowResultList.Add(authFlowResult3);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result1 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow1");
            result1.Should().NotBeNull();
            result1.Success.Should().BeFalse();
            result1.Errors.Should().HaveCount(1);

            var result2 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow2");
            result2.Should().NotBeNull();
            result2.Success.Should().BeFalse();
            result2.Errors.Should().HaveCount(1);

            var result3 = resultList.FirstOrDefault(x => x.AuthFlowName == "IAuthFlowProxy");
            result3.Should().NotBeNull();
            result3.Success.Should().BeFalse();
            result3.Errors.Should().HaveCount(1);
            result3.Errors.Should().BeEquivalentTo(errors3);
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
                new NullTokenResultException(NullAuthFlowResultExceptionMessage),
            };

            var errors3 = new[]
            {
                new Exception("Exception 2"),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(null, errors2, "IAuthFlowProxy");
            var authFlowResult3 = new AuthFlowResult(null, errors3, "authFlow3");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult3);

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);
            authFlowResultList.Add(authFlowResult3);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result1 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow1");
            result1.Should().NotBeNull();
            result1.Success.Should().BeFalse();
            result1.Errors.Should().HaveCount(1);

            var result2 = resultList.FirstOrDefault(x => x.AuthFlowName == "IAuthFlowProxy");
            result2.Should().NotBeNull();
            result2.Success.Should().BeFalse();
            result2.Errors.Should().HaveCount(1);
            result2.Errors.Should().BeEquivalentTo(errors2);

            var result3 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow3");
            result3.Should().NotBeNull();
            result3.Success.Should().BeFalse();
            result3.Errors.Should().HaveCount(1);
            result3.Errors.Should().BeEquivalentTo(errors3);
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
                new NullTokenResultException(NullAuthFlowResultExceptionMessage),
            };

            var errors3 = new[]
            {
                new Exception("Exception 2"),
                new Exception("Exception 3"),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(null, errors2, "IAuthFlowProxy");
            var authFlowResult3 = new AuthFlowResult(this.tokenResult, errors3, "authFlow3");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult3);

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);
            authFlowResultList.Add(authFlowResult3);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result1 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow1");
            result1.Should().NotBeNull();
            result1.Success.Should().BeFalse();
            result1.Errors.Should().HaveCount(1);

            var result2 = resultList.FirstOrDefault(x => x.AuthFlowName == "IAuthFlowProxy");
            result2.Should().NotBeNull();
            result2.Success.Should().BeFalse();
            result2.Errors.Should().HaveCount(1);
            result2.Errors.Should().BeEquivalentTo(errors2);

            var result3 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow3");
            result3.Should().NotBeNull();
            result3.Success.Should().BeTrue();
            result3.Errors.Should().HaveCount(2);
            result3.Errors.Should().BeEquivalentTo(errors3);
            result3.TokenResult.Should().BeEquivalentTo(this.tokenResult);
        }

        [Test]
        public async Task HasThreeAuthFlows_Returns_Early_With_TokenResultAndErrors()
        {
            var errors1 = new[]
            {
                new Exception("Exception 1"),
            };

            var errors2 = new[]
            {
                new Exception("Exception 2"),
                new Exception("Exception 3"),
            };

            var errors3 = new[]
            {
                new Exception("This is a catastrophic failure. AuthFlow result is null!"),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(this.tokenResult, errors2, "authFlow2");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow3.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult1);
            authFlowResultList.Add(authFlowResult2);

            // Act
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
            var resultList = await authFlowExecutor.GetTokenAsync();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            var result1 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow1");
            result1.Should().NotBeNull();
            result1.Success.Should().BeFalse();
            result1.Errors.Should().HaveCount(1);

            var result2 = resultList.FirstOrDefault(x => x.AuthFlowName == "authFlow2");
            result2.Should().NotBeNull();
            result2.Success.Should().BeTrue();
            result2.Errors.Should().HaveCount(2);
            result2.Errors.Should().BeEquivalentTo(errors2);
            result2.TokenResult.Should().BeEquivalentTo(this.tokenResult);
        }

        private AuthFlowExecutor Subject(IEnumerable<IAuthFlow> authFlows)
        {
            var logger = this.serviceProvider.GetService<ILogger<AuthFlowExecutor>>();
            return new AuthFlowExecutor(logger, authFlows);
        }
    }
}
