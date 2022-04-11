// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlows
{
    /// <summary>
    /// The token result extensions.
    /// </summary>
    internal static class TokenResultExtensions
    {
        /// <summary>
        /// The set authentication type.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="authType">The auth type.</param>
        internal static void SetAuthenticationType(this TokenResult result, AuthType authType)
        {
            if (result != null)
            {
                result.AuthType = authType;
            }
        }
    }
}
