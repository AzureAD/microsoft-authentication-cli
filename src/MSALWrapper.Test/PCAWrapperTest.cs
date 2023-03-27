// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
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

    public class PCAWrapperTest
    {
        private const string TestUser = "user@microsoft.com";

        private readonly IAccount userLive = new MockAccount("first@live.com");
        private readonly IAccount userMicrosoft1 = new MockAccount("second@microsoft.com");
        private readonly IAccount userMicrosoft2 = new MockAccount("third@microsoft.com");

        private MemoryTarget logTarget;
        private ILogger logger;

        // MSAL Specific Mocks
        private Mock<IPublicClientApplication> pcaClientMock;
        private Mock<IAccount> testAccount;
        private TokenResult tokenResult;

        [SetUp]
        public void Setup()
        {
            (this.logger, this.logTarget) = MemoryLogger.Create();

            // MSAL Mocks
            this.testAccount = new Mock<IAccount>(MockBehavior.Strict);
            this.testAccount.Setup(a => a.Username).Returns(TestUser);

            this.pcaClientMock = new Mock<IPublicClientApplication>(MockBehavior.Strict);

            // Mock successful token result
            this.tokenResult = new TokenResult(new JsonWebToken(TokenResultTest.FakeToken), Guid.NewGuid());
        }

        [Test]
        public async Task TryToGetCachedAccountAsync_NoAccounts()
        {
            this.MockAccounts(new List<IAccount>());

            // Act
            IPCAWrapper subject = this.Subject();
            IAccount result = await subject.TryToGetCachedAccountAsync();

            // Assert
            this.pcaClientMock.VerifyAll();
            result.Should().BeNull();
        }

        [Test]
        public async Task TryToGetCachedAccountAsync_Null()
        {
            this.MockAccounts(null);

            // Act
            IAccount result = await this.Subject().TryToGetCachedAccountAsync();

            // Assert
            this.pcaClientMock.VerifyAll();
            result.Should().BeNull();
        }

        [Test]
        public async Task TryToGetCachedAccountAsync_OneAccount()
        {
            var joe = new MockAccount("joe@microsoft.com");
            IList<IAccount> accounts = new List<IAccount>() { joe };
            this.MockAccounts(accounts);

            // Act
            IAccount result = await this.Subject().TryToGetCachedAccountAsync();

            // Assert
            this.pcaClientMock.VerifyAll();
            result.Should().BeSameAs(joe);
        }

        [Test]
        public async Task TryToGetCachedAccountAsync_TwoAccounts()
        {
            var first = new MockAccount("first@live.com");
            var second = new MockAccount("second@microsoft.com");
            IList<IAccount> accounts = new List<IAccount>() { first, second };
            this.MockAccounts(accounts);

            // Act
            IAccount result = await this.Subject().TryToGetCachedAccountAsync();

            // Assert
            this.pcaClientMock.VerifyAll();
            result.Should().BeNull();
        }

        [Test]
        public async Task TryToGetCachedAccountAsync_TwoAccounts_WithPreferredDomain()
        {
            IList<IAccount> accounts = new List<IAccount>() { this.userLive, this.userMicrosoft1 };
            this.MockAccounts(accounts);

            // Act
            IAccount result = await this.Subject().TryToGetCachedAccountAsync("microsoft.com");

            // Assert
            this.pcaClientMock.VerifyAll();
            result.Should().BeSameAs(this.userMicrosoft1);
        }

        [Test]
        public async Task TryToGetCachedAccountAsync_MultipleAccounts_WithPreferredDomain()
        {
            IList<IAccount> accounts = new List<IAccount>() { this.userLive, this.userMicrosoft1, this.userMicrosoft2 };
            this.MockAccounts(accounts);

            // Act
            IAccount result = await this.Subject().TryToGetCachedAccountAsync();

            // Assert
            this.pcaClientMock.VerifyAll();
            result.Should().BeNull();
        }

        private void MockAccounts(IEnumerable<IAccount> accounts)
        {
            this.pcaClientMock
                .Setup(pca => pca.GetAccountsAsync())
                .ReturnsAsync(accounts);
        }

        private IPCAWrapper Subject() => new PCAWrapper(this.logger, this.pcaClientMock.Object);
    }
}
