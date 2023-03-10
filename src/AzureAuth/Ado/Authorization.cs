// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Ado
{
    /// <summary>
    /// Authorization Header schemes.
    /// </summary>
    public enum Authorization
    {
        /// <summary> Base64 encoded basic authorization. </summary>
        Basic,

        /// <summary> Bearer token authorization. </summary>
        Bearer,
    }
}
