// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat.Test
{
    using FluentAssertions;
    using NUnit.Framework;

    public class PatOptionsTest
    {
        private const string Organization = "contoso";
        private const string DisplayName = "Test PAT";

        [Test]
        public void CacheKey_Uses_Organization_DisplayName_And_Scopes()
        {
            var patOptions = new PatOptions
            {
                Organization = Organization,
                DisplayName = DisplayName,
                Scopes = new[] { "test.scope.a", "test.scope.b", "test.scope.c" },
            };
            var expected = $"{Organization} {DisplayName} test.scope.a test.scope.b test.scope.c";

            patOptions.CacheKey().Should().BeEquivalentTo(expected);
        }

        [Test]
        public void CacheKey_Scope_Order_Is_Deterministic()
        {
            // These two PAT options have the same organization, display name,
            // and scopes, but the scopes are in different orders. They should
            // still be considered to have the same PAT cache key.
            var patOptions1 = new PatOptions
            {
                Organization = Organization,
                DisplayName = DisplayName,
                Scopes = new[] { "test.scope.a", "test.scope.b", "test.scope.c" },
            };
            var patOptions2 = new PatOptions
            {
                Organization = Organization,
                DisplayName = DisplayName,
                Scopes = new[] { "test.scope.b", "test.scope.a", "test.scope.c" },
            };

            patOptions1.CacheKey().Should().BeEquivalentTo(patOptions2.CacheKey());
        }
    }
}
