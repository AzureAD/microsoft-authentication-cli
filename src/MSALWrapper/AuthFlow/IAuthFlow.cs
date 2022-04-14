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
        /// The get token async.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        Task<TokenResult> GetTokenAsync();
    }
}
