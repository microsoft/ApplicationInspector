using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ApplicationInspector.UnitTest.Commands
{

    [TestClass]
    public class TestExportTagsCmd
    {

        [TestMethod]
        public void Export_Pass()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                //empty
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.Success);
        }


        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                IgnoreDefaultRules = true
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json")
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.Success);
        }


        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.Success);
        }


        [TestMethod]
        public void ExportNoRules_Fail()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                IgnoreDefaultRules = true
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void ExportToFile_Pass()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt")
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.Success);
        }


        [TestMethod]
        public void ExportToFile_Fail()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\exporttags.txt")
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.CriticalError);
        }



        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                    exitCode = ExportTagsCommand.ExitCode.CriticalError;
                else if (testLogContent.ToLower().Contains("trace"))
                    exitCode = ExportTagsCommand.ExitCode.Success;

            }
            catch (Exception)
            {
                exitCode = ExportTagsCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.Success);
        }



        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"mybadrule.json"),
                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
            }
            catch (Exception e)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                    exitCode = ExportTagsCommand.ExitCode.Success;
                else
                    exitCode = ExportTagsCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.Success);
        }



        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                    exitCode = ExportTagsCommand.ExitCode.CriticalError;
                else if (testLogContent.ToLower().Contains("debug"))
                    exitCode = ExportTagsCommand.ExitCode.Success;

            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.Success);
        }



        [TestMethod]
        [Ignore]
        public void InvalidLogPath_Fail()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\logdebug.txt"),
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = ExportTagsCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }




        [TestMethod]
        [Ignore]//another faulty fail that passes when run individually...MSTest flaw?
        public void InsecureLogPath_Fail()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
            };

            ExportTagsCommand.ExitCode exitCode = ExportTagsCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (ExportTagsCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = ExportTagsCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == ExportTagsCommand.ExitCode.CriticalError);
        }


    }
}
