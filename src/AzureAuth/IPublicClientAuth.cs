// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Authentication.MSALWrapper;

    /// <summary>
    /// An interface for getting a <see cref="TokenResult"/>.
    /// </summary>
    public interface IPublicClientAuth
    {
        TokenResult Token(Guid client, Guid tenant, IEnumerable<string> scopes, AuthMode[] authModes, string domain, string prompt, TimeSpan timeout);
    }
}
