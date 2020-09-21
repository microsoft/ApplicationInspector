// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.ApplicationInspector.RulesEngine;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.Commands
{
    /// <summary>
    /// Common helper used by VerifyRulesCommand and PackRulesCommand classes to reduce duplication
    /// </summary>
    internal class RulesVerifier
    {
        private RuleSet? _rules;
        private readonly string? _rulesPath;
        private readonly Logger? _logger;
        private readonly bool _failFast;
        private bool _verified;
        public bool IsVerified => _verified;
        public RuleSet? CompiledRuleset => _rules;
        private List<RuleStatus>? _ruleStatuses;

        public RulesVerifier(string? rulePath, Logger? logger, bool failFast = true)
        {
            _logger = logger;
            _rulesPath = rulePath;
            _failFast = failFast;
            _rules = new RuleSet(_logger);
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

                CheckIntegrity();
            }

            return _ruleStatuses;
        }

        public bool Verify(Rule rule)
        {
            return CheckIntegrity(rule);
        }

        private void CheckIntegrity()
        {
            _verified = true;

            foreach (Rule rule in _rules.AsEnumerable())
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
                Rule sameRule = _rules.FirstOrDefault(x => x.Id == rule.Id);
                if (_rules.Count(x => x.Id == rule.Id) > 1)
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
                            isValid = false;
                        }
                    }
                }
            }

            //valid search pattern
            foreach (SearchPattern searchPattern in rule.Patterns ?? new SearchPattern[] { })
            {
                try
                {
                    Regex regex = new Regex(searchPattern.Pattern);
                }
                catch (Exception e)
                {
                    _logger?.Error(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL, rule.Id??"", searchPattern.Pattern??"", e.Message));
                    isValid = false;
                    break;
                }
            }

            if (rule.Tags?.Length == 0)
            {
                isValid = false;
            }

            return isValid;
        }

        #region basicFileIO
        private void LoadDirectory(string? path)
        {
            foreach (string filename in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            {
                LoadFile(filename);
            }
        }

        private void LoadFile(string file)
        {
            try
            {
                _rules?.AddFile(file, null);
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);//Ensure console message indicates problem for Build process
                WriteOnce.SafeLog(e.Message, NLog.LogLevel.Error);
                throw new OpException(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULE_LOADFILE_FAILED, file));
            }
        }

        #endregion
    }
}