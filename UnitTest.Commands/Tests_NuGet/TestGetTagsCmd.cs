using ApplicationInspector.Unitprocess.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationInspector.Unitprocess.Commands
{
    /// <summary>
    /// Test class for Analyze Commands
    /// Each method really needs to be complete i.e. options and command objects created and checked for exceptions etc. based on inputs so
    /// doesn't create a set of shared objects
    /// Note: in order to avoid log reuse, include the optional parameter CloseLogOnCommandExit = true
    /// </summary>
    [TestClass]
    public class TestGetTagsCmd
    {
        [TestInitialize]
        public void InitOutput()
        {
            Directory.CreateDirectory(Helper.GetPath(Helper.AppPath.testOutput));
        }

        [TestCleanup]
        public void CleanUp()
        {
            try
            {
                Directory.Delete(Helper.GetPath(Helper.AppPath.testOutput), true);
            }
            catch
            {
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\log.txt")
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void BasicAnalyze_Pass()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none" //allow source under unittest path
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {

            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void BasicZipRead_Pass()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\main.zip"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {

            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"baddir\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidRulesPath_Fail()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"notfound.json")
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = true,
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = GetTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = GetTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultAndCustomRulesMatched_Pass()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json")
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                if (result.Metadata.UniqueTags.Any(v => v.Contains("Authentication.General")) &&
                    result.Metadata.UniqueTags.Any(v => v.Contains("Data.Custom1")))
                {
                    exitCode = GetTagsResult.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                exitCode = GetTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void ExclusionFilter_Pass()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\project\one"),
                FilePathExclusions = "main.cpp"
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public async Task ExpectedTagCountAsync()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SingleThread = true
            };

            GetTagsCommand command = new GetTagsCommand(options);
            var result = await command.GetResultAsync(new CancellationToken());
            Assert.AreEqual(GetTagsResult.ExitCode.Success, result.ResultCode);
            Assert.AreEqual(0, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(0, result.Metadata.UniqueMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueTags.Count);
        }

        [TestMethod]
        public void ExpectedTagCount()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SingleThread = true
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
                if (exitCode == GetTagsResult.ExitCode.Success)
                {
                    exitCode = result.Metadata.TotalMatchesCount == 0 && result.Metadata.UniqueMatchesCount == 0 && result.Metadata.UniqueTags.Count == 7 ? GetTagsResult.ExitCode.Success : GetTagsResult.ExitCode.NoMatches;
                }
            }
            catch (Exception)
            {
                exitCode = GetTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);

            GetTagsResult.ExitCode exitCodeMultiThread = GetTagsResult.ExitCode.CriticalError;
            options.SingleThread = false;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCodeMultiThread = result.ResultCode;
                if (exitCodeMultiThread == GetTagsResult.ExitCode.Success)
                {
                    exitCodeMultiThread = result.Metadata.TotalMatchesCount == 0 && result.Metadata.UniqueMatchesCount == 0 && result.Metadata.UniqueTags.Count == 7 ? GetTagsResult.ExitCode.Success : GetTagsResult.ExitCode.NoMatches;
                }
            }
            catch (Exception)
            {
                exitCodeMultiThread = GetTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCodeMultiThread == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void NoMatchesFound_Pass()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = GetTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                FilePathExclusions = "none", //allow source under unittest path

                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                {
                    exitCode = GetTagsResult.ExitCode.CriticalError;
                }
                else if (testLogContent.ToLower().Contains("trace"))
                {
                    exitCode = GetTagsResult.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                exitCode = GetTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfile.cpp"),
                FilePathExclusions = "none", //allow source under unittest path

                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
            }
            catch (Exception)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                {
                    exitCode = GetTagsResult.ExitCode.Success;
                }
                else
                {
                    exitCode = GetTagsResult.ExitCode.CriticalError;
                }
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
                CloseLogOnCommandExit = true
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                {
                    exitCode = GetTagsResult.ExitCode.CriticalError;
                }
                else if (testLogContent.ToLower().Contains("debug"))
                {
                    exitCode = GetTagsResult.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                GetTagsCommand command = new GetTagsCommand(options);
                GetTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            GetTagsCommandOptions options = new GetTagsCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                ConsoleVerbosityLevel = "none"
            };

            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                // Attempt to open output file.
                using (var writer = new StreamWriter(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt")))
                {
                    // Redirect standard output from the console to the output file.
                    Console.SetOut(writer);

                    GetTagsCommand command = new GetTagsCommand(options);
                    GetTagsResult result = command.GetResult();
                    exitCode = result.ResultCode;
                    try
                    {
                        string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt"));
                        if (String.IsNullOrEmpty(testContent))
                        {
                            exitCode = GetTagsResult.ExitCode.Success;
                        }
                        else
                        {
                            exitCode = GetTagsResult.ExitCode.NoMatches;
                        }
                    }
                    catch (Exception)
                    {
                        exitCode = GetTagsResult.ExitCode.Success;//no console output file found
                    }
                }
            }
            catch (Exception)
            {
                exitCode = GetTagsResult.ExitCode.CriticalError;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }
    }
}