using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AppInspector.Tests.Commands;

[ExcludeFromCodeCoverage]
public class TestExportTagsCmd
{
    private ILoggerFactory factory = new NullLoggerFactory();

    private readonly LogOptions logOptions = new();
    private string testRulesPath = string.Empty;
    public TestExportTagsCmd()
    {
        factory = logOptions.GetLoggerFactory();
        testRulesPath = Path.Combine("TestData","TestExportTagsCmd","Rules", "TestRules.json");
    }

    [Fact]
    public void ExportCustom()
    {
        ExportTagsOptions options = new()
        {
            IgnoreDefaultRules = true,
            CustomRulesPath = testRulesPath
        };
        ExportTagsCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Contains("Test.Tags.Linux", result.TagsList);
        Assert.Contains("Test.Tags.Windows", result.TagsList);
        Assert.Equal(2, result.TagsList.Count);
        Assert.Equal(ExportTagsResult.ExitCode.Success, result.ResultCode);
    }

    [Fact]
    public void ExportDefault()
    {
        ExportTagsOptions options = new()
        {
            IgnoreDefaultRules = false
        };
        ExportTagsCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(ExportTagsResult.ExitCode.Success, result.ResultCode);
    }

    [Fact]
    public void NoDefaultNoCustomRules()
    {
        ExportTagsOptions options = new()
        {
            IgnoreDefaultRules = true
        };
        Assert.Throws<OpException>(() => new ExportTagsCommand(options));
    }

    [Fact]
    public void ExportJsonSerialization()
    {
        ExportTagsOptions options = new()
        {
            IgnoreDefaultRules = true,
            CustomRulesPath = testRulesPath
        };
        ExportTagsCommand command = new(options, factory);
        var result = command.GetResult();
        
        // Test JSON serialization via the production JsonWriter to ensure tags are included
        using var memoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(memoryStream);
        
        // Use the JsonWriter that production code uses
        var jsonWriter = new Microsoft.ApplicationInspector.CLI.Writers.JsonWriter(streamWriter, factory);
        var cliOptions = new Microsoft.ApplicationInspector.CLI.CLIExportTagsCmdOptions
        {
            IgnoreDefaultRules = true,
            CustomRulesPath = testRulesPath
        };
        
        jsonWriter.WriteResults(result, cliOptions, autoClose: false);
        streamWriter.Flush();
        
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        string json = reader.ReadToEnd();
        
        // Verify tags are present in JSON output from JsonWriter
        Assert.Contains("Test.Tags.Linux", json);
        Assert.Contains("Test.Tags.Windows", json);
        Assert.Contains("tagsList", json);
        Assert.Contains("appVersion", json);
    }
}