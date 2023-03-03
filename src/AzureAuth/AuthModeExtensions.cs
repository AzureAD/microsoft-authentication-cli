// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Office.Lasso.Interfaces;

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
        public static AuthMode CombinedAuthMode(this IEnumerable<AuthMode> authModes, IEnv env)
        {
            if (env.InteractiveAuthDisabled())
            {
#if PlatformWindows
                return AuthMode.IWA;
#else
                return 0;
#endif
            }

            return authModes.Aggregate((a1, a2) => a1 | a2);
        }
    }
}
