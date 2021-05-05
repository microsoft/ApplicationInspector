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
    public class CLITestGetTagsCommand
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
        public void UnknownFormat_Fail()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -b -s {0} -f unknown -k none", Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"));
                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidOutputfilePath_Fail()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -f json -k none -o {1}",
                     Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                     Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"badir\output.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -f json -k none -o {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfilepath.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidRulesPath_Fail()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -r badrulespath -f json -k none -o {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -i -f json -k none -o {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -i -r {1} -f json -k none -o {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -r {1} -f json -k none -o {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultAndCustomRulesPosMatches_Pass()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -r {1} -f json -k none -o {2}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                exitCode = testContent.Contains("Data.Parsing.JSON") ? GetTagsResult.ExitCode.Success : exitCode;
                exitCode = testContent.Contains("Data.Custom1") ? GetTagsResult.ExitCode.Success : exitCode;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void ExclusionFilter_Pass()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -r {1} -f json -o {2} -k {3}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\project\one"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"),
                    ".cpp");

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public void ExpectedTagCountDupsAllowed_Pass()
        {
            GetTagsResult.ExitCode exitCodeSingleThread = GetTagsResult.ExitCode.CriticalError;

            try
            {
                string args = string.Format(@"get-tags -s {0} -f json -o {1} -k none --single-threaded",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCodeSingleThread = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                if (exitCodeSingleThread == GetTagsResult.ExitCode.Success)
                {
                    string content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                    var result = JsonConvert.DeserializeObject<GetTagsResult>(content);
                    exitCodeSingleThread = result.Metadata.TotalMatchesCount == 0 && result.Metadata.UniqueMatchesCount == 0 && result.Metadata.UniqueTags.Count == 7 ? GetTagsResult.ExitCode.Success : GetTagsResult.ExitCode.NoMatches;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCodeSingleThread == GetTagsResult.ExitCode.Success);

            GetTagsResult.ExitCode exitCodeMultiThread = GetTagsResult.ExitCode.CriticalError;

            try
            {

                string args = string.Format(@"get-tags -s {0} -f json -o {1} -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\mainduptags.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCodeMultiThread = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                if (exitCodeMultiThread == GetTagsResult.ExitCode.Success)
                {
                    string content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));
                    var result = JsonConvert.DeserializeObject<GetTagsResult>(content);
                    exitCodeMultiThread = result.Metadata.TotalMatchesCount == 0 && result.Metadata.UniqueMatchesCount == 0 && result.Metadata.UniqueTags.Count == 7 ? GetTagsResult.ExitCode.Success : GetTagsResult.ExitCode.NoMatches;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCodeMultiThread == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void NoMatchesFound_Pass()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -f json -o {1} -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.NoMatches);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -f json -l {1} -v trace -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log1.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log1.txt"));
                exitCode = testContent.ToLower().Contains("trace") ? GetTagsResult.ExitCode.Success : GetTagsResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -f json -l {1} -v error -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\nofile.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log2.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log2.txt"));
                exitCode = testContent.ToLower().Contains("error") ? GetTagsResult.ExitCode.Success : GetTagsResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -f json -l {1} -v debug -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log3.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log3.txt"));
                exitCode = testContent.ToLower().Contains("debug") ? GetTagsResult.ExitCode.Success : GetTagsResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -f json -l {1} -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfile.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"badir\log.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -f json -l {1} -k none",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);

                string args = string.Format(@"get-tags -s {0} -x none -f text -k none -o {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));


                exitCode = (GetTagsResult.ExitCode)Helper.RunProcess(appInspectorPath, args, out string testContent);

                if (exitCode == GetTagsResult.ExitCode.Success)
                {
                    exitCode = String.IsNullOrEmpty(testContent) ? GetTagsResult.ExitCode.Success : GetTagsResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            GetTagsResult.ExitCode exitCode = GetTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"get-tags -s {0} -x none -f text -k none -l {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (GetTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == GetTagsResult.ExitCode.CriticalError);
        }
    }
}