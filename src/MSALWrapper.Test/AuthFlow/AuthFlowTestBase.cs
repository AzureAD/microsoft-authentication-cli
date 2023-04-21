// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Moq;
    using NUnit.Framework;

    internal class AuthFlowTestBase
    {
        protected static readonly Guid ClientId = new Guid("5af6def2-05ec-4cab-b9aa-323d75b5df40");
        protected static readonly Guid TenantId = new Guid("8254f6f7-a09f-4752-8bd6-391adc3b912e");
        protected Mock<IPCAWrapper> mockPca;
        protected Mock<IAccount> mockAccount;
        protected string testUsername = "John Doe";
        protected ILogger logger;

        [SetUp]
        public void Setup()
        {
            (this.logger, _) = MemoryLogger.Create();
            this.mockPca = new Mock<IPCAWrapper>(MockBehavior.Strict);
            this.mockAccount = new Mock<IAccount>(MockBehavior.Strict);
        }

        [TearDown]
        public void TearDown()
        {
            this.mockPca.VerifyAll();
            this.mockAccount.VerifyAll();
        }

        protected virtual void SetupCachedAccount()
        {
            this.mockAccount.Setup(a => a.Username).Returns(this.testUsername);
            this.mockPca
                .Setup(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync(this.mockAccount.Object);
        }

        protected virtual void SetupNoCachedAccount()
        {
            this.mockPca
                .Setup(pca => pca.TryToGetCachedAccountAsync(It.IsAny<string>()))
                .ReturnsAsync((IAccount)null);
        }
    }
}
