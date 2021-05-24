using ApplicationInspector.Unitprocess.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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
    public class CLITestAnalyzeCmd
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
        public void BasicHTMLOutput_Pass()
        {
            string args = string.Format(@"analyze -s {0} -f html -k none", Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"));
            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }


        [TestMethod]
        public void UnknownFormat_Fail() //dupliacte tags not supported for html format
        {
            string args = string.Format(@"analyze -b -s {0} -f unknown -k none", Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"));
            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }

        [TestMethod]
        public void ZipReadHTMLOutput_Pass()
        {
            string args = string.Format(@"analyze -s {0} -f html -k none -l {1}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\mainx.zip"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }

        [TestMethod]
        public void InvalidOutputfilePath_Fail()
        {
            string args = string.Format(@"analyze -s {0} -f json -k none -o {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"badir\output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            string args = string.Format(@"analyze -s {0} -f json -k none -o {1}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfilepath.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void InvalidRulesPath_Fail()
        {            
            string args = string.Format(@"analyze -s {0} -r badrulespath -f json -k none -o {1}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            

            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            string args = string.Format(@"analyze -s {0} -i -f json -k none -o {1}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            string args = string.Format(@"analyze -s {0} -i -r {1} -f json -k none -o {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            string args = string.Format(@"analyze -s {0} -r {1} -f json -k none -o {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }

        [TestMethod]
        public void DefaultAndCustomRulesPosMatches_Pass()
        {
            string args = string.Format(@"analyze -s {0} -r {1} -f json -k none -o {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            var exitCode  = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
            string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
            Assert.IsTrue(testContent.Contains("Data.Parsing.JSON"));
            Assert.IsTrue(testContent.Contains("Data.Custom1"));
        }

        [TestMethod]
        public void ExclusionFilter_Pass()
        {
            string args = string.Format(@"analyze -s {0} -r {1} -f json -o {2} -k {3}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\project\one"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"),
                "*.cpp");

            var exitCode  = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            
            Assert.AreEqual(AnalyzeResult.ExitCode.NoMatches, exitCode);
        }

        [TestMethod]
        public void MultiFiles()
        {
            var mainduptags = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp");
            string args = string.Format(@"analyze -s {0} -f json -o {1} -k none",
                $"{mainduptags}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            var exitCodeSingleThread = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCodeSingleThread);
                
            string content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
            var result = JsonConvert.DeserializeObject<AnalyzeResult>(content);
            var matches = result.Metadata.TotalMatchesCount;
            var uniqueMatches = result.Metadata.UniqueMatchesCount;
            mainduptags = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp");
            args = string.Format(@"analyze -s {0} -f json -o {1} -k none",
                $"{mainduptags},{mainduptags}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            exitCodeSingleThread = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCodeSingleThread);

            content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
            var result2 = JsonConvert.DeserializeObject<AnalyzeResult>(content);

            Assert.AreEqual(matches * 2, result2.Metadata.TotalMatchesCount);
            Assert.AreEqual(uniqueMatches, result2.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void ExpectedTagCountDupsAllowed_Pass()
        {
            string args = string.Format(@"analyze -s {0} -f json -o {1} -k none --single-threaded",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            var exitCodeSingleThread = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCodeSingleThread);

            string content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
            var result = JsonConvert.DeserializeObject<AnalyzeResult>(content);
                
            Assert.AreEqual(11, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueMatchesCount);
            
            args = string.Format(@"analyze -s {0} -f json -o {1} -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            var exitCodeMultiThread = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCodeMultiThread);

            content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
            result = JsonConvert.DeserializeObject<AnalyzeResult>(content);
            Assert.AreEqual(11, result.Metadata.TotalMatchesCount);
            Assert.AreEqual(7, result.Metadata.UniqueMatchesCount);
        }

        [TestMethod]
        public void NoMatchesFound_Pass()
        {
            string args = string.Format(@"analyze -s {0} -f json -o {1} -k none",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

            Assert.AreEqual(AnalyzeResult.ExitCode.NoMatches, exitCode);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            string args = string.Format(@"analyze -s {0} -f json -l {1} -v trace -k none",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log1.txt"));

            var exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);

            string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log1.txt"));
            Assert.IsTrue(testContent.Contains("trace", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            string args = string.Format(@"analyze -s {0} -f json -l {1} -v error -k none",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\nofile.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log2.txt"));

            var exitCode  = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
            string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log2.txt"));
            Assert.IsTrue(testContent.Contains("error", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            string args = string.Format(@"analyze -s {0} -f json -l {1} -v debug -k none",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log3.txt"));

            var exitCode  = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
            string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log3.txt"));
            Assert.IsTrue(testContent.Contains("debug", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;
            string args = string.Format(@"analyze -s {0} -f json -l {1} -k none",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfile.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"badir\log.txt"));

            exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            
            Assert.AreEqual(AnalyzeResult.ExitCode.CriticalError, exitCode);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            string args = string.Format(@"analyze -s {0} -f json -l {1} -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"));

            var exitCode  = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);

            string args = string.Format(@"analyze -s {0} -x none -f text -k none -o {1} --no-show-progress",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));


            var exitCode = (AnalyzeResult.ExitCode)Helper.RunProcess(appInspectorPath, args, out string testContent);
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
            Assert.IsTrue(string.IsNullOrEmpty(testContent));
        }

        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            string args = string.Format(@"analyze -s {0} -x none -f text -k none -l {1} --no-show-progress",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            var  exitCode = (AnalyzeResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            Assert.AreEqual(AnalyzeResult.ExitCode.Success, exitCode);
        }
    }
}