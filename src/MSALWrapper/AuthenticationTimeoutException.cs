// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;

    /// <summary>
    /// Authentication timeout exception.
    /// </summary>
    public class AuthenticationTimeoutException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationTimeoutException"/> class.
        /// </summary>
        /// <param name="message">/// The message.</param>
        public AuthenticationTimeoutException(string message)
            : base(message)
        {
        }
    }
}
