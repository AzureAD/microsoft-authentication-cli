// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;

    using static Microsoft.Authentication.MSALWrapper.MsalWrapper;

    public static class CachedAuth
    {
        /// <summary>
        /// Try to get a token silently.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger"/>.</param>
        /// <param name="cachedAuthTimeout">The timeout for getting a cached token.</param>
        /// <param name="scopes">Auth Scopes.</param>
        /// <param name="account"><see cref="IAccount"/> object to use or null if no account has been found.</param>
        /// <param name="pcaWrapper">An <see cref="IPCAWrapper"/>.</param>
        /// <param name="errors">List of errors to report any Exceptions into.</param>
        /// <returns>A <see cref="Task{TResult}"/> or null if no cached token was acquired.</returns>
        public static async Task<TokenResult> TryCachedAuthAsync(ILogger logger, TimeSpan cachedAuthTimeout, IEnumerable<string> scopes, IAccount account, IPCAWrapper pcaWrapper, IList<Exception> errors)
        {
            if (account == null)
            {
                logger.LogDebug("No cached account");
                return null;
            }

            logger.LogDebug($"Using cached account '{account.Username}'");

            TokenResult tokenResult = null;
            try
            {
                tokenResult = await TaskExecutor.CompleteWithin(
                                logger,
                                cachedAuthTimeout,
                                "Get Token Silent",
                                (cancellationToken) => pcaWrapper.GetTokenSilentAsync(scopes, account, cancellationToken),
                                errors)
                                .ConfigureAwait(false);
                tokenResult.SetSilent();
            }
            catch (MsalUiRequiredException ex)
            {
                errors.Add(ex);
                logger.LogDebug($"Cached Auth failed:\n{ex.Message}");
            }

            return tokenResult;
        }
    }
}
