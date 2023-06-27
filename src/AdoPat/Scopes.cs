// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    /// <summary>Azure DevOps scopes and utility methods for working with them.</summary>
    public static class Scopes
    {
        // These scopes should be kept up-to-date with https://aka.ms/azure-devops-pat-scopes.
        public static HashSet<string> ValidScopes = new HashSet<string>
        {
            // Agent Pools
            "vso.agentpools",
            "vso.agentpools_manage",
            "vso.environment_manage",

            // Analytics
            "vso.analytics",

            // Audit Log
            "vso.auditlog",

            // Build
            "vso.build",
            "vso.build_execute",

            // Code
            "vso.code",
            "vso.code_write",
            "vso.code_manage",
            "vso.code_full",
            "vso.code_status",

            // Entitlements
            "vso.entitlements",
            "vso.memberentitlementmanagement",
            "vso.memberentitlementmanagement_write",

            // Extensions
            "vso.extension",
            "vso.extension_manage",
            "vso.extension.data",
            "vso.extension.data_write",

            // Graph & Identity
            "vso.graph",
            "vso.graph_manage",
            "vso.identity",
            "vso.identity_manage",

            // Load Test
            "vso.loadtest",
            "vso.loadtest_write",

            // Machine Group
            "vso.machinegroup_manage",

            // Marketplace
            "vso.gallery",
            "vso.gallery_acquire",
            "vso.gallery_publish",
            "vso.gallery_manage",

            // Notifications
            "vso.notification",
            "vso.notification_write",
            "vso.notification_manage",
            "vso.notification_diagnostics",

            // Packaging
            "vso.packaging",
            "vso.packaging_write",
            "vso.packaging_manage",

            // Project and Team
            "vso.project",
            "vso.project_write",
            "vso.project_manage",

            // Release
            "vso.release",
            "vso.release_execute",
            "vso.release_manage",

            // Security
            "vso.security_manage",

            // Service Connections
            "vso.serviceendpoint",
            "vso.serviceendpoint_query",
            "vso.serviceendpoint_manage",

            // Settings
            "vso.settings",
            "vso.settings_write",

            // Symbols
            "vso.symbols",
            "vso.symbols_write",
            "vso.symbols_manage",

            // Task Groups
            "vso.taskgroups_read",
            "vso.taskgroups_write",
            "vso.taskgroups_manage",

            // Team Dashboard
            "vso.dashboards",
            "vso.dashboards_manage",

            // Test Management
            "vso.test",
            "vso.test_write",

            // Tokens
            "vso.tokens",
            "vso.tokenadministration",

            // User Profile
            "vso.profile",
            "vso.profile_write",

            // Variable Groups
            "vso.variablegroups_read",
            "vso.variablegroups_write",
            "vso.variablegroups_manage",

            // Wiki
            "vso.wiki",
            "vso.wiki_write",

            // Work Items
            "vso.work",
            "vso.work_write",
            "vso.work_full",
        };

        /// <summary>Validate the given scopes.</summary>
        /// <param name="scopes">The scopes to validate.</param>
        /// <returns>The set of invalid scopes present in the given scopes. The
        /// empty set means no scopes were invalid.</returns>
        public static ImmutableHashSet<string> Validate(IEnumerable<string> scopes)
        {
            return scopes.Except(ValidScopes).ToImmutableHashSet();
        }

        /// <summary>Normalize scopes by deduplicating and converting to lowercase.</summary>
        /// <param name="scopes">The scopes to normalize.</param>
        /// <returns>Normalized scopes in sorted order. This might be the empty set.</returns>
        public static ImmutableSortedSet<string> Normalize(IEnumerable<string> scopes)
        {
            var scopesCount = scopes?.Count() ?? 0;
            return (scopesCount == 0) ?
                ImmutableSortedSet.Create<string>() :
                scopes.Select(scope => scope.ToLower()).ToImmutableSortedSet();
        }
    }
}
