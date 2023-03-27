// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Interfaces;

    /// <summary>
    /// Extensions to <see cref="AuthMode"/>s.
    /// </summary>
    public static class AuthModeExtensions
    {
        /// <summary>
        /// Filters a list of <see cref="AuthMode"/> for interaction returning a single aggregate <see cref="AuthMode"/>.
        /// </summary>
        /// <param name="authMode">Starting <see cref="AuthMode"/>to filter.</param>
        /// <param name="env">An <see cref="IEnv"/>.</param>
        /// <param name="logger">An <see cref="ILogger"/>.</param>
        /// <returns>An <see cref="AuthMode"/> with only non-interactive auth modes allowed, if specificed by the environment.</returns>
        public static AuthMode PreventInteractionIfNeeded(this AuthMode authMode, IEnv env, ILogger logger)
        {
            if (env.InteractiveAuthDisabled())
            {
                logger.LogWarning($"Interactive authentication is disabled.");
#if PlatformWindows
                logger.LogWarning($"Only Integrated Windows Authentication will be attempted.");
                return AuthMode.IWA;
#else
                return 0;
#endif
            }

            return authMode;
        }
    }
}
