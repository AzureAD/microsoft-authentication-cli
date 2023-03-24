// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    using Microsoft.Authentication.MSALWrapper;

    /// <summary>
    /// Azure DevOps constant values.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// ADO pipeline id.
        /// This is defined by ADO pipelines. See https://learn.microsoft.com/en-us/azure/devops/pipelines/build/variables.
        /// </summary>
        public const string SystemDefinitionId = "SYSTEM_DEFINITIONID";

        /// <summary>
        /// The default auth params for AzureDevops.
        /// </summary>
        public static readonly AuthParameters AdoParams = new AuthParameters(Client.VisualStudio, Tenant.Microsoft, new[] { Scope.AzureDevOpsDefault });

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
    }
}
