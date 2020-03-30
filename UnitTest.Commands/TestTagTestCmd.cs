using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ApplicationInspector.UnitTest.Commands
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
        [TestMethod]
        public void RulesPresent_Pass()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagTestCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestPassed);
        }


        [TestMethod]
        public void RulesPresent_Fail()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myfakerule.json"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestFailed);
        }



        [TestMethod]
        [Ignore]//occasional fail loading isharpzip lib for unknown reasons -todo to ensure only test related
        public void BasicZipReadDiff_Pass()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\main.zip"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception e)
            {
                exitCode = TagTestCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestPassed);
        }


        [TestMethod]
        public void RulesNotPresent_Pass()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myfakerule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                TestType = "RulesNotPresent"
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;

            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagTestCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestPassed);
        }



        [TestMethod]
        public void RulesNotPresent_Fail()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                TestType = "RulesNotPresent"
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagTestCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestFailed);
        }



        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\nofilehere.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void RulesPresentNoResults_Fail()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestFailed);
        }


        [TestMethod]
        public void RulesNotPresentNoResults_Success()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                TestType = "RulesNotPresent",
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestPassed);
        }


        [TestMethod]
        public void RulesNotPresentNoResults_Fail()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                TestType = "RulesNotPresent",
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestFailed);
        }


        [TestMethod]
        public void NoRules_Fail()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.CriticalError);
        }



        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                    exitCode = TagTestCommand.ExitCode.CriticalError;
                else if (testLogContent.ToLower().Contains("trace"))
                    exitCode = TagTestCommand.ExitCode.TestPassed;

            }
            catch (Exception)
            {
                exitCode = TagTestCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestPassed);
        }



        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\nofilehere.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                command.Run();
            }
            catch (Exception)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                    exitCode = TagTestCommand.ExitCode.TestPassed;
                else
                    exitCode = TagTestCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestPassed);
        }



        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                    exitCode = TagTestCommand.ExitCode.CriticalError;
                else if (testLogContent.ToLower().Contains("debug"))
                    exitCode = TagTestCommand.ExitCode.TestPassed;

            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestPassed);
        }



        [TestMethod]
        [Ignore]
        public void InvalidLogPath_Fail()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\logdebug.txt"),
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagTestCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }




        [TestMethod]
        [Ignore]//another faulty fail that passes when run individually...MSTest flaw?
        public void InsecureLogPath_Fail()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagTestCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.CriticalError);
        }



        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none",
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"),
                ConsoleVerbosityLevel = "none"
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                // Attempt to open output file.
                using (var writer = new StreamWriter(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt")))
                {

                    // Redirect standard output from the console to the output file.
                    Console.SetOut(writer);

                    TagTestCommand command = new TagTestCommand(options);
                    exitCode = (TagTestCommand.ExitCode)command.Run();
                    try
                    {
                        string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt"));
                        if (String.IsNullOrEmpty(testContent))
                            exitCode = TagTestCommand.ExitCode.TestPassed;
                        else
                            exitCode = TagTestCommand.ExitCode.TestFailed;
                    }
                    catch (Exception)
                    {
                        exitCode = TagTestCommand.ExitCode.TestPassed;//no console output file found
                    }
                }
            }
            catch (Exception)
            {
                exitCode = TagTestCommand.ExitCode.CriticalError;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.TestPassed);
        }


        [TestMethod]
        public void NoOutputSelected_Fail()
        {
            TagTestCommandOptions options = new TagTestCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                FilePathExclusions = "none",
                //OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"), 
                ConsoleVerbosityLevel = "none" //together with no output file = no output at all which is a fail
            };

            TagTestCommand.ExitCode exitCode = TagTestCommand.ExitCode.CriticalError;
            try
            {
                TagTestCommand command = new TagTestCommand(options);
                exitCode = (TagTestCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestCommand.ExitCode.CriticalError);
        }
    }
}

