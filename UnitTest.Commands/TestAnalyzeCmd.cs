using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ApplicationInspector.UnitTest.Commands
{
    /// <summary>
    /// Test class for Analyze Commands
    /// Each method really needs to be complete i.e.options and command objects created and checked for exceptions etc. based on inputs so 
    /// doesn't create a set of shared objects
    /// 
    /// </summary>
    [TestClass]
    public class TestAnalyzeCmd
    {
        [TestMethod]
        public void BasicHTML()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "",
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run(); //alternate to send output to named file, console or browser
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void SimpleTagsHTML_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "",
                SimpleTagsOnly = true,
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run(); //alternate to send output to named file, console or browser
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }

        [TestMethod]
        public void DupTagsHTML_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "",
                AllowDupTags = true,
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run(); //alternate to send output to named file, console or browser
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }

        [TestMethod]
        public void BasicZipRead_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\mainx.zip"),
                FilePathExclusions = "",
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run(); //alternate to send output to named file, console or browser
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
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "",
                IgnoreDefaultRules = true,
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run(); //alternate to send output to named file, console or browser
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
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "",
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run(); //alternate to send output to named file, console or browser
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
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "",
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run(); //alternate to send output to named file, console or browser
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void InvalidOutfilePath_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "",
                OutputFileFormat = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"badir\noway"),
                SuppressBrowserOpen = false
            };
            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run(); //alternate to send output to named file, console or browser
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"baddir\main.cpp"),
                FilePathExclusions = "",
                SuppressBrowserOpen = false
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run(); //alternate to send output to named file, console or browser
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void InvalidRulesPath_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "",
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"notfound.json"),
                SuppressBrowserOpen = false
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run(); //alternate to send output to named file, console or browser
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        [Ignore]
        public void SimpleTagsText_Pass()
        {
            throw new NotImplementedException("Please create a test first.");
        }

        [TestMethod]
        [Ignore]
        public void SimpleTagsJson_Pass()
        {
            throw new NotImplementedException("Please create a test first.");
        }

        [TestMethod]
        [Ignore]
        public void ExclusionFilterByPass_Pass()
        {
            throw new NotImplementedException("Please create a test first.");
        }


        [TestMethod]
        [Ignore]
        public void ExclusionFilterByPass_Fail()
        {

        }

        [TestMethod]
        [Ignore]
        public void DefaultLogByLevel_Pass()
        {
            throw new NotImplementedException("Please create a test first.");
        }

        [TestMethod]
        [Ignore]
        public void CustomLogByLevel_Pass()
        {
            throw new NotImplementedException("Please create a test first.");
        }


        [TestMethod]
        [Ignore]
        public void ExpectedTagFound_Pass()
        {
            throw new NotImplementedException("Please create a test first.");
        }



        [TestMethod]
        [Ignore]
        public void ExpectedTagCountFound_Pass()
        {
            throw new NotImplementedException("Please create a test first.");
        }




        [TestMethod]
        [Ignore]
        public void InvalidLogPath_Fail()
        {
            throw new NotImplementedException("Please create a test first.");
        }



        [TestMethod]
        [Ignore]
        public void LowConsoleOutput_Pass()
        {
            throw new NotImplementedException("Please create a test first.");
        }



        [TestMethod]
        [Ignore]
        public void HighConsoleOutput_Pass()
        {
            throw new NotImplementedException("Please create a test first.");
        }



        [TestMethod]
        [Ignore]
        public void NoUncontrolledExits()
        {
            throw new NotImplementedException("Please create a test first.");
        }




        [TestMethod]
        [Ignore]
        public void NoMatchesFound_Pass()
        {
            throw new NotImplementedException("Please create a test first.");
        }
    }
}
