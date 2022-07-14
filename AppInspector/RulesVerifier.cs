// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;

namespace Microsoft.ApplicationInspector.Commands
{
    using Microsoft.ApplicationInspector.RulesEngine;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.CST.OAT;

    public class RulesVerifierResult
    {
        public RulesVerifierResult(List<RuleStatus> ruleStatuses, RuleSet compiledRuleSets)
        {
            RuleStatuses = ruleStatuses;
            CompiledRuleSet = compiledRuleSets;
        }
        public List<RuleStatus> RuleStatuses { get; }
        public RuleSet CompiledRuleSet { get; }
        public  bool Verified => RuleStatuses.All(x => x.Verified);
    }

    public class RulesVerifierOptions
    {
        /// <summary>
        /// If desired you may provide the analyzer to use. An analyzer with AI defaults will be created to use for validation.
        /// </summary>
        public Analyzer? Analyzer { get; set; }
        /// <summary>
        /// To receive log messages, provide a LoggerFactory with your preferred configuration.
        /// </summary>
        public ILoggerFactory? LoggerFactory { get; set; }
        /// <summary>
        /// If true, the verifier will stop on the first issue and will not continue reporting issues.
        /// </summary>
        public bool FailFast { get; set; }
        public Languages LanguageSpecs { get; set; } = new Languages();
    }

    /// <summary>
    /// Common helper used by VerifyRulesCommand and PackRulesCommand classes to reduce duplication
    /// </summary>
    public class RulesVerifier
    {
        private readonly ILogger _logger;
        private readonly RulesVerifierOptions _options;
        private bool _failFast => _options.FailFast;
        private ILoggerFactory? _loggerFactory => _options.LoggerFactory;
        private readonly Analyzer _analyzer;
        public RulesVerifier(RulesVerifierOptions options)
        {
            _options = options;
            _logger = _options.LoggerFactory?.CreateLogger<RulesVerifier>() ?? NullLogger<RulesVerifier>.Instance;
            _analyzer = _options.Analyzer ?? new ApplicationInspectorAnalyzer(_options.LoggerFactory);
        }

        /// <summary>
        /// Compile ruleset from a path to a directory or file containing a rule.json file and verify the status of the rules.
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

        public RulesVerifierResult Verify(RuleSet ruleset)
        {
            return new RulesVerifierResult(CheckIntegrity(ruleset), ruleset);
        }

        public List<RuleStatus> CheckIntegrity(RuleSet ruleSet)
        {
            List<RuleStatus> ruleStatuses = new();
            foreach (ConvertedOatRule rule in ruleSet.GetOatRules() ?? Array.Empty<ConvertedOatRule>())
            {
                RuleStatus ruleVerified = CheckIntegrity(rule);

                ruleStatuses.Add(ruleVerified);

                if (_failFast && !ruleVerified.Verified)
                {
                    break;
                }
            }
            var duplicatedRules = ruleSet.GroupBy(x => x.Id).Where(y => y.Count() > 1);
            if (duplicatedRules.Any())
            {
                foreach (var rule in duplicatedRules)
                {
                    _logger.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_DUPLICATEID_FAIL), rule.Key);
                    var relevantStati = ruleStatuses.Where(x => x.RulesId == rule.Key);
                    foreach(var status in relevantStati)
                    {
                        status.Errors = status.Errors.Append(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_DUPLICATEID_FAIL, rule.Key));
                    }
                    if (_failFast)
                    {
                        break;
                    }
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
                string[] languages = _options.LanguageSpecs.GetNames();
                // Check for unknown language
                foreach (string lang in rule.AppliesTo)
                {
                    if (!string.IsNullOrEmpty(lang))
                    {
                        if (!languages.Any(x => x.Equals(lang, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            _logger.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_LANGUAGE_FAIL), rule.Id ?? "");
                            errors.Add(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_LANGUAGE_FAIL, rule.Id ?? ""));
                        }
                    }
                }
            }

            foreach (var pattern in rule.FileRegexes ?? Array.Empty<string>())
            {
                try
                {
                    _ = new Regex(pattern, RegexOptions.Compiled);
                }
                catch (Exception e)
                {
                    _logger?.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL), rule.Id ?? "", pattern ?? "", e.Message);
                    errors.Add(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL, rule.Id ?? "", pattern ?? "", e.Message));
                }
            }

            //valid search pattern
            foreach (SearchPattern searchPattern in rule.Patterns ?? Array.Empty<SearchPattern>())
            {
                if (searchPattern.PatternType == PatternType.RegexWord || searchPattern.PatternType == PatternType.Regex)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(searchPattern.Pattern))
                        {
                            throw new ArgumentException();
                        }
                        _ = new Regex(searchPattern.Pattern);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL), rule.Id ?? "", searchPattern.Pattern ?? "", e.Message);
                        errors.Add(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL, rule.Id ?? "", searchPattern.Pattern ?? "", e.Message));
                    }
                }
            }

            // validate conditions
            foreach(var condition in rule.Conditions ?? Array.Empty<SearchCondition>())
            {
                if (condition.SearchIn is null)
                {
                    _logger?.LogError("SearchIn is null in {ruleId}",rule.Id);
                    errors.Add($"SearchIn is null in {rule.Id}");
                }
                else if (condition.SearchIn.StartsWith("finding-region"))
                {
                    var parSplits = condition.SearchIn.Split(new char[] { ')', '(' });
                    if (parSplits.Length == 3)
                    {
                        var splits = parSplits[1].Split(',');
                        if (splits.Length == 2)
                        {
                            if (int.TryParse(splits[0], out int int1) && int.TryParse(splits[1], out int int2))
                            {
                                if (int1 > 0 && int2 < 0)
                                {
                                    _logger?.LogError("The finding region must have a negative number or 0 for the lines before and a positive number or 0 for lines after. {0}", rule.Id);
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

            var singleList = new [] {convertedOatRule};
            
            // validate all must match samples are matched
            foreach (var mustMatchElement in rule.MustMatch ?? Array.Empty<string>())
            {
                var language = convertedOatRule.AppInspectorRule.AppliesTo?.FirstOrDefault() as string ?? "csharp";
                var tc = new TextContainer(mustMatchElement, language, _options.LanguageSpecs);
                if (!_analyzer.Analyze(singleList, tc).Any())
                {
                    _logger?.LogError("Rule {ID} does not match the 'MustMatch' test {MustMatch}. ", rule.Id, mustMatchElement);
                    errors.Add($"Rule {rule.Id} does not match the 'MustMatch' test {mustMatchElement}. ");
                }
            }
            
            // validate no must not match conditions are matched
            foreach (var mustNotMatchElement in rule.MustNotMatch ?? Array.Empty<string>())
            {
                var language = convertedOatRule.AppInspectorRule.AppliesTo?.FirstOrDefault() as string ?? "csharp";
                var tc = new TextContainer(mustNotMatchElement, language, _options.LanguageSpecs);
                if (_analyzer.Analyze(singleList, tc).Any())
                {
                    _logger?.LogError("Rule {ID} matches the 'MustNotMatch' test {MustNotMatch}. ", rule.Id, mustNotMatchElement);
                    errors.Add($"Rule {rule.Id} does not match the 'MustMatch' test {mustNotMatchElement}. ");
                }
            }
            
            if (rule.Tags?.Length == 0)
            {
                _logger?.LogError("Rule must specify tags. {0}", rule.Id);
                errors.Add($"Rule must specify tags. {rule.Id}");
            }
            return new RuleStatus()
            {
                RulesId = rule.Id,
                RulesName = rule.Name,
                Errors = errors,
                OatIssues = _analyzer.EnumerateRuleIssues(convertedOatRule)
            };
        }
    }
}