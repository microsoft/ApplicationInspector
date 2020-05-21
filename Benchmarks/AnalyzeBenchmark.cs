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
    [ConcurrencyVisualizerProfiler]
    public class AnalyzeBenchmark
    {
        public AnalyzeBenchmark()
        {
        }

        [Benchmark(Baseline = true)]
        public void AnalyzeSingleThreaded()
        {
            var path = "D:\\GitHub\\ApplicationInspector\\UnitTest.Commands";
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
            var path = "D:\\GitHub\\ApplicationInspector\\UnitTest.Commands";
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
