// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Office.Lasso.Interfaces;

    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extensions to <see cref="AuthMode"/>s.
    /// </summary>
    public static class AuthModeExtensions
    {
        /// <summary>
        /// Filters a list of <see cref="AuthMode"/> for interaction returning a single aggregate <see cref="AuthMode"/>.
        /// </summary>
        /// <param name="authModes"><see cref="AuthMode"/>s to aggregate and filter.</param>
        /// <param name="env">An <see cref="IEnv"/> to use.</param>
        /// <returns>A single aggregate <see cref="AuthMode"/> filtered for interactivity.</returns>
        public static AuthMode FilterInteraction(this IEnumerable<AuthMode> authModes, IEnv env)
        {
            if (InteractiveAuthDisabled(env))
            {
#if PlatformWindows
                return AuthMode.IWA;
#else
                return 0;
#endif
            }

            return authModes.Aggregate((a1, a2) => a1 | a2);
        }

        /// <summary>
        /// Determines whether interactive auth is allowed or not.
        /// </summary>
        /// <param name="env">An <see cref="IEnv"/> to use.</param>
        /// <returns>A boolean to indicate PCA is disabled.</returns>
        public static bool InteractiveAuthDisabled(IEnv env)
        {
            return !string.IsNullOrEmpty(env.Get(EnvVars.NoUser)) ||
                string.Equals("1", env.Get(EnvVars.CorextNonInteractive));
        }
    }
}
