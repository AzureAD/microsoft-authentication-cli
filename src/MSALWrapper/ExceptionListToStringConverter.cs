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
        /// Executes the convertion.
        /// </summary>
        /// <param name="exceptions">/// The exceptions.</param>
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
    }
}
