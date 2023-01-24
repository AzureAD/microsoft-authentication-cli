// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Test
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using FluentAssertions;
    using Microsoft.Identity.Client;
    using NUnit.Framework;
    using static Microsoft.Authentication.AzureAuth.ExceptionListToStringConverter;

    public class ExceptionListToStringConverterTest
    {
        [Test]
        public void EmptyExceptionList()
        {
            IEnumerable<Exception> exceptions = new List<Exception>();

            // Act
            string result = ExceptionListToStringConverter.Execute(exceptions);

            // Assert
            result.Should().Be("[]");
        }

        [Test]
        public void NullExceptionList()
        {
            IEnumerable<Exception> exceptions = null;

            // Act
            Action subject = () => ExceptionListToStringConverter.Execute(exceptions);

            // Assert
            subject.Should().Throw<NullReferenceException>();
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
            SerializableException expectedResult = new SerializableException()
            {
                Message = "This is an exception",
                ExceptionType = "System.Exception",
            };

            // Assert
            var result_deserialized = JsonSerializer.Deserialize<List<SerializableException>>(result);
            result_deserialized[0].Should().BeEquivalentTo(expectedResult);
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
            Action subject = () => ExceptionListToStringConverter.Execute(exceptions);

            // Assert
            subject.Should().Throw<ArgumentNullException>();
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
            IEnumerable<SerializableException> expectedResult = new List<SerializableException>()
            {
                new SerializableException()
                {
                    Message = "This is the first exception",
                    ExceptionType = "Microsoft.Identity.Client.MsalUiRequiredException",
                    AADErrorCode = "AADSTS1",
                },
                new SerializableException()
                {
                    AADErrorCode = "AADSTS2",
                    ExceptionType = "Microsoft.Identity.Client.MsalServiceException",
                    Message = "This is the second exception",
                },
                new SerializableException()
                {
                    AADErrorCode = "AADSTS3",
                    ExceptionType = "Microsoft.Identity.Client.MsalClientException",
                    Message = "This is the third exception",
                },
            };

            // Assert
            var result_deserialized = JsonSerializer.Deserialize<List<SerializableException>>(result);
            result_deserialized.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void ExceptionList_WithExceptionsContainingNewlinesAndTabs()
        {
            IEnumerable<Exception> exceptions = new List<Exception>()
            {
                new MsalUiRequiredException("1", "This is the first \texception"),
                new MsalServiceException("2", "This is the \r\nsecond exception\n"),
                new MsalClientException("3", "This\r is the third exception\n\r"),
            };

            // Act
            string result = ExceptionListToStringConverter.Execute(exceptions);
            IEnumerable<SerializableException> expectedResult = new List<SerializableException>()
            {
                new SerializableException()
                {
                    Message = "This is the first \texception",
                    ExceptionType = "Microsoft.Identity.Client.MsalUiRequiredException",
                    AADErrorCode = "AADSTS1",
                },
                new SerializableException()
                {
                    AADErrorCode = "AADSTS2",
                    ExceptionType = "Microsoft.Identity.Client.MsalServiceException",
                    Message = "This is the \nsecond exception\n",
                },
                new SerializableException()
                {
                    AADErrorCode = "AADSTS3",
                    ExceptionType = "Microsoft.Identity.Client.MsalClientException",
                    Message = "This is the third exception\n",
                },
            };

            // Assert
            var result_deserialized = JsonSerializer.Deserialize<List<SerializableException>>(result);
            result_deserialized.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void ExceptionList_WithInnerExceptions()
        {
            List<Exception> exceptionList = new ()
            {
                new Exception("This is the first exception"),
                new MsalServiceException(
                    "2",
                    "This is the second exception",
                    new MsalClientException(
                        "3",
                        "This is the inner exception of the second exception")),
            };

            var result = ExceptionListToStringConverter.Execute(exceptionList);
            IEnumerable<SerializableException> expectedResult = new List<SerializableException>()
            {
                new SerializableException()
                {
                    Message = "This is the first exception",
                    ExceptionType = "System.Exception",
                },
                new SerializableException()
                {
                    AADErrorCode = "AADSTS2",
                    ExceptionType = "Microsoft.Identity.Client.MsalServiceException",
                    Message = "This is the second exception",
                    InnerException = new SerializableException()
                    {
                        AADErrorCode = "AADSTS3",
                        ExceptionType = "Microsoft.Identity.Client.MsalClientException",
                        Message = "This is the inner exception of the second exception",
                    },
                },
            };

            // Assert
            var result_deserialized = JsonSerializer.Deserialize<List<SerializableException>>(result);
            result_deserialized.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void ExceptionList_WithAggregateAndInnerExceptions()
        {
            List<Exception> exceptionList = new ()
            {
                new AggregateException(new Exception("Abra ca dabra")),
                new MsalServiceException(
                    "2",
                    "This is the second exception",
                    new MsalClientException(
                        "3",
                        "This is the inner exception of the second exception")),
            };

            var result = ExceptionListToStringConverter.Execute(exceptionList);
            IEnumerable<SerializableException> expectedResult = new List<SerializableException>()
            {
                new SerializableException()
                {
                    Message = "One or more errors occurred. (Abra ca dabra)",
                    ExceptionType = "System.AggregateException",
                    InnerException = new SerializableException()
                    {
                        ExceptionType = "System.Exception",
                        Message = "Abra ca dabra",
                    },
                },
                new SerializableException()
                {
                    AADErrorCode = "AADSTS2",
                    ExceptionType = "Microsoft.Identity.Client.MsalServiceException",
                    Message = "This is the second exception",
                    InnerException = new SerializableException()
                    {
                        AADErrorCode = "AADSTS3",
                        ExceptionType = "Microsoft.Identity.Client.MsalClientException",
                        Message = "This is the inner exception of the second exception",
                    },
                },
            };

            // Assert
            var result_deserialized = JsonSerializer.Deserialize<List<SerializableException>>(result);
            result_deserialized.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void ExceptionList_WithInnerExceptionHavingAnotherInnerException()
        {
            List<Exception> exceptionList = new ()
            {
                new Exception("This is the first exception"),
                new Exception(
                    "This is the second exception",
                    new Exception(
                        "This is the inner exception of the second exception",
                        new Exception("This is the inner exception of the inner exception"))),
            };

            var result = ExceptionListToStringConverter.Execute(exceptionList);
            IEnumerable<SerializableException> expectedResult = new List<SerializableException>()
            {
                new SerializableException()
                {
                    Message = "This is the first exception",
                    ExceptionType = "System.Exception",
                },
                new SerializableException()
                {
                    ExceptionType = "System.Exception",
                    Message = "This is the second exception",
                    InnerException = new SerializableException()
                    {
                        ExceptionType = "System.Exception",
                        Message = "This is the inner exception of the second exception",
                        InnerException = new SerializableException()
                        {
                            ExceptionType = "System.Exception",
                            Message = "This is the inner exception of the inner exception",
                        },
                    },
                },
            };

            // Assert
            var result_deserialized = JsonSerializer.Deserialize<List<SerializableException>>(result);
            result_deserialized.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void ExceptionList_WithMSALExceptionsWithErrorCode()
        {
            List<Exception> exceptions = new ()
            {
                new MsalServiceException("1", "This is an MSAL Service exception"),
                new MsalClientException("2", "This is an MSAL Client exception"),
                new MsalUiRequiredException("3", "This is an MSAL UI required exception"),
            };
            string result = ExceptionListToStringConverter.Execute(exceptions);
            IEnumerable<SerializableException> expectedResult = new List<SerializableException>()
            {
                new SerializableException()
                {
                    Message = "This is an MSAL Service exception",
                    ExceptionType = "Microsoft.Identity.Client.MsalServiceException",
                    AADErrorCode = "AADSTS1",
                },
                new SerializableException()
                {
                    AADErrorCode = "AADSTS2",
                    ExceptionType = "Microsoft.Identity.Client.MsalClientException",
                    Message = "This is an MSAL Client exception",
                },
                new SerializableException()
                {
                    AADErrorCode = "AADSTS3",
                    ExceptionType = "Microsoft.Identity.Client.MsalUiRequiredException",
                    Message = "This is an MSAL UI required exception",
                },
            };

            // Assert
            var result_deserialized = JsonSerializer.Deserialize<List<SerializableException>>(result);
            result_deserialized.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void ExceptionList_WithMSALExceptionsWithCorrelationIDs()
        {
            var correlationID1 = Guid.NewGuid().ToString();
            var correlationID2 = Guid.NewGuid().ToString();
            var msalServiceException = new MsalServiceException("1", "An MSAL Service Exception message");
            msalServiceException.CorrelationId = correlationID1;

            var msalUIRequiredException = new MsalUiRequiredException("2", "An MSAL UI Required Exception message");
            msalUIRequiredException.CorrelationId = correlationID2;

            var msalUIRequiredExceptionNoCorrelationId = new MsalUiRequiredException("3", "An MSAL UI Required Exception message without correlation ID");

            List<Exception> exceptions = new ()
            {
                msalServiceException,
                msalUIRequiredException,
                msalUIRequiredExceptionNoCorrelationId,
            };

            string result = ExceptionListToStringConverter.Execute(exceptions);
            IEnumerable<SerializableException> expectedResult = new List<SerializableException>()
            {
                new SerializableException()
                {
                    Message = "An MSAL Service Exception message",
                    ExceptionType = "Microsoft.Identity.Client.MsalServiceException",
                    AADErrorCode = "AADSTS1",
                    CorrelationId = correlationID1,
                },
                new SerializableException()
                {
                    AADErrorCode = "AADSTS2",
                    ExceptionType = "Microsoft.Identity.Client.MsalUiRequiredException",
                    Message = "An MSAL UI Required Exception message",
                    CorrelationId = correlationID2,
                },
                new SerializableException()
                {
                    AADErrorCode = "AADSTS3",
                    ExceptionType = "Microsoft.Identity.Client.MsalUiRequiredException",
                    Message = "An MSAL UI Required Exception message without correlation ID",
                },
            };

            // Assert
            var result_deserialized = JsonSerializer.Deserialize<List<SerializableException>>(result);
            result_deserialized.Should().BeEquivalentTo(expectedResult);
        }
    }
}
