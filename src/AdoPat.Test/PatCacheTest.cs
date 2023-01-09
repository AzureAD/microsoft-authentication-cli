// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat.Test
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using FluentAssertions;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;
    using Moq;
    using NUnit.Framework;

    public class PatCacheTest
    {
        [Test]
        [Ignore("Not implemented.")]
        public void PersistenceIsntValid()
        {
        }

        [Test]
        [Ignore("Not implemented.")]
        public void CacheDoesntExist()
        {
        }

        [Test]
        public void GetCache_EmptyCache()
        {
            // Arrange
            var key = "needle";
            var haystack = "{}";

            var storage = new Mock<IStorageWrapper>(MockBehavior.Strict);
            storage.Setup(s => s.ReadData()).Returns(Encoding.UTF8.GetBytes(haystack));

            var cache = new PatCache(storage.Object);

            // Act
            var patToken = cache.GetPat(key);

            // Assert
            patToken.Should().BeNull();
        }
    }
}
