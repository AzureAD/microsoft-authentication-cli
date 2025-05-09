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

    using Microsoft.Authentication.AzureAuth;
    using Microsoft.Authentication.AzureAuth.Commands;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;

    using Moq;

    using NLog.Extensions.Logging;
    using NLog.Targets;

    using NUnit.Framework;

    internal class CommandAadTest
    {
        private const string InvalidTOML = @"[invalid TOML"; // Note the missing closing square bracket here.
        private const string CompleteAliasTOML = @"
[alias.contoso]
resource = ""67eeda51-3891-4101-a0e3-bf0c64047157""
client = ""73e5793e-8f71-4da2-9f71-575cb3019b37""
domain = ""contoso.com""
tenant = ""a3be859b-7f9a-4955-98ed-f3602dbd954c""
scopes = [ ""67eeda51-3891-4101-a0e3-bf0c64047157/.default"", ]
prompt_hint = ""sample prompt hint.""
";

        private const string PartialAliasTOML = @"
[alias.fabrikam]
resource = ""ab7e45b7-ea4c-458c-97bd-670ccb543376""
domain = ""fabrikam.com""
";

        private const string InvalidAliasTOML = @"
[alias.litware]
domain = ""litware.com""
invalid_key = ""this is not a valid alias key""
";

        private const string RootDriveWindows = @"Z:\";
        private const string RootDriveUnix = "/";
        private const string PromptHintPrefix = "AzureAuth";

        private CommandExecuteEventData eventData;
        private IFileSystem fileSystem;
        private IServiceProvider serviceProvider;
        private MemoryTarget logTarget;
        private Mock<IEnv> envMock;
        private Mock<ITelemetryService> telemetryServiceMock;
        private Mock<IAuthFlow> authFlowMock;

        /// <summary>
        /// The setup.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            this.eventData = new CommandExecuteEventData();
            this.fileSystem = new MockFileSystem();
            this.fileSystem.Directory.CreateDirectory(RootDrive());

            // Setup in memory logging target with NLog - allows making assertions against what has been logged.
            var loggingConfig = new NLog.Config.LoggingConfiguration();
            this.logTarget = new MemoryTarget("memory_target");
            this.logTarget.Layout = "${message}"; // Define a simple layout so we don't get timestamps in messages.
            loggingConfig.AddTarget(this.logTarget);
            loggingConfig.AddRuleForAllLevels(this.logTarget);

            this.envMock = new Mock<IEnv>(MockBehavior.Strict);
            this.telemetryServiceMock = new Mock<ITelemetryService>(MockBehavior.Strict);
            this.authFlowMock = new Mock<IAuthFlow>(MockBehavior.Strict);

            // Setup Dependency Injection container to provide logger and out class under test (the "subject").
            this.serviceProvider = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog(loggingConfig);
                })
                .AddSingleton(this.eventData)
                .AddSingleton(this.fileSystem)
                .AddSingleton(this.envMock.Object)
                .AddSingleton(this.telemetryServiceMock.Object)
                .AddSingleton(this.authFlowMock.Object)
                .AddTransient<CommandAad>()
                .BuildServiceProvider();
        }

        /// <summary>
        /// The test to evaluate options provided alias missing config file.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsProvidedAliasMissingConfigFile()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.AliasName = "contoso";
            this.envMock.Setup(e => e.Get("AZUREAUTH_CONFIG")).Returns((string)null);

            subject.EvaluateOptions().Should().BeFalse();
            this.logTarget.Logs.Should().Contain("The --alias field was given, but no --config was specified.");
        }

        /// <summary>
        /// The test for evaluating options provided alias not in empty config file.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsProvidedAliasNotInEmptyConfigFile()
        {
            string configFile = RootPath("empty.toml");
            this.fileSystem.File.Create(configFile);

            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.AliasName = "notfound";
            subject.ConfigFilePath = configFile;

            subject.EvaluateOptions().Should().BeFalse();
            this.logTarget.Logs.Should().Contain($"Alias 'notfound' was not found in {configFile}");
        }

        /// <summary>
        /// The test to evaluate options provided alias not in populated config file.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsProvidedAliasNotInPopulatedConfigFile()
        {
            string configFile = RootPath("partial.toml");
            this.fileSystem.File.WriteAllText(configFile, PartialAliasTOML);

            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.AliasName = "notfound";
            subject.ConfigFilePath = configFile;

            subject.EvaluateOptions().Should().BeFalse();
            this.logTarget.Logs.Should().Contain($"Alias 'notfound' was not found in {configFile}");
        }

        /// <summary>
        /// The test to evaluate options provided alias without command line options.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsProvidedAliasWithoutCommandLineOptions()
        {
            string configFile = RootPath("complete.toml");
            this.fileSystem.File.WriteAllText(configFile, CompleteAliasTOML);
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);
            Alias expected = new Alias
            {
                Resource = "67eeda51-3891-4101-a0e3-bf0c64047157",
                Client = "73e5793e-8f71-4da2-9f71-575cb3019b37",
                Domain = "contoso.com",
                Tenant = "a3be859b-7f9a-4955-98ed-f3602dbd954c",
                Scopes = new List<string> { "67eeda51-3891-4101-a0e3-bf0c64047157/.default" },
                PromptHint = "sample prompt hint.",
            };

            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.AliasName = "contoso";
            subject.ConfigFilePath = configFile;

            subject.EvaluateOptions().Should().BeTrue();
            subject.TokenFetcherOptions.Should().BeEquivalentTo(expected);
        }

        /// <summary>
        /// The test to evaluate options provided alias with command line options.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsProvidedAliasWithCommandLineOptions()
        {
            string configFile = RootPath("complete.toml");
            string clientOverride = "3933d919-5ba4-4eb7-b4b1-19d33e8b82c0";
            this.fileSystem.File.WriteAllText(configFile, CompleteAliasTOML);
            Alias expected = new Alias
            {
                Resource = "67eeda51-3891-4101-a0e3-bf0c64047157",
                Client = clientOverride,
                Domain = "contoso.com",
                Tenant = "a3be859b-7f9a-4955-98ed-f3602dbd954c",
                Scopes = new List<string> { "67eeda51-3891-4101-a0e3-bf0c64047157/.default" },
                PromptHint = "sample prompt hint.",
            };

            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.AliasName = "contoso";
            subject.ConfigFilePath = configFile;

            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);

            // Specify a client override on the command line.
            subject.Client = clientOverride;

            subject.EvaluateOptions().Should().BeTrue();
            subject.TokenFetcherOptions.Should().BeEquivalentTo(expected);
        }

        /// <summary>
        /// Assert evaluate options provided alias with env var configured config file.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsProvidedAliasWithEnvVarConfig()
        {
            string configFile = RootPath("complete.toml");
            string clientOverride = "3933d919-5ba4-4eb7-b4b1-19d33e8b82c0";
            this.fileSystem.File.WriteAllText(configFile, CompleteAliasTOML);
            Alias expected = new Alias
            {
                Resource = "67eeda51-3891-4101-a0e3-bf0c64047157",
                Client = clientOverride,
                Domain = "contoso.com",
                Tenant = "a3be859b-7f9a-4955-98ed-f3602dbd954c",
                Scopes = new List<string> { "67eeda51-3891-4101-a0e3-bf0c64047157/.default" },
                PromptHint = "sample prompt hint.",
            };

            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.AliasName = "contoso";
            subject.ConfigFilePath = null;

            // Specify config via env var
            this.envMock.Setup(e => e.Get("AZUREAUTH_CONFIG")).Returns(configFile);
            this.envMock.Setup(env => env.Get(It.Is<string>(key => key != "AZUREAUTH_CONFIG"))).Returns<string>(key => null);

            // Specify a client override on the command line.
            subject.Client = clientOverride;

            subject.EvaluateOptions().Should().BeTrue();
            subject.TokenFetcherOptions.Should().BeEquivalentTo(expected);
            this.envMock.VerifyAll();
        }

        /// <summary>
        /// The test to evaluate options provided invalid config.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsProvidedInvalidConfig()
        {
            string configFile = RootPath("invalid.toml");
            this.fileSystem.File.WriteAllText(configFile, InvalidTOML);

            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.AliasName = "contoso";
            subject.ConfigFilePath = configFile;

            subject.EvaluateOptions().Should().BeFalse();
            this.logTarget.Logs.Should().ContainMatch($"Error parsing TOML in config file at '{configFile}':*");
        }

        /// <summary>
        ///  The test to evaluate options when config file does not exist.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsConfigFileDoesNotExist()
        {
            string configFile = RootPath("does_not_exists_config.toml");

            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.AliasName = "contoso";
            subject.ConfigFilePath = null;

            // Specify config via env var
            this.envMock.Setup(e => e.Get("AZUREAUTH_CONFIG")).Returns(configFile);

            subject.EvaluateOptions().Should().BeFalse();
            this.logTarget.Logs.Should().ContainMatch($"The file '{configFile}' does not exist.*");
            this.envMock.VerifyAll();
        }

        /// <summary>
        /// The test to evaluate options provided invalid alias.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsProvidedInvalidAlias()
        {
            string configFile = RootPath("invalid.toml");
            this.fileSystem.File.WriteAllText(configFile, InvalidAliasTOML);

            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.AliasName = "litware";
            subject.ConfigFilePath = configFile;

            subject.EvaluateOptions().Should().BeFalse();
            this.logTarget.Logs.Should().ContainMatch($"Error parsing TOML in config file at '{configFile}':*");
        }

        /// <summary>
        /// The test to evaluate options without alias missing resource.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsWithoutAliasMissingResource()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = null;
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);

            subject.EvaluateOptions().Should().BeFalse();
            this.logTarget.Logs.Should().Contain("The --resource field or the --scope field is required.");
        }

        /// <summary>
        /// The test to evaluate options without alias missing client.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsWithoutAliasMissingClient()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = null;
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);

            subject.EvaluateOptions().Should().BeFalse();
            this.logTarget.Logs.Should().Contain("The --client field is required.");
        }

        /// <summary>
        /// The test to evaluate options without alias missing tenant.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsWithoutAliasMissingTenant()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = null;
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);

            subject.EvaluateOptions().Should().BeFalse();
            this.logTarget.Logs.Should().Contain("The --tenant field is required.");
        }

        /// <summary>
        /// The test to evaluate options without alias missing required options.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsWithoutAliasMissingRequiredOptions()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = null;
            subject.Client = null;
            subject.Tenant = null;
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);

            subject.EvaluateOptions().Should().BeFalse();
            this.logTarget.Logs.Should().Contain(new[]
            {
                "The --resource field or the --scope field is required.",
                "The --client field is required.",
                "The --tenant field is required.",
            });
        }

        /// <summary>
        /// The test to evaluate options without resource but with scopes.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsWithOverridedScopes()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = null;
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";
            subject.Scopes = new string[] { "f0e8d801-3a50-48fd-b2da-6476d6e832a2/.default" };
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);

            subject.EvaluateOptions().Should().BeTrue();
        }

        /// <summary>
        /// The test to evaluate options with normal parameters.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsWithNormalParameters()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);

            subject.EvaluateOptions().Should().BeTrue();
        }

        /// <summary>
        /// The test to evaluate options with both resource and scopes.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsWithResourceAndScopes()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";
            subject.Scopes = new string[] { "f0e8d801-3a50-48fd-b2da-6476d6e832a2/.default" };
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);

            subject.EvaluateOptions().Should().BeTrue();
            this.logTarget.Logs.Should().Contain(new[]
            {
                "The --scope option was provided with the --resource option. Only --scope will be used.",
            });
        }

        /// <summary>
        /// The test to evaluate options without alias valid command line options.
        /// </summary>
        [Test]
        public void TestEvaluateOptionsWithoutAliasValidCommandLineOptions()
        {
            Alias expected = new Alias
            {
                Resource = null,
                Client = "e19f71ed-3b14-448d-9346-9eff9753646b",
                Domain = null,
                Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9",
                Scopes = new List<string>() { "f0e8d801-3a50-48fd-b2da-6476d6e832a2/.default" },
            };

            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);

            subject.EvaluateOptions().Should().BeTrue();
            subject.TokenFetcherOptions.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void TestEvaluateOptionsWithAuthModeFromCommandLineOptions()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";
            subject.AuthModes = new List<AuthMode>() { AuthMode.DeviceCode };

            this.envMock.Setup(env => env.Get(EnvVars.AuthMode)).Returns("Web,DeviceCode");
            subject.EvaluateOptions().Should().BeTrue();
            subject.AuthModes.Should().Contain(new[] { AuthMode.DeviceCode });
        }

        [Test]
        public void TestEvaluateOptionsWithAuthModeFromEnvVar()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";

            this.envMock.Setup(env => env.Get("AZUREAUTH_MODE")).Returns("Web,DeviceCode");
            subject.EvaluateOptions().Should().BeTrue();
            subject.AuthModes.Should().Contain(new[] { AuthMode.Web, AuthMode.DeviceCode });
        }

        [Test]
        public void TestEvaluateOptionsWithNoAuthModeInEnvVarOrOptions()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";

            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);
            subject.EvaluateOptions().Should().BeTrue();
            subject.AuthModes.Should().Contain(new[] { AuthMode.Default });
        }

        [Test]
        public void TestEvaluateOptionsWithAuthModeFromInvalidEnvVars()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";

            this.envMock.Setup(env => env.Get(EnvVars.AuthMode)).Returns("Invalid");
            subject.EvaluateOptions().Should().BeFalse();
            this.logTarget.Logs.Should().ContainMatch($"Invalid value specified for environment variable {EnvVars.AuthMode}*");
        }

        [Test]
        public void TestEvaluateOptionsWithAuthModeFromEmptyEnvVars()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";

            this.envMock.Setup(env => env.Get(EnvVars.AuthMode)).Returns("");
            subject.EvaluateOptions().Should().BeTrue();
            subject.AuthModes.Should().Contain(new[] { AuthMode.Default });
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
