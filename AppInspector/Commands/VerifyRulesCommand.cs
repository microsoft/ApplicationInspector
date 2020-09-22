// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;

namespace Microsoft.ApplicationInspector.Commands
{
    public class VerifyRulesOptions : CommandOptions
    {
        public bool VerifyDefaultRules { get; set; }
        public string? CustomRulesPath { get; set; }
        public bool Failfast { get; set; }
    }

    public class RuleStatus
    {
        public string? RulesId { get; set; }
        public string? RulesName { get; set; }
        public bool Verified { get; set; }
    }

    public class VerifyRulesResult : Result
    {
        public enum ExitCode
        {
            Verified = 0,
            NotVerified = 1,
            CriticalError = Utils.ExitCode.CriticalError
        }

        [JsonProperty(Order = 2, PropertyName = "resultCode")]
        public ExitCode ResultCode { get; set; }

        [JsonProperty(Order = 3, PropertyName = "ruleStatusList")]
        public List<RuleStatus> RuleStatusList { get; set; }

        public VerifyRulesResult()
        {
            RuleStatusList = new List<RuleStatus>();
        }
    }

    /// <summary>
    /// Used to verify user custom ruleset.  Default ruleset has no need for support outside of PackRulesCommand for verification
    /// since each build performs a verification already and the output is added to the binary manifest
    /// </summary>
    public class VerifyRulesCommand
    {
        private readonly VerifyRulesOptions _options;
        private string? _rules_path;

        public VerifyRulesCommand(VerifyRulesOptions opt)
        {
            _options = opt;

            try
            {
                _options.Log ??= Utils.SetupLogging(_options);
                WriteOnce.Log ??= _options.Log;

                ConfigureConsoleOutput();
                ConfigRules();
            }
            catch (OpException e) //group error handling
            {
                WriteOnce.Error(e.Message);
                throw;
            }
        }

        #region configure

        /// <summary>
        /// Establish console verbosity
        /// For NuGet DLL use, console is muted overriding any arguments sent
        /// </summary>
        private void ConfigureConsoleOutput()
        {
            WriteOnce.SafeLog("VerifyRulesCommand::ConfigureConsoleOutput", LogLevel.Trace);

            //Set console verbosity based on run context (none for DLL use) and caller arguments
            if (!Utils.CLIExecutionContext)
            {
                WriteOnce.Verbosity = WriteOnce.ConsoleVerbosity.None;
            }
            else
            {
                WriteOnce.ConsoleVerbosity verbosity = WriteOnce.ConsoleVerbosity.Medium;
                if (!Enum.TryParse(_options.ConsoleVerbosityLevel, true, out verbosity))
                {
                    WriteOnce.Error(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-x"));
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_ARG_VALUE, "-x"));
                }
                else
                {
                    WriteOnce.Verbosity = verbosity;
                }
            }
        }

        private void ConfigRules()
        {
            WriteOnce.SafeLog("VerifyRulesCommand::ConfigRules", LogLevel.Trace);

            if (_options.VerifyDefaultRules && !Utils.CLIExecutionContext)
            {
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_NO_CLI_DEFAULT));
            }

            if (!_options.VerifyDefaultRules && string.IsNullOrEmpty(_options.CustomRulesPath))
            {
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.CMD_NORULES_SPECIFIED));
            }

            _rules_path = _options.VerifyDefaultRules ? Utils.GetPath(Utils.AppPath.defaultRulesSrc) : _options.CustomRulesPath;
        }

        #endregion configure

        /// <summary>
        /// Option for DLL use as alternate to Run which only outputs a file to return results as string
        /// CommandOption defaults will not have been set when used as DLL via CLI processing so some checks added
        /// </summary>
        /// <returns>output results</returns>
        public VerifyRulesResult GetResult()
        {
            WriteOnce.SafeLog("VerifyRulesCommand::Run", LogLevel.Trace);
            WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_RUNNING, "Verify Rules"));

            VerifyRulesResult verifyRulesResult = new VerifyRulesResult() { AppVersion = Utils.GetVersionString() };

            try
            {
                RulesVerifier verifier = new RulesVerifier(_rules_path, _options.Log);
                verifyRulesResult.ResultCode = VerifyRulesResult.ExitCode.Verified;
                verifyRulesResult.RuleStatusList = verifier.Verify();
                verifyRulesResult.ResultCode = verifier.IsVerified ? VerifyRulesResult.ExitCode.Verified : VerifyRulesResult.ExitCode.NotVerified;
            }
            catch (OpException e)
            {
                WriteOnce.Error(e.Message);
                //caught for CLI callers with final exit msg about checking log or throws for DLL callers
                throw;
            }

            return verifyRulesResult;
        }
    }
}