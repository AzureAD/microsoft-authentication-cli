// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    /// <summary>
    /// An interface for a thin, testable wrapper around MSAL's Storage class.
    /// </summary>
    public interface IStorageWrapper
    {
        /// <summary>
        /// Read and unprotect cache data.
        /// </summary>
        /// <returns>Unprotected cache data.</returns>
        byte[] ReadData();

        /// <summary>
        /// Protect and write cache data to a file. It overrides existing data.
        /// </summary>
        /// <param name="data">Unprotected data to be cached.</param>
        void WriteData(byte[] data);
    }
}
