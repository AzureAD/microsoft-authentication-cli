// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// A static class for creating an enumeration of auth flows given settings.
    /// </summary>
    public class AuthFlowFactory
    {
        /// <summary>
        /// Create a list of <see cref="IAuthFlow"/> instances based on the given settings.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger"/>.</param>
        /// <param name="authMode">The desired <see cref="AuthMode"/>.</param>
        /// <param name="clientId">The client id.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="scopes">The scopes.</param>
        /// <param name="osxKeyChainSuffix">A suffix to customize the OSX msal cache.</param>
        /// <param name="preferredDomain">Preferred domain to use when filtering cached accounts.</param>
        /// <param name="pcaWrapper">An injected PCAWrapper to use.</param>
        /// <param name="promptHint">A prompt hint to contextualize an auth prompt if given.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IAuthFlow"/> instances.</returns>
        public static IEnumerable<IAuthFlow> Create(
            ILogger logger,
            AuthMode authMode,
            Guid clientId,
            Guid tenantId,
            IEnumerable<string> scopes,
            string osxKeyChainSuffix,
            string preferredDomain,
            IPCAWrapper pcaWrapper,
            string promptHint)
        {
            List<IAuthFlow> flows = new List<IAuthFlow>();
            if (authMode.IsWeb())
            {
                flows.Add(new Web(logger, clientId, tenantId, scopes, osxKeyChainSuffix, preferredDomain, pcaWrapper, promptHint));
            }

            if (authMode.IsDeviceCode())
            {
                flows.Add(new DeviceCode(logger, clientId, tenantId, scopes, osxKeyChainSuffix, preferredDomain, pcaWrapper, promptHint));
            }

            if (authMode.IsBroker())
            {
                flows.Add(new Broker(logger, clientId, tenantId, scopes, osxKeyChainSuffix, preferredDomain, pcaWrapper, promptHint));
            }

            return flows;
        }
    }
}
