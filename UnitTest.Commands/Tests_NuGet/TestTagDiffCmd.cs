namespace ApplicationInspector.Unitprocess.Commands
{
    using ApplicationInspector.Unitprocess.Misc;
    using Microsoft.ApplicationInspector.CLI;
    using Microsoft.ApplicationInspector.Commands;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public class TestTagDiffCmd
    {
        private ILoggerFactory loggerFactory;
        [TestInitialize]
        public void InitOutput()
        {
            Directory.CreateDirectory(TestHelpers.GetPath(TestHelpers.AppPath.testOutput));
            loggerFactory = TestHelpers.GenerateLoggerFactory();
        }

        [TestCleanup]
        public void CleanUp()
        {
            loggerFactory.Dispose();
            Directory.Delete(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), true);
        }


        [TestMethod]
        public void Equality_Pass()
        {
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\maincopy.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };

            TagDiffCommand command = new(options, loggerFactory);
            TagDiffResult result = command.GetResult();

            Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, result.ResultCode);
        }

        [TestMethod]
        public void Equality_Fail()
        {
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainx.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;

            TagDiffCommand command = new(options, loggerFactory);
            TagDiffResult result = command.GetResult();
            exitCode = result.ResultCode;

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void BasicZipReadDiff_Pass()
        {
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"zipped\mainx.zip") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainx.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new(options, loggerFactory);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void InEquality_Pass()
        {
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainx.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                TestType = "Inequality"
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;

            try
            {
                TagDiffCommand command = new(options, loggerFactory);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void InEquality_Fail()
        {
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\maincopy.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                TestType = "Inequality"
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new(options, loggerFactory);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void OneSrcResult_Fail()
        {
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\empty.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new(options, loggerFactory);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
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
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\nofilehere.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainx.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new(options, loggerFactory);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
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
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\empty.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\blank.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new(options, loggerFactory);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
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
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainx.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                IgnoreDefaultRules = true
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new(options, loggerFactory);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
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
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\maincopy.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                IgnoreDefaultRules = false,
                CustomRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
            };

            TagDiffCommand command = new(options, loggerFactory);
            TagDiffResult result = command.GetResult();

            Assert.AreEqual(TagDiffResult.ExitCode.TestPassed, result.ResultCode);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Pass()
        {
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\maincopy.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                IgnoreDefaultRules = true,
                CustomRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new(options, loggerFactory);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }

        [TestMethod]
        public void DefaultWithCustomRules_Fail()
        {
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\mainx.cpp") },
                FilePathExclusions = Array.Empty<string>(), //allow source under unittest path
                CustomRulesPath = Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testRules), @"myrule.json"),
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                TagDiffCommand command = new(options, loggerFactory);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.CriticalError;
            }

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestFailed);
        }

        [TestMethod]
        public void NoConsoleOutput_Pass()
        {
            TagDiffOptions options = new()
            {
                SourcePath1 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\main.cpp") },
                SourcePath2 = new string[] { Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testSource), @"unzipped\simple\maincopy.cpp") },
                FilePathExclusions = Array.Empty<string>(),
            };

            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;
            try
            {
                // Attempt to open output file.
                using var writer = new StreamWriter(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"consoleout.txt"));
                // Redirect standard output from the console to the output file.
                Console.SetOut(writer);

                TagDiffCommand command = new(options, loggerFactory);
                TagDiffResult result = command.GetResult();
                exitCode = result.ResultCode;
                try
                {
                    string testContent = File.ReadAllText(Path.Combine(TestHelpers.GetPath(TestHelpers.AppPath.testOutput), @"consoleout.txt"));
                    if (string.IsNullOrEmpty(testContent))
                    {
                        exitCode = TagDiffResult.ExitCode.TestPassed;
                    }
                    else
                    {
                        exitCode = TagDiffResult.ExitCode.TestFailed;
                    }
                }
                catch (Exception)
                {
                    exitCode = TagDiffResult.ExitCode.TestPassed;//no console output file found
                }
            }
            catch (Exception)
            {
                exitCode = TagDiffResult.ExitCode.TestPassed;
            }

            //reset to normal
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            Assert.IsTrue(exitCode == TagDiffResult.ExitCode.TestPassed);
        }
    }
}