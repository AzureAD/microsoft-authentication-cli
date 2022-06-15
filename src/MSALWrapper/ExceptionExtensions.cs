// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client;

/// <summary>
/// Exceptions extensions.
/// </summary>
public static class ExceptionsExtensions
{
    /// <summary>
    /// Convert exception to formatted string.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The <see cref="string"/>.</returns>
    /// <exception cref="ArgumentNullException">.</exception>
    public static string ToFormattedString(this Exception exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException("Cannot pass null to the exception extension method.");
        }

        IEnumerable<string> messages = exception
            .GetAllExceptions()
            .Where(e => !string.IsNullOrWhiteSpace(e.Message))
            .Select(e => e.Message.Trim());

        var flattened = string.Join(Environment.NewLine, messages);
        return flattened;
    }

    /// <summary>
    /// Extracts all inner exceptions.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The <see cref="IEnumerable"/>.</returns>
    /// <exception cref="ArgumentNullException">Argument Null Exception.</exception>
    public static IEnumerable<Exception> GetAllExceptions(this Exception exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException("Cannot pass null to the exception extension method.");
        }

        if (!(exception is AggregateException))
        {
            yield return exception;
        }

        if (exception is AggregateException aggregateException)
        {
            foreach (Exception innerEx in aggregateException.InnerExceptions.SelectMany(e => e.GetAllExceptions()))
            {
                yield return innerEx;
            }
        }
        else if (exception.InnerException != null)
        {
            foreach (Exception innerEx in exception.InnerException.GetAllExceptions())
            {
                yield return innerEx;
            }
        }
    }

    /// <summary>
    /// Extracts correlation IDs from the list of exceptions.
    /// </summary>
    /// <param name="exceptions">List of exceptions from which correlation IDs are extracted.</param>
    /// <returns>List of correlation IDs.</returns>
    public static List<string> ExtractCorrelationIDsFromException(IList<Exception> exceptions)
    {
        var correlationIDs = new List<string>();
        foreach (Exception exception in exceptions)
        {
            if (exception.GetType() == typeof(MsalServiceException))
            {
                var msalServiceException = (MsalServiceException)exception;
                correlationIDs.Add(msalServiceException.CorrelationId?.ToString());
            }
            else if (exception.GetType() == typeof(MsalUiRequiredException))
            {
                var msalUiRequiredException = (MsalUiRequiredException)exception;
                correlationIDs.Add(msalUiRequiredException.CorrelationId?.ToString());
            }
        }

        return correlationIDs;
    }
}
