// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat.Test
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;
    using Moq;
    using NUnit.Framework;

    public class PatClientTest
    {
        private const bool AllOrgs = false;
        private const string DisplayName = "Test PAT";
        private const string Scope = "test.scope";

        // This is a test token. A real value would be a much longer string.
        private const string Token = "Test Token";

        // The Unix Epoch is used as an obviously fake test time which occurs in the past and cannot accidentally be valid.
        private readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // This list of accounts uses dummy data, not real accounts.
        private readonly List<Guid> targetAccounts = new List<Guid> { new Guid("b7b59161-cd70-46e9-aca5-883f24060eb1") };

        // This is a dummy authorization ID, not valid in any real contexts.
        private readonly Guid authorizationId = new Guid("ee0c5586-a96f-4a44-b1d9-8613028b1078");

        [Test]
        public async Task CreatePatAsync()
        {
            // Arrange
            var patTokenCreateRequest = new PatTokenCreateRequest
            {
                DisplayName = DisplayName,
                Scope = Scope,
                ValidTo = this.unixEpoch.AddDays(7),
                AllOrgs = AllOrgs,
            };
            var patToken = new PatToken
            {
                DisplayName = DisplayName,
                Scope = Scope,
                TargetAccounts = this.targetAccounts,
                ValidTo = this.unixEpoch.AddDays(7),
                ValidFrom = this.unixEpoch,
                AuthorizationId = this.authorizationId,
                Token = Token,
            };
            var patTokenResult = new PatTokenResult
            {
                PatToken = patToken,
                PatTokenError = SessionTokenError.None,
            };

            var client = new Mock<ITokensHttpClientWrapper>(MockBehavior.Strict);
            client.Setup(c => c.CreatePatAsync(
                It.IsAny<PatTokenCreateRequest>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(patTokenResult);

            var patClient = new PatClient(client.Object);

            // Act
            var renewedPatToken = await patClient.CreatePatAsync(patTokenCreateRequest);

            // Assert
            renewedPatToken.Should().BeEquivalentTo(patToken);
        }

        [Test]
        public async Task GetActivePatsAsync_ReturnsEmptyDictionaryWithNoResults()
        {
            // Arrange
            var emptyPage = new PagedPatTokens(continuationToken: string.Empty, patTokens: new List<PatToken> { });
            var expectedTokens = new Dictionary<Guid, PatToken>();

            var client = new Mock<ITokensHttpClientWrapper>(MockBehavior.Strict);
            client.Setup(c => c.ListPatsAsync(
                It.IsAny<DisplayFilterOptions?>(),
                It.IsAny<SortByOptions?>(),
                It.IsAny<bool?>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyPage);

            var patClient = new PatClient(client.Object);

            // Act
            var activePats = await patClient.GetActivePatsAsync();

            // Assert
            activePats.Should().BeEquivalentTo(expectedTokens);
        }

        [Test]
        public async Task GetActivePatsAsync_ReturnsDictionaryWithAllPages()
        {
            // Arrange
            var pat1 = new PatToken { AuthorizationId = new Guid("8e510ed2-f485-4dfb-963c-bbaa30d60ba0"), DisplayName = "PAT 1" };
            var pat2 = new PatToken { AuthorizationId = new Guid("40ba0030-1dad-4bbe-82c5-8c49266d4e1d"), DisplayName = "PAT 2" };
            var page1 = new PagedPatTokens(continuationToken: "Page 1", patTokens: new List<PatToken> { pat1 });
            var page2 = new PagedPatTokens(continuationToken: string.Empty, patTokens: new List<PatToken> { pat2 });
            var expectedTokens = new Dictionary<Guid, PatToken>() { { pat1.AuthorizationId, pat1 }, { pat2.AuthorizationId, pat2 } };

            var client = new Mock<ITokensHttpClientWrapper>(MockBehavior.Strict);
            client.SetupSequence(c => c.ListPatsAsync(
                It.IsAny<DisplayFilterOptions?>(),
                It.IsAny<SortByOptions?>(),
                It.IsAny<bool?>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page1)
            .ReturnsAsync(page2);

            var patClient = new PatClient(client.Object);

            // Act
            var activePats = await patClient.GetActivePatsAsync();

            // Assert
            activePats.Should().BeEquivalentTo(expectedTokens);
        }

        [Test]
        public async Task RegeneratePatAsync_RevokesOldPatAndReturnsNewPat()
        {
            // Arrange
            var issued = this.unixEpoch.AddDays(-7);
            var validTo = this.unixEpoch;
            var regeneratedValidTo = this.unixEpoch.AddDays(7);
            var patToken = new PatToken
            {
                DisplayName = DisplayName,
                Scope = Scope,
                TargetAccounts = this.targetAccounts,
                ValidTo = validTo,
                ValidFrom = issued,
                AuthorizationId = this.authorizationId,
                Token = Token,
            };

            var expectedToken = patToken;
            expectedToken.ValidTo = regeneratedValidTo;
            expectedToken.ValidFrom = validTo;

            var patTokenResult = new PatTokenResult { PatToken = expectedToken };

            var client = new Mock<ITokensHttpClientWrapper>(MockBehavior.Strict);
            client.Setup(c => c.CreatePatAsync(
                It.IsAny<PatTokenCreateRequest>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(patTokenResult);

            // This returns a Task because it's normally a void method and
            // moq has some trouble with that: https://stackoverflow.com/a/66799787/3288364
            client.Setup(c => c.RevokeAsync(
                It.IsAny<Guid>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

            var patClient = new PatClient(client.Object);

            // Act
            var regeneratedPat = await patClient.RegeneratePatAsync(patToken, regeneratedValidTo);

            // Assert
            client.VerifyAll();
            regeneratedPat.Should().BeEquivalentTo(expectedToken);
        }

        [Test]
        public async Task RegeneratePatAsync_CreationFailureNullPatToken()
        {
            // Arrange
            var issued = this.unixEpoch.AddDays(-7);
            var validTo = this.unixEpoch;
            var regeneratedValidTo = this.unixEpoch.AddDays(7);
            var patToken = new PatToken
            {
                DisplayName = DisplayName,
                Scope = Scope,
                TargetAccounts = this.targetAccounts,
                ValidTo = validTo,
                ValidFrom = issued,
                AuthorizationId = this.authorizationId,
                Token = Token,
            };
            var failingResult = new PatTokenResult
            {
                PatToken = null,
                PatTokenError = SessionTokenError.InvalidAuthorizationId,
            };

            var client = new Mock<ITokensHttpClientWrapper>(MockBehavior.Strict);
            client.Setup(c => c.CreatePatAsync(
                It.IsAny<PatTokenCreateRequest>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failingResult);

            var patClient = new PatClient(client.Object);

            // Act
            Func<Task> act = () => patClient.RegeneratePatAsync(patToken, regeneratedValidTo);

            // Assert
            await act.Should().ThrowAsync<PatClientException>()
                .WithMessage("Failed to create PAT: InvalidAuthorizationId");
        }
    }
}
