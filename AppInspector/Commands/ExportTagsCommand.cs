// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.ApplicationInspector.Commands
{
    public class ExportTagsCommand : Command
    {
        public enum ExitCode
        {
            Success = 0,
            Error = 1,
            CriticalError = 2
        }

        private string _arg_outputFile;
        private string _arg_customRulesPath;
        private bool _arg_ignoreDefaultRules;
        private RuleSet _rules;
        private string _arg_consoleVerbosityLevel;

        public ExportTagsCommand(ExportTagsCommandOptions opt)
        {
            _rules = new RuleSet(WriteOnce.Log);
            _arg_customRulesPath = opt.CustomRulesPath;
            _arg_outputFile = opt.OutputFilePath;
            _arg_ignoreDefaultRules = opt.IgnoreDefaultRules;
            _arg_consoleVerbosityLevel = opt.ConsoleVerbosityLevel ?? "medium";
            _arg_logger = opt.Log;

            WriteOnce.ConsoleVerbosity verbosity = WriteOnce.ConsoleVerbosity.Medium;
            if (!Enum.TryParse(_arg_consoleVerbosityLevel, true, out verbosity))
                throw new OpException(String.Format(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-x")));
            WriteOnce.Verbosity = verbosity;

            ConfigureOutput();
            ConfigRules();
        }


        private void ConfigureOutput()
        {
            //setup output                       
            TextWriter outputWriter;

            if (!string.IsNullOrEmpty(_arg_outputFile))
            {
                outputWriter = File.CreateText(_arg_outputFile);
                outputWriter.WriteLine(Utils.GetVersionString());
                WriteOnce.Writer = outputWriter;
                WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.Low;
            }
        }


        void ConfigRules()
        {
            List<string> rulePaths = new List<string>();
            if (!_arg_ignoreDefaultRules)
                rulePaths.Add(Utils.GetPath(Utils.AppPath.defaultRules));

            if (!string.IsNullOrEmpty(_arg_customRulesPath))
                rulePaths.Add(_arg_customRulesPath);

            foreach (string rulePath in rulePaths)
            {
                if (Directory.Exists(rulePath))
                    _rules.AddDirectory(rulePath);
                else if (File.Exists(rulePath))
                    _rules.AddFile(rulePath);
                else
                {
                    throw new OpException(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_RULE_PATH, rulePath));
                }
            }

            //error check based on ruleset not path enumeration
            if (_rules.Count() == 0)
            {
                throw new OpException(ErrMsg.GetString(ErrMsg.ID.CMD_NORULES_SPECIFIED));
            }
        }


        /// <summary>
        /// Option for DLL use as alternate to Run which only outputs a file to return results as string
        /// CommandOption defaults will not have been set when used as DLL via CLI processing so some checks added
        /// </summary>
        /// <returns>output results</returns>
        public string GetResult()
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            if (!assembly.GetName().Name.Contains("ApplicationInspector.CLI"))
            {
                WriteOnce.FlushAll();
                WriteOnce.Log = _arg_logger;
            }

            _arg_outputFile ??= "output.txt";
            ConfigureOutput();

            if ((int)ExitCode.CriticalError != Run())
            {
                return File.ReadAllText(_arg_outputFile);
            }

            return string.Empty;
        }


        //Main entry from CLI
        public override int Run()
        {
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

            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "Exporttags"), true, WriteOnce.ConsoleVerbosity.Low);
            WriteOnce.FlushAll();
            if (!String.IsNullOrEmpty(_arg_outputFile))
                WriteOnce.Any(ErrMsg.FormatString(ErrMsg.ID.ANALYZE_OUTPUT_FILE, _arg_outputFile), true, ConsoleColor.Gray, WriteOnce.ConsoleVerbosity.Low);


            return (int)ExitCode.Success;
        }
    }
}
