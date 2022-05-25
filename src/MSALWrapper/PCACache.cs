// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Extensions.Msal;
    using Microsoft.IdentityModel.Tokens;

    /// <summary>
    /// The PCA cache class.
    /// </summary>
    internal class PCACache
    {
        // OSX
        private const string MacOSAccountName = "MSALCache";
        private const string MacOSServiceName = "Microsoft.Developer.IdentityService";

        // Linux
        private const string LinuxKeyRingSchema = "com.microsoft.identity.tokencache";
        private const string LinuxKeyRingCollection = "default";
        private const string LinuxKeyRingLabel = "MSAL token cache";
        private static KeyValuePair<string, string> linuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", "1");
        private static KeyValuePair<string, string> linuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "Microsoft Develoepr Tools");

        private readonly ILogger logger;
        private readonly string osxKeyChainSuffix;

        private readonly string cacheDir;
        private readonly string cacheFileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="PCACache"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="osxKeyChainSuffix">The osx key chain suffix.</param>
        internal PCACache(ILogger logger, Guid tenantId, string osxKeyChainSuffix = null)
        {
            this.logger = logger;
            this.osxKeyChainSuffix = string.IsNullOrWhiteSpace(osxKeyChainSuffix) ? $"{tenantId}" : $"{osxKeyChainSuffix}.{tenantId}";
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            this.cacheDir = Path.Combine(appData, ".IdentityService");

            var azureAuthCacheFile = Environment.GetEnvironmentVariable(Constants.AZUREAUTH_CACHE_FILE);
            if (!azureAuthCacheFile.Intersect(Path.GetInvalidFileNameChars()).IsNullOrEmpty())
            {
                throw new ArgumentException($"Environment variable '{Constants.AZUREAUTH_CACHE_FILE}' Contains invalid path characters.");
            }

            this.cacheFileName = string.IsNullOrWhiteSpace(azureAuthCacheFile) ? $"msal_{tenantId}.cache" : azureAuthCacheFile;
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

            var osxKeychainItem = MacOSServiceName + (string.IsNullOrWhiteSpace(this.osxKeyChainSuffix) ? string.Empty : $".{this.osxKeyChainSuffix}");
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
            }
            catch (AggregateException ex) when (ex.InnerException.Message.Contains("Could not get access to the shared lock file"))
            {
                var exceptionMessage = ex.ToFormattedString();
                errors.Add(ex);

                this.logger.LogError("An unexpected error occured creating the cache.");
                throw new Exception(exceptionMessage);
            }
        }
    }
}
