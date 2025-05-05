// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    /// <summary>
    /// A static class to hold env var names.
    /// </summary>
    public static class EnvVars
    {
        /// <summary>Env var indicating we are in Cloud Build.</summary>
        public const string CloudBuild = "QBUILD_DISTRIBUTED";

        /// <summary>
        /// Holds the name of the env var to get an application insights ingestion token.
        /// </summary>
        public static readonly string ApplicationInsightsIngestionTokenEnvVar = $"{EnvVarPrefix}_APPLICATION_INSIGHTS_INGESTION_TOKEN";

        /// <summary>
        /// Holds the path of the registry key to get an application insights ingestion token.
        /// </summary>
        public static readonly string ApplicationInsightsIngestionTokenRegKeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\AzureAuth";

        /// <summary>
        /// Holds the name of the registry key to get an application insights ingestion token.
        /// </summary>
        public static readonly string ApplicationInsightsIngestionTokenRegKeyName = "ApplicationInsightsIngestionToken";

        /// <summary>
        /// Holds the name of the env var to get a config file from.
        /// </summary>
        public static readonly string Config = $"{EnvVarPrefix}_CONFIG";

        /// <summary>
        /// Name of the env var used to disable Public Client Authentication.
        /// </summary>
        public static readonly string NoUser = $"{EnvVarPrefix}_NO_USER";

        /// <summary>
        /// Name of the env var for the Azure DevOps pipelines default personal access token.
        /// </summary>
        public static readonly string SystemAccessToken = "SYSTEM_ACCESSTOKEN";

        /// <summary>
        /// Name of the env var to check for an Azure DevOps personal access token.
        /// </summary>
        public static readonly string AdoPat = $"{EnvVarPrefix}_ADO_PAT";

        /// <summary>
        /// Name of the env var to get the Auth Mode.
        /// </summary>
        public static readonly string AuthMode = $"{EnvVarPrefix}_MODE";

        /// <summary>
        /// Name of the env var used to disable user based authentication modes.
        /// NOTE: This is a private variable and it is recommended to not rely on this variable.
        /// </summary>
        internal static readonly string CorextNonInteractive = $"Corext_NonInteractive";

        private const string EnvVarPrefix = "AZUREAUTH";
    }
}
