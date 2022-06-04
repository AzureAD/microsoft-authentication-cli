// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System.IO;
    using System.Linq;

    /// <summary>
    /// LegalFileNameChecker is a helper class to check if a filename is legal.
    /// </summary>
    internal static class LegalFileNameChecker
    {
        /// <summary>
        /// Check if the given file name valid.
        /// </summary>
        /// <param name="filename">the file name.</param>
        /// <returns>
        /// Whether the file name valid.
        /// </returns>
        public static bool IsValidFilename(this string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return false;
            }

            if (filename.Intersect(Path.GetInvalidFileNameChars()).Any())
            {
                return false;
            }

            return true;
        }
    }
}
