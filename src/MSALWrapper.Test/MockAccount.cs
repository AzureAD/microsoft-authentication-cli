// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using Microsoft.Identity.Client;

    /// <summary>
    /// A Mock implementation for <see cref="IAccount"/>.
    /// </summary>
    public class MockAccount : IAccount
    {
        private string userName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockAccount"/> class.
        /// </summary>
        /// <param name="userName">
        /// The user name.
        /// </param>
        public MockAccount(string userName)
        {
            this.userName = userName;
        }

        /// <summary>
        /// Gets the username.
        /// </summary>
        public string Username => this.userName;

        /// <summary>
        /// Gets the environment.
        /// </summary>
        public string Environment => throw new System.NotImplementedException();

        /// <summary>
        /// Gets home <see cref="AccountId"/>.
        /// </summary>
        public AccountId HomeAccountId => throw new System.NotImplementedException();
    }
}
