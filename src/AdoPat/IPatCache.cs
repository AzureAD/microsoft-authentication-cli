// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;

    /// <summary>
    /// A simple cache mapping <see cref="string"/> to <see cref="PatToken"/> backed by secure storage.
    /// </summary>
    public interface IPatCache
    {
        /// <summary>
        /// Get a <see cref="PatToken"/> from the cache.
        /// </summary>
        /// <param name="key">The key for the target entry.</param>
        /// <returns>The target value.</returns>
        PatToken Get(string key);

        /// <summary>
        /// Put a <see cref="PatToken"/> into the cache. May overwrite existing values.
        /// </summary>
        /// <param name="key">The key for this entry.</param>
        /// <param name="patToken">The value for this entry.</param>
        void Put(string key, PatToken patToken);
    }
}
