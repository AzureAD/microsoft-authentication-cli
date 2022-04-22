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
        /// Gets the token for a resource.
        /// </summary>
        /// <returns>A <see cref="Task"/> of <see cref="TokenResult"/>.</returns>
        Task<AuthFlowResult> GetTokenAsync();
    }
}
