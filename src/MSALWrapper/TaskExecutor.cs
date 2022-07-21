// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The task executor class.
    /// </summary>
    internal static class TaskExecutor
    {
        /// <summary>
        /// Completes a task within the timeout period.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="logger">The logger.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="taskName">The task name.</param>
        /// <param name="getTask">A function that return the task you want to complete within the given timeout.</param>
        /// <param name="errorsList">The errors list.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        internal static async Task<T> CompleteWithin<T>(ILogger logger, TimeSpan timeout, string taskName, Func<CancellationToken, Task<T>> getTask, IList<Exception> errorsList)
            where T : class
        {
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(timeout);
            try
            {
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
