using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.ApplicationInspector.CLI;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AppInspector.Tests.Commands;

/// <summary>
///     Regression coverage for the shared JsonWriter path used by exporttags, tagdiff and verifyrules.
///     Results were previously serialized against the <see cref="Result" /> base type, so every property
///     declared on the derived result was dropped and the output contained only appVersion.
/// </summary>
[ExcludeFromCodeCoverage]
public class TestJsonWriter
{
    private readonly ILoggerFactory _factory = new LogOptions().GetLoggerFactory();

    [Fact]
    public void ExportTagsWritesTagsList()
    {
        var rulesPath = Path.Combine("TestData", "TestExportTagsCmd", "Rules", "TestRules.json");
        ExportTagsOptions options = new()
        {
            IgnoreDefaultRules = true,
            CustomRulesPath = rulesPath
        };
        var result = new ExportTagsCommand(options, _factory).GetResult();

        var json = WriteAsJson(result, new CLIExportTagsCmdOptions
        {
            IgnoreDefaultRules = true,
            CustomRulesPath = rulesPath
        });

        var written = JsonSerializer.Deserialize<ExportTagsResult>(json);
        Assert.NotNull(written);
        Assert.Equal(result.TagsList.OrderBy(x => x), written!.TagsList.OrderBy(x => x));
        Assert.Contains("Test.Tags.Linux", written.TagsList);
        Assert.Contains("Test.Tags.Windows", written.TagsList);
        Assert.False(string.IsNullOrEmpty(written.AppVersion));
    }

    /// <summary>
    ///     Guards <see cref="TagDiffResult.TagDiffList" /> staying a property. As a field it was invisible to
    ///     System.Text.Json and silently omitted from the report.
    /// </summary>
    [Fact]
    public void TagDiffWritesTagDiffList()
    {
        var rulesPath = Path.Combine("TestData", "TestTagDiffCmd", "Rules", "FindWindows.json");
        var sourceWithLinux = Path.Combine("TestData", "TestTagDiffCmd", "Samples", "FourWindowsOneLinux.js");
        var sourceWithoutLinux = Path.Combine("TestData", "TestTagDiffCmd", "Samples", "FourWindowsNoLinux.js");

        TagDiffOptions options = new()
        {
            SourcePath1 = new[] { sourceWithLinux },
            SourcePath2 = new[] { sourceWithoutLinux },
            FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            IgnoreDefaultRules = true,
            TestType = TagTestType.Equality,
            CustomRulesPath = rulesPath
        };
        var result = new TagDiffCommand(options, _factory).GetResult();
        Assert.NotEmpty(result.TagDiffList);

        var json = WriteAsJson(result, new CLITagDiffCmdOptions
        {
            SourcePath1 = options.SourcePath1,
            SourcePath2 = options.SourcePath2,
            IgnoreDefaultRules = true,
            CustomRulesPath = rulesPath
        });

        var written = JsonSerializer.Deserialize<TagDiffResult>(json);
        Assert.NotNull(written);
        Assert.Equal(
            result.TagDiffList.Select(x => (x.Tag, x.Source)).OrderBy(x => x.Tag),
            written!.TagDiffList.Select(x => (x.Tag, x.Source)).OrderBy(x => x.Tag));
        Assert.Equal(result.ResultCode, written.ResultCode);
    }

    [Fact]
    public void VerifyRulesWritesRuleStatusList()
    {
        var rulesPath = Path.Combine("TestData", "TestVerifyRulesCmd", "Rules", "ValidRules.json");
        VerifyRulesOptions options = new()
        {
            CustomRulesPath = rulesPath
        };
        var result = new VerifyRulesCommand(options, _factory).GetResult();
        Assert.NotEmpty(result.RuleStatusList);

        var json = WriteAsJson(result, new CLIVerifyRulesCmdOptions
        {
            CustomRulesPath = rulesPath
        });

        // RuleStatus exposes computed and OAT-owned members that do not round-trip, so assert on the
        // parsed document rather than deserializing back into VerifyRulesResult.
        using var document = JsonDocument.Parse(json);
        var ruleStatusList = document.RootElement.GetProperty("ruleStatusList");
        Assert.Equal(JsonValueKind.Array, ruleStatusList.ValueKind);
        Assert.Equal(result.RuleStatusList.Count, ruleStatusList.GetArrayLength());

        var writtenIds = ruleStatusList.EnumerateArray().Select(x => x.GetProperty("RulesId").GetString());
        Assert.Equal(result.RuleStatusList.Select(x => x.RulesId).OrderBy(x => x), writtenIds.OrderBy(x => x));
    }

    /// <summary>
    ///     Writes a result to a temporary file through the same public entry point the CLI uses, so the test
    ///     covers writer selection as well as serialization.
    /// </summary>
    private string WriteAsJson(Result result, CLICommandOptions options)
    {
        var outputFilePath = Path.Combine(Path.GetTempPath(), $"test_json_{Guid.NewGuid()}.json");
        options.OutputFilePath = outputFilePath;
        options.OutputFileFormat = "json";

        try
        {
            new ResultsWriter(_factory).Write(result, options);
            Assert.True(File.Exists(outputFilePath));
            return File.ReadAllText(outputFilePath);
        }
        finally
        {
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }
        }
    }
}
