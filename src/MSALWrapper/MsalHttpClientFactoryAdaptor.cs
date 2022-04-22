// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlow
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The msal http client factory adaptor.
    /// </summary>
    internal class MsalHttpClientFactoryAdaptor : IMsalHttpClientFactory
    {
        private HttpClient instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsalHttpClientFactoryAdaptor"/> class.
        /// Creates an instance of msal http client factory adaptor.
        /// </summary>
        /// <param name="instance">A http client.</param>
        public MsalHttpClientFactoryAdaptor(HttpClient instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            this.instance = instance;
        }

        /// <inheritdoc/>
        public HttpClient GetHttpClient()
        {
            // MSAL calls this method each time it wants to use an HTTP client.
            // We ensure we only create a single instance to avoid socket exhaustion.
            return this.instance;
        }

        /// <summary>
        /// Gets the msal http client.
        /// </summary>
        /// <returns>An instance of <see cref="HttpClient"/>.</returns>
        public HttpClient CreateHttpClient()
        {
            // MSAL calls this method each time it wants to use an HTTP client.
            // We ensure we only create a single instance to avoid socket exhaustion.
            HttpClientHandler handler = new HttpClientHandler();
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
            };

            return client;
        }
    }
}
