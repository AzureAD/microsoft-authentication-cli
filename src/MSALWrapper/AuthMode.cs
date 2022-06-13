// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;

    /// <summary>
    /// Auth modes.
    /// </summary>
    [Flags]
    public enum AuthMode : short
    {
        /// <summary>
        /// Web auth mode (Embedded Web View for Windows and System Web Browser for OSX).
        /// </summary>
        Web = 1 << 0,

        /// <summary>
        /// Device code flow auth mode.
        /// </summary>
        DeviceCode = 1 << 1,

#if PlatformWindows
        /// <summary>
        /// Broker auth mode(WAM - Web account Manager).
        /// </summary>
        Broker = 1 << 2,

        /// <summary>
        /// Integrated Windows auth mode.
        /// </summary>
        IWA = 1 << 3,

        /// <summary>
        /// All auth modes.
        /// </summary>
        All = IWA | Broker | Web | DeviceCode,

        /// <summary>
        /// Default auth mode.
        /// </summary>
        Default = IWA | Broker | Web,
#else
        /// <summary>
        /// All auth modes.
        /// </summary>
        All = Web | DeviceCode,

        /// <summary>
        /// Default auth mode.
        /// </summary>
        Default = Web,
#endif
    }

    /// <summary>
    /// Auth mode extensions.
    /// </summary>
    public static class AuthModeExtensions
    {
        /// <summary>
        /// Checks if authMode is broker.
        /// </summary>
        /// <param name="authMode">The auth mode.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public static bool IsBroker(this AuthMode authMode)
        {
#if PlatformWindows
            return (AuthMode.Broker & authMode) == AuthMode.Broker;
#else
            return false;
#endif
        }

        /// <summary>
        /// Checks if authMode is web.
        /// </summary>
        /// <param name="authMode">The auth mode.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public static bool IsWeb(this AuthMode authMode)
        {
            return (AuthMode.Web & authMode) == AuthMode.Web;
        }

        /// <summary>
        /// Checks if authMode is device code.
        /// </summary>
        /// <param name="authMode">The auth mode.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public static bool IsDeviceCode(this AuthMode authMode)
        {
            return (AuthMode.DeviceCode & authMode) == AuthMode.DeviceCode;
        }

        /// <summary>
        /// Checks if authMode is IWA Enabled.
        /// </summary>
        /// <param name="authMode">The <see cref="AuthMode"/>.</param>
        /// <returns>true or false.</returns>
        public static bool IsIWA(this AuthMode authMode)
        {
#if PlatformWindows
            return (AuthMode.IWA & authMode) == AuthMode.IWA;
#else
            return false;
#endif
        }
    }
}
