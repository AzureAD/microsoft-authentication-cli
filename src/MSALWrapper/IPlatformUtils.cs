// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    /// <summary>
    /// An interface for platform utility methods.
    /// </summary>
    public interface IPlatformUtils
    {
        /// <summary>
        /// Check if running on Windows 10 or 11.
        /// </summary>
        /// <returns><see cref="bool"/> - true if running on Windows 10 or 11.</returns>
        bool IsWindows10Or11();

        /// <summary>
        /// Check if running on any version of Windows.
        /// </summary>
        /// <returns><see cref="bool"/> - true if running on any version of Windows.</returns>
        bool IsWindows();
        /// <summary>
        /// Check if running on macOS.
        /// </summary>
        /// <returns><see cref="bool"/> - true if running on macOS.</returns>
        bool IsMacOS();

        /// <summary>
        /// Check if macOS brokered authentication is available.
        /// Requires macOS, Company Portal installed, and CP version >= 2603.
        /// </summary>
        /// <returns><see cref="bool"/> - true if macOS broker prerequisites are met.</returns>
        bool IsMacOSBrokerAvailable();
    }
}
