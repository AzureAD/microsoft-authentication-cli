using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Microsoft.Authentication.MSALWrapper
{
    /// <summary>
    /// Provides helper methods for Linux-specific functionality in the MSAL wrapper.
    /// </summary>
    public static class LinuxHelper
    {
        /// <summary>
        /// Checks if the current platform is Linux.
        /// </summary>
        /// <returns>True if running on Linux, false otherwise.</returns>
        public static bool IsLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }

        /// <summary>
        /// Checks if the current Linux environment is headless (no display server).
        /// </summary>
        /// <returns>True if headless Linux environment, false otherwise.</returns>
        public static bool IsHeadlessLinux()
        {
            // Check if DISPLAY environment variable is not set or empty
            var display = Environment.GetEnvironmentVariable("DISPLAY");
            if (string.IsNullOrEmpty(display))
            {
                return true;
            }

            // Check if WAYLAND_DISPLAY is not set or empty
            var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
            if (string.IsNullOrEmpty(waylandDisplay))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets directory permissions to user only (700) on Unix systems.
        /// </summary>
        /// <param name="directoryPath">The directory path to set permissions for.</param>
        /// <param name="logger">logging directory permission information</param>
        [SupportedOSPlatform("linux")]
        public static void SetDirectoryPermissions(string directoryPath, ILogger logger)
        {
            if (!IsLinux())
            {
                return;
            }

            try
            {
                var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
                File.SetUnixFileMode(directoryPath, mode);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Failed to set directory permissions for '{directoryPath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Sets file permissions to user only (600) on Unix systems.
        /// </summary>
        /// <param name="filePath">The file path to set permissions for.</param>
        /// <param name="logger">logging file information permission</param>
        [SupportedOSPlatform("linux")]
        public static void SetFilePermissions(string filePath, ILogger logger)
        {
            if (!IsLinux())
            {
                return;
            }

            try
            {
                var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                File.SetUnixFileMode(filePath, mode);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Failed to set file permissions for '{filePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Tries to find a shell execute handler on Linux. 
        /// </summary>
        /// <returns>True if a handler is found, false otherwise.</returns>
        /// kept this functions in case we need to expand shell execute functionality in future
        private static bool TryGetLinuxShellExecuteHandler()
        {
            string[] handlers = { "xdg-open", "gnome-open", "kfmclient", "wslview" };
            foreach (var h in handlers)
            {
                if (IsExecutableOnPath(h))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsExecutableOnPath(string executableName)
        {
            return TryLocateExecutable(executableName, null, out _);
        }

        private static bool TryLocateExecutable(
            string program,
            ICollection<string> pathsToIgnore,
            out string path)
        {
            path = null;

            var pathValue = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathValue))
            {
                return false;
            }

            foreach (var basePath in pathValue.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(basePath))
                {
                    continue;
                }

                var candidatePath = Path.Combine(basePath, program);

                if (!File.Exists(candidatePath))
                {
                    continue;
                }

                if (pathsToIgnore != null &&
                    pathsToIgnore.Contains(candidatePath, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                path = candidatePath;
                return true;
            }

            return false;
        }
    }
}
