// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// simple wrapper for serializing results for simple tags only during processing
    /// </summary>
    public class TagsFile
    {
        [JsonProperty(PropertyName = "tags")]
        public string[] Tags { get; set; }
    }

    /// <summary>
    /// Used to compare two source paths and report tag differences
    /// </summary>
    public class TagDiffCommand : Command
    {
        public enum ExitCode
        {
            TestPassed = 0,
            TestFailed = 1,
            CriticalError = Utils.ExitCode.CriticalError //ensure common value for final exit log mention
        }

        enum TagTestType { Equality, Inequality }

        string _arg_src1, _arg_src2;
        string _arg_customRulesPath;
        string _arg_outputFile;
        bool _arg_ignoreDefault;
        string _arg_test_type_raw;
        TagTestType _arg_tagTestType;
        string _arg_consoleVerbosityLevel;

        public TagDiffCommand(TagDiffCommandOptions opt)
        {
            _arg_src1 = opt.SourcePath1;
            _arg_src2 = opt.SourcePath2;
            _arg_customRulesPath = opt.CustomRulesPath;
            _arg_consoleVerbosityLevel = opt.ConsoleVerbosityLevel ?? "medium";
            _arg_ignoreDefault = opt.IgnoreDefaultRules;
            _arg_outputFile = opt.OutputFilePath;
            _arg_test_type_raw = opt.TestType ?? "Equality";
            _arg_logger = opt.Log;
            _arg_log_file_path = opt.LogFilePath;
            _arg_close_log_on_exit = Utils.CLIExecutionContext ? true : opt.CloseLogOnCommandExit;

            _arg_logger ??= Utils.SetupLogging(opt);
            WriteOnce.Log ??= _arg_logger;

            try
            {
                ConfigureConsoleOutput();
                ConfigureCompareType();
                ConfigSourceToScan();
            }
            catch (Exception e) //group error handling
            {
                WriteOnce.Error(e.Message);
                if (_arg_close_log_on_exit)
                {
                    Utils.Logger = null;
                    WriteOnce.Log = null;
                }
                throw;
            }
        }

        #region config

        /// <summary>
        /// Establish console verbosity
        /// For NuGet DLL use, console is muted overriding any arguments sent
        /// </summary>
        void ConfigureConsoleOutput()
        {
            WriteOnce.SafeLog("TagDiffCommand::ConfigureConsoleOutput", LogLevel.Trace);

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


        void ConfigureCompareType()
        {
            if (!Enum.TryParse(_arg_test_type_raw, true, out _arg_tagTestType))
            {
                WriteOnce.Error((ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, _arg_test_type_raw)));
                throw new Exception(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, _arg_test_type_raw));
            }
        }


        void ConfigureFileOutput()
        {
            WriteOnce.SafeLog("TagDiff::ConfigOutput", LogLevel.Trace);
            //intentionally empty as this is put in Run() for this command; see note
        }


        void ConfigSourceToScan()
        {
            WriteOnce.SafeLog("TagDiff::ConfigRules", LogLevel.Trace);

            if (_arg_src1 == _arg_src2)
            {
                throw new Exception(ErrMsg.GetString(ErrMsg.ID.TAGDIFF_SAME_FILE_ARG));
            }
            else if (string.IsNullOrEmpty(_arg_src1) || string.IsNullOrEmpty(_arg_src2))
            {
                throw new Exception(ErrMsg.GetString(ErrMsg.ID.CMD_INVALID_ARG_VALUE));
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
            WriteOnce.SafeLog("TagDiffCommand::Run", LogLevel.Trace);
            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_RUNNING, "tagdiff"));

            ExitCode exitCode = ExitCode.CriticalError;
            //save to quiet analyze cmd and restore
            WriteOnce.ConsoleVerbosity saveVerbosity = WriteOnce.Verbosity;
            AnalyzeCommand.ExitCode analyzeCmdResult1 = AnalyzeCommand.ExitCode.CriticalError;
            AnalyzeCommand.ExitCode analyzeCmdResult2 = AnalyzeCommand.ExitCode.CriticalError;

            try
            {
                #region setup analyze calls

                string tmp1 = Path.GetTempFileName();
                string tmp2 = Path.GetTempFileName();

                AnalyzeCommand cmd1 = new AnalyzeCommand(new AnalyzeCommandOptions
                {
                    SourcePath = _arg_src1,
                    OutputFilePath = tmp1,
                    OutputFileFormat = "json",
                    CustomRulesPath = _arg_customRulesPath,
                    IgnoreDefaultRules = _arg_ignoreDefault,
                    SimpleTagsOnly = true,
                    ConsoleVerbosityLevel = "none",
                    Log = _arg_logger
                });
                AnalyzeCommand cmd2 = new AnalyzeCommand(new AnalyzeCommandOptions
                {
                    SourcePath = _arg_src2,
                    OutputFilePath = tmp2,
                    OutputFileFormat = "json",
                    CustomRulesPath = _arg_customRulesPath,
                    IgnoreDefaultRules = _arg_ignoreDefault,
                    SimpleTagsOnly = true,
                    ConsoleVerbosityLevel = "none",
                    Log = _arg_logger
                });


                analyzeCmdResult1 = (AnalyzeCommand.ExitCode)cmd1.Run();
                analyzeCmdResult2 = (AnalyzeCommand.ExitCode)cmd2.Run();

                ConfigureFileOutput();

                //restore
                WriteOnce.Verbosity = saveVerbosity;

                #endregion

                bool equalTagsCompare1 = true;
                bool equalTagsCompare2 = true;

                //process results for each analyze call before comparing results
                if (analyzeCmdResult1 == AnalyzeCommand.ExitCode.CriticalError)
                {
                    throw new Exception(ErrMsg.FormatString(ErrMsg.ID.CMD_CRITICAL_FILE_ERR, _arg_src1));
                }
                else if (analyzeCmdResult2 == AnalyzeCommand.ExitCode.CriticalError)
                {
                    throw new Exception(ErrMsg.FormatString(ErrMsg.ID.CMD_CRITICAL_FILE_ERR, _arg_src2));
                }
                else if (analyzeCmdResult1 == AnalyzeCommand.ExitCode.NoMatches || analyzeCmdResult2 == AnalyzeCommand.ExitCode.NoMatches)
                {
                    throw new Exception(ErrMsg.GetString(ErrMsg.ID.TAGDIFF_NO_TAGS_FOUND));
                }
                else //compare tag results; assumed (result1&2 == AnalyzeCommand.ExitCode.Success)
                {
                    //setup output here rather than top to avoid analyze command output in this command output                   
                    TextWriter outputWriter;
                    if (!string.IsNullOrEmpty(_arg_outputFile))
                    {
                        outputWriter = File.CreateText(_arg_outputFile);
                        outputWriter.WriteLine(Utils.GetVersionString());
                        WriteOnce.Writer = outputWriter;
                    }

                    string file1TagsJson = File.ReadAllText(tmp1);
                    string file2TagsJson = File.ReadAllText(tmp2);

                    var file1Tags = JsonConvert.DeserializeObject<TagsFile>(file1TagsJson);
                    var file2Tags = JsonConvert.DeserializeObject<TagsFile>(file2TagsJson);

                    //can't simply compare counts as content may differ; must compare both in directions in two passes a->b; b->a
                    WriteOnce.General(ErrMsg.FormatString(ErrMsg.ID.TAGDIFF_RESULTS_GAP, Path.GetFileName(_arg_src1), Path.GetFileName(_arg_src2)),
                            true, WriteOnce.ConsoleVerbosity.High);
                    equalTagsCompare1 = CompareTags(file1Tags.Tags, file2Tags.Tags);

                    //reverse order for second pass
                    WriteOnce.General(ErrMsg.FormatString(ErrMsg.ID.TAGDIFF_RESULTS_GAP, Path.GetFileName(_arg_src2), Path.GetFileName(_arg_src1)),
                            true, WriteOnce.ConsoleVerbosity.High);
                    equalTagsCompare2 = CompareTags(file2Tags.Tags, file1Tags.Tags);

                    //final results
                    bool resultsDiffer = !(equalTagsCompare1 && equalTagsCompare2);
                    if (_arg_tagTestType == TagTestType.Inequality && !resultsDiffer)
                        exitCode = ExitCode.TestFailed;
                    else if (_arg_tagTestType == TagTestType.Equality && resultsDiffer)
                        exitCode = ExitCode.TestFailed;
                    else
                        exitCode = ExitCode.TestPassed;

                    WriteOnce.General(ErrMsg.FormatString(ErrMsg.ID.TAGDIFF_RESULTS_DIFFER), false);
                    WriteOnce.Result(resultsDiffer.ToString());
                    WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "tagdiff"));
                    if (!String.IsNullOrEmpty(_arg_outputFile) && Utils.CLIExecutionContext)
                        WriteOnce.Info(ErrMsg.FormatString(ErrMsg.ID.ANALYZE_OUTPUT_FILE, _arg_outputFile), true, WriteOnce.ConsoleVerbosity.Low, false);

                    WriteOnce.FlushAll();
                }

                //cleanup
                try
                {
                    File.Delete(tmp1);
                    File.Delete(tmp2);
                }
                catch
                {
                    //no action needed; 
                }
            }
            catch (Exception e)
            {
                WriteOnce.Verbosity = saveVerbosity;
                if (analyzeCmdResult1 == AnalyzeCommand.ExitCode.Success && analyzeCmdResult2 == AnalyzeCommand.ExitCode.Success) //error not previously logged
                    WriteOnce.Error(e.Message);
                else
                    WriteOnce.Error(e.Message, true, WriteOnce.ConsoleVerbosity.Low, false);//console but don't log again

                //exit normaly for CLI callers and throw for DLL callers
                if (Utils.CLIExecutionContext)
                    return (int)ExitCode.CriticalError;
                else
                    throw;
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



        bool CompareTags(string[] fileTags1, string[] fileTags2)
        {
            bool found = true;
            //are all tags in file1 found in file2
            foreach (string s1 in fileTags1)
            {
                if (!fileTags2.Contains(s1))
                {
                    found = false;
                    WriteOnce.Result(s1, true, WriteOnce.ConsoleVerbosity.High);
                }
            }

            //none missing
            if (found)
                WriteOnce.Result(ErrMsg.GetString(ErrMsg.ID.TAGTEST_RESULTS_NONE), true, WriteOnce.ConsoleVerbosity.High);

            return found;
        }


    }
}
