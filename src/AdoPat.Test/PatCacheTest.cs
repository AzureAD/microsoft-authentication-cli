// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat.Test
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using FluentAssertions;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;
    using Moq;
    using NUnit.Framework;

    public class PatCacheTest
    {
        private const string DisplayName = "Test PAT";
        private const string Scope = "test.scope";

        // This is a test token. A real value would be a much longer string.
        private const string Token = "Test Token";

        // This list of accounts uses dummy data, not real accounts.
        private readonly List<Guid> targetAccounts = new List<Guid> { new Guid("b7b59161-cd70-46e9-aca5-883f24060eb1") };

        // This is a dummy authorization ID, not valid in any real contexts.
        private readonly Guid authorizationId = new Guid("ee0c5586-a96f-4a44-b1d9-8613028b1078");

        [Test]
        public void GetCache_NoDataInCache()
        {
            // Arrange
            var key = "needle";

            var storage = new Mock<IStorageWrapper>(MockBehavior.Strict);
            storage.Setup(s => s.ReadData()).Returns(new byte[] { });

            var cache = new PatCache(storage.Object);

            // Act
            var patToken = cache.Get(key);

            // Assert
            patToken.Should().BeNull();
        }

        [Test]
        public void GetCache_EmptyJson()
        {
            // Arrange
            var key = "needle";
            var pats = new Dictionary<string, PatToken> { };

            var storage = new Mock<IStorageWrapper>(MockBehavior.Strict);
            storage.Setup(s => s.ReadData()).Returns(JsonSerializer.SerializeToUtf8Bytes(pats));

            var cache = new PatCache(storage.Object);

            // Act
            var patToken = cache.Get(key);

            // Assert
            patToken.Should().BeNull();
        }

        [Test]
        public void GetCache_SinglePat()
        {
            // Arrange
            var key = "needle";
            var expectedPat = new PatToken
            {
                DisplayName = DisplayName,
                Scope = Scope,
                TargetAccounts = this.targetAccounts,
                ValidTo = DateTime.UnixEpoch.AddDays(-7),
                ValidFrom = DateTime.UnixEpoch,
                AuthorizationId = this.authorizationId,
                Token = Token,
            };
            var pats = new Dictionary<string, PatToken> { { key, expectedPat } };
            var data = JsonSerializer.SerializeToUtf8Bytes(pats);

            var storage = new Mock<IStorageWrapper>(MockBehavior.Strict);
            storage.Setup(s => s.ReadData()).Returns(data);

            var cache = new PatCache(storage.Object);

            // Act
            var patToken = cache.Get(key);

            // Assert
            patToken.Should().BeEquivalentTo(expectedPat);
        }

        [Test]
        public void GetCache_MultiplePats()
        {
            // Arrange
            var key = "needle";
            var expectedPat = new PatToken
            {
                DisplayName = DisplayName,
                Scope = Scope,
                TargetAccounts = this.targetAccounts,
                ValidTo = DateTime.UnixEpoch.AddDays(-7),
                ValidFrom = DateTime.UnixEpoch,
                AuthorizationId = this.authorizationId,
                Token = Token,
            };
            var pats = new Dictionary<string, PatToken>
            {
                { "lol", new PatToken { DisplayName = "wut" } },
                { key, expectedPat },
            };
            var data = JsonSerializer.SerializeToUtf8Bytes(pats);

            var storage = new Mock<IStorageWrapper>(MockBehavior.Strict);
            storage.Setup(s => s.ReadData()).Returns(data);

            var cache = new PatCache(storage.Object);

            // Act
            var patToken = cache.Get(key);

            // Assert
            patToken.Should().BeEquivalentTo(expectedPat);
        }

        [Test]
        public void PutCache_NoDataInCache()
        {
            var key = "needle";
            var pat = new PatToken
            {
                DisplayName = DisplayName,
                Scope = Scope,
                TargetAccounts = this.targetAccounts,
                ValidTo = DateTime.UnixEpoch.AddDays(-7),
                ValidFrom = DateTime.UnixEpoch,
                AuthorizationId = this.authorizationId,
                Token = Token,
            };
            var pats = new Dictionary<string, PatToken> { { key, pat } };
            var expectedData = JsonSerializer.SerializeToUtf8Bytes(pats);

            var data = new byte[] { };
            var storage = new Mock<IStorageWrapper>(MockBehavior.Strict);
            storage.Setup(s => s.ReadData()).Returns(data);
            storage.Setup(s => s.WriteData(It.IsAny<byte[]>())).Callback((byte[] d) => data = d);

            var cache = new PatCache(storage.Object);

            // Act
            cache.Put(key, pat);

            // Assert
            // The in-memory cache should contain the PatToken and so should
            // the underlying storage.
            cache.Get(key).Should().BeEquivalentTo(pat);
            data.Should().BeEquivalentTo(expectedData);
        }

        [Test]
        public void PutCache_ExistingDataInCache()
        {
            var key = "needle";
            var pat = new PatToken
            {
                DisplayName = DisplayName,
                Scope = Scope,
                TargetAccounts = this.targetAccounts,
                ValidTo = DateTime.UnixEpoch.AddDays(-7),
                ValidFrom = DateTime.UnixEpoch,
                AuthorizationId = this.authorizationId,
                Token = Token,
            };

            // First, write existing data as bytes without our target PatToken.
            var pats = new Dictionary<string, PatToken> { { "lol", new PatToken { DisplayName = "wut" } } };
            var existingData = JsonSerializer.SerializeToUtf8Bytes(pats);

            // Then, add our target PatToken to the expected data for later
            // comparison.
            pats.Add(key, pat);
            var expectedData = JsonSerializer.SerializeToUtf8Bytes(pats);

            var data = new byte[] { };
            var storage = new Mock<IStorageWrapper>(MockBehavior.Strict);
            storage.Setup(s => s.ReadData()).Returns(existingData);
            storage.Setup(s => s.WriteData(It.IsAny<byte[]>())).Callback((byte[] d) => data = d);

            var cache = new PatCache(storage.Object);

            // Act
            cache.Put(key, pat);

            // Assert
            // The in-memory cache should contain the PatToken and so should
            // the underlying storage.
            cache.Get(key).Should().BeEquivalentTo(pat);
            data.Should().BeEquivalentTo(expectedData);
        }

        [Test]
        public void PutCache_OverwriteExistingPatToken()
        {
            var key = "needle";
            var pat = new PatToken
            {
                DisplayName = DisplayName,
                Scope = Scope,
                TargetAccounts = this.targetAccounts,
                ValidTo = DateTime.UnixEpoch.AddDays(-7),
                ValidFrom = DateTime.UnixEpoch,
                AuthorizationId = this.authorizationId,
                Token = Token,
            };

            // Make our expected PatToken differ only by the Token value.
            var expectedPat = pat;
            expectedPat.Token = "New Token Value";

            // First, write existing data as bytes.
            var pats = new Dictionary<string, PatToken> { { key, pat } };
            var existingData = JsonSerializer.SerializeToUtf8Bytes(pats);

            // Then, overwrite the existing data with the expected token.
            pats[key] = expectedPat;
            var expectedData = JsonSerializer.SerializeToUtf8Bytes(pats);

            var data = new byte[] { };
            var storage = new Mock<IStorageWrapper>(MockBehavior.Strict);
            storage.Setup(s => s.ReadData()).Returns(existingData);
            storage.Setup(s => s.WriteData(It.IsAny<byte[]>())).Callback((byte[] d) => data = d);

            var cache = new PatCache(storage.Object);

            // Act
            cache.Put(key, pat);

            // Assert
            // The in-memory cache should contain *ONLY* the expected PatToken
            // and so should the underlying storage.
            cache.Get(key).Should().BeEquivalentTo(expectedPat);
            data.Should().BeEquivalentTo(expectedData);
        }
    }
}
