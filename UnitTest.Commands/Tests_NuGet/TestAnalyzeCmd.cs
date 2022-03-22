﻿namespace ApplicationInspector.Unitprocess.Commands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.CLI;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Test class for Analyze Commands
    /// Each method really needs to be complete i.e. options and command objects created and checked for exceptions etc. based on inputs so
    /// doesn't create a set of shared objects
    /// Note: in order to avoid log reuse, include the optional parameter CloseLogOnCommandExit = true
    /// </summary>
    [TestClass]
    public class TestAnalyzeCmd
    {
        private string testFilePath = string.Empty;
        private string testRulesPath = string.Empty;

        [TestInitialize]
        public void InitOutput()
        {
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
            testFilePath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput),"TestFile.js");
            testRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "TestRules.json");
            File.WriteAllText(testFilePath, fourWindows);
            File.WriteAllText(testRulesPath, findWindows);
        }

        [TestCleanup]
        public void CleanUp()
        {
            Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
        }

        // This rule looks for the string "windows"
        string findWindows = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI_TEST_WINDOWS"",
    ""description"": ""This rule checks for the string 'windows'"",
    ""tags"": [
      ""Test.Tags.Windows""
    ],
    ""severity"": ""Moderate"",
    ""patterns"": [
      {
                ""confidence"": ""High"",
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
        // This string contains windows four times.
        string fourWindows =
@"windows
windows
windows
linux
windows";

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

            AnalyzeCommand command = new(options);
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

            AnalyzeCommand command = new(options);
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
            File.WriteAllText(testFilePath, fourWindows);
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

            AnalyzeCommand command = new(options);
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

            AnalyzeCommand command = new(options);
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

            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(5, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(2, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void ScanUnknownFileTypes()
        {
            string scanPath = Path.GetTempFileName();

            File.WriteAllText(scanPath, fourWindows);

            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { scanPath },
                CustomRulesPath = testRulesPath,
                ScanUnknownTypes = true,
                IgnoreDefaultRules= true
            };
            AnalyzeCommand command = new(options);
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

            File.WriteAllText(scanPath, fourWindows);

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

            AnalyzeCommand command = new(options);
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

            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(0, result.Metadata.Matches.Count);
            Assert.AreEqual(2, result.Metadata.UniqueTags.Count);
        }
    }
}