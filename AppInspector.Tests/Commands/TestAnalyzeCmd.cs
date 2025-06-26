using Xunit;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Assert = Xunit.Assert;

namespace AppInspector.Tests.Commands;

/// <summary>
///     Test class for the Analyze Command
/// </summary>
[ExcludeFromCodeCoverage]
public class TestAnalyzeCmd
{
    private const int numTimeOutFiles = 25;
    private const int numTimesContent = 25;
    
    private const string hardToFindContent = @"
asefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlakasefljkajsdfklasjdfklasjdfklasdfjklasdjfaklsdfjaklsdjfaklsfaksdjfkasdasdklfalskdfjalskdjfalskdjflaksdjflaskjdflaksjdflaksjdfljaskldfjjdkfaklsdfjlak@company.com1
buy@tacos.com
";

    private static string testFilePath = string.Empty;
    private static string testFilePathWithJsonExt = string.Empty;
    private static string testRulesPath = string.Empty;
    private static string appliesToTestRulePath = string.Empty;
    private static string doesNotApplyToTestRulePath = string.Empty;
    private static string dependsOnOneWayRulePath = string.Empty;
    private static string dependsOnChainRulePath = string.Empty;
    private static string dependsOnTwoWayRulePath = string.Empty;
    private static string overridesTestRulePath = string.Empty;
    private static string overridesWithoutOverrideTestRulePath = string.Empty;
    private static string unknownFileTypePath = string.Empty;
    private static string findWindowsWithFileRegex = string.Empty;

    // Test files for timeout tests
    private static readonly List<string> enumeratingTimeOutTestsFiles = new();

    private static string heavyRulePath = string.Empty;

    private ILoggerFactory factory = new NullLoggerFactory();
    private static string fourWindowsOne2000Path;
    private static string justAPath;
    private static string justBPath;
    private static string justCPath;
    public TestAnalyzeCmd()
    {
        testFilePath = Path.Combine("TestData", "TestAnalyzeCmd", "Samples", "FourWindowsOneLinux.js");
        unknownFileTypePath = Path.Combine("TestData", "TestAnalyzeCmd", "Samples", "FourWindowsOneLinux.unknownextension");
        testFilePathWithJsonExt = Path.Combine("TestData", "TestAnalyzeCmd", "Samples", "FourWindowsOneLinux.json");
        fourWindowsOne2000Path = Path.Combine("TestData", "TestAnalyzeCmd", "Samples", "ThreeWindowsOneWindows2000.js");
        justAPath = Path.Combine("TestData", "TestAnalyzeCmd", "Samples", "JustA.cs");
        justBPath = Path.Combine("TestData", "TestAnalyzeCmd", "Samples", "JustB.cs");
        justCPath = Path.Combine("TestData", "TestAnalyzeCmd", "Samples", "JustC.cs");
        
        testRulesPath = Path.Combine("TestData", "TestAnalyzeCmd", "Rules", "FindWindows.json");
        heavyRulePath = Path.Combine("TestData", "TestAnalyzeCmd", "Rules", "HeavyRule.json");
        appliesToTestRulePath = Path.Combine("TestData", "TestAnalyzeCmd", "Rules", "FindWindowsWithAppliesTo.json");
        doesNotApplyToTestRulePath = Path.Combine("TestData", "TestAnalyzeCmd", "Rules", "FindWindowsWithDoesNotApplyTo.json");
        dependsOnOneWayRulePath = Path.Combine("TestData", "TestAnalyzeCmd", "Rules", "DependsOnOneWay.json");
        dependsOnChainRulePath = Path.Combine("TestData", "TestAnalyzeCmd", "Rules", "DependsOnChain.json");
        dependsOnTwoWayRulePath = Path.Combine("TestData", "TestAnalyzeCmd", "Rules", "DependsOnTwoWay.json");
        findWindowsWithFileRegex = Path.Combine("TestData", "TestAnalyzeCmd", "Rules", "FindWindowsWithFileRegex.json");


        overridesTestRulePath =
            Path.Combine("TestData", "TestAnalyzeCmd", "Rules", "FindWindowsWithOverride.json");
        overridesWithoutOverrideTestRulePath = Path.Combine("TestData", "TestAnalyzeCmd", "Rules", "FindWindowsWithOverrideRuleWithoutOverride.json");

        Directory.CreateDirectory("TestOutput");
        for (var i = 0; i < numTimeOutFiles; i++)
        {
            var newPath = Path.Combine("TestOutput", $"TestFile-{i}.js");
            File.WriteAllText(newPath, string.Join('\n', Enumerable.Repeat(hardToFindContent, numTimesContent)));
            enumeratingTimeOutTestsFiles.Add(newPath);
        }
        factory = new LogOptions().GetLoggerFactory();
    }
    
    [Fact]
    public void Overrides()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { fourWindowsOne2000Path },
            CustomRulesPath = overridesTestRulePath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(4, result.Metadata.TotalMatchesCount);
        Assert.Equal(2, result.Metadata.UniqueMatchesCount);

        options = new AnalyzeOptions
        {
            SourcePath = new string[1] { fourWindowsOne2000Path },
            CustomRulesPath = overridesWithoutOverrideTestRulePath,
            IgnoreDefaultRules = true
        };

        command = new AnalyzeCommand(options, factory);
        result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        // This has one additional result for the same file because the match is not being overridden.
        Assert.Equal(5, result.Metadata.TotalMatchesCount);
        Assert.Equal(2, result.Metadata.UniqueMatchesCount);
    }

    [Fact]
    public async Task OverridesAsync()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { fourWindowsOne2000Path },
            CustomRulesPath = overridesTestRulePath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = await command.GetResultAsync();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(4, result.Metadata.TotalMatchesCount);
        Assert.Equal(2, result.Metadata.UniqueMatchesCount);

        options = new AnalyzeOptions
        {
            SourcePath = new string[1] { fourWindowsOne2000Path },
            CustomRulesPath = overridesWithoutOverrideTestRulePath,
            IgnoreDefaultRules = true
        };

        command = new AnalyzeCommand(options, factory);
        result = await command.GetResultAsync();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        // This has one additional result for the same file because the match is not being overridden.
        Assert.Equal(5, result.Metadata.TotalMatchesCount);
        Assert.Equal(2, result.Metadata.UniqueMatchesCount);
    }

    private void DeleteTestFiles(IEnumerable<string> pathsToDelete)
    {
        foreach (var path in pathsToDelete) File.Delete(path);
    }

    /// <summary>
    ///     Checks that the enumeration timeout works
    /// </summary>
    [Theory]
    [CombinatorialData]
    public void EnumeratingTimeoutTimesOut([CombinatorialValues(true, false)]bool singleThread, [CombinatorialValues(true, false)]bool noShowProgress)
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new[] { enumeratingTimeOutTestsFiles[0] },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            EnumeratingTimeout = 1,
            SingleThread = singleThread,
            NoShowProgress = noShowProgress
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();

        Assert.NotEqual(numTimeOutFiles, result.Metadata.TotalFiles);
    }

    /// <summary>
    ///     Checks that the overall processing timeout works
    /// </summary>
    [Theory]
    [CombinatorialData]
    public void ProcessingTimeoutTimesOut(bool singleThread, bool noShowProgress)
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new[] { enumeratingTimeOutTestsFiles[0] },
            CustomRulesPath = heavyRulePath,
            IgnoreDefaultRules = true,
            ProcessingTimeOut = 1,
            SingleThread = singleThread,
            NoShowProgress = noShowProgress
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();

        Assert.Equal(1, result.Metadata.FilesTimeOutSkipped);
    }

    /// <summary>
    ///     Checks that the individual file timeout times out
    /// </summary>
    [Theory]
    [CombinatorialData]
    public void FileTimeoutTimesOut(bool singleThread, bool noShowProgress)
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new[] { enumeratingTimeOutTestsFiles[0] },
            CustomRulesPath = heavyRulePath,
            IgnoreDefaultRules = true,
            FileTimeOut = 1,
            SingleThread = singleThread,
            NoShowProgress = noShowProgress
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();

        Assert.Equal(1, result.Metadata.FilesTimedOut);
    }

    /// <summary>
    ///     Checks that the parameter to restrict the maximum number of matches for a tag works
    /// </summary>
    /// <param name="MaxNumberOfMatchesParameter"></param>
    /// <param name="ActualExpectedNumberOfMatches"></param>
    [InlineData(0, 4)] // 0 is the default value and indicates disabled. So we should find all 4.
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(9999, 4)] // 9999 is larger than 4, but there are only 4 to find.
    [Theory]
    public void MaxNumMatches(int MaxNumberOfMatchesParameter, int ActualExpectedNumberOfMatches)
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            MaxNumMatchesPerTag = MaxNumberOfMatchesParameter,
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();

        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(ActualExpectedNumberOfMatches,
            result.Metadata.Matches.Count(x => x.Tags?.Contains("Test.Tags.Windows") ?? false));
    }

    /// <summary>
    ///     Checks that the parameter to restrict the maximum number of matches for a tag works using asynchronous.
    /// </summary>
    /// <param name="MaxNumberOfMatchesParameter"></param>
    /// <param name="ActualExpectedNumberOfMatches"></param>
    [InlineData(0, 4)] // 0 is the default value and indicates disabled. So we should find all 4.
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(9999, 4)] // 9999 is larger than 4, but there are only 4 to find.
    [Theory]
    public async Task MaxNumMatchesAsync(int MaxNumberOfMatchesParameter, int ActualExpectedNumberOfMatches)
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            MaxNumMatchesPerTag = MaxNumberOfMatchesParameter,
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = await command.GetResultAsync(new CancellationToken());

        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(ActualExpectedNumberOfMatches,
            result.Metadata.Matches.Count(x => x.Tags?.Contains("Test.Tags.Windows") ?? false));
    }

    /// <summary>
    ///     Ensure that an exception is thrown when a source file does not exist
    /// </summary>
    [Fact]
    public void DetectMissingSourcePath()
    {
        // This will cause an exception when we try to scan a path to a non-extant file.
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { Path.Combine(testFilePath, ".not.a.real.file") }
        };

        Assert.Throws<OpException>(() => new AnalyzeCommand(options));
    }

    /// <summary>
    ///     Ensure that an exception is thrown when the rules file which is specified does not exist.
    /// </summary>
    [Fact]
    public void DetectMissingRulesPath()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            CustomRulesPath = Path.Combine(testRulesPath, ".not.a.real.file"),
            IgnoreDefaultRules = true
        };

        Assert.Throws<OpException>(() => new AnalyzeCommand(options));
    }

    /// <summary>
    ///     Ensure that an exception is thrown when no rules are specified and default rules are disabled.
    /// </summary>
    [Fact]
    public void NoRulesSpecified()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            IgnoreDefaultRules = true
        };

        Assert.Throws<OpException>(() => new AnalyzeCommand(options));
    }

    /// <summary>
    ///     Test that the exclusion globs work
    /// </summary>
    [Fact]
    public void TestExclusionFilter()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            FilePathExclusions = new[] { "**/FourWindowsOneLinux.js" },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.NoMatches, result.ResultCode);
        Assert.Equal(0, result.Metadata.TotalMatchesCount);
    }

    [Fact]
    public void TestNoMatchesOkay()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            FilePathExclusions = new[] { "**/FourWindowsOneLinux.js" },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            SuccessErrorCodeOnNoMatches = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(0, result.Metadata.TotalMatchesCount);
    }

    [Fact]
    public async Task TestNoMatchesOkayAsync()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            FilePathExclusions = new[] { "**/FourWindowsOneLinux.js" },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            SuccessErrorCodeOnNoMatches = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = await command.GetResultAsync();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(0, result.Metadata.TotalMatchesCount);
    }

    [Fact]
    public async Task ExpectedResultCountsAsync()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = await command.GetResultAsync();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(5, result.Metadata.TotalMatchesCount);
        Assert.Equal(2, result.Metadata.UniqueMatchesCount);
    }

    [Theory]
    [InlineData(true, 0, 0)]
    [InlineData(false, 2, 5)]
    public void ExpectedResultCounts(bool disableArchive, int expectedUniqueCount, int expectedCount)
    {
        AnalyzeOptions options = new()
        {
            // This file is in the repo under test data and should be placed in the working directory by the build
            SourcePath = new string[1] { Path.Combine("TestData", "FourWindowsOneLinux.zip") },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            DisableCrawlArchives = disableArchive
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(disableArchive ? AnalyzeResult.ExitCode.NoMatches : AnalyzeResult.ExitCode.Success,
            result.ResultCode);
        Assert.Equal(expectedCount, result.Metadata.TotalMatchesCount);
        Assert.Equal(expectedUniqueCount, result.Metadata.UniqueMatchesCount);
    }

    [Fact]
    public void ExpectedResultCountsNoArgs()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(5, result.Metadata.TotalMatchesCount);
        Assert.Equal(2, result.Metadata.UniqueMatchesCount);
    }

    [InlineData("afile.js", AnalyzeResult.ExitCode.Success, 4, 1)]
    [InlineData("afile.js.cs", AnalyzeResult.ExitCode.NoMatches, 0, 0)]
    [InlineData("adifferentfile.js", AnalyzeResult.ExitCode.Success, 1, 1)]
    [Theory]
    public void AppliesToFileName(string testFileName, AnalyzeResult.ExitCode expectedExitCode,
        int expectedTotalMatches, int expectedUniqueMatches)
    {
        var innerFilePath = Path.Combine("TestData", "TestAnalyzeCmd", "Samples", testFileName);
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { innerFilePath },
            CustomRulesPath = findWindowsWithFileRegex,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(expectedExitCode, result.ResultCode);
        Assert.Equal(expectedTotalMatches, result.Metadata.TotalMatchesCount);
        Assert.Equal(expectedUniqueMatches, result.Metadata.UniqueMatchesCount);
    }


    [Fact]
    public void ScanUnknownFileTypes()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { unknownFileTypePath },
            CustomRulesPath = testRulesPath,
            ScanUnknownTypes = true,
            IgnoreDefaultRules = true
        };
        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(1, result.Metadata.TotalFiles);
        Assert.Equal(0, result.Metadata.FilesSkipped);
        Assert.Equal(1, result.Metadata.FilesAffected);
        Assert.Equal(5, result.Metadata.TotalMatchesCount);
        Assert.Equal(2, result.Metadata.UniqueMatchesCount);
    }

    [Fact]
    public void MultiPath_Pass()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[2]
            {
                unknownFileTypePath,
                testFilePath
            },
            CustomRulesPath = testRulesPath,
            ScanUnknownTypes = true,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(5 * 2, result.Metadata.TotalMatchesCount);
        Assert.Equal(2, result.Metadata.UniqueMatchesCount);
    }

    [Fact]
    public void TagsOnly()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            TagsOnly = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();

        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Empty(result.Metadata.Matches);
        Assert.Equal(2, result.Metadata.UniqueTags.Count);
    }

    [Fact]
    public void SingleVsMultiThread()
    {
        List<string> testFiles = new();
        Directory.CreateDirectory("TestOutput");
        var iterations = 100;
        var content = File.ReadAllText(testFilePath);
        for (var i = 0; i < iterations; i++)
        {
            var testFileName = Path.Combine("TestOutput",
                $"SingleVsMultiThread-TestFile-{i}.js");
            File.WriteAllText(testFileName, content);
            testFiles.Add(testFileName);
        }

        AnalyzeOptions optionsSingle = new()
        {
            SourcePath = testFiles,
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            SingleThread = true
        };

        AnalyzeOptions optionsMulti = new()
        {
            SourcePath = testFiles,
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            SingleThread = false
        };

        AnalyzeCommand commandSingle = new(optionsSingle, factory);
        var resultSingle = commandSingle.GetResult();

        AnalyzeCommand commandMulti = new(optionsMulti, factory);
        var resultMulti = commandMulti.GetResult();

        Assert.Equal(AnalyzeResult.ExitCode.Success, resultSingle.ResultCode);
        Assert.Equal(AnalyzeResult.ExitCode.Success, resultMulti.ResultCode);
        Assert.Equal(5 * iterations, resultSingle.Metadata.TotalMatchesCount);
        Assert.Equal(5 * iterations, resultMulti.Metadata.TotalMatchesCount);
        Assert.True(resultSingle.Metadata.Matches.All(x =>
            resultMulti.Metadata.Matches.Any(y =>
                y.Tags?.All(z => x.Tags?.All(w => w.Contains(z)) ?? false) ?? false)));
    }

    [InlineData(new[] { Severity.Moderate }, 1)]
    [InlineData(new[] { Severity.Important }, 4)]
    [InlineData(new[] { Severity.Important | Severity.Moderate }, 5)]
    [Theory]
    public void SeverityFilters(Severity[] severityFilter, int ActualExpectedNumberOfMatches)
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            CustomRulesPath = testRulesPath,
            SeverityFilters = severityFilter,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();

        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(ActualExpectedNumberOfMatches, result.Metadata.Matches.Count);
    }

    [InlineData(new[] { Confidence.High }, 1)]
    [InlineData(new[] { Confidence.Medium }, 4)]
    [InlineData(new[] { Confidence.Medium | Confidence.High }, 5)]
    [Theory]
    public void ConfidenceFilters(Confidence[] confidenceFilter, int ActualExpectedNumberOfMatches)
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            CustomRulesPath = testRulesPath,
            ConfidenceFilters = confidenceFilter,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();

        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(ActualExpectedNumberOfMatches, result.Metadata.Matches.Count);
    }

    [Fact]
    public void TagsInBuildFiles()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePathWithJsonExt },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            AllowAllTagsInBuildFiles = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();

        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(5, result.Metadata.Matches.Count);
        Assert.Equal(2, result.Metadata.UniqueMatchesCount);

        AnalyzeOptions dontAllowAllTagsOptions = new()
        {
            SourcePath = new string[1] { testFilePathWithJsonExt },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            AllowAllTagsInBuildFiles = false
        };

        AnalyzeCommand dontAllowAllTagsCommand = new(dontAllowAllTagsOptions);
        var dontAllowAllTagsResult = dontAllowAllTagsCommand.GetResult();

        Assert.Equal(AnalyzeResult.ExitCode.NoMatches, dontAllowAllTagsResult.ResultCode);
        Assert.Empty(dontAllowAllTagsResult.Metadata.Matches);
        Assert.Empty(dontAllowAllTagsResult.Metadata.Matches);
    }

    [InlineData(1, 3)]
    [InlineData(2, 5)]
    [InlineData(3, 5)]
    [InlineData(50, 5)]
    [InlineData(0, 0)]
    [Theory]
    public void ContextLines(int numLinesContextArgument, int expectedNewLinesInResult)
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            ContextLines = numLinesContextArgument
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();

        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        var linuxResult = result.Metadata.Matches.Where(x => x.RuleId == "AI_TEST_LINUX");
        Assert.Equal(expectedNewLinesInResult, linuxResult.First().Excerpt.Count(x => x == '\n'));
    }

    [Fact]
    public void FileMetadata()
    {
        AnalyzeOptions optionsWithoutMetadata = new()
        {
            SourcePath = new string[1] { testFilePath },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            NoFileMetadata = true
        };

        AnalyzeOptions optionsWithMetadata = new()
        {
            SourcePath = new string[1] { testFilePath },
            CustomRulesPath = testRulesPath,
            IgnoreDefaultRules = true,
            NoFileMetadata = false
        };

        AnalyzeCommand commandWithoutMetadata = new(optionsWithoutMetadata, factory);
        var resultWithoutMetadata = commandWithoutMetadata.GetResult();

        AnalyzeCommand commandWithMetadata = new(optionsWithMetadata, factory);
        var resultWithMetadata = commandWithMetadata.GetResult();

        Assert.Equal(1, resultWithMetadata.Metadata.TotalFiles);
        Assert.Equal(0, resultWithoutMetadata.Metadata.TotalFiles);
    }

    /// <summary>
    ///     Test that the applies_to parameter allows the specified types
    /// </summary>
    [Fact]
    public void TestAppliesTo()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            CustomRulesPath = appliesToTestRulePath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(4, result.Metadata.TotalMatchesCount);
    }

    /// <summary>
    ///     Test that the depends_on rule parameter properly limits matches one way
    /// </summary>
    [Fact]
    public void TestDependsOnOneWay()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[] { testFilePath, fourWindowsOne2000Path },
            CustomRulesPath = dependsOnOneWayRulePath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(2, result.Metadata.TotalMatchesCount);
        Assert.Contains("Dependee", result.Metadata.UniqueTags);
        Assert.Contains("Dependant", result.Metadata.UniqueTags);
    }

    /// <summary>
    ///     Test that the depends_on rule parameter properly limits matches one way
    /// </summary>
    [Fact]
    public void TestDependsOnChain()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[] { justAPath, justBPath, justCPath },
            CustomRulesPath = dependsOnChainRulePath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(3, result.Metadata.TotalMatchesCount);
        Assert.Contains("Category.A", result.Metadata.UniqueTags);
        Assert.Contains("Category.B", result.Metadata.UniqueTags);
        Assert.Contains("Category.C", result.Metadata.UniqueTags);

        options = new()
        {
            SourcePath = new string[] { justBPath, justCPath },
            CustomRulesPath = dependsOnChainRulePath,
            IgnoreDefaultRules = true
        };

        command = new(options, factory);
        result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.NoMatches, result.ResultCode);
        Assert.Equal(0, result.Metadata.TotalMatchesCount);
        Assert.DoesNotContain("Category.A", result.Metadata.UniqueTags);
        Assert.DoesNotContain("Category.B", result.Metadata.UniqueTags);
        Assert.DoesNotContain("Category.C", result.Metadata.UniqueTags);

        options = new()
        {
            SourcePath = new string[] { justAPath, justCPath },
            CustomRulesPath = dependsOnChainRulePath,
            IgnoreDefaultRules = true
        };

        command = new(options, factory);
        result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(1, result.Metadata.TotalMatchesCount);
        Assert.Contains("Category.A", result.Metadata.UniqueTags);
        Assert.DoesNotContain("Category.B", result.Metadata.UniqueTags);
        Assert.DoesNotContain("Category.C", result.Metadata.UniqueTags);

        options = new()
        {
            SourcePath = new string[] { justAPath, justBPath },
            CustomRulesPath = dependsOnChainRulePath,
            IgnoreDefaultRules = true
        };

        command = new(options, factory);
        result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(2, result.Metadata.TotalMatchesCount);
        Assert.Contains("Category.A", result.Metadata.UniqueTags);
        Assert.Contains("Category.B", result.Metadata.UniqueTags);
        Assert.DoesNotContain("Category.C", result.Metadata.UniqueTags);
    }

    /// <summary>
    ///     Test that the depends_on rule parameter properly limits matches one way
    /// </summary>
    [Fact]
    public void TestDependsOnOneWayWithoutDependee()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[] { testFilePath },
            CustomRulesPath = dependsOnOneWayRulePath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(1, result.Metadata.TotalMatchesCount);
        Assert.Contains("Dependee", result.Metadata.UniqueTags);
        Assert.DoesNotContain("Dependant", result.Metadata.UniqueTags);
    }

    /// <summary>
    ///     Test that the depends_on rule parameter properly limits matches two ways
    /// </summary>
    [Fact]
    public void TestDependsOnTwoWay()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[] { testFilePath, fourWindowsOne2000Path },
            CustomRulesPath = dependsOnTwoWayRulePath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);
        Assert.Equal(2, result.Metadata.TotalMatchesCount);
        Assert.Contains("RuleOne", result.Metadata.UniqueTags);
        Assert.Contains("RuleTwo", result.Metadata.UniqueTags);
    }

    /// <summary>
    ///     Test that the depends_on rule parameter properly limits matches two ways
    /// </summary>
    [Fact]
    public void TestDependsOnTwoWayWithoutDependee()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[] { testFilePath },
            CustomRulesPath = dependsOnTwoWayRulePath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.NoMatches, result.ResultCode);
        Assert.Equal(0, result.Metadata.TotalMatchesCount);
        Assert.DoesNotContain("RuleOne", result.Metadata.UniqueTags);
        Assert.DoesNotContain("RuleTwo", result.Metadata.UniqueTags);
    }

    [Fact]
    public void TestDependsOnTwoWayWithoutDependant()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[] { fourWindowsOne2000Path },
            CustomRulesPath = dependsOnTwoWayRulePath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.NoMatches, result.ResultCode);
        Assert.Equal(0, result.Metadata.TotalMatchesCount);
        Assert.DoesNotContain("RuleOne", result.Metadata.UniqueTags);
        Assert.DoesNotContain("RuleTwo", result.Metadata.UniqueTags);
    }

    /// <summary>
    ///     Test that the does_not_apply_to parameter excludes the specified types
    /// </summary>
    [Fact]
    public void TestDoesNotApplyTo()
    {
        AnalyzeOptions options = new()
        {
            SourcePath = new string[1] { testFilePath },
            CustomRulesPath = doesNotApplyToTestRulePath,
            IgnoreDefaultRules = true
        };

        AnalyzeCommand command = new(options, factory);
        var result = command.GetResult();
        Assert.Equal(AnalyzeResult.ExitCode.NoMatches, result.ResultCode);
        Assert.Equal(0, result.Metadata.TotalMatchesCount);
    }
}