// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    using System.Collections.Generic;

    using Microsoft.Office.Lasso.Interfaces;

    /// <summary>
    /// A class for getting an ADO PAT from an <see cref="IEnv"/> or an AAD access token through MSAL.
    /// </summary>
    public static class AdoToken
    {
        private static readonly IEnumerable<string> PatEnvVars = new[]
        {
            EnvVars.AdoPat,
            EnvVars.SystemAccessToken,
        };

        /// <summary>
        /// Get either an ADO PAT from the environment. If one is not set, then do normal ADO AAD Auth.
        /// </summary>
        /// <param name="env">The <see cref="IEnv"/> to use for interacting with the environment.</param>
        /// <returns>A PAT from the env if set, otherwise null.</returns>
        public static PatResult PatFromEnv(IEnv env)
        {
            foreach (var envVar in PatEnvVars)
            {
                var pat = env.Get(envVar);
                var exists = !string.IsNullOrEmpty(pat);

                if (exists)
                {
                    return new ()
                    {
                        Exists = exists,
                        Value = pat,
                        EnvVarSource = envVar,
                    };
                }
            }

            return new ()
            {
                Exists = false,
            };
        }

        /// <summary>
        /// The result struct for retrieving a PAT out of the environment.
        /// </summary>
        public record PatResult
        {
            /// <summary>
            /// Gets a value indicating whether a PAT was found in the environment.
            /// </summary>
            public bool Exists { get; init; }

            /// <summary>
            /// Gets the source env var that contained the PAT.
            /// </summary>
            public string EnvVarSource { get; init; }

            /// <summary>
            /// Gets the PAT value itself.
            /// </summary>
            public string Value { get; init; }
        }
    }
}
