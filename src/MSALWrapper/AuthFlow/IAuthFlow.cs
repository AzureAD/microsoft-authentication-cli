// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlows
{
    using System.Threading.Tasks;

    /// <summary>
    /// The IAuthFlows interface.
    /// </summary>
    public interface IAuthFlow
    {
        /// <summary>
        /// Gets the jwt token for a resource.
        /// </summary>
        /// <returns>A <see cref="Task"/> of <see cref="TokenResult"/>.</returns>
        Task<TokenResult> GetTokenAsync();
    }
}
