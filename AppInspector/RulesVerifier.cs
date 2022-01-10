// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.Commands
{
    using Microsoft.ApplicationInspector.RulesEngine;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.ApplicationInspector.Common;

    /// <summary>
    /// Common helper used by VerifyRulesCommand and PackRulesCommand classes to reduce duplication
    /// </summary>
    internal class RulesVerifier
    {
        public RuleSet CompiledRuleset { get; set; }
        private readonly string? _rulesPath;
        private readonly Logger? _logger;
        private readonly bool _failFast;
        private bool _verified;
        public bool IsVerified => _verified;
        private List<RuleStatus>? _ruleStatuses;

        public RulesVerifier(string? rulePath, Logger? logger, bool failFast = true)
        {
            _logger = logger;
            _rulesPath = rulePath;
            _failFast = failFast;
            CompiledRuleset = new RuleSet(_logger);
        }

        /// <summary>
        /// Return list of rule verification results
        /// </summary>
        /// <returns></returns>
        public List<RuleStatus> Verify()
        {
            _ruleStatuses = new List<RuleStatus>();
            if (!string.IsNullOrEmpty(_rulesPath))
            {
                if (Directory.Exists(_rulesPath))
                {
                    LoadDirectory(_rulesPath);
                }
                else if (File.Exists(_rulesPath))
                {
                    LoadFile(_rulesPath);
                }
                else
                {
                    throw new OpException(MsgHelp.FormatString(MsgHelp.ID.CMD_INVALID_RULE_PATH, _rulesPath));
                }
            }

            CheckIntegrity();

            return _ruleStatuses;
        }

        public bool Verify(Rule rule)
        {
            return CheckIntegrity(rule);
        }

        private void CheckIntegrity()
        {
            _verified = true;

            foreach (Rule rule in CompiledRuleset.AsEnumerable() ?? Array.Empty<Rule>())
            {
                bool ruleVerified = CheckIntegrity(rule);
                _ruleStatuses?.Add(new RuleStatus()
                {
                    RulesId = rule.Id,
                    RulesName = rule.Name,
                    Verified = ruleVerified
                });

                _verified = ruleVerified && _verified;

                if (_failFast && !ruleVerified)
                {
                    return;
                }
            }
        }
        public bool CheckIntegrity(Rule rule)
        {
            bool isValid = true;

            // Check for null Id
            if (rule.Id == null)
            {
                _logger?.Error(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_NULLID_FAIL, rule.Name));
                isValid = false;
            }
            else
            {
                // Check for same ID
                if (CompiledRuleset.Count(x => x.Id == rule.Id) > 1)
                {
                    _logger?.Error(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_DUPLICATEID_FAIL, rule.Id));
                    isValid = false;
                }
            }

            //applicability
            if (rule.AppliesTo != null)
            {
                string[] languages = Language.GetNames();
                // Check for unknown language
                foreach (string lang in rule.AppliesTo)
                {
                    if (!string.IsNullOrEmpty(lang))
                    {
                        if (!languages.Any(x => x.Equals(lang, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            _logger?.Error(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_LANGUAGE_FAIL, rule.Id ?? ""));
                            return false;
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
                    _logger?.Error(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL, rule.Id ?? "", pattern ?? "", e.Message));

                    return false;
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
                        _logger?.Error(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL, rule.Id ?? "", searchPattern.Pattern ?? "", e.Message));
                        return false;
                    }
                }
            }

            foreach(var condition in rule.Conditions ?? Array.Empty<SearchCondition>())
            {
                if (condition.SearchIn is null)
                {
                    _logger?.Error("SearchIn is null in {0}",rule.Id);
                    return false;
                }
                if (condition.SearchIn.StartsWith("finding-region"))
                {
                    var parSplits = condition.SearchIn.Split(new char[] { ')', '(' });
                    if (parSplits.Length == 3)
                    {
                        var splits = parSplits[1].Split(',');
                        if (splits.Length == 2)
                        {
                            if (int.TryParse(splits[0], out int int1) && int.TryParse(splits[1], out int int2))
                            {
                                if (int1 == 0 && int2 == 0)
                                {
                                    _logger?.Error("At least one finding region specifier must be non 0. {0}", rule.Id);
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            _logger?.Error("Improperly specified finding region. {0}", rule.Id);
                            return false;
                        }
                    }
                    else
                    {
                        _logger?.Error("Improperly specified finding region. {0}", rule.Id);
                        return false;
                    }
                }
            }

            if (rule.Tags?.Length == 0)
            {
                isValid = false;
            }

            return isValid;
        }

        public void LoadDirectory(string? path)
        {
            if (path is null) { return; }
            foreach (string filename in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            {
                LoadFile(filename);
            }
        }

        public void LoadFile(string file)
        {
            try
            {
                CompiledRuleset.AddFile(file, null);
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);//Ensure console message indicates problem for Build process
                WriteOnce.SafeLog(e.Message, NLog.LogLevel.Error);
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULE_LOADFILE_FAILED, file));
            }
        }

        public void LoadRuleSet(RuleSet ruleSet)
        {
            foreach(Rule rule in ruleSet)
            {
                CompiledRuleset.AddRule(rule);
            }
        }

        public void ClearRules()
        {
            CompiledRuleset = new RuleSet(_logger);
        }
    }
}