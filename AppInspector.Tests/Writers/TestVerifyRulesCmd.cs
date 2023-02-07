using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CommandLine;
using Microsoft.ApplicationInspector.CLI;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.Commands;

// TODO: This does not intentionally try to make the OAT rule maker fail
// The OAT rules are being validated but there aren't test cases that intentionally try to break it.
[TestClass]
[ExcludeFromCodeCoverage]
public class TestWriters
{
    [TestMethod]
    public void MustNotMatchDetectIncorrect()
    {
        int N = 1000;
        // Holds the result object which will be serialized
        AnalyzeResult _result;

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

}