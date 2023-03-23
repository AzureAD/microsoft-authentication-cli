// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test.Commands.Ado
{
    using FluentAssertions;
    using Microsoft.Authentication.AzureAuth.Ado;
    using Microsoft.Authentication.AzureAuth.Commands.Ado;
    using NUnit.Framework;

    internal class CommandTokenTest
    {
        [TestCase("foobar", CommandToken.OutputMode.Token, "foobar")]
        [TestCase("foobar", CommandToken.OutputMode.HeaderValue, "Basic OmZvb2Jhcg==")]
        [TestCase("foobar", CommandToken.OutputMode.Header, "Authorization: Basic OmZvb2Jhcg==")]
        public void FormatToken_Basic(string input, CommandToken.OutputMode mode, string expected)
        {
            CommandToken.FormatToken(input, mode, Authorization.Basic).Should().Be(expected);
        }

        [TestCase("foobar", CommandToken.OutputMode.Token, "foobar")]
        [TestCase("foobar", CommandToken.OutputMode.HeaderValue, "Bearer foobar")]
        [TestCase("foobar", CommandToken.OutputMode.Header, "Authorization: Bearer foobar")]
        public void FormatToken_Bearer(string input, CommandToken.OutputMode mode, string expected)
        {
            CommandToken.FormatToken(input, mode, Authorization.Bearer).Should().Be(expected);
        }
    }
}
