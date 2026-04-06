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
        public async Task MacOS_NoCachedAccount_PersistedAccountFound_SilentSuccess()
        {
            // No cached account via preferred domain lookup, but a persisted username
            // matches an account in the MSAL cache — silent auth succeeds.
            this.SetupCachedAccount(false);

            // Save a persisted account so ResolveAccountAsync can find it.
            var store = new DefaultAccountStore(this.logger);
            store.SaveDefaultAccount(TestUsername, ClientId, TenantId.ToString());

            try
            {
                // Set up accounts returned by TryToGetCachedAccountsAsync
                this.mockAccount.Setup(a => a.Username).Returns(TestUsername);
                this.mockPca
                    .Setup(pca => pca.TryToGetCachedAccountsAsync(null))
                    .ReturnsAsync(new List<IAccount> { this.mockAccount.Object });

                this.mockPca
                    .Setup(pca => pca.GetTokenSilentAsync(Scopes, this.mockAccount.Object, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(this.testToken);

                AuthFlowResult result = await this.Subject().GetTokenAsync();

                result.TokenResult.Should().Be(this.testToken);
                result.TokenResult.IsSilent.Should().BeTrue();
                result.Errors.Should().BeEmpty();
            }
            finally
            {
                store.ClearDefaultAccount(ClientId, TenantId.ToString());
            }
        }

        [Test]
        public async Task MacOS_NoCachedAccount_NoPersistedAccount_Interactive_Success()
        {
            // Ensure no stale persisted account from other tests.
            var store = new DefaultAccountStore(this.logger);
            store.ClearDefaultAccount(ClientId, TenantId.ToString());

            // No cached or persisted account — falls through to interactive auth.
            // Use SetupSequence so the first call returns null (ResolveAccountAsync)
            // and the second call returns the account (PersistDefaultAccount).
            this.mockPca
                .SetupSequence(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync((IAccount)null)
                .ReturnsAsync(this.mockAccount.Object);

            this.mockAccount.Setup(a => a.Username).Returns(TestUsername);

            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveSuccess(withAccount: false);

            try
            {
                AuthFlowResult result = await this.Subject().GetTokenAsync();

                result.TokenResult.Should().Be(this.testToken);
                result.TokenResult.IsSilent.Should().BeFalse();
                result.Errors.Should().BeEmpty();

                // Verify the account was persisted for future runs
                var persisted = store.GetDefaultAccount(ClientId, TenantId.ToString());
                persisted.Should().Be(TestUsername);
            }
            finally
            {
                store.ClearDefaultAccount(ClientId, TenantId.ToString());
            }
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
            // Use SetupSequence: first returns null (ResolveAccountAsync),
            // second returns account (PersistDefaultAccount).
            this.mockPca
                .SetupSequence(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync((IAccount)null)
                .ReturnsAsync(this.mockAccount.Object);

            this.mockAccount.Setup(a => a.Username).Returns(TestUsername);

            this.SetupWithPromptHint();
            this.SetupGetTokenInteractiveMsalUiRequiredException(null);
            this.SetupGetTokenInteractiveWithClaimsSuccess();

            var store = new DefaultAccountStore(this.logger);

            try
            {
                AuthFlowResult result = await this.Subject().GetTokenAsync();

                result.TokenResult.Should().Be(this.testToken);
                result.Errors.Should().HaveCount(1);
                result.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
            }
            finally
            {
                store.ClearDefaultAccount(ClientId, TenantId.ToString());
            }
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
