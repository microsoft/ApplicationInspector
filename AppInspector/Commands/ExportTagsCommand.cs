// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using RulesEngine;


namespace Microsoft.AppInspector
{
    public class ExportTagsCommand : ICommand
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
        private WriteOnce.ConsoleVerbosity _arg_consoleVerbosityLevel;

        public ExportTagsCommand(ExportTagsCommandOptions opt)
        {
            _rules = new RuleSet(WriteOnce.Log);
            _arg_customRulesPath = opt.CustomRulesPath;
            _arg_outputFile = opt.OutputFilePath;
            _arg_ignoreDefaultRules = opt.IgnoreDefaultRules;
            if (!Enum.TryParse(opt.ConsoleVerbosityLevel, true, out _arg_consoleVerbosityLevel))
                throw new OpException(String.Format(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-x")));
            WriteOnce.Verbosity = _arg_consoleVerbosityLevel;
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
                outputWriter.WriteLine(Program.GetVersionString());
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

        public int Run()
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

            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "Exporttags"));
            WriteOnce.FlushAll();
            if (!String.IsNullOrEmpty(_arg_outputFile))
                WriteOnce.Any(ErrMsg.FormatString(ErrMsg.ID.ANALYZE_OUTPUT_FILE, _arg_outputFile),true, ConsoleColor.Gray,WriteOnce.ConsoleVerbosity.Low);


            return (int)ExitCode.Success;
        }
    }
}
