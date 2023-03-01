// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test.Ado
{
    using FluentAssertions;

    using Microsoft.Authentication.AzureAuth.Ado;
    using Moq;
    using NUnit.Framework;

    public class AdoTokenAadTest
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void Foobar()
        {
            AdoToken.AccessToken().Should().NotBeEmpty();
        }
    }
}
