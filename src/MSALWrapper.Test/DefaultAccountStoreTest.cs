// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.IO;

    using FluentAssertions;

    using Microsoft.Authentication.TestHelper;
    using Microsoft.Extensions.Logging;

    using NLog.Targets;

    using NUnit.Framework;

    internal class DefaultAccountStoreTest
    {
        private static readonly Guid TestClientId = new Guid("5af6def2-05ec-4cab-b9aa-323d75b5df40");
        private const string TestTenantId = "8254f6f7-a09f-4752-8bd6-391adc3b912e";
        private const string TestUsername = "testuser@contoso.com";

        private ILogger logger;
        private MemoryTarget logTarget;
        private string tempDir;

        [SetUp]
        public void SetUp()
        {
            (this.logger, this.logTarget) = MemoryLogger.Create();

            // Use a temp directory to avoid polluting the real ~/.azureauth
            this.tempDir = Path.Combine(Path.GetTempPath(), "azureauth_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(this.tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(this.tempDir))
            {
                Directory.Delete(this.tempDir, recursive: true);
            }
        }

        [Test]
        public void SaveAndGet_RoundTrip()
        {
            var store = new DefaultAccountStore(this.logger);
            store.SaveDefaultAccount(TestUsername, TestClientId, TestTenantId);

            var result = store.GetDefaultAccount(TestClientId, TestTenantId);

            result.Should().Be(TestUsername);
        }

        [Test]
        public void GetDefaultAccount_NoFile_ReturnsNull()
        {
            var store = new DefaultAccountStore(this.logger);

            // Use a unique client/tenant that will never have a file
            var result = store.GetDefaultAccount(Guid.NewGuid(), "nonexistent-tenant");

            result.Should().BeNull();
        }

        [Test]
        public void SaveDefaultAccount_NullUsername_DoesNothing()
        {
            var store = new DefaultAccountStore(this.logger);
            var uniqueClientId = Guid.NewGuid();
            var uniqueTenantId = Guid.NewGuid().ToString();

            store.SaveDefaultAccount(null, uniqueClientId, uniqueTenantId);

            var result = store.GetDefaultAccount(uniqueClientId, uniqueTenantId);
            result.Should().BeNull();
        }

        [Test]
        public void SaveDefaultAccount_EmptyUsername_DoesNothing()
        {
            var store = new DefaultAccountStore(this.logger);
            var uniqueClientId = Guid.NewGuid();
            var uniqueTenantId = Guid.NewGuid().ToString();

            store.SaveDefaultAccount(string.Empty, uniqueClientId, uniqueTenantId);

            var result = store.GetDefaultAccount(uniqueClientId, uniqueTenantId);
            result.Should().BeNull();
        }

        [Test]
        public void ClearDefaultAccount_RemovesFile()
        {
            var store = new DefaultAccountStore(this.logger);
            store.SaveDefaultAccount(TestUsername, TestClientId, TestTenantId);

            store.ClearDefaultAccount(TestClientId, TestTenantId);

            var result = store.GetDefaultAccount(TestClientId, TestTenantId);
            result.Should().BeNull();
        }

        [Test]
        public void ClearDefaultAccount_NoFile_DoesNotThrow()
        {
            var store = new DefaultAccountStore(this.logger);

            // Should not throw when clearing a non-existent account
            Action act = () => store.ClearDefaultAccount(Guid.NewGuid(), "nonexistent-tenant");
            act.Should().NotThrow();
        }

        [Test]
        public void SaveDefaultAccount_OverwritesExisting()
        {
            var store = new DefaultAccountStore(this.logger);
            store.SaveDefaultAccount("old@contoso.com", TestClientId, TestTenantId);
            store.SaveDefaultAccount("new@contoso.com", TestClientId, TestTenantId);

            var result = store.GetDefaultAccount(TestClientId, TestTenantId);
            result.Should().Be("new@contoso.com");
        }

        [Test]
        public void GetDefaultAccount_CorruptFile_ReturnsNull()
        {
            // Write a corrupt file to the expected path
            var store = new DefaultAccountStore(this.logger);

            // First save a valid account to create the directory structure
            store.SaveDefaultAccount(TestUsername, TestClientId, TestTenantId);

            // Then overwrite the file with garbage
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var filePath = Path.Combine(homeDir, ".azureauth", $"default_account_{TestTenantId}_{TestClientId}.json");
            File.WriteAllText(filePath, "not valid json!!");

            var result = store.GetDefaultAccount(TestClientId, TestTenantId);
            result.Should().BeNull();

            // Clean up
            File.Delete(filePath);
        }

        [Test]
        public void DifferentClientTenant_AreIndependent()
        {
            var store = new DefaultAccountStore(this.logger);
            var clientId2 = Guid.NewGuid();
            var tenantId2 = Guid.NewGuid().ToString();

            store.SaveDefaultAccount("user1@contoso.com", TestClientId, TestTenantId);
            store.SaveDefaultAccount("user2@contoso.com", clientId2, tenantId2);

            store.GetDefaultAccount(TestClientId, TestTenantId).Should().Be("user1@contoso.com");
            store.GetDefaultAccount(clientId2, tenantId2).Should().Be("user2@contoso.com");

            // Clean up
            store.ClearDefaultAccount(TestClientId, TestTenantId);
            store.ClearDefaultAccount(clientId2, tenantId2);
        }
    }
}
