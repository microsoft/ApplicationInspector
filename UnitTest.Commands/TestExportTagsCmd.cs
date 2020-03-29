using ApplicationInspector.UnitTest.Commands;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace UnitTest.Commands
{

    [TestClass]
    public class TestExportTagsCmd
    {

        [TestMethod]
        public void BasicExport_Pass()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                //empty
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                IgnoreDefaultRules = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json")
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void ExportNoRules_Fail()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                IgnoreDefaultRules = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void BasicExportToFile_Pass()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt")
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void BasicExportToFile_Fail()
        {
            ExportTagsCommandOptions options = new ExportTagsCommandOptions()
            {
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\exporttags.txt")
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                ExportTagsCommand command = new ExportTagsCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }


    }
}
