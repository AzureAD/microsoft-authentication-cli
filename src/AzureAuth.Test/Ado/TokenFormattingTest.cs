// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test.Ado
{
    using FluentAssertions;

    using Microsoft.Authentication.AzureAuth.Ado;

    using NUnit.Framework;

    internal class TokenFormattingTest
    {
        [Test]
        public void Header_Bearer()
        {
            "foobar".AsHeader(Authorization.Bearer).Should().Be("Authorization: Bearer foobar");
        }

        [Test]
        public void Header_Bearer_Value()
        {
            "foobar".AsHeaderValue(Authorization.Bearer).Should().Be("Bearer foobar");
        }

        /*
        * Base64 encoding takes an input, turns it into binary form, (typically 8-bit characters)
        * And then takes 6-bit chunks and represents the string using the 2^6 (64) characters.
        * Because of the difference in lengths of encoding between 8 bit and 6 bit (decoded and encoded values)
        * the encoded values may contain padding.
        * See https://en.wikipedia.org/wiki/Base64#Output_padding for details.
        */

        [TestCase("foobar", "Zm9vYmFy")]
        [TestCase("foobars", "Zm9vYmFycw==")]
        public void Base64(string input, string output)
        {
            input.Base64().Should().Be(output);
        }

        [TestCase("foobar", "OmZvb2Jhcg==")]
        [TestCase("foobars", "OmZvb2JhcnM=")]
        public void Header_Basic(string input, string output)
        {
            input.AsHeader(Authorization.Basic).Should().Be($"Authorization: Basic {output}");
        }

        [TestCase("foobar", "OmZvb2Jhcg==")]
        [TestCase("foobars", "OmZvb2JhcnM=")]
        public void Header_Basic_Value(string input, string output)
        {
            input.AsHeaderValue(Authorization.Basic).Should().Be($"Basic {output}");
        }
    }
}
