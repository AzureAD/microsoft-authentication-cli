// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.Logging;

    using AADTokenFetcher = Microsoft.Authentication.MSALWrapper.TokenFetcher;

    /// <summary>
    /// An abstraction arounf <see cref="MSALWrapper.TokenFetcher"/> specifically for Azure Devops.
    /// </summary>
    internal static class TokenFetcher
    {
        /// <summary>
        /// Get an Azure Devops AAD access token.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="mode">Auth Mode.</param>
        /// <param name="domain">Domain.</param>
        /// <param name="prompt">Prompt Hint.</param>
        /// <param name="timeout">Timeout.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<AADTokenFetcher.Result> AccessTokenAsync(ILogger logger, AuthMode mode, string domain, string prompt, TimeSpan timeout)
        {
            return await AADTokenFetcher.AccessTokenAsync(
                logger: logger,
                client: new Guid(Constants.Client.VisualStudio),
                tenant: new Guid(Constants.Tenant.Microsoft),
                scopes: new[] { Constants.Scope.AzureDevopsDefault },
                mode: mode,
                domain: domain,
                prompt: prompt,
                timeout: timeout);
        }
    }
}
