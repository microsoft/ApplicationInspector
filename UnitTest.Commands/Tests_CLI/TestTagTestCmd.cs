using ApplicationInspector.UnitTest.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;

namespace ApplicationInspector.UnitTest.CLICommands
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
        [TestMethod]
        public void RulesPresent_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\main.zip"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }


        [TestMethod]
        public void RulesNotPresent_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -t rulesnotpresent -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }



        [TestMethod]
        public void RulesNotPresent_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -t rulesnotpresent -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestFailed);
        }



        [TestMethod]
        public void InvalidSourcePath_Fail()
        {

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\badir\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
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
            TagTestOptions options = new TagTestOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                TestType = "RulesNotPresent",
                FilePathExclusions = "none", //allow source under unittest path
            };

            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -t rulesnotpresent -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r rulesnotpresent -r {1} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myfakerule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -k none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -f text -k none -o {2} -l {3}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }


        [TestMethod]
        public void RulesPresentToJsonOutFile_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -f json -k none -o {2} -l {3}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }


        [TestMethod]
        public void RulesPresentToUnknownFormat_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -f unknown -k none -o {2} -l {3}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;
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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -v trace -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"mybadrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));


                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;

                if (exitCode == TagTestResult.ExitCode.CriticalError)
                {
                    string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                    exitCode = testContent.ToLower().Contains("trace") ? TagTestResult.ExitCode.TestPassed : TagTestResult.ExitCode.CriticalError;
                }

            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }



        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -v error -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfilepath.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));


                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;

                if (exitCode == TagTestResult.ExitCode.CriticalError)
                {
                    string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                    exitCode = testContent.ToLower().Contains("error") ? TagTestResult.ExitCode.TestPassed : TagTestResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }



        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -v debug -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;

                if (exitCode == TagTestResult.ExitCode.CriticalError)
                {
                    string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                    exitCode = testContent.ToLower().Contains("debug") ? TagTestResult.ExitCode.TestPassed : TagTestResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }



        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\baddir\log.txt"));


                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;

            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }




        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\blank.cpp"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (TagTestResult.ExitCode)test.ExitCode;

            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
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
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -x none -f text -o {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                Process p = new Process();
                p.StartInfo.FileName = appInspectorPath;
                p.StartInfo.Arguments = args;
                p.StartInfo.RedirectStandardOutput = true;
                bool started = p.Start();

                string testContent = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                exitCode = (TagTestResult.ExitCode)p.ExitCode;

                if (exitCode == TagTestResult.ExitCode.TestPassed)
                {
                    exitCode = String.IsNullOrEmpty(testContent) ? TagTestResult.ExitCode.TestPassed : TagTestResult.ExitCode.CriticalError;
                }

            }
            catch (Exception)
            {
                exitCode = TagTestResult.ExitCode.CriticalError;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.TestPassed);
        }


        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"tagtest -s {0} -r {1} -k none -x none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process p = new Process();
                p.StartInfo.FileName = appInspectorPath;
                p.StartInfo.Arguments = args;
                p.StartInfo.RedirectStandardOutput = true;
                bool started = p.Start();

                string testContent = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                exitCode = (TagTestResult.ExitCode)p.ExitCode;

            }
            catch (Exception)
            {

            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == TagTestResult.ExitCode.CriticalError);
        }
    }

}

