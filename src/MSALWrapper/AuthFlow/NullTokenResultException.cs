// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;

    /// <summary>
    /// The null token result exception, which should be thrown when an <see cref="IAuthFlow"/> returns a null <see cref="TokenResult"/>.
    /// </summary>
    public class NullTokenResultException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullTokenResultException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public NullTokenResultException(string message)
            : base(message)
        {
        }
    }
}
