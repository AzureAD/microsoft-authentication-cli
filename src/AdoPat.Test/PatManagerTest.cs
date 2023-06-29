// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;
    using Moq;
    using NLog.Targets;
    using NUnit.Framework;

    public class PatManagerTest
    {
        private const string Organization = "contoso";
        private const string OrganizationHash = "11f0b04eda61baebd5646fde72b0058e9713c30e950d2f3457bbbc1c3c68b31a";
        private const string DisplayName = "Test PAT";
        private const string DisplayNameHash = "c27c517aad4237d8e221d8f52a438cb24f7f8078582d6cb025c238a25b2460f7";
        private const string Scope = "test.scope";
        private const string ScopeHash = "c403db29c2175b51cfe6fd672b336e5171ff4e7a326a916edca508fd3be018f9";

        // This is a test token. A real value would be a much longer string.
        private const string Token = "Test Token";

        // This list of accounts uses dummy data, not real accounts.
        private readonly List<Guid> targetAccounts = new List<Guid> { new Guid("b7b59161-cd70-46e9-aca5-883f24060eb1") };

        // This is a dummy authorization ID, not valid in any real contexts.
        private readonly Guid authorizationId = new Guid("ee0c5586-a96f-4a44-b1d9-8613028b1078");

        private readonly string cacheKey = string.Join('-', OrganizationHash, DisplayNameHash, ScopeHash);

        private ILogger logger;
        private MemoryTarget logTarget;

        // Common mocks which are configured via the Setup and Teardown methods.
        private Mock<IPatCache> cache;
        private Mock<IPatClient> client;

        // PAT options which are common to all tests.
        private static PatOptions PatOptions => new()
        {
            DisplayName = DisplayName,
            Organization = Organization,
            Scopes = ImmutableSortedSet.CreateRange<string>(new[] { Scope }),
        };

        [SetUp]
        public void Setup()
        {
            this.cache = new Mock<IPatCache>(MockBehavior.Strict);
            this.client = new Mock<IPatClient>(MockBehavior.Strict);
            (this.logger, this.logTarget) = MemoryLogger.Create();
        }

        [TearDown]
        public void Teardown()
        {
            // To be certain we have called all the necessary methods we verify
            // each mock after every test.
            this.cache.VerifyAll();
            this.client.VerifyAll();
        }

        public PatToken PatToken(
            string displayName = null,
            string scope = null,
            List<Guid> targetAccounts = null,
            DateTime? validTo = null,
            DateTime? validFrom = null,
            Guid? authorizationId = null,
            string token = null)
        {
            return new PatToken
            {
                DisplayName = displayName ?? DisplayName,
                Scope = scope ?? Scope,
                TargetAccounts = targetAccounts ?? this.targetAccounts,
                ValidTo = validTo ?? DateTime.UnixEpoch.AddDays(7),
                ValidFrom = validFrom ?? DateTime.UnixEpoch,
                AuthorizationId = authorizationId ?? this.authorizationId,
                Token = token ?? Token,
            };
        }

        [Test]
        public async Task GetPatAsync_ReturnsCachedPatWhenCacheHasValidPat()
        {
            // Arrange
            var expectedPat = this.PatToken();
            var activePats = new Dictionary<Guid, PatToken>
            {
                { expectedPat.AuthorizationId, expectedPat },
            };

            // The cache should contain our target PAT.
            this.cache.Setup(c => c.Get(It.IsAny<string>())).Returns(expectedPat);

            // The client needs to be injected into the manager, but should be unused.
            this.client.Setup(c => c.ListActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(activePats);

            var manager = new PatManager(
                this.logger,
                this.cache.Object,
                this.client.Object,
                now: () => DateTime.UnixEpoch);

            // Act
            var pat = await manager.GetPatAsync(PatOptions);

            // Assert
            pat.Should().BeEquivalentTo(expectedPat);

            this.logTarget.Logs.Count.Should().Be(4);
            this.logTarget.Logs[0].Should().Be($"Checking for PAT in cache with key '{this.cacheKey}'");
            this.logTarget.Logs[1].Should().Be("Found PAT in cache");
            this.logTarget.Logs[2].Should().Be($"PAT active: True");
            this.logTarget.Logs[3].Should().Be($"PAT expiring soon: False");
        }

        [Test]
        public async Task GetPatAsync_CreatesNewPatWhenCacheIsMissingPat()
        {
            // Arrange
            var expectedKey = $"{OrganizationHash}-{DisplayNameHash}-{ScopeHash}";
            var expectedPat = this.PatToken();

            // The cache is empty and therefore returns `null` for any key.
            this.cache.Setup(c => c.Get(It.IsAny<string>())).Returns<PatToken>(null);
            this.cache.Setup(c => c.Put(expectedKey, expectedPat));

            // The client will return a new PatToken upon creation request.
            this.client.Setup(c => c.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPat);

            var manager = new PatManager(
                this.logger,
                this.cache.Object,
                this.client.Object,
                now: () => DateTime.UnixEpoch);

            // Act
            var pat = await manager.GetPatAsync(PatOptions);

            // Assert
            pat.Should().BeEquivalentTo(expectedPat);

            this.logTarget.Logs.Count.Should().Be(3);
            this.logTarget.Logs[0].Should().Be($"Checking for PAT in cache with key '{this.cacheKey}'");
            this.logTarget.Logs[1].Should().Be("No matching PAT found in cache");
            this.logTarget.Logs[2].Should().Be($"Creating new PAT with organization='{Organization}', displayName='{DisplayName}', scopes='{Scope}'");
        }

        [Test]
        public async Task GetPatAsync_RegeneratesPatWhenPatHasExpired()
        {
            // Arrange
            var expectedKey = $"{OrganizationHash}-{DisplayNameHash}-{ScopeHash}";
            var expiringPat = this.PatToken(
                validTo: DateTime.UnixEpoch,
                validFrom: DateTime.UnixEpoch.AddDays(-7));
            var expectedPat = this.PatToken(
                authorizationId: new Guid("6135c9a4-d08c-467f-9902-6ff439651657"),
                token: "Fake Token");
            var activePats = new Dictionary<Guid, PatToken>
            {
                { expiringPat.AuthorizationId, expiringPat },
            };

            // The PAT is found within the cache, but it has expired.
            this.cache.Setup(c => c.Get(It.IsAny<string>())).Returns(expiringPat);
            this.cache.Setup(c => c.Put(expectedKey, expectedPat));

            // The client will return a new PatToken upon creation request.
            this.client.Setup(c => c.ListActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(activePats);
            this.client.Setup(c => c.RegenerateAsync(
                It.IsAny<PatToken>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPat);

            var manager = new PatManager(
                this.logger,
                this.cache.Object,
                this.client.Object,
                now: () => DateTime.UnixEpoch);

            // Act
            var pat = await manager.GetPatAsync(PatOptions);

            // Assert
            pat.Should().BeEquivalentTo(expectedPat);

            this.logTarget.Logs.Count.Should().Be(5);
            this.logTarget.Logs[0].Should().Be($"Checking for PAT in cache with key '{this.cacheKey}'");
            this.logTarget.Logs[1].Should().Be("Found PAT in cache");
            this.logTarget.Logs[2].Should().Be("PAT active: True");
            this.logTarget.Logs[3].Should().Be("PAT expiring soon: True");
            this.logTarget.Logs[4].Should().Be($"Regenerating PAT with organization='{Organization}', displayName='{DisplayName}', scopes='{Scope}'");
        }

        [Test]
        public async Task GetPatAsync_CreatesNewPatWhenPatIsInactive()
        {
            // Arrange
            var expectedKey = $"{OrganizationHash}-{DisplayNameHash}-{ScopeHash}";
            var inactivePat = this.PatToken();
            var expectedPat = this.PatToken(
                authorizationId: new Guid("5045619c-e1b1-46e3-a1e4-ba5f38ddf49b"),
                token: "Fake Token");

            // The inactivePat is not in the list of active PATs from Azure DevOps.
            var activePats = new Dictionary<Guid, PatToken> { };

            // The cache is empty and therefore returns `null` for any key.
            this.cache.Setup(c => c.Get(It.IsAny<string>())).Returns(inactivePat);
            this.cache.Setup(c => c.Put(expectedKey, expectedPat));

            // The client will return a new PatToken upon creation request.
            this.client.Setup(c => c.ListActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(activePats);
            this.client.Setup(c => c.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPat);

            var manager = new PatManager(
                this.logger,
                this.cache.Object,
                this.client.Object,
                now: () => DateTime.UnixEpoch);

            // Act
            var pat = await manager.GetPatAsync(PatOptions);

            // Assert
            pat.Should().BeEquivalentTo(expectedPat);

            this.logTarget.Logs.Count.Should().Be(4);
            this.logTarget.Logs[0].Should().Be($"Checking for PAT in cache with key '{this.cacheKey}'");
            this.logTarget.Logs[1].Should().Be("Found PAT in cache");
            this.logTarget.Logs[2].Should().Be("PAT active: False");
            this.logTarget.Logs[3].Should().Be($"Creating new PAT with organization='{Organization}', displayName='{DisplayName}', scopes='{Scope}'");
        }
    }
}
