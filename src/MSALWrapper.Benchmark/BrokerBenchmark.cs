// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Benchmark
{
    using BenchmarkDotNet.Attributes;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Identity.Client;
    using Microsoft.Identity.Client.Broker;
    using Microsoft.Extensions.Logging;
    using Microsoft.Authentication.MSALWrapper.AuthFlow;
    using BenchmarkDotNet.Running;

    /// <summary>
    /// <see cref="BrokerBenchmark"/> class is for benchmark comparison between C++ native broker (available after Microsoft.Identity.Client 4.44) and .Net managed broker.
    /// </summary>
    public class BrokerBenchmark
    {
        private readonly Guid clientID = Guid.Parse("00000000-0000-0000-0000-000000000000"); // Replace with your own client ID.
        private readonly Guid tenantID = Guid.Parse("00000000-0000-0000-0000-000000000000"); // Replace with your own tenant ID.
        private readonly List<string> scopes = new List<string>();
        private readonly ILogger logger;

        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<BrokerBenchmark>();
            Console.WriteLine(summary);
        }

        /// <summary>
        /// Use the same cache file in normal process.
        /// </summary>
        private string CacheFilePath
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string absolutePath = Path.Combine(appData, ".IdentityService", $"msal_{this.tenantID}.cache");
                return absolutePath;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrokerBenchmark"/> class.
        /// </summary>
        public BrokerBenchmark()
        {
            var loggerFactory = new LoggerFactory();
            this.logger = loggerFactory.CreateLogger<BrokerBenchmark>();

            WarmUp();
        }

        /// <summary>
        /// Warm up a cache file in case of outline data points.
        /// </summary>
        public void WarmUp()
        {
            NativeBrokerBenchmark();
            ManagedBrokerBenchmark();
        }

        /// <summary>
        /// Benchmark with C++ native broker.
        /// </summary>
        [Benchmark]
        public void NativeBrokerBenchmark()
        {
            var pcaWrapper = BuildPCAWrapper(this.logger, this.clientID, this.tenantID, null, this.CacheFilePath, true);
            Broker broker = new Broker(this.logger, this.clientID, this.tenantID, this.scopes, this.CacheFilePath, pcaWrapper: pcaWrapper);

            broker.GetTokenAsync().Wait();
        }

        /// <summary>
        /// Benchmark with .Net managed broker.
        /// </summary>
        [Benchmark]
        public void ManagedBrokerBenchmark()
        {
            var pcaWrapper = BuildPCAWrapper(this.logger, this.clientID, this.tenantID, null, this.CacheFilePath, false);
            Broker broker = new Broker(this.logger, this.clientID, this.tenantID, this.scopes, this.CacheFilePath, pcaWrapper: pcaWrapper);

            broker.GetTokenAsync().Wait();
        }

        private IPCAWrapper BuildPCAWrapper(ILogger logger, Guid clientId, Guid tenantId, string? osxKeyChainSuffix, string cacheFilePath, bool useNativeBroker)
        {
            IList<Exception> errors = new List<Exception>();

            var clientBuilder =
                    PublicClientApplicationBuilder
                    .Create($"{clientId}")
                    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                    .WithLogging(
                    this.LogMSAL,
                    Identity.Client.LogLevel.Verbose,
                    enablePiiLogging: false,
                    enableDefaultPlatformLogging: true)
;
            if (useNativeBroker)
            {
                clientBuilder.WithBrokerPreview();
            }
            else
            {
                clientBuilder.WithBroker();
            }

            return new PCAWrapper(logger, clientBuilder.Build(), errors, tenantId, osxKeyChainSuffix, cacheFilePath);
        }
        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }
    }
}
