// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using RulesEngine;
using Microsoft.AppInspector;

namespace Microsoft.AppInspector.Commands
{
    /// <summary>
    /// Wraps rulesengine verify for ruleset
    /// </summary>
    public class VerifyRulesCommand : ICommand
   {
        public enum ExitCode
        {
            Verified = 0,
            NotVerified = 1,
            CriticalError = 2
        }


        public VerifyRulesCommand(VerifyRulesCommandOptions opt)
        {
            _arg_customRulesPath = opt.CustomRulesPath;
            _arg_ignoreDefaultRules = opt.IgnoreDefaultRules;

            ConfigRules();
        }


        void ConfigRules()
        {
            _rulePaths = new List<string>();
            if (!_arg_ignoreDefaultRules)
                _rulePaths.Add(Utils.GetPath(Utils.AppPath.defaultRules));

            if (!string.IsNullOrEmpty(_arg_customRulesPath))
                _rulePaths.Add(_arg_customRulesPath);

            if (_rulePaths.Count == 0)
                throw new OpException(ErrMsg.GetString(ErrMsg.ID.CMD_NORULES_SPECIFIED));
        }


        public int Run()
        {
            bool issues = false;

            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_RUNNING, "Verify Rules"));
            
            //load [each] rules file separately to report out where a failure is happening 
            IEnumerable<string> fileListing = new List<string>();
            foreach (string rulePath in _rulePaths)
            {
                if (Directory.Exists(rulePath))
                    fileListing = Directory.EnumerateFiles(rulePath, "*.json", SearchOption.AllDirectories);
                else if (File.Exists(rulePath) && Path.GetExtension(rulePath) == ".json")
                    fileListing = new List<string>() { new string(rulePath) };
            
                RuleSet rules = new RuleSet();
                foreach (string filename in fileListing)
                {
                    try
                    {
                        rules.AddFile(filename);
                    }
                    catch (Exception e)
                    {
                        WriteOnce.Log.Error(ErrMsg.FormatString(ErrMsg.ID.VERIFY_RULE_FAILED, filename));
                        WriteOnce.Log.Error(e.Message + "\n" + e.StackTrace);
                        issues = true;
                    }
                }
            }

            //final report
            if (issues)
                WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.VERIFY_RULES_RESULTS_FAIL), true, ConsoleColor.Red, WriteOnce.ConsoleVerbosity.Low);
            else
                WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.VERIFY_RULES_RESULTS_SUCCESS), true, ConsoleColor.Green, WriteOnce.ConsoleVerbosity.Low);

            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "Verify Rules"));

            return issues ? (int)ExitCode.NotVerified : (int)ExitCode.Verified;
        }


        List<string> _rulePaths = new List<string>();
        private string _arg_customRulesPath;
        private bool _arg_ignoreDefaultRules;
    }
}
