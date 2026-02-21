// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// Extension methods to Lasso's <see cref="IEnv"/> interface.
    /// </summary>
    public static class IEnvExtensions
    {
        private const string CorextPositiveValue = "1";

        /// <summary>
        /// Determines whether we are running in an Azure DevOps Pipeline environment.
        /// </summary>
        /// <param name="env">The <see cref="IEnv"/> to use to get environment variables.</param>
        /// <returns>True if running in an Azure DevOps Pipeline.</returns>
        public static bool IsAdoPipeline(this IEnv env)
        {
            return string.Equals("True", env.Get(EnvVars.TfBuild), StringComparison.OrdinalIgnoreCase);
        }

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

        /// <summary>
        /// Get the auth modes from the environment or set the default.
        /// </summary>
        /// <param name="env">The <see cref="IEnv"/> to use.</param>
        /// <param name="eventData">Event data to add the auth mode to.</param>
        /// <returns>AuthModes.</returns>
        public static IEnumerable<AuthMode> ReadAuthModeFromEnvOrSetDefault(this IEnv env)
        {
            var authModesFromEnv = env.Get(EnvVars.AuthMode);

            // If auth modes are not specified in the environment, then return the default.
            if (string.IsNullOrEmpty(authModesFromEnv))
            {
                return new[] { AuthMode.Default };
            }

            var result = new List<AuthMode>();
            foreach (var val in authModesFromEnv.Split(','))
            {
                if (Enum.TryParse<AuthMode>(val, ignoreCase: true, out var mode))
                {
                    result.Add(mode);
                }
                else
                {
                    // If the environment variable is not a valid auth mode, then return an empty list.
                    return new List<AuthMode>();
                }
            }

            return result;
        }
    }
}
