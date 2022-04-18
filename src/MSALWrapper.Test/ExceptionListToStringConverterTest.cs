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
        [SetUp]
        public void Setup()
        {
        }

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
    }
}
