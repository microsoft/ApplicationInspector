using ApplicationInspector.Unitprocess.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ApplicationInspector.Unitprocess.Commands
{
    /// <summary>
    /// Test class for Analyze Commands
    /// Each method really needs to be complete i.e. options and command objects created and checked for exceptions etc. based on inputs so
    /// doesn't create a set of shared objects
    ///
    /// </summary>
    [TestClass]
    public class TestTagDiffCmd
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
        public void Equality_Pass()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void Equality_Fail()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void BasicZipReadDiff_Pass()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\mainx.zip"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void InEquality_Pass()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                TestType = "Inequality"
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;

            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void InEquality_Fail()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                TestType = "Inequality"
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void SameSrcFile_Fail()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void OneSrcResult_Fail()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\nofilehere.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoResults_Fail()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\blank.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = true
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = false,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Fail()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();

                exitCode = result.ResultCode;
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                {
                    exitCode = TagDiffResult.ExitCode.CriticalError;
                }
                else if (testLogContent.ToLower().Contains("trace"))
                {
                    exitCode = TagDiffResult.ExitCode.TestPassed;
                }
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\nofilehere.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                {
                    exitCode = TagDiffResult.ExitCode.TestPassed;
                }
                else
                {
                    exitCode = TagDiffResult.ExitCode.CriticalError;
                }
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();

                exitCode = result.ResultCode;
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                {
                    exitCode = TagDiffResult.ExitCode.CriticalError;
                }
                else if (testLogContent.ToLower().Contains("debug"))
                {
                    exitCode = TagDiffResult.ExitCode.TestPassed;
                }
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\log.txt"),
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            TagDiffOptions options = new TagDiffOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none",
                ConsoleVerbosityLevel = "none"
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                // Attempt to open output file.
                using (var writer = new StreamWriter(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt")))
                {
                    // Redirect standard output from the console to the output file.
                    Console.SetOut(writer);

                    TagDiffCommand command = new TagDiffCommand(options);
                    TagDiffResult result = command.GetResult();
                    exitCode = result.ResultCode;
                    try
                    {
                        string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt"));
                        if (String.IsNullOrEmpty(testContent))
                        {
                            exitCode = TagDiffResult.ExitCode.TestPassed;
                        }
                        else
                        {
                            exitCode = TagDiffResult.ExitCode.TestFailed;
                        }
                    }
                    catch (Exception)
                    {
                        exitCode = TagDiffResult.ExitCode.TestPassed;//no console output file found
                    }
                }
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.TestPassed;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }
    }
}