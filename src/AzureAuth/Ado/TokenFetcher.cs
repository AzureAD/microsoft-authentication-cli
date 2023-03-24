// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Extensions.Logging;

    using AADTokenFetcher = Microsoft.Authentication.MSALWrapper.MsalWrapper;

    /// <summary>
    /// An abstraction around <see cref="MSALWrapper.MsalWrapper"/> specifically for Azure Devops.
    /// </summary>
    internal static class TokenFetcher
    {
        /// <summary>
        /// Get an Azure DevOps AAD access token.
        /// </summary>
        /// <param name="logger">A <see cref="ILogger"/> to use.</param>
        /// <param name="mode">The <see cref="AuthMode"/>. Controls which <see cref="IAuthFlow"/>s should be used.</param>
        /// <param name="domain">The domain (account suffix) to filter cached accounts with.</param>
        /// <param name="prompt">A prompt hint to display to the user if needed.</param>
        /// <param name="timeout">The max <see cref="TimeSpan"/> we should spend attempting token acquisition for.</param>
        /// <returns>A <see cref="AADTokenFetcher.Result"/>.</returns>
        public static AADTokenFetcher.Result AccessToken(
            ILogger logger,
            AuthMode mode,
            string domain,
            string prompt,
            TimeSpan timeout)
        {
            return new AADTokenFetcher().AccessToken(
                logger: logger,
                client: new Guid(Constants.Client.VisualStudio),
                tenant: new Guid(Constants.Tenant.Microsoft),
                scopes: new[] { Constants.Scope.AzureDevOpsDefault },
                mode: mode,
                domain: domain,
                prompt: prompt,
                timeout: timeout);
        }
    }
}
