// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    using System;
    using System.Text;

    /// <summary>
    /// Methods for printing ADO Tokens.
    /// </summary>
    public static class TokenFormatter
    {
        private const string AuthorizationHeader = "Authorization:";
        private const string Basic = "Basic";
        private const string Bearer = "Bearer";

        /// <summary>
        /// Base64 encode <paramref name="value"/> adding padding (=) characters as needed.
        /// </summary>
        /// <param name="value">Value to base64 encode.</param>
        /// <returns>Base64 encoded value.</returns>
        public static string Base64(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Authorization Header using the Basic scheme
        /// that handles base64 encoding of <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The credential.</param>
        /// <returns>A full Authorization header using the Basic scheme.</returns>
        public static string HeaderBasic(string value)
        {
            return string.Join(' ', new[] { AuthorizationHeader, Basic, Base64(value) });
        }

        /// <summary>
        /// The value for an Authorization header using the Basic scheme
        /// that handles base64 encoding of <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The credential.</param>
        /// <returns>The Authorization header value.</returns>
        public static string HeaderBasicValue(string value)
        {
            return string.Join(' ', new[] { Basic, Base64(value) });
        }

        /// <summary>
        /// Authorization Header using Bearer scheme.
        /// </summary>
        /// <param name="value">The crednetial.</param>
        /// <returns>A full Authorization header using the Bearer scheme.</returns>
        public static string HeaderBearer(string value)
        {
            return string.Join(' ', new[] { AuthorizationHeader, Bearer, value });
        }

        /// <summary>
        /// The value for an Authorization header using the Bearer scheme.
        /// </summary>
        /// <param name="value">The credential.</param>
        /// <returns>The Authorization header value.</returns>
        public static string HeaderBearerValue(string value)
        {
            return string.Join(' ', new[] { Bearer, value });
        }
    }
}
