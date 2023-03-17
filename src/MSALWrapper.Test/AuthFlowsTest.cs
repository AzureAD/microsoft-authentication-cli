// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper.AuthFlow;

    using NUnit.Framework;

    internal class AuthFlowsTest
    {
        [Test]
        public void AuthFlows_Has_Id()
        {
            var client = Guid.NewGuid();
            var tenant = Guid.NewGuid();

            var subject = new AuthFlows(client, tenant, new List<IAuthFlow>());

            subject.Id.Should().Be($"{client}_{tenant}");
        }
    }
}
