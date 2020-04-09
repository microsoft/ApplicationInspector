using ApplicationInspector.Unitprocess.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ApplicationInspector.Unitprocess.Commands
{
    [TestClass]
    public class TestPackRulesCmd
    {
        [TestMethod]
        public void DefaultRules_Fail()
        {
            PackRulesOptions options = new PackRulesOptions()
            {
                RepackDefaultRules = true
            };

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                PackRulesCommand command = new PackRulesCommand(options);
                PackRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            PackRulesOptions options = new PackRulesOptions()
            {
            };

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                PackRulesCommand command = new PackRulesCommand(options);
                PackRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void CustomRulesNoOutputFilePath_Fail()
        {
            PackRulesOptions options = new PackRulesOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                PackRulesCommand command = new PackRulesCommand(options);
                PackRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            PackRulesOptions options = new PackRulesOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\logdebug.txt"),
            };

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                PackRulesCommand command = new PackRulesCommand(options);
                PackRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = PackRulesResult.ExitCode.CriticalError;
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            PackRulesOptions options = new PackRulesOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt")
            };

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                PackRulesCommand command = new PackRulesCommand(options);
                PackRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = PackRulesResult.ExitCode.CriticalError;
            }

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }
    }
}