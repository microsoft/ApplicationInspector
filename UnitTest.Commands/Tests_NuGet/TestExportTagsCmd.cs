﻿namespace ApplicationInspector.Unitprocess.Commands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;

    [TestClass]
    public class TestExportTagsCmd
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
        }


        [TestMethod]
        public void Export_Pass()
        {
            ExportTagsOptions options = new()
            {
                //empty
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new(options);
                ExportTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            ExportTagsOptions options = new()
            {
                IgnoreDefaultRules = true
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new(options);
                ExportTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            ExportTagsOptions options = new()
            {
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json")
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new(options);
                ExportTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            ExportTagsOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new(options);
                ExportTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            ExportTagsOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new(options);
                ExportTagsResult result = command.GetResult();
                exitCode = result.ResultCode;

                if (exitCode == ExportTagsResult.ExitCode.Success)
                {
                    string testLogContent = File.ReadAllText(options.LogFilePath);
                    if (string.IsNullOrEmpty(testLogContent))
                    {
                        exitCode = ExportTagsResult.ExitCode.CriticalError;
                    }
                    else if (testLogContent.ToLower().Contains("trace"))
                    {
                        exitCode = ExportTagsResult.ExitCode.Success;
                    }
                }
            }
            catch (Exception)
            {
                exitCode = ExportTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            ExportTagsOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"mybadrule.json"),
                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new(options);
                ExportTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!string.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                {
                    exitCode = ExportTagsResult.ExitCode.Success;
                }
                else
                {
                    exitCode = ExportTagsResult.ExitCode.CriticalError;
                }
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            ExportTagsOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new(options);
                ExportTagsResult result = command.GetResult();
                exitCode = result.ResultCode;

                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (string.IsNullOrEmpty(testLogContent))
                {
                    exitCode = ExportTagsResult.ExitCode.CriticalError;
                }
                else if (testLogContent.ToLower().Contains("debug"))
                {
                    exitCode = ExportTagsResult.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            ExportTagsOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\logdebug.txt"),
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.Success;
            try
            {
                ExportTagsCommand command = new(options);
                ExportTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = ExportTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            ExportTagsOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new(options);
                ExportTagsResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = ExportTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);
        }
    }
}