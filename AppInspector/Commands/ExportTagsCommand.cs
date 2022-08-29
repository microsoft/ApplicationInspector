﻿// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.Commands
{
    using Microsoft.ApplicationInspector.RulesEngine;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    /// <summary>
    /// Options for the Export Tags command.
    /// </summary>
    public class ExportTagsOptions
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
            CriticalError = Common.Utils.ExitCode.CriticalError //ensure common value for final exit log mention
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
        private readonly ILogger _logger;
        private readonly ILoggerFactory? _loggerFactory;
        private RuleSet _rules;

        public ExportTagsCommand(ExportTagsOptions opt, ILoggerFactory? loggerFactory = null)
        {
            _options = opt;
            _logger = loggerFactory?.CreateLogger<ExportTagsCommand>() ?? NullLogger<ExportTagsCommand>.Instance;
            _loggerFactory = loggerFactory;
            _rules = new RuleSet();
            ConfigRules();
        }

        
        private void ConfigRules()
        {
            _logger.LogTrace("ExportTagsCommand::ConfigRules");
            _rules = new RuleSet(_loggerFactory);
            if (!_options.IgnoreDefaultRules)
            {
                _rules = RuleSetUtils.GetDefaultRuleSet(_loggerFactory);
            }

            if (!string.IsNullOrEmpty(_options?.CustomRulesPath))
            {
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

        
        public ExportTagsResult GetResult()
        {
            _logger.LogTrace("ExportTagsCommand::Run");
            _logger.LogInformation(MsgHelp.GetString(MsgHelp.ID.CMD_RUNNING), "Export Tags");

            ExportTagsResult exportTagsResult = new()
            {
                AppVersion = Common.Utils.GetVersionString()
            };

            exportTagsResult.TagsList = _rules.SelectMany(x => x.Tags ?? Array.Empty<string>()).Distinct().OrderBy(x => x).ToList();
            exportTagsResult.ResultCode = ExportTagsResult.ExitCode.Success;

            return exportTagsResult;
        }
    }
}