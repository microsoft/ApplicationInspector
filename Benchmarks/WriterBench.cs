using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using DotLiquid;
using Microsoft.ApplicationInspector.CLI;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.RulesEngine;

namespace ApplicationInspector.Benchmarks;
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net70)]
public class WriterBench
{
    [Params(1000, 10000)]
    public int N;

    // Holds the result object which will be serialized
    private AnalyzeResult _result;
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        var _exerpt = "Hello World";
        var helper = new MetaDataHelper("..");
        var matchRecord = new MatchRecord("rule-id", "rule-name")
        {
            Boundary = new Boundary() { Index = 0, Length = 1 },
            EndLocationColumn = 0,
            EndLocationLine = 1,
            Excerpt = _exerpt,
            FileName = "TestFile",
            LanguageInfo = new LanguageInfo(),
            Tags = new []{"TestTag"}
        };
        for (int i = 0; i < N; i++)
        {
            helper.AddMatchRecord(matchRecord);
        }
        helper.PrepareReport();

        _result = new AnalyzeResult() { Metadata = helper.GetMetadata(), ResultCode = 0 };
    }
    
    [Benchmark(Baseline = true)]
    public void AnalyzeSingleThreaded()
    {
        var tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        CLIAnalyzeCmdOptions analyzeOpts = new CLIAnalyzeCmdOptions()
        {
            OutputFileFormat = "json",
            OutputFilePath = tmpPath
        };
        var writerFactory = new WriterFactory();
        var writer = writerFactory.GetWriter(analyzeOpts);
        writer.WriteResults(_result,analyzeOpts);
        File.Delete(tmpPath);
    }
    
    public static string GetExecutingDirectoryName()
    {
        if (Assembly.GetEntryAssembly()?.GetName().CodeBase is string codeBaseLoc)
        {
            var location = new Uri(codeBaseLoc);
            return new FileInfo(location.AbsolutePath).Directory?.FullName ?? string.Empty;
        }

        return string.Empty;
    }
}