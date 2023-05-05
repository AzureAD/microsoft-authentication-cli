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

    internal class DeviceCodeTest : AuthFlowTestBase
    {
        public AuthFlow.DeviceCode Subject() => new AuthFlow.DeviceCode(this.logger, this.authParameters, pcaWrapper: this.mockPca.Object, promptHint: PromptHint);

        [Test]
        public async Task Success()
        {
            this.SetupDeviceCodeSuccess();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(this.testToken);
            authFlowResult.TokenResult.IsSilent.Should().BeFalse();
            authFlowResult.Errors.Should().BeEmpty();
            authFlowResult.AuthFlowName.Should().Be("devicecode");
        }

        [Test]
        public async Task Returns_Null()
        {
            this.SetupDeviceCodeReturnsNull();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(0);
            authFlowResult.AuthFlowName.Should().Be("devicecode");
        }

        [Test]
        public async Task Throws_MsalException()
        {
            this.SetupDeviceCodeThrowsMsalException();

            // Act
            AuthFlow.DeviceCode deviceCode = this.Subject();
            var authFlowResult = await deviceCode.GetTokenAsync();

            // Assert
            authFlowResult.TokenResult.Should().Be(null);
            authFlowResult.Errors.Should().HaveCount(1);
            authFlowResult.Errors[0].Should().BeOfType(typeof(MsalException));
            authFlowResult.AuthFlowName.Should().Be("devicecode");
        }

        private void SetupDeviceCodeSuccess()
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenDeviceCodeAsync(Scopes, It.IsAny<Func<DeviceCodeResult, Task>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.testToken);
        }

        private void SetupDeviceCodeReturnsNull()
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenDeviceCodeAsync(Scopes, It.IsAny<Func<DeviceCodeResult, Task>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TokenResult)null);
        }

        private void SetupDeviceCodeThrowsMsalException()
        {
            this.mockPca
                .Setup((pca) => pca.GetTokenDeviceCodeAsync(Scopes, It.IsAny<Func<DeviceCodeResult, Task>>(), It.IsAny<CancellationToken>()))
                .Throws(new MsalException("1", "Msal Exception."));
        }
    }
}
