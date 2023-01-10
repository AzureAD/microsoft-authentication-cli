// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;

    /// <summary>
    /// A simple cache mapping <see cref="string"/> to <see cref="PatToken"/> backed by secure storage.
    /// </summary>
    public class PatCache
    {
        private IStorageWrapper storage;
        private Lazy<Dictionary<string, PatToken>> cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatCache"/> class.
        /// </summary>
        /// <param name="storage">A class which implements <see cref="IStorageWrapper"/>.</param>
        public PatCache(IStorageWrapper storage)
        {
            // Note: This assumes any required persistance validity has been
            // checked beforehand.
            this.storage = storage;

            // The cache is lazy so that the initial cache read from the
            // underlying storage only happens once and subsequent reads
            // operate on the in-memory value.
            this.cache = new Lazy<Dictionary<string, PatToken>>(() => this.ReadStorage());
        }

        /// <summary>
        /// Get a <see cref="PatToken"/> from the cache.
        /// </summary>
        /// <param name="key">The key for the target entry.</param>
        /// <returns>The target value.</returns>
        public PatToken GetPat(string key)
        {
            PatToken token = null;
            this.cache.Value.TryGetValue(key, out token);
            return token;
        }

        /// <summary>
        /// Put a <see cref="PatToken"/> into the cache. May overwrite existing values.
        /// </summary>
        /// <param name="key">The key for this entry.</param>
        /// <param name="patToken">The value for this entry.</param>
        public void PutPat(string key, PatToken patToken)
        {
            // Insert or overwrite the key with the given PatToken. Skip
            // intermediate string serialization by serializing directly to
            // UTF-8 bytes.
            this.cache.Value[key] = patToken;
            var data = JsonSerializer.SerializeToUtf8Bytes(this.cache.Value);

            // We follow a "write-through" caching method to ensure the
            // persistent storage never differs from the in-memory cache.
            this.storage.WriteData(data);
        }

        // A helper method to wrap cache deserialization and ensure a Dictionary is always returned.
        private Dictionary<string, PatToken> ReadStorage()
        {
            var data = this.storage.ReadData();

            // Interpret an uninitialized cache as an empty JSON object.
            if (data.Length == 0)
            {
                data = Encoding.UTF8.GetBytes("{}");
            }

            var span = new ReadOnlySpan<byte>(data);
            return JsonSerializer.Deserialize<Dictionary<string, PatToken>>(span);
        }
    }
}
