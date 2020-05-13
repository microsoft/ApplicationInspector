using ApplicationInspector.Unitprocess.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ApplicationInspector.Unitprocess.Commands
{
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

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;
        }

        [TestMethod]
        [Ignore] //default option won't find rules unless run from CLI; todo to look at addressed
        public void DefaultRules_Pass()
        {
            VerifyRulesOptions options = new VerifyRulesOptions()
            {
                VerifyDefaultRules = true
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            VerifyRulesOptions options = new VerifyRulesOptions()
            {
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void CustomRules_Pass()
        {
            VerifyRulesOptions options = new VerifyRulesOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            VerifyRulesOptions options = new VerifyRulesOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;

                if (exitCode == VerifyRulesResult.ExitCode.Verified)
                {
                    string testLogContent = File.ReadAllText(options.LogFilePath);
                    if (String.IsNullOrEmpty(testLogContent))
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

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            VerifyRulesOptions options = new VerifyRulesOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"mybadrule.json"),
                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                {
                    exitCode = VerifyRulesResult.ExitCode.Verified;
                }
                else
                {
                    exitCode = VerifyRulesResult.ExitCode.CriticalError;
                }
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            VerifyRulesOptions options = new VerifyRulesOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;

                if (exitCode == VerifyRulesResult.ExitCode.Verified)
                {
                    string testLogContent = File.ReadAllText(options.LogFilePath);
                    if (String.IsNullOrEmpty(testLogContent))
                    {
                        exitCode = VerifyRulesResult.ExitCode.CriticalError;
                    }
                    else if (testLogContent.ToLower().Contains("debug"))
                    {
                        exitCode = VerifyRulesResult.ExitCode.Verified;
                    }
                }
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            VerifyRulesOptions options = new VerifyRulesOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\logdebug.txt"),
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = VerifyRulesResult.ExitCode.CriticalError;
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            VerifyRulesOptions options = new VerifyRulesOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp")
            };

            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                VerifyRulesCommand command = new VerifyRulesCommand(options);
                VerifyRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = VerifyRulesResult.ExitCode.CriticalError;
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.CriticalError);
        }
    }
}