// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    /// <summary>
    /// Extension method to the <see cref="TokenResult"/> class.
    /// </summary>
    internal static class TokenResultExtensions
    {
        /// <summary>
        /// Sets the Silent property to true if the token result is not null.
        /// </summary>
        /// <param name="result">The result.</param>
        internal static void SetSilent(this TokenResult result)
        {
            if (result != null)
            {
                result.IsSilent = true;
            }
        }
    }
}
