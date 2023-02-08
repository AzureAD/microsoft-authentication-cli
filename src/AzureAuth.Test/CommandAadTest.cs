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

    /// <summary>
    /// The command main test.
    /// </summary>
    internal class CommandMainTest
    {
        private const string FakeToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsInJoIjoieHh4IiwieDV0IjoieHh4Iiwia2lkIjoieHh4In0.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYXVkIjoiMTExMTExMTEtMTExMS0xMTExLTExMTEtMTExMTExMTExMTExIiwiaWF0IjoxNjE3NjY0Mjc2LCJuYmYiOjE2MTc2NjQyNzYsImV4cCI6MTYxNzY2ODE3NiwiYWNyIjoiMSIsImFpbyI6IllTQjBiM1JoYkd4NUlHWmhhMlVnYTJWNUlDTWtKVjQ9Iiwic2NwIjoidXNlcl9pbXBlcnNvbmF0aW9uIiwidW5pcXVlX25hbWUiOiJreXJhZGVyQG1pY3Jvc29mdC5jb20iLCJ1cG4iOiJreXJhZGVyQG1pY3Jvc29mdC5jb20iLCJ2ZXIiOiIxLjAifQ.bNc3QlL4zIClzFqH68A4hxsR7K-jabQvzB2EodgujQqc0RND_VLVkk2h3iDy8so3azN-964c2z5AiBGY6PVtWKYB-h0Z_VnzbebhDjzPLspEsANyQxaDX_ugOrf7BerQOtILWT5Vqs-A3745Bh0eTDFZpobmeENpANNhRE-yKwScjU8BDY9RimdrA2Z00V0lSliUQwnovWmtfdlbEpWObSFQAK7wCcNnUesV-jNZAUMrDkmTItPA9Z1Ks3NUbqdqMP3D6n99sy8DxQeFmbNQGYocYqI7QH24oNXODq0XB-2zpvCqy4T2jiBLgN_XEaZ5zTzEOzztpgMIWH1AUvEIyw";
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
                Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2",
                Client = "e19f71ed-3b14-448d-9346-9eff9753646b",
                Domain = null,
                Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9",
                Scopes = null,
            };

            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);

            subject.EvaluateOptions().Should().BeTrue();
            subject.TokenFetcherOptions.Should().BeEquivalentTo(expected);
        }

        /// <summary>
        /// The test to ensure the prompt hint text has a valid prefix with user's option.
        /// </summary>
        [Test]
        public void TestPromptHintPrefix()
        {
            string promptHintOption = "Test Prompt Hint";

            CommandAad.PrefixedPromptHint(promptHintOption)
                .Should().BeEquivalentTo($"{PromptHintPrefix}: {promptHintOption}");
        }

        /// <summary>
        /// The test to ensure the prompt hint text has a valid prefix without user's option.
        /// </summary>
        [Test]
        public void TestPromptHintPrefixWithoutOption()
        {
            CommandAad.PrefixedPromptHint(null)
                            .Should().BeEquivalentTo(PromptHintPrefix);
        }

        /// <summary>
        /// Test to generate event data from a null authflow result.
        /// </summary>
        [Test]
        public void TestGenerateEvent_FromNullAuthResult()
        {
            AuthFlowResult authFlowResult = null;
            var subject = this.serviceProvider.GetService<CommandAad>();

            // Act
            var eventData = subject.AuthFlowEventData(authFlowResult);

            // Assert
            eventData.Should().BeNull();
        }

        /// <summary>
        /// Test to generate event data from an authflow result with null token result and null errors.
        /// </summary>
        [Test]
        public void TestGenerateEvent_From_AuthFlowResult_With_Null_TokenResult_Null_Errors()
        {
            AuthFlowResult authFlowResult = new AuthFlowResult(null, null, "AuthFlowName");
            var subject = this.serviceProvider.GetService<CommandAad>();

            // Act
            var eventData = subject.AuthFlowEventData(authFlowResult);

            // Assert
            eventData.Properties.Should().NotContainKey("msal_correlation_ids");
            eventData.Properties.Should().NotContainKey("error_messages");
            eventData.Measures.Should().NotContainKey("token_validity_hours");
            eventData.Properties.Should().NotContainKey("silent");

            eventData.Properties.Should().Contain("authflow", "AuthFlowName");
            eventData.Properties.Should().Contain("success", "False");
            eventData.Measures.Should().ContainKey("duration_milliseconds");
        }

        /// <summary>
        /// Test to generate event data from an authflow result with null token result and some errors.
        /// </summary>
        [Test]
        public void TestGenerateEvent_From_AuthFlowResult_With_Errors_And_Null_TokenResult()
        {
            var errors = new[]
            {
                new Exception("Exception 1."),
            };

            AuthFlowResult authFlowResult = new AuthFlowResult(null, errors, "AuthFlowName");
            var subject = this.serviceProvider.GetService<CommandAad>();

            // Act
            var eventData = subject.AuthFlowEventData(authFlowResult);

            // Assert
            eventData.Properties.Should().NotContainKey("msal_correlation_ids");
            eventData.Measures.Should().NotContainKey("token_validity_minutes");
            eventData.Properties.Should().NotContainKey("silent");
            eventData.Properties.Should().Contain("authflow", "AuthFlowName");
            eventData.Properties.Should().Contain("success", "False");
            eventData.Properties.Should().Contain("error_messages", "[{\"Message\":\"Exception 1.\",\"InnerException\":null,\"ExceptionType\":\"System.Exception\",\"AADErrorCode\":null,\"CorrelationId\":null}]");
            eventData.Measures.Should().ContainKey("duration_milliseconds");
        }

        [Test]
        public void TestSendEvent_From_AuthFlowResult_With_Errors_And_Null_TokenResult()
        {
            var errors = new[]
            {
                new Exception("Exception 1."),
            };

            AuthFlowResult authFlowResult = new AuthFlowResult(null, errors, "Sample");

            this.authFlowMock.Setup((a) => a.GetTokenAsync()).ReturnsAsync(authFlowResult);

            // It would be nice to increase the assertion level here to include specific attributes on the eventData sent.
            // The mock setup matching with strict behavior is difficult to get right, and we have other tests already validating the contents
            // of events generated have what we expect. This validates we are in fact sending them.
            this.telemetryServiceMock.Setup(s => s.SendEvent("authflow_Sample", It.IsAny<EventData>()));

            var subject = this.serviceProvider.GetService<CommandAad>();
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);

            // mock valid args
            subject.Resource = "f0e8d801-3a50-48fd-b2da-6476d6e832a2";
            subject.Client = "e19f71ed-3b14-448d-9346-9eff9753646b";
            subject.Tenant = "9f6227ee-3d14-473e-8bed-1281171ef8c9";
            subject.EvaluateOptions().Should().BeTrue();

            // Act
            // Note: If the mock telem service matching fails, the OnExecute's global try-catch will hide that failure.
            subject.OnExecute().Should().Be(1);

            // Assert
            this.telemetryServiceMock.VerifyAll();
        }

        /// <summary>
        /// Test to generate event data from an authflow result with token result and msal errors.
        /// </summary>
        [Test]
        public void TestGenerateEvent_From_AuthFlowResult_With_MsalErrors_And_TokenResult()
        {
            // TODO
            var correlationID1 = Guid.NewGuid().ToString();
            var msalServiceException = new MsalServiceException("1", "An MSAL Service Exception message");
            msalServiceException.CorrelationId = correlationID1;

            var msalUIRequiredException = new MsalUiRequiredException("2", "An MSAL UI Required Exception message");
            msalUIRequiredException.CorrelationId = null;

            var errors = new[]
            {
                msalServiceException,
                msalUIRequiredException,
            };

            var tokenResultCorrelationID = Guid.NewGuid();
            var tokenResult = new TokenResult(new JsonWebToken(FakeToken), tokenResultCorrelationID);

            AuthFlowResult authFlowResult = new AuthFlowResult(tokenResult, errors, "AuthFlowName");
            var subject = this.serviceProvider.GetService<CommandAad>();

            // Act
            var eventData = subject.AuthFlowEventData(authFlowResult);

            // Assert
            eventData.Properties.Should().Contain("authflow", "AuthFlowName");
            eventData.Properties.Should().Contain("success", "True");
            eventData.Properties.Should().Contain("msal_correlation_id", $"{tokenResultCorrelationID}");
            eventData.Properties.Should().Contain("silent", "False");
            eventData.Properties.Should().Contain("error_messages", "[{\"Message\":\"An MSAL Service Exception message\",\"InnerException\":null,\"ExceptionType\":\"Microsoft.Identity.Client.MsalServiceException\",\"AADErrorCode\":\"AADSTS1\",\"CorrelationId\":\"" + $"{correlationID1}" + "\"},{\"Message\":\"An MSAL UI Required Exception message\",\"InnerException\":null,\"ExceptionType\":\"Microsoft.Identity.Client.MsalUiRequiredException\",\"AADErrorCode\":\"AADSTS2\",\"CorrelationId\":null}]");
            eventData.Measures.Should().ContainKey("token_validity_minutes");
            eventData.Measures.Should().ContainKey("duration_milliseconds");
        }

        /// <summary>
        /// Test to generate event data from an authflow result with token result and no errors.
        /// </summary>
        [Test]
        public void TestGenerateEvent_From_AuthFlowResult_With_TokenResult_And_Null_Errors()
        {
            // TODO
            var tokenResultCorrelationID = Guid.NewGuid();
            var tokenResult = new TokenResult(new JsonWebToken(FakeToken), tokenResultCorrelationID);

            AuthFlowResult authFlowResult = new AuthFlowResult(tokenResult, null, "AuthFlowName");
            var subject = this.serviceProvider.GetService<CommandAad>();

            var expectedCorrelationIDs = $"{tokenResultCorrelationID}";

            // Act
            var eventData = subject.AuthFlowEventData(authFlowResult);

            // Assert
            eventData.Properties.Should().NotContainKey("error_messages");
            eventData.Properties.Should().Contain("authflow", "AuthFlowName");
            eventData.Properties.Should().Contain("success", "True");
            eventData.Properties.Should().Contain("msal_correlation_id", expectedCorrelationIDs);
            eventData.Properties.Should().Contain("silent", "False");
            eventData.Measures.Should().ContainKey("token_validity_minutes");
            eventData.Measures.Should().ContainKey("duration_milliseconds");
        }

        [TestCase("1", true)]
        [TestCase("non-empty-string", false)]
        [TestCase("true", false)]
        [TestCase("", false)]
        public void InteractiveAuth_IsDisabledOnCorextEnvVar(string corextNonInteractive, bool expected)
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);
            this.envMock.Setup(e => e.Get("Corext_NonInteractive")).Returns(corextNonInteractive);
            subject.InteractiveAuthDisabled().Should().Be(expected);
        }

        [TestCase("1", true)]
        [TestCase("non-empty-string", true)]
        [TestCase("true", true)]
        [TestCase("", false)]
        public void InteractiveAuth_IsDisabledOnNoUserEnvVar(string noUser, bool expected)
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);
            this.envMock.Setup(e => e.Get("AZUREAUTH_NO_USER")).Returns(noUser);
            subject.InteractiveAuthDisabled().Should().Be(expected);
        }

        [Test]
        public void InteractiveAuth_IsEnabledIfEnvVarsAreNotSet()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            this.envMock.Setup(env => env.Get(It.IsAny<string>())).Returns((string)null);
            subject.InteractiveAuthDisabled().Should().BeFalse();
        }

#if PlatformWindows
        [TestCase("non-empty-string")]
        public void GetCombinedAuthMode_withInteractiveAuthDisabled(string noUser)
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            this.envMock.Setup(e => e.Get("AZUREAUTH_NO_USER")).Returns(noUser);
            subject.CombinedAuthMode.Should().Be(AuthMode.IWA);
        }

        public void GetCombinedAuthMode_withInteractiveAuthEnabled()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            var authModes = new List<AuthMode>();
            authModes.Add(AuthMode.Broker);
            subject.AuthModes = authModes;
            subject.CombinedAuthMode.Should().Be(AuthMode.Broker);
        }
#endif

        public void GetCombinedAuthMode_withInteractiveAuthEnabled_NonWindowsPlatform()
        {
            CommandAad subject = this.serviceProvider.GetService<CommandAad>();
            var authModes = new List<AuthMode>();
            authModes.Add(AuthMode.Web);
            subject.AuthModes = authModes;
            subject.CombinedAuthMode.Should().Be(AuthMode.Web);
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