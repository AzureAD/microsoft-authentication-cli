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
    }
}
