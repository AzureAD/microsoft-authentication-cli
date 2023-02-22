// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    /// <summary>
    /// Azure DevOps constant values.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// Azure tenant IDs.
        /// </summary>
        public static class Tenant
        {
            /// <summary>
            /// Microsoft tenant ID.
            /// </summary>
            public static string Msft = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        }

        /// <summary>
        /// App Registration Client IDs.
        /// </summary>
        public static class Client
        {
            /// <summary>
            /// Visual Studio 2019 and earlier client ID.
            /// </summary>
            public static string VisualStudio = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        }

        /// <summary>
        /// Resource Scopes.
        /// </summary>
        public static class Scope
        {
            /// <summary>
            /// The default scope used for Azure DevOps.
            /// </summary>
            public static string AzureDevopsDefault = "499b84ac-1321-427f-aa17-267ca6975798/.default";
        }
    }
}
