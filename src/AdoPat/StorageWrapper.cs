// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using Microsoft.Identity.Client.Extensions.Msal;

    /// <summary>
    /// TODO.
    /// </summary>
    public class StorageWrapper : IStorageWrapper
    {
        private Storage storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageWrapper"/> class.
        /// </summary>
        /// <param name="storage">An instance of <see cref="Storage"/>.</param>
        public StorageWrapper(Storage storage)
        {
            this.storage = storage;
        }

        /// <inheritdoc/>
        public byte[] ReadData()
        {
            return this.storage.ReadData();
        }

        /// <inheritdoc/>
        public void WriteData(byte[] data)
        {
            this.storage.WriteData(data);
        }
    }
}
