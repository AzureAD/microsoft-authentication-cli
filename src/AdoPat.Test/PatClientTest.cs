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
        [Test]
        public async Task CreatePatAsync()
        {
            // Arrange
            var displayName = "Test PAT";
            var scope = "test.scope";
            var validTo = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var allOrgs = false;
            var patTokenCreateRequest = new PatTokenCreateRequest
            {
                DisplayName = displayName,
                Scope = scope,
                ValidTo = validTo,
                AllOrgs = allOrgs,
            };
            var patToken = new PatToken
            {
                DisplayName = displayName,
                Scope = scope,
                TargetAccounts = new List<Guid> { new Guid("b7b59161-cd70-46e9-aca5-883f24060eb1") },
                ValidTo = validTo,
                ValidFrom = DateTime.UtcNow,
                AuthorizationId = new Guid("ee0c5586-a96f-4a44-b1d9-8613028b1078"),
                Token = "A real token would be much, much longer.",
            };
            var expectedResult = new PatTokenResult { PatToken = patToken, PatTokenError = SessionTokenError.None };

            var client = new Mock<ITokensHttpClientProvider>(MockBehavior.Strict);
            client.Setup(thcp => thcp.CreatePatAsync(
                It.IsAny<PatTokenCreateRequest>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

            var patClient = new PatClient(client.Object);

            // Act
            var patTokenResult = await patClient.CreatePatAsync(patTokenCreateRequest);

            // Assert
            patTokenResult.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task GetActivePatsAsync_ReturnsEmptySetWithNoResults()
        {
            // Arrange
            var emptyPage = new PagedPatTokens(continuationToken: string.Empty, patTokens: new List<PatToken> { });
            var expectedTokens = new HashSet<PatToken>();

            var client = new Mock<ITokensHttpClientProvider>(MockBehavior.Strict);
            client.SetupSequence(thcp => thcp.ListPatsAsync(
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
        public async Task GetActivePatsAsync_ReturnsSetWithAllPages()
        {
            // Arrange
            var pat1 = new PatToken { DisplayName = "PAT 1" };
            var pat2 = new PatToken { DisplayName = "PAT 2" };
            var page1 = new PagedPatTokens(continuationToken: "Page 1", patTokens: new List<PatToken> { pat1 });
            var page2 = new PagedPatTokens(continuationToken: string.Empty, patTokens: new List<PatToken> { pat2 });
            var expectedTokens = new HashSet<PatToken>() { pat1, pat2 };

            var client = new Mock<ITokensHttpClientProvider>(MockBehavior.Strict);
            client.SetupSequence(thcp => thcp.ListPatsAsync(
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
    }
}
