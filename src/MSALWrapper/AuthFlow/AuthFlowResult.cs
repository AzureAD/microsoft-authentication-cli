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
        /// Initializes a new instance of the <see cref="AuthFlowResult"/> class with a null TokenResult and empty error list.
        /// </summary>
        public AuthFlowResult()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthFlowResult"/> class.
        /// </summary>
        /// <param name="tokenResult">A <see cref="MSALWrapper.TokenResult"/>.</param>
        /// <param name="errors">A list of errors encountered while getting (or failing to get) the given token result. Will initialize a new empty List if null is given.</param>
        public AuthFlowResult(TokenResult tokenResult, IList<Exception> errors)
        {
            this.TokenResult = tokenResult;
            this.Errors = errors ?? new List<Exception>();
        }

        /// <summary>
        /// Get a token result.
        /// </summary>
        public TokenResult TokenResult { get; internal set; }

        /// <summary>
        /// Get a list of errors.
        /// </summary>
        public IList<Exception> Errors { get; internal set; }

        /// <summary>
        /// Get a value indicating whether the TokenResult represents a non-null <see cref="MSALWrapper.TokenResult"/>.
        /// </summary>
        public bool Success
        {
            get { return this.TokenResult != null; }
        }

        /// <summary>
        /// Add the given errors to the <see cref="Errors"/> list.
        /// </summary>
        /// <param name="errors">The errors to add.</param>
        public void AddErrors(IEnumerable<Exception> errors)
        {
            foreach (var error in errors)
            {
                this.Errors.Add(error);
            }
        }
    }
}
