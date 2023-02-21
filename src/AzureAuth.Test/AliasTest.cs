// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Test
{
    using System.Collections.Generic;

    using FluentAssertions;

    using NUnit.Framework;

    internal class AliasTest
    {
        private Alias alias;
        private Alias other;
        private Alias expected;

        /// <summary>
        /// The setup.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            this.alias = new Alias();
            this.other = new Alias();
            this.expected = new Alias();
        }

        /// <summary>
        /// The test for merge.
        /// </summary>
        [Test]
        public void TestMerge()
        {
            // No members set on alias, other, or expected
            Alias result = this.alias.Override(this.other);

            result.Should().BeEquivalentTo(this.expected);
        }

        /// <summary>
        /// The test for merge prefers other members.
        /// </summary>
        /// <param name="resource">
        /// The resource.
        /// </param>
        [TestCase(null)]
        [TestCase("a string that is not null")]
        public void TestMergePrefersOtherMembers(string resource)
        {
            this.alias.Resource = resource;
            this.other.Resource = "expected resource";

            this.expected.Resource = "expected resource";

            Alias result = this.alias.Override(this.other);

            result.Should().BeEquivalentTo(this.expected);
        }

        /// <summary>
        /// The test to merge prefers non null members.
        /// </summary>
        [Test]
        public void TestMergePrefersNonNullMembers()
        {
            this.alias.Resource = "expected resource";
            this.expected.Resource = "expected resource";

            Alias result = this.alias.Override(this.other);

            result.Should().BeEquivalentTo(this.expected);
        }

        /// <summary>
        /// The test to merge prompt hints.
        /// </summary>
        [Test]
        public void TestMergePromptHint()
        {
            this.alias.PromptHint = "expected prompt hint";
            this.expected.PromptHint = "expected prompt hint";

            Alias result = this.alias.Override(this.other);

            result.Should().BeEquivalentTo(this.expected);
        }

        /// <summary>
        /// The test to merge multiple members.
        /// </summary>
        [Test]
        public void TestMergeMultipleMembers()
        {
            this.alias.Resource = "expected resource";
            this.other.Client = "expected client";

            this.expected.Resource = "expected resource";
            this.expected.Client = "expected client";

            Alias result = this.alias.Override(this.other);

            result.Should().BeEquivalentTo(this.expected);
        }

        /// <summary>
        /// The test to merge all members.
        /// </summary>
        [Test]
        public void TestMergeAllMembers()
        {
            this.alias.Resource = "unexpected resource";
            this.alias.Client = "unexpected client";
            this.alias.Domain = "unexpected domain";
            this.alias.Tenant = "unexpected tenant";
            this.alias.PromptHint = "unexpected prompt hint";
            this.alias.Scopes = new List<string> { "unexpected scope" };

            Alias result = this.alias.Override(this.other);

            result.Should().BeEquivalentTo(this.alias);
        }
    }
}
