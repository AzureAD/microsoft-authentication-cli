// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlows
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.Identity.Client;

    /// <summary>
    /// The msal http client factory adaptor.
    /// </summary>
    internal class MsalHttpClientFactoryAdaptor : IMsalHttpClientFactory
    {
        /// <summary>
        /// The get http client.
        /// </summary>
        /// <returns>An instance of <see cref="HttpClient"/>.</returns>

        public HttpClient GetHttpClient()
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
