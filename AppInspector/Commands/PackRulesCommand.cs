// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.ApplicationInspector.Commands;

public class PackRulesOptions
{
    public string? CustomRulesPath { get; set; }
    public bool PackEmbeddedRules { get; set; }
    public string? CustomCommentsPath { get; set; }
    public string? CustomLanguagesPath { get; set; }
    public bool DisableRequireUniqueIds { get; set; }
    public bool RequireMustMatch { get; set; }
    public bool RequireMustNotMatch { get; set; }
}

public class PackRulesResult : Result
{
    public enum ExitCode
    {
        Success = 0,
        Error = 1,
        CriticalError = Utils.ExitCode.CriticalError //ensure common value for final exit log mention
    }

    [JsonProperty(Order = 2)] public ExitCode ResultCode { get; set; }

    /// <summary>
    ///     List of Rules to pack as specified in pack command
    /// </summary>
    [JsonProperty(Order = 3)]
    public List<Rule>? Rules { get; set; }
}

/// <summary>
///     Used to combine validated rules into one json
/// </summary>
public class PackRulesCommand
{
    private readonly ILogger<PackRulesCommand> _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly PackRulesOptions _options;

    public PackRulesCommand(PackRulesOptions opt, ILoggerFactory? loggerFactory = null)
    {
        _options = opt;
        _logger = loggerFactory?.CreateLogger<PackRulesCommand>() ?? NullLogger<PackRulesCommand>.Instance;
        _loggerFactory = loggerFactory;
        ConfigRules();
    }


    private void ConfigRules()
    {
        _logger.LogTrace("PackRulesCommand::ConfigRules");

        if (string.IsNullOrEmpty(_options.CustomRulesPath) && !_options.PackEmbeddedRules)
        {
            throw new OpException(MsgHelp.GetString(MsgHelp.ID.CMD_NORULES_SPECIFIED));
        }
    }


    /// <summary>
    ///     Intentional as no identified value in calling from DLL at this time
    /// </summary>
    /// <returns></returns>
    public PackRulesResult GetResult()
    {
        _logger.LogTrace("PackRulesCommand::ConfigRules");
        _logger.LogInformation(MsgHelp.GetString(MsgHelp.ID.CMD_RUNNING), "Pack Rules");

        PackRulesResult packRulesResult = new()
        {
            AppVersion = Utils.GetVersionString()
        };

        try
        {
            RulesVerifierOptions options = new()
            {
                LoggerFactory = _loggerFactory,
                LanguageSpecs = Languages.FromConfigurationFiles(_loggerFactory, _options.CustomCommentsPath,
                    _options.CustomLanguagesPath),
                DisableRequireUniqueIds = _options.DisableRequireUniqueIds,
                RequireMustMatch = _options.RequireMustMatch,
                RequireMustNotMatch = _options.RequireMustNotMatch
            };
            RulesVerifier verifier = new(options);
            var ruleSet = _options.PackEmbeddedRules ? RuleSetUtils.GetDefaultRuleSet() : new RuleSet();
            if (!string.IsNullOrEmpty(_options.CustomRulesPath))
            {
                ruleSet.AddPath(_options.CustomRulesPath);
            }

            var result = verifier.Verify(ruleSet);
            if (!result.Verified)
            {
                throw new OpException(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_RESULTS_FAIL));
            }

            packRulesResult.Rules = result.CompiledRuleSet.GetAppInspectorRules().ToList();
            packRulesResult.ResultCode = PackRulesResult.ExitCode.Success;
        }
        catch (OpException e)
        {
            _logger.LogError(e.Message);
            //caught for CLI callers with final exit msg about checking log or throws for DLL callers
            throw;
        }

        return packRulesResult;
    }
}