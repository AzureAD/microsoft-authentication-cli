// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.IO;

    /// <summary>AzureAuth constant values.</summary>
    internal static class Constants
    {
        /// <summary>
        /// The default application directory for AzureAuth.
        /// On Windows this is <c>%LOCALAPPDATA%\Programs\AzureAuth</c>.
        /// On Unix-like platforms this is <c>~/.azureauth</c>.
        /// </summary>
#if PlatformWindows
        public static readonly string AppDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "AzureAuth");
#else
        public static readonly string AppDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".azureauth");
#endif
    }
}
