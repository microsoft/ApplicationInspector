namespace ApplicationInspector.Unitprocess.Commands
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
    public class TestAnalyzeTagsOnlyCmd
    {
        [TestInitialize]
        public void InitOutput()
        {
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
        }

        [TestCleanup]
        public void CleanUp()
        {
            try
            {
                Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
            }
            catch
            {
            }
        }

        [TestMethod]
        public void BasicAnalyze_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(),
                TagsOnly = true
            };

            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
        }

        [TestMethod]
        public void BasicZipRead_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"zipped\main.zip") },
                FilePathExclusions = Array.Empty<string>(),
                TagsOnly = true,
                SingleThread = true
            };
            
            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(),
                TagsOnly = true,
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
                FilePathExclusions = Array.Empty<string>(),
                TagsOnly = true,
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
            };

            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(),
                TagsOnly = true,
                CustomRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
            };

            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void DefaultAndCustomRulesMatched_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(),
                TagsOnly = true,
                CustomRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json")
            };

            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
                Assert.IsTrue(result.Metadata.UniqueTags.Any(v => v.Contains("Cryptography.HashAlgorithm.Legacy")));
                Assert.IsTrue(result.Metadata.UniqueTags.Any(v => v.Contains("Data.Custom1")));
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
                FilePathExclusions = new string[] { "main.cpp" },
                TagsOnly = true
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
        public async Task ExpectedTagCountAsync()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                SingleThread = true,
                TagsOnly = true
            };

            AnalyzeCommand command = new(options);
            var result = await command.GetResultAsync(new CancellationToken());
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(0, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(0, result.Metadata.UniqueMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueTags.Count);
        }

        [TestMethod]
        public void ExpectedTagCountTwoFiles()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"), Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\onetag.js")},
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                SingleThread = true,
                TagsOnly = true
            };

            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(result.ResultCode, AnalyzeResult.ExitCode.Success);
            Assert.AreEqual(0, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(8, result.Metadata.UniqueTags.Count);

            options.SingleThread = false;
            command = new AnalyzeCommand(options);
            result = command.GetResult();
            Assert.AreEqual(result.ResultCode, AnalyzeResult.ExitCode.Success);
            Assert.AreEqual(0, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(8, result.Metadata.UniqueTags.Count);
        }

        [TestMethod]
        public void ExpectedTagCount()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                SingleThread = true,
                TagsOnly = true
            };

            AnalyzeCommand command = new(options);
            AnalyzeResult result = command.GetResult();
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(0, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(0, result.Metadata.UniqueMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueTags.Count);

        }
        
        [TestMethod]
        public void ExpectedTagCountMultiThread()
        {
                AnalyzeOptions options = new()
                {
                    SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp") },
                    FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                    SingleThread = false,
                    TagsOnly = true
                };
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                Assert.AreEqual(AnalyzeResult.ExitCode.Success, result.ResultCode);
                Assert.AreEqual(0, result.Metadata.TotalMatchesCount);
                Assert.AreEqual(0, result.Metadata.UniqueMatchesCount);
                Assert.AreEqual(7, result.Metadata.UniqueTags.Count);
        }

        [TestMethod]
        public void NoMatchesFound_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\empty.cpp") },
                FilePathExclusions = Array.Empty<string>(),
                TagsOnly = true
            };

            try
            {
                AnalyzeCommand command = new(options);
                AnalyzeResult result = command.GetResult();
                Assert.AreEqual(AnalyzeResult.ExitCode.NoMatches, result.ResultCode);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            AnalyzeOptions options = new()
            {
                SourcePath = new string[1] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\empty.cpp") },
                FilePathExclusions = Array.Empty<string>(),
                TagsOnly = true,
            };
            LogOptions logOpts = new()
            {
                DisableConsoleOutput = true
            };
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                // Attempt to open output file.
                using var writer = new StreamWriter(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"consoleout.txt"));
                // Redirect standard output from the console to the output file.
                Console.SetOut(writer);

                AnalyzeCommand command = new(options, logOpts.GetLoggerFactory());
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
                try
                {
                    string testContent = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"consoleout.txt"));
                    if (string.IsNullOrEmpty(testContent))
                    {
                        exitCode = AnalyzeResult.ExitCode.Success;
                    }
                    else
                    {
                        exitCode = AnalyzeResult.ExitCode.NoMatches;
                    }
                }
                catch (Exception)
                {
                    exitCode = AnalyzeResult.ExitCode.Success;//no console output file found
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            //reset to normal
            StreamWriter standardOutput = new(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }
    }
}