// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    ///     Storage for rules
    /// </summary>
    public abstract class AbstractRuleSet
    {
        protected ILogger _logger;
        protected readonly List<ConvertedOatRule> _oatRules = new();//used for analyze cmd primarily
        protected IEnumerable<Rule> _rules { get => _oatRules.Select(x => x.AppInspectorRule); }
        private readonly Regex _searchInRegex = new("\\((.*),(.*)\\)", RegexOptions.Compiled);
        public void AddPath(string path, string? tag = null)
        {
            if (Directory.Exists(path))
            {
                AddDirectory(path, tag);
            }
            else if (File.Exists(path))
            {
                AddFile(path, tag);
            }
            else
            {
                throw new ArgumentException("The path must exist.", nameof(path));
            }
        }

        /// <summary>
        ///     Parse a directory with rule files and loads the rules
        /// </summary>
        /// <param name="path"> Path to rules folder </param>
        /// <param name="tag"> Tag for the rules </param>
        public void AddDirectory(string path, string? tag = null)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException();

            foreach (string filename in Directory.EnumerateFileSystemEntries(path, "*.json", SearchOption.AllDirectories))
            {
                AddFile(filename, tag);
            }
        }

        /// <summary>
        ///     Load rules from a file
        /// </summary>
        /// <param name="filename"> Filename with rules </param>
        /// <param name="tag"> Tag for the rules </param>
        public void AddFile(string? filename, string? tag = null)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException(null, nameof(filename));

            if (!File.Exists(filename))
                throw new FileNotFoundException();

            using StreamReader file = File.OpenText(filename);
            AddString(file.ReadToEnd(), filename, tag);
        }

        /// <summary>
        ///     Adds the elements of the collection to the Ruleset
        /// </summary>
        /// <param name="collection"> Collection of rules </param>
        public void AddRange(IEnumerable<Rule>? collection)
        {
            foreach (Rule rule in collection ?? Array.Empty<Rule>())
            {
                AddRule(rule);
            }
        }

        /// <summary>
        ///     Add rule into Ruleset
        /// </summary>
        /// <param name="rule"> </param>
        public void AddRule(Rule rule)
        {
            if (AppInspectorRuleToOatRule(rule) is ConvertedOatRule oatRule)
            {
                _logger.LogTrace("Attempting to add rule: {RuleId}:{RuleName}", rule.Id, rule.Name);
                _oatRules.Add(oatRule);
            }
            else
            {
                _logger.LogError("Rule '{RuleId}:{RuleName}' could not be converted into an OAT rule. There may be message in the logs indicating why. You can  run rule verification to identify the issue", rule.Id, rule.Name);
            }
        }

        /// <summary>
        ///     Load rules from JSON string
        /// </summary>
        /// <param name="jsonstring"> JSON string </param>
        /// <param name="sourcename"> Name of the source (file, stream, etc..) </param>
        /// <param name="tag"> Tag for the rules </param>
        public void AddString(string jsonstring, string sourcename, string? tag = null)
        {
            AddRange(StringToRules(jsonstring ?? string.Empty, sourcename ?? string.Empty, tag));
        }

        /// <summary>
        ///     Filters rules within Ruleset by languages
        /// </summary>
        /// <param name="languages"> Languages </param>
        /// <returns> Filtered rules </returns>
        public IEnumerable<ConvertedOatRule> ByLanguage(string language)
        {
            if (!string.IsNullOrEmpty(language))
            {
                return _oatRules.Where(x => x.AppInspectorRule.AppliesTo is string[] appliesList && appliesList.Contains(language));
            }
            return Array.Empty<ConvertedOatRule>();
        }

        /// <summary>
        ///     Filters rules within Ruleset by applies to regexes
        /// </summary>
        /// <param name="languages"> Languages </param>
        /// <returns> Filtered rules </returns>
        public IEnumerable<ConvertedOatRule> ByFilename(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                return _oatRules.Where(x => x.AppInspectorRule.CompiledFileRegexes.Any(y => y.IsMatch(input)));
            }
            return Array.Empty<ConvertedOatRule>();
        }

        public IEnumerable<ConvertedOatRule> GetUniversalRules()
        {
            return _oatRules.Where(x => (x.AppInspectorRule.FileRegexes is null || x.AppInspectorRule.FileRegexes.Length == 0) && (x.AppInspectorRule.AppliesTo is null || x.AppInspectorRule.AppliesTo.Length == 0));
        }

        public ConvertedOatRule? AppInspectorRuleToOatRule(Rule rule)
        {
            var clauses = new List<Clause>();
            int clauseNumber = 0;
            var expression = new StringBuilder("(");
            foreach (var pattern in rule.Patterns)
            {
                if (pattern.Pattern != null)
                {
                    var scopes = pattern.Scopes ?? new PatternScope[] { PatternScope.All };
                    var modifiers = pattern.Modifiers?.ToList() ?? new List<string>();
                    if (pattern.PatternType is PatternType.String or PatternType.Substring)
                    {
                        clauses.Add(new OatSubstringIndexClause(scopes, useWordBoundaries: pattern.PatternType == PatternType.String)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),//important to pattern index identification
                            Data = new List<string>() { pattern.Pattern },
                            Capture = true,
                            Arguments = pattern.Modifiers?.ToList() ?? new List<string>()
                        });
                        if (clauseNumber > 0)
                        {
                            expression.Append(" OR ");
                        }
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (pattern.PatternType == PatternType.Regex)
                    {
                        clauses.Add(new OatRegexWithIndexClause(scopes)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),//important to pattern index identification
                            Data = new List<string>() { pattern.Pattern },
                            Capture = true,
                            Arguments = modifiers,
                            CustomOperation = "RegexWithIndex"
                        });
                        if (clauseNumber > 0)
                        {
                            expression.Append(" OR ");
                        }
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (pattern.PatternType == PatternType.RegexWord)
                    {
                        clauses.Add(new OatRegexWithIndexClause(scopes)
                        {
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),//important to pattern index identification
                            Data = new List<string>() { $"\\b({pattern.Pattern})\\b" },
                            Capture = true,
                            Arguments = pattern.Modifiers?.ToList() ?? new List<string>(),
                            CustomOperation = "RegexWithIndex"
                        });

                        if (clauseNumber > 0)
                        {
                            expression.Append(" OR ");
                        }
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                }
            }

            if (clauses.Count > 0)
            {
                expression.Append(')');
            }
            else
            {
                return new ConvertedOatRule(rule.Id, rule);
            }

            foreach (var condition in rule.Conditions ?? Array.Empty<SearchCondition>())
            {
                if (condition.Pattern?.Pattern != null)
                {
                    List<string> conditionModifiers = condition.Pattern.Modifiers?.ToList() ?? new();
                    if (condition.SearchIn?.Equals("finding-only", StringComparison.InvariantCultureIgnoreCase) != false)
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = conditionModifiers,
                            FindingOnly = true,
                            CustomOperation = "Within",
                            Scopes = condition.Pattern.Scopes ?? new PatternScope[] { PatternScope.All }
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (condition.SearchIn.StartsWith("finding-region", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var argList = new List<int>();
                        Match m = _searchInRegex.Match(condition.SearchIn);
                        if (m.Success)
                        {
                            for (int i = 1; i < m.Groups.Count; i++)
                            {
                                if (int.TryParse(m.Groups[i].Value, out int value))
                                {
                                    argList.Add(value);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        if (argList.Count == 2)
                        {
                            clauses.Add(new WithinClause()
                            {
                                Data = new List<string>() { condition.Pattern.Pattern },
                                Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                                Invert = condition.NegateFinding,
                                Arguments = conditionModifiers,
                                FindingRegion = true,
                                CustomOperation = "Within",
                                Before = argList[0],
                                After = argList[1],
                                Scopes = condition.Pattern.Scopes ?? new PatternScope[] { PatternScope.All }
                            });
                            expression.Append(" AND ");
                            expression.Append(clauseNumber);
                            clauseNumber++;
                        }
                    }
                    else if (condition.SearchIn.Equals("same-line", StringComparison.InvariantCultureIgnoreCase))
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = conditionModifiers,
                            SameLineOnly = true,
                            CustomOperation = "Within",
                            Scopes = condition.Pattern.Scopes ?? new PatternScope[] { PatternScope.All }
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (condition.SearchIn.Equals("same-file", StringComparison.InvariantCultureIgnoreCase))
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                            SameFile = true
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (condition.SearchIn.Equals("only-before", StringComparison.InvariantCultureIgnoreCase))
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                            OnlyBefore = true
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else if (condition.SearchIn.Equals("only-after", StringComparison.InvariantCultureIgnoreCase))
                    {
                        clauses.Add(new WithinClause()
                        {
                            Data = new List<string>() { condition.Pattern.Pattern },
                            Label = clauseNumber.ToString(CultureInfo.InvariantCulture),
                            Invert = condition.NegateFinding,
                            Arguments = condition.Pattern.Modifiers?.ToList() ?? new List<string>(),
                            OnlyAfter = true
                        });
                        expression.Append(" AND ");
                        expression.Append(clauseNumber);
                        clauseNumber++;
                    }
                    else
                    {
                        _logger.LogWarning("Search condition {Condition} is not one of the accepted values and this condition will be ignored", condition.SearchIn);
                        return null;
                    }
                }
            }
            return new ConvertedOatRule(rule.Id, rule)
            {
                Clauses = clauses,
                Expression = expression.ToString()
            };
        }

        public IEnumerable<ConvertedOatRule> GetOatRules() => _oatRules;

        public IEnumerable<Rule> GetAppInspectorRules() => _rules;

        internal abstract IEnumerable<Rule> StringToRules(string jsonstring, string sourcename, string? tag = null);
    }
}
