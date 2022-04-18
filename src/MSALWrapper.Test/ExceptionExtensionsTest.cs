// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using FluentAssertions;
    using NUnit.Framework;

    internal class ExceptionExtensionsTest
    {
        [Test]
        public void GenericException_ToFormattedString()
        {
            string message = "An exception was thrown";
            Exception exception = new Exception(message);

            // Act
            string result = exception.ToFormattedString();

            // Assert
            result.Should().Be(message);
        }

        [Test]
        public void NullException_ToFormattedString()
        {
            Exception exception = null;
            Action subject = () => exception.ToFormattedString();
            subject.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void AggregateException_ToFormattedString()
        {
            Exception exception = new AggregateException(new Exception("Abra ca dabra"));

            // Act
            var exceptionMessage = exception.ToFormattedString();

            // Assert
            exceptionMessage.Should().Be("Abra ca dabra");
        }

        [Test]
        public void AggregateException_MatchingWhenClause_ToFormattedString()
        {
            string message = "Could not get access to the shared lock file";

            try
            {
                throw new AggregateException(new Exception(message));
            }
            catch (AggregateException ex) when (ex.InnerException.Message.Contains(message))
            {
                var exceptionMessage = ex.ToFormattedString();
                exceptionMessage.Should().Be(message);
            }
        }

        [Test]
        public void AggregateException_WithInnerExceptionUnAuthorizedAccessMessage_ToFormattedString()
        {
            var invalidOperationMessage = "Could not get access to the shared lock file.";
            var unAuthorizedAccessMessage = "Access to the path /Users/midoleng/.local/share/.IdentityService/msal.cache.lockfile is denied.";
            var ioMessage = "Permission denied";
            var messages = new List<string>()
            {
                invalidOperationMessage,
                unAuthorizedAccessMessage,
                ioMessage,
            };

            var invalidOperationException = new InvalidOperationException(invalidOperationMessage);
            var unAuthorizedAccessException = new UnauthorizedAccessException(unAuthorizedAccessMessage);
            var ioException = new IOException(ioMessage);
            AggregateException exception = new AggregateException(invalidOperationException, unAuthorizedAccessException, ioException);
            var expectedMessage = string.Join(Environment.NewLine, messages);

            // Act
            string result = exception.ToFormattedString();

            // Assert
            result.Should().Be(expectedMessage);
        }

        [Test]
        public void AggregateExceptionWithNoInnerException_ToFormattedString()
        {
            AggregateException exception = new AggregateException();
            var expectedMessage = string.Empty;

            // Act
            string result = exception.ToFormattedString();

            // Assert
            result.Should().Be(expectedMessage);
        }

        [Test]
        public void AggregateExceptionWithAnotherAggregateException_ToFormattedString()
        {
            var invalidOperationMessage = "Could not get access to the shared lock file.";
            var unAuthorizedAccessMessage = "Access to the path /Users/midoleng/.local/share/.IdentityService/msal.cache.lockfile is denied.";
            var ioMessage = "Permission denied";
            var messages = new List<string>()
            {
                invalidOperationMessage,
                unAuthorizedAccessMessage,
                ioMessage,
            };

            var invalidOperationException = new InvalidOperationException(invalidOperationMessage);
            var unAuthorizedAccessException = new UnauthorizedAccessException(unAuthorizedAccessMessage);
            var ioException = new IOException(ioMessage);
            AggregateException exception = new AggregateException(
                invalidOperationException,
                new AggregateException(unAuthorizedAccessException, ioException));
            var expectedMessage = string.Join(Environment.NewLine, messages);

            // Act
            string result = exception.ToFormattedString();

            // Assert
            result.Should().Be(expectedMessage);
        }

        [Test]
        public void NullException_GetAllExceptions()
        {
            Exception exception = null;
            Action subject = () => exception.GetAllExceptions().ToList();
            subject.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void ExceptionWithInnerException_GetAllExceptions()
        {
            var innerExceptionMessage = "This is an inner exception message.";
            var innerException = new InvalidOperationException(innerExceptionMessage);
            string message = "An inner exception was thrown";
            Exception exception = new Exception(message, innerException);
            var expected = new List<Exception>()
            {
                exception,
                innerException,
            };

            // Act
            var result = exception.GetAllExceptions().ToList();

            // Assert
            result.Should().BeEquivalentTo(expected);
        }
    }
}
