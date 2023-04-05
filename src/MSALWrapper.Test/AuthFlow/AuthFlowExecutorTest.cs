// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using FluentAssertions;
    using FluentAssertions.Equivalency;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.IdentityModel.JsonWebTokens;

    using Moq;

    using NUnit.Framework;

    public class AuthFlowExecutorTest
    {
        private const string NullAuthFlowResultExceptionMessage = "Auth flow 'IAuthFlowProxy' returned a null AuthFlowResult.";

        private ILogger logger;
        private TokenResult tokenResult;
        private IStopwatch stopwatch;
        private Guid client = Guid.NewGuid();
        private Guid tenant = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            (this.logger, _) = MemoryLogger.Create();

            // Mock successful token result
            this.tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken), Guid.NewGuid());
            this.stopwatch = new StopwatchTracker(TimeSpan.FromSeconds(60));
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
            Action authFlowExecutor = () => new AuthFlowExecutor(null, null, this.stopwatch);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'logger')");
        }

        [Test]
        public void ConstructorWith_Null_AuthFlows()
        {
            Action authFlowExecutor = () => new AuthFlowExecutor(this.logger, null, this.stopwatch);

            // Assert
            authFlowExecutor.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ConstructorWith_Valid_Arguments()
        {
            Action authFlowExecutor = () => this.Subject(new List<IAuthFlow>());

            // Assert
            authFlowExecutor.Should().NotThrow();
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
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
            var authFlowExecutor = this.Subject(new[] { authFlow1.Object, authFlow2.Object, authFlow3.Object });
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
        public async Task Timeout_Kills_Current_AuthFlow_Returns_TimeoutException()
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
            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(authFlowResult);

            // Act
            var authFlowExecutor = this.Subject(new[] { alwaysTimesOutAuthFlow, neverUsedAuthFlow.Object });

            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            stopwatch.VerifyAll();
            neverUsedAuthFlow.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Count.Should().Be(1);
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);
            resultList[0].Should().BeEquivalentTo(authFlowResult, this.ExcludeDurationTimeSpan);
        }

        [Test]
        public async Task MultipleAuthFlows_Returns_Early_When_TimedOut()
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
            var authFlowResultList = new List<AuthFlowResult>();
            authFlowResultList.Add(waitAndFailResult);
            authFlowResultList.Add(alwaysTimesOutResult);

            // Act
            var authFlowExecutor = this.Subject(new[] { waitAndFailAuthFlow, alwaysTimesOutAuthFlow, neverUsedAuthFlow.Object });

            var result = await authFlowExecutor.GetTokenAsync();
            var resultList = result.ToList();

            // Assert
            stopwatch.VerifyAll();
            neverUsedAuthFlow.VerifyAll();
            resultList.Should().NotBeNull();
            resultList.Count.Should().Be(2);
            resultList.Should().BeEquivalentTo(authFlowResultList, this.ExcludeDurationTimeSpan);

            // Assert Order of results.
            resultList[0].Should().BeEquivalentTo(waitAndFailResult, this.ExcludeDurationTimeSpan);
            resultList[1].Should().BeEquivalentTo(alwaysTimesOutResult, this.ExcludeDurationTimeSpan);
        }

        private EquivalencyAssertionOptions<AuthFlowResult> ExcludeDurationTimeSpan(EquivalencyAssertionOptions<AuthFlowResult> options)
        {
            options.Excluding(result => result.Duration);
            return options;
        }

        private AuthFlowExecutor Subject(IEnumerable<IAuthFlow> authFlows)
        {
            return new AuthFlowExecutor(this.logger, authFlows, this.stopwatch);
        }

        // This auth flow is for delaying the return and testing timeout.
        private class DelayAuthFlow : IAuthFlow
        {
            private readonly TimeSpan delay;

            public DelayAuthFlow(TimeSpan delay)
            {
                this.delay = delay;
            }

            public string Name() => "delayed_test_auth_flow";

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
