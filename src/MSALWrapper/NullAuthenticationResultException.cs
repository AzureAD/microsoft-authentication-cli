// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;

    /// <summary>
    /// The null authentication result exception.
    /// </summary>
    internal class NullAuthenticationResultException : ArgumentNullException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullAuthenticationResultException"/> class.
        /// </summary>
        public NullAuthenticationResultException()
            : base("AuthenticationResult cannot be null!")
        {
        }
    }
}
