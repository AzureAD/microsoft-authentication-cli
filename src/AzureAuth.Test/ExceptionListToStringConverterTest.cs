// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Test
{
    using System;
    using System.Collections.Generic;
    using FluentAssertions;
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
            string expectedResult = @"[" +
                                        @"{""Message"":""This is an exception""," +
                                        @"""InnerException"":null," +
                                        @"""ExceptionType"":""System.Exception""," +
                                        @"""AADErrorCode"":null}" +
                                    @"]";

            // Assert
            result.Should().Be(expectedResult);
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
            string expectedStr = @"[" +
                                    @"{""Message"":null," +
                                    @"""InnerException"":null," +
                                    @"""ExceptionType"":null," +
                                    @"""AADErrorCode"":null}," +

                                     @"{""Message"":""This is the second exception""," +
                                    @"""InnerException"":null," +
                                    @"""ExceptionType"":""Microsoft.Identity.Client.MsalServiceException""," +
                                    @"""AADErrorCode"":""AADSTS2""}" +
                                 @"]";

            // Assert
            result.Should().Be(expectedStr);
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
            string expectedStr = @"[" +
                                    @"{""Message"":""This is the first exception""," +
                                    @"""InnerException"":null," +
                                    @"""ExceptionType"":""Microsoft.Identity.Client.MsalUiRequiredException""," +
                                    @"""AADErrorCode"":""AADSTS1""}," +

                                    @"{""Message"":""This is the second exception""," +
                                    @"""InnerException"":null," +
                                    @"""ExceptionType"":""Microsoft.Identity.Client.MsalServiceException""," +
                                    @"""AADErrorCode"":""AADSTS2""}," +

                                    @"{""Message"":""This is the third exception""," +
                                    @"""InnerException"":null," +
                                    @"""ExceptionType"":""Microsoft.Identity.Client.MsalClientException""," +
                                    @"""AADErrorCode"":""AADSTS3""}" +
                                 @"]";

            // Assert
            result.Should().Be(expectedStr);
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
            string expectedResult = @"[" +
                                        @"{""Message"":""This is the first \texception""," +
                                        @"""InnerException"":null," +
                                        @"""ExceptionType"":""Microsoft.Identity.Client.MsalUiRequiredException""," +
                                        @"""AADErrorCode"":""AADSTS1""}," +

                                        @"{""Message"":""This is the \nsecond exception\n""," +
                                        @"""InnerException"":null," +
                                        @"""ExceptionType"":""Microsoft.Identity.Client.MsalServiceException""," +
                                        @"""AADErrorCode"":""AADSTS2""}," +

                                        @"{""Message"":""This is the third exception\n""," +
                                        @"""InnerException"":null," +
                                        @"""ExceptionType"":""Microsoft.Identity.Client.MsalClientException""," +
                                        @"""AADErrorCode"":""AADSTS3""}" +
                                    @"]";

            // Assert
            result.Should().Be(expectedResult);
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
                new MsalServiceException("2", "This is the second exception", new MsalClientException("3", "This is the inner exception of the second exception")),
            };

            var exceptionString = ExceptionListToStringConverter.Execute(exceptionList);
            string expectedExceptionStr = @"[" +
                                        @"{""Message"":""This is the first exception""," +
                                        @"""InnerException"":null," +
                                        @"""ExceptionType"":""System.Exception""," +
                                        @"""AADErrorCode"":null}," +

                                        @"{""Message"":""This is the second exception""," +
                                        @"""InnerException"":" +
                                            @"{""Message"":""This is the inner exception of the second exception""," +
                                            @"""InnerException"":null," +
                                            @"""ExceptionType"":""Microsoft.Identity.Client.MsalClientException""," +
                                            @"""AADErrorCode"":""AADSTS3""}," +
                                        @"""ExceptionType"":""Microsoft.Identity.Client.MsalServiceException""," +
                                        @"""AADErrorCode"":""AADSTS2""}" +
                                    @"]";
            exceptionString.Should().Be(expectedExceptionStr);
        }

        [Test]
        public void ExceptionList_WithAggregateAndInnerExceptions()
        {
            List<Exception> exceptionList = new ()
            {
                new AggregateException(new Exception("Abra ca dabra")),
                new MsalServiceException("2", "This is the second exception", new MsalClientException("3", "This is the inner exception of the second exception")),
            };

            var exceptionString = ExceptionListToStringConverter.Execute(exceptionList);
            string expectedExceptionStr = @"[" +
                                            @"{""Message"":""One or more errors occurred. (Abra ca dabra)""," +
                                            @"""InnerException"":" +
                                                @"{""Message"":""Abra ca dabra""," +
                                                @"""InnerException"":null," +
                                                @"""ExceptionType"":""System.Exception""," +
                                                @"""AADErrorCode"":null}," +
                                            @"""ExceptionType"":""System.AggregateException""," +
                                            @"""AADErrorCode"":null}," +

                                            @"{""Message"":""This is the second exception""," +
                                            @"""InnerException"":" +
                                                @"{""Message"":""This is the inner exception of the second exception""," +
                                                @"""InnerException"":null," +
                                                @"""ExceptionType"":""Microsoft.Identity.Client.MsalClientException""," +
                                                @"""AADErrorCode"":""AADSTS3""}," +
                                            @"""ExceptionType"":""Microsoft.Identity.Client.MsalServiceException""," +
                                            @"""AADErrorCode"":""AADSTS2""}" +
                                        @"]";
            exceptionString.Should().Be(expectedExceptionStr);
        }

        [Test]
        public void ExceptionList_WithNullAndInnerExceptions()
        {
            List<Exception> exceptionList = new ()
            {
                new Exception("This is the first exception"),
                null,
                new Exception("This is the third exception", new Exception("This is the inner exception of the third exception")),
            };

            var exceptionString = ExceptionListToStringConverter.Execute(exceptionList);
            var expectedExceptionString = @"[" +
                                            @"{""Message"":""This is the first exception""," +
                                            @"""InnerException"":null," +
                                            @"""ExceptionType"":""System.Exception""," +
                                            @"""AADErrorCode"":null}," +

                                            @"{""Message"":null," +
                                            @"""InnerException"":null," +
                                            @"""ExceptionType"":null," +
                                            @"""AADErrorCode"":null}," +

                                            @"{""Message"":""This is the third exception""," +
                                            @"""InnerException"":" +
                                                @"{""Message"":""This is the inner exception of the third exception""," +
                                                @"""InnerException"":null," +
                                                @"""ExceptionType"":""System.Exception""," +
                                                @"""AADErrorCode"":null}," +
                                            @"""ExceptionType"":""System.Exception""," +
                                            @"""AADErrorCode"":null}" +
                                          @"]";

            exceptionString.Should().Be(expectedExceptionString);
        }

        [Test]
        public void ExceptionList_WithInnerExceptionHavingAnotherInnerException()
        {
            List<Exception> exceptionList = new ()
            {
                new Exception("This is the first exception"),
                new Exception("This is the second exception", new Exception("This is the inner exception of the second exception", new Exception("This is the inner exception of the inner exception"))),
            };

            var exceptionString = ExceptionListToStringConverter.Execute(exceptionList);
            var expectedExceptionString = @"[" +
                                            @"{""Message"":""This is the first exception""," +
                                            @"""InnerException"":null," +
                                            @"""ExceptionType"":""System.Exception""," +
                                            @"""AADErrorCode"":null}," +

                                            @"{""Message"":""This is the second exception""," +
                                            @"""InnerException"":" +
                                                @"{""Message"":""This is the inner exception of the second exception""," +
                                                @"""InnerException"":" +
                                                    @"{""Message"":""This is the inner exception of the inner exception""," +
                                                    @"""InnerException"":null," +
                                                    @"""ExceptionType"":""System.Exception""," +
                                                    @"""AADErrorCode"":null}," +
                                                @"""ExceptionType"":""System.Exception""," +
                                                @"""AADErrorCode"":null}," +
                                            @"""ExceptionType"":""System.Exception""," +
                                            @"""AADErrorCode"":null}" +
                                          @"]";
            exceptionString.Should().Be(expectedExceptionString);
        }

        [Test]
        public void ExceptionList_WithMSALExceptionsWithErrorCode()
        {
            List<Exception> exceptionList = new ()
            {
                new MsalServiceException("1", "This is an MSAL Service exception"),
                new MsalClientException("2", "This is an MSAL Client exception"),
                new MsalUiRequiredException("3", "This is an MSAL UI required exception"),
            };

            var exceptionString = ExceptionListToStringConverter.Execute(exceptionList);
            string expectedExceptionStr = @"[" +
                                            @"{""Message"":""This is an MSAL Service exception""," +
                                            @"""InnerException"":null," +
                                            @"""ExceptionType"":""Microsoft.Identity.Client.MsalServiceException""," +
                                            @"""AADErrorCode"":""AADSTS1""}," +

                                            @"{""Message"":""This is an MSAL Client exception""," +
                                            @"""InnerException"":null," +
                                            @"""ExceptionType"":""Microsoft.Identity.Client.MsalClientException""," +
                                            @"""AADErrorCode"":""AADSTS2""}," +

                                            @"{""Message"":""This is an MSAL UI required exception""," +
                                            @"""InnerException"":null," +
                                            @"""ExceptionType"":""Microsoft.Identity.Client.MsalUiRequiredException""," +
                                            @"""AADErrorCode"":""AADSTS3""}" +
                                        @"]";
            exceptionString.Should().Be(expectedExceptionStr);
        }
    }
}
