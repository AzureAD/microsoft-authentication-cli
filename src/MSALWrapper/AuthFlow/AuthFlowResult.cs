// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
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
        public IList<Exception> Errors { get; internal set; } = new List<Exception>();

        /// <summary>
        /// Gets a value indicating whether the TokenResult represents a non-null <see cref="MSALWrapper.TokenResult"/>.
        /// </summary>
        public bool Success
        {
            get { return this.TokenResult != null; }
        }
    }
}
