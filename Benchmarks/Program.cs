using System;
using System.IO;
using ApplicationInspector.Unitprocess.Misc;
using BenchmarkDotNet.Running;
using Benchmarks;

namespace ApplicationInspector.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<AnalyzeBenchmark>();
        }
    }
}
