// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    // If you add a output mode here, be sure to update the corresponding CLI help text.

    /// <summary>
    /// The output mode.
    /// </summary>
    public enum OutputMode
    {
        /// <summary>
        /// The status.
        /// </summary>
        Status,

        /// <summary>
        /// The token.
        /// </summary>
        Token,

        /// <summary>
        /// The json.
        /// </summary>
        Json,

        /// <summary>
        /// The none.
        /// </summary>
        None,

        /// <summary>
        /// The SID.
        /// </summary>
        SID,
    }
}
