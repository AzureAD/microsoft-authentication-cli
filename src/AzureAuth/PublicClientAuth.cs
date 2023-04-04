// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client.NativeInterop;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// A class for handling AAD Token acquisition, results logging, and telemetry collection.
    /// </summary>
    public class PublicClientAuth : IPublicClientAuth
    {
        private readonly ILogger logger;
        private readonly IEnv env;
        private readonly ITelemetryService telemetryService;
        private readonly IMsalWrapper msalWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicClientAuth"/> class.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger"/>.</param>
        /// <param name="env">An <see cref="IEnv"/>.</param>
        /// <param name="telemetryService">An <see cref="ITelemetryService"/>.</param>
        /// <param name="msalWrapper">An <see cref="IMsalWrapper"/>.</param>
        /// <exception cref="ArgumentNullException">All parameters must not be null.</exception>
        public PublicClientAuth(ILogger logger, IEnv env, ITelemetryService telemetryService, IMsalWrapper msalWrapper)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            this.msalWrapper = msalWrapper ?? throw new ArgumentNullException(nameof(msalWrapper));
        }

        /// <inheritdoc/>
        public TokenResult Token(MSALWrapper.AuthParameters authParams, IEnumerable<AuthMode> authModes, string domain, string prompt, TimeSpan timeout, EventData eventData)
        {
            var result = this.msalWrapper.AccessToken(
                this.logger,
                authParams,
                authModes.Combine().PreventInteractionIfNeeded(this.env, this.logger),
                domain,
                PromptHint.Prefixed(prompt),
                timeout);

            // Report individual auth flow telemetry
            result.Attempts.SendTelemetry(this.telemetryService);

            var totalErrorCount = result.Attempts.SelectMany(attempt => attempt.Errors).Count();
            eventData.Add("error_count", totalErrorCount);
            eventData.Add("authflow_count", result.Attempts.Count);

            var authflow = result.Success;
            if (authflow == null)
            {
                foreach (var attempt in result.Attempts)
                {
                    this.logger.LogDebug($"{attempt.AuthFlowName} failed after {attempt.Duration.TotalSeconds:0.00} sec. Error count: {attempt.Errors.Count}");
                    foreach (var e in attempt.Errors)
                    {
                        this.logger.LogDebug($"  {e.Message}");
                    }
                }

                return null;
            }

            this.logger.LogDebug($"Acquired an AAD access token via {authflow.AuthFlowName} in {authflow.Duration.TotalSeconds:0.00} sec");
            eventData.Add("silent", authflow.TokenResult.IsSilent);
            eventData.Add("sid", authflow.TokenResult.SID);
            eventData.Add("succeeded_mode", authflow.AuthFlowName);

            return authflow.TokenResult;
        }
    }
}
