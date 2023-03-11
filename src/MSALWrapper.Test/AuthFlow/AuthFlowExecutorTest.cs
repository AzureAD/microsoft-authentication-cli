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

    using NLog;
    using NLog.Extensions.Logging;
    using NLog.Targets;

    using NUnit.Framework;

    public class AuthFlowExecutorTest
    {
        private const string NullAuthFlowResultExceptionMessage = "Auth flow 'IAuthFlowProxy' returned a null AuthFlowResult.";

        private MemoryTarget logTarget;
        private ServiceProvider serviceProvider;
        private Extensions.Logging.ILogger logger;
        private TokenResult tokenResult;
        private IStopwatch stopwatch;

        [SetUp]
        public void Setup()
        {
            // Setup in memory logging target with NLog - allows making assertions against what has been logged.
            var loggingConfig = new NLog.Config.LoggingConfiguration();
            this.logTarget = new MemoryTarget("memory_target");
            this.logTarget.Layout = "${message}";
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
             .BuildServiceProvider();

            this.logger = this.serviceProvider.GetService<ILogger<AuthFlowExecutorTest>>();

            // Mock successful token result
            this.tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken), Guid.NewGuid());
            this.stopwatch = new StopwatchTracker(TimeSpan.FromSeconds(60));
        }

        [Test]
        public void GetToken_AllNullArgs()
        {
            Action authFlowExecutor = () => AuthFlowExecutor.GetToken(null, null, null, null);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        public void GetToken_Null_AuthFlows()
        {
            Action authFlowExecutor = () => AuthFlowExecutor.GetToken(this.logger, null, null, null);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'authFlows')");
        }

        [Test]
        public void GetToken_Null_StopWatch()
        {
            Action authFlowExecutor = () => AuthFlowExecutor.GetToken(this.logger, new List<IAuthFlow>(), null, null);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'stopWatch')");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void GetToken_Bad_LockName(string lockName)
        {
            Action authFlowExecutor = () => AuthFlowExecutor.GetToken(this.logger, new List<IAuthFlow>(), this.stopwatch, lockName);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentException>().WithMessage("Parameter 'lockName' cannot be null, empty, or whitespace");
        }

        [Test]
        public void GetToken_No_AuthFlows()
        {
            var expected = new AuthFlowExecutor.Result();

            var subject = this.Subject(new List<IAuthFlow>());

            subject.Should().BeEquivalentTo(expected);
            this.logTarget.Logs.Should().StartWith("Warning: There are 0 auth flows to execute!");
        }

        [Test]
        public void Single_AuthFlow_Returns_TokenResult()
        {
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            var authFlowName = "authFlowName1";
            var authFlowResult = new AuthFlowResult(this.tokenResult, null, authFlowName);
            var attempts = new List<AuthFlowResult> { authFlowResult };
            var expected = new AuthFlowExecutor.Result() { Attempts = attempts };

            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult);

            // Act
            var result = this.Subject(new[] { authFlow1.Object });

            // Assert
            authFlow1.VerifyAll();
            result.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void Single_AuthFlow_Returns_Null_TokenResult()
        {
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            var authFlowName = "authFlowName1";
            var authFlowResult = new AuthFlowResult(null, null, authFlowName);
            var attempts = new List<AuthFlowResult>() { authFlowResult };
            var expected = new AuthFlowExecutor.Result() { Attempts = attempts };

            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult);

            // Act
            var result = this.Subject(new[] { authFlow1.Object });

            // Assert
            authFlow1.VerifyAll();
            result.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void Single_AuthFlow_Returns_Null_TokenResult_With_Errors()
        {
            var errors1 = new[]
            {
                new Exception("Exception 1."),
            };
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            var authFlowName = "authFlowName1";
            var authFlowResult = new AuthFlowResult(null, errors1, authFlowName);
            var attempts = new List<AuthFlowResult>() { authFlowResult };
            var expected = new AuthFlowExecutor.Result() { Attempts = attempts };

            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult);

            // Act
            var result = this.Subject(new[] { authFlow1.Object });

            // Assert
            authFlow1.VerifyAll();
            result.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void Single_AuthFlow_Returns_Null_AuthFlowResult()
        {
            var errors1 = new[]
            {
                new NullTokenResultException(NullAuthFlowResultExceptionMessage),
            };
            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            var authFlowResult = new AuthFlowResult(null, errors1, "IAuthFlowProxy");
            var attempts = new List<AuthFlowResult>() { authFlowResult };

            // We Don't expect this case to actually happen in practice, because we make sure
            // all our authFlows return AuthFlowResults instead of null, for failure cases.
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync((AuthFlowResult)null);

            // Act
            var result = this.Subject(new[] { authFlow1.Object });

            // Assert - here we do not use an expected EquivalentTo assertion because the AuthFlowExecutor
            // creates a new AuthFlowResult when our mock auth flow gives back a null. That new value has a Duration set on it,
            // that is not constant and we can't create and reliable test that includes that duration property. We instead assert these other things.
            authFlow1.VerifyAll();
            result.Success.Should().BeNull();
            result.Attempts[0].Errors.Should().BeEquivalentTo(errors1);
        }

        [Test]
        public void Two_AuthFlows_First_Returns_Null_TokenResult_Second_Succeeds()
        {
            var authFlowResult1 = new AuthFlowResult(null, null, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(this.tokenResult, null, "authFlow2");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var attempts = new List<AuthFlowResult>() { authFlowResult1, authFlowResult2 };
            var expected = new AuthFlowExecutor.Result() { Attempts = attempts };

            // Act
            var result = this.Subject(new[] { authFlow1.Object, authFlow2.Object });

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            result.Should().BeEquivalentTo(expected, opts => opts.WithStrictOrdering());
        }

        [Test]
        public void Two_AuthFlows_First_Returns_Null_TokenResult_With_Errors_Second_Succeeds()
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

            var attempts = new List<AuthFlowResult>() { authFlowResult1, authFlowResult2 };
            var expected = new AuthFlowExecutor.Result() { Attempts = attempts };

            // Act
            var result = this.Subject(new[] { authFlow1.Object, authFlow2.Object });

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            result.Should().BeEquivalentTo(expected, opts => opts.WithStrictOrdering());
        }

        [Test]
        public void Two_AuthFlows_Both_Return_Null_TokenResult_With_Errors()
        {
            var errors1 = new[]
            {
                new Exception("mfa required"),
            };

            var errors2 = new[]
            {
                new Exception("Exception 1"),
                new Exception("Exception 2"),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(null, errors2, "authFlow2");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var attempts = new List<AuthFlowResult>() { authFlowResult1, authFlowResult2 };
            var expected = new AuthFlowExecutor.Result() { Attempts = attempts };

            // Act
            var result = this.Subject(new[] { authFlow1.Object, authFlow2.Object });

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            result.Should().BeEquivalentTo(expected, opts => opts.WithStrictOrdering());
        }

        [Test]
        public void Two_AuthFlows_First_Fails_Second_Succeeds_Both_With_Errors()
        {
            var errors1 = new[]
            {
                new Exception("mfa required"),
            };

            var errors2 = new[]
            {
                new Exception("Exception 1"),
                new Exception("Exception 2"),
            };

            var authFlowResult1 = new AuthFlowResult(null, errors1, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(this.tokenResult, errors2, "authFlow2");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var attempts = new List<AuthFlowResult>() { authFlowResult1, authFlowResult2 };
            var expected = new AuthFlowExecutor.Result() { Attempts = attempts };

            // Act
            var result = this.Subject(new[] { authFlow1.Object, authFlow2.Object });

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            result.Should().BeEquivalentTo(expected, opts => opts.WithStrictOrdering());
        }

        [Test]
        public void Three_AuthFlows_First_Two_Return_Null_TokenResult_Third_Succeeds()
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

            var attempts = new List<AuthFlowResult>()
            {
                authFlowResult1,
                authFlowResult2,
                authFlowResult3,
            };
            var expected = new AuthFlowExecutor.Result() { Attempts = attempts };

            // Act
            var result = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            result.Should().BeEquivalentTo(expected, opts => opts.WithStrictOrdering());
        }

        [Test]
        public void Three_AuthFlows_Returns_Early_On_Second_Success()
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

            var authFlowResult1 = new AuthFlowResult(null, errors1, "authFlow1");
            var authFlowResult2 = new AuthFlowResult(this.tokenResult, errors2, "authFlow2");

            var authFlow1 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow1.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult1);

            var authFlow2 = new Mock<IAuthFlow>(MockBehavior.Strict);
            authFlow2.Setup(p => p.GetTokenAsync()).ReturnsAsync(authFlowResult2);

            var authFlow3 = new Mock<IAuthFlow>(MockBehavior.Strict);

            var attempts = new List<AuthFlowResult>() { authFlowResult1, authFlowResult2 };
            var expected = new AuthFlowExecutor.Result() { Attempts = attempts };

            // Act
            var result = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });

            // Assert
            authFlow1.VerifyAll();
            authFlow2.VerifyAll();
            authFlow3.VerifyAll();
            result.Should().BeEquivalentTo(expected, opts => opts.WithStrictOrdering());
        }

        [Test]
        public void Timeout_Kills_Current_AuthFlow_Returns_TimeoutException()
        {
            var stopwatch = new Mock<IStopwatch>(MockBehavior.Strict);
            this.stopwatch = stopwatch.Object;

            var timeAfterwarningLength = AuthFlowExecutor.WarningDelay + TimeSpan.FromSeconds(1);
            var remainingTimeForWarningMessage = TimeSpan.FromSeconds(10);

            stopwatch.Setup(tm => tm.Start());
            stopwatch.Setup(tm => tm.TimedOut()).Returns(true);
            stopwatch.Setup(tm => tm.Stop());

            var alwaysTimesOutAuthFlow = new DelayAuthFlow(TimeSpan.FromSeconds(100));

            // This auth flow has no setups, because they should never be used.
            var neverUsedAuthFlow = new Mock<IAuthFlow>(MockBehavior.Strict);

            var timeoutError = new[]
            {
                new TimeoutException("Global timeout hit during DelayAuthFlow"),
            };
            var authFlowResult = new AuthFlowResult(null, timeoutError, "DelayAuthFlow");
            var attempts = new List<AuthFlowResult>() { authFlowResult };

            // Act
            var result = this.Subject(new[] { alwaysTimesOutAuthFlow, neverUsedAuthFlow.Object });

            // Assert
            stopwatch.VerifyAll();
            neverUsedAuthFlow.VerifyAll();
            result.Success.Should().BeNull();
            result.Attempts.Should().BeEquivalentTo(attempts, this.ExcludeDurationTimeSpan);
        }

        [Test]
        public void MultipleAuthFlows_Returns_Early_When_TimedOut()
        {
            var stopwatch = new Mock<IStopwatch>(MockBehavior.Strict);
            this.stopwatch = stopwatch.Object;

            var timeAfterwarningLength = AuthFlowExecutor.WarningDelay + TimeSpan.FromSeconds(1);
            var remainingTimeForWarningMessage = TimeSpan.FromSeconds(10);

            stopwatch.Setup(tm => tm.Start());
            stopwatch.Setup(tm => tm.Elapsed()).Returns(timeAfterwarningLength);
            stopwatch.Setup(tm => tm.Remaining()).Returns(remainingTimeForWarningMessage);
            stopwatch.SetupSequence(tm => tm.TimedOut()).Returns(false).Returns(true);
            stopwatch.Setup(tm => tm.Stop());

            var waitAndFailAuthFlow = new DelayAuthFlow(TimeSpan.FromSeconds(1));
            var alwaysTimesOutAuthFlow = new DelayAuthFlow(TimeSpan.FromSeconds(100));

            // This auth flow has no setups, because they should never be used.
            var neverUsedAuthFlow = new Mock<IAuthFlow>(MockBehavior.Strict);

            var authFlowError = new[]
            {
                new Exception("Exception 1"),
            };
            var timeoutError = new[]
            {
                new TimeoutException("Global timeout hit during DelayAuthFlow"),
            };
            var waitAndFailResult = new AuthFlowResult(null, authFlowError, "DelayAuthFlow");
            var alwaysTimesOutResult = new AuthFlowResult(null, timeoutError, "DelayAuthFlow");
            var attempts = new List<AuthFlowResult>() { waitAndFailResult, alwaysTimesOutResult };

            // Act
            var result = this.Subject(new[] { waitAndFailAuthFlow, alwaysTimesOutAuthFlow, neverUsedAuthFlow.Object });

            // Assert
            stopwatch.VerifyAll();
            neverUsedAuthFlow.VerifyAll();
            result.Success.Should().BeNull();
            result.Attempts.Should().BeEquivalentTo(attempts, this.ExcludeDurationTimeSpan);
        }

        private EquivalencyAssertionOptions<AuthFlowResult> ExcludeDurationTimeSpan(EquivalencyAssertionOptions<AuthFlowResult> options)
        {
            options.Excluding(result => result.Duration);
            options.WithStrictOrdering();
            return options;
        }

        private AuthFlowExecutor.Result Subject(IEnumerable<IAuthFlow> authFlows)
        {
            return AuthFlowExecutor.GetToken(this.logger, authFlows, this.stopwatch, "Local\\authflow_executor_tests");
        }

        // This auth flow is for delaying the return and testing timeout.
        private class DelayAuthFlow : IAuthFlow
        {
            private readonly TimeSpan delay;

            public DelayAuthFlow(TimeSpan delay)
            {
                this.delay = delay;
            }

            public async Task<AuthFlowResult> GetTokenAsync()
            {
                var errors = new[]
                {
                    new Exception("Exception 1"),
                };
                var authFlowResult = new AuthFlowResult(null, errors, "DelayAuthFlow");
                await Task.Delay(this.delay);
                return authFlowResult;
            }
        }
    }
}
