// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// An interface for getting a <see cref="TokenResult"/>.
    /// </summary>
    public interface IPublicClientAuth
    {
        /// <summary>
        /// Gets a <see cref="TokenResult"/> using the provided parameters by using Public Client Authentication.
        /// </summary>
        /// <param name="authParams">The authentication parameters.</param>
        /// <param name="authModes">The auth modes to use.</param>
        /// <param name="domain">The domain to filter too.</param>
        /// <param name="prompt">The prompt hint.</param>
        /// <param name="timeout">The timout.</param>
        /// <param name="eventData">An eventData to extend with telemetry fields.</param>
        /// <returns>A <see cref="TokenResult"/> or null.</returns>
        TokenResult Token(AuthParameters authParams, IEnumerable<AuthMode> authModes, string domain, string prompt, TimeSpan timeout, EventData eventData);
    }
}
