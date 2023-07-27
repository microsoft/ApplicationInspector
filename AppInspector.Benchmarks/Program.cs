using BenchmarkDotNet.Running;

namespace ApplicationInspector.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // new DebugInProcessConfig()
        //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
        var summary = BenchmarkRunner.Run<WriterBench>();
    }
}