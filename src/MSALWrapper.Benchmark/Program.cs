using BenchmarkDotNet.Running;

namespace Microsoft.Authentication.MSALWrapper.Benchmark
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<BrokerBenchmark>();
            Console.WriteLine(summary);
        }
    }
}
