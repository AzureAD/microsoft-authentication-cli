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
        /// Initializes a new instance of the <see cref="AuthFlowResult"/> class.
        /// </summary>
        /// <param name="tokenResult">A <see cref="MSALWrapper.TokenResult"/>.</param>
        /// <param name="errors">A list of errors encountered while getting (or failing to get) the given token result.</param>
        public AuthFlowResult(TokenResult tokenResult, IList<Exception> errors)
        {
            this.TokenResult = tokenResult;
            this.Errors = errors ?? new List<Exception>();
        }

        /// <summary>
        /// Gets the token result.
        /// </summary>
        public TokenResult TokenResult { get; internal set; }

        /// <summary>
        /// Gets the list of errors.
        /// </summary>
        public IList<Exception> Errors { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the TokenResult represents a non-null <see cref="MSALWrapper.TokenResult"/>.
        /// </summary>
        public bool Success
        {
            get { return this.TokenResult != null; }
        }

        /// <summary>
        /// Adds the errors from the attempted auth flows to the main error list on Auth flow executor.
        /// </summary>
        /// <param name="authFlowResult">A <see cref="MSALWrapper.AuthFlowResult"/> that creates a main list of errors after attempting all the auth flows in the list.</param>
        /// <param name="result">A <see cref="MSALWrapper.AuthFlowResult"/> that collects errors from the attempted auth flows at a time.</param>
        internal static void AddErrorsToAuthFlowExecutorList(AuthFlowResult authFlowResult, AuthFlowResult result)
        {
            foreach (var error in result.Errors)
            {
                authFlowResult.Errors.Add(error);
            }
        }
    }
}
