// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace MSALWrapper.Test.AuthFlow
{
    using System.Collections.Generic;
    using System.Linq;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;

    using NUnit.Framework;

    internal class AuthFlowFactoryTest
    {
        [Test]
        public void JustBroker()
        {
            IEnumerable<IAuthFlow> subject = AuthFlowFactory.Something(AuthMode.Broker);

            subject.Should().HaveCount(1);
            subject.First().GetType().Name.Should().Be(typeof(Broker).Name);
        }
    }
}
