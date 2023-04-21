// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Identity.Client;

    using Moq;

    using NUnit.Framework;

    internal class IntegratedWindowsAuthenticationTest : AuthFlowTestBase
    {
        public AuthFlow.IntegratedWindowsAuthentication Subject() => new AuthFlow.IntegratedWindowsAuthentication(this.logger, ClientId, TenantId, Scopes, pcaWrapper: this.pca.Object);

        [Test]
        public async Task IWA_Success()
        {
            this.SetupIWAReturnsResult();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.testToken);
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("iwa");
        }

        [Test]
        public void General_Exceptions_Are_ReThrown()
        {
            var message = "Something somwhere has gone terribly wrong!";
            this.pca
                .Setup(pca => pca.GetTokenIntegratedWindowsAuthenticationAsync(Scopes, It.IsAny<CancellationToken>()))
                .Throws(new Exception(message));

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            Func<Task> subject = async () => await iwa.GetTokenAsync();

            // Assert
            subject.Should().ThrowExactlyAsync<Exception>().WithMessage(message);
        }

        [Test]
        public async Task IWA_ReturnsNull()
        {
            this.SetupIWAReturnsNull();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("iwa");
        }

        [Test]
        public async Task GetTokenIWA_MsalUIRequired_2FA()
        {
            this.SetupIWAUIRequiredFor2FA();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            authFlowResult.Errors[0].Message.Should().Be("AADSTS50076 MSAL UI Required Exception!");
            authFlowResult.AuthFlowName.Should().Be("iwa");
        }

        [Test]
        public async Task GetTokenIWA_MsalServiceException()
        {
            this.IWAServiceException();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalServiceException));
            authFlowResult.AuthFlowName.Should().Be("iwa");
        }

        [Test]
        public async Task GetTokenIWA_MsalClientException()
        {
            this.IWAClientException();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalClientException));
            authFlowResult.AuthFlowName.Should().Be("iwa");
        }

        private void SetupIWAReturnsResult()
        {
            this.pca
               .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(Scopes, It.IsAny<CancellationToken>()))
               .ReturnsAsync(this.testToken);
        }

        private void SetupIWAReturnsNull()
        {
            this.pca
               .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(Scopes, It.IsAny<CancellationToken>()))
               .ReturnsAsync((TokenResult)null);
        }

        private void SetupIWAUIRequiredFor2FA()
        {
            this.pca
                .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(Scopes, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "AADSTS50076 MSAL UI Required Exception!"));
        }

        private void IWAServiceException()
        {
            this.pca
                .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(Scopes, It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalExceptionErrorCode, MsalExceptionMessage));
        }

        private void IWAClientException()
        {
            this.pca
                .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(Scopes, It.IsAny<CancellationToken>()))
                .Throws(new MsalClientException("1", "Could not find a WAM account for the silent request."));
        }
    }
}
