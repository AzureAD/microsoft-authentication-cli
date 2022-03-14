// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;

    /// <summary>
    /// The auth mode.
    /// </summary>
    [Flags]
    public enum AuthMode : short
    {
        /// <summary>
        /// The web.
        /// </summary>
        Web = 1 << 0,

        /// <summary>
        /// The device code.
        /// </summary>
        DeviceCode = 1 << 1,

#if PlatformWindows
        /// <summary>
        /// The broker.
        /// </summary>
        Broker = 1 << 2,

        /// <summary>
        /// All auth modes.
        /// </summary>
        All = Broker | Web | DeviceCode,

        /// <summary>
        /// The default.
        /// </summary>
        Default = Broker,
#else
        /// <summary>
        /// The all mode.
        /// </summary>
        All = Web | DeviceCode,

        /// <summary>
        /// The default mode.
        /// </summary>
        Default = Web,
#endif
    }

    /// <summary>
    /// The auth mode extensions.
    /// </summary>
    public static class AuthModeExtensions
    {
        /// <summary>
        /// The is broker.
        /// </summary>
        /// <param name="authMode">
        /// The auth mode.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool IsBroker(this AuthMode authMode)
        {
#if PlatformWindows
            return (AuthMode.Broker & authMode) == AuthMode.Broker;
#else
            return false;
#endif
        }

        /// <summary>
        /// The is web.
        /// </summary>
        /// <param name="authMode">
        /// The auth mode.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool IsWeb(this AuthMode authMode)
        {
            return (AuthMode.Web & authMode) == AuthMode.Web;
        }

        /// <summary>
        /// The is device code.
        /// </summary>
        /// <param name="authMode">
        /// The auth mode.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool IsDeviceCode(this AuthMode authMode)
        {
            return (AuthMode.DeviceCode & authMode) == AuthMode.DeviceCode;
        }
    }
}
