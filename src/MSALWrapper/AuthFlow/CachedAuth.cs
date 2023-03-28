// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using static Microsoft.Authentication.MSALWrapper.MsalWrapper;

    internal static class CachedAuth
    {
        internal static async Task<TokenResult> TryCachedAuthAsync(ILogger logger, TimeSpan integratedWindowsAuthTimeout, IEnumerable<string> scopes, IAccount account, IPCAWrapper pcaWrapper, IList<Exception> errors)
        {
            var tokenResult = await TaskExecutor.CompleteWithin(
                            logger,
                            integratedWindowsAuthTimeout,
                            "Get Token Silent",
                            (cancellationToken) => pcaWrapper.GetTokenSilentAsync(scopes, account, cancellationToken),
                            errors)
                            .ConfigureAwait(false);
            tokenResult.SetSilent();
        }
    }
}
