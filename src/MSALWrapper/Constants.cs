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
        /// The auth osx key chain suffix.
        /// </summary>
        public const string AuthOSXKeyChainSuffix = "azureauth";

        /// <summary>
        /// The aad oauth redirect uri.
        /// </summary>
        public static readonly Uri AadOAuthRedirectUri = new Uri("urn:ietf:wg:oauth:2.0:oob");

        // local host loopback Uri for web auth flow.

        /// <summary>
        /// The aad redirect uri.
        /// </summary>
        public static readonly Uri AadRedirectUri = new Uri("http://localhost");
    }
}
