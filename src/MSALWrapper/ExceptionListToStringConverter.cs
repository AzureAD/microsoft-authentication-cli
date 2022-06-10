// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The exception list to string converter.
    /// </summary>
    public static class ExceptionListToStringConverter
    {
        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="exceptions">
        /// The exceptions.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string Execute(IEnumerable<Exception> exceptions)
        {
            if (exceptions == null || exceptions.Count() == 0)
            {
                return null;
            }

            return string.Join("\n", exceptions.Select(SingleLineException));
        }

        private static string SingleLineException(Exception ex)
        {
            if (ex != null)
            {
                var message = ex.Message
                                .Replace("\n", string.Empty)
                                .Replace("\r", string.Empty)
                                .Replace("\t", string.Empty);
                return $"{ex.GetType()}: {message}";
            }
            else
            {
                return "null";
            }
        }

        /// <summary>
        /// A method to serialize the exceptions to a JSON string.
        /// </summary>
        /// <param name="exceptions">The exceptions to be serialized.</param>
        /// <returns>Returns a JSON string.</returns>
        public static string SerializeExceptions(IList<Exception> exceptions)
        {
            return System.Text.Json.JsonSerializer.Serialize(exceptions);
        }
    }
}
