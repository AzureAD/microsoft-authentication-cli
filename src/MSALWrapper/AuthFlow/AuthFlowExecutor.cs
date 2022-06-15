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

                    EventData eventData = this.GenerateEventData(result, authFlowName);
                    this.telemetryService.SendEvent($"authflow_{authFlowName}", eventData);

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

        /// <summary>
        /// Gets an instance of <see cref="EventData"/> by extracting information from an AuthFlowResult instance.
        /// </summary>
        /// <param name="result">An instance of AuthFlowResult from which eventdata is populated.</param>
        /// <param name="authFlowName">Name of the auth flow.</param>
        /// <returns>Returns an instance of EventData.</returns>
        public EventData GenerateEventData(AuthFlowResult result, string authFlowName)
        {
            var eventData = new EventData();
            eventData.Add("auth_mode", authFlowName);
            eventData.Add("success", result.Success);
            eventData.Add("errors", ExceptionListToStringConverter.SerializeExceptions(result.Errors));
            eventData.Add("no_of_interactive_prompts", result.InteractivePromptsCount);
            List<string> correlationIDs = ExceptionsExtensions.ExtractCorrelationIDsFromException(result.Errors);

            if (result.Success)
            {
                correlationIDs.Add(result.TokenResult.CorrelationID.ToString());
                eventData.Add("token_validity_hours", result.TokenResult.ValidFor.Hours);
                eventData.Add("is_silent", result.TokenResult.AuthType == AuthType.Silent);
            }

            eventData.Add("msal_correlation_ids", correlationIDs);
            return eventData;
        }
    }
}
