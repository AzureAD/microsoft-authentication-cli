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
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;

    using Moq;

    using NLog.Targets;

    using NUnit.Framework;

    /// <summary>
    /// Tests for macOS-specific broker authentication behavior.
    /// Uses a mock IPlatformUtils that reports IsMacOS()=true to simulate macOS.
    /// </summary>
    internal class BrokerMacOSTest : AuthFlowTestBase
    {
        private Mock<IPlatformUtils> mockPlatformUtils;

        [SetUp]
        public new void Setup()
        {
            this.mockPlatformUtils = new Mock<IPlatformUtils>(MockBehavior.Strict);
            this.mockPlatformUtils.Setup(p => p.IsMacOS()).Returns(true);
        }

        public AuthFlow.Broker Subject() => new AuthFlow.Broker(
            this.logger,
            this.authParameters,
            pcaWrapper: this.mockPca.Object,
            promptHint: PromptHint,
            platformUtils: this.mockPlatformUtils.Object);

        [Test]
        public async Task MacOS_GetTokenSilent_WithCachedAccount_Success()
        {
            // Cached account exists in the MSAL cache — silent auth succeeds.
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentSuccess();

            AuthFlowResult result = await this.Subject().GetTokenAsync();

            result.TokenResult.Should().Be(this.testToken);
            result.TokenResult.IsSilent.Should().BeTrue();
            result.Errors.Should().BeEmpty();
            result.AuthFlowName.Should().Be("broker");
        }

        [Test]
        public async Task MacOS_NoCachedAccount_Interactive_Success()
        {
            // No cached account — falls through to interactive auth.
            this.mockPca
                .Setup(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync((IAccount)null);

            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveSuccess(withAccount: false);

            AuthFlowResult result = await this.Subject().GetTokenAsync();

            result.TokenResult.Should().Be(this.testToken);
            result.TokenResult.IsSilent.Should().BeFalse();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public async Task MacOS_GeneralException_Windows_Would_Rethrow()
        {
            // Verify that on a "Windows" platform mock, general exceptions ARE rethrown.
            // This is the control test proving the macOS exception filter is meaningful.
            var windowsPlatform = new Mock<IPlatformUtils>(MockBehavior.Strict);
            windowsPlatform.Setup(p => p.IsMacOS()).Returns(false);

            var broker = new AuthFlow.Broker(
                this.logger,
                this.authParameters,
                pcaWrapper: this.mockPca.Object,
                promptHint: PromptHint,
                platformUtils: windowsPlatform.Object);

            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentReturnsNull();
            this.SetupWithPromptHint();

            this.mockPca
                .Setup(pca => pca.GetTokenInteractiveAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Broker crash"));

            Func<Task> act = async () => await broker.GetTokenAsync();

            await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("Broker crash");
        }

        [Test]
        public async Task MacOS_SilentFails_Interactive_RetriesWithClaims_Success()
        {
            // No cached account, interactive gets MsalUiRequiredException, retries with claims.
            this.mockPca
                .Setup(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync((IAccount)null);

            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveMsalUiRequiredException(null);
            this.SetupGetTokenInteractiveWithClaimsSuccess();

            AuthFlowResult result = await this.Subject().GetTokenAsync();

            result.TokenResult.Should().Be(this.testToken);
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
        }

        [Test]
        public async Task MacOS_GetTokenSilent_MsalServiceException_ReturnsError()
        {
            this.SetupCachedAccount();
            this.SetupAccountUsername();

            this.mockPca
                .Setup(pca => pca.GetTokenSilentAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MsalServiceException("1", "Service error"));

            AuthFlowResult result = await this.Subject().GetTokenAsync();

            result.TokenResult.Should().BeNull();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Should().BeOfType(typeof(MsalServiceException));
        }
    }
}
