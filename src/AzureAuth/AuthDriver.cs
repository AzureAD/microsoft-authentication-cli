// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Interfaces;

    /// <summary>
    /// A class for handling AAD Token acuisition, results logging, and telemetry collection.
    /// </summary>
    public class AuthDriver
    {
        private readonly ILogger logger;
        private readonly IEnv env;
        private readonly ITelemetryService telemetryService;
        private readonly ITokenFetcher tokenFetcher;

        public AuthDriver(ILogger logger, IEnv env, ITelemetryService telemetryService, ITokenFetcher tokenFetcher)
        {
            this.logger = logger;
            this.env = env;
            this.telemetryService = telemetryService;
            this.tokenFetcher = tokenFetcher;
        }
    }
}
