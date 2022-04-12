// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.AuthFlows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Extensions.Msal;

    /// <summary>
    /// The PCA cache class.
    /// </summary>
    internal class PCACache : IPCACache
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

        private readonly IPublicClientApplication publicClientApplication;
        private readonly ILogger logger;
        private readonly string osxKeyChainSuffix;
        private readonly bool verifyPersistence;

        private readonly string cacheDir;
        private readonly string cacheFileName;

        /// <summary>
        /// Initializes a new instance of the <see cref="PCACache"/> class.
        /// </summary>
        /// <param name="publicClientApplication">The public client application.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="osxKeyChainSuffix">The osx key chain suffix.</param>
        /// <param name="verifyPersistence">The verify persistence.</param>
        internal PCACache(IPublicClientApplication publicClientApplication, ILogger logger, Guid tenantId, string osxKeyChainSuffix = null, bool verifyPersistence = false)
        {
            this.publicClientApplication = publicClientApplication;
            this.logger = logger;

            this.osxKeyChainSuffix = string.IsNullOrWhiteSpace(osxKeyChainSuffix) ? $"{tenantId}" : $"{osxKeyChainSuffix}.{tenantId}";
            this.verifyPersistence = verifyPersistence;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            this.cacheDir = Path.Combine(appData, ".IdentityService");
            this.cacheFileName = $"msal_{tenantId}.cache";
        }

        /// <summary>
        /// Sets up the token cache.
        /// </summary>
        /// <param name="errorsList">The errors list.</param>
        public void SetupTokenCache(List<Exception> errorsList)
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
                if (this.verifyPersistence)
                {
                    cacher.VerifyPersistence();
                }

                cacher.RegisterCache(this.publicClientApplication.UserTokenCache);
            }
            catch (MsalCachePersistenceException ex)
            {
                this.logger.LogWarning($"MSAL token cache verification failed.\n{ex.Message}\n");
                errorsList.Add(ex);
            }
            catch (AggregateException ex) when (ex.InnerException.Message.Contains("Could not get access to the shared lock file"))
            {
                var exceptionMessage = ex.ToFormattedString();
                errorsList.Add(ex);

                this.logger.LogError("An unexpected error occured creating the cache.");
                throw new Exception(exceptionMessage);
            }
        }
    }
}
