//Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using CommandLine;
using Microsoft.ApplicationInspector.Commands;
using NLog;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.ApplicationInspector.CLI
{
    public class Program
    {
        /// <summary>
        /// CLI program entry point which defines command verbs and options to running
        /// </summary>
        /// <param name="args"></param>
        public static int Main(string[] args)
        {
            int finalResult = (int)Utils.ExitCode.CriticalError;

            Utils.CLIExecutionContext = true;//set manually at start from CLI

            WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.Medium;
            try
            {
                var argsResult = Parser.Default.ParseArguments<CLIAnalyzeCmdOptions,
                    CLITagDiffCmdOptions,
                    CLITagTestCmdOptions,
                    CLIExportTagsCmdOptions,
                    CLIVerifyRulesCmdOptions,
                    CLIPackRulesCmdOptions>(args)
                  .MapResult(
                    (CLIAnalyzeCmdOptions cliOptions) => VerifyOutputArgsRun(cliOptions),
                    (CLITagDiffCmdOptions cliOptions) => VerifyOutputArgsRun(cliOptions),
                    (CLITagTestCmdOptions cliOptions) => VerifyOutputArgsRun(cliOptions),
                    (CLIExportTagsCmdOptions cliOptions) => VerifyOutputArgsRun(cliOptions),
                    (CLIVerifyRulesCmdOptions cliOptions) => VerifyOutputArgsRun(cliOptions),
                    (CLIPackRulesCmdOptions cliOptions) => VerifyOutputArgsRun(cliOptions),
                    errs => 1
                  );

                finalResult = argsResult;
            }
            catch (OpException)
            {
                //log, output file and console have already been written to ensure all are updated for NuGet and CLI callers
                //that may exit at different call points
            }
            catch (Exception e)
            {
                //unlogged exception so report out for CLI callers
                WriteOnce.SafeLog(e.Message + "\n" + e.StackTrace, NLog.LogLevel.Error);
            }

            //final exit msg to review log
            if (finalResult == (int)Utils.ExitCode.CriticalError)
            {
                if (!string.IsNullOrEmpty(Utils.LogFilePath))
                {
                    WriteOnce.Info(MsgHelp.FormatString(MsgHelp.ID.RUNTIME_ERROR_UNNAMED, Utils.LogFilePath), true, WriteOnce.ConsoleVerbosity.Low, false);
                }
                else
                {
                    WriteOnce.Info(MsgHelp.GetString(MsgHelp.ID.RUNTIME_ERROR_PRELOG), true, WriteOnce.ConsoleVerbosity.Medium, false);
                }
            }
            else
            {
                if (File.Exists(Utils.LogFilePath))
                {
                    var fileInfo = new FileInfo(Utils.LogFilePath);
                    if (fileInfo.Length > 0)
                    {
                        WriteOnce.Info(MsgHelp.FormatString(MsgHelp.ID.CMD_REMINDER_CHECK_LOG, Utils.LogFilePath??Utils.GetPath(Utils.AppPath.defaultLog)), true, WriteOnce.ConsoleVerbosity.Low, false);
                    }
                }
            }

            return finalResult;
        }

        #region OutputArgsCheckandRun

        //idea is to check output args which are not applicable to NuGet callers before the command operation is run for max efficiency

        private static int VerifyOutputArgsRun(CLITagDiffCmdOptions options)
        {
            Logger logger = Utils.SetupLogging(options, true);
            WriteOnce.Log = logger;
            options.Log = logger;

            CommonOutputChecks(options);
            return RunTagDiffCommand(options);
        }

        private static int VerifyOutputArgsRun(CLITagTestCmdOptions options)
        {
            Logger logger = Utils.SetupLogging(options, true);
            WriteOnce.Log = logger;
            options.Log = logger;

            CommonOutputChecks(options);
            return RunTagTestCommand(options);
        }

        private static int VerifyOutputArgsRun(CLIExportTagsCmdOptions options)
        {
            Logger logger = Utils.SetupLogging(options, true);
            WriteOnce.Log = logger;
            options.Log = logger;

            CommonOutputChecks(options);
            return RunExportTagsCommand(options);
        }

        private static int VerifyOutputArgsRun(CLIVerifyRulesCmdOptions options)
        {
            Logger logger = Utils.SetupLogging(options, true);
            WriteOnce.Log = logger;
            options.Log = logger;

            CommonOutputChecks(options);
            return RunVerifyRulesCommand(options);
        }

        private static int VerifyOutputArgsRun(CLIPackRulesCmdOptions options)
        {
            Logger logger = Utils.SetupLogging(options, true);
            WriteOnce.Log = logger;
            options.Log = logger;

            if (options.RepackDefaultRules && !string.IsNullOrEmpty(options.OutputFilePath)) 
            {
                WriteOnce.Info("output file argument ignored for -d option");
            }

            options.OutputFilePath = options.RepackDefaultRules ? Utils.GetPath(Utils.AppPath.defaultRulesPackedFile) : options.OutputFilePath;
            if (string.IsNullOrEmpty(options.OutputFilePath))
            {
                WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.PACK_MISSING_OUTPUT_ARG));
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.PACK_MISSING_OUTPUT_ARG));
            }
            else
            {
                CommonOutputChecks(options);
            }

            return RunPackRulesCommand(options);
        }

        private static int VerifyOutputArgsRun(CLIAnalyzeCmdOptions options)
        {
            Logger logger = Utils.SetupLogging(options, true);
            WriteOnce.Log = logger;
            options.Log = logger;

            //analyze with html format limit checks
            if (options.OutputFileFormat == "html")
            {
                options.OutputFilePath = options.OutputFilePath ?? "output.html";
                string extensionCheck = Path.GetExtension(options.OutputFilePath);
                if (extensionCheck != ".html" && extensionCheck != ".htm")
                {
                    WriteOnce.Info(MsgHelp.GetString(MsgHelp.ID.ANALYZE_HTML_EXTENSION));
                }

                if (options.AllowDupTags) //fix #183; duplicates results for html format is not supported which causes filedialog issues
                {
                    WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NODUPLICATES_HTML_FORMAT));
                    throw new OpException(MsgHelp.GetString(MsgHelp.ID.ANALYZE_NODUPLICATES_HTML_FORMAT));
                }

                if (options.SimpleTagsOnly) //won't work for html that expects full data for UI
                {
                    WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.ANALYZE_SIMPLETAGS_HTML_FORMAT));
                    throw new Exception(MsgHelp.GetString(MsgHelp.ID.ANALYZE_SIMPLETAGS_HTML_FORMAT));
                }
            }

            CommonOutputChecks((CLICommandOptions)options);
            return RunAnalyzeCommand(options);
        }

        /// <summary>
        /// Checks that either output filepath is valid or console verbosity is not visible to ensure
        /// some output can be achieved...other command specific inputs that are relevant to both CLI
        /// and NuGet callers are checked by the commands themselves
        /// </summary>
        /// <param name="options"></param>
        private static void CommonOutputChecks(CLICommandOptions options)
        {
            //validate requested format
            string fileFormatArg = options.OutputFileFormat;
            string[] validFormats =
            {
                "html",
                "text",
                "json"
            };

            string[] checkFormats;
            if (options is CLIAnalyzeCmdOptions cliAnalyzeOptions)
            {
                checkFormats = validFormats;
                fileFormatArg = cliAnalyzeOptions.OutputFileFormat;
            }
            else if (options is CLIPackRulesCmdOptions cliPackRulesOptions)
            {
                checkFormats = validFormats.Skip(2).Take(1).ToArray();
                fileFormatArg = cliPackRulesOptions.OutputFileFormat;
            }
            else
            {
                checkFormats = validFormats.Skip(1).Take(2).ToArray();
            }

            bool isValidFormat = checkFormats.Any(v => v.Equals(fileFormatArg.ToLower()));
            if (!isValidFormat)
            {
                WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"));
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-f"));
            }

            //validate output is not empty if no file output specified
            if (string.IsNullOrEmpty(options.OutputFilePath))
            {
                if (options.ConsoleVerbosityLevel.ToLower() == "none")
                {
                    WriteOnce.Error(MsgHelp.GetString(MsgHelp.ID.CMD_NO_OUTPUT));
                    throw new Exception(MsgHelp.GetString(MsgHelp.ID.CMD_NO_OUTPUT));
                }
                else if (options.ConsoleVerbosityLevel.ToLower() == "low")
                {
                    WriteOnce.SafeLog("Verbosity set low.  Detailed output limited.", NLog.LogLevel.Info);
                }
            }
            else
            {
                ValidFileWritePath(options.OutputFilePath);
            }
        }

        /// <summary>
        /// Ensure output file path can be written to
        /// </summary>
        /// <param name="filePath"></param>
        private static void ValidFileWritePath(string filePath)
        {
            try
            {
                File.WriteAllText(filePath, "");//verify ability to write to location
            }
            catch (Exception)
            {
                WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, filePath));
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_FILE_OR_DIR, filePath));
            }
        }

        #endregion OutputArgsCheckandRun

        #region RunCmdsWriteResults

        private static int RunAnalyzeCommand(CLIAnalyzeCmdOptions cliOptions)
        {
            AnalyzeResult.ExitCode exitCode = AnalyzeResult.ExitCode.CriticalError;

            AnalyzeCommand command = new AnalyzeCommand(new AnalyzeOptions()
            {
                SourcePath = cliOptions.SourcePath ?? "",
                CustomRulesPath = cliOptions.CustomRulesPath ?? "",
                IgnoreDefaultRules = cliOptions.IgnoreDefaultRules,
                AllowDupTags = cliOptions.AllowDupTags,
                ConfidenceFilters = cliOptions.ConfidenceFilters,
                MatchDepth = cliOptions.MatchDepth,
                FilePathExclusions = cliOptions.FilePathExclusions,
                ConsoleVerbosityLevel = cliOptions.ConsoleVerbosityLevel,
                Log = cliOptions.Log,
                SingleThread = cliOptions.SingleThread
            }); ;

            AnalyzeResult analyzeResult = command.GetResult();
            exitCode = analyzeResult.ResultCode;
            ResultsWriter.Write(analyzeResult, cliOptions);

            return (int)exitCode;
        }

        private static int RunTagDiffCommand(CLITagDiffCmdOptions cliOptions)
        {
            TagDiffResult.ExitCode exitCode = TagDiffResult.ExitCode.CriticalError;

            TagDiffCommand command = new TagDiffCommand(new TagDiffOptions()
            {
                SourcePath1 = cliOptions.SourcePath1 ?? "",
                SourcePath2 = cliOptions.SourcePath2 ?? "",
                CustomRulesPath = cliOptions.CustomRulesPath,
                IgnoreDefaultRules = cliOptions.IgnoreDefaultRules,
                FilePathExclusions = cliOptions.FilePathExclusions,
                ConsoleVerbosityLevel = cliOptions.ConsoleVerbosityLevel,
                TestType = cliOptions.TestType,
                Log = cliOptions.Log
            });

            TagDiffResult tagDiffResult = command.GetResult();
            exitCode = tagDiffResult.ResultCode;
            ResultsWriter.Write(tagDiffResult, cliOptions);

            return (int)exitCode;
        }

        private static int RunTagTestCommand(CLITagTestCmdOptions cliOptions)
        {
            TagTestResult.ExitCode exitCode = TagTestResult.ExitCode.CriticalError;

            TagTestCommand command = new TagTestCommand(new TagTestOptions()
            {
                SourcePath = cliOptions.SourcePath,
                CustomRulesPath = cliOptions.CustomRulesPath,
                FilePathExclusions = cliOptions.FilePathExclusions,
                TestType = cliOptions.TestType,
                ConsoleVerbosityLevel = cliOptions.ConsoleVerbosityLevel,
                Log = cliOptions.Log
            });

            TagTestResult tagTestCommand = command.GetResult();
            exitCode = tagTestCommand.ResultCode;
            ResultsWriter.Write(tagTestCommand, cliOptions);

            return (int)exitCode;
        }

        private static int RunExportTagsCommand(CLIExportTagsCmdOptions cliOptions)
        {
            ExportTagsResult.ExitCode exitCode = ExportTagsResult.ExitCode.CriticalError;

            ExportTagsCommand command = new ExportTagsCommand(new ExportTagsOptions()
            {
                IgnoreDefaultRules = cliOptions.IgnoreDefaultRules,
                CustomRulesPath = cliOptions.CustomRulesPath,
                ConsoleVerbosityLevel = cliOptions.ConsoleVerbosityLevel,
                Log = cliOptions.Log
            });

            ExportTagsResult exportTagsResult = command.GetResult();
            exitCode = exportTagsResult.ResultCode;
            ResultsWriter.Write(exportTagsResult, cliOptions);

            return (int)exitCode;
        }

        private static int RunVerifyRulesCommand(CLIVerifyRulesCmdOptions cliOptions)
        {
            VerifyRulesResult.ExitCode exitCode = VerifyRulesResult.ExitCode.CriticalError;

            VerifyRulesCommand command = new VerifyRulesCommand(new VerifyRulesOptions()
            {
                VerifyDefaultRules = cliOptions.VerifyDefaultRules,
                CustomRulesPath = cliOptions.CustomRulesPath,
                ConsoleVerbosityLevel = cliOptions.ConsoleVerbosityLevel,
                Failfast = cliOptions.Failfast,
                Log = cliOptions.Log
            });

            VerifyRulesResult exportTagsResult = command.GetResult();
            exitCode = exportTagsResult.ResultCode;
            ResultsWriter.Write(exportTagsResult, cliOptions);

            return (int)exitCode;
        }

        private static int RunPackRulesCommand(CLIPackRulesCmdOptions cliOptions)
        {
            PackRulesResult.ExitCode exitCode = PackRulesResult.ExitCode.CriticalError;

            PackRulesCommand command = new PackRulesCommand(new PackRulesOptions()
            {
                RepackDefaultRules = cliOptions.RepackDefaultRules,
                CustomRulesPath = cliOptions.CustomRulesPath,
                ConsoleVerbosityLevel = cliOptions.ConsoleVerbosityLevel,
                Log = cliOptions.Log
            });

            PackRulesResult exportTagsResult = command.GetResult();
            exitCode = exportTagsResult.ResultCode;
            ResultsWriter.Write(exportTagsResult, cliOptions);

            return (int)exitCode;
        }

        #endregion RunCmdsWriteResults
    }
}