namespace ApplicationInspector.Unitprocess.CLICommands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;

    [TestClass]
    public class CLITestVerifyRulesCmd
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
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -d -f text -l {0}",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -f text -l {0}",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
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
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -r {0} -f text -l {1}",
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void VerifyRulesToTxtFile_Pass()
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -r {0} -f json -o {1} -l {2}",
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void VerifyRulesToJsonFile_Pass()
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -r {0} -f json -o {1} -l {2}",
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void VerifyRulesToUnknownFile_Fail()
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -r {0} -f unknown -o {1} -l {2}",
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void VerifyRulesToBadFile_Fail()
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -r {0} -f text -l {1}",
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"\baddir\output.txt"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -r {0} -v trace -f text -l {1}",
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                if (exitCode == VerifyRulesResult.ExitCode.Verified)
                {
                    string testLogContent = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));
                    if (string.IsNullOrEmpty(testLogContent))
                    {
                        exitCode = VerifyRulesResult.ExitCode.CriticalError;
                    }
                    else if (testLogContent.ToLower().Contains("trace"))
                    {
                        exitCode = VerifyRulesResult.ExitCode.Verified;
                    }
                }
                else
                {
                    exitCode = VerifyRulesResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
                exitCode = VerifyRulesResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -r {0} -f text -l {1}",
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"\badir\myrule.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                if (exitCode == VerifyRulesResult.ExitCode.CriticalError)
                {
                    string testLogContent = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));
                    if (!string.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                    {
                        exitCode = VerifyRulesResult.ExitCode.Verified;
                    }
                    else
                    {
                        exitCode = VerifyRulesResult.ExitCode.CriticalError;
                    }
                }
            }
            catch (Exception)
            {
                exitCode = VerifyRulesResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -r {0} -f text -l {1}",
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"\badir\log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -r {0} -l {1}",
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\empty.cpp"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = TestHelpers.GetPath(TestHelpers.AppPath.appInspectorCLI);
                string args = string.Format(@"verifyrules -r {0} -f text -x none -o {1} -l {2}",
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)TestHelpers.RunProcess(appInspectorPath, args, out string testContent);

                if (exitCode == VerifyRulesResult.ExitCode.Verified)
                {
                    exitCode = string.IsNullOrEmpty(testContent) ? VerifyRulesResult.ExitCode.Verified : VerifyRulesResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.Verified);
        }

        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"verifyrules -r {0} -f text -x none -l {1}",
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                        Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (VerifyRulesResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == VerifyRulesResult.ExitCode.CriticalError);
        }
    }
}