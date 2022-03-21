﻿namespace ApplicationInspector.Unitprocess.CLICommands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using System;
    using System.IO;

    /// <summary>
    /// Test class for Analyze Commands
    /// Each method really needs to be complete i.e. options and command objects created and checked for exceptions etc. based on inputs so
    /// doesn't create a set of shared objects
    ///
    /// </summary>
    [TestClass]
    public class CLITestAnalyzeTagsOnlyCommand
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
        public void UnknownFormat_Fail()
        {
            string args = string.Format(@"analyze -b -s {0} -f unknown -g none -t", Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"));
            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void InvalidOutputfilePath_Fail()
        {
            string args = string.Format(@"analyze -s {0} -f json -g none -o {1} -t",
                     Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                     Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"badir\output.txt"));

            var exitCode  = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            string args = string.Format(@"analyze -s {0} -f json -g none -o {1} -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\badfilepath.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void InvalidRulesPath_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -r badrulespath -f json -g none -o {1} -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -i -f json -g none -o {1} -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -i -r {1} -f json -g none -o {2} -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -r {1} -f json -g none -o {2} -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultAndCustomRulesPosMatches_Pass()
        {
            
            string args = string.Format(@"analyze -s {0} -r {1} -f json -g none -o {2} -t",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            string testContent = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
            Assert.IsTrue(testContent.Contains("Cryptography.HashAlgorithm.Legacy"));
            Assert.IsTrue(testContent.Contains("Data.Custom1"));
        }

        [TestMethod]
        public void ExclusionFilter_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -r {1} -f json -o {2} -g {3} -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\project\one"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"),
                    "*.cpp");

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public void MultiFiles()
        {
            var mainduptags = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp");
            string args = string.Format(@"analyze -s {0} -f json -o {1} -g none -t",
                $"{mainduptags}",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCodeSingleThread = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCodeSingleThread);

            string content = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));
            var result = JsonConvert.DeserializeObject<AnalyzeResult>(content);
            Assert.IsNotNull(result);
            var matches = result.Metadata.TotalMatchesCount;
            var uniqueMatches = result.Metadata.UniqueMatchesCount;
            mainduptags = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp");
            args = string.Format(@"analyze -s {0} -f json -o {1} -g none -t",
                $"{mainduptags},{mainduptags}",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            exitCodeSingleThread = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCodeSingleThread);

            content = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));
            var result2 = JsonConvert.DeserializeObject<AnalyzeResult>(content);
            Assert.IsNotNull(result2);

            Assert.AreEqual(matches * 2, result2.Metadata.TotalMatchesCount);
            Assert.AreEqual(uniqueMatches, result2.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void ExpectedTagCountDupsAllowed_Pass()
        {
            string args = string.Format(@"analyze -s {0} -f json -o {1} -g none --single-threaded -t",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
            string content = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));
            var result = JsonConvert.DeserializeObject<AnalyzeResult>(content);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(0, result.Metadata.UniqueMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueTags.Count);
            
            args = string.Format(@"analyze -s {0} -f json -o {1} -g none -t",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
            content = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));
            result = JsonConvert.DeserializeObject<AnalyzeResult>(content);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(0, result.Metadata.UniqueMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueTags.Count);
            
        }

        [TestMethod]
        public void NoMatchesFound_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -f json -o {1} -g none -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -f json -l {1} -v trace -g none -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log1.txt"));

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                string testContent = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log1.txt"));
                exitCode = testContent.ToLower().Contains("trace") ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -f json -l {1} -v error -g none -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\nofile.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log2.txt"));

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                string testContent = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log2.txt"));
                exitCode = testContent.ToLower().Contains("error") ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -f json -l {1} -v debug -g none -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log3.txt"));

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                string testContent = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log3.txt"));
                exitCode = testContent.ToLower().Contains("debug") ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.Success);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -f json -l {1} -g none -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\badfile.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"badir\log.txt"));

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -f json -l {1} -g none -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"));

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = TestHelpers.GetPath(TestHelpers.AppPath.appInspectorCLI);

                string args = string.Format(@"analyze -s {0} -x none -f text -g none -o {1} --no-show-progress -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (AnalyzeResult.ExitCode)TestHelpers.RunProcess(appInspectorPath, args, out string testContent);

                if (exitCode == AnalyzeResult.ExitCode.Success)
                {
                    exitCode = string.IsNullOrEmpty(testContent) ? AnalyzeResult.ExitCode.Success : AnalyzeResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
            }

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }

        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"analyze -s {0} -x none -f text -g none -l {1} --no-show-progress -t",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

                exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == AnalyzeResult.ExitCode.CriticalError);
        }
    }
}