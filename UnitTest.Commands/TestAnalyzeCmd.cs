using Microsoft.ApplicationInspector.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ApplicationInspector.UnitTest.Commands
{
    /// <summary>
    /// Test class for Analyze Commands
    /// Each method really needs to be complete i.e. options and command objects created and checked for exceptions etc. based on inputs so 
    /// doesn't create a set of shared objects
    /// 
    /// </summary>
    [TestClass]
    public class TestAnalyzeCmd
    {
        [TestMethod]
        public void BasicAnalyzeHTMLOut_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void SimpleTagsHTMLOut_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SimpleTagsOnly = true,
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }

        [TestMethod]
        public void DupTagsHTMLOut_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                AllowDupTags = true,
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }

        [TestMethod]
        public void BasicZipRead_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"zipped\mainx.zip"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void SimpleTagsTextOut_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true,
                SimpleTagsOnly = true,
                OutputFileFormat = "text"
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                string contentResult = command.GetResult();
                if (String.IsNullOrEmpty(contentResult))
                {
                    exitCode = AnalyzeCommand.ExitCode.NoMatches;
                }
                else if (contentResult.Contains("Authentication.General"))
                {
                    exitCode = AnalyzeCommand.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }

        [TestMethod]
        public void SimpleTagsJsonOut_JSSrc_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\onetag.js"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true,
                SimpleTagsOnly = true,
                OutputFileFormat = "json"
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                string contentResult = command.GetResult();
                if (String.IsNullOrEmpty(contentResult))
                {
                    exitCode = AnalyzeCommand.ExitCode.NoMatches;
                }
                else if (contentResult.Contains("Data.Parsing.JSON"))
                {
                    exitCode = AnalyzeCommand.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void SimpleTagsJsonOut_CPPSrc_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true,
                SimpleTagsOnly = true,
                OutputFileFormat = "json"
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                string contentResult = command.GetResult();
                if (String.IsNullOrEmpty(contentResult))
                {
                    exitCode = AnalyzeCommand.ExitCode.NoMatches;
                }
                else if (contentResult.Contains("Authentication.General"))
                {
                    exitCode = AnalyzeCommand.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }



        [TestMethod]
        public void InvalidOutfilePath_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                OutputFileFormat = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"badir\noway"),
                SuppressBrowserOpen = false
            };
            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }

        [TestMethod]
        public void InvalidSourcePath_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"baddir\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = false
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void InvalidRulesPath_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"notfound.json"),
                SuppressBrowserOpen = false
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void NoDefaultNoCustomRules_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = true,
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }


        [TestMethod]
        public void NoDefaultCustomRules_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void DefaultAndCustomRulesMatched_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                CustomRulesPath = Path.Combine(Helper.GetPath(Helper.AppPath.testRules), @"myrule.json"),
                SuppressBrowserOpen = true,
                SimpleTagsOnly = true,
                OutputFileFormat = "text"
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                string contentResult = command.GetResult();
                if (String.IsNullOrEmpty(contentResult))
                {
                    exitCode = AnalyzeCommand.ExitCode.NoMatches;
                }
                else if (contentResult.Contains("Custom1") && contentResult.Contains("Authentication.General")) //from default and custom rules
                {
                    exitCode = AnalyzeCommand.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }



        [TestMethod]
        public void ExclusionFilter_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                //FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true,
                OutputFileFormat = "text"
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                string contentResult = command.GetResult();
                if (String.IsNullOrEmpty(contentResult))
                {
                    exitCode = AnalyzeCommand.ExitCode.NoMatches;
                }
                else if (contentResult.Contains("Data.Parse.JSON")) //from default and custom rules
                {
                    exitCode = AnalyzeCommand.ExitCode.Success;
                }
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.NoMatches);
        }



        [TestMethod]
        public void ExpectedTagCountDupsAllowed_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true,
                AllowDupTags = true,
                OutputFileFormat = "text",
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt")
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
                if (exitCode == AnalyzeCommand.ExitCode.Success)
                {
                    string[] lines = File.ReadAllLines(options.OutputFilePath);
                    List<string> tags = Helper.GetTagsFromFile(lines);
                    exitCode = tags.Count == 34 ? AnalyzeCommand.ExitCode.Success : AnalyzeCommand.ExitCode.NoMatches;
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void ExpectedTagCountNoDupsAllowed_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true,
                AllowDupTags = false,
                SimpleTagsOnly = true,
                OutputFileFormat = "json"
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                string contentResult = command.GetResult();
                if (String.IsNullOrEmpty(contentResult))
                {
                    exitCode = AnalyzeCommand.ExitCode.NoMatches;
                }
                else
                {
                    var file1Tags = JsonConvert.DeserializeObject<TagsFile>(contentResult);
                    exitCode = file1Tags.Tags.Length == 21 ? AnalyzeCommand.ExitCode.Success : AnalyzeCommand.ExitCode.NoMatches;
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }




        [TestMethod]
        public void NoMatchesFound_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.NoMatches);
        }


        [TestMethod]
        public void LogTraceLevel_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true,
                LogFileLevel = "trace",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logtrace.txt"),
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                    exitCode = AnalyzeCommand.ExitCode.CriticalError;
                else if (testLogContent.ToLower().Contains("trace"))
                    exitCode = AnalyzeCommand.ExitCode.Success;

            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }



        [TestMethod]
        public void LogErrorLevel_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\badfile.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true,
                LogFileLevel = "error",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logerror.txt"),
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                command.Run();
            }
            catch (Exception)
            {
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (!String.IsNullOrEmpty(testLogContent) && testLogContent.ToLower().Contains("error"))
                    exitCode = AnalyzeCommand.ExitCode.Success;
                else
                    exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }



        [TestMethod]
        public void LogDebugLevel_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true,
                LogFileLevel = "debug",
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"logdebug.txt"),
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
                string testLogContent = File.ReadAllText(options.LogFilePath);
                if (String.IsNullOrEmpty(testLogContent))
                    exitCode = AnalyzeCommand.ExitCode.CriticalError;
                else if (testLogContent.ToLower().Contains("debug"))
                    exitCode = AnalyzeCommand.ExitCode.Success;

            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }



        [TestMethod]
        [Ignore]
        public void InvalidLogPath_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true,
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"baddir\logdebug.txt"),
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);//test fails even when values match unless this case run individually -mstest bug?
        }




        [TestMethod]
        public void InsecureLogPath_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"nosuchfile.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                SuppressBrowserOpen = true,
                LogFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\main.cpp"),
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }



        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                OutputFileFormat = "text",
                OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"),
                SuppressBrowserOpen = true,
                ConsoleVerbosityLevel = "none"
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                // Attempt to open output file.
                using (var writer = new StreamWriter(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt")))
                {

                    // Redirect standard output from the console to the output file.
                    Console.SetOut(writer);

                    AnalyzeCommand command = new AnalyzeCommand(options);
                    exitCode = (AnalyzeCommand.ExitCode)command.Run();
                    try
                    {
                        string testContent = File.ReadAllText(Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"consoleout.txt"));
                        if (String.IsNullOrEmpty(testContent))
                            exitCode = AnalyzeCommand.ExitCode.Success;
                        else
                            exitCode = AnalyzeCommand.ExitCode.NoMatches;
                    }
                    catch (Exception)
                    {
                        exitCode = AnalyzeCommand.ExitCode.Success;//no console output file found
                    }
                }
            }
            catch (Exception)
            {
                exitCode = AnalyzeCommand.ExitCode.CriticalError;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.Success);
        }


        [TestMethod]
        public void NoOutputSelected_Fail()
        {
            AnalyzeCommandOptions options = new AnalyzeCommandOptions()
            {
                SourcePath = Path.Combine(Helper.GetPath(Helper.AppPath.testSource), @"unzipped\simple\empty.cpp"),
                FilePathExclusions = "none", //allow source under unittest path
                OutputFileFormat = "text",
                SuppressBrowserOpen = true,
                //OutputFilePath = Path.Combine(Helper.GetPath(Helper.AppPath.testOutput), @"output.txt"), 
                ConsoleVerbosityLevel = "none" //together with no output file = no output at all which is a fail
            };

            AnalyzeCommand.ExitCode exitCode = AnalyzeCommand.ExitCode.CriticalError;
            try
            {
                AnalyzeCommand command = new AnalyzeCommand(options);
                exitCode = (AnalyzeCommand.ExitCode)command.Run();
            }
            catch (Exception)
            {
                //check for specific error if desired
            }

            Assert.IsTrue(exitCode == AnalyzeCommand.ExitCode.CriticalError);
        }
    }
}
