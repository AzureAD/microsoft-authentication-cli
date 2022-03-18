// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    /// <summary>
    /// A static class to hold env var names.
    /// </summary>
    internal static class EnvVars
    {
        /// <summary>
        /// Holds the name of the env var to get an application insights ingestion token.
        /// </summary>
        public static readonly string ApplicationInsightsIngestionTokenEnvVar = $"{EnvVarPrefix}_APPLICATION_INSIGHTS_INGESTION_TOKEN";

        /// <summary>
        /// Holds the name of the env var to get a config file from.
        /// </summary>
        public static readonly string Config = $"{EnvVarPrefix}_CONFIG";

        private const string EnvVarPrefix = "AZUREAUTH";
    }
}
