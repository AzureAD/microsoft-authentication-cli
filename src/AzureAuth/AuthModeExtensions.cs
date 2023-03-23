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
        /// <param name="authMode">Starting <see cref="AuthMode"/>to filter.</param>
        /// <param name="env">An <see cref="IEnv"/> to use.</param>
        /// <returns>An <see cref="AuthMode"/> with only non-interactive auth modes allowed, if specificed by the environment.</returns>
        public static AuthMode PreventInteractionIfNeeded(this AuthMode authMode, IEnv env)
        {
            if (env.InteractiveAuthDisabled())
            {
#if PlatformWindows
                return AuthMode.IWA;
#else
                return 0;
#endif
            }

            return authMode;
        }
    }
}
