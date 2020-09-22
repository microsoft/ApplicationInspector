// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    ///
    /// </summary>
    public class ExportTagsOptions : CommandOptions
    {
        public string? CustomRulesPath { get; set; }
        public bool IgnoreDefaultRules { get; set; }
    }

    /// <summary>
    /// Final result of GetResult call
    /// </summary>
    public class ExportTagsResult : Result
    {
        public enum ExitCode
        {
            Success = 0,
            Error = 1,
            CriticalError = Utils.ExitCode.CriticalError //ensure common value for final exit log mention
        }

        [JsonProperty(Order = 2, PropertyName = "resultCode")]
        public ExitCode ResultCode { get; set; }

        /// <summary>
        /// List of tags exported from specified ruleset
        /// </summary>
        [JsonProperty(Order = 3, PropertyName = "tagsList")]
        public List<string> TagsList { get; set; }

        public ExportTagsResult()
        {
            TagsList = new List<string>();
        }
    }

    /// <summary>
    /// Export command operation manages setp and delivery of ExportResult objects
    /// </summary>
    public class ExportTagsCommand
    {
        private readonly ExportTagsOptions _options;
        private RuleSet _rules;

        public ExportTagsCommand(ExportTagsOptions opt)
        {
            _options = opt;

            try
            {
                _options.Log ??= Utils.SetupLogging(_options);
                WriteOnce.Log ??= _options.Log;
                _rules = new RuleSet(_options.Log);

                ConfigureConsoleOutput();
                ConfigRules();
            }
            catch (Exception e)
            {
                WriteOnce.Error(e.Message);
                throw new OpException(e.Message);
            }
        }

        #region ConfigMethods

        /// <summary>
        /// Establish console verbosity
        /// For NuGet DLL use, console is muted overriding any arguments sent
        /// </summary>
        private void ConfigureConsoleOutput()
        {
            WriteOnce.SafeLog("ExportTagsCommand::ConfigureConsoleOutput", LogLevel.Trace);

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
            WriteOnce.SafeLog("ExportTagsCommand::ConfigRules", LogLevel.Trace);

            if (!_options.IgnoreDefaultRules)
            {
                _rules = Utils.GetDefaultRuleSet(_options?.Log);
            }

            if (!string.IsNullOrEmpty(_options?.CustomRulesPath))
            {
                if (_rules == null)
                {
                    _rules = new RuleSet(_options.Log);
                }

                if (Directory.Exists(_options.CustomRulesPath))
                {
                    _rules.AddDirectory(_options.CustomRulesPath);
                }
                else if (File.Exists(_options.CustomRulesPath))
                {
                    _rules.AddFile(_options.CustomRulesPath);
                }
                else
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_RULE_PATH, _options.CustomRulesPath));
                }
            }

            //error check based on ruleset not path enumeration
            if (_rules == null || !_rules.Any())
            {
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.CMD_NORULES_SPECIFIED));
            }
        }

        #endregion ConfigMethods

        public ExportTagsResult GetResult()
        {
            WriteOnce.SafeLog("ExportTagsCommand::Run", LogLevel.Trace);
            WriteOnce.Operation(MsgHelp.FormatString(MsgHelp.ID.CMD_RUNNING, "Export Tags"));

            ExportTagsResult exportTagsResult = new ExportTagsResult()
            {
                AppVersion = Utils.GetVersionString()
            };

            SortedDictionary<string, string> uniqueTags = new SortedDictionary<string, string>();

            try
            {
                foreach (Rule? r in _rules)
                {
                    //builds a list of unique tags
                    foreach (string t in r?.Tags ?? new string[] { })
                    {
                        if (uniqueTags.ContainsKey(t))
                        {
                            continue;
                        }
                        else
                        {
                            uniqueTags.Add(t, t);
                        }
                    }
                }

                //generate results list
                foreach (string s in uniqueTags.Values)
                {
                    exportTagsResult.TagsList.Add(s);
                }

                exportTagsResult.ResultCode = ExportTagsResult.ExitCode.Success;
            }
            catch (OpException e)
            {
                WriteOnce.Error(e.Message);
                //caught for CLI callers with final exit msg about checking log or throws for DLL callers
                throw;
            }

            return exportTagsResult;
        }
    }
}