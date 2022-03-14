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

    /// <summary>
    /// The exception extensions test.
    /// </summary>
    internal class ExceptionExtensionsTest
    {
        /// <summary>
        /// The generic exception_ to formatted string.
        /// </summary>
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

        /// <summary>
        /// The null exception_ to formatted string.
        /// </summary>
        [Test]
        public void NullException_ToFormattedString()
        {
            Exception exception = null;
            Action subject = () => exception.ToFormattedString();
            subject.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// The aggregate exception_ to formatted string.
        /// </summary>
        [Test]
        public void AggregateException_ToFormattedString()
        {
            Exception exception = new AggregateException(new Exception("Abra ca dabra"));

            // Act
            var exceptionMessage = exception.ToFormattedString();

            // Assert
            exceptionMessage.Should().Be("Abra ca dabra");
        }

        /// <summary>
        /// The aggregate exception matching when clause_ to formatted string.
        /// </summary>
        /// <exception cref="AggregateException">
        /// The Aggregate Exception.
        /// </exception>
        [Test]
        public void AggregateExceptionMatchingWhenClause_ToFormattedString()
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

        /// <summary>
        /// The aggregate exception with inner exception un authorized access message_ to formatted string.
        /// </summary>
        [Test]
        public void AggregateExceptionWithInnerExceptionUnAuthorizedAccessMessage_ToFormattedString()
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

        /// <summary>
        /// The aggregate exception with no inner exception_ to formatted string.
        /// </summary>
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

        /// <summary>
        /// The aggregate exception with another aggregate exception_ to formatted string.
        /// </summary>
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

        /// <summary>
        /// The null exception_ get all exceptions.
        /// </summary>
        [Test]
        public void NullException_GetAllExceptions()
        {
            Exception exception = null;
            Action subject = () => exception.GetAllExceptions().ToList();
            subject.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// The exception with inner exception_ get all exceptions.
        /// </summary>
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
