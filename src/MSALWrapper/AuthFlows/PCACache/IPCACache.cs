// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Extensions.Msal;

    /// <summary>
    /// The PCACache interface.
    /// </summary>
    internal interface IPCACache
    {
        /// <summary>
        /// The setup token cache.
        /// </summary>
        void SetupTokenCache();

        /// <summary>
        /// The setup token cache.
        /// </summary>
        /// <param name="errorsList">The errors list.</param>
        void SetupTokenCache(List<Exception> errorsList);
    }
}
