using System;
using System.IO;
using System.Reflection;
using ApplicationInspector.Unitprocess.Misc;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using Microsoft.ApplicationInspector.Commands;

namespace Benchmarks
{
    public class AnalyzeBenchmark
    {
        public AnalyzeBenchmark()
        {
        }

        [Benchmark(Baseline = true)]
        public void AnalyzeSingleThreaded()
        {
            var path = "/Users/gabe/Documents/GitHub/ApplicationInspector/UnitTest.Commands/source";
            AnalyzeCommand command = new AnalyzeCommand(new AnalyzeOptions()
            {
                SourcePath = path,
                AllowDupTags = true,
                SingleThread = true,
                IgnoreDefaultRules = false
            });

            AnalyzeResult analyzeResult = command.GetResult();
        }

        [Benchmark]
        public void AnalyzeMultiThread()
        {
            var path = "/Users/gabe/Documents/GitHub/ApplicationInspector/UnitTest.Commands/source";
            AnalyzeCommand command = new AnalyzeCommand(new AnalyzeOptions()
            {
                SourcePath = path,
                AllowDupTags = true,
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
