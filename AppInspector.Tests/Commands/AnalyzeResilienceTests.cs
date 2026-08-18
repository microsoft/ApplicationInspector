using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.Logging;
using Xunit;

namespace AppInspector.Tests.Commands;

/// <summary>
///     A single file that cannot be analyzed must be recorded against that file and leave the rest of the
///     scan intact, rather than abandoning the run.
/// </summary>
[ExcludeFromCodeCoverage]
public class AnalyzeResilienceTests : IDisposable
{
    // A label containing an unmatched parenthesis corrupts the expression handed to the engine, which throws
    // while evaluating rather than simply not matching. Verification rejects such a label, so the scan below
    // disables it; the point here is what the scan does when evaluation throws, not how the rule got that way.
    private const string throwingRule = @"[
    {
        ""id"": ""SA900001"",
        ""name"": ""Testing.Rules.Throws"",
        ""tags"": [ ""Testing.Rules.Throws"" ],
        ""severity"": ""Critical"",
        ""description"": ""expression that fails at evaluation time"",
        ""patterns"": [
            { ""pattern"": ""alpha"", ""type"": ""substring"", ""label"": ""a)"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""beta"",  ""type"": ""substring"", ""label"": ""b"", ""scopes"": [ ""code"" ] }
        ]
    }
]";

    private readonly string _rulePath;
    private readonly string _sourceDirectory;

    public AnalyzeResilienceTests()
    {
        _sourceDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_sourceDirectory);
        File.WriteAllText(Path.Combine(_sourceDirectory, "one.c"), "alpha\n");
        File.WriteAllText(Path.Combine(_sourceDirectory, "two.c"), "beta\n");

        _rulePath = Path.Combine(_sourceDirectory, "rule.json");
        File.WriteAllText(_rulePath, throwingRule);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_sourceDirectory, true);
        }
        catch (IOException)
        {
        }
    }

    private AnalyzeOptions Options(bool singleThread)
    {
        return new AnalyzeOptions
        {
            SourcePath = new[] { Path.Combine(_sourceDirectory, "one.c"), Path.Combine(_sourceDirectory, "two.c") },
            CustomRulesPath = _rulePath,
            IgnoreDefaultRules = true,
            DisableCustomRuleVerification = true,
            SingleThread = singleThread
        };
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailingFileIsRecordedAndScanContinues(bool singleThread)
    {
        var factory = new LogOptions().GetLoggerFactory();
        AnalyzeCommand command = new(Options(singleThread), factory);

        var result = command.GetResult();

        Assert.Equal(2, result.Metadata.Files.Count);
        Assert.All(result.Metadata.Files, x => Assert.Equal(ScanState.Error, x.Status));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailingFileIsRecordedAndScanContinuesAsync(bool singleThread)
    {
        var factory = new LogOptions().GetLoggerFactory();
        AnalyzeCommand command = new(Options(singleThread), factory);

        var result = await command.GetResultAsync();

        Assert.Equal(2, result.Metadata.Files.Count);
        Assert.All(result.Metadata.Files, x => Assert.Equal(ScanState.Error, x.Status));
    }
}
