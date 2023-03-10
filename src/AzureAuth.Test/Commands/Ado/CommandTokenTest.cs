// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test.Commands.Ado
{
    using FluentAssertions;
    using Microsoft.Authentication.AzureAuth.Commands.Ado;
    using NUnit.Framework;

    internal class CommandTokenTest
    {
        [Test]
        public void FormatPat_Token()
        {
            CommandToken.FormatPat("foobar", CommandToken.OutputMode.Token).Should().Be("foobar");
        }

        [Test]
        public void FormatPat_HeaderValue()
        {
            CommandToken.FormatPat("foobar", CommandToken.OutputMode.HeaderValue).Should().Be("Basic Zm9vYmFy");
        }

        [Test]
        public void FormatPat_Header()
        {
            CommandToken.FormatPat("foobar", CommandToken.OutputMode.Header).Should().Be("Authorization: Basic Zm9vYmFy");
        }
    }
}
