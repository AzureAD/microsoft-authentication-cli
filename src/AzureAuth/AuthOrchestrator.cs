// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Interfaces;

    /// <summary>
    /// A class for handling AAD Token acquisition, results logging, and telemetry collection.
    /// </summary>
    public class AuthOrchestrator : IAuthOrchestrator
    {
        private readonly ILogger logger;
        private readonly IEnv env;
        private readonly ITelemetryService telemetryService;
        private readonly ITokenFetcher tokenFetcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthOrchestrator"/> class.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger"/>.</param>
        /// <param name="env">An <see cref="IEnv"/>.</param>
        /// <param name="telemetryService">An <see cref="ITelemetryService"/>.</param>
        /// <param name="tokenFetcher">An <see cref="ITokenFetcher"/>.</param>
        /// <exception cref="ArgumentNullException">All parameters must not be null.</exception>
        public AuthOrchestrator(ILogger logger, IEnv env, ITelemetryService telemetryService, ITokenFetcher tokenFetcher)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            this.tokenFetcher = tokenFetcher ?? throw new ArgumentNullException(nameof(tokenFetcher));
        }

        /// <inheritdoc/>
        public TokenResult Token(Guid client, Guid tenant, IEnumerable<string> scopes, AuthMode[] authModes, string domain, string prompt, TimeSpan timeout)
        {
            var result = this.tokenFetcher.AccessToken(
                this.logger,
                client,
                tenant,
                scopes,
                authModes.Combine().PreventInteractionIfNeeded(this.env),
                domain,
                PromptHint.Prefixed(prompt),
                timeout);

            result.Attempts.SendTelemetry(this.telemetryService);

            if (result.Success != null)
            {
                return result.Success.TokenResult;
            }

            return null;
        }
    }
}
