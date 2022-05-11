// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;

    /// <summary>
    /// The bad authflow implementation exception.
    /// </summary>
    public class BadAuthFlowImplementException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BadAuthFlowImplementException"/> class.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public BadAuthFlowImplementException(string message)
            : base(message)
        {
        }
    }
}
