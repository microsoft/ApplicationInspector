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
    public class TestTagDiffCmd
    {
        [TestMethod]
        public void Equality_Pass()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestPassed);
        }


        [TestMethod]
        public void Equality_Fail()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestFailed);
        }



        [TestMethod]
        public void BasicZipReadDiff_Pass()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\mainx.zip"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestPassed);
        }


        [TestMethod]
        public void InEquality_Pass()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                TestType = "Inequality"
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;

            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestPassed);
        }



        [TestMethod]
        public void InEquality_Fail()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                TestType = "Inequality"
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestFailed);
        }



        [TestMethod]
        public void SameSrcFile_Fail()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };
            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.CriticalError);
        }

        [TestMethod]
        public void OneSrcResult_Fail()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };
            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\nofilehere.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void NoResults_Fail()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\blank.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = true
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = false,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestPassed);
        }


        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestPassed);
        }



        [TestMethod]
        public void DefaultWithCustomRules_Fail()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestFailed);
        }




        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                    exitCode = TagDiffCommand.ExitCode.CriticalError;
                else if (testLogContent.ToLower().Contains("trace"))
                    exitCode = TagDiffCommand.ExitCode.TestPassed;

            }
            catch (Exception)
            {
                exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestPassed);
        }



        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\nofilehere.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                command.Run();
            }
            catch (Exception)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                    exitCode = TagDiffCommand.ExitCode.TestPassed;
                else
                    exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestPassed);
        }



        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                    exitCode = TagDiffCommand.ExitCode.CriticalError;
                else if (testLogContent.ToLower().Contains("debug"))
                    exitCode = TagDiffCommand.ExitCode.TestPassed;

            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestPassed);
        }



        [TestMethod]
        [Ignore]
        public void InvalidLogPath_Fail()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\logdebug.txt"),
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }




        [TestMethod]
        [Ignore]//another faulty fail that passes when run individually...MSTest flaw?
        public void InsecureLogPath_Fail()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.CriticalError);
        }



        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                FilePathExclusions = "none",
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"),
                ConsoleVerbosityLevel = "none"
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                // Attempt to open output file.
                using (var writer = new StreamWriter(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt")))
                {

                    // Redirect standard output from the console to the output file.
                    Console.SetOut(writer);

                    TagDiffCommand command = new TagDiffCommand(options);
                    exitCode = (TagDiffCommand.ExitCode)command.Run();
                    try
                    {
                        string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt"));
                        if (String.IsNullOrEmpty(testContent))
                            exitCode = TagDiffCommand.ExitCode.TestPassed;
                        else
                            exitCode = TagDiffCommand.ExitCode.TestFailed;
                    }
                    catch (Exception)
                    {
                        exitCode = TagDiffCommand.ExitCode.TestPassed;//no console output file found
                    }
                }
            }
            catch (Exception)
            {
                exitCode = TagDiffCommand.ExitCode.CriticalError;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.TestPassed);
        }


        [TestMethod]
        public void NoOutputSelected_Fail()
        {
            TagDiffCommandOptions options = new TagDiffCommandOptions()
            {
                SourcePath1 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                SourcePath2 = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                FilePathExclusions = "none",
                //OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"), 
                ConsoleVerbosityLevel = "none" //together with no output file = no output at all which is a fail
            };

            TagDiffCommand.ExitCode exitCode = TagDiffCommand.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new TagDiffCommand(options);
                exitCode = (TagDiffCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffCommand.ExitCode.CriticalError);
        }
    }
}

