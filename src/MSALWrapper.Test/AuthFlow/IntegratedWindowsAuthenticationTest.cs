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

    public class IntegratedWindowsAuthenticationTest
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
        private Mock<IPCAWrapper> pcaWrapperMock;
        private Mock<IAccount> testAccount;
        private IEnumerable<string> scopes = new string[] { $"{ResourceId}/.default" };
        private TokenResult tokenResult;

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
             .AddTransient<AuthFlow.IntegratedWindowsAuthentication>((provider) =>
             {
                 var logger = provider.GetService<ILogger<AuthFlow.IntegratedWindowsAuthentication>>();
                 return new AuthFlow.IntegratedWindowsAuthentication(logger, ClientId, TenantId, this.scopes, pcaWrapper: this.pcaWrapperMock.Object);
             })
             .BuildServiceProvider();

            // Mock successful token result
            this.tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken), Guid.NewGuid());
        }

        public AuthFlow.IntegratedWindowsAuthentication Subject() => this.serviceProvider.GetService<AuthFlow.IntegratedWindowsAuthentication>();

        [Test]
        public async Task CachedAuthSuccess()
        {
            this.MockAccount();
            this.CachedAuthResult();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.TokenResult.IsSilent.Should().BeTrue();
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("IntegratedWindowsAuthentication");
        }

        [Test]
        public async Task GetCachedToken_ReturnsNull()
        {
            this.MockAccount();
            this.CachedAuthReturnsNull();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            IAccount account = await iwa.GetCachedAccountAsync();
            var authFlowResult = await iwa.GetTokenSilentAsync(account);

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.AuthFlowName.Should().Be("IntegratedWindowsAuthentication");
        }

        [Test]
        public void General_Exceptions_Are_ReThrown()
        {
            var message = "Something somwhere has gone terribly wrong!";
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new Exception(message));

            this.MockAccount();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            Func<Task> subject = async () => await iwa.GetTokenAsync();

            // Assert
            subject.Should().ThrowExactlyAsync<Exception>().WithMessage(message);

            this.pcaWrapperMock.VerifyAll();
        }

        [Test]
        public async Task CachedAuth_Throws_ServiceException()
        {
            this.MockAccount();
            this.CachedAuthServiceException();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalServiceException));
            authFlowResult.AuthFlowName.Should().Be("IntegratedWindowsAuthentication");
        }

        [Test]
        public async Task GetTokenSilent_OperationCanceledException()
        {
            this.MockAccount();
            this.CachedAuthTimeout();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            IAccount account = await iwa.GetCachedAccountAsync();
            var authFlowResult = await iwa.GetTokenSilentAsync(account);

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(2);
            authFlowResult.Errors[0].Should().BeOfType(typeof(AuthenticationTimeoutException));
            authFlowResult.Errors[0].Message.Should().Be("Get Token Silent timed out after 00:00:30");
            authFlowResult.Errors[1].Should().BeOfType(typeof(NullTokenResultException));
            authFlowResult.AuthFlowName.Should().Be("IntegratedWindowsAuthentication");
        }

        [Test]
        public async Task GetTokenSilent_MsalClientException()
        {
            this.MockAccount();
            this.CachedAuthClientException();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalClientException));
            authFlowResult.AuthFlowName.Should().Be("IntegratedWindowsAuthentication");
        }

        [Test]
        public async Task GetTokenSilent_NullReferenceException()
        {
            this.MockAccount();
            this.CachedAuthNullReferenceException();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(NullReferenceException));
            authFlowResult.AuthFlowName.Should().Be("IntegratedWindowsAuthentication");
        }

        [Test]
        public async Task NoCachedAccounts_IWASuccess()
        {
            this.MockAccountReturnsNull();
            this.IWAReturnsResult();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.TokenResult.IsSilent.Should().BeTrue();
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("IntegratedWindowsAuthentication");
        }

        [Test]
        public async Task GetTokenIWA_ReturnsNull()
        {
            this.MockAccountReturnsNull();
            this.IWAReturnsNull();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task GetTokenIWA_MsalUIRequired_2FA()
        {
            this.MockAccountReturnsNull();
            this.IWAUIRequiredFor2FA();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[0].Message.Should().Be("AADSTS50076 MSAL UI Required Exception!");
            authFlowResult.AuthFlowName.Should().Be("IntegratedWindowsAuthentication");
        }

        [Test]
        public async Task GetTokenIWA_GenericMsalUIRequired()
        {
            this.MockAccountReturnsNull();
            this.IWAGenericUIRequiredException();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.AuthFlowName.Should().Be("IntegratedWindowsAuthentication");
        }

        [Test]
        public async Task GetTokenIWA_MsalServiceException()
        {
            this.MockAccountReturnsNull();
            this.IWAServiceException();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert - this method should not throw for known types of excpeptions, instead return null, so
            // our caller can retry auth another way.
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalServiceException));
            authFlowResult.AuthFlowName.Should().Be("IntegratedWindowsAuthentication");
        }

        [Test]
        public async Task GetTokenIWA_MsalClientException()
        {
            this.MockAccountReturnsNull();
            this.IWAClientException();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            this.pcaWrapperMock.VerifyAll();
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalClientException));
            authFlowResult.AuthFlowName.Should().Be("IntegratedWindowsAuthentication");
        }

        private void CachedAuthResult()
        {
            this.pcaWrapperMock
               .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
               .ReturnsAsync(this.tokenResult);
        }

        private void CachedAuthReturnsNull()
        {
            this.pcaWrapperMock
               .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
               .ReturnsAsync((TokenResult)null);
        }

        private void CachedAuthUIRequired()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, null, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "UI is required"));
        }

        private void CachedAuthServiceException()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalServiceExceptionErrorCode, MsalServiceExceptionMessage));
        }

        private void CachedAuthTimeout()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        private void CachedAuthClientException()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new MsalClientException("1", "Could not find a WAM account for the silent request."));
        }

        private void CachedAuthNullReferenceException()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, this.testAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new NullReferenceException("There was a null reference excpetion. This should absolutly never happen and if it does it is a bug."));
        }

        private void IWAReturnsResult()
        {
            this.pcaWrapperMock
               .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
               .ReturnsAsync(this.tokenResult);
        }

        private void IWAReturnsNull()
        {
            this.pcaWrapperMock
               .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
               .ReturnsAsync((TokenResult)null);
        }

        private void CachedAuthUIRequiredNoAccount()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenSilentAsync(this.scopes, null, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "No account hint given!"));
        }

        private void IWAUIRequiredFor2FA()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "AADSTS50076 MSAL UI Required Exception!"));
        }

        private void IWAGenericUIRequiredException()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("2", "MSAL UI Required Exception!"));
        }

        private void IWAServiceException()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalServiceExceptionErrorCode, MsalServiceExceptionMessage));
        }

        private void IWAClientException()
        {
            this.pcaWrapperMock
                .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
                .Throws(new MsalClientException("1", "Could not find a WAM account for the silent request."));
        }

        private void MockAccount()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync(this.testAccount.Object);
        }

        private void MockAccountReturnsNull()
        {
            this.pcaWrapperMock
                .Setup(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync((IAccount)null);
        }
    }
}
