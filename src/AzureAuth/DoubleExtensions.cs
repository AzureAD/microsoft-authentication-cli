// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;

    /// <summary>
    /// Extensions to <see cref="double"/>.
    /// </summary>
    internal static class DoubleExtensions
    {
        /// <summary>
        /// Create a <see cref="TimeSpan"/> of n minutes.
        /// </summary>
        /// <param name="n">Number of minutes.</param>
        /// <returns><see cref="TimeSpan"/> with n minutes.</returns>
        public static TimeSpan Minutes(this double n)
        {
            return TimeSpan.FromMinutes(n);
        }
    }
}
