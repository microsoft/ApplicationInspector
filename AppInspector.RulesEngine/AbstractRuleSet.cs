// Copyright (C) Microsoft. All rights reserved. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     Base class for a set of <see cref="Rule" /> objects to be operated on by the <see cref="RuleProcessor" />. This is
///     the abstract class used to allow for the default <see cref="RuleSet" /> and <see cref="TypedRuleSet{T}" /> for use
///     with rules that have extra properties.
/// </summary>
public abstract class AbstractRuleSet
{
    protected readonly List<ConvertedOatRule> _oatRules = new();
    private readonly Regex _searchInRegex = new("\\((.*),(.*)\\)", RegexOptions.Compiled);
    protected ILogger _logger = NullLogger.Instance;
    protected IEnumerable<Rule> _rules => _oatRules.Select(x => x.AppInspectorRule);
    public bool EnableNonBacktrackingRegex {  get; set; }

    /// <summary>
    ///     Filters rules within Ruleset by language
    /// </summary>
    /// <param name="language"></param>
    /// <returns> Filtered rules </returns>
    public IEnumerable<ConvertedOatRule> ByLanguage(string language)
    {
        if (!string.IsNullOrEmpty(language))
        {
            return _oatRules.Where(x =>
                x.AppInspectorRule.AppliesTo is { } appliesList && appliesList.Contains(language));
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
            return _oatRules.Where(x => x.AppInspectorRule.CompiledFileRegexes.Any(y => y.IsMatch(input) && !x.AppInspectorRule.CompiledExcludeFileRegexes.Any(z => z.IsMatch(input))));
        }

        return Array.Empty<ConvertedOatRule>();
    }

    /// <summary>
    ///     Get the set of rules that apply to all files
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ConvertedOatRule> GetUniversalRules()
    {
        return _oatRules.Where(x => x.AppInspectorRule.IsUniversal);
    }

    /// <summary>
    ///     Convert an AppInspector rule into an OAT rule.
    /// </summary>
    /// <param name="rule">The <see cref="Rule" /> to convert.</param>
    /// <returns>A <see cref="ConvertedOatRule" /> if the AI rule was valid otherwise null.</returns>
    public ConvertedOatRule? AppInspectorRuleToOatRule(Rule rule)
    {
        var clauses = new List<Clause>();
        var conditionNumber = 0;
        var patternExprs = new List<string>();
        var patternLabelCounter = 0;  // Stable pattern label, independent of clause numbering

        foreach (var pattern in rule.Patterns)
        {
            // "b" and "nb" can be added manually to rules. Options are exclusive.
            if (EnableNonBacktrackingRegex)
            {
                // non-backtracking on. "b" will override "nb"
                if (pattern.Modifiers.Any(m => m.Equals("b", StringComparison.InvariantCultureIgnoreCase)))
                {
                    pattern.Modifiers.RemoveAll(m => m.Equals("nb", StringComparison.InvariantCultureIgnoreCase));
                }
                else
                {
                    if (!pattern.Modifiers.Any(m => m.Equals("nb", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        pattern.Modifiers.Add("nb");
                    }
                }    
               
            }
            else
            {
                // backtracking on. "nb" will override "b"
                if (pattern.Modifiers.Any(m => m.Equals("nb", StringComparison.InvariantCultureIgnoreCase)))
                {
                    pattern.Modifiers.RemoveAll(m => m.Equals("b", StringComparison.InvariantCultureIgnoreCase));
                }

                // "b" is a default option for regex engine, so no need to add "b" explicitly
            }            

            // Pass the stable pattern label (used for pattern indexing) and the running condition counter separately
            var patternExpression = ProcessPatternWithConditions(pattern, clauses, patternLabelCounter, ref conditionNumber);
            if (patternExpression != null)
            {
                patternExprs.Add(patternExpression);
                patternLabelCounter++;
            }
            else
            {
                _logger.LogWarning("Clause could not be generated from pattern {pattern}", pattern.Pattern);
            }
        }

        if (clauses.Count == 0)
        {
            return new ConvertedOatRule(rule.Id, rule);
        }

        var patternBody = string.Join(" OR ", patternExprs);
        // OAT rejects any expression token that begins with more than one open parenthesis, so the pattern group is
        // only wrapped when it does not already begin with one. That is safe because OAT evaluates expressions
        // strictly left to right with no operator precedence, so the rule level conditions appended below still
        // apply to the whole pattern group either way.
        var generatedExpression = new StringBuilder(patternBody.StartsWith('(') ? patternBody : $"({patternBody})");

        foreach (var condition in rule.Conditions ?? Array.Empty<SearchCondition>())
        {
            var conditionLabel = condition.Label ?? ConditionLabel(conditionNumber);
            var clause = GenerateCondition(condition, conditionLabel, null);
            if (clause is { })
            {
                clauses.Add(clause);
                generatedExpression.Append(" AND ");
                generatedExpression.Append(conditionLabel);
                conditionNumber++;
            }
        }

        // Evaluating an expression recurses once per level of parenthesis nesting, so a deeply nested one
        // would exhaust the stack before any rule could be reported. Refuse the rule instead.
        if (rule.Expression is { } authored && RuleExpression.MaxNestingOf(authored) > RuleExpression.MaxNestingDepth)
        {
            _logger.LogError(
                "Expression in rule {id} nests parentheses more than {max} deep and will not be used. This rule will not match anything.",
                rule.Id, RuleExpression.MaxNestingDepth);
            return new ConvertedOatRule(rule.Id, rule);
        }

        // An authored expression is a per-finding predicate, applied by the RuleProcessor. The engine is only asked
        // whether the file is worth examining, so it gets a plain disjunction of every clause: a superset of the
        // authored expression that still forces each clause to run and contribute its captures. Handing the engine
        // the authored expression instead would evaluate NOT file-wide, letting one compliant finding suppress
        // sibling findings that individually satisfy the rule.
        if (!string.IsNullOrWhiteSpace(rule.Expression))
        {
            return new ConvertedOatRule(rule.Id, rule)
            {
                Clauses = clauses,
                Expression = string.Join(" OR ", clauses.Select(clause => clause.Label))
            };
        }

        return new ConvertedOatRule(rule.Id, rule)
        {
            Clauses = clauses,
            Expression = generatedExpression.ToString()
        };
    }

    /// <summary>
    ///     Builds the OAT clause for a condition.
    /// </summary>
    /// <param name="condition">The condition to convert.</param>
    /// <param name="clauseLabel">
    ///     The label to give the clause. Conditions use their own label namespace so that they cannot collide with the
    ///     numeric labels pattern clauses require.
    /// </param>
    /// <param name="ownerPatternIndex">
    ///     The index of the pattern that declared this condition, or null when the condition is declared at rule level and
    ///     therefore gates every pattern.
    /// </param>
    private Clause? GenerateCondition(SearchCondition condition, string clauseLabel, int? ownerPatternIndex)
    {
        if (condition.Pattern is { } conditionPattern)
        {
            var subClause = GenerateClause(conditionPattern);
            if (subClause is null)
            {
                _logger.LogWarning("SubClause for condition could not be generated");
            }
            else
            {
                if (condition.SearchIn?.Equals("finding-only", StringComparison.InvariantCultureIgnoreCase) != false)
                {
                    var clause = NewWithinClause();
                    clause.FindingOnly = true;
                    return clause;
                }

                if (condition.SearchIn.StartsWith("finding-region", StringComparison.InvariantCultureIgnoreCase))
                {
                    var argList = new List<int>();
                    var m = _searchInRegex.Match(condition.SearchIn);
                    if (m.Success)
                    {
                        for (var i = 1; i < m.Groups.Count; i++)
                            if (int.TryParse(m.Groups[i].Value, out var value))
                            {
                                argList.Add(value);
                            }
                            else
                            {
                                break;
                            }
                    }

                    if (argList.Count == 2)
                    {
                        var clause = NewWithinClause();
                        clause.FindingRegion = true;
                        clause.Before = argList[0];
                        clause.After = argList[1];
                        return clause;
                    }
                }
                else if (condition.SearchIn.Equals("same-line", StringComparison.InvariantCultureIgnoreCase))
                {
                    var clause = NewWithinClause();
                    clause.SameLineOnly = true;
                    return clause;
                }
                else if (condition.SearchIn.Equals("same-file", StringComparison.InvariantCultureIgnoreCase))
                {
                    var clause = NewWithinClause();
                    clause.SameFile = true;
                    return clause;
                }
                else if (condition.SearchIn.Equals("only-before", StringComparison.InvariantCultureIgnoreCase))
                {
                    var clause = NewWithinClause();
                    clause.OnlyBefore = true;
                    return clause;
                }
                else if (condition.SearchIn.Equals("only-after", StringComparison.InvariantCultureIgnoreCase))
                {
                    var clause = NewWithinClause();
                    clause.OnlyAfter = true;
                    return clause;
                }
                else
                {
                    _logger.LogWarning(
                        "Search condition {Condition} is not one of the accepted values and this condition will be ignored",
                        condition.SearchIn);
                }

                WithinClause NewWithinClause()
                {
                    return new WithinClause(subClause)
                    {
                        Label = clauseLabel,
                        Invert = condition.NegateFinding,
                        LanguageAppliesTo = condition.AppliesTo,
                        LanguageDoesNotApplyTo = condition.DoesNotApplyTo,
                        OwnerPatternIndex = ownerPatternIndex
                    };
                }
            }
        }

        return null;
    }

    /// <summary>
    ///     Conditions are labelled in their own namespace so that an automatically numbered condition cannot collide
    ///     with an automatically numbered pattern. The pattern a finding belongs to travels on the clause's
    ///     <see cref="OatExtensions.OatRegexWithIndexClause.PatternIndex" />, not its label, so labels are free to be
    ///     anything the author chooses.
    /// </summary>
    private static string ConditionLabel(int conditionNumber)
    {
        return "c" + conditionNumber.ToString(CultureInfo.InvariantCulture);
    }

    private Clause? GenerateClause(SearchPattern pattern, int clauseNumber = -1, string? label = null)
    {
        if (pattern.Pattern != null)
        {
            var scopes = pattern.Scopes ?? new[] { PatternScope.All };
            var clauseLabel = label ?? clauseNumber.ToString(CultureInfo.InvariantCulture);
            if (pattern.PatternType is PatternType.String or PatternType.Substring)
            {
                return new OatSubstringIndexClause(scopes, useWordBoundaries: pattern.PatternType == PatternType.String,
                    xPaths: pattern.XPaths, jsonPaths: pattern.JsonPaths, yamlPaths: pattern.YamlPaths, xPathNameSpaces: pattern.XPathNamespaces)
                {
                    Label = clauseLabel,
                    PatternIndex = clauseNumber,
                    Data = new List<string> { pattern.Pattern },
                    Capture = true,
                    Arguments = pattern.Modifiers,
                };
            }

            if (pattern.PatternType == PatternType.Regex)
            {
                return new OatRegexWithIndexClause(scopes, null, pattern.XPaths, pattern.JsonPaths, pattern.YamlPaths, pattern.XPathNamespaces)
                {
                    Label = clauseLabel,
                    PatternIndex = clauseNumber,
                    Data = new List<string> { pattern.Pattern },
                    Capture = true,
                    Arguments = pattern.Modifiers,
                };
            }

            if (pattern.PatternType == PatternType.RegexWord)
            {
                return new OatRegexWithIndexClause(scopes, null, pattern.XPaths, pattern.JsonPaths, pattern.YamlPaths, pattern.XPathNamespaces)
                {
                    Label = clauseLabel,
                    PatternIndex = clauseNumber,
                    Data = new List<string> { $"\\b({pattern.Pattern})\\b" },
                    Capture = true,
                    Arguments = pattern.Modifiers,
                };
            }
        }

        return null;
    }

    /// <summary>
    ///     Process a single pattern and its associated conditions, building the expression and clauses.
    /// </summary>
    /// <param name="pattern">The search pattern to process</param>
    /// <param name="clauses">List of clauses to append to</param>
    /// <param name="patternLabel">Stable label for the pattern clause (used for pattern indexing)</param>
    /// <param name="conditionNumber">Running counter used to label condition clauses</param>
    /// <returns>Expression string for this pattern and its conditions</returns>
    private string? ProcessPatternWithConditions(SearchPattern pattern, List<Clause> clauses, int patternLabel, ref int conditionNumber)
    {
        // An author supplied label replaces the numeric one in the expression; the pattern index is carried
        // separately on the clause, so naming a pattern cannot disturb which pattern a finding is reported against.
        var label = pattern.Label ?? patternLabel.ToString(CultureInfo.InvariantCulture);

        if (GenerateClause(pattern, patternLabel, label) is not { } primaryClause)
        {
            return null;
        }

        clauses.Add(primaryClause);
        var expressionText = new StringBuilder();
        expressionText.Append(label);

        // Apply pattern-specific conditions if they exist
        var addedCondition = false;
        foreach (var specificCondition in pattern.Conditions ?? Array.Empty<SearchCondition>())
        {
            var conditionLabel = specificCondition.Label ?? ConditionLabel(conditionNumber);
            if (GenerateCondition(specificCondition, conditionLabel, patternLabel) is not { } specificCondClause)
            {
                continue;
            }

            clauses.Add(specificCondClause);
            expressionText.Append(" AND ");
            expressionText.Append(conditionLabel);
            conditionNumber++;
            addedCondition = true;
        }

        // Parenthesize only when conditions were actually added, so that a pattern whose conditions all failed to
        // generate does not produce a redundant group.
        return addedCondition ? $"({expressionText})" : expressionText.ToString();
    }

    /// <summary>
    ///     Get the OAT Rules used in this RuleSet.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ConvertedOatRule> GetOatRules()
    {
        return _oatRules;
    }

    /// <summary>
    ///     Get the AppInspector Rules contained in this RuleSet.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Rule> GetAppInspectorRules()
    {
        return _rules;
    }
}