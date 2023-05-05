// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    using Moq;

    using NUnit.Framework;

    internal class CachedAuthTest : AuthFlowTestBase
    {
        public IAuthFlow Subject() => new CachedAuth(this.logger, this.authParameters, pcaWrapper: this.mockPca.Object);

        [Test]
        public void Null_Account_Returns_Null_Without_Errors()
        {
            IList<Exception> errors = new List<Exception>();
            var subject = CachedAuth.GetTokenAsync(
                this.logger,
                new[] { "scope" },
                null,
                new Mock<IPCAWrapper>(MockBehavior.Strict).Object,
                errors).Result;

            subject.Should().BeNull();
            errors.Should().BeEmpty();
        }

        [Test]
        public void MsalUiRequiredException_Results_In_Null_With_Error()
        {
            // Setup
            this.SetupAccountUsername();
            this.SetupGetTokenSilentMsalUiRequiredException();

            IList<Exception> errors = new List<Exception>();

            // Act
            var subject = CachedAuth.GetTokenAsync(
                this.logger,
                Scopes,
                this.mockAccount.Object,
                this.mockPca.Object,
                errors).Result;

            // Assert
            subject.Should().BeNull();
            errors.Should().HaveCount(1);
            errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
        }

        [Test]
        public void SuccessfulCachedAuth_IsSilent()
        {
            // Setup
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentSuccess();

            // Act
            AuthFlowResult result = this.Subject().GetTokenAsync().Result;

            // Assert
            result.Should().NotBeNull();
            result.Errors.Should().BeEmpty();
            result.TokenResult.Should().BeEquivalentTo(this.testToken);
            result.TokenResult.IsSilent.Should().BeTrue();
        }

        [Test]
        public void CachedAuthFlow_No_Account_Results_In_Null_Without_Errors()
        {
            // Setup
            this.SetupCachedAccount(false);

            // Act
            AuthFlowResult result = this.Subject().GetTokenAsync().Result;

            // Assert
            result.Should().NotBeNull();
            result.TokenResult.Should().BeNull();
        }

        [Test]
        public void CachedAuthFlow_MsalUiRequiredException_Results_In_Null_With_Error()
        {
            // Setup
            this.SetupCachedAccount();
            this.SetupAccountUsername();
            this.SetupGetTokenSilentMsalUiRequiredException();

            // Act
            AuthFlowResult result = this.Subject().GetTokenAsync().Result;

            // Assert
            result.Should().NotBeNull();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Should().BeOfType(typeof(MsalUiRequiredException));
        }
    }
}
