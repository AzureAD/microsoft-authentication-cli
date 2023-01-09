// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// TODO.
    /// </summary>
    public interface IStorageWrapper
    {
        /// <summary>
        /// TODO.
        /// </summary>
        /// <returns>TODO.</returns>
        byte[] ReadData();

        /// <summary>
        /// TODO.
        /// </summary>
        /// <param name="data">TODO.</param>
        void WriteData(byte[] data);
    }
}
