// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The Auth Flow result.
    /// </summary>
    public class AuthFlowResult
    {
        /// <summary>
        /// Gets the token result.
        /// </summary>
        public TokenResult TokenResult { get; internal set; }

        /// <summary>
        /// Gets the list of errors.
        /// </summary>
        public IList<Exception> Errors { get; internal set; }
    }
}
