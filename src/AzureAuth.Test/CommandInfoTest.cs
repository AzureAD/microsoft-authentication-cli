// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test
{
    using System;
    using System.IO.Abstractions;
    using System.IO.Abstractions.TestingHelpers;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Authentication.AzureAuth;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;
    using Moq;
    using NLog.Extensions.Logging;
    using NLog.Targets;
    using NUnit.Framework;

    internal class CommandInfoTest
    {
        private MockFileSystem fileSystem;
        private MemoryTarget logTarget;
        private Mock<IEnv> envMock;
        private ServiceProvider serviceProvider;

        [SetUp]
        public void Setup()
        {
            this.fileSystem = new MockFileSystem();

            // Setup in memory logging target with NLog - allows making assertions against what has been logged.
            var loggingConfig = new NLog.Config.LoggingConfiguration();
            this.logTarget = new MemoryTarget("memory_target");
            this.logTarget.Layout = "${message}"; // Define a simple layout so we don't get timestamps in messages.
            loggingConfig.AddTarget(this.logTarget);
            loggingConfig.AddRuleForAllLevels(this.logTarget);

            this.envMock = new Mock<IEnv>(MockBehavior.Strict);

            // Setup Dependency Injection container to provide logger and out class under test (the "subject").
            this.serviceProvider = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog(loggingConfig);
                })
                .AddSingleton<IFileSystem>(this.fileSystem)
                .AddSingleton<IEnv>(this.envMock.Object)
                .AddTransient<CommandInfo>()
                .BuildServiceProvider();
        }

        /// <summary>
        /// After calling <see cref="CommandInfo.ExecuteResetDeviceID"/>, the device ID should be different with the previous one.
        /// </summary>
        public void TestResetDeviceID()
        {
            CommandInfo subject = this.serviceProvider.GetService<CommandInfo>();
            subject.ResetDeviceID = true;

            string deviceIDBeforeReset = TelemetryMachineIDHelper.GetRandomDeviceIDAsync(this.fileSystem).Result;
            deviceIDBeforeReset.Should().NotBeNullOrEmpty();

            subject.OnExecute();

            string deviceIDAfterReset = TelemetryMachineIDHelper.GetRandomDeviceIDAsync(this.fileSystem).Result;
            deviceIDAfterReset.Should().NotBeEquivalentTo(deviceIDBeforeReset);
        }
    }
}
