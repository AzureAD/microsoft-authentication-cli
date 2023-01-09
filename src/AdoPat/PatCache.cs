// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;

    /// <summary>
    /// TODO.
    /// </summary>
    public class PatCache
    {
        private IStorageWrapper storage;
        private Lazy<Dictionary<string, PatToken>> cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatCache"/> class.
        /// </summary>
        /// <param name="storage">TODO.</param>
        public PatCache(IStorageWrapper storage)
        {
            this.storage = storage;
            this.cache = new Lazy<Dictionary<string, PatToken>>(() => this.ReadStorage());
        }

        /// <summary>
        /// TODO.
        /// </summary>
        /// <param name="key">TODO.</param>
        /// <returns>TODO.</returns>
        public PatToken GetPat(string key)
        {
            PatToken token = null;
            var cache = this.Cache();
            cache.TryGetValue(key, out token);
            return token;
        }

        /// <summary>
        /// TODO.
        /// </summary>
        /// <param name="key">TODO.</param>
        /// <param name="patToken">TODO.</param>
        /// <returns>TODO.</returns>
        public PatToken PutPat(string key, PatToken patToken)
        {
            var data = Encoding.UTF8.GetBytes("data");
            this.storage.WriteData(data);
            return null;
        }

        private Dictionary<string, PatToken> Cache()
        {
            return this.cache.Value;
        }

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
