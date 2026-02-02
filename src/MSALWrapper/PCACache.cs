// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.Authentication.MSALWrapper.Test")]
namespace Microsoft.Authentication.MSALWrapper
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Extensions.Msal;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// The PCA cache class.
    /// </summary>
    internal class PCACache
    {
        // OSX
        private const string MacOSAccountName = "MSALCache";
        private const string MacOSServiceName = "Microsoft.Developer.IdentityService";
        private const string OSXKeyChainCategory = "azureauth";

        // Linux
        private const string LinuxKeyRingSchema = "com.microsoft.identity.tokencache";
        private const string LinuxKeyRingCollection = "default";
        private const string LinuxKeyRingLabel = "MSAL token cache";
        private static KeyValuePair<string, string> linuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", "1");
        private static KeyValuePair<string, string> linuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "Microsoft Develoepr Tools");

        // Plain text cache fallback for headless Linux
        private const string PlainTextCacheDir = ".azureauth";
        private const string PlainTextCacheFileName = "msal_cache.json";

        private readonly ILogger logger;
        private readonly string osxKeyChainSuffix;

        private readonly string cacheDir;
        private readonly string cacheFileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="PCACache"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="tenantId">The tenant id.</param>
        internal PCACache(ILogger logger, Guid tenantId)
        {
            this.logger = logger;
            this.osxKeyChainSuffix = $"{OSXKeyChainCategory}.{tenantId}";

            this.cacheFileName = $"msal_{tenantId}.cache";
            this.cacheDir = this.GetCacheServiceFolder();
        }

        /// <summary>
        /// Sets up the token cache.
        /// </summary>
        /// <param name="userTokenCache">An <see cref="ITokenCache"/> to use.</param>
        /// <param name="errors">The errors list to append error encountered to.</param>
        public void SetupTokenCache(ITokenCache userTokenCache, IList<Exception> errors)
        {
            var cacheDisabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Constants.OEAUTH_MSAL_DISABLE_CACHE));
            if (cacheDisabled)
            {
                return;
            }

            var osxKeychainItem = $"{MacOSServiceName}.{this.osxKeyChainSuffix}";

            var storageProperties = new StorageCreationPropertiesBuilder(this.cacheFileName, this.cacheDir)
            .WithLinuxKeyring(LinuxKeyRingSchema, LinuxKeyRingCollection, LinuxKeyRingLabel, linuxKeyRingAttr1, linuxKeyRingAttr2)
            .WithMacKeyChain(osxKeychainItem, MacOSAccountName)
            .Build();

            try
            {
                MsalCacheHelper cacher = MsalCacheHelper.CreateAsync(storageProperties).Result;
                cacher.VerifyPersistence();
                cacher.RegisterCache(userTokenCache);
            }
            catch (MsalCachePersistenceException ex)
            {
                this.logger.LogWarning($"MSAL token cache verification failed.\n{ex.Message}\n");
                errors.Add(ex);

                // On Linux, if keyring fails and we're in a headless environment, try plain text fallback
                if (IsLinux() && IsHeadlessLinux())
                {
                    this.logger.LogInformation("Attempting plain text cache fallback for headless Linux environment.");
                    this.SetupPlainTextCache(userTokenCache, errors);
                }
            }
            catch (AggregateException ex) when (ex.InnerException.Message.Contains("Could not get access to the shared lock file"))
            {
                var exceptionMessage = ex.ToFormattedString();
                errors.Add(ex);

                this.logger.LogError("An unexpected error occured creating the cache.");
                throw new Exception(exceptionMessage);
            }
        }

        /// <summary>
        /// Sets up a plain text cache fallback for headless Linux environments.
        /// </summary>
        /// <param name="userTokenCache">An <see cref="ITokenCache"/> to use.</param>
        /// <param name="errors">The errors list to append error encountered to.</param>
        private void SetupPlainTextCache(ITokenCache userTokenCache, IList<Exception> errors)
        {
            try
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var cacheDir = Path.Combine(homeDir, PlainTextCacheDir);
                var cacheFilePath = Path.Combine(cacheDir, PlainTextCacheFileName);

                // Create directory if it doesn't exist
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                    // Set directory permissions to user only (700)
                    SetDirectoryPermissions(cacheDir);
                }

                // Create or ensure cache file exists with proper permissions
                if (!File.Exists(cacheFilePath))
                {
                    File.WriteAllText(cacheFilePath, "{}");
                    SetFilePermissions(cacheFilePath);
                }
                else
                {
                    // Ensure existing file has proper permissions
                    SetFilePermissions(cacheFilePath);
                }

                var storageProperties = new StorageCreationPropertiesBuilder(PlainTextCacheFileName, cacheDir)
                    .WithUnprotectedFile()
                    .Build();

                MsalCacheHelper cacher = MsalCacheHelper.CreateAsync(storageProperties).Result;
                cacher.RegisterCache(userTokenCache);

                this.logger.LogInformation($"Plain text cache fallback configured at: {cacheFilePath}");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"Plain text cache fallback failed: {ex.Message}");
                errors.Add(ex);
            }
        }

        /// <summary>
        /// Checks if the current platform is Linux.
        /// </summary>
        /// <returns>True if running on Linux, false otherwise.</returns>
        private static bool IsLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }

        /// <summary>
        /// Checks if the current Linux environment is headless (no display server).
        /// </summary>
        /// <returns>True if headless Linux environment, false otherwise.</returns>
        private static bool IsHeadlessLinux()
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
        private void SetDirectoryPermissions(string directoryPath)
        {
            if (IsLinux())
            {
                try
                {
                    // Set directory permissions to 700 (user read/write/execute, no permissions for group/others)
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"700 \"{directoryPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning($"Failed to set directory permissions: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Sets file permissions to user only (600) on Unix systems.
        /// </summary>
        /// <param name="filePath">The file path to set permissions for.</param>
        private void SetFilePermissions(string filePath)
        {
            if (IsLinux())
            {
                try
                {
                    // Set file permissions to 600 (user read/write, no permissions for group/others)
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"600 \"{filePath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning($"Failed to set file permissions: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets the absolute path of the cache folder. Only available on Windows.
        /// </summary>
        /// <returns>The absolute path of the cache folder.</returns>
        private string GetCacheServiceFolder()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string absolutePath = Path.Combine(appData, ".IdentityService");
            return absolutePath;
        }
    }
}
