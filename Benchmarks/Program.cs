namespace ApplicationInspector.Benchmarks
{
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Running;

    public class Program
    {
        public static void Main(string[] args)
        {
            // new DebugInProcessConfig()
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
            //var summary = BenchmarkRunner.Run<AnalyzeBenchmark>();
        }
    }
}
