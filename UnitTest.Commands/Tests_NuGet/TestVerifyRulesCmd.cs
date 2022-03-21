namespace ApplicationInspector.Unitprocess.Commands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;

    [TestClass]
    public class TestVerifyRulesCmd
    {
        [TestInitialize]
        public void InitOutput()
        {
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
        }

        [TestCleanup]
        public void CleanUp()
        {
            try
            {
                Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
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
                CustomRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
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
    }
}