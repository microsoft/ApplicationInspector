// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using JsonCons.JsonPath;
using Microsoft.ApplicationInspector.Common;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.ApplicationInspector.RulesEngine.Schema;
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
    private readonly RuleSchemaProvider? _schemaProvider;

    public RulesVerifier(RulesVerifierOptions options)
    {
        _options = options;
        _logger = _options.LoggerFactory?.CreateLogger<RulesVerifier>() ?? NullLogger<RulesVerifier>.Instance;
        _analyzer = _options.Analyzer ?? new ApplicationInspectorAnalyzer(_options.LoggerFactory);
        
        if (_options.EnableSchemaValidation)
        {
            _schemaProvider = _options.SchemaProvider ?? new RuleSchemaProvider(_options.CustomSchemaPath);
        }
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
        RuleSet CompiledRuleset = new(_loggerFactory, _schemaProvider);

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

    /// <summary>
    /// Check an <see cref="AbstractRuleSet"/> for rules errors
    /// </summary>
    /// <param name="ruleSet">The rule set to check</param>
    /// <returns>An <see cref="IList{RuleStatus}"/> with a <see cref="RuleStatus"/> for each <see cref="Rule"/> in the <paramref name="ruleSet"/></returns>
    public IList<RuleStatus> CheckIntegrity(AbstractRuleSet ruleSet)
    {
        List<RuleStatus> ruleStatuses = new();
        foreach (var rule in ruleSet.GetOatRules())
        {
            var ruleVerified = CheckIntegrity(rule);

            ruleStatuses.Add(ruleVerified);
        }

        // By default unique IDs are required for rules
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

        // Check for the presence of the `depends_on` field and ensure that any tags which are depended on exist in the full set of rules
        var allTags = ruleSet.GetAppInspectorRules().SelectMany(x => x.Tags ?? Array.Empty<string>()).ToList();
        var rulesWithDependsOnWithNoMatchingTags = ruleSet.GetAppInspectorRules().Where(x => !x.DependsOnTags?.All(tag => allTags.Contains(tag)) ?? false);
        foreach(var dependslessRule in rulesWithDependsOnWithNoMatchingTags)
        {
            _logger.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_DEPENDS_ON_TAG_MISSING), dependslessRule.Id, string.Join(',', dependslessRule.DependsOnTags?.Where(tag => !allTags.Contains(tag)) ?? Array.Empty<string>()));
            foreach(var status in ruleStatuses.Where(x => x.Rule == dependslessRule))
            {
                status.Errors = status.Errors.Append(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_DEPENDS_ON_TAG_MISSING, dependslessRule.Id, string.Join(',',dependslessRule.DependsOnTags?.Where(tag => !allTags.Contains(tag)) ?? Array.Empty<string>())));
            }
        }

        // Overrides are removed on a per file basis where depends_on is removed on a cross scan basis. Because of this, if you have RuleA with no DependsOnTags which is overriden with RuleB which does have tags,
        // and then those tags are not present, you may expect to get RuleA but will not.
        // This checks to ensure if a rule is overridden it has at least all the depends on tags of its overrider
        var appInsStyleRules = ruleSet.GetAppInspectorRules();
        foreach (var rule in ruleSet.GetAppInspectorRules())
        {
            foreach(var overrde in rule.Overrides ?? Array.Empty<string>())
            {
                foreach(var overriddenRule in appInsStyleRules.Where(x => x.Id == overrde))
                {
                    var missingTags = rule.DependsOnTags?.Where(x => !(overriddenRule.DependsOnTags?.Contains(x) ?? false));
                    if (missingTags?.Any() ?? false)
                    {
                        _logger.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_OVERRIDDEN_RULE_DEPENDS_ON_TAG_MISSING), overriddenRule.Id, string.Join(',', missingTags ?? Array.Empty<string>()));
                        foreach (var status in ruleStatuses.Where(x => x.Rule == overriddenRule))
                        {
                            status.Errors = status.Errors.Append(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_OVERRIDDEN_RULE_DEPENDS_ON_TAG_MISSING, overriddenRule.Id, string.Join(',', missingTags ?? Array.Empty<string>())));
                        }
                    }
                }

            }
        }

        return ruleStatuses;
    }

    public RuleStatus CheckIntegrity(ConvertedOatRule convertedOatRule)
    {
        List<string> errors = new();
        List<SchemaValidationError> schemaErrors = new();
        bool passedSchemaValidation = true;

        // App Inspector checks
        var rule = convertedOatRule.AppInspectorRule;

        // Schema validation step (use stored result from rule loading if available)
        if (_options.EnableSchemaValidation && _schemaProvider != null)
        {
            SchemaValidationResult validationResult;
            
            // Use the stored schema validation result from rule loading if available
            if (rule.SchemaValidationResult != null)
            {
                validationResult = rule.SchemaValidationResult;
            }
            else
            {
                // Fallback to individual rule validation (inefficient)
                _logger.LogDebug("No stored schema validation result for rule {RuleId}, performing re-validation", rule.Id);
                validationResult = _schemaProvider.ValidateRule(rule);
            }
            
            schemaErrors.AddRange(validationResult.Errors);
            passedSchemaValidation = validationResult.IsValid;

            if (!validationResult.IsValid)
            {
                if (_options.SchemaValidationLevel == SchemaValidationLevel.Error)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        var errorMessage = $"Schema validation error at {error.Path}: {error.Message}";
                        errors.Add(errorMessage);
                        _logger.LogError("Schema validation error for rule {RuleId}: {Error}", rule.Id ?? "Unknown", errorMessage);
                    }
                }
                else if (_options.SchemaValidationLevel == SchemaValidationLevel.Warning)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        var errorMessage = $"Schema validation warning at {error.Path}: {error.Message}";
                        _logger.LogWarning("Schema validation warning for rule {RuleId}: {Error}", rule.Id ?? "Unknown", errorMessage);
                    }
                }
            }
        }
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

        // Check that regexes for filenames are valid
        foreach (var pattern in (IList<string>?)rule.FileRegexes ?? Array.Empty<string>())
        {
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
        }

        //valid search pattern
        foreach (var searchPattern in rule.Patterns ?? Array.Empty<SearchPattern>())
        {
            // Check that pattern regex arguments are valid
            if (searchPattern.PatternType == PatternType.RegexWord || searchPattern.PatternType == PatternType.Regex)
            {
                if (searchPattern.Pattern is null)
                {
                    _logger?.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL_PATTERN_NULL), rule.Id ?? "");
                }
                else
                {
                    Regex? resultingRegex = Utils.StringToRegex(searchPattern.Pattern, searchPattern.Modifiers, _logger);
                    if (resultingRegex is null)
                    {
                        _logger?.LogError(MsgHelp.GetString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL), rule.Id ?? "",
                                                searchPattern.Pattern ?? "", string.Join(",", searchPattern.Modifiers));
                        errors.Add(MsgHelp.FormatString(MsgHelp.ID.VERIFY_RULES_REGEX_FAIL, rule.Id ?? "",
                            searchPattern.Pattern ?? "", string.Join(",",searchPattern.Modifiers)));
                    }
                }
            }

            // Check that JsonPaths are valid
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

            // Check that XPaths are valid
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
                            searchPattern.XPaths, rule.Id, e.Message));
                    }
                }

                // Check that YamlPaths are valid
                if (searchPattern.YamlPaths is not null)
                {
                    foreach (var yamlPath in searchPattern.YamlPaths)
                    {
                        var problems = YamlPathExtensions.GetQueryProblems(yamlPath);
                        if (!problems.Any())
                        {
                            continue;
                        }

                        _logger?.LogError(
                            "The provided YamlPath '{YamlPath}' value was not valid in Rule {Id} : {message}",
                            searchPattern.YamlPaths, rule.Id, string.Join(',', problems));
                        errors.Add(string.Format("The provided YamlPath '{0}' value was not valid in Rule {1} : {2}",
                            searchPattern.YamlPaths, string.Join(',', problems)));
                    }
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


        var expressionErrors = ValidateExpression(rule, convertedOatRule).ToList();
        errors.AddRange(expressionErrors);

        var oatIssues = _analyzer.EnumerateRuleIssues(convertedOatRule).ToList();

        var singleList = new[] { convertedOatRule };

        // We need to provide a language for the TextContainer, which will later be referenced by the Rule when executed.
        // We can grab any Language that the rule applies to, if there are none, it means it applies to all languages, except any in DoesNotApplyTo.
        // Then we fall back to grab any language from the languages configuration that isn't DoesNotApplyTo for this rule.
        // Finally we fall back to a default value that has no comment styling
        var language = convertedOatRule.AppInspectorRule.AppliesTo?.FirstOrDefault() ??
                       _options.LanguageSpecs.GetNames().FirstOrDefault(x =>
                           !convertedOatRule.AppInspectorRule.DoesNotApplyTo?.Contains(x,
                               StringComparer.InvariantCultureIgnoreCase) ?? true) ?? "Rule Verifier Default Value";

        // Self tests run the rule, and a rule already known to be malformed can fail hard rather than
        // simply not matching, so only run them once the rule is structurally sound.
        var ruleIsSound = expressionErrors.Count == 0 && oatIssues.Count == 0;

        // validate all must match samples are matched
        foreach (var mustMatchElement in (IList<string>?)rule.MustMatch ?? Array.Empty<string>())
        {
            if (!ruleIsSound)
            {
                break;
            }

            if (!SelfTestMatches(singleList, mustMatchElement, language, rule, errors))
            {
                _logger?.LogError("Rule {ID} does not match the 'MustMatch' test {MustMatch}. ", rule.Id,
                    mustMatchElement);
                errors.Add($"Rule {rule.Id} does not match the 'MustMatch' test {mustMatchElement}. ");
            }
        }

        // validate no must not match conditions are matched
        foreach (var mustNotMatchElement in (IList<string>?)rule.MustNotMatch ?? Array.Empty<string>())
        {
            if (!ruleIsSound)
            {
                break;
            }

            if (SelfTestMatches(singleList, mustNotMatchElement, language, rule, errors))
            {
                _logger?.LogError("Rule {ID} matches the 'MustNotMatch' test '{MustNotMatch}'. ", rule.Id,
                    mustNotMatchElement);
                errors.Add($"Rule {rule.Id} matches the 'MustNotMatch' test '{mustNotMatchElement}'.");
            }
        }

        // Check for at least one tag being populated
        if ((rule.Tags?.Count ?? 0) == 0)
        {
            _logger?.LogError("Rule must specify tags. {0}", rule.Id);
            errors.Add($"Rule must specify tags. {rule.Id}");
        }

        // If RequireMustMatch is set every rule must have a must-match self-test
        if (_options.RequireMustMatch)
        {
            if (rule.MustMatch?.Any() is not true)
            {
                _logger?.LogError("Rule must specify MustMatch when `RequireMustMatch` is set. {0}", rule.Id);
                errors.Add($"Rule must specify MustMatch when `RequireMustMatch` is set. {rule.Id}");
            }
        }

        // If RequireMustNotMatch is set every rule must have a must-not-match self-test
        if (_options.RequireMustNotMatch)
        {
            if (rule.MustNotMatch?.Any() is not true)
            {
                _logger?.LogError("Rule must specify MustNotMatch when `RequireMustNotMatch` is set. {0}", rule.Id);
                errors.Add($"Rule must specify MustNotMatch when `RequireMustNotMatch` is set. {rule.Id}");
            }
        }
        
        // Require Description so the Sarif is valid for GitHub sarif upload action
        if (string.IsNullOrEmpty(rule.Description))
        {
            _logger?.LogError("Rule must contain a Description. {0}", rule.Id);
            errors.Add($"Rule must contain a Description. {rule.Id}");
        }

        return new RuleStatus
        {
            Rule = rule,
            RulesId = rule.Id,
            RulesName = rule.Name,
            Errors = errors,
            // Materialized because RuleStatus.Verified and any consumer reporting the issues each
            // enumerate this, and EnumerateRuleIssues re-runs the whole check on every enumeration.
            OatIssues = oatIssues,
            HasPositiveSelfTests = rule.MustMatch?.Count > 0,
            HasNegativeSelfTests = rule.MustNotMatch?.Count > 0,
            SchemaValidationErrors = schemaErrors,
            PassedSchemaValidation = passedSchemaValidation
        };
    }

    private static readonly string[] BinaryOperators = { "AND", "OR", "XOR", "NAND", "NOR" };

    private static readonly string[] ReservedLabels = { "AND", "OR", "XOR", "NAND", "NOR", "NOT" };

    /// <summary>
    ///     Runs one self-test sample. A rule can fail hard rather than simply not matching, and a bad rule
    ///     must not take verification down with it, so failures are reported as verification errors.
    /// </summary>
    private bool SelfTestMatches(ConvertedOatRule[] rules, string sample, string language, Rule rule,
        List<string> errors)
    {
        try
        {
            var tc = new TextContainer(sample, language, _options.LanguageSpecs);
            return _analyzer.Analyze(rules, tc).Any();
        }
        catch (Exception e)
        {
            _logger?.LogError("Rule {ID} failed to run against its self-test sample. {Type}: {Message}", rule.Id,
                e.GetType(), e.Message);
            errors.Add($"Rule {rule.Id} failed to run against its self-test sample. {e.GetType()}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Validates labels, <see cref="Rule.Expression" /> and condition scoping.
    /// </summary>
    private IEnumerable<string> ValidateExpression(Rule rule, ConvertedOatRule convertedOatRule)
    {
        List<string> errors = new();

        void Error(string message)
        {
            _logger?.LogError("{Message}", message);
            errors.Add(message);
        }

        foreach (var label in convertedOatRule.Clauses.Select(x => x.Label))
        {
            if (label is null)
            {
                continue;
            }

            if (label.Any(char.IsWhiteSpace) || label.Contains('(') || label.Contains(')'))
            {
                Error(
                    $"Label '{label}' in rule {rule.Id} may not contain whitespace or parentheses because expressions are split on spaces.");
            }

            // An operator named as a label reads as an operator to one evaluator and as an operand to the
            // other, so the rule would match and then report nothing.
            if (ReservedLabels.Contains(label, StringComparer.OrdinalIgnoreCase))
            {
                Error(
                    $"Label '{label}' in rule {rule.Id} is an expression operator and may not be used as a label. Reserved names are {string.Join(", ", ReservedLabels)}.");
            }
        }

        // A duplicated label makes OAT abandon the expression, so the rule would silently never match.
        foreach (var duplicate in convertedOatRule.Clauses.Select(x => x.Label).Where(x => x is not null)
                     .GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key))
        {
            Error($"Label '{duplicate}' is used by more than one pattern or condition in rule {rule.Id}.");
        }

        var patternLabels = rule.Patterns
            .Select((pattern, index) => pattern.Label ?? index.ToString(CultureInfo.InvariantCulture)).ToList();

        // Taken from the generated clauses rather than recomputed, so pattern level and rule level
        // conditions are both covered however they were numbered.
        var conditionLabels = convertedOatRule.Clauses.OfType<WithinClause>()
            .Select(x => x.Label).Where(x => x is not null).Select(x => x!).ToList();

        if (string.IsNullOrWhiteSpace(rule.Expression))
        {
            return errors;
        }

        var expression = rule.Expression!;

        if (rule.Conditions?.Any(x => x.NegateFinding) ?? false)
        {
            Error(
                $"Rule {rule.Id} supplies an expression, so negation must be expressed with NOT in the expression rather than negate_finding.");
        }

        var tokens = expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var depth = 0;
        var closedTooEarly = false;
        var nextGroup = 0;
        // Sibling groups at the same nesting level are independent, so operators are tracked per group
        // rather than per depth.
        var openGroups = new Stack<int>();
        openGroups.Push(nextGroup++);
        var operatorsByGroup = new Dictionary<int, HashSet<string>>();

        foreach (var token in tokens)
        {
            foreach (var _ in token.Where(x => x == '('))
            {
                depth++;
                openGroups.Push(nextGroup++);
            }

            var bare = token.Trim('(', ')');
            if (BinaryOperators.Contains(bare, StringComparer.OrdinalIgnoreCase))
            {
                if (!operatorsByGroup.TryGetValue(openGroups.Peek(), out var seen))
                {
                    seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    operatorsByGroup[openGroups.Peek()] = seen;
                }

                seen.Add(bare);
            }

            foreach (var _ in token.Where(x => x == ')'))
            {
                depth--;
                if (depth < 0)
                {
                    closedTooEarly = true;
                }

                if (openGroups.Count > 1)
                {
                    openGroups.Pop();
                }
            }
        }

        // A running count that ends at zero is not enough: 'a) AND (b' balances overall while closing a group
        // that was never opened.
        if (depth != 0 || closedTooEarly)
        {
            Error($"Expression '{expression}' in rule {rule.Id} has unbalanced parentheses.");
        }

        // The engine is handed a weaker expression than the authored one, so it never sees these operands and
        // cannot report them. An operand naming nothing is silently false when findings are judged, which reads
        // as the rule mysteriously matching less than it should.
        var knownLabels = new HashSet<string>(patternLabels.Concat(conditionLabels), StringComparer.Ordinal);
        var unresolved = tokens
            .Select(token => token.Trim('(', ')'))
            .Where(bare => bare.Length > 0)
            .Where(bare => !ReservedLabels.Contains(bare, StringComparer.OrdinalIgnoreCase))
            .Where(bare => !knownLabels.Contains(bare))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var bare in unresolved)
        {
            Error(
                $"Expression '{expression}' in rule {rule.Id} refers to '{bare}', which is not a pattern or condition label in this rule. Known labels are {string.Join(", ", knownLabels.OrderBy(x => x, StringComparer.Ordinal))}.");
        }

        if (RuleExpression.MaxNestingOf(expression) > RuleExpression.MaxNestingDepth)
        {
            Error(
                $"Expression in rule {rule.Id} nests parentheses more than {RuleExpression.MaxNestingDepth} deep. Evaluation recurses once per level, so this would exhaust the stack.");
        }

        // Expressions are folded left to right with no operator precedence, so mixing operators within one
        // group almost never means what the author intended.
        foreach (var group in operatorsByGroup.Where(x => x.Value.Count > 1))
        {
            Error(
                $"Expression '{expression}' in rule {rule.Id} mixes the operators {string.Join(", ", group.Value.OrderBy(x => x))} without parentheses. Expressions are evaluated left to right with no operator precedence, so add parentheses to make the grouping explicit.");
        }

        errors.AddRange(CheckExpressionCanReportAFinding(rule, expression, patternLabels, conditionLabels));

        // Clauses are evaluated in the order they appear and captures accumulate as they go, so a condition
        // reached before any pattern has nothing to test and is always false.
        var seenAPattern = false;
        string? conditionBeforeAnyPattern = null;

        foreach (var bare in tokens.Select(token => token.Trim('(', ')')))
        {
            if (patternLabels.Contains(bare))
            {
                seenAPattern = true;
            }
            else if (!seenAPattern && conditionLabels.Contains(bare))
            {
                conditionBeforeAnyPattern ??= bare;
            }
        }

        if (conditionBeforeAnyPattern is not null)
        {
            Error(
                $"Expression '{expression}' in rule {rule.Id} uses the condition '{conditionBeforeAnyPattern}' before any pattern. Conditions test findings that earlier clauses produced, so one evaluated first is always false and the rule can never report. Put a pattern label ahead of it.");
        }

        return errors;
    }

    /// <summary>
    ///     A finding originates from exactly one pattern, so a term naming a different pattern is false
    ///     while that finding is being judged. An expression that therefore has no satisfying assignment
    ///     will match at the rule level and then report nothing, which looks like the rule silently
    ///     failing. Conditions are treated as free, since their value depends on the file being scanned.
    /// </summary>
    private IEnumerable<string> CheckExpressionCanReportAFinding(Rule rule, string expression,
        IReadOnlyList<string> patternLabels, IReadOnlyList<string> conditionLabels)
    {
        // The search is exponential in the number of conditions, so it runs on a fixed budget of evaluations.
        // Exhausting it means no satisfying assignment was found within the budget, which is not evidence that
        // none exists, so the rule is left alone rather than reported.
        const int maxEvaluations = 4096;

        if (RuleExpression.TryParse(expression) is not { } parsed || patternLabels.Count == 0)
        {
            yield break;
        }

        if (conditionLabels.Count >= 31)
        {
            yield break;
        }

        var combinations = 1L << conditionLabels.Count;
        var evaluations = 0;

        foreach (var originatingPattern in patternLabels)
        for (var conditionValues = 0L; conditionValues < combinations; conditionValues++)
        {
            if (++evaluations > maxEvaluations)
            {
                yield break;
            }

            var assignment = conditionValues;
            if (parsed.Evaluate(label =>
                {
                    for (var conditionIndex = 0; conditionIndex < conditionLabels.Count; conditionIndex++)
                        if (conditionLabels[conditionIndex] == label)
                        {
                            return (assignment & (1L << conditionIndex)) != 0;
                        }

                    return label == originatingPattern;
                }))
            {
                yield break;
            }
        }

        var message =
            $"Expression '{expression}' in rule {rule.Id} can never report a finding, because it requires more than one pattern to be true at once and a finding comes from a single pattern. Split the patterns into separate rules, or express the extra requirement as a condition.";
        _logger?.LogError("{Message}", message);
        yield return message;
    }
}