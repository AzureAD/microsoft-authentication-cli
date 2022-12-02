// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
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
        /// Executes the convertion of exceptions to a JSON string.
        /// </summary>
        /// <param name="exceptions">List of exceptions.</param>
        /// <returns>JSON format of exception list.</returns>
        public static string Execute(IEnumerable<Exception> exceptions)
        {
            if (exceptions == null || exceptions.Count() == 0)
            {
                return null;
            }

            List<SerializableException> customExceptions = new List<SerializableException>();
            foreach (Exception exception in exceptions)
            {
                var customException = new SerializableException(exception);
                customExceptions.Add(customException);
            }

            return JsonSerializer.Serialize(customExceptions);
        }
    }

    /// <summary>
    /// CustomException class with only select properties of Exception class.
    /// </summary>
    internal class SerializableException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableException"/> class.
        /// </summary>
        /// <param name="exception">Exception.</param>
        public SerializableException(Exception exception)
        {
            if (exception != null)
            {
                if (exception.Message != null)
                {
                    this.Message = exception.Message.Replace("\r", string.Empty);
                }

                if (exception.InnerException != null)
                {
                    this.InnerException = new SerializableException(exception.InnerException);
                }

                var exceptionType = exception.GetType();
                if (exceptionType == typeof(MsalClientException) ||
                    exceptionType == typeof(MsalServiceException) ||
                    exceptionType == typeof(MsalUiRequiredException))
                {
                    var msalException = (MsalException)exception;
                    if (msalException.ErrorCode != null)
                    {
                        // AAD error codes have the prefix AADSTS to the `ErrorCode` property of an `MsalException` class.
                        // See https://learn.microsoft.com/en-us/azure/active-directory/develop/reference-aadsts-error-codes
                        this.AADErrorCode = $"AADSTS{msalException.ErrorCode}";
                    }
                }

                this.ExceptionType = $"{exceptionType}";
            }
        }

        /// <summary>Gets or sets Message.</summary>
        public string Message { get; set; }

        /// <summary>Gets or sets InnerException.</summary>
        public SerializableException InnerException { get; set; }

        /// <summary>Gets or sets Type.</summary>
        /// Note: This property is intentionally made of type `string` instead of `System.Type`.
        /// This is because `System.Type` instances cannot be serialized using `System.text.JSONSerializer` for security reasons.
        /// For more information, see https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/dataset-datatable-dataview/security-guidance
        public string ExceptionType { get; set; }

        /// <summary>Gets or sets AADErrorCode.</summary>
        public string AADErrorCode { get; set; }
    }
}
