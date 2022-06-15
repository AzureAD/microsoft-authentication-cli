// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// The auth flows class.
    /// </summary>
    public class AuthFlowExecutor : IAuthFlow
    {
        private readonly IEnumerable<IAuthFlow> authflows;
        private readonly ILogger logger;
        private readonly ITelemetryService telemetryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthFlowExecutor"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="telemetryService">The telemetry service.</param>
        /// <param name="authFlows">The list of auth flows.</param>
        public AuthFlowExecutor(ILogger logger, ITelemetryService telemetryService, IEnumerable<IAuthFlow> authFlows)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(logger));
            this.authflows = authFlows ?? throw new ArgumentNullException(nameof(authFlows));
        }

        /// <summary>
        /// Get a auth flow result.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            AuthFlowResult result = new AuthFlowResult(null, new List<Exception>(), 0);

            if (this.authflows.Count() == 0)
            {
                this.logger.LogWarning("Warning: There are 0 auth modes to execute!");
            }

            foreach (var authFlow in this.authflows)
            {
                var authFlowName = authFlow.GetType().Name;
                this.logger.LogDebug($"Starting {authFlowName}...");

                var attempt = await authFlow.GetTokenAsync();

                if (attempt == null)
                {
                    var oopsMessage = $"Auth flow '{authFlowName}' returned a null AuthFlowResult.";
                    result.Errors.Add(new NullTokenResultException(oopsMessage));
                    this.logger.LogDebug(oopsMessage);
                }
                else
                {
                    result.AddErrors(attempt.Errors);
                    this.SendTelemetryEvent(attempt, authFlowName);

                    this.logger.LogDebug($"{authFlowName} success: {attempt.Success}.");
                    if (attempt.Success)
                    {
                        result.TokenResult = attempt.TokenResult;
                        break;
                    }
                }
            }

            return result;
        }

        private void SendTelemetryEvent(AuthFlowResult attempt, string authFlowName)
        {
            var eventData = new EventData();
            eventData.Add("auth_mode", authFlowName);
            eventData.Add("success", attempt.Success);
            eventData.Add("errors", ExceptionListToStringConverter.SerializeExceptions(attempt.Errors));
            eventData.Add("no_of_interactive_prompts", attempt.InteractivePromptCount);
            List<string> correlationIDs = ExceptionsExtensions.ExtractCorrelationIDsFromException(attempt.Errors);

            if (attempt.Success)
            {
                correlationIDs.Add(attempt.TokenResult.CorrelationID.ToString());
                eventData.Add("token_validity_hours", attempt.TokenResult.ValidFor.Hours);
                eventData.Add("is_silent", attempt.TokenResult.AuthType == AuthType.Silent);
            }

            eventData.Add("msal_correlation_ids", correlationIDs);
            this.telemetryService.SendEvent($"authflow_{authFlowName}", eventData);
        }
    }
}
