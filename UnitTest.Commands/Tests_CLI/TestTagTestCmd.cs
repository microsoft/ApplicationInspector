using ApplicationInspector.Unitprocess.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ApplicationInspector.Unitprocess.CLICommands
{
    /// <summary>
    /// Test class for Analyze Commands
    /// Each method really needs to be complete i.e. options and command objects created and checked for exceptions etc. based on inputs so
    /// doesn't create a set of shared objects
    ///
    /// </summary>
    [TestClass]
    public class CLITestTagTestCmd
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
        public void RulesPresent_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void RulesPresent_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void ZipReadDiff_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\main.zip"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void RulesNotPresent_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -t rulesnotpresent -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void RulesNotPresent_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -t rulesnotpresent -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\badir\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void RulesPresentNoResults_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void RulesNotPresentNoResults_Success()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -t rulesnotpresent -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void RulesNotPresentNoResults_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r rulesnotpresent -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myfakerule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void NoRules_Fail()
        {
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -k none -l {1}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void RulesPresentToTxtOutFile_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -f text -k none -o {2} -l {3}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void RulesPresentToJsonOutFile_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -f json -k none -o {2} -l {3}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void RulesPresentToUnknownFormat_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -f unknown -k none -o {2} -l {3}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -v trace -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"mybadrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                if (exitCode == TagTestResult.ExitCode.CriticalError)
                {
                    string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                    exitCode = testContent.ToLower().Contains("trace") ? TagTestResult.ExitCode.TestPassed : TagTestResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -v error -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfilepath.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                if (exitCode == TagTestResult.ExitCode.CriticalError)
                {
                    string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                    exitCode = testContent.ToLower().Contains("error") ? TagTestResult.ExitCode.TestPassed : TagTestResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -v debug -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                if (exitCode == TagTestResult.ExitCode.CriticalError)
                {
                    string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                    exitCode = testContent.ToLower().Contains("debug") ? TagTestResult.ExitCode.TestPassed : TagTestResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\baddir\log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\blank.cpp"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -x none -f text -o {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (TagTestResult.ExitCode)Helper.RunProcess(appInspectorPath, args, out string testContent);

                if (exitCode == TagTestResult.ExitCode.TestPassed)
                {
                    exitCode = String.IsNullOrEmpty(testContent) ? TagTestResult.ExitCode.TestPassed : TagTestResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagtest -s {0} -r {1} -k none -x none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagTestResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);
        }
    }
}