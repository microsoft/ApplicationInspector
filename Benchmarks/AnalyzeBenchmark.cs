using BenchmarkDotNet.Attributes;
using Microsoft.ApplicationInspector.Commands;
using System;
using System.IO;
using System.Reflection;

namespace Benchmarks
{
    //[ConcurrencyVisualizerProfiler]
    public class AnalyzeBenchmark
    {
        // Manually put the file you want to benchmark. But don't put this in a path with "Test" in the name ;)
        private const string path = "C:\\Users\\gstocco\\Documents\\GitHub\\ApplicationInspector\\RulesEngine";

        public AnalyzeBenchmark()
        {
        }

        //[Benchmark(Baseline = true)]
        //public void AnalyzeSingleThreaded()
        //{
        //    AnalyzeCommand command = new AnalyzeCommand(new AnalyzeOptions()
        //    {
        //        SourcePath = path,
        //        SingleThread = true,
        //        IgnoreDefaultRules = false
        //    });

        //    AnalyzeResult analyzeResult = command.GetResult();
        //}

        [Benchmark]
        public void AnalyzeMultiThread()
        {
            AnalyzeCommand command = new AnalyzeCommand(new AnalyzeOptions()
            {
                SourcePath = path,
                SingleThread = false,
                IgnoreDefaultRules = false,
                FilePathExclusions = "bin,obj"
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
