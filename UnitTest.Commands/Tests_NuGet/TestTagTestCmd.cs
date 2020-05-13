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
    public class TestTagTestCmd
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
        public void RulesPresent_Pass()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void RulesPresent_Fail()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myfakerule.json"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void BasicZipReadDiff_Pass()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\main.zip"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void RulesNotPresent_Pass()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myfakerule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                TestType = "RulesNotPresent"
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void RulesNotPresent_Fail()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                TestType = "RulesNotPresent"
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\nofilehere.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void RulesPresentNoResults_Fail()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void RulesNotPresentNoResults_Success()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                TestType = "RulesNotPresent",
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void RulesNotPresentNoResults_Fail()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                TestType = "RulesNotPresent",
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void NoRules_Fail()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;

                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                {
                    exitCode = TagTestResult.ExitCode.CriticalError;
                }
                else if (testLogContent.ToLower().Contains("trace"))
                {
                    exitCode = TagTestResult.ExitCode.TestPassed;
                }
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\nofilehere.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                {
                    exitCode = TagTestResult.ExitCode.TestPassed;
                }
                else
                {
                    exitCode = TagTestResult.ExitCode.CriticalError;
                }
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;

                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                {
                    exitCode = TagTestResult.ExitCode.CriticalError;
                }
                else if (testLogContent.ToLower().Contains("debug"))
                {
                    exitCode = TagTestResult.ExitCode.TestPassed;
                }
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\logdebug.txt"),
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                TagTestResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none",
                ConsoleVerbosityLevel = "none"
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                // Attempt to open output file.
                using (var writer = new StreamWriter(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt")))
                {
                    // Redirect standard output from the console to the output file.
                    Console.SetOut(writer);

                    TagTestCommand command = new TagTestCommand(options);
                    TagTestResult result = command.GetResult();
                    exitCode = result.ResultCode;
                    try
                    {
                        string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt"));
                        if (String.IsNullOrEmpty(testContent))
                        {
                            exitCode = TagTestResult.ExitCode.TestPassed;
                        }
                        else
                        {
                            exitCode = TagTestResult.ExitCode.TestFailed;
                        }
                    }
                    catch (Exception)
                    {
                        exitCode = TagTestResult.ExitCode.TestPassed;//no console output file found
                    }
                }
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }
    }
}