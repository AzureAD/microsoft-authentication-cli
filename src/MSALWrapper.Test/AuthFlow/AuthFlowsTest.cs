// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper.AuthFlow;

    using NUnit.Framework;

    internal class AuthFlowsTest
    {
        [Test]
        public void Requires_Flows()
        {
            Action subject = () => new AuthFlows(Guid.NewGuid(), Guid.NewGuid(), null);
            subject.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null. (Parameter 'authFlows')");
        }

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
