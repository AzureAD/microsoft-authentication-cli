// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test.Ado
{
    using FluentAssertions;

    using Microsoft.Authentication.AzureAuth.Ado;

    using NUnit.Framework;

    internal class TokenFormatterTest
    {
        [Test]
        public void Header_Bearer()
        {
            TokenFormatter.HeaderBearer("foobar").Should().Be("Authorization: Bearer foobar");
        }

        [Test]
        public void Header_Bearer_Value()
        {
            TokenFormatter.HeaderBearerValue("foobar").Should().Be("Bearer foobar");
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
            TokenFormatter.Base64(input).Should().Be(output);
        }

        [TestCase("foobar", "Zm9vYmFy")]
        [TestCase("foobars", "Zm9vYmFycw==")]
        public void Header_Basic(string input, string output)
        {
            TokenFormatter.HeaderBasic(input).Should().Be($"Authorization: Basic {output}");
        }

        [TestCase("foobar", "Zm9vYmFy")]
        [TestCase("foobars", "Zm9vYmFycw==")]
        public void Header_Basic_Value(string input, string output)
        {
            TokenFormatter.HeaderBasicValue(input).Should().Be($"Basic {output}");
        }

        
    }
}
