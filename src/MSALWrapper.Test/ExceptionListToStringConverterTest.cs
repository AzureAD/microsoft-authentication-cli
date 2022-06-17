// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
    using FluentAssertions;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Identity.Client;
    using NUnit.Framework;

    public class ExceptionListToStringConverterTest
    {
        [Test]
        public void ExtractCorrelationIDs_FromEmptyExceptionList()
        {
            List<Exception> exceptions = new List<Exception>();

            // Act
            var correlationIDs = ExceptionListToStringConverter.ExtractCorrelationIDsFromException(exceptions);

            // Assert
            correlationIDs.Should().BeEmpty();
        }

        [Test]
        public void ExtractCorrelationIDs_FromNullExceptionList()
        {
            List<Exception> exceptions = null;

            // Act
            var correlationIDs = ExceptionListToStringConverter.ExtractCorrelationIDsFromException(exceptions);

            // Assert
            correlationIDs.Should().BeNull();
        }

        [Test]
        public void ExtractCorrelationIDs_FromNonMSALExceptionList()
        {
            List<Exception> exceptions = new List<Exception>()
            {
                new Exception("Exception1"),
                new Exception("Exception2"),
            };

            // Act
            var correlationIDs = ExceptionListToStringConverter.ExtractCorrelationIDsFromException(exceptions);

            // Assert
            correlationIDs.Should().BeEmpty();
        }

        [Test]
        public void ExtractCorrelationIDs_FromMSALExceptionList()
        {
            var correlationID1 = Guid.NewGuid().ToString();
            var correlationID2 = Guid.NewGuid().ToString();
            var msalServiceException = new MsalServiceException("errorcode", "An MSAL Service Exception message");
            msalServiceException.CorrelationId = correlationID1;

            var msalUIRequiredException = new MsalUiRequiredException("errorcode", "An MSAL UI Required Exception message");
            msalUIRequiredException.CorrelationId = correlationID2;

            var exceptions = new List<Exception>()
            {
                msalServiceException,
                msalUIRequiredException,
            };

            var expectedCorrelationIDs = new List<string>()
            {
                correlationID1,
                correlationID2,
            };

            // Act
            var correlationIDs = ExceptionListToStringConverter.ExtractCorrelationIDsFromException(exceptions);

            // Assert
            correlationIDs.Should().BeEquivalentTo(expectedCorrelationIDs);
        }

        [Test]
        public void ExtractCorrelationIDs_FromMSALExceptionList_WithOneNullCorrelationID()
        {
            var correlationID1 = Guid.NewGuid().ToString();
            var msalServiceException = new MsalServiceException("errorcode", "An MSAL Service Exception message");
            msalServiceException.CorrelationId = correlationID1;

            var msalUIRequiredException = new MsalUiRequiredException("errorcode", "An MSAL UI Required Exception message");
            msalUIRequiredException.CorrelationId = null;

            var exceptions = new List<Exception>()
            {
                msalServiceException,
                msalUIRequiredException,
            };

            var expectedCorrelationIDs = new List<string>()
            {
                correlationID1,
            };

            // Act
            var correlationIDs = ExceptionListToStringConverter.ExtractCorrelationIDsFromException(exceptions);

            // Assert
            correlationIDs.Should().BeEquivalentTo(expectedCorrelationIDs);
        }
    }
}
