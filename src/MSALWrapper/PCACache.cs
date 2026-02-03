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
        private readonly string plainTextCacheFileName;

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
            this.plainTextCacheFileName = $"msal_{tenantId}_cache.json";
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
                if (LinuxHelper.IsLinux() && LinuxHelper.IsHeadlessLinux())
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
                var cacheFilePath = Path.Combine(cacheDir, this.plainTextCacheFileName);

                // Create directory if it doesn't exist
#pragma warning disable CA1416
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                    // Set directory permissions to user only (700)
                    LinuxHelper.SetDirectoryPermissions(cacheDir, logger);
                }

                // Create or ensure cache file exists with proper permissions
                if (!File.Exists(cacheFilePath))
                {
                    File.WriteAllText(cacheFilePath, "{}");
                    LinuxHelper.SetFilePermissions(cacheFilePath, logger);
                }
                else
                {
                    // Ensure existing file has proper permissions
                    LinuxHelper.SetFilePermissions(cacheFilePath, logger);
                }
#pragma warning restore CA1416

                var storageProperties = new StorageCreationPropertiesBuilder(this.plainTextCacheFileName, cacheDir)
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
