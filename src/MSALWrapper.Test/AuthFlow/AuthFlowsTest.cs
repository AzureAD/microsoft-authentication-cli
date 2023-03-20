// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
            subject.LockName.Should().Be($"{client}_{tenant}");
        }

        [Test]
        public void Is_Iterable()
        {
            IEnumerable<IAuthFlow> flows = new List<IAuthFlow>();
            var subject = new AuthFlows(Guid.NewGuid(), Guid.NewGuid(), flows);
            // Use LINQ Count() to forces us to implement IEnumerable<IAuthFlow>
            subject.Count().Should().Be(0);
        }
    }
}
