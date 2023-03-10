// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    using System;
    using System.Text;

    /// <summary>
    /// <see cref="string"/> extension methods for formatting tokens.
    /// </summary>
    public static class TokenFormatting
    {
        private const string AuthorizationHeader = "Authorization:";
        private const string Basic = "Basic";
        private const string Bearer = "Bearer";

        /// <summary>
        /// Base64 encode <paramref name="value"/> adding padding (=) characters as needed.
        /// </summary>
        /// <param name="value">Value to base64 encode.</param>
        /// <returns>Base64 encoded value.</returns>
        public static string Base64(this string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Create an Authorization Header.
        /// Handles base64 encoding of <paramref name="value"/> for <see cref="Authorization.Basic"/> credentials.
        /// </summary>
        /// <param name="value">The credential.</param>
        /// <param name="scheme"><see cref="Authorization"/> scheme.</param>
        /// <returns>A full Authorization header using the Basic scheme.</returns>
        public static string AsHeader(this string value, Authorization scheme)
        {
            return string.Join(' ', new[] { AuthorizationHeader, scheme.AsString(), scheme.FormatValue(value) });
        }

        /// <summary>
        /// Create an Authorization Header Value (without the Authorization prefix/key).
        /// Handles base64 encoding of <paramref name="value"/> for <see cref="Authorization.Basic"/> credentials.
        /// </summary>
        /// <param name="value">The credential.</param>
        /// /// <param name="scheme"><see cref="Authorization"/> scheme.</param>
        /// <returns>The Authorization header value.</returns>
        public static string AsHeaderValue(this string value, Authorization scheme)
        {
            return string.Join(' ', new[] { scheme.AsString(), scheme.FormatValue(value) });
        }

        private static string FormatValue(this Authorization scheme, string value) => scheme switch
        {
            Authorization.Basic => value.Base64(),
            Authorization.Bearer => value,
        };

        private static string AsString(this Authorization scheme) => scheme switch
        {
            Authorization.Basic => Basic,
            Authorization.Bearer => Bearer,
        };
    }
}
