// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Security.Claims;

    using Microsoft.IdentityModel.JsonWebTokens;

    /// <summary>
    /// The json web token extensions.
    /// </summary>
    internal static class JsonWebTokenExtensions
    {
        /// <summary>
        /// The get azure user name.
        /// </summary>
        /// <param name="jwt">The jwt.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string GetAzureUserName(this JsonWebToken jwt)
        {
            string idp = jwt.TryGetClaim("idp", out Claim idpClaim)
                ? idpClaim.Value.ToLowerInvariant()
                : null;

            // If the identity provider is AAD (*not* MSA) we should use the UPN claim
            if (!StringComparer.OrdinalIgnoreCase.Equals(idp, "live.com") &&
                jwt.TryGetClaim("upn", out Claim upnClaim))
            {
                return upnClaim.Value;
            }

            // For MSA IDPs or if the UPN claim is missing, we should use the 'email' claim
            if (jwt.TryGetClaim("email", out Claim emailClaim))
            {
                return emailClaim.Value;
            }

            return null;
        }

        /// <summary>
        /// The get display name.
        /// </summary>
        /// <param name="jwt">The jwt.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string GetDisplayName(this JsonWebToken jwt)
        {
            jwt.TryGetClaim("name", out Claim name);
            return name?.Value;
        }
    }
}
