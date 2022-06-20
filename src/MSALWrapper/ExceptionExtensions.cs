// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Exceptions extensions.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Convert exception to formatted string.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The <see cref="string"/>.</returns>
        /// <exception cref="ArgumentNullException">.</exception>
        public static string ToFormattedString(this Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("Cannot pass null to the exception extension method.");
            }

            IEnumerable<string> messages = exception
                .GetAllExceptions()
                .Where(e => !string.IsNullOrWhiteSpace(e.Message))
                .Select(e => e.Message.Trim());

            var flattened = string.Join(Environment.NewLine, messages);
            return flattened;
        }

        /// <summary>
        /// Extracts all inner exceptions.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The <see cref="IEnumerable"/>.</returns>
        /// <exception cref="ArgumentNullException">Argument Null Exception.</exception>
        public static IEnumerable<Exception> GetAllExceptions(this Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("Cannot pass null to the exception extension method.");
            }

            if (!(exception is AggregateException))
            {
                yield return exception;
            }

            if (exception is AggregateException aggregateException)
            {
                foreach (Exception innerEx in aggregateException.InnerExceptions.SelectMany(e => e.GetAllExceptions()))
                {
                    yield return innerEx;
                }
            }
            else if (exception.InnerException != null)
            {
                foreach (Exception innerEx in exception.InnerException.GetAllExceptions())
                {
                    yield return innerEx;
                }
            }
        }
    }
}
