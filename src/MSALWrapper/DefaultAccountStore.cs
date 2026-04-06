// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.IO;
    using System.Text.Json;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Persists the last successfully authenticated account username so that
    /// subsequent runs can look up the specific IAccount from the MSAL cache
    /// instead of relying on the OperatingSystemAccount sentinel (which is
    /// not supported on macOS).
    /// </summary>
    public class DefaultAccountStore
    {
        private const string AccountDir = ".azureauth";
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAccountStore"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public DefaultAccountStore(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Saves the default account username for a given client and tenant.
        /// </summary>
        /// <param name="username">The account username (email) to persist.</param>
        /// <param name="clientId">The client ID.</param>
        /// <param name="tenantId">The tenant ID.</param>
        public void SaveDefaultAccount(string username, Guid clientId, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            try
            {
                var filePath = this.GetFilePath(clientId, tenantId);
                var dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(new DefaultAccountData { Username = username });
                File.WriteAllText(filePath, json);
                this.logger.LogDebug($"Persisted default account for tenant {tenantId}");
            }
            catch (Exception ex)
            {
                this.logger.LogDebug($"Failed to persist default account: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the persisted default account username for a given client and tenant.
        /// </summary>
        /// <param name="clientId">The client ID.</param>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>The persisted username, or null if not found or on error.</returns>
        public string GetDefaultAccount(Guid clientId, string tenantId)
        {
            try
            {
                var filePath = this.GetFilePath(clientId, tenantId);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<DefaultAccountData>(json);
                return data?.Username;
            }
            catch (Exception ex)
            {
                this.logger.LogDebug($"Failed to read default account: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clears the persisted default account for a given client and tenant.
        /// </summary>
        /// <param name="clientId">The client ID.</param>
        /// <param name="tenantId">The tenant ID.</param>
        public void ClearDefaultAccount(Guid clientId, string tenantId)
        {
            try
            {
                var filePath = this.GetFilePath(clientId, tenantId);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    this.logger.LogDebug($"Cleared default account for tenant {tenantId}");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogDebug($"Failed to clear default account: {ex.Message}");
            }
        }

        private string GetFilePath(Guid clientId, string tenantId)
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, AccountDir, $"default_account_{tenantId}_{clientId}.json");
        }

        private class DefaultAccountData
        {
            public string Username { get; set; }
        }
    }
}
