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
        private readonly Guid clientID = Guid.Parse("872cd9fa-d31f-45e0-9eab-6e460a02d1f1"); // Replace with your own client ID.
        private readonly Guid tenantID = Guid.Parse("72f988bf-86f1-41af-91ab-2d7cd011db47"); // Replace with your own tenant ID.
        private readonly List<string> scopes = new List<string>();
        private readonly ILogger logger;

        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<BrokerBenchmark>();
            Console.WriteLine(summary);
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
        }

        /// <summary>
        /// Benchmark with C++ native broker.
        /// </summary>
        [Benchmark]
        public void NativeBrokerBenchmark()
        {
            var pcaWrapper = BuildPCAWrapper(this.logger, this.clientID, this.tenantID.ToString());
            AuthParameters authParameters = new AuthParameters(this.clientID, this.tenantID, this.scopes);
            Broker broker = new Broker(this.logger, authParameters, pcaWrapper: pcaWrapper);

            broker.GetTokenAsync().Wait();
        }

        private IPCAWrapper BuildPCAWrapper(ILogger logger, Guid clientId, string tenantId)
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
            clientBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));

            return new PCAWrapper(logger, clientBuilder.Build(), errors, tenantId);
        }
        private void LogMSAL(Identity.Client.LogLevel level, string message, bool containsPii)
        {
            this.logger.LogTrace($"MSAL: {message}");
        }
    }
}
