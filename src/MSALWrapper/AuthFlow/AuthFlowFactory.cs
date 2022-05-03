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
        /// <param name="preferredDomain">Preferred domain to use when filtering cached accounts.</param>
        /// <param name="promptHint">A prompt hint to contextualize an auth prompt if given.</param>
        /// <param name="osxKeyChainSuffix">A suffix to customize the OSX msal cache.</param>
        /// <param name="pcaWrapper">An optional injected <see cref="IPCAWrapper"/> to use.</param>
        /// <param name="platformUtils">An optional injected <see cref="IPlatformUtils"/> to use.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IAuthFlow"/> instances.</returns>
        public static IEnumerable<IAuthFlow> Create(
            ILogger logger,
            AuthMode authMode,
            Guid clientId,
            Guid tenantId,
            IEnumerable<string> scopes,
            string preferredDomain,
            string promptHint,
            string osxKeyChainSuffix,
            IPCAWrapper pcaWrapper = null,
            IPlatformUtils platformUtils = null)
        {
            logger = logger ?? throw new ArgumentNullException(nameof(logger));
            platformUtils = platformUtils ?? new PlatformUtils(logger);

            // This is a list. The order in which flows get added is very important
            // as it sets the order in which auth flows will be attempted.
            List<IAuthFlow> flows = new List<IAuthFlow>();

            // This check silently fails on winserver if broker has been requested.
            // Future: Consider making AuthMode platform aware at Runtime.
            // https://github.com/AzureAD/microsoft-authentication-cli/issues/55
            if (authMode.IsBroker() && platformUtils.IsWindows10Or11())
            {
                flows.Add(new Broker(logger, clientId, tenantId, scopes, osxKeyChainSuffix, preferredDomain, pcaWrapper, promptHint));
            }

            if (authMode.IsWeb())
            {
                flows.Add(new Web(logger, clientId, tenantId, scopes, osxKeyChainSuffix, preferredDomain, pcaWrapper, promptHint));
            }

            if (authMode.IsDeviceCode())
            {
                flows.Add(new DeviceCode(logger, clientId, tenantId, scopes, osxKeyChainSuffix, preferredDomain, pcaWrapper, promptHint));
            }

            return flows;
        }
    }
}
