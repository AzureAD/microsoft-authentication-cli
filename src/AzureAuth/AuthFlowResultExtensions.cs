// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// Extension methods for <see cref="AuthFlowResult"/>.
    /// </summary>
    public static class AuthFlowResultExtensions
    {
        public static void SendTelemetry(this IEnumerable<AuthFlowResult> results, ITelemetryService telemetryService)
        {

        }

        /// <summary>
        /// Convert to an <see cref="Office.Lasso.Telemetry.EventData"/>.
        /// </summary>
        /// <param name="result">Current <see cref="AuthFlowResult"/>.</param>
        /// <returns>An <see cref="Office.Lasso.Telemetry.EventData"/> representng this <see cref="AuthFlowResult"/>.</returns>
        public static EventData EventData(this AuthFlowResult result)
        {
            if (result == null)
            {
                return null;
            }

            var eventData = new EventData();
            eventData.Add("authflow", result.AuthFlowName);
            eventData.Add("success", result.Success);
            eventData.Add("duration_milliseconds", (int)result.Duration.TotalMilliseconds);

            if (result.Errors.Any())
            {
                var error_messages = ExceptionListToStringConverter.Execute(result.Errors);
                eventData.Add("error_messages", error_messages);
            }

            if (result.Success)
            {
                eventData.Add("msal_correlation_id", result.TokenResult.CorrelationID.ToString());
                eventData.Add("token_validity_minutes", result.TokenResult.ValidFor.TotalMinutes);
                eventData.Add("silent", result.TokenResult.IsSilent);
            }

            return eventData;
        }
    }
}
