// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlows
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The task executor.
    /// </summary>
    internal class TaskExecutor
    {
        /// <summary>
        /// The complete within.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="timeout">The timeout.</param>
        /// <param name="taskName">The task name.</param>
        /// <param name="getTask">The get task.</param>
        /// <param name="errorsList">The errors list.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        internal static async Task<T> CompleteWithin<T>(TimeSpan timeout, string taskName, Func<CancellationToken, Task<T>> getTask, List<Exception> errorsList = null, ILogger logger = null)
            where T : class
        {
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeout);
            try
            {
                logger?.LogDebug($"{taskName} has {timeout.TotalMinutes} minutes to complete before timeout.");
                return await getTask(source.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                var warningMessage = $"{taskName} timed out after {timeout.TotalMinutes} minutes.";
                logger?.LogWarning(warningMessage);
                errorsList?.Add(new AuthenticationTimeoutException(warningMessage));
                return null;
            }
        }
    }
}
