﻿namespace ApplicationInspector.Unitprocess.Commands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;

    [TestClass]
    public class TestVerifyRulesCmd
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
        public void DefaultRules_Pass()
        {
            VerifyRulesOptions options = new()
            {
                VerifyDefaultRules = true
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            VerifyRulesOptions options = new()
            {
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void CustomRules_Pass()
        {
            VerifyRulesOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            VerifyRulesOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;

                if (exitCode == VerifyRulesResult.ExitCode.Verified)
                {
                    string testLogContent = File.ReadAllText(options.LogFilePath);
                    if (string.IsNullOrEmpty(testLogContent))
                    {
                        exitCode = VerifyRulesResult.ExitCode.CriticalError;
                    }
                    else if (testLogContent.ToLower().Contains("trace"))
                    {
                        exitCode = VerifyRulesResult.ExitCode.Verified;
                    }
                }
            }
            catch (Exception)
            {
                exitCode = VerifyRulesResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            VerifyRulesOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"mybadrule.json"),
                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            
            VerifyRulesCommand command = new(options);
            VerifyRulesResult result = command.GetResult();
            Assert.AreEqual(VerifyRulesResult.ExitCode.CriticalError, result.ResultCode);

            string testLogContent = File.ReadAllText(options.LogFilePath);
            if (string.IsNullOrEmpty(testLogContent) || !testLogContent.ToLower().Contains("error"))
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            VerifyRulesOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\logdebug.txt"),
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = VerifyRulesResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            VerifyRulesOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp")
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = VerifyRulesResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.CriticalError);
        }
    }
}