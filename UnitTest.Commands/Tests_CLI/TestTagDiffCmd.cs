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

            //because these are static and each test is meant to be indpendent null assign the references to create the log
            WriteOnce.Log = null;
            Utils.Logger = null;
        }

        [TestMethod]
        public void Equality_Pass()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void Equality_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void ZipReadDiff_Pass()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\mainx.zip"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void InEquality_Pass()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -t inequality -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void InEquality_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -t inequality -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void OneSrcResult_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfile.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoResults_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\blank.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -i -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -i -r {2} -g none -l {3}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -r {2} -g none -l {3}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void TagdiffToTextFilePath_Pass()
        {
            try
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

                if ((TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')) == TagDiffResult.ExitCode.TestFailed)//looking for diff list
                {
                    if (!File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")) ||
                        new FileInfo(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")).Length == 0)
                    {
                        Assert.Fail();
                    }
                }
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TagdiffToJsonFilePath_Pass()
        {
            try
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

                if ((TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')) == TagDiffResult.ExitCode.TestPassed)
                {
                    string content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"));
                    var result = JsonConvert.DeserializeObject<TagDiffResult>(content);
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.TagDiffList.Count > 0);
                }
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TagdiffToUnknownFormatFilePath_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
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

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void TagdiffToOutFilePath_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2} -o {3}",
                     Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                     Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                     Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\badir\tagdiffout.txt"),
                     Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -v trace -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                Assert.IsTrue(testContent.ToLower().Contains("trace"));
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfilepath.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                Assert.IsTrue(testContent.ToLower().Contains("error"));
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            try
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
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\baddir\log.txt"));

                Assert.AreEqual(TagDiffResult.ExitCode.CriticalError,(TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\blank.cpp"));

                Assert.AreEqual(TagDiffResult.ExitCode.CriticalError, (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' ')));
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);

                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -x none -f text -g none -o {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, (TagDiffResult.ExitCode)Helper.RunProcess(appInspectorPath, args, out string testContent));
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -x none -f text -g none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }
    }
}