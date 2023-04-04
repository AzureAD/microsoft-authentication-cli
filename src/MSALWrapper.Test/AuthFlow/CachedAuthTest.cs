// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;

    using Moq;

    using NUnit.Framework;

    using System;
    using System.Collections.Generic;

    internal class CachedAuthTest
    {
        private ILogger logger;
        private Mock<IPCAWrapper> pcaWrapper;

        [SetUp]
        public void Setup()
        {
            (this.logger, _) = MemoryLogger.Create();
            this.pcaWrapper = new Mock<IPCAWrapper>(MockBehavior.Strict);
        }

        [Test]
        public void Null_Account_Returns_Null_Without_Errors()
        {
            IList<Exception> errors = new List<Exception>();
            var subject = CachedAuth.TryCachedAuthAsync(
                this.logger,
                TimeSpan.FromSeconds(1),
                new[] { "scope" },
                null,
                this.pcaWrapper.Object,
                errors).Result;

            subject.Should().BeNull();
            errors.Should().BeEmpty();
        }
    }
}
