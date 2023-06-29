// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    using System.Collections.Generic;

    using Microsoft.Authentication.MSALWrapper;

    /// <summary>
    /// Azure DevOps constant values.
    /// </summary>
    internal static class Constants
    {
        /// <summary>The default preferred domain used when retrieving cached accounts.</summary>
        public const string PreferredDomain = "microsoft.com";

        /// <summary>The base URL for Azure DevOps.</summary>
        public const string BaseUrl = "https://dev.azure.com";

        /// <summary>The PAT lockfile name. The containing directory is platform specific, thus configured at runtime.</summary>
        public const string PatLockfileName = "azureauth-ado-pat.lock";

        /// <summary>
        /// ADO pipeline id.
        /// This is defined by ADO pipelines. See https://learn.microsoft.com/en-us/azure/devops/pipelines/build/variables.
        /// </summary>
        public const string SystemDefinitionId = "SYSTEM_DEFINITIONID";

        /// <summary>
        /// Azure tenant IDs.
        /// </summary>
        public static class Tenant
        {
            /// <summary>
            /// Microsoft tenant ID.
            /// </summary>
            public const string Microsoft = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        }

        /// <summary>
        /// App Registration Client IDs.
        /// </summary>
        public static class Client
        {
            /// <summary>
            /// Visual Studio 2019 and earlier client ID.
            /// </summary>
            public const string VisualStudio = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        }

        /// <summary>
        /// Resource Scopes.
        /// </summary>
        public static class Scope
        {
            /// <summary>
            /// The default scope used for Azure DevOps.
            /// </summary>
            public const string AzureDevOpsDefault = "499b84ac-1321-427f-aa17-267ca6975798/.default";
        }

        /// <summary>Parameters used to configure persistent, encrypted storage for Azure DevOps PATs.</summary>
        public static class PatStorageParameters
        {
            /// <summary>The PAT cache file. The containing directory is platform and user specific, thus configured at runtime.</summary>
            public const string CacheFileName = "azureauth-pat.cache";

            /// <summary>The Mac keychain account name.</summary>
            public const string MacOSAccountName = "com.microsoft.identify.azureauth.ado.pat";

            /// <summary>The Mac keychain service name.</summary>
            public const string MacOSServiceName = "AzureAuth ADO PAT Cache";

            /// <summary>The Linux keyring schema name.</summary>
            public const string LinuxKeyRingSchemaName = "com.microsoft.identity.azureauth.ado.pat";

            /// <summary>The user-readable label for the Linux keyring secret.</summary>
            public const string LinuxKeyRingLabel = "AzureAuth ADO PAT Cache";

            /// <summary>The Linux keyring collection. "default" is persisted, "session" is destroyed on logout.</summary>
            public const string LinuxKeyRingCollection = "default";

            /// <summary>An additional attribute used to decorate the Linux keyring secret.</summary>
            public static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", "1");

            /// <summary>Another additional attribute used to decorate the Linux keyring secret.</summary>
            public static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "Microsoft Developer Tools");
        }
    }
}
