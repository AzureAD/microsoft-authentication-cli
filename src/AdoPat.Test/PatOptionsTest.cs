// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat.Test
{
    using FluentAssertions;
    using NUnit.Framework;

    public class PatOptionsTest
    {
        private const string Organization = "contoso";
        private const string OrganizationHash = "11f0b04eda61baebd5646fde72b0058e9713c30e950d2f3457bbbc1c3c68b31a";
        private const string DisplayName = "Test PAT";
        private const string DisplayNameHash = "c27c517aad4237d8e221d8f52a438cb24f7f8078582d6cb025c238a25b2460f7";

        [Test]
        public void CacheKey_Uses_Organization_DisplayName_And_Scopes()
        {
            var patOptions = new PatOptions
            {
                Organization = Organization,
                DisplayName = DisplayName,
                Scopes = new[] { "test.scope.a", "test.scope.b", "test.scope.c" },
            };
            var expected = $"{OrganizationHash}-{DisplayNameHash}-eb52c21ad04b684f72bb1cef51e7f4ca58ce5a753744123a5df8f4e1781f831d";

            patOptions.CacheKey().Should().Be(expected);
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

            patOptions1.CacheKey().Should().Be(patOptions2.CacheKey());
        }
    }
}
