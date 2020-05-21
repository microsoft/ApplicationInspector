using System;
using System.IO;
using System.Reflection;
using ApplicationInspector.Unitprocess.Misc;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Microsoft.ApplicationInspector.Commands;

namespace Benchmarks
{
    //[ConcurrencyVisualizerProfiler]
    public class AnalyzeBenchmark
    {
        // Manually put the file you want to benchmark. But don't put this in a path with "Test" in the name ;)
        private const string path = "D:\\runtime-master.zip";

        public AnalyzeBenchmark()
        {
        }

        [Benchmark(Baseline = true)]
        public void AnalyzeSingleThreaded()
        {
            AnalyzeCommand command = new AnalyzeCommand(new AnalyzeOptions()
            {
                SourcePath = path,
                AllowDupTags = false,
                SingleThread = true,
                IgnoreDefaultRules = false
            });

            AnalyzeResult analyzeResult = command.GetResult();
        }

        [Benchmark]
        public void AnalyzeMultiThread()
        {
            AnalyzeCommand command = new AnalyzeCommand(new AnalyzeOptions()
            {
                SourcePath = path,
                AllowDupTags = false,
                SingleThread = false,
                IgnoreDefaultRules = false
            });

            AnalyzeResult analyzeResult = command.GetResult();
        }

        public static string GetExecutingDirectoryName()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).Directory.FullName;
        }
    }
}
