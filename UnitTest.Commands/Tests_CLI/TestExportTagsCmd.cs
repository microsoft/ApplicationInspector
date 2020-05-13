using ApplicationInspector.Unitprocess.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.IO;

namespace ApplicationInspector.Unitprocess.CLICommands
{
    [TestClass]
    public class CLITestExportTagsCmd
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
        public void BasicTxtOutput_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -f text -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void BasicJsonOutput_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -f json -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void ExportHTML_Fail()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -f html -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void ExportUnknownFormat_Fail()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -f unknown -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -i -f text -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -i -r {0} -f text  -l {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -r {0} -f text -l {1}",
                  Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                  Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void ExportNoRules_Fail()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -i -f text -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void ExportToTextFilePath_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -o {0} -f text -l {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                if (exitCode == ExportTagsResult.ExitCode.Success)
                {
                    if (!File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt")) ||
                        new FileInfo(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt")).Length == 0)
                    {
                        exitCode = ExportTagsResult.ExitCode.CriticalError;
                    }
                }
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void ExportToJsonFilePath_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -o {0} -f json -v trace -l {1}",
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                if (File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json")))
                {
                    File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"));
                }

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                if (exitCode == ExportTagsResult.ExitCode.Success)
                {
                    string content = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"));
                    var result = JsonConvert.DeserializeObject<ExportTagsResult>(content);
                    exitCode = result.TagsList.Count > 0 ? ExportTagsResult.ExitCode.Success : ExportTagsResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void ExportToBadOutFilePath_Fail()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -o {0} -f text -l {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\baddir\exporttags.txt"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -o {0} -f json -l {1} -v trace ",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"),
                     Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                exitCode = testContent.ToLower().Contains("trace") ? ExportTagsResult.ExitCode.Success : ExportTagsResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -o {0} -f json -l {1} -v error",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\baddir\exporttags.txt"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                exitCode = testContent.ToLower().Contains("error") ? ExportTagsResult.ExitCode.Success : ExportTagsResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -o {0} -r {1} -f json -l {2} -v debug",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"mybadrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                exitCode = testContent.ToLower().Contains("debug") ? ExportTagsResult.ExitCode.Success : ExportTagsResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -o {0} -f json -l {1} ",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"badir\log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }

        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -f json -l {0}",
                     Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = string.Format(@"exporttags -x none -f text -o {0}",
                         Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"));

                exitCode = (ExportTagsResult.ExitCode)Helper.RunProcess(appInspectorPath, args, out string testContent);

                if (exitCode == ExportTagsResult.ExitCode.Success)
                {
                    exitCode = String.IsNullOrEmpty(testContent) ? ExportTagsResult.ExitCode.Success : ExportTagsResult.ExitCode.CriticalError;
                }
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }

        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string args = string.Format(@"exporttags -x none -f text -l {0}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                exitCode = (ExportTagsResult.ExitCode)Microsoft.ApplicationInspector.CLI.Program.Main(args.Split(' '));
            }
            catch (Exception)
            {
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);
        }
    }
}