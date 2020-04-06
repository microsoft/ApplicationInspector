using ApplicationInspector.UnitTest.Misc;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace ApplicationInspector.UnitTest.CLICommands
{

    [TestClass]
    public class CLITestExportTagsCmd
    {

        [TestMethod]
        public void BasicTxtOutput_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -f text -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;

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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -f json -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;

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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -f html -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;
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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -f unknown -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;
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
            ExportTagsOptions options = new ExportTagsOptions()
            {
                IgnoreDefaultRules = true
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -i -f text -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;
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
            ExportTagsOptions options = new ExportTagsOptions()
            {
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json")
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -i -r {0} -f text  -l {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;
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
            ExportTagsOptions options = new ExportTagsOptions()
            {
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -r {0} -f text -l {1}",
                  Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                  Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;
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
            ExportTagsOptions options = new ExportTagsOptions()
            {
                IgnoreDefaultRules = true
            };

            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -i -f text -l {0}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;
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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -o {0} -f text -l {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;

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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -o {0} -f json -v trace -l {1}",
                //@"c:\temp\doit.json", @"c:\temp\dolog.txt");
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"),
                Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                if (File.Exists(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json")))
                    File.Delete(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.json"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;

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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -o {0} -f text -l {1}",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\baddir\exporttags.txt"),
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;
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
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -o {0} -f json -l {1} -v trace ",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"),
                     Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                exitCode = testContent.ToLower().Contains("trace") ? ExportTagsResult.ExitCode.Success : ExportTagsResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
                exitCode = ExportTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }



        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -o {0} -f json -l {1} -v error",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"\baddir\exporttags.txt"),
                     Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                exitCode = testContent.ToLower().Contains("error") ? ExportTagsResult.ExitCode.Success : ExportTagsResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
                exitCode = ExportTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }



        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -o {0} -r {1} -f json -l {2} -v debug",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"),
                     Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"mybadrule.json"),
                     Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;

                string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));
                exitCode = testContent.ToLower().Contains("debug") ? ExportTagsResult.ExitCode.Success : ExportTagsResult.ExitCode.CriticalError;
            }
            catch (Exception)
            {
                exitCode = ExportTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }



        [TestMethod]
        public void InvalidLogPath_Fail()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -o {0} -f json -l {1} ",
                    Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"),
                     Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"badir\log.txt"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;

            }
            catch (Exception)
            {
                exitCode = ExportTagsResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }




        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -f json -l {0}",
                     Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"));

                Process test = new Process();
                test.StartInfo.FileName = appInspectorPath;
                test.StartInfo.Arguments = args;
                bool started = test.Start();
                test.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)test.ExitCode;

            }
            catch (Exception)
            {
                exitCode = ExportTagsResult.ExitCode.CriticalError;
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
                string args = String.Format(@"exporttags -x none -f text -o {0}",
                         Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"exporttags.txt"));

                Process p = new Process();
                p.StartInfo.FileName = appInspectorPath;
                p.StartInfo.Arguments = args;
                p.StartInfo.RedirectStandardOutput = true;
                bool started = p.Start();

                string testContent = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)p.ExitCode;

                if (exitCode == ExportTagsResult.ExitCode.Success)
                {
                    exitCode = String.IsNullOrEmpty(testContent) ? ExportTagsResult.ExitCode.Success : ExportTagsResult.ExitCode.CriticalError;
                }

            }
            catch (Exception)
            {

            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.Success);
        }


        [TestMethod]
        public void NoConsoleNoFileOutput_Fail()
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;
            try
            {
                string appInspectorPath = Helper.GetPath(Helper.AppPath.appInspectorCLI);
                string args = String.Format(@"exporttags -x none -f text -l {0}",
                        Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"log.txt"));

                Process p = new Process();
                p.StartInfo.FileName = appInspectorPath;
                p.StartInfo.Arguments = args;
                p.StartInfo.RedirectStandardOutput = true;
                bool started = p.Start();

                string testContent = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                exitCode = (ExportTagsResult.ExitCode)p.ExitCode;

            }
            catch (Exception)
            {

            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == ExportTagsResult.ExitCode.CriticalError);
        }

    }
}
