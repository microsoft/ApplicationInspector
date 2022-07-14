using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppInspector.Tests.Commands
{
    /// <summary>
    /// Test class for TagDiff Command
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestTagDiffCmd
    {
        private string testFileFourWindowsOneLinuxPath = string.Empty;
        private string testFileFourWindowsOneLinuxCopyPath = string.Empty;
        private string testFileFourWindowsNoLinuxPath = string.Empty;
        private string testRulesPath = string.Empty;

        private LogOptions logOptions = new();
        private ILoggerFactory loggerFactory = new NullLoggerFactory();

        [TestInitialize]
        public void InitOutput()
        {
            loggerFactory = logOptions.GetLoggerFactory();
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
            testFileFourWindowsOneLinuxPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "TestFile.js");
            File.WriteAllText(testFileFourWindowsOneLinuxPath, fourWindowsOneLinux);

            testFileFourWindowsOneLinuxCopyPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "TestFileCopy.js");
            File.WriteAllText(testFileFourWindowsOneLinuxCopyPath, fourWindowsOneLinux);

            testFileFourWindowsNoLinuxPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "TestFileNoLinux.js");
            File.WriteAllText(testFileFourWindowsNoLinuxPath, fourWindowsNoLinux);

            testRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "TestRules.json");
            File.WriteAllText(testRulesPath, findWindows);
        }

        [ClassCleanup]
        public static void CleanUp()
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
        // This string contains windows four times and linux once.
        string fourWindowsOneLinux =
@"windows
windows
linux
windows
windows
";
        string fourWindowsNoLinux =
@"windows
windows
windows
windows
";

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
            TagDiffResult result = command.GetResult();

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
            TagDiffResult result = command.GetResult();

            Assert.AreEqual(expectedExitCode, result.ResultCode);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            TagDiffOptions options = new()
            {
                SourcePath1 = new[] { $"{testFileFourWindowsOneLinuxPath}.not.a.path" },
                SourcePath2 = new[] { testFileFourWindowsOneLinuxPath },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
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

            TagDiffCommand command = new TagDiffCommand(options, loggerFactory);
            Assert.ThrowsException<OpException>(() => command.GetResult());
        }
    }
}