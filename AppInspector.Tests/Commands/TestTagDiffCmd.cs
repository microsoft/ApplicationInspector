using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.Commands;

/// <summary>
///     Test class for TagDiff Command
/// </summary>
[TestClass]
[ExcludeFromCodeCoverage]
public class TestTagDiffCmd
{
    private static ILoggerFactory loggerFactory = new NullLoggerFactory();

    private static readonly LogOptions logOptions = new();
    private static string testFileFourWindowsNoLinuxPath = string.Empty;
    private static string testFileFourWindowsOneLinuxCopyPath = string.Empty;
    private static string testFileFourWindowsOneLinuxPath = string.Empty;
    private static string testRulesPath = string.Empty;

    [ClassInitialize]
    public static void InitOutput(TestContext testContext)
    {
        loggerFactory = logOptions.GetLoggerFactory();
        Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
        testFileFourWindowsOneLinuxPath =
            Path.Combine("TestData", "TestTagDiffCmd", "Samples", "FourWindowsOneLinux.js");

        testFileFourWindowsOneLinuxCopyPath = testFileFourWindowsOneLinuxPath;

        testFileFourWindowsNoLinuxPath =
            Path.Combine("TestData", "TestTagDiffCmd", "Samples", "FourWindowsNoLinux.js");

        testRulesPath = Path.Combine("TestData", "TestTagDiffCmd", "Rules", "FindWindows.json");
    }

    [DataRow(TagTestType.Equality, TagDiffResult.ExitCode.TestPassed)]
    [DataRow(TagTestType.Inequality, TagDiffResult.ExitCode.TestFailed)]
    [TestMethod]
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

        Assert.AreEqual(expectedExitCode, result.ResultCode);
    }

    [DataRow(TagTestType.Equality, TagDiffResult.ExitCode.TestFailed)]
    [DataRow(TagTestType.Inequality, TagDiffResult.ExitCode.TestPassed)]
    [TestMethod]
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

        Assert.AreEqual(expectedExitCode, result.ResultCode);
    }

    [TestMethod]
    public void InvalidSourcePath_Fail()
    {
        TagDiffOptions options = new()
        {
            SourcePath1 = new[] { $"{testFileFourWindowsOneLinuxPath}.not.a.path" },
            SourcePath2 = new[] { testFileFourWindowsOneLinuxPath },
            FilePathExclusions = Array.Empty<string>() //allow source under unittest path
        };
        var cmd = new TagDiffCommand(options, loggerFactory);
        Assert.ThrowsException<OpException>(() => cmd.GetResult());
    }

    [TestMethod]
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
        Assert.ThrowsException<OpException>(() => command.GetResult());
    }
}