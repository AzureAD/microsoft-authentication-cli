// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Office.Lasso.Interfaces;

    /// <summary>
    /// Business logic for either getting an ADO PAT from <see cref="IEnv"/>
    /// or doing normal AAD Authentication.
    /// </summary>
    public static class AdoToken
    {
        private static readonly IEnumerable<string> PatEnvVars = new[] { "SYSTEM_ACCESSTOKEN" };

        /// <summary>
        /// Get either an ADO PAT from the environment. If one is not set, then do normal ADO AAD Auth.
        /// </summary>
        /// <param name="env">The <see cref="IEnv"/> to use for interacting with the environment.</param>
        /// <returns>A PAT from the env if set, otherwise null.</returns>
        public static PatResult PatFromEnv(IEnv env)
        {
            string pat = env.Get(PatEnvVars.First());
            var exists = !string.IsNullOrEmpty(pat);
            return new ()
            {
                Exists = exists,
                Value = exists ? pat : default,
                EnvVarSource = exists ? PatEnvVars.First() : default,
            };
        }

        /// <summary>
        /// The result struct for retrieving a PAT out of the environment.
        /// </summary>
        public record PatResult
        {
            /// <summary>
            /// Gets a value indicating whether a PAT was found in the env.
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
