// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Windows.Forms;
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
        /// Converts exceptions to JSON string.
        /// </summary>
        /// <param name="exceptions">List of exceptions.</param>
        /// <returns>JSON format of exception list.</returns>
        public static string Execute(IEnumerable<Exception> exceptions)
        {
            if (exceptions == null || exceptions.Count() == 0)
            {
                return null;
            }

            List<CustomException> customExceptions = new List<CustomException>();
            foreach (Exception exception in exceptions)
            {
                var customException = new CustomException(exception);
                customExceptions.Add(customException);
            }

            return JsonSerializer.Serialize(customExceptions);
        }
    }

    /// <summary>
    /// CustomException class with only select properties of Exception class.
    /// </summary>
    internal class CustomException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomException"/> class.
        /// </summary>
        /// <param name="exception">Exception.</param>
        public CustomException(Exception exception)
        {
            if (exception?.Message != null)
            {
                var singleLineMessage = exception.Message
                                    .Replace("\n", string.Empty)
                                    .Replace("\r", string.Empty)
                                    .Replace("\t", string.Empty);
                this.Message = $"{exception.GetType()}: {singleLineMessage}";
            }

            if (exception?.InnerException != null)
            {
                this.InnerException = new CustomException(exception.InnerException);
            }
        }

        /// <summary>Gets or sets Message.</summary>
        public string Message { get; set; }

        /// <summary>Gets or sets InnerException.</summary>
        public CustomException InnerException { get; set; }
    }
}
