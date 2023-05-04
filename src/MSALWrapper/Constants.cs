// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;

    /// <summary>
    /// The constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The aad oauth redirect uri.
        /// </summary>
        public static readonly Uri AadOAuthRedirectUri = new Uri("urn:ietf:wg:oauth:2.0:oob");

        // local host loopback Uri for web auth flow.

        /// <summary>
        /// The aad redirect uri.
        /// </summary>
        public static readonly Uri AadRedirectUri = new Uri("http://localhost");

        /// <summary>
        /// The name of an environment variable used to disable file cache configuration.
        /// </summary>
        internal const string OEAUTH_MSAL_DISABLE_CACHE = "OEAUTH_MSAL_DISABLE_CACHE";

        internal static class AuthFlow
        {
            public const string CachedAuth = "cache";
            public const string Iwa = "iwa";
            public const string Broker = "broker";
            public const string Web = "web";
            public const string DeviceCode = "devicecode";
        }
    }
}
