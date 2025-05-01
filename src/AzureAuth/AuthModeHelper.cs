using Microsoft.Authentication.AzureAuth.Commands;
using Microsoft.Authentication.MSALWrapper;
using Microsoft.Extensions.Logging;
using Microsoft.Office.Lasso.Interfaces;
using Microsoft.Office.Lasso.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Authentication.AzureAuth
{
    /// <summary>
    /// Helper class for <see cref="AuthMode"/>s.
    /// </summary>
    public static class AuthModeHelper
    {
        /// <summary>
        /// Get the auth modes from the environment or set the default.
        /// </summary>
        /// <param name="env">The <see cref="IEnv"/> to use.</param>
        /// <param name="eventData">Event data to add the auth mode to.</param>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <returns>AuthModes.</returns>
        public static IEnumerable<AuthMode> ReadAuthModeFromEnvOrSetDefault(IEnv env, EventData eventData, ILogger logger)
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
                    logger.LogError($"Invalid value specified for environment variable {EnvVars.AuthMode}. Allowed values are: {CommandAad.AuthModeHelperText}");
                    return new List<AuthMode>();
                }
            }

            eventData.Add($"env_{EnvVars.AuthMode}", authModesFromEnv);
            return result;
        }
    }
}
