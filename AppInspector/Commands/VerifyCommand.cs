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
            _arg_RulesPath = opt.CustomRulesPath;
            _ignoreDefault = opt.IgnoreDefaultRules;
        }

        public int Run()
        {
            WriteOnce.Write("Verify rules command running\n", ConsoleColor.Cyan, WriteOnce.ConsoleVerbosityLevel.Low);


            if (String.IsNullOrEmpty(_arg_RulesPath) && !_ignoreDefault)
                _arg_RulesPath = Helper.GetPath(Helper.AppPath.defaultRules);
            else if (!Directory.Exists(_arg_RulesPath) && !File.Exists(_arg_RulesPath))
            {
                WriteOnce.Error(string.Format("Error: Not a valid file or directory {0}", _arg_RulesPath));
                return (int)ExitCode.CriticalError;
            }

            //load [each] rules file separately to report out where a failure is happening 
            IEnumerable<string> fileListing;
            fileListing = Directory.EnumerateFiles(_arg_RulesPath, "*.json", SearchOption.AllDirectories);

            bool bIssues = false;
            foreach (string filename in fileListing)
            {
                if (Path.GetExtension(filename) == ".json")
                {
                    RuleSet rules = new RuleSet(Program.Logger);
                   
                    try
                    {
                        rules.AddFile(filename);
                        RuleProcessor processor = new RuleProcessor(false, false, Program.Logger);
                    }
                    catch (Exception e)
                    {
                        WriteOnce.Any("Rule parsing failed for file: " + filename);
                        Program.Logger.Debug(e.Message);
                        bIssues = true;
                    }
                }
            }

            WriteOnce.Any(string.Format("Verify rules completed {0}", !bIssues ? "successfully" : "with errors."),
                ConsoleColor.Cyan);

            return bIssues ? (int)ExitCode.NotVerified : (int)ExitCode.Verified;
        }

        private string _arg_RulesPath;
        private bool _ignoreDefault;
    }
}
