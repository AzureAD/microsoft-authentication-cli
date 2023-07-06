// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    /// <summary>Azure DevOps-specific authentication parameters.</summary>
    public static class AuthParameters
    {
        /// <summary>Create Azure DevOps-specific authentication parameters.</summary>
        /// <param name="tenant">The tenant. Defaults to Microsoft.</param>
        /// <returns>A new instance of the <see cref="MSALWrapper.AuthParameters"/> class.</returns>
        public static MSALWrapper.AuthParameters AdoParameters(string tenant = Constants.Tenant.Microsoft)
        {
            return new MSALWrapper.AuthParameters(
                Constants.Client.VisualStudio,
                tenant,
                new[] { Constants.Scope.AzureDevOpsDefault });
        }
    }
}
