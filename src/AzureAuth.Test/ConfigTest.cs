// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.IO.Abstractions.TestingHelpers;
    using System.Runtime.InteropServices;
    using FluentAssertions;
    using NUnit.Framework;
    using Tomlyn;

    /// <summary>
    /// The config test.
    /// </summary>
    public class ConfigTest
    {
        private const string InvalidTOML = @"[invalid TOML"; // Note the missing closing square bracket here.
        private const string CompleteAliasTOML = @"
[alias.contoso]
resource = ""67eeda51-3891-4101-a0e3-bf0c64047157""
client = ""73e5793e-8f71-4da2-9f71-575cb3019b37""
domain = ""contoso.com""
tenant = ""a3be859b-7f9a-4955-98ed-f3602dbd954c""
scopes = [ "".default"", ]
";

        private const string PartialAliasTOML = @"
[alias.fabrikam]
resource = ""ab7e45b7-ea4c-458c-97bd-670ccb543376""
domain = ""fabrikam.com""
";

        private const string MultipleAliasesTOML = @"
[alias]
# Contoso Ltd.
contoso = { resource = ""67eeda51-3891-4101-a0e3-bf0c64047157"", domain = ""contoso.com"" }
# Fabrikam, Inc.
fabrikam = { resource = ""ab7e45b7-ea4c-458c-97bd-670ccb543376"", domain = ""fabrikam.com"" }
";

        private const string InvalidAliasTOML = @"
[alias.litware]
domain = ""litware.com""
invalid_key = ""this is not a valid alias key""
";

        private const string RootDriveWindows = @"Z:\";
        private const string RootDriveUnix = "/";

        private IFileSystem fileSystem;

        /// <summary>
        /// The setup.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            this.fileSystem = new MockFileSystem();
            this.fileSystem.Directory.CreateDirectory(RootDrive());
        }

        /// <summary>
        /// The test from file disallows relative paths.
        /// </summary>
        /// <param name="configFile">
        /// The config file.
        /// </param>
        [TestCase(null)]
        [TestCase("./unix.toml")]
        [TestCase(@".\windows.toml")]
        [TestCase(@"..\up-one.toml")]
        [TestCase(@"windows\style\path.toml")]

        // All of these are invalid, on both Mac and Windows.
        public void TestFromFileDisallowsRelativePaths(string configFile)
        {
            Action subject = () => Config.FromFile(configFile, this.fileSystem);
            subject.Should().Throw<ArgumentException>();
        }

        /// <summary>
        /// The test from file accepts full paths_ mac.
        /// </summary>
        /// <param name="configFile">
        /// The config file.
        /// </param>
        [Platform("MacOsX,Unix")] // Only valid on Mac
        [TestCase("/unix/style/path.toml")]
        public void TestFromFileAcceptsFullPaths_Mac(string configFile)
        {
            Action subject = () => Config.FromFile(configFile, this.fileSystem);
            subject.Should().NotThrow<ArgumentException>();
        }

        /// <summary>
        /// The test from file accepts full paths_ win.
        /// </summary>
        /// <param name="configFile">
        /// The config file.
        /// </param>
        [Platform("Win")] // Only valid on Windows
        [TestCase(@"C:\windows\style\path\with\drive.toml")]
        public void TestFromFileAcceptsFullPaths_Win(string configFile)
        {
            Action subject = () => Config.FromFile(configFile, this.fileSystem);
            subject.Should().NotThrow<ArgumentException>();
        }

        /// <summary>
        /// The test from file requires valid toml.
        /// </summary>
        [Test]
        public void TestFromFileRequiresValidTOML()
        {
            string configFile = RootPath(@"invalid.toml");
            this.fileSystem.File.WriteAllText(configFile, InvalidTOML);

            Action subject = () => Config.FromFile(configFile, this.fileSystem);
            subject.Should().Throw<TomlException>();
        }

        /// <summary>
        /// The test from file requires aliases.
        /// </summary>
        [Test]
        public void TestFromFileRequiresAliases()
        {
            string configFile = RootPath(@"complete.toml");
            this.fileSystem.File.WriteAllText(configFile, CompleteAliasTOML);

            Config expected = new Config { Alias = new Dictionary<string, Alias>() };
            expected.Alias.Add("contoso", new Alias
            {
                Resource = "67eeda51-3891-4101-a0e3-bf0c64047157",
                Client = "73e5793e-8f71-4da2-9f71-575cb3019b37",
                Domain = "contoso.com",
                Tenant = "a3be859b-7f9a-4955-98ed-f3602dbd954c",
                Scopes = new List<string> { ".default" },
            });

            Config config = Config.FromFile(configFile, this.fileSystem);
            config.Should().BeEquivalentTo(expected);
        }

        /// <summary>
        /// The test from file allows partial aliases.
        /// </summary>
        [Test]
        public void TestFromFileAllowsPartialAliases()
        {
            string configFile = RootPath(@"partial.toml");
            this.fileSystem.File.WriteAllText(configFile, PartialAliasTOML);

            Config expected = new Config { Alias = new Dictionary<string, Alias>() };
            expected.Alias.Add("fabrikam", new Alias
            {
                Resource = "ab7e45b7-ea4c-458c-97bd-670ccb543376",
                Client = null,
                Domain = "fabrikam.com",
                Tenant = null,
                Scopes = null,
            });

            Config config = Config.FromFile(configFile, this.fileSystem);
            config.Should().BeEquivalentTo(expected);
        }

        /// <summary>
        /// The test from file allows multiple aliases.
        /// </summary>
        [Test]
        public void TestFromFileAllowsMultipleAliases()
        {
            string configFile = RootPath(@"multiple.toml");
            this.fileSystem.File.WriteAllText(configFile, MultipleAliasesTOML);

            Config expected = new Config { Alias = new Dictionary<string, Alias>() };
            expected.Alias.Add("contoso", new Alias
            {
                Resource = "67eeda51-3891-4101-a0e3-bf0c64047157",
                Domain = "contoso.com",
            });
            expected.Alias.Add("fabrikam", new Alias
            {
                Resource = "ab7e45b7-ea4c-458c-97bd-670ccb543376",
                Domain = "fabrikam.com",
            });

            Config config = Config.FromFile(configFile, this.fileSystem);
            config.Should().BeEquivalentTo(expected);
        }

        /// <summary>
        /// The test from file disallows invalid aliases.
        /// </summary>
        [Test]
        public void TestFromFileDisallowsInvalidAliases()
        {
            string configFile = RootPath(@"invalid-alias.toml");
            this.fileSystem.File.WriteAllText(configFile, InvalidAliasTOML);

            Action subject = () => Config.FromFile(configFile, this.fileSystem);
            subject.Should().Throw<TomlException>();
        }

        /// <summary>
        /// The root path.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string RootPath(string filename)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"{RootDriveWindows}{filename}";
            }
            else
            {
                return $"{RootDriveUnix}{filename}";
            }
        }

        /// <summary>
        /// The root drive.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string RootDrive() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? RootDriveWindows : RootDriveUnix;
    }
}
