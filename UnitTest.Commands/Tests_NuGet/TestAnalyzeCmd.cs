using ApplicationInspector.Unitprocess.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace ApplicationInspector.Unitprocess.Commands
{
    /// <summary>
    /// Test class for Analyze Commands
    /// Each method really needs to be complete i.e. options and command objects created and checked for exceptions etc. based on inputs so
    /// doesn't create a set of shared objects
    /// Note: in order to avoid log reuse, include the optional parameter CloseLogOnCommandExit = true
    /// </summary>
    [TestClass]
    public class TestAnalyzeCmd
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
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\log.txt")
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void BasicAnalyze_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none" //allow source under unittest path
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
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
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\main.zip"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
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
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"baddir\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
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
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"notfound.json")
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
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
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = true,
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
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
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
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
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
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
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json")
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                if (result.Metadata.UniqueTags.Any(v => v.Contains("Authentication.General")) &&
                    result.Metadata.UniqueTags.Any(v => v.Contains("Data.Custom1")))
                {
                    exitCode = AnalyzeResult.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void ExclusionFilter_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\project\one")
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
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
        public void ExpectedTagCountDupsAllowed_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                AllowDupTags = true,
                SingleThread = true
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
                if (exitCode == AnalyzeResult.ExitCode.Success)
                {
                    exitCode = result.Metadata.TotalMatchesCount == 11 && result.Metadata.UniqueMatchesCount == 7 ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.NoMatches;
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);

            AnalyzeResult.ExitCode exitCodeMultiThread = AnalyzeResult.ExitCode.CriticalError;
            options.SingleThread = false;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCodeMultiThread = result.ResultCode;
                if (exitCodeMultiThread == AnalyzeResult.ExitCode.Success)
                {
                    exitCodeMultiThread = result.Metadata.TotalMatchesCount == 11 && result.Metadata.UniqueMatchesCount == 7 ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.NoMatches;
                }
            }
            catch (Exception)
            {
                exitCodeMultiThread = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCodeMultiThread == AnalyzeResult.ExitCode.Success);

        }

        [TestMethod]
        public void ExpectedTagCountNoDupsAllowed_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                AllowDupTags = false
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
                if (exitCode == AnalyzeResult.ExitCode.Success)
                {
                    exitCode = result.Metadata.TotalMatchesCount == 7 && result.Metadata.UniqueMatchesCount == 7 ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.NoMatches;
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void NoMatchesFound_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                FilePathExclusions = "none", //allow source under unittest path

                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                {
                    exitCode = AnalyzeResult.ExitCode.CriticalError;
                }
                else if (testLogContent.ToLower().Contains("trace"))
                {
                    exitCode = AnalyzeResult.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfile.cpp"),
                FilePathExclusions = "none", //allow source under unittest path

                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
            }
            catch (Exception)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                {
                    exitCode = AnalyzeResult.ExitCode.Success;
                }
                else
                {
                    exitCode = AnalyzeResult.ExitCode.CriticalError;
                }
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
                CloseLogOnCommandExit = true
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                AnalyzeResult result = command.GetResult();
                exitCode = result.ResultCode;
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                {
                    exitCode = AnalyzeResult.ExitCode.CriticalError;
                }
                else if (testLogContent.ToLower().Contains("debug"))
                {
                    exitCode = AnalyzeResult.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
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
        public void NoConsoleOutput_Pass()
        {
            AnalyzeOptions options = new AnalyzeOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                ConsoleVerbosityLevel = "none"
            };

            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                // Attempt to open output file.
                using (var writer = new StreamWriter(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt")))
                {
                    // Redirect standard output from the console to the output file.
                    Console.SetOut(writer);

                    AnalyzeCommand command = new AnalyzeCommand(options);
                    AnalyzeResult result = command.GetResult();
                    exitCode = result.ResultCode;
                    try
                    {
                        string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt"));
                        if (String.IsNullOrEmpty(testContent))
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
            }
            catch (Exception)
            {
                exitCode = AnalyzeResult.ExitCode.CriticalError;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }
    }
}