namespace ApplicationInspector.Unitprocess.Commands
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
            Directory.Delete(Helper.GetPath(Helper.AppPath.testOutput), true);
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
    }
}