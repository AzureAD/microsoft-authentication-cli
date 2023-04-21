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
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;

    using Moq;

    using NUnit.Framework;

    internal class IntegratedWindowsAuthenticationTest : AuthFlowTestBase
    {
        private const string MsalExceptionErrorCode = "1";
        private const string MsalExceptionMessage = "MSAL Exception";

        // These Guids were randomly generated and do not refer to a real resources
        private static readonly Guid ResourceId = new Guid("6e979987-a7c8-4604-9b37-e51f06f08f1a");
        private static readonly Guid Correlationid = new Guid("8254f6f7-a09f-4752-8bd6-391adc3b912f");

        private readonly TokenResult tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken), Correlationid);
        private readonly IEnumerable<string> scopes = new string[] { $"{ResourceId}/.default" };

        public AuthFlow.IntegratedWindowsAuthentication Subject() => new AuthFlow.IntegratedWindowsAuthentication(this.logger, ClientId, TenantId, this.scopes, pcaWrapper: this.mockPca.Object);

        [Test]
        public async Task IWA_Success()
        {
            this.SetupIWAReturnsResult();

            // Act
            AuthFlow.IntegratedWindowsAuthentication iwa = this.Subject();
            var authFlowResult = await iwa.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.tokenResult);
            authFlowResult.TokenResult.IsSilent.Should().BeTrue();
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("iwa");
        }

        [Test]
        public void General_Exceptions_Are_ReThrown()
        {
            var message = "Something somwhere has gone terribly wrong!";
            this.mockPca
                .Setup(pca => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
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
            this.mockPca
               .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
               .ReturnsAsync(this.tokenResult);
        }

        private void SetupIWAReturnsNull()
        {
            this.mockPca
               .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
               .ReturnsAsync((TokenResult)null);
        }

        private void SetupIWAUIRequiredFor2FA()
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("1", "AADSTS50076 MSAL UI Required Exception!"));
        }

        private void SetupIWAGenericUIRequiredException()
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
                .Throws(new MsalUiRequiredException("2", "MSAL UI Required Exception!"));
        }

        private void IWAServiceException()
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
                .Throws(new MsalServiceException(MsalExceptionErrorCode, MsalExceptionMessage));
        }

        private void IWAClientException()
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenIntegratedWindowsAuthenticationAsync(this.scopes, It.IsAny<CancellationToken>()))
                .Throws(new MsalClientException("1", "Could not find a WAM account for the silent request."));
        }
    }
}
