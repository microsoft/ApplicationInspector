namespace ApplicationInspector.Unitprocess.Commands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.CLI;
    using Microsoft.ApplicationInspector.Commands;
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
        private string testFilePath;
        private string testRulesPath;

        [TestInitialize]
        public void InitOutput()
        {
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
            testFilePath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput),"TestFile");
            testRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), "TestRules.json");
        }

        [TestCleanup]
        public void CleanUp()
        {
            File.Delete(testFilePath);
            File.Delete(testRulesPath);
            Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
        }

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [TestMethod]
        public void MaxNumMatches_Pass(int MaxNumberOfMatches)
        {
            string findWindows = @"[
{
    ""name"": ""Platform: Microsoft Windows"",
    ""id"": ""AI029200"",
    ""description"": ""Platform: Microsoft Windows"",
    ""tags"": [
      ""Platform.OS.Microsoft.WindowsStandard""
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
  }]";
            string lotsOfWindows =
@"windows
windows
windows
linux
windows";
            File.WriteAllText(testFilePath, lotsOfWindows);
            File.WriteAllText(testRulesPath, findWindows);

            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { testFilePath },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path,
                MaxNumMatchesPerTag = MaxNumberOfMatches,
                CustomRulesPath = testRulesPath,
                IgnoreDefaultRules = true,
                ScanUnknownTypes = true
            };

            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();

            Thread.Sleep(10);
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(MaxNumberOfMatches, result.Metadata.Matches.Count(x => x.Tags?.Contains("Platform.OS.Microsoft.WindowsStandard") ?? false));
        }

        [TestMethod]
        public void MaxNumMatchesDisabled_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path,
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(3, result.Metadata.Matches.Count(x => x.Tags?.Contains("Platform.OS.Microsoft.WindowsStandard") ?? false));
            exitCode = result.ResultCode;
            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public async Task MaxNumMatchesAsync_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path,
                MaxNumMatchesPerTag = 1
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            AnalyzeCommand command = new(options);
            AnalyzeResult result = await command.GetResultAsync(new CancellationTokenSource().Token);
            Assert.AreEqual(1, result.Metadata.Matches.Count(x => x.Tags?.Contains("Platform.OS.Microsoft.WindowsStandard") ?? false));
            exitCode = result.ResultCode;
            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public async Task MaxNumMatchesAsyncDisabled_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path,
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            AnalyzeCommand command = new(options);
            AnalyzeResult result = await command.GetResultAsync(new CancellationTokenSource().Token);
            Assert.AreEqual(3, result.Metadata.Matches.Count(x => x.Tags?.Contains("Platform.OS.Microsoft.WindowsStandard") ?? false));
            exitCode = result.ResultCode;
            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void BasicAnalyze_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>() //allow source under unittest path
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {

            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void BasicZipRead_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"zipped\main.zip") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {

            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"baddir\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidRulesPath_Fail()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                CustomRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"notfound.json")
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                IgnoreDefaultRules = true,
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
            };

            AnalyzeResult.ExitCode exitCode;
            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                CustomRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
            };

            AnalyzeResult.ExitCode exitCode;
            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultAndCustomRulesMatched_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                CustomRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json")
            };

            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
                Assert.IsTrue(result.Metadata.UniqueTags?.Any(v => v.Contains("Cryptography.Encryption.General")));
                Assert.IsTrue(result.Metadata.UniqueTags?.Any(v => v.Contains("Data.Custom1")));
            }
            catch (Exception)
            {
                throw;
            }
        }

        [TestMethod]
        public void ExclusionFilter_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\project\one") },
                FilePathExclusions = new string[] { "**/main.cpp" }
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public async Task ExpectedTagCountAsync_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                SingleThread = true
            };
            AnalyzeCommand command = new(options);
            AnalyzeResult result = await command.GetResultAsync(new CancellationToken());
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(11, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void ScanUnknownFileTypesTest_Include()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unknowntest") },
                FilePathExclusions = Array.Empty<string>(),
                ScanUnknownTypes = true
            };
            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(2, result.Metadata.TotalFiles);
            Assert.AreEqual(0, result.Metadata.FilesSkipped);
            Assert.AreEqual(2, result.Metadata.FilesAffected);
            Assert.AreEqual(68, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(22, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void ScanUnknownFileTypesTest_Exclude()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unknowntest") },
                FilePathExclusions = Array.Empty<string>(),
                ScanUnknownTypes = false,
                SingleThread = true
            };
            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(2, result.Metadata.TotalFiles);
            Assert.AreEqual(1, result.Metadata.FilesSkipped);
            Assert.AreEqual(1, result.Metadata.FilesAffected);
            Assert.AreEqual(36, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(21, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void MultiPath_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[] 
                { 
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp")
                },
                FilePathExclusions = Array.Empty<string>(),
            };

            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);

            AnalyzeOptions options2 = new()
            {
                SourcePath = new string[2]
                {
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp")
                },
                FilePathExclusions = Array.Empty<string>(),
            };

            AnalyzeCommand command2 = new(options2);
            AnalyzeResult result2 = command2.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result2.ResultCode);
            Assert.AreEqual(result.Metadata.TotalMatchesCount * 2, result2.Metadata.TotalMatchesCount);
            Assert.AreEqual(result.Metadata.UniqueMatchesCount, result2.Metadata.UniqueMatchesCount); 
        }


        [TestMethod]
        public void ExpectedTagCountDupsAllowed_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp") },
                FilePathExclusions = Array.Empty<string>(),
                SingleThread = true
            };

            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(11, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueMatchesCount);


            options.SingleThread = false;
            command = new AnalyzeCommand(options);
            result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(11, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void NoMatchesFound_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\empty.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };


            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();

            Assert.AreEqual(AnalyzeResult.ExitCode.NoMatches, result.ResultCode);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\empty.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };

            LogOptions logOpts = new()
            {
                DisableConsoleOutput = true
            };

            // Attempt to open output file.
            using var writer = new StreamWriter(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"consoleout.txt"));
            // Redirect standard output from the console to the output file.
            Console.SetOut(writer);

            AnalyzeCommand command = new(options,logOpts.GetLoggerFactory());
            AnalyzeResult result = command.GetResult();

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);

            string consoleContent = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"consoleout.txt"));

            Assert.IsTrue(string.IsNullOrEmpty(consoleContent));

            //reset to normal
            StreamWriter standardOutput = new(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };
            Console.SetOut(standardOutput);
        }
    }
}