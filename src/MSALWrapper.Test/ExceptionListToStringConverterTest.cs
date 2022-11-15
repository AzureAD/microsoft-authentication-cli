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
        public void EmptyExceptionList()
        {
            IEnumerable<Exception> exceptions = new List<Exception>();

            // Act
            string result = ExceptionListToStringConverter.Execute(exceptions);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public void NullExceptionList()
        {
            IEnumerable<Exception> exceptions = null;

            // Act
            string result = ExceptionListToStringConverter.Execute(exceptions);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public void ExceptionList_WithOneException()
        {
            IEnumerable<Exception> exceptions = new List<Exception>()
            {
                new Exception("This is an exception"),
            };

            // Act
            string result = ExceptionListToStringConverter.Execute(exceptions);

            // Assert
            result.Should().Be("System.Exception: This is an exception");
        }

        [Test]
        public void ExceptionList_WithNullValuesinExceptionList()
        {
            IEnumerable<Exception> exceptions = new List<Exception>()
            {
                null,
                new MsalServiceException("2", "This is the second exception"),
            };

            // Act
            string result = ExceptionListToStringConverter.Execute(exceptions);

            // Assert
            result.Should().Be("null\n" +
                "Microsoft.Identity.Client.MsalServiceException: This is the second exception");
        }

        [Test]
        public void ExceptionList_WithManyExceptions()
        {
            IEnumerable<Exception> exceptions = new List<Exception>()
            {
                new MsalUiRequiredException("1", "This is the first exception"),
                new MsalServiceException("2", "This is the second exception"),
                new MsalClientException("3", "This is the third exception"),
            };

            // Act
            string result = ExceptionListToStringConverter.Execute(exceptions);

            // Assert
            result.Should().Be(
                "Microsoft.Identity.Client.MsalUiRequiredException: This is the first exception\n" +
                "Microsoft.Identity.Client.MsalServiceException: This is the second exception\n" +
                "Microsoft.Identity.Client.MsalClientException: This is the third exception");
        }

        [Test]
        public void ExceptionList_WithExceptionsContainingNewlines()
        {
            IEnumerable<Exception> exceptions = new List<Exception>()
            {
                new MsalUiRequiredException("1", "This is the first exception"),
                new MsalServiceException("2", "This is the \r\nsecond exception\n"),
                new MsalClientException("3", "This\r is the third exception\n\r"),
            };

            // Act
            string result = ExceptionListToStringConverter.Execute(exceptions);

            // Assert
            result.Should().Be(
                "Microsoft.Identity.Client.MsalUiRequiredException: This is the first exception\n" +
                "Microsoft.Identity.Client.MsalServiceException: This is the second exception\n" +
                "Microsoft.Identity.Client.MsalClientException: This is the third exception");
        }

        [Test]
        public void ExceptionList_WithExceptionsContainingTabs()
        {
            IEnumerable<Exception> exceptions = new List<Exception>()
            {
                new Exception("\tThis is the first exception"),
                new Exception("This is the \tsecond exception"),
                new Exception("This is the third exception\t"),
            };

            // Act
            string result = ExceptionListToStringConverter.Execute(exceptions);

            // Assert
            result.Should().Be("System.Exception: This is the first exception\n" +
                "System.Exception: This is the second exception\n" +
                "System.Exception: This is the third exception");
        }

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

        [Test]
        public void ExceptionList_WithInnerExceptions()
        {
            List<Exception> exceptionList = new ()
            {
                new Exception("This is the first exception"),
                new MsalServiceException("2", "This is the second exception", new MsalClientException("3", "This is the inner exception of second exception")),
            };
            var exceptionString = ExceptionListToStringConverter.Execute(exceptionList);

            exceptionString.Should().Be("System.Exception: This is the first exception\n" +
                "Microsoft.Identity.Client.MsalServiceException: This is the second exception\n" +
                "Microsoft.Identity.Client.MsalClientException: This is the inner exception of second exception");
        }
    }
}
