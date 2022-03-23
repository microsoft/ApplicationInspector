using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInspector.CLI;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.Commands
{
    /// <summary>
    /// Test class for the Analyze Command
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestAnalyzeCmd
    {
        /*
TODO, these parameters are not currently tested:
FileTimeout
ProcessingTimeout
        */

        private string testFilePath = string.Empty;
        private string testRulesPath = string.Empty;

        private LogOptions logOptions = new();
        private ILoggerFactory factory = new NullLoggerFactory();

        [TestInitialize]
        public void InitOutput()
        {
            factory = logOptions.GetLoggerFactory();
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
            testFilePath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput),"TestFile.js");
            testRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "TestRules.json");
            File.WriteAllText(testFilePath, fourWindowsOneLinux);
            File.WriteAllText(testRulesPath, findWindows);
        }

        [TestCleanup]
        public void CleanUp()
        {
            Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
        }

        // These simple test rules rules look for the string "windows" and "linux"
        string findWindows = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""severity"": ""Important"",
    ""patterns"": [
      {
                ""confidence"": ""Medium"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""windows"",
        ""type"": ""String"",
      }
    ]
},
{
    ""name"": ""Platform: Linux"",
    ""id"": ""AI_TEST_LINUX"",
    ""description"": ""This rule checks for the string 'linux'"",
    ""tags"": [
      ""Test.Tags.Linux""
    ],
    ""severity"": ""Moderate"",
    ""patterns"": [
      {
                ""confidence"": ""High"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""linux"",
        ""type"": ""String"",
      }
    ]
}
]";
        string findWindowsWithOverride = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""severity"": ""Important"",
    ""patterns"": [
      {
                ""confidence"": ""Medium"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""windows"",
        ""type"": ""String"",
      }
    ]
},
{
    ""name"": ""Platform: Linux"",
    ""id"": ""AI_TEST_LINUX"",
    ""description"": ""This rule checks for the string 'windows2000'"",
    ""tags"": [
      ""Test.Tags.Win2000""
    ],
    ""severity"": ""Moderate"",
    ""overrides"": [ ""AI_TEST_WINDOWS"" ],
    ""patterns"": [
      {
                ""confidence"": ""High"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""windows 2000"",
        ""type"": ""String"",
      }
    ]
}
]";
        string findWindowsWithOverrideRuleWithoutOverride = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""severity"": ""Important"",
    ""patterns"": [
      {
                ""confidence"": ""Medium"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""windows"",
        ""type"": ""String"",
      }
    ]
},
{
    ""name"": ""Platform: Linux"",
    ""id"": ""AI_TEST_LINUX"",
    ""description"": ""This rule checks for the string 'windows2000'"",
    ""tags"": [
      ""Test.Tags.Win2000""
    ],
    ""severity"": ""Moderate"",
    ""patterns"": [
      {
                ""confidence"": ""High"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""windows 2000"",
        ""type"": ""String"",
      }
    ]
}
]";
        string findWindowsWithFileRegex = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""severity"": ""Important"",
    ""applies_to_file_regex"": [""afile.js""],
    ""patterns"": [
      {
                ""confidence"": ""Medium"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""windows"",
        ""type"": ""String"",
      }
    ]
},
{
    ""name"": ""Platform: Linux"",
    ""id"": ""AI_TEST_LINUX"",
    ""description"": ""This rule checks for the string 'linux'"",
    ""tags"": [
      ""Test.Tags.Linux""
    ],
    ""severity"": ""Moderate"",
    ""applies_to_file_regex"": [""adifferentfile.js""],
    ""patterns"": [
      {
                ""confidence"": ""High"",
        ""modifiers"": [
          ""i""
        ],
        ""pattern"": ""linux"",
        ""type"": ""String"",
      }
    ]
}
]";
        // This string contains windows four times and linux once.
        string fourWindowsOneLinux =
@"windows
windows
linux
windows
windows
";

        // This string contains windows four times and linux once.
        string threeWindowsOneWindows2000 =
@"windows
windows
windows 2000
windows
";
        [DataTestMethod]
        public void Overrides()
        {
            var overridesTestRulePath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "OverrideTestRule.json");
            var overridesWithoutOverrideTestRulePath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "OverrideTestRuleWithoutOverride.json");
            var fourWindowsOne2000Path = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "FourWindowsOne2000.cs");
            
            File.WriteAllText(fourWindowsOne2000Path, threeWindowsOneWindows2000);
            File.WriteAllText(overridesTestRulePath, findWindowsWithOverride);
            File.WriteAllText(overridesWithoutOverrideTestRulePath, findWindowsWithOverrideRuleWithoutOverride);

            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { fourWindowsOne2000Path },
                CustomRulesPath = overridesTestRulePath,
                IgnoreDefaultRules = true
            };

            AnalyzeCommand command = new(options, factory);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(4, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(2, result.Metadata.UniqueMatchesCount);

            options = new()
            {
                SourcePath = new string[1] { fourWindowsOne2000Path },
                CustomRulesPath = overridesWithoutOverrideTestRulePath,
                IgnoreDefaultRules = true
            };

            command = new(options, factory);
            result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            // This has one additional result for the same file because the match is not being overridden.
            Assert.AreEqual(5, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(2, result.Metadata.UniqueMatchesCount);
        }

        [DataTestMethod]
        public async Task OverridesAsync()
        {
            var overridesTestRulePath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "OverrideTestRule.json");
            var overridesWithoutOverrideTestRulePath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "OverrideTestRuleWithoutOverride.json");
            var fourWindowsOne2000Path = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "FourWindowsOne2000.cs");

            File.WriteAllText(fourWindowsOne2000Path, threeWindowsOneWindows2000);
            File.WriteAllText(overridesTestRulePath, findWindowsWithOverride);
            File.WriteAllText(overridesWithoutOverrideTestRulePath, findWindowsWithOverrideRuleWithoutOverride);

            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { fourWindowsOne2000Path },
                CustomRulesPath = overridesTestRulePath,
                IgnoreDefaultRules = true
            };

            AnalyzeCommand command = new(options, factory);
            AnalyzeResult result = await command.GetResultAsync();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(4, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(2, result.Metadata.UniqueMatchesCount);

            options = new()
            {
                SourcePath = new string[1] { fourWindowsOne2000Path },
                CustomRulesPath = overridesWithoutOverrideTestRulePath,
                IgnoreDefaultRules = true
            };

            command = new(options, factory);
            result = await command.GetResultAsync();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            // This has one additional result for the same file because the match is not being overridden.
            Assert.AreEqual(5, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(2, result.Metadata.UniqueMatchesCount);
        }

        /// <summary>
        /// Checks that the parameter to restrict the maximum number of matches for a tag works
        /// </summary>
        /// <param name="MaxNumberOfMatchesParameter"></param>
        /// <param name="ActualExpectedNumberOfMatches"></param>
        [DataRow(0, 4)] // 0 is the default value and indicates disabled. So we should find all 4.
        [DataRow(1, 1)]
        [DataRow(2, 2)]
        [DataRow(3, 3)]
        [DataRow(4, 4)]
        [DataRow(9999, 4)] // 9999 is larger than 4, but there are only 4 to find.
        [DataTestMethod]
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
            AnalyzeResult result = command.GetResult();

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(ActualExpectedNumberOfMatches, result.Metadata.Matches.Count(x => x.Tags?.Contains("Test.Tags.Windows") ?? false));
        }

        /// <summary>
        /// Checks that the parameter to restrict the maximum number of matches for a tag works using asynchronous.
        /// </summary>
        /// <param name="MaxNumberOfMatchesParameter"></param>
        /// <param name="ActualExpectedNumberOfMatches"></param>
        [DataRow(0, 4)] // 0 is the default value and indicates disabled. So we should find all 4.
        [DataRow(1, 1)]
        [DataRow(2, 2)]
        [DataRow(3, 3)]
        [DataRow(4, 4)]
        [DataRow(9999, 4)] // 9999 is larger than 4, but there are only 4 to find.
        [DataTestMethod]
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
            AnalyzeResult result = await command.GetResultAsync(new CancellationToken());

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(ActualExpectedNumberOfMatches, result.Metadata.Matches.Count(x => x.Tags?.Contains("Test.Tags.Windows") ?? false));
        }

        /// <summary>
        /// Ensure that an exception is thrown when a source file does not exist
        /// </summary>
        [TestMethod]
        public void DetectMissingSourcePath()
        {
            // This will cause an exception when we try to scan a path to a non-extant file.
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(testFilePath, ".not.a.real.file") },
            };

            Assert.ThrowsException<OpException>(() => new AnalyzeCommand(options));
        }

        /// <summary>
        /// Ensure that an exception is thrown when the rules file which is specified does not exist.
        /// </summary>
        [TestMethod]
        public void DetectMissingRulesPath()
        {
            // We need to ensure the test file exists, it doesn't matter what is in it.
            File.WriteAllText(testFilePath, fourWindowsOneLinux);
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { testFilePath },
                CustomRulesPath = Path.Combine(testRulesPath, ".not.a.real.file"),
                IgnoreDefaultRules = true
            };

            Assert.ThrowsException<OpException>(() => new AnalyzeCommand(options));
        }

        /// <summary>
        /// Ensure that an exception is thrown when no rules are specified and default rules are disabled.
        /// </summary>
        [TestMethod]
        public void NoRulesSpecified()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { testFilePath },
                IgnoreDefaultRules = true,
            };

            Assert.ThrowsException<OpException>(() => new AnalyzeCommand(options));
        }

        /// <summary>
        /// Test that the exclusion globs work
        /// </summary>
        [TestMethod]
        public void TestExclusionFilter()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { testFilePath },
                FilePathExclusions = new string[] { "**/TestFile.js" },
                CustomRulesPath = testRulesPath,
                IgnoreDefaultRules = true
            };

            AnalyzeCommand command = new(options, factory);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.NoMatches, result.ResultCode);
            Assert.AreEqual(0, result.Metadata.TotalMatchesCount);
        }

        [TestMethod]
        public async Task ExpectedResultCountsAsync()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { testFilePath },
                CustomRulesPath = testRulesPath,
                IgnoreDefaultRules = true
            };

            AnalyzeCommand command = new(options, factory);
            AnalyzeResult result = await command.GetResultAsync();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(5, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(2, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void ExpectedResultCounts()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { testFilePath },
                CustomRulesPath = testRulesPath,
                IgnoreDefaultRules = true
            };

            AnalyzeCommand command = new(options, factory);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(5, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(2, result.Metadata.UniqueMatchesCount);
        }

        [DataRow("afile.js", AnalyzeResult.ExitCode.Success, 4, 1)]
        [DataRow("adifferentfile.js", AnalyzeResult.ExitCode.Success, 1, 1)]
        [DataTestMethod]
        public void AppliesToFileName(string testFileName, AnalyzeResult.ExitCode expectedExitCode, int expectedTotalMatches, int expectedUniqueMatches)
        {
            var appliesToTestRulePath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "AppliesToFileNameTestRule.json");
            var appliesToTestFilePath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), testFileName);
            File.WriteAllText(appliesToTestFilePath, fourWindowsOneLinux);
            File.WriteAllText(appliesToTestRulePath, findWindowsWithFileRegex);
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { appliesToTestFilePath },
                CustomRulesPath = appliesToTestRulePath,
                IgnoreDefaultRules = true
            };

            AnalyzeCommand command = new(options, factory);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(expectedExitCode, result.ResultCode);
            Assert.AreEqual(expectedTotalMatches, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(expectedUniqueMatches, result.Metadata.UniqueMatchesCount);
        }


        [TestMethod]
        public void ScanUnknownFileTypes()
        {
            string scanPath = Path.GetTempFileName();

            File.WriteAllText(scanPath, fourWindowsOneLinux);

            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { scanPath },
                CustomRulesPath = testRulesPath,
                ScanUnknownTypes = true,
                IgnoreDefaultRules= true
            };
            AnalyzeCommand command = new(options, factory);
            AnalyzeResult result = command.GetResult();

            File.Delete(scanPath);

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(1, result.Metadata.TotalFiles);
            Assert.AreEqual(0, result.Metadata.FilesSkipped);
            Assert.AreEqual(1, result.Metadata.FilesAffected);
            Assert.AreEqual(5, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(2, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void MultiPath_Pass()
        {
            string scanPath = Path.GetTempFileName();

            File.WriteAllText(scanPath, fourWindowsOneLinux);

            AnalyzeOptions options = new()
            {
                SourcePath = new string[2]
                {
                    scanPath,
                    testFilePath
                },
                CustomRulesPath = testRulesPath,
                ScanUnknownTypes = true,
                IgnoreDefaultRules = true
            };

            AnalyzeCommand command = new(options, factory);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(5 * 2, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(2, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
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
            AnalyzeResult result = command.GetResult();

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(0, result.Metadata.Matches.Count);
            Assert.AreEqual(2, result.Metadata.UniqueTags.Count);
        }

        [TestMethod]
        public void SingleVsMultiThread()
        {
            List<string> testFiles = new();
            int iterations = 1000;
            for (int i = 0; i < iterations; i++)
            {
                string innerFileName = Path.GetTempFileName();
                File.WriteAllText(innerFileName, fourWindowsOneLinux);
                testFiles.Add(innerFileName);
            }

            AnalyzeOptions optionsSingle = new()
            {
                SourcePath = testFiles,
                CustomRulesPath = testRulesPath,
                IgnoreDefaultRules = true,
                SingleThread = true,
                ScanUnknownTypes = true // Temp files are named .tmp so we need to scan unknown types.
            };

            AnalyzeOptions optionsMulti = new()
            {
                SourcePath = testFiles,
                CustomRulesPath = testRulesPath,
                IgnoreDefaultRules = true,
                SingleThread = false,
                ScanUnknownTypes = true
            };

            AnalyzeCommand commandSingle = new(optionsSingle, factory);
            AnalyzeResult resultSingle = commandSingle.GetResult();

            AnalyzeCommand commandMulti = new(optionsMulti, factory);
            AnalyzeResult resultMulti = commandMulti.GetResult();

            foreach(var testFile in testFiles)
            {
                File.Delete(testFile);
            }

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, resultSingle.ResultCode);
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, resultMulti.ResultCode);
            Assert.AreEqual(5 * iterations, resultSingle.Metadata.TotalMatchesCount);
            Assert.AreEqual(5 * iterations, resultMulti.Metadata.TotalMatchesCount);
            Assert.IsTrue(resultSingle.Metadata.Matches.All(x => resultMulti.Metadata.Matches.Any(y => y.Tags?.All(z => x.Tags?.All(w => w.Contains(z)) ?? false) ?? false)));
        }

        [DataRow(new Severity[] { Severity.Moderate }, 1)]
        [DataRow(new Severity[] { Severity.Important }, 4)]
        [DataRow(new Severity[] { Severity.Important | Severity.Moderate }, 5)]

        [DataTestMethod]
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
            AnalyzeResult result = command.GetResult();

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(ActualExpectedNumberOfMatches, result.Metadata.Matches.Count);
        }

        [DataRow(new Confidence[] { Confidence.High }, 1)]
        [DataRow(new Confidence[] { Confidence.Medium }, 4)]
        [DataRow(new Confidence[] { Confidence.Medium | Confidence.High }, 5)]
        [DataTestMethod]
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
            AnalyzeResult result = command.GetResult();

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(ActualExpectedNumberOfMatches, result.Metadata.Matches.Count);
        }

        [TestMethod]
        public void TagsInBuildFiles()
        {
            string innerTestFilePath = $"{Path.GetTempFileName()}.json"; // JSON is considered a build file, it does not have code/comment sections.
            File.WriteAllText(innerTestFilePath, fourWindowsOneLinux);

            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { innerTestFilePath },
                CustomRulesPath = testRulesPath,
                IgnoreDefaultRules = true,
                AllowAllTagsInBuildFiles = true
            };

            AnalyzeCommand command = new(options, factory);
            AnalyzeResult result = command.GetResult();

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(5, result.Metadata.Matches.Count);
            Assert.AreEqual(2, result.Metadata.UniqueMatchesCount);

            AnalyzeOptions dontAllowAllTagsOptions = new()
            {
                SourcePath = new string[1] { innerTestFilePath },
                CustomRulesPath = testRulesPath,
                IgnoreDefaultRules = true,
                AllowAllTagsInBuildFiles = false
            };

            AnalyzeCommand dontAllowAllTagsCommand = new(dontAllowAllTagsOptions);
            AnalyzeResult dontAllowAllTagsResult = dontAllowAllTagsCommand.GetResult();

            File.Delete(innerTestFilePath);

            Assert.AreEqual(AnalyzeResult.ExitCode.NoMatches, dontAllowAllTagsResult.ResultCode);
            Assert.AreEqual(0, dontAllowAllTagsResult.Metadata.Matches.Count);
            Assert.AreEqual(0, dontAllowAllTagsResult.Metadata.Matches.Count);
        }

        [DataRow(1, 3)]
        [DataRow(2, 5)]
        [DataRow(3, 5)]
        [DataRow(50, 5)]
        [DataRow(0, 0)]
        [DataTestMethod]
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
            AnalyzeResult result = command.GetResult();

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            var linuxResult = result.Metadata.Matches.Where(x => x.RuleId == "AI_TEST_LINUX");
            Assert.AreEqual(expectedNewLinesInResult, linuxResult.First().Excerpt.Count(x => x == '\n'));
        }

        [TestMethod]
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
            AnalyzeResult resultWithoutMetadata = commandWithoutMetadata.GetResult();

            AnalyzeCommand commandWithMetadata = new(optionsWithMetadata, factory);
            AnalyzeResult resultWithMetadata = commandWithMetadata.GetResult();

            Assert.AreEqual(1, resultWithMetadata.Metadata.TotalFiles);
            Assert.AreEqual(0, resultWithoutMetadata.Metadata.TotalFiles);
        }

    }
}