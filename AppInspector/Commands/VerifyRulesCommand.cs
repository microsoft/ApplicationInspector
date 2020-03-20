// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Used to verify user custom ruleset.  Default ruleset has no need for support outside of PackRulesCommand for verification
    /// since each build performs a verification already and the output is added to the binary manifest
    /// </summary>
    public class VerifyRulesCommand : Command
    {
        public enum ExitCode
        {
            Verified = 0,
            NotVerified = 1,
            CriticalError = 2
        }

        string _arg_customRulesPath;
        string _arg_outputFile;
        string _arg_consoleVerbosityLevel;

        public VerifyRulesCommand(VerifyRulesCommandOptions opt)
        {
            _arg_customRulesPath = opt.CustomRulesPath;
            _arg_outputFile = opt.OutputFilePath;
            _arg_consoleVerbosityLevel = opt.ConsoleVerbosityLevel ?? "medium";
            _arg_logger = opt.Log;

            WriteOnce.ConsoleVerbosity verbosity = WriteOnce.ConsoleVerbosity.Medium;
            if (!Enum.TryParse(_arg_consoleVerbosityLevel, true, out verbosity))
                throw new OpException(String.Format(ErrMsg.FormatString(ErrMsg.ID.CMD_INVALID_ARG_VALUE, "-x")));
            WriteOnce.Verbosity = verbosity;

            ConfigOutput();
            ConfigRules();
        }


        private void ConfigOutput()
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
            if (string.IsNullOrEmpty(_arg_customRulesPath))
                throw new OpException(ErrMsg.GetString(ErrMsg.ID.CMD_NORULES_SPECIFIED));

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
            ConfigOutput();

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
            bool issues = false;

            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_RUNNING, "Verify Rules"));

            RulesVerifier verifier = new RulesVerifier(_arg_customRulesPath);
            if (!verifier.Verify())
                return (int)ExitCode.NotVerified;

            RuleSet rules = verifier.CompiledRuleset;

            //report each add
            foreach (Rule rule in rules)
            {
                WriteOnce.Result(string.Format("Rule {0}-{1} verified", rule.Id, rule.Name), true, WriteOnce.ConsoleVerbosity.High);
            }

            WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.VERIFY_RULES_RESULTS_SUCCESS), true, ConsoleColor.Green, WriteOnce.ConsoleVerbosity.Low);
            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "Verify Rules"));

            WriteOnce.FlushAll();
            if (!String.IsNullOrEmpty(_arg_outputFile))
                WriteOnce.Any(ErrMsg.FormatString(ErrMsg.ID.ANALYZE_OUTPUT_FILE, _arg_outputFile), true, ConsoleColor.Gray, WriteOnce.ConsoleVerbosity.Low);

            return issues ? (int)ExitCode.NotVerified : (int)ExitCode.Verified;
        }

    }

}
