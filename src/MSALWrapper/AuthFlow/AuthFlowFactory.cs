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
        /// <param name="authParams">The <see cref="AuthParameters"/>.</param>
        /// <param name="authMode">The desired <see cref="AuthMode"/>.</param>
        /// <param name="preferredDomain">Preferred domain to use when filtering cached accounts.</param>
        /// <param name="promptHint">A prompt hint to contextualize an auth prompt if given.</param>
        /// <param name="pcaWrapper">An optional injected <see cref="IPCAWrapper"/> to use.</param>
        /// <param name="platformUtils">An optional injected <see cref="IPlatformUtils"/> to use.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IAuthFlow"/> instances.</returns>
        public static IEnumerable<IAuthFlow> Create(
            ILogger logger,
            AuthParameters authParams,
            AuthMode authMode,
            string preferredDomain,
            string promptHint,
            IPCAWrapper pcaWrapper = null,
            IPlatformUtils platformUtils = null)
        {
            logger = logger ?? throw new ArgumentNullException(nameof(logger));
            platformUtils = platformUtils ?? new PlatformUtils(logger);

            // This is a list. The order in which flows get added is very important
            // as it sets the order in which auth flows will be attempted.
            List<IAuthFlow> flows = new List<IAuthFlow>();

            // We skip CachedAuth if Broker is present in authMode on windows 10 or 11, since Broker 
            // already tries CachedAuth with its PCAWrapper object built using withBroker(options).
            // The same applies on macOS where the broker handles its own silent attempt.
            // Note: If broker is requested on macOS but unavailable, we throw before reaching here.
            bool brokerWillRun = authMode.IsBroker() && (platformUtils.IsWindows10Or11() || platformUtils.IsMacOSBrokerAvailable());
            if (!brokerWillRun)
            {
                flows.Add(new CachedAuth(logger, authParams, preferredDomain, pcaWrapper));
            }

            // We try IWA as the first auth flow as it works for any Windows version
            // and tries to auth silently.
            if (authMode.IsIWA() && platformUtils.IsWindows())
            {
                flows.Add(new IntegratedWindowsAuthentication(logger, authParams, preferredDomain, pcaWrapper));
            }

            // This check silently fails on winserver if broker has been requested.
            // Future: Consider making AuthMode platform aware at Runtime.
            // https://github.com/AzureAD/microsoft-authentication-cli/issues/55
            if (authMode.IsBroker())
            {
                if (platformUtils.IsWindows10Or11())
                {
                    flows.Add(new Broker(logger, authParams, preferredDomain: preferredDomain, pcaWrapper: pcaWrapper, promptHint: promptHint, platformUtils: platformUtils));
                }
                else if (platformUtils.IsMacOS())
                {
                    if (platformUtils.IsMacOSBrokerAvailable())
                    {
                        flows.Add(new Broker(logger, authParams, preferredDomain: preferredDomain, pcaWrapper: pcaWrapper, promptHint: promptHint, platformUtils: platformUtils));
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Broker authentication was requested but is not available on this machine. " +
                            "macOS broker requires Company Portal version 5.2603.0 or later " +
                            $"(checked: {PlatformUtils.CompanyPortalAppPath}). " +
                            "Please install or update Company Portal, then try again.");
                    }
                }
            }

            if (authMode.IsWeb())
            {
                flows.Add(new Web(logger, authParams, preferredDomain: preferredDomain, pcaWrapper: pcaWrapper, promptHint: promptHint));
            }

            if (authMode.IsDeviceCode())
            {
                flows.Add(new DeviceCode(logger, authParams, preferredDomain: preferredDomain, pcaWrapper: pcaWrapper, promptHint: promptHint));
            }

            return flows;
        }
    }
}
