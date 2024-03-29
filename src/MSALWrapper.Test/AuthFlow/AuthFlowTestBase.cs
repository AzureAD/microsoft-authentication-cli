// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;

    using Moq;

    using NLog.Targets;

    using NUnit.Framework;

    internal class AuthFlowTestBase
    {
        protected const string TestUsername = "John Doe";
        protected const string PromptHint = "test prompt hint";
        protected const string MsalExceptionErrorCode = "1";
        protected const string MsalExceptionMessage = "MSAL Exception";
        protected const string GeneralExceptionMessage = "General Exception";
        protected const string Claims = "claims";

        // These Guids were randomly generated and do not refer to a real resources
        protected static readonly Guid ResourceId = new Guid("6e979987-a7c8-4604-9b37-e51f06f08f1a");
        protected static readonly IEnumerable<string> Scopes = new string[] { $"{ResourceId}/.default" };
        protected static readonly Guid ClientId = new Guid("5af6def2-05ec-4cab-b9aa-323d75b5df40");
        protected static readonly Guid TenantId = new Guid("8254f6f7-a09f-4752-8bd6-391adc3b912e");
        protected static readonly Guid CorrelationId = new Guid("8254f6f7-a09f-4752-8bd6-391adc3b912f");

        protected Mock<IPCAWrapper> mockPca;
        protected Mock<IAccount> mockAccount;
        protected ILogger logger;
        protected MemoryTarget logTarget;
        protected TokenResult testToken;
        protected AuthParameters authParameters;

        [SetUp]
        public void Setup()
        {
            (this.logger, this.logTarget) = MemoryLogger.Create();
            this.mockPca = new Mock<IPCAWrapper>(MockBehavior.Strict);
            this.mockAccount = new Mock<IAccount>(MockBehavior.Strict);
            this.testToken = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken), CorrelationId);
            this.authParameters = new AuthParameters(ClientId, TenantId, Scopes);
        }

        [TearDown]
        public void TearDown()
        {
            this.mockPca.VerifyAll();
            this.mockAccount.VerifyAll();
        }

        protected virtual void SetupCachedAccount(bool exists = true)
        {
            this.mockPca
                .Setup(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync(exists ? this.mockAccount.Object : null);
        }

        protected virtual void SetupAccountUsername()
        {
            this.mockAccount.Setup(a => a.Username).Returns(TestUsername);
        }

        protected virtual void SetupWithPromptHint()
        {
            this.mockPca
                .Setup(pca => pca.WithPromptHint(PromptHint))
                .Returns((string s) => this.mockPca.Object);
        }

        protected virtual void SetupGetTokenSilentSuccess()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenSilentAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.testToken);
        }

        protected virtual void SetupGetTokenSilentReturnsNull()
        {
            this.mockPca
               .Setup((pca) => pca.GetTokenSilentAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
               .ReturnsAsync((TokenResult)null);
        }

        protected virtual void SetupGetTokenSilentMsalUiRequiredException()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenSilentAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MsalUiRequiredException(MsalExceptionErrorCode, "2fa is required", new Exception("inner 2fa exception"), UiRequiredExceptionClassification.AcquireTokenSilentFailed));
        }

        protected virtual void SetupGetTokenSilentTimeout()
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenSilentAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }

        protected virtual void SetupGetTokenInteractiveSuccess(bool withAccount)
        {
            this.mockPca
               .Setup((pca) => pca.GetTokenInteractiveAsync(Scopes, withAccount ? this.mockAccount.Object : null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(this.testToken);
        }

        protected virtual void SetupGetTokenInteractiveReturnsNull(bool withAccount)
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, withAccount ? this.mockAccount.Object : null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TokenResult)null);
        }

        protected virtual void SetupGetTokenInteractiveMsalUiRequiredException(IAccount account)
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, account, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MsalUiRequiredException(MsalExceptionErrorCode, "UI Required Exception"));
        }

        protected virtual void SetupGetTokenInteractiveMsalServiceException(bool withAccount)
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, withAccount ? this.mockAccount.Object : null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MsalServiceException("1", "MSAL Service Exception"));
        }

        protected virtual void SetupGetTokenInteractiveTimeout(bool withAccount)
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, withAccount ? this.mockAccount.Object : null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());
        }

        protected virtual void SetupGetTokenInteractiveGeneralException(bool withAccount)
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenInteractiveAsync(Scopes, withAccount ? this.mockAccount.Object : null, It.IsAny<CancellationToken>()))
                .Throws(new Exception(GeneralExceptionMessage));
        }

        protected virtual void SetupGetTokenInteractiveWithClaimsSuccess()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.testToken);
        }

        protected virtual void SetupGetTokenInteractiveWithClaimsReturnsNull()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TokenResult)null);
        }

        protected virtual void SetupGetTokenInteractiveWithClaimsThrowsServiceException()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MsalServiceException(MsalExceptionErrorCode, MsalExceptionMessage));
        }

        protected virtual void SetupGetTokenInteractiveWithClaimsTimeout()
        {
            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException());
        }
    }
}
