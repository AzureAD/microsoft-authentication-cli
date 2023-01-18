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
        /// Executes the convertion of exceptions to a JSON string.
        /// </summary>
        /// <param name="exceptions">List of exceptions.</param>
        /// <returns>JSON format of exception list.</returns>
        public static string Execute(IEnumerable<Exception> exceptions)
        {
            List<SerializableException> serializableExceptions = new List<SerializableException>();
            foreach (Exception exception in exceptions)
            {
                var customException = new SerializableException(exception);
                serializableExceptions.Add(customException);
            }

            return JsonSerializer.Serialize(serializableExceptions);
        }

        /// <summary>
        /// CustomException class with only select properties of Exception class.
        /// </summary>
        public class SerializableException
        {
            private string message;

            /// <summary>
            /// Initializes a new instance of the <see cref="SerializableException"/> class.
            /// </summary>
            /// <param name="exception">Exception.</param>
            public SerializableException(Exception exception)
            {
                if (exception == null)
                {
                    throw new ArgumentNullException(nameof(exception));
                }

                if (exception.Message != null)
                {
                    this.Message = exception.Message;
                }

                if (exception.InnerException != null)
                {
                    // This is a recursive call where the InnerExceptions are converted to SerializableExceptions until no InnerExceptions are found.
                    this.InnerException = new SerializableException(exception.InnerException);
                }

                this.ExceptionType = $"{exception.GetType()}";
                this.SetAADErrorCode(exception);
                this.SetCorrelationID(exception);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="SerializableException"/> class without intitializing the properties.
            /// </summary>
            public SerializableException()
            {
            }

            /// <summary>Gets or sets Message.</summary>
            public string Message
            {
                get => this.message;
                set
                {
                    this.message = value.Replace("\r", string.Empty);
                }
            }

            /// <summary>Gets or sets InnerException.</summary>
            public SerializableException InnerException { get; set; }

            /// <summary>Gets or sets Type.</summary>
            /// Note: This property is intentionally made of type `string` instead of `System.Type`.
            /// This is because `System.Type` instances cannot be serialized using `System.text.JSONSerializer` for security reasons.
            /// For more information, see https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/dataset-datatable-dataview/security-guidance
            public string ExceptionType { get; set; }

            /// <summary>Gets or Sets AADErrorCode.</summary>
            public string AADErrorCode { get; set; }

            /// <summary>Gets or Sets CorrelationIds.</summary>
            public string CorrelationId { get; set; }

            /// <summary>
            /// Sets the error code.
            /// </summary>
            /// <param name="exception">exception from which error code is extracted</param>
            private void SetAADErrorCode(Exception exception)
            {
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
            }

            /// <summary>
            /// Extracts correlation ID from the exception.
            /// </summary>
            /// <param name="exception">exception from which correlation IDs are extracted.</param>
            private void SetCorrelationID(Exception exception)
            {
                if (exception.GetType() == typeof(MsalServiceException))
                {
                    var msalServiceException = (MsalServiceException)exception;
                    if (msalServiceException.CorrelationId != null)
                    {
                        this.CorrelationId = msalServiceException.CorrelationId;
                    }
                }
                else if (exception.GetType() == typeof(MsalUiRequiredException))
                {
                    var msalUiRequiredException = (MsalUiRequiredException)exception;
                    if (msalUiRequiredException.CorrelationId != null)
                    {
                        this.CorrelationId = msalUiRequiredException.CorrelationId;
                    }
                }
            }
        }
    }
}
