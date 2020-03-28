// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.ApplicationInspector.Commands
{

    /// <summary>
    /// Used to test a specific set of rules were all found in target source; Pass/Fail as well as inverse option to test if a set of rules is not 
    /// found in source code
    /// </summary>
    public class TagTestCommand : Command
    {
        public enum ExitCode
        {
            TagsTestSuccess = 0,
            SomeTagsFailed = 1,
            CriticalError = Utils.ExitCode.CriticalError //ensure common value for final exit log mention
        }

        enum TagTestType { RulesPresent, RulesNotPresent }

        string _arg_srcPath;
        string _arg_customRulesPath;
        string _arg_outputFile;
        string _arg_tag_test_type_raw;
        TagTestType _arg_tagTestType;
        RuleSet _rulesSet;
        string _arg_consoleVerbosityLevel;

        /// Compares a set of rules against a source path...
        /// Used for both RulesPresent and RulesNotePresent options
        /// Focus is pass/fail not detailed comparison output -see Tagdiff for more
        public TagTestCommand(TagTestCommandOptions opt)
        {
            _arg_srcPath = opt.SourcePath;
            _arg_customRulesPath = opt.CustomRulesPath;
            _arg_outputFile = opt.OutputFilePath;
            _arg_consoleVerbosityLevel = opt.ConsoleVerbosityLevel ?? "medium";
            _arg_tag_test_type_raw = opt.TestType ?? "RulesPresent";
            _arg_logger = opt.Log;
            _arg_log_file_path = opt.LogFilePath;
            _arg_log_level = opt.LogFileLevel;
            _arg_close_log_on_exit = Utils.CLIExecutionContext ? true : opt.CloseLogOnCommandExit;

            _arg_logger ??= Utils.SetupLogging(opt);
            WriteOnce.Log ??= _arg_logger;
            _rulesSet = new RuleSet(_arg_logger);

            try
            {
                ConfigureConsoleOutput();
                ConfigureCompareTest();
                ConfigureRules();
            }
            catch (Exception e) //group error handling
            {
                WriteOnce.Error(e.Message);
                if (_arg_close_log_on_exit)
                {
                    Utils.Logger = null;
                    WriteOnce.Log = null;
                }
                throw e;
            }
        }


        #region configure


        /// <summary>
        /// Establish console verbosity
        /// For NuGet DLL use, console is muted overriding any arguments sent
        /// </summary>
        void ConfigureConsoleOutput()
        {
            WriteOnce.SafeLog("TagTestCommand::ConfigureConsoleOutput", LogLevel.Trace);

            //Set console verbosity based on run context (none for DLL use) and caller arguments
            if (!Utils.CLIExecutionContext)
                WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.None;
            else
            {
                WriteOnce.ConsoleVerbosity verbosity = WriteOnce.ConsoleVerbosity.Medium;
                if (!Enum.TryParse(_arg_consoleVerbosityLevel, true, out verbosity))
                {
                    WriteOnce.Error(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-x"));
                    throw new Exception(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-x"));
                }
                else
                    WriteOnce.Verbosity = verbosity;
            }
        }


        void ConfigureFileOutput()
        {
            WriteOnce.SafeLog("TagTestCommand::ConfigOutput", LogLevel.Trace);

            WriteOnce.FlushAll();

            //setup output                       
            TextWriter outputWriter;
            if (!string.IsNullOrEmpty(_arg_outputFile))
            {
                outputWriter = File.CreateText(_arg_outputFile);
                outputWriter.WriteLine(Utils.GetVersionString());
                WriteOnce.Writer = outputWriter;
            }
        }


        void ConfigureCompareTest()
        {
            if (!Enum.TryParse(_arg_tag_test_type_raw, true, out _arg_tagTestType))
            {
                WriteOnce.Error((ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, _arg_tag_test_type_raw)));
                throw new Exception(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, _arg_tag_test_type_raw));
            }
        }


        public void ConfigureRules()
        {
            WriteOnce.SafeLog("TagTestCommand::ConfigRules", LogLevel.Trace);

            if (string.IsNullOrEmpty(_arg_customRulesPath))
            {
                WriteOnce.Error(ErrMsg.GetString(ErrMsg.ID.CMD_NORULES_SPECIFIED));
                throw new Exception(ErrMsg.GetString(ErrMsg.ID.CMD_NORULES_SPECIFIED));
            }

            List<string> rulePaths = new List<string>();

            if (!string.IsNullOrEmpty(_arg_customRulesPath))
                rulePaths.Add(_arg_customRulesPath);

            foreach (string rulePath in rulePaths)
            {
                if (Directory.Exists(rulePath))
                    _rulesSet.AddDirectory(rulePath);
                else if (File.Exists(rulePath))
                    _rulesSet.AddFile(rulePath);
                else
                {
                    throw new Exception(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_RULE_PATH, rulePath));
                }
            }

            //error check based on ruleset not path enumeration
            if (_rulesSet.Count() == 0)
            {
                throw new Exception(ErrMsg.GetString(ErrMsg.ID.CMD_NORULES_SPECIFIED));
            }

        }

        #endregion



        /// <summary>
        /// Option for DLL use as alternate to Run which only outputs a file to return results as string
        /// CommandOption defaults will not have been set when used as DLL via CLI processing so some checks added
        /// </summary>
        /// <returns>output results</returns>
        public override string GetResult()
        {
            _arg_outputFile = Path.GetTempFileName();
            if ((int)ExitCode.CriticalError != Run())
            {
                return File.ReadAllText(_arg_outputFile);
            }

            return string.Empty;
        }


        /// <summary>
        /// Main entry from CLI
        /// </summary>
        /// <returns></returns>
        public override int Run()
        {
            WriteOnce.SafeLog("TagTestCommand::Run", LogLevel.Trace);
            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_RUNNING, "tagtest"));

            //init based on true or false present argument value
            ExitCode exitCode = ExitCode.CriticalError;
            WriteOnce.ConsoleVerbosity saveVerbosity = WriteOnce.Verbosity;
            AnalyzeCommand.ExitCode analyzeCmdResult = AnalyzeCommand.ExitCode.CriticalError;

            try
            {
                //one file vs ruleset
                string tmp1 = Path.GetTempFileName();

                //setup analyze call with silent option
                AnalyzeCommand cmd1 = new AnalyzeCommand(new AnalyzeCommandOptions
                {
                    SourcePath = _arg_srcPath,
                    OutputFilePath = tmp1,
                    OutputFileFormat = "json",
                    IgnoreDefaultRules = true,
                    CustomRulesPath = _arg_customRulesPath,
                    SimpleTagsOnly = true,
                    ConsoleVerbosityLevel = "None",
                    Log = _arg_logger
                });


                //get and perform initial analyze on results
                analyzeCmdResult = (AnalyzeCommand.ExitCode)cmd1.Run();

                //must be done here to avoid losing our handle from analyze command overwriting WriteOnce.Writer
                _arg_outputFile ??= "output.json";
                ConfigureFileOutput();

                //restore
                WriteOnce.Verbosity = saveVerbosity;

                if (analyzeCmdResult == AnalyzeCommand.ExitCode.CriticalError)
                {
                    throw new Exception(ErrMsg.GetString(ErrMsg.ID.CMD_CRITICAL_FILE_ERR));
                }
                else if (analyzeCmdResult == AnalyzeCommand.ExitCode.NoMatches)
                {
                    WriteOnce.General(ErrMsg.FormatString(ErrMsg.ID.TAGTEST_RESULTS_TEST_TYPE, _arg_tagTestType.ToString()), false, WriteOnce.ConsoleVerbosity.Low);
                    if (_arg_tagTestType == TagTestType.RulesPresent)
                        WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.TAGTEST_RESULTS_FAIL), true, ConsoleColor.Red, WriteOnce.ConsoleVerbosity.Low);
                    else
                        WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.TAGTEST_RESULTS_SUCCESS), true, ConsoleColor.Green, WriteOnce.ConsoleVerbosity.Low);

                    WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "Tagtest"));

                    exitCode = _arg_tagTestType == TagTestType.RulesPresent ? ExitCode.SomeTagsFailed : ExitCode.TagsTestSuccess;
                }
                else //assumed (result == AnalyzeCommand.ExitCode.MatchesFound)
                {
                    string file1TagsJson = File.ReadAllText(tmp1);
                    var file1Tags = JsonConvert.DeserializeObject<TagsFile>(file1TagsJson);
                    File.Delete(tmp1);

                    exitCode = ExitCode.TagsTestSuccess;
                    foreach (Rule r in _rulesSet)
                    {
                        //supports both directions by generalizing 
                        string[] testList1 = _arg_tagTestType == TagTestType.RulesNotPresent ?
                            r.Tags : file1Tags.Tags;

                        string[] testList2 = _arg_tagTestType == TagTestType.RulesNotPresent ?
                            file1Tags.Tags : r.Tags;

                        foreach (string t in testList2)
                        {
                            if (TagTest(testList1, t))
                                WriteOnce.Result(ErrMsg.FormatString(ErrMsg.ID.TAGTEST_RESULTS_TAGS_FOUND, t), true, WriteOnce.ConsoleVerbosity.High);
                            else
                            {
                                exitCode = ExitCode.SomeTagsFailed;
                                WriteOnce.Result(ErrMsg.FormatString(ErrMsg.ID.TAGTEST_RESULTS_TAGS_MISSING, t), true, WriteOnce.ConsoleVerbosity.High);
                            }

                            if (exitCode != ExitCode.TagsTestSuccess)
                                break;
                        }

                        if (exitCode != ExitCode.TagsTestSuccess)
                            break;
                    }

                    //results
                    WriteOnce.General(ErrMsg.FormatString(ErrMsg.ID.TAGTEST_RESULTS_TEST_TYPE, _arg_tagTestType.ToString()), false, WriteOnce.ConsoleVerbosity.Low);

                    if (exitCode == ExitCode.SomeTagsFailed)
                        WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.TAGTEST_RESULTS_FAIL), true, ConsoleColor.Red, WriteOnce.ConsoleVerbosity.Low);
                    else
                        WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.TAGTEST_RESULTS_SUCCESS), true, ConsoleColor.Green, WriteOnce.ConsoleVerbosity.Low);

                    WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "Tagtest"));
                    if (!String.IsNullOrEmpty(_arg_outputFile) && Utils.CLIExecutionContext)
                        WriteOnce.Any(ErrMsg.FormatString(ErrMsg.ID.ANALYZE_OUTPUT_FILE, _arg_outputFile), true, ConsoleColor.Gray, WriteOnce.ConsoleVerbosity.Low);

                    WriteOnce.FlushAll();
                }
            }
            catch (Exception e)
            {
                WriteOnce.Verbosity = saveVerbosity;
                if (analyzeCmdResult == AnalyzeCommand.ExitCode.Success) //then error was not previously logged
                    WriteOnce.Error(e.Message);
                else
                    WriteOnce.Error(e.Message, true, WriteOnce.ConsoleVerbosity.Low, false);

                //exit normaly for CLI callers and throw for DLL callers
                if (Utils.CLIExecutionContext)
                    return (int)ExitCode.CriticalError;
                else
                    throw e;
            }
            finally
            {
                if (_arg_close_log_on_exit)
                {
                    Utils.Logger = null;
                    WriteOnce.Log = null;
                }
            }

            return (int)exitCode;
        }


        bool TagTest(string[] list, string test)
        {
            if (_arg_tagTestType == TagTestType.RulesNotPresent)
                return (!list.Any(v => v.Equals(test)));
            else
                return (list.Any(v => v.Equals(test)));
        }
    }
}
