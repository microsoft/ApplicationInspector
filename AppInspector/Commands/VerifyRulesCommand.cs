// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Microsoft.ApplicationInspector.Commands;

public class VerifyRulesOptions
{
    public bool VerifyDefaultRules { get; set; }
    public string? CustomRulesPath { get; set; }
    public string? CustomCommentsPath { get; set; }
    public string? CustomLanguagesPath { get; set; }
    public bool DisableRequireUniqueIds { get; set; }
    public bool RequireMustMatch { get; set; }
    public bool RequireMustNotMatch { get; set; }
}

public class VerifyRulesResult : Result
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExitCode
    {
        Verified = 0,
        NotVerified = 1,
        CriticalError = Utils.ExitCode.CriticalError
    }

    public VerifyRulesResult()
    {
        RuleStatusList = new List<RuleStatus>();
    }

    [JsonPropertyName("resultCode")]
    public ExitCode ResultCode { get; set; }

    [JsonPropertyName("ruleStatusList")]
    public List<RuleStatus> RuleStatusList { get; set; }

    [JsonIgnore] public IEnumerable<RuleStatus> Unverified => RuleStatusList.Where(x => !x.Verified);
}

/// <summary>
///     Used to verify user custom ruleset.  Default ruleset has no need for support outside of PackRulesCommand for
///     verification
///     since each build performs a verification already and the output is added to the binary manifest
/// </summary>
public class VerifyRulesCommand
{
    private readonly ILogger<VerifyRulesCommand> _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly VerifyRulesOptions _options;

    public VerifyRulesCommand(VerifyRulesOptions opt, ILoggerFactory? loggerFactory = null)
    {
        _options = opt;
        _logger = loggerFactory?.CreateLogger<VerifyRulesCommand>() ?? NullLogger<VerifyRulesCommand>.Instance;
        _loggerFactory = loggerFactory;
        ConfigRules();
    }


    private void ConfigRules()
    {
        _logger.LogTrace("VerifyRulesCommand::ConfigRules");

        if (!_options.VerifyDefaultRules && string.IsNullOrEmpty(_options.CustomRulesPath))
        {
            throw new OpException(MsgHelp.GetString(MsgHelp.ID.CMD_NORULES_SPECIFIED));
        }
    }


    /// <summary>
    ///     Option for DLL use as alternate to Run which only outputs a file to return results as string
    ///     CommandOption defaults will not have been set when used as DLL via CLI processing so some checks added
    /// </summary>
    /// <returns>output results</returns>
    public VerifyRulesResult GetResult()
    {
        _logger.LogTrace("VerifyRulesCommand::Run");
        _logger.LogInformation(MsgHelp.GetString(MsgHelp.ID.CMD_RUNNING), "Verify Rules");

        VerifyRulesResult verifyRulesResult = new() { AppVersion = Utils.GetVersionString() };

        try
        {
            var analyzer = new ApplicationInspectorAnalyzer();
            RulesVerifierOptions options = new()
            {
                Analyzer = analyzer,
                DisableRequireUniqueIds = _options.DisableRequireUniqueIds,
                RequireMustMatch = _options.RequireMustMatch,
                RequireMustNotMatch = _options.RequireMustNotMatch,
                LoggerFactory = _loggerFactory,
                LanguageSpecs = Languages.FromConfigurationFiles(_loggerFactory, _options.CustomCommentsPath,
                    _options.CustomLanguagesPath)
            };
            RulesVerifier verifier = new(options);
            verifyRulesResult.ResultCode = VerifyRulesResult.ExitCode.Verified;

            RuleSet? ruleSet = new(true,_loggerFactory);
            if (_options.VerifyDefaultRules)
            {
                ruleSet = RuleSetUtils.GetDefaultRuleSet(_loggerFactory);
            }

            try
            {
                if (_options.CustomRulesPath != null)
                {
                    if (Directory.Exists(_options.CustomRulesPath))
                    {
                        ruleSet.AddDirectory(_options.CustomRulesPath);
                    }
                    else if (File.Exists(_options.CustomRulesPath))
                    {
                        ruleSet.AddFile(_options.CustomRulesPath);
                    }
                }
            }
            catch (JsonException e)
            {
                _logger.LogError(e.Message);
                verifyRulesResult.ResultCode = VerifyRulesResult.ExitCode.CriticalError;
                return verifyRulesResult;
            }

            var verifyResult = verifier.Verify(ruleSet);
            verifyRulesResult.RuleStatusList = verifyResult.RuleStatuses;
            verifyRulesResult.ResultCode = verifyResult.Verified
                ? VerifyRulesResult.ExitCode.Verified
                : VerifyRulesResult.ExitCode.NotVerified;
        }
        catch (OpException e)
        {
            _logger.LogTrace(e.Message);
            //caught for CLI callers with final exit msg about checking log or throws for DLL callers
            throw;
        }

        return verifyRulesResult;
    }
}