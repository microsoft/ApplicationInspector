using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AppInspector.Tests.Commands;

/// <summary>
///     Test class for TagDiff Command
/// </summary>
[ExcludeFromCodeCoverage]
public class TestTagDiffCmd
{
    private static ILoggerFactory loggerFactory = new NullLoggerFactory();

    private static readonly LogOptions logOptions = new();
    private static string testFileFourWindowsNoLinuxPath = string.Empty;
    private static string testFileFourWindowsOneLinuxCopyPath = string.Empty;
    private static string testFileFourWindowsOneLinuxPath = string.Empty;
    private static string testRulesPath = string.Empty;

    public TestTagDiffCmd()
    {
        loggerFactory = logOptions.GetLoggerFactory();
        testFileFourWindowsOneLinuxPath =
            Path.Combine("TestData", "TestTagDiffCmd", "Samples", "FourWindowsOneLinux.js");

        testFileFourWindowsOneLinuxCopyPath = testFileFourWindowsOneLinuxPath;

        testFileFourWindowsNoLinuxPath =
            Path.Combine("TestData", "TestTagDiffCmd", "Samples", "FourWindowsNoLinux.js");

        testRulesPath = Path.Combine("TestData", "TestTagDiffCmd", "Rules", "FindWindows.json");
    }

    [InlineData(TagTestType.Equality, TagDiffResult.ExitCode.TestPassed)]
    [InlineData(TagTestType.Inequality, TagDiffResult.ExitCode.TestFailed)]
    [Theory]
    public void Equality(TagTestType tagTestType, TagDiffResult.ExitCode expectedExitCode)
    {
        TagDiffOptions options = new()
        {
            SourcePath1 = new[] { testFileFourWindowsOneLinuxPath },
            SourcePath2 = new[] { testFileFourWindowsOneLinuxCopyPath },
            FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            IgnoreDefaultRules = true,
            TestType = tagTestType,
            CustomRulesPath = testRulesPath
        };

        TagDiffCommand command = new(options, loggerFactory);
        var result = command.GetResult();

        Assert.Equal(expectedExitCode, result.ResultCode);
    }

    [InlineData(TagTestType.Equality, TagDiffResult.ExitCode.TestFailed)]
    [InlineData(TagTestType.Inequality, TagDiffResult.ExitCode.TestPassed)]
    [Theory]
    public void Inequality(TagTestType tagTestType, TagDiffResult.ExitCode expectedExitCode)
    {
        TagDiffOptions options = new()
        {
            SourcePath1 = new[] { testFileFourWindowsOneLinuxPath },
            SourcePath2 = new[] { testFileFourWindowsNoLinuxPath },
            FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            IgnoreDefaultRules = true,
            TestType = tagTestType,
            CustomRulesPath = testRulesPath
        };

        TagDiffCommand command = new(options, loggerFactory);
        var result = command.GetResult();

        Assert.Equal(expectedExitCode, result.ResultCode);
    }

    [Fact]
    public void InvalidSourcePath_Fail()
    {
        TagDiffOptions options = new()
        {
            SourcePath1 = new[] { $"{testFileFourWindowsOneLinuxPath}.not.a.path" },
            SourcePath2 = new[] { testFileFourWindowsOneLinuxPath },
            FilePathExclusions = Array.Empty<string>() //allow source under unittest path
        };
        var cmd = new TagDiffCommand(options, loggerFactory);
        Assert.Throws<OpException>(() => cmd.GetResult());
    }

    [Fact]
    public void NoDefaultNoCustomRules_Fail()
    {
        TagDiffOptions options = new()
        {
            SourcePath1 = new[] { testFileFourWindowsOneLinuxPath },
            SourcePath2 = new[] { testFileFourWindowsOneLinuxCopyPath },
            FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            IgnoreDefaultRules = true
        };

        var command = new TagDiffCommand(options, loggerFactory);
        Assert.Throws<OpException>(() => command.GetResult());
    }
}