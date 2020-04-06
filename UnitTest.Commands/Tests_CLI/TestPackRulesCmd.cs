using ApplicationInspector.UnitTest.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;

namespace ApplicationInspector.UnitTest.CLICommands
{
    [TestClass]
    public class CLITestPackRulesCmd
    {

        [TestMethod]
        [Ignore] //default option won't find rules unless run from CLI; todo to look at addressed
        public void DefaultRules_Pass()
        {

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -d -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)test.ExitCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.Success);
        }


        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)test.ExitCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }



        [TestMethod]
        public void CustomRules_Pass()
        {
            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -r {0} -f json -o {1} -l {2}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"packedrules.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)test.ExitCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.Success);
        }



        [TestMethod]
        [Ignore] //overide in command to force json due to unresolved failure in options constr
        public void PackRulesToTxtFile_Fail()
        {
            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -r {0} -f text -o {1} -l {2}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)test.ExitCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }



        [TestMethod]
        public void PackRulesToJsonFile_Pass()
        {
            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -r {0} -f json -o {1} -l {2}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)test.ExitCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.Success);
        }


        [TestMethod]
        public void PackRulesToBadFile_Fail()
        {
            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -r {0} -f json -o {1} -l {2}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\baddir\output.txt"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)test.ExitCode;
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }



        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -r {0} -f json -o {1} -v trace -l {2}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)test.ExitCode;

                if (exitCode == PackRulesResult.ExitCode.Success)
                {
                    string testLogContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                    if (String.IsNullOrEmpty(testLogContent))
                        exitCode = PackRulesResult.ExitCode.CriticalError;
                    else if (testLogContent.ToLower().Contains("trace"))
                        exitCode = PackRulesResult.ExitCode.Success;
                }
                else
                {
                    exitCode = PackRulesResult.ExitCode.CriticalError;
                }

            }
            catch (Exception)
            {
                exitCode = PackRulesResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.Success);
        }



        [TestMethod]
        public void LogErrorLevel_Pass()
        {

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -r {0} -f text -l {1}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"\badir\myrule.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)test.ExitCode;

                if (exitCode == PackRulesResult.ExitCode.CriticalError)
                {
                    string testLogContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                    if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                        exitCode = PackRulesResult.ExitCode.Success;
                    else
                        exitCode = PackRulesResult.ExitCode.CriticalError;
                }


            }
            catch (Exception)
            {

            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.Success);
        }



        [TestMethod]
        public void LogDebugLevel_Pass()
        {

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -r {0} -f json -o {1} -v debug -l {2}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)test.ExitCode;

                if (exitCode == PackRulesResult.ExitCode.Success)
                {
                    string testLogContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                    if (String.IsNullOrEmpty(testLogContent))
                        exitCode = PackRulesResult.ExitCode.CriticalError;
                    else if (testLogContent.ToLower().Contains("debug"))
                        exitCode = PackRulesResult.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.Success);
        }



        [TestMethod]
        public void InvalidLogPath_Fail()
        {

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -r {0} -f text -l {1}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\badir\log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)test.ExitCode;

            }
            catch (Exception)
            {
                exitCode = PackRulesResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }


        [TestMethod]
        public void InsecureLogPath_Fail()
        {

            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -r {0} -f text -l {1}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"empty.cpp"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)test.ExitCode;

            }
            catch (Exception)
            {
                exitCode = PackRulesResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }


        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -r {0} -f json -x none -o {1} -l {2}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"packedrules.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"packedrules.json"));

                Process p = new Process();
                p.StartInfo.FileName = appInspectorPath;
                p.StartInfo.Arguments = args;
                p.StartInfo.RedirectStandardOutput = true;
                bool started = p.Start();

                string testContent = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)p.ExitCode;

                if (exitCode == PackRulesResult.ExitCode.Success)
                {
                    exitCode = String.IsNullOrEmpty(testContent) ? PackRulesResult.ExitCode.Success : PackRulesResult.ExitCode.Error;
                }
            }
            catch (Exception)
            {
                exitCode = PackRulesResult.ExitCode.CriticalError;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.Success);
        }


        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"packrules -r {0} -f text -x none -l {1}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process p = new Process();
                p.StartInfo.FileName = appInspectorPath;
                p.StartInfo.Arguments = args;
                p.StartInfo.RedirectStandardOutput = true;
                bool started = p.Start();

                string testContent = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                exitCode = (PackRulesResult.ExitCode)p.ExitCode;

            }
            catch (Exception)
            {

            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == PackRulesResult.ExitCode.CriticalError);
        }

    }
}
