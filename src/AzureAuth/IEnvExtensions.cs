// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using Microsoft.Office.Lasso.Interfaces;

    /// <summary>
    /// Extension methods to Lasso's <see cref="IEnv"/> interface.
    /// </summary>
    public static class IEnvExtensions
    {
        private const string CorextPositiveValue = "1";

        /// <summary>
        /// Determines whether interactive auth is disabled or not.
        /// </summary>
        /// <param name="env">The <see cref="IEnv"/> to use to get environment variables.</param>
        /// <returns>A boolean to indicate if interactive auth modes should be disabled.</returns>
        public static bool InteractiveAuthDisabled(this IEnv env)
        {
            return !string.IsNullOrEmpty(env.Get(EnvVars.NoUser)) ||
                string.Equals(CorextPositiveValue, env.Get(EnvVars.CorextNonInteractive));
        }
    }
}
