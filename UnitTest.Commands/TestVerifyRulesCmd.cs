using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ApplicationInspector.UnitTest.Commands
{

    [TestClass]
    public class TestVerifyRulesCmd
    {

        [TestMethod]
        [Ignore] //default option won't find rules unless run from CLI; todo to look at addressed
        public void DefaultRules_Pass()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {
                VerifyDefaultRules = true
            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                exitCode = (VerifyRulesCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.Verified);
        }


        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {

            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                exitCode = (VerifyRulesCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.CriticalError);
        }



        [TestMethod]
        public void CustomRules_Pass()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                exitCode = (VerifyRulesCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.Verified);
        }




        [TestMethod]
        public void VerifyRulesToFile_Pass()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"verifyRules.txt")
            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                exitCode = (VerifyRulesCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.Verified);
        }


        [TestMethod]
        public void VerifyRulesToFile_Fail()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\verifyRules.txt")
            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                exitCode = (VerifyRulesCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.CriticalError);
        }




        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                exitCode = (VerifyRulesCommand.ExitCode)command.Run();
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                    exitCode = VerifyRulesCommand.ExitCode.CriticalError;
                else if (testLogContent.ToLower().Contains("trace"))
                    exitCode = VerifyRulesCommand.ExitCode.Verified;

            }
            catch (Exception)
            {
                exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.Verified);
        }



        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\verifyRules.txt"),
                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                command.Run();
            }
            catch (Exception)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                    exitCode = VerifyRulesCommand.ExitCode.Verified;
                else
                    exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.Verified);
        }



        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"verifyrules.txt"),
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                exitCode = (VerifyRulesCommand.ExitCode)command.Run();
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                    exitCode = VerifyRulesCommand.ExitCode.CriticalError;
                else if (testLogContent.ToLower().Contains("debug"))
                    exitCode = VerifyRulesCommand.ExitCode.Verified;

            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.Verified);
        }



        [TestMethod]
        [Ignore]
        public void InvalidLogPath_Fail()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"verifyrules.txt"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\logdebug.txt"),
            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                exitCode = (VerifyRulesCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }




        [TestMethod]
        [Ignore]//another faulty fail that passes when run individually...MSTest flaw?
        public void InsecureLogPath_Fail()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"verifyrules.txt"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json")
            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                exitCode = (VerifyRulesCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void NoOutputSelected_Fail()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                ConsoleVerbosityLevel = "none",
                //OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"verifyrules.txt"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt")
            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                exitCode = (VerifyRulesCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void NoConsoleOutputFileOutput_Pass()
        {
            VerifyRulesCommandOptions options = new VerifyRulesCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"verifyrules.txt"),
                ConsoleVerbosityLevel = "none"
            };

            VerifyRulesCommand.ExitCode exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            try
            {
                // Attempt to open output file.
                using (var writer = new StreamWriter(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt")))
                {

                    // Redirect standard output from the console to the output file.
                    Console.SetOut(writer);

                    VerifyRulesCommand command = new VerifyRulesCommand(options);
                    exitCode = (VerifyRulesCommand.ExitCode)command.Run();
                    try
                    {
                        string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt"));
                        if (String.IsNullOrEmpty(testContent))
                            exitCode = VerifyRulesCommand.ExitCode.Verified;
                        else
                            exitCode = VerifyRulesCommand.ExitCode.NotVerified;
                    }
                    catch (Exception)
                    {
                        exitCode = VerifyRulesCommand.ExitCode.Verified;//no console output file found
                    }
                }
            }
            catch (Exception e)
            {
                exitCode = VerifyRulesCommand.ExitCode.CriticalError;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == VerifyRulesCommand.ExitCode.Verified);
        }


    }
}
