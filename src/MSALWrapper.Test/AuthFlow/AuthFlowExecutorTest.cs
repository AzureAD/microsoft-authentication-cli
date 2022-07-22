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
    using FluentAssertions.Equivalency;
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
        private ITimeoutManager timeoutManager;

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
            this.tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken), Guid.NewGuid());
            this.timeoutManager = new TimeoutManager(TimeSpan.FromSeconds(60));
        }

        [Test]
        public void ConstructorWith_AllNullArgs()
        {
            Action authFlowExecutor = () => new AuthFlowExecutor(null, null, null);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ConstructorWith_Null_Logger()
        {
            Action authFlowExecutor = () => new AuthFlowExecutor(null, this.authFlows, this.timeoutManager);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ConstructorWith_Null_AuthFlows()
        {
            var logger = this.serviceProvider.GetService<ILogger<AuthFlowExecutor>>();
            Action authFlowExecutor = () => new AuthFlowExecutor(logger, null, this.timeoutManager);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ConstructorWith_Valid_Arguments()
        {
            var logger = this.serviceProvider.GetService<ILogger<AuthFlowExecutor>>();
            Action authFlowExecutor = () => new AuthFlowExecutor(logger, this.authFlows, this.timeoutManager);

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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);
            resultList.Should().Contain(authFlowResult);

            // Assert Order of results.
            resultList.ToList()[0].Should().BeEquivalentTo(authFlowResult);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);
            resultList.Should().Contain(authFlowResult);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);
            resultList.Should().Contain(authFlowResult);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult, this.ExcludeDurationTimeSpan);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1);
            resultList[1].Should().BeEquivalentTo(authFlowResult2);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1);
            resultList[1].Should().BeEquivalentTo(authFlowResult2);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1, this.ExcludeDurationTimeSpan);
            resultList[1].Should().BeEquivalentTo(authFlowResult2, this.ExcludeDurationTimeSpan);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1, this.ExcludeDurationTimeSpan);
            resultList[1].Should().BeEquivalentTo(authFlowResult2, this.ExcludeDurationTimeSpan);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1, this.ExcludeDurationTimeSpan);
            resultList[1].Should().BeEquivalentTo(authFlowResult2, this.ExcludeDurationTimeSpan);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1, this.ExcludeDurationTimeSpan);
            resultList[1].Should().BeEquivalentTo(authFlowResult2, this.ExcludeDurationTimeSpan);
            resultList[2].Should().BeEquivalentTo(authFlowResult3, this.ExcludeDurationTimeSpan);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1, this.ExcludeDurationTimeSpan);
            resultList[1].Should().BeEquivalentTo(authFlowResult2, this.ExcludeDurationTimeSpan);
            resultList[2].Should().BeEquivalentTo(authFlowResult3, this.ExcludeDurationTimeSpan);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1, this.ExcludeDurationTimeSpan);
            resultList[1].Should().BeEquivalentTo(authFlowResult2, this.ExcludeDurationTimeSpan);
            resultList[2].Should().BeEquivalentTo(authFlowResult3, this.ExcludeDurationTimeSpan);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1, this.ExcludeDurationTimeSpan);
            resultList[1].Should().BeEquivalentTo(authFlowResult2, this.ExcludeDurationTimeSpan);
            resultList[2].Should().BeEquivalentTo(authFlowResult3, this.ExcludeDurationTimeSpan);
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1, this.ExcludeDurationTimeSpan);
            resultList[1].Should().BeEquivalentTo(authFlowResult2, this.ExcludeDurationTimeSpan);
        }

        [Test]
        public async Task HasMultipleAuthFlows_Returns_Early_With_TimeoutException()
        {
            var timeoutError = new[]
            {
                new TimeoutException("Timeout exception"),
            };

            var errors1 = new[]
            {
                new Exception("Exception 1"),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(null, timeoutError, "authFlow2");

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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object }, this.timeoutManager);
            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Count.Should().Be(2);
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(authFlowResult1, this.ExcludeDurationTimeSpan);
            resultList[1].Should().BeEquivalentTo(authFlowResult2, this.ExcludeDurationTimeSpan);
        }

        private EquivalencyAssertionOptions<AuthFlowResult> ExcludeDurationTimeSpan(EquivalencyAssertionOptions<AuthFlowResult> options)
        {
            options.Excluding(result => result.Duration);
            return options;
        }

        private AuthFlowExecutor Subject(IEnumerable<IAuthFlow> authFlows, ITimeoutManager timeoutManager)
        {
            var logger = this.serviceProvider.GetService<ILogger<AuthFlowExecutor>>();
            return new AuthFlowExecutor(logger, authFlows, timeoutManager);
        }
    }
}
