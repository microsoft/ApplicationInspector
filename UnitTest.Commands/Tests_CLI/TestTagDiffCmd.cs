namespace ApplicationInspector.Unitprocess.CLICommands
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
    public class CLITestTagDiffCmd
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
        }

        [TestMethod]
        public void Equality_Pass()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void Equality_Fail()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.TestFailed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void ZipReadDiff_Pass()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\mainx.zip"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void InEquality_Pass()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -t inequality -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void InEquality_Fail()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -t inequality -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.TestFailed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void OneSrcResult_Fail()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.CriticalError, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfile.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.CriticalError, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void NoResults_Fail()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\blank.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.CriticalError, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -i -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.CriticalError, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -i -r {2} -g none -l {3}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -r {2} -g none -l {3}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void TagdiffToTextFilePath_Pass()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -f text -g none -o {2} -l {3}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            if (File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")))
            {
                File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
            }

            Assert.AreEqual(TagDiffResult.ExitCode.TestFailed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
            if (!File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")) ||
                new FileInfo(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")).Length == 0)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TagdiffToJsonFilePath_Pass()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -f json -g none -o {2} -l {3}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            if (File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json")))
            {
                File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"));
            }

            Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
            string content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"));
            var result = JsonConvert.DeserializeObject<TagDiffResult>(content);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TagdiffToUnknownFormatFilePath_Fail()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -f unknown -g none -o {2} -l {3}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            if (File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json")))
            {
                File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"));
            }
            Assert.AreEqual(TagDiffResult.ExitCode.CriticalError, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void TagdiffToOutFilePath_Fail()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2} -o {3}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\badir\tagdiffout.txt"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));


            Assert.AreEqual(TagDiffResult.ExitCode.CriticalError, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -v trace -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.TestFailed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
            string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
            Assert.IsTrue(testContent.ToLower().Contains("trace"));
        }

        [TestMethod]
        public void LogErrorLevel_Fail()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfilepath.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
            Assert.AreEqual(TagDiffResult.ExitCode.CriticalError, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
            string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
            Assert.IsTrue(testContent.ToLower().Contains("error"));
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -r {2} -g none -v debug -l {3}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\baddir\mainx.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"mybadrule.json"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.CriticalError, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
            string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
            Assert.IsTrue(testContent.ToLower().Contains("debug"));
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\baddir\log.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.CriticalError,(TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\blank.cpp"));

                Assert.AreEqual(TagDiffResult.ExitCode.CriticalError, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);

            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -x none -f text -g none -o {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

            Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, (TagDiffResult.ExitCode)Helper.RunProcess(appInspectorPath, args, out string testContent));
            Assert.AreEqual(string.Empty, testContent);
        }

        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -x none -f text -g none -l {2}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
            Assert.AreEqual(TagDiffResult.ExitCode.CriticalError, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
        }
    }
}