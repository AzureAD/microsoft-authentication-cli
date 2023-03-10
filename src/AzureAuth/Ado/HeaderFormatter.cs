// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    using System;
    using System.Text;

    /// <summary>
    /// Methods for printing ADO Tokens.
    /// </summary>
    public static class HeaderFormatter
    {
        private const string AuthorizationHeader = "Authorization:";
        private const string Basic = "Basic";
        private const string Bearer = "Bearer";

        /// <summary>
        /// Authorization Header using Basic scheme.
        /// </summary>
        /// <param name="value">The credential.</param>
        /// <returns></returns>
        public static string HeaderBasic(string value)
        {
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
            return string.Join(' ', new[] { AuthorizationHeader, Basic, encoded });
        }

        public static string HeaderBasicValue(string value)
        {
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
            return string.Join(' ', new[] { Basic, encoded });
        }

        public static string HeaderBearer(string value)
        {
            return string.Join(' ', new[] { AuthorizationHeader, Bearer, value });
        }

        public static string HeaderBearerValue(string value)
        {
            return string.Join(' ', new[] { Bearer, value });
        }
    }
}
