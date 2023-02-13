// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using gfs.YamlDotNet.YamlPath;
using JsonCons.JsonPath;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     Common helper used by VerifyRulesCommand and PackRulesCommand classes to reduce duplication
/// </summary>
public class RulesVerifier
{
    private readonly Analyzer _analyzer;
    private readonly ILogger _logger;
    private readonly RulesVerifierOptions _options;

    public RulesVerifier(RulesVerifierOptions options)
    {
        _options = options;
        _logger = _options.LoggerFactory?.CreateLogger<RulesVerifier>() ?? NullLogger<RulesVerifier>.Instance;
        _analyzer = _options.Analyzer ?? new ApplicationInspectorAnalyzer(_options.LoggerFactory);
    }

    private ILoggerFactory? _loggerFactory => _options.LoggerFactory;

    /// <summary>
    ///     Compile ruleset from a path to a directory or file containing a rule.json file and verify the status of the rules.
    /// </summary>
    /// <param name="fileName">Path to rules.</param>
    /// <returns></returns>
    /// <exception cref="OpException"></exception>
    public RulesVerifierResult Verify(string rulesPath)
    {
        RuleSet CompiledRuleset = new(_loggerFactory);

        if (!string.IsNullOrEmpty(rulesPath))
        {
            if (Directory.Exists(rulesPath))
            {
                CompiledRuleset.AddDirectory(rulesPath);
            }
            else if (File.Exists(rulesPath))
            {
                CompiledRuleset.AddFile(rulesPath);
            }
            else
            {
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_RULE_PATH, rulesPath));
            }
        }

        return Verify(CompiledRuleset);
    }

    public RulesVerifierResult Verify(AbstractRuleSet ruleset)
    {
        return new RulesVerifierResult(CheckIntegrity(ruleset), ruleset);
    }

    public List<RuleStatus> CheckIntegrity(AbstractRuleSet ruleSet)
    {
        List<RuleStatus> ruleStatuses = new();
        foreach (var rule in ruleSet.GetOatRules())
        {
            var ruleVerified = CheckIntegrity(rule);

            ruleStatuses.Add(ruleVerified);
        }

        if (!_options.DisableRequireUniqueIds)
        {
            var duplicatedRules = ruleSet.GetAppInspectorRules().GroupBy(x => x.Id).Where(y => y.Count() > 1);
            foreach (var rule in duplicatedRules)
            {
                _logger.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_DUPLICATEID_FAIL), rule.Key);
                var relevantStati = ruleStatuses.Where(x => x.RulesId == rule.Key);
                foreach (var status in relevantStati)
                    status.Errors =
                        status.Errors.Append(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_DUPLICATEID_FAIL, rule.Key));
            }
        }

        return ruleStatuses;
    }

    public RuleStatus CheckIntegrity(ConvertedOatRule convertedOatRule)
    {
        List<string> errors = new();

        // App Inspector checks
        var rule = convertedOatRule.AppInspectorRule;
        // Check for null Id
        if (string.IsNullOrEmpty(rule.Id))
        {
            _logger.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_NULLID_FAIL), rule.Name);
            errors.Add(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_NULLID_FAIL, rule.Name));
        }

        //applicability
        if (rule.AppliesTo != null)
        {
            var languages = _options.LanguageSpecs.GetNames();
            // Check for unknown language
            foreach (var lang in rule.AppliesTo)
                if (!string.IsNullOrEmpty(lang))
                {
                    if (!languages.Any(x => x.Equals(lang, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        _logger.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_LANGUAGE_FAIL), rule.Id ?? "", lang);
                        errors.Add(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_LANGUAGE_FAIL, rule.Id ?? "", lang));
                    }
                }
        }

        foreach (var pattern in rule.FileRegexes ?? Array.Empty<string>())
            try
            {
                _ = new Regex(pattern, RegexOptions.Compiled);
            }
            catch (Exception e)
            {
                _logger?.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL), rule.Id ?? "", pattern ?? "",
                    e.Message);
                errors.Add(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL, rule.Id ?? "", pattern ?? "",
                    e.Message));
            }

        //valid search pattern
        foreach (var searchPattern in rule.Patterns ?? Array.Empty<SearchPattern>())
        {
            if (searchPattern.PatternType == PatternType.RegexWord || searchPattern.PatternType == PatternType.Regex)
            {
                try
                {
                    if (string.IsNullOrEmpty(searchPattern.Pattern))
                    {
                        throw new ArgumentException();
                    }
                    RegexOptions regexOpts = Utils.RegexModifierToRegexOptions(searchPattern.Modifiers ?? Array.Empty<string>());

                    _ = new Regex(searchPattern.Pattern, regexOpts);
                }
                catch (Exception e)
                {
                    _logger?.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL), rule.Id ?? "",
                        searchPattern.Pattern ?? "", e.Message);
                    errors.Add(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL, rule.Id ?? "",
                        searchPattern.Pattern ?? "", e.Message));
                }
            }

            if (searchPattern.JsonPaths is not null)
            {
                foreach (var jsonPath in searchPattern.JsonPaths)
                {
                    try
                    {
                        _ = JsonSelector.Parse(jsonPath);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(
                            "The provided JsonPath '{JsonPath}' value was not valid in Rule {Id} : {message}",
                            searchPattern.JsonPaths, rule.Id, e.Message);
                        errors.Add(string.Format("The provided JsonPath '{0}' value was not valid in Rule {1} : {2}",
                            searchPattern.JsonPaths, rule.Id, e.Message));
                    }
                }
            }

            if (searchPattern.XPaths is not null)
            {
                foreach (var xpath in searchPattern.XPaths)
                {
                    try
                    {
                        XPathExpression.Compile(xpath);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError("The provided XPath '{XPath}' value was not valid in Rule {Id} : {message}",
                            searchPattern.XPaths, rule.Id, e.Message);
                        errors.Add(string.Format("The provided XPath '{0}' value was not valid in Rule {1} : {2}",
                            searchPattern.JsonPaths, rule.Id, e.Message));
                    }
                }
                
                if (searchPattern.YamlPaths is not null)
                {
                    foreach (var yamlPath in searchPattern.YamlPaths)
                    {
                        var problems = YamlPathExtensions.GetQueryProblems(yamlPath);
                        if (!problems.Any())
                        {
                            continue;
                        }

                        _logger?.LogError("The provided YamlPath '{XPath}' value was not valid in Rule {Id} : {message}",
                            searchPattern.XPaths, rule.Id, string.Join(',',problems));
                        errors.Add(string.Format("The provided XPath '{0}' value was not valid in Rule {1} : {2}",
                            searchPattern.JsonPaths, string.Join(',',problems)));
                    }
                }
            }

            // validate conditions
            foreach (var condition in rule.Conditions ?? Array.Empty<SearchCondition>())
            {
                if (condition.SearchIn is null)
                {
                    _logger?.LogError("SearchIn is null in {ruleId}", rule.Id);
                    errors.Add($"SearchIn is null in {rule.Id}");
                }
                else if (condition.SearchIn.StartsWith("finding-region"))
                {
                    var parSplits = condition.SearchIn.Split(')', '(');
                    if (parSplits.Length == 3)
                    {
                        var splits = parSplits[1].Split(',');
                        if (splits.Length == 2)
                        {
                            if (int.TryParse(splits[0], out var int1) && int.TryParse(splits[1], out var int2))
                            {
                                if (int1 > 0 && int2 < 0)
                                {
                                    _logger?.LogError(
                                        "The finding region must have a negative number or 0 for the lines before and a positive number or 0 for lines after. {0}",
                                        rule.Id);
                                    errors.Add(
                                        $"The finding region must have a negative number or 0 for the lines before and a positive number or 0 for lines after. {rule.Id}");
                                }
                            }
                        }
                        else
                        {
                            _logger?.LogError("Improperly specified finding region. {id}", rule.Id);
                            errors.Add($"Improperly specified finding region. {rule.Id}");
                        }
                    }
                    else
                    {
                        _logger?.LogError("Improperly specified finding region. {id}", rule.Id);
                        errors.Add($"Improperly specified finding region. {rule.Id}");
                    }
                }
            }
            

        var singleList = new[] { convertedOatRule };

        // We need to provide a language for the TextContainer, which will later be referenced by the Rule when executed.
        // We can grab any Language that the rule applies to, if there are none, it means it applies to all languages, except any in DoesNotApplyTo.
        // Then we fall back to grab any language from the languages configuration that isn't DoesNotApplyTo for this rule.
        var language = convertedOatRule.AppInspectorRule.AppliesTo?.FirstOrDefault() ??
                       _options.LanguageSpecs.GetNames().FirstOrDefault(x =>
                           !convertedOatRule.AppInspectorRule.DoesNotApplyTo?.Contains(x,
                               StringComparer.InvariantCultureIgnoreCase) ?? true) ?? "csharp";

        // validate all must match samples are matched
        foreach (var mustMatchElement in rule.MustMatch ?? Array.Empty<string>())
        {
            var tc = new TextContainer(mustMatchElement, language, _options.LanguageSpecs);
            if (!_analyzer.Analyze(singleList, tc).Any())
            {
                _logger?.LogError("Rule {ID} does not match the 'MustMatch' test {MustMatch}. ", rule.Id,
                    mustMatchElement);
                errors.Add($"Rule {rule.Id} does not match the 'MustMatch' test {mustMatchElement}. ");
            }
        }

        // validate no must not match conditions are matched
        foreach (var mustNotMatchElement in rule.MustNotMatch ?? Array.Empty<string>())
        {
            var tc = new TextContainer(mustNotMatchElement, language, _options.LanguageSpecs);
            if (_analyzer.Analyze(singleList, tc).Any())
            {
                _logger?.LogError("Rule {ID} matches the 'MustNotMatch' test '{MustNotMatch}'. ", rule.Id,
                    mustNotMatchElement);
                errors.Add($"Rule {rule.Id} matches the 'MustNotMatch' test '{mustNotMatchElement}'.");
            }
        }

        if (rule.Tags?.Length == 0)
        {
            _logger?.LogError("Rule must specify tags. {0}", rule.Id);
            errors.Add($"Rule must specify tags. {rule.Id}");
        }

        if (_options.RequireMustMatch)
        {
            if (rule.MustMatch?.Any() is not true)
            {
                _logger?.LogError("Rule must specify MustMatch when `RequireMustMatch` is set. {0}", rule.Id);
                errors.Add($"Rule must specify MustMatch when `RequireMustMatch` is set. {rule.Id}");
            }
        }

        if (_options.RequireMustNotMatch)
        {
            if (rule.MustNotMatch?.Any() is not true)
            {
                _logger?.LogError("Rule must specify MustNotMatch when `RequireMustNotMatch` is set. {0}", rule.Id);
                errors.Add($"Rule must specify MustNotMatch when `RequireMustNotMatch` is set. {rule.Id}");
            }
        }

        return new RuleStatus
        {
            RulesId = rule.Id,
            RulesName = rule.Name,
            Errors = errors,
            OatIssues = _analyzer.EnumerateRuleIssues(convertedOatRule),
            HasPositiveSelfTests = rule.MustMatch?.Length > 0,
            HasNegativeSelfTests = rule.MustNotMatch?.Length > 0
        };
    }
}