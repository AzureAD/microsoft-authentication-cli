// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The platform information.
    /// </summary>
    public struct PlatformInformation
    {
        /// <summary>
        /// The operating system type.
        /// </summary>
        public readonly string OperatingSystemType;

        /// <summary>
        /// The cpu architecture.
        /// </summary>
        public readonly string CpuArchitecture;

        /// <summary>
        /// The clr version.
        /// </summary>
        public readonly string ClrVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformInformation"/> struct.
        /// </summary>
        /// <param name="osType">
        /// The os type.
        /// </param>
        /// <param name="cpuArch">
        /// The cpu arch.
        /// </param>
        /// <param name="clrVersion">
        /// The clr version.
        /// </param>
        public PlatformInformation(string osType, string cpuArch, string clrVersion)
        {
            this.OperatingSystemType = osType;
            this.CpuArchitecture = cpuArch;
            this.ClrVersion = clrVersion;
        }
    }

    /// <summary>
    /// The platform utils.
    /// </summary>
    internal static class PlatformUtils
    {
        /// <summary>
        /// The method that checks for windows.
        /// </summary>
        /// <param name="logger">
        /// The logger.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool IsWindows10(ILogger logger)
        {
            if (!IsWindows(logger))
            {
                return false;
            }

#if NETFRAMEWORK
            logger.LogTrace("IsWindows10: Using NetFramework Check");

            // Implementation of version checking was taken from:
            // https://github.com/dotnet/runtime/blob/6578f257e3be2e2144a65769706e981961f0130c/src/libraries/System.Private.CoreLib/src/System/Environment.Windows.cs#L110-L122
            //
            // Note that we cannot use Environment.OSVersion in .NET Framework (or Core versions less than 5.0) as
            // the implementation in those versions "lies" about Windows versions > 8.1 if there is no application manifest.
            if (RtlGetVersionEx(out RTL_OSVERSIONINFOEX osvi) != 0)
            {
                logger.LogTrace($"IsWindows10: osvi was not 0 it was {osvi}");
                return false;
            }

            logger.LogTrace($"IsWindows10: osvi.dwMajorVersion is {osvi.DwMajorVersion}");
            return (int)osvi.DwMajorVersion == 10;
#else
            logger.LogTrace("IsWindows10: Using NetStandard Check");
            try
            {
                var os = Environment.OSVersion;
                logger.LogTrace($"{os}");
                logger.LogTrace($"{os.Version}");

                return os.Version.Major == 10 && os.Version.Minor == 0;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"IsWindows10 check failed:\n{ex}");
                return false;
            }
#endif
        }

        /// <summary>
        /// Check if the current Operating System is Windows.
        /// </summary>
        /// <param name="logger">
        /// The logger.
        /// </param>
        /// <returns>
        /// True if running on Windows, false otherwise.
        /// </returns>
        public static bool IsWindows(ILogger logger)
        {
#if NETFRAMEWORK
            logger.LogTrace($"IsWindows: Using NetFramework : Environment.OSVersion.Platform == PlatformID.Win32NT = {Environment.OSVersion.Platform == PlatformID.Win32NT}");
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
            logger.LogTrace($"IsWindows: Using NetStandard : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) = {RuntimeInformation.IsOSPlatform(OSPlatform.Windows)}");
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
        }

        #region Windows Native Version APIs

        // Interop code sourced from the .NET Runtime as of version 5.0:
        // https://github.com/dotnet/runtime/blob/6578f257e3be2e2144a65769706e981961f0130c/src/libraries/Common/src/Interop/Windows/NtDll/Interop.RtlGetVersion.cs
        [DllImport("ntdll.dll", ExactSpelling = true)]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);

        private static unsafe int RtlGetVersionEx(out RTL_OSVERSIONINFOEX osvi)
        {
            osvi = default;
            osvi.DwOSVersionInfoSize = (uint)sizeof(RTL_OSVERSIONINFOEX);
            return RtlGetVersion(ref osvi);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct RTL_OSVERSIONINFOEX
        {
            internal uint DwOSVersionInfoSize;
            internal uint DwMajorVersion;
            internal uint DwMinorVersion;
            internal uint DwBuildNumber;
            internal uint DwPlatformId;
            internal fixed char SzCSDVersion[128];
        }
#endregion
    }
}
