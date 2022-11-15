// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The exception list to string converter.
    /// </summary>
    public static class ExceptionListToStringConverter
    {
        /// <summary>
        /// Extracts correlation IDs from the list of exceptions.
        /// </summary>
        /// <param name="exceptions">List of exceptions from which correlation IDs are extracted.</param>
        /// <returns>List of correlation IDs.</returns>
        public static List<string> ExtractCorrelationIDsFromException(IList<Exception> exceptions)
        {
            var correlationIDs = new List<string>();

            if (exceptions == null)
            {
                return null;
            }

            foreach (Exception exception in exceptions)
            {
                if (exception.GetType() == typeof(MsalServiceException))
                {
                    var msalServiceException = (MsalServiceException)exception;
                    if (msalServiceException.CorrelationId != null)
                    {
                        correlationIDs.Add(msalServiceException.CorrelationId);
                    }
                }
                else if (exception.GetType() == typeof(MsalUiRequiredException))
                {
                    var msalUiRequiredException = (MsalUiRequiredException)exception;
                    if (msalUiRequiredException.CorrelationId != null)
                    {
                        correlationIDs.Add(msalUiRequiredException.CorrelationId);
                    }
                }
            }

            return correlationIDs;
        }

        /// <summary>
        /// Executes the convertion of exceptions to a string.
        /// </summary>
        /// <param name="exceptions">The exceptions.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string Execute(IEnumerable<Exception> exceptions)
        {
            if (exceptions == null || exceptions.Count() == 0)
            {
                return null;
            }

            return string.Join("\n", exceptions.Select(SingleLineException));
        }

        /// <summary>
        /// Converts exceptions to a single string.
        /// </summary>
        /// <param name="ex">The exceptions.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private static string SingleLineException(Exception ex)
        {
            if (ex != null)
            {
                IEnumerable<Exception> innerExceptions = ExceptionExtensions.GetAllExceptions(ex);
                var messages = new List<string>();

                foreach (Exception innerException in innerExceptions)
                {
                    var message = innerException.Message
                                    .Replace("\n", string.Empty)
                                    .Replace("\r", string.Empty)
                                    .Replace("\t", string.Empty);
                    messages.Add($"{innerException.GetType()}: {message}");
                }

                return string.Join("\n", messages);
            }
            else
            {
                return "null";
            }
        }
    }
}
