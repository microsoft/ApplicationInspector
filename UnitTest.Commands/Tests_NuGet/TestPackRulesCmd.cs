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
    }
}