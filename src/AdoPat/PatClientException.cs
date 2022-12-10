namespace Microsoft.Authentication.AdoPat
{
    using System;

    /// <summary>
    /// PAT client exception.
    /// </summary>
    public class PatClientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PatClientException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public PatClientException(string message)
            : base(message)
        {
        }
    }
}
