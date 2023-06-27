// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat.Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using FluentAssertions;
    using NUnit.Framework;

    public class ScopesTest
    {
        private static HashSet<string> validScopes = Scopes.ValidScopes;

        [Test, TestCaseSource(nameof(validScopes))]
        public void Valid_Scopes_Are_Valid(string scope)
        {
            var expected = ImmutableHashSet.Create<string>();
            Scopes.Validate(new[] { scope }).Should().BeEquivalentTo(expected);
        }

        [Test]
        public void Invalid_Scopes_Are_Invalid()
        {
            var scopes = new[] { "vso.invalid_scope", "VSO.INVALID_SCOPE" };
            var expected = ImmutableHashSet.CreateRange<string>(scopes);
            Scopes.Validate(scopes).Should().BeEquivalentTo(expected);
        }

        public void Only_Invalid_Scopes_Are_Invalid()
        {
            var invalidScope = "vso.invalid_scope";
            var scopes = new[] { "vso.packaging", invalidScope };
            var expected = ImmutableHashSet.CreateRange<string>(new[] { invalidScope });
            Scopes.Validate(scopes).Should().BeEquivalentTo(expected);
        }

        [Test]
        public void Normalization_Ignores_Case()
        {
            var given = new[] { "FOO", "BAR" };
            var expected = ImmutableSortedSet.CreateRange<string>(new[] { "foo", "bar" });
            var actual = Scopes.Normalize(given);
            actual.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void Normalization_Removes_Duplicates()
        {
            var given = new[] { "foo", "foo" };
            var expected = ImmutableSortedSet.CreateRange<string>(new[] { "foo" });
            var actual = Scopes.Normalize(given);
            actual.Should().BeEquivalentTo(expected);
        }
    }
}
