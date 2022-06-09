// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System.IO;
    using System.Linq;

    /// <summary>
    /// <see cref="LegalFileChecker"/> is a helper class to check if a filename is legal.
    /// </summary>
    internal static class LegalFileChecker
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

        /// <summary>
        /// Check if the given file path valid.
        /// </summary>
        /// <param name="filePath">the file path.</param>
        /// <returns>
        /// Whether the file name valid.
        /// </returns>
        public static bool IsValidFilePath(this string filePath)
        {
            try
            {
                new FileInfo(filePath);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
