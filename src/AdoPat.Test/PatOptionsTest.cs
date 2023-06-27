// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat.Test
{
    using System.Collections.Immutable;
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
                Scopes = ImmutableSortedSet.CreateRange<string>(new[] { "test.scope.a", "test.scope.b", "test.scope.c" }),
            };
            var expected = $"{OrganizationHash}-{DisplayNameHash}-eb52c21ad04b684f72bb1cef51e7f4ca58ce5a753744123a5df8f4e1781f831d";

            patOptions.CacheKey().Should().Be(expected);
        }
    }
}
