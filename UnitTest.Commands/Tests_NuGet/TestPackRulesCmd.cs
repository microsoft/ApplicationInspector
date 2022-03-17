namespace ApplicationInspector.Unitprocess.Commands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;

    [TestClass]
    public class TestPackRulesCmd
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

        [Ignore]
        [TestMethod]
        public void DefaultRules_Fail()
        {
            PackRulesOptions options = new()
            {
                RepackDefaultRules = true
            };

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                PackRulesCommand command = new(options);
                PackRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomNoEmbeddedRules_Fail()
        {
            PackRulesOptions options = new()
            {
            };

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                PackRulesCommand command = new(options);
                PackRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomEmbeddedRules_Pass()
        {
            PackRulesOptions options = new() { PackEmbeddedRules = true };

            try
            {
                PackRulesCommand command = new(options);
                PackRulesResult result = command.GetResult();
                Assert.AreEqual(PackRulesResult.ExitCode.Success, result.ResultCode);
            }
            catch (Exception)
            {
                Assert.Fail();
                //check for specific error if desired
            }
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            PackRulesOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\logdebug.txt"),
            };

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                PackRulesCommand command = new(options);
                PackRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = PackRulesResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [Ignore]
        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            PackRulesOptions options = new()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"main.cpp")
            };

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                PackRulesCommand command = new(options);
                PackRulesResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = PackRulesResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }
    }
}