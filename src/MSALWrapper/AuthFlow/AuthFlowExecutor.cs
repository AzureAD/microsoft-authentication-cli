// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
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
            this.telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            this.authflows = authFlows ?? throw new ArgumentNullException(nameof(authFlows));
        }

        /// <summary>
        /// Gets the auth flow result.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task<AuthFlowResult> GetTokenAsync()
        {
            AuthFlowResult result = new AuthFlowResult(null, new List<Exception>());
            foreach (var authFlow in this.authflows)
            {
                var attempt = await authFlow.GetTokenAsync();
                string authFlowName = authFlow.GetType().Name;
                this.SendTelemetryEvent(attempt, authFlowName);

                if (attempt == null)
                {
                    var oopsMessage = $"Auth flow '{authFlowName}' returned a null AuthFlowResult.";
                    result.Errors.Add(new Exception(oopsMessage));
                    this.logger.LogDebug(oopsMessage);
                }
                else
                {
                    result.AddErrors(attempt.Errors);

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
            var eventData = attempt.EventData;
            var errorListJSON = ExceptionListToStringConverter.SerializeExceptions(attempt.Errors);
            eventData.Add("errors", errorListJSON);
            eventData.Add("success", attempt.Success);

            if (attempt.Success)
            {
                string correlationID = attempt.TokenResult.CorrelationID.ToString();
                attempt.CorrelationIDs.Add(correlationID);
                eventData.Add("msal_correlation_ids", attempt.CorrelationIDs);
                eventData.Add("token_validity_hours", attempt.TokenResult.ValidFor.Hours);
            }

            this.telemetryService.SendEvent($"authflow_{authFlowName}", eventData);
        }
    }
}
