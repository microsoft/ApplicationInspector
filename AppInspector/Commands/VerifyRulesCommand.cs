// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using NLog;
using System;
using System.IO;

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
            CriticalError = Utils.ExitCode.CriticalError
        }

        string _rules_path;
        string _arg_custom_rules_path;
        string _arg_outputFile;
        bool _arg_verify_default_rules;
        string _arg_consoleVerbosityLevel;

        public VerifyRulesCommand(VerifyRulesCommandOptions opt)
        {
            _arg_verify_default_rules = opt.VerifyDefaultRules;
            _arg_custom_rules_path = opt.CustomRulesPath;
            _arg_outputFile = opt.OutputFilePath;
            _arg_consoleVerbosityLevel = opt.ConsoleVerbosityLevel ?? "medium";
            _arg_logger = opt.Log;
            _arg_log_file_path = opt.LogFilePath;
            _arg_log_level = opt.LogFileLevel;
            _arg_close_log_on_exit = Utils.CLIExecutionContext ? true : opt.CloseLogOnCommandExit;

            _rules_path = _arg_verify_default_rules ? Utils.GetPath(Utils.AppPath.defaultRulesSrc) : _arg_custom_rules_path;

            _arg_logger ??= Utils.SetupLogging(opt);
            WriteOnce.Log ??= _arg_logger;

            try
            {
                ConfigureConsoleOutput();
                ConfigFileOutput();
                ConfigRules();
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

        /// <summary>
        /// Establish console verbosity
        /// For NuGet DLL use, console is muted overriding any arguments sent
        /// </summary>
        void ConfigureConsoleOutput()
        {
            WriteOnce.SafeLog("VerifyRulesCommand::ConfigureConsoleOutput", LogLevel.Trace);

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
            WriteOnce.SafeLog("VerifyRulesCommand::ConfigOutput", LogLevel.Trace);

            WriteOnce.FlushAll();//in case called more than once

            if (string.IsNullOrEmpty(_arg_outputFile) && _arg_consoleVerbosityLevel.ToLower() == "none")
            {
                throw new Exception(ErrMsg.GetString(ErrMsg.ID.CMD_NO_OUTPUT));
            }
            else if (!string.IsNullOrEmpty(_arg_outputFile))
            {
                WriteOnce.Writer = File.CreateText(_arg_outputFile);
                WriteOnce.Writer.WriteLine(Utils.GetVersionString());
            }
            else
            {
                WriteOnce.Writer = Console.Out;
            }
        }


        void ConfigRules()
        {
            WriteOnce.SafeLog("VerifyRulesCommand::ConfigRules", LogLevel.Trace);

            if (_arg_verify_default_rules && !Utils.CLIExecutionContext)
                throw new Exception(ErrMsg.GetString(ErrMsg.ID.VERIFY_RULES_NO_CLI_DEFAULT));

            if (!_arg_verify_default_rules && string.IsNullOrEmpty(_arg_custom_rules_path))
                throw new Exception(ErrMsg.GetString(ErrMsg.ID.CMD_NORULES_SPECIFIED));
        }


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


        /// <summary>
        /// Main entry from CLI
        /// </summary>
        /// <returns></returns>
        public override int Run()
        {
            WriteOnce.SafeLog("VerifyRulesCommand::Run", LogLevel.Trace);
            WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_RUNNING, "verifyrules"));

            ExitCode exitCode = ExitCode.CriticalError;

            try
            {
                RulesVerifier verifier = new RulesVerifier(_rules_path);
                verifier.Verify();
                exitCode = ExitCode.Verified;

                RuleSet rules = verifier.CompiledRuleset;

                //report each add to console if desired
                foreach (Rule rule in rules)
                {
                    WriteOnce.Result(string.Format("Rule {0}-{1} verified", rule.Id, rule.Name), true, WriteOnce.ConsoleVerbosity.High);
                }

                WriteOnce.Any(ErrMsg.GetString(ErrMsg.ID.VERIFY_RULES_RESULTS_SUCCESS), true, ConsoleColor.Green, WriteOnce.ConsoleVerbosity.Low);
                WriteOnce.Operation(ErrMsg.FormatString(ErrMsg.ID.CMD_COMPLETED, "verifyrules"));
                if (!String.IsNullOrEmpty(_arg_outputFile) && Utils.CLIExecutionContext)
                    WriteOnce.Any(ErrMsg.FormatString(ErrMsg.ID.ANALYZE_OUTPUT_FILE, _arg_outputFile), true, ConsoleColor.Gray, WriteOnce.ConsoleVerbosity.Low);
                WriteOnce.FlushAll();
            }
            catch (Exception e)
            {
                WriteOnce.Error(e.Message);
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

    }

}
