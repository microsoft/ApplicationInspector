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
    ///     Base class for a set of <see cref="Rule"/> objects to be operated on by the <see cref="RuleProcessor"/>. This is the abstract class used to allow for the default <see cref="RuleSet"/> and <see cref="TypedRuleSet{T}"/> for use with rules that have extra properties.
    /// </summary>
    public abstract class AbstractRuleSet
    {
        protected ILogger _logger = NullLogger.Instance;
        protected readonly List<ConvertedOatRule> _oatRules = new();
        protected IEnumerable<Rule> _rules { get => _oatRules.Select(x => x.AppInspectorRule); }
        private readonly Regex _searchInRegex = new("\\((.*),(.*)\\)", RegexOptions.Compiled);
        
        /// <summary>
        ///     Load rules from a file or directory
        /// </summary>
        /// <param name="path"> File or directory path containing rules</param>
        /// <param name="tag"> Tag for the rules </param>
        /// <exception cref="ArgumentException">Thrown if the filename is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified file cannot be found on the file system</exception>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">Thrown if the specified file cannot be deserialized as a <see cref="List{T}"/></exception>
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
        ///     Parse a directory with rule files and attempts to load all .json files in the directory as rules
        /// </summary>
        /// <param name="path"> Path to rules folder </param>
        /// <param name="tag"> Tag for the rules </param>
        /// <exception cref="ArgumentException">Thrown if the filename is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified file cannot be found on the file system</exception>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">Thrown if the specified file cannot be deserialized as a <see cref="List{T}"/></exception>
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
        /// <exception cref="ArgumentException">Thrown if the filename is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified file cannot be found on the file system</exception>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">Thrown if the specified file cannot be deserialized as a <see cref="List{T}"/></exception>
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
            if (AppInspectorRuleToOatRule(rule) is { } oatRule)
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
        /// <param name="jsonString"> JSON string </param>
        /// <param name="sourceName"> Name of the source (file, stream, etc..) </param>
        /// <param name="tag">Additional runtime tag for the rules </param>
        /// <returns>If the rules were added successfully</returns>
        public void AddString(string jsonString, string sourceName, string? tag = null)
        {
            if (StringToRules(jsonString ?? string.Empty, sourceName ?? string.Empty, tag) is { } deserializedList)
            {
                AddRange(deserializedList);
            }
        }

        /// <summary>
        ///     Filters rules within Ruleset by language
        /// </summary>
        /// <param name="language"></param>
        /// <returns> Filtered rules </returns>
        public IEnumerable<ConvertedOatRule> ByLanguage(string language)
        {
            if (!string.IsNullOrEmpty(language))
            {
                return _oatRules.Where(x => x.AppInspectorRule.AppliesTo is { } appliesList && appliesList.Contains(language));
            }
            return Array.Empty<ConvertedOatRule>();
        }

        /// <summary>
        ///     Filters rules within Ruleset filename
        /// </summary>
        /// <param name="input"></param>
        /// <returns> Filtered rules </returns>
        public IEnumerable<ConvertedOatRule> ByFilename(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                return _oatRules.Where(x => x.AppInspectorRule.CompiledFileRegexes.Any(y => y.IsMatch(input)));
            }
            return Array.Empty<ConvertedOatRule>();
        }

        /// <summary>
        /// Get the set of rules that apply to all files
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ConvertedOatRule> GetUniversalRules()
        {
            return _oatRules.Where(x => (x.AppInspectorRule.FileRegexes is null || x.AppInspectorRule.FileRegexes.Length == 0) && (x.AppInspectorRule.AppliesTo is null || x.AppInspectorRule.AppliesTo.Length == 0));
        }

        /// <summary>
        ///     Convert an AppInspector rule into an OAT rule.
        /// </summary>
        /// <param name="rule">The <see cref="Rule"/> to convert.</param>
        /// <returns>A <see cref="ConvertedOatRule"/> if the AI rule was valid otherwise null.</returns>
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

        /// <summary>
        /// Get the OAT Rules used in this RuleSet.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ConvertedOatRule> GetOatRules() => _oatRules;

        /// <summary>
        /// Get the AppInspector Rules contained in this RuleSet.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Rule> GetAppInspectorRules() => _rules;

        /// <summary>
        /// Deserialize a JSON formatted string.
        /// </summary>
        /// <param name="jsonString"></param>
        /// <param name="sourceName"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">Thrown if the specified string cannot be deserialized as a <see cref="List{T}"/></exception>
        internal abstract IEnumerable<Rule> StringToRules(string jsonString, string sourceName, string? tag = null);
    }
}
