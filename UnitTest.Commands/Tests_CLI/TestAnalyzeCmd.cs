namespace ApplicationInspector.Unitprocess.CLICommands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.CodeAnalysis.Sarif;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// Test class for Analyze Commands
    /// Each method really needs to be complete i.e. options and command objects created and checked for exceptions etc. based on inputs so
    /// doesn't create a set of shared objects
    ///
    /// </summary>
    [TestClass]
    public class CLITestAnalyzeCmd
    {
        [TestInitialize]
        public void InitOutput()
        {
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
        }

        [TestCleanup]
        public void CleanUp()
        {
            innerCleanUp(0, 10, 50);
        }

        private void innerCleanUp(int iteration, int maxTries, int timeOutInMs)
        {
            if (iteration < maxTries)
            {
                try
                {
                    Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
                }
                catch (Exception ex) when (ex is not FileNotFoundException)
                {
                    Thread.Sleep(timeOutInMs);
                    innerCleanUp(iteration + 1, maxTries, timeOutInMs);
                }
            }
            else
            {
                throw new Exception($"Could not delete the test output after {maxTries} attempts.");
            }
        }

        [TestMethod]
        public void BasicHTMLOutput_Pass()
        {
            string args = string.Format(@"analyze -s {0} -f html -g none", Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"));
            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }


        [TestMethod]
        public void UnknownFormat_Fail() //dupliacte tags not supported for html format
        {
            string args = string.Format(@"analyze -b -s {0} -f unknown -g none", Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"));
            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void ZipReadHTMLOutput_Pass()
        {
            string args = string.Format(@"analyze -s {0} -f html -g none -l {1}",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"zipped\mainx.zip"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }

        [TestMethod]
        public void InvalidOutputfilePath_Fail()
        {
            string args = string.Format(@"analyze -s {0} -f json -g none -o {1}",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"badir\output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            string args = string.Format(@"analyze -s {0} -f json -g none -o {1}",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\badfilepath.cpp"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void InvalidRulesPath_Fail()
        {            
            string args = string.Format(@"analyze -s {0} -r badrulespath -f json -g none -o {1}",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            

            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            string args = string.Format(@"analyze -s {0} -i -f json -g none -o {1}",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            string args = string.Format(@"analyze -s {0} -i -r {1} -f json -g none -o {2}",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            string args = string.Format(@"analyze -s {0} -r {1} -f json -g none -o {2}",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }

        [TestMethod]
        public void DefaultAndCustomRulesPosMatches_Pass()
        {
            string args = string.Format(@"analyze -s {0} -r {1} -f json -g none -o {2}",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCode  = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
            string testContent = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));
            Assert.IsTrue(testContent.Contains("Cryptography.HashAlgorithm.Legacy"));
            Assert.IsTrue(testContent.Contains("Data.Custom1"));
        }

        [TestMethod]
        public void ExclusionFilter_Pass()
        {
            string args = string.Format(@"analyze -s {0} -r {1} -f json -o {2} -g {3}",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\project\one"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"),
                "*.cpp");

            var exitCode  = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            
            Assert.AreEqual(AnalyzeResult.ExitCode.NoMatches, exitCode);
        }

        [TestMethod]
        public void MultiFiles()
        {
            var mainduptags = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp");
            string args = string.Format(@"analyze -s {0} -f json -o {1} -g none",
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
            args = string.Format(@"analyze -s {0} -f json -o {1} -g none",
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
            string args = string.Format(@"analyze -s {0} -f json -o {1} -g none --single-threaded",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCodeSingleThread = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCodeSingleThread);

            string content = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));
            var result = JsonConvert.DeserializeObject<AnalyzeResult>(content);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueMatchesCount);
            
            args = string.Format(@"analyze -s {0} -f json -o {1} -g none",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCodeMultiThread = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCodeMultiThread);

            content = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));
            result = JsonConvert.DeserializeObject<AnalyzeResult>(content);
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void NoMatchesFound_Pass()
        {
            string args = string.Format(@"analyze -s {0} -f json -o {1} -g none",
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.NoMatches, exitCode);
        }

        // This test fails because we try to scan the log file. It is already open for logging so it will throw us an error in the logs - but continue scanning other files. In this case we get no matches because it is the only file.
        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            string args = string.Format(@"analyze -s {0} -f json -l {1} -g none --no-show-progress",
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp"));

            var exitCode  = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.NoMatches, exitCode);
        }

        [TestMethod]
        public void NoConsoleNoFileOutput_Pass()
        {
            string logPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"log.txt");
            string codePath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp");
            string appInspectorPath = TestHelpers.GetPath(TestHelpers.AppPath.appInspectorCLI);
            string args = ($"analyze -s {codePath} --disable-log-file --disable-console -l {logPath} --no-show-progress");

            var exitCode = (AnalyzeResult.ExitCode)TestHelpers.RunProcess(appInspectorPath, args, out string testContent);
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
            Assert.IsTrue(string.IsNullOrEmpty(testContent));
            Assert.IsTrue(string.IsNullOrEmpty(File.ReadAllText(logPath)));
        }
    }
}