// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using RulesEngine;


namespace Microsoft.AppInspector.Commands
{
    public class ExportTagsCommand : ICommand
   {
        public enum ExitCode
        {
            Success = 0,
            Error = 1,
            CriticalError = 2
        }

        private string _arg_RulesPath;
        private string _arg_outputFile;

        public ExportTagsCommand(ExportTagsCommandOptions opt)
        {
            _arg_RulesPath = opt.CustomRulesPath;
            _arg_outputFile = opt.OutputFilePath;
        }

        public int Run()
        {
            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_RUNNING, "Exporttags"));
            
            if (String.IsNullOrEmpty(_arg_RulesPath))
            {
                _arg_RulesPath = Utils.GetPath(Utils.AppPath.defaultRules);
            }
            else if (!Directory.Exists(_arg_RulesPath) && !File.Exists(_arg_RulesPath))
            {
                WriteOnce.Error(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_RULE_PATH, _arg_RulesPath));

                return (int)ExitCode.CriticalError;
            }


            //setup output                       
            TextWriter outputWriter;

            if (!string.IsNullOrEmpty(_arg_outputFile))
            {
                outputWriter = File.CreateText(_arg_outputFile);
                outputWriter.WriteLine(Program.GetVersionString());
                WriteOnce.Writer = outputWriter;
                
            }
            else
            {
                //override user if no output file so output to console appears
                WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.High;
            }

            //initialize rules
            RuleSet rules = new RuleSet();
            rules.AddDirectory(_arg_RulesPath);

            SortedDictionary<string, string> uniqueTags = new SortedDictionary<string, string>();

            foreach (Rule r in rules)
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
                WriteOnce.Result(s, true, WriteOnce.ConsoleVerbosity.High);

            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "Exporttags"));
            WriteOnce.FlushAll();

            return (int)ExitCode.Success;
        }
    }
}
