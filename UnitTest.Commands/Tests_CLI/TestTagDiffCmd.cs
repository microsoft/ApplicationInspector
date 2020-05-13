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
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -l {2}",
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
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -l {2}",
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
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -l {2}",
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
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -t inequality -k none -l {2}",
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
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -t inequality -k none -l {2}",
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
        public void SameSrcFile_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
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
        public void OneSrcResult_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -l {2}",
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
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -l {2}",
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
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -l {2}",
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
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -i -k none -l {2}",
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
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -i -r {2} -k none -l {3}",
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
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -r {2} -k none -l {3}",
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
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -f text -k none -o {2} -l {3}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                if (File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")))
                {
                    File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                }

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                if (exitCode == TagDiffResult.ExitCode.TestFailed)//looking for diff list
                {
                    if (!File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")) ||
                        new FileInfo(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")).Length == 0)
                    {
                        exitCode = TagDiffResult.ExitCode.CriticalError;
                    }
                }
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void TagdiffToJsonFilePath_Pass()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -f json -k none -o {2} -l {3}",
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                   Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                if (File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json")))
                {
                    File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"));
                }

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                if (exitCode == TagDiffResult.ExitCode.TestPassed)
                {
                    string content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"));
                    var result = JsonConvert.DeserializeObject<TagDiffResult>(content);
                    exitCode = result.TagDiffList.Count > 0 ? TagDiffResult.ExitCode.TestPassed : TagDiffResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void TagdiffToUnknownFormatFilePath_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -f unknown -k none -o {2} -l {3}",
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
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -l {2} -o {3}",
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
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -v trace -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                exitCode = testContent.ToLower().Contains("trace") ? TagDiffResult.ExitCode.TestPassed : TagDiffResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfilepath.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                exitCode = testContent.ToLower().Contains("error") ? TagDiffResult.ExitCode.TestPassed : TagDiffResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -r {2} -k none -v debug -l {3}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\baddir\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"mybadrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
                if (exitCode == TagDiffResult.ExitCode.CriticalError)
                {
                    string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                    exitCode = testContent.ToLower().Contains("debug") ? TagDiffResult.ExitCode.TestPassed : TagDiffResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainx.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\baddir\log.txt"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -k none -l {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\blank.cpp"));

                exitCode = (TagDiffResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);

                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -x none -f text -k none -o {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\maincopy.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (TagDiffResult.ExitCode)Helper.RunProcess(appInspectorPath, args, out string testContent);

                if (exitCode == TagDiffResult.ExitCode.TestPassed)
                {
                    exitCode = String.IsNullOrEmpty(testContent) ? TagDiffResult.ExitCode.TestPassed : TagDiffResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"tagdiff --src1 {0} --src2 {1} -x none -f text -k none -l {2}",
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