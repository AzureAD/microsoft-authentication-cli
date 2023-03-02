// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System.Threading.Tasks;

    /// <summary>
    /// The IAuthFlow interface.
    /// </summary>
    public interface IAuthFlow
    {
        /// <summary>
        /// Get the name of the auth flow.
        /// </summary>
        /// <returns>The name of the auth flow.</returns>
        string Name();

        /// <summary>
        /// Get a token for a resource.
        /// </summary>
        /// <returns>A <see cref="Task"/> of <see cref="TokenResult"/>.</returns>
        Task<AuthFlowResult> GetTokenAsync();
    }
}
