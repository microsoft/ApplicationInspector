// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.ApplicationInspector.Commands
{
    public class ExportTagsCommand : Command
    {
        public enum ExitCode
        {
            Success = 0,
            Error = 1,
            CriticalError = Utils.ExitCode.CriticalError //ensure common value for final exit log mention
        }

        string _arg_outputFile;
        string _arg_customRulesPath;
        bool _arg_ignoreDefaultRules;
        RuleSet _rules;
        string _arg_consoleVerbosityLevel;

        public ExportTagsCommand(ExportTagsCommandOptions opt)
        {
            _rules = new RuleSet(WriteOnce.Log);
            _arg_customRulesPath = opt.CustomRulesPath;
            _arg_outputFile = opt.OutputFilePath;
            _arg_ignoreDefaultRules = opt.IgnoreDefaultRules;
            _arg_consoleVerbosityLevel = opt.ConsoleVerbosityLevel ?? "medium";
            _arg_logger = opt.Log;
            _arg_log_file_path = opt.LogFilePath;
            _arg_log_level = opt.LogFileLevel;
            _arg_close_log_on_exit = Utils.CLIExecutionContext ? true : opt.CloseLogOnCommandExit;

            _arg_logger ??= Utils.SetupLogging(opt);
            WriteOnce.Log ??= _arg_logger;

            try
            {
                ConfigureConsoleOutput();
                ConfigFileOutput();
                ConfigRules();
            }
            catch (Exception e)
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


        #region ConfigMethods

        /// <summary>
        /// Establish console verbosity
        /// For NuGet DLL use, console is muted overriding any arguments sent
        /// </summary>
        void ConfigureConsoleOutput()
        {
            WriteOnce.SafeLog("ExportTagsCommand::ConfigureConsoleOutput", LogLevel.Trace);

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


        private void ConfigFileOutput()
        {
            WriteOnce.SafeLog("ExportTagsCommand::ConfigOutput", LogLevel.Trace);

            //setup output                       
            TextWriter outputWriter;

            if (!string.IsNullOrEmpty(_arg_outputFile))
            {
                outputWriter = File.CreateText(_arg_outputFile);
                outputWriter.WriteLine(Utils.GetVersionString());
                WriteOnce.Writer = outputWriter;
            }
        }


        void ConfigRules()
        {
            WriteOnce.SafeLog("ExportTagsCommand::ConfigRules", LogLevel.Trace);

            if (!_arg_ignoreDefaultRules)
            {
                _rules = Utils.GetDefaultRuleSet(_arg_logger);
            }

            if (!string.IsNullOrEmpty(_arg_customRulesPath))
            {
                if (_rules == null)
                    _rules = new RuleSet(_arg_logger);

                if (Directory.Exists(_arg_customRulesPath))
                    _rules.AddDirectory(_arg_customRulesPath);
                else if (File.Exists(_arg_customRulesPath))
                    _rules.AddFile(_arg_customRulesPath);
                else
                    throw new Exception(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_RULE_PATH, _arg_customRulesPath));
            }

            //error check based on ruleset not path enumeration
            if (_rules == null || _rules.Count() == 0)
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
            ConfigFileOutput();

            if ((int)ExitCode.CriticalError != Run())
            {
                return File.ReadAllText(_arg_outputFile);
            }

            return string.Empty;
        }


        //Main entry from CLI
        public override int Run()
        {
            WriteOnce.SafeLog("ExportTagsCommand::Run", LogLevel.Trace);
            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_RUNNING, "Exporttags"));

            SortedDictionary<string, string> uniqueTags = new SortedDictionary<string, string>();

            foreach (Rule r in _rules)
            {
                //builds a list of unique tags
                foreach (string t in r.Tags)
                {
                    if (uniqueTags.ContainsKey(t))
                        continue;
                    else
                        uniqueTags.Add(t, t);
                }
            }

            //separate loop so results are sorted (Sorted type)
            foreach (string s in uniqueTags.Values)
                WriteOnce.Result(s, true);

            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "Exporttags"));
            if (!String.IsNullOrEmpty(_arg_outputFile) && Utils.CLIExecutionContext)
                WriteOnce.Any(ErrMsg.FormatString(ErrMsg.ID.ANALYZE_OUTPUT_FILE, _arg_outputFile), true, ConsoleColor.Gray, WriteOnce.ConsoleVerbosity.Low);

            WriteOnce.FlushAll();

            if (_arg_close_log_on_exit)
            {
                Utils.Logger = null;
                WriteOnce.Log = null;
            }

            return (int)ExitCode.Success;
        }
    }
}
