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
    /// <remarks>
    /// These are extension methods on <see cref="AuthFlowResult"/> and
    /// <see cref="IEnumerable{AuthFlowResult}"/> because they use types owned by other libraries (Lasso and MSALWrapper).
    /// We don't want to cross those boundaries and make our MSALWRapper aware of types from a CLI framework. AzureAuth is what
    /// knows about both these things and is defining functions to move from one to the other. They are extensions purely for
    /// syntactic sugar.
    /// </remarks>
    public static class AuthFlowResultExtensions
    {
        /// <summary>
        /// Convert to an <see cref="Office.Lasso.Telemetry.EventData"/>.
        /// </summary>
        /// <param name="result">Current <see cref="AuthFlowResult"/>.</param>
        /// <returns>An <see cref="Office.Lasso.Telemetry.EventData"/> representng this <see cref="AuthFlowResult"/>.</returns>
        /// <remarks>
        /// Why is this an extension method? <see cref="AuthFlowResult"/> is owned by <see cref="MSALWrapper"/>
        /// and <see cref="Office.Lasso.Telemetry.EventData"/> is owned by <see cref="Office.Lasso"/>.
        /// So <see cref="AuthFlowResult"/> does not actually know what an <see cref="Office.Lasso.Telemetry.EventData"/> is
        /// without bringing a reference to the CLI framework into the <see cref="MSALWrapper"/>. This is not a boundary we want to cross.
        /// Instead, here in AzureAuth, we can define the conversion using an extension method to house it rather than an arbitrary
        /// static class to hold the same function. Being an extension here is purely syntactic sugar. The main preference is
        /// around defining a "pure" function for this.
        /// </remarks>
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

        /// <summary>
        /// Send telemetry events for n <see cref="AuthFlowResult"/> instance.
        /// </summary>
        /// <param name="results"><see cref="AuthFlowResult"/>s to send telemetry for.</param>
        /// <param name="telemetryService">The <see cref="ITelemetryService"/> to use to send the events.</param>
        /// /// <remarks>
        /// Why is this an extension method? <see cref="AuthFlowResult"/> is owned by <see cref="MSALWrapper"/>
        /// and <see cref="ITelemetryService"/> is owned by <see cref="Office.Lasso"/>.
        /// So <see cref="AuthFlowResult"/> does not actually know what a <see cref="ITelemetryService"/> is
        /// without bringing a reference to the CLI framework into the <see cref="MSALWrapper"/>. This is not a boundary we want to cross.
        /// Instead, here in AzureAuth, we can define the conversion using an extension method to house it rather than an arbitrary
        /// static class to hold the same function. Being an extension here is purely syntactic sugar. The main preference is
        /// around defining a "pure" function for this.
        /// </remarks>
        public static void SendTelemetry(this IEnumerable<AuthFlowResult> results, ITelemetryService telemetryService)
        {
            foreach (var result in results)
            {
                telemetryService.SendEvent($"authflow_{result.AuthFlowName}", result.EventData());
            }
        }
    }
}
