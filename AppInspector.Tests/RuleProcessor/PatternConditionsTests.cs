using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

[ExcludeFromCodeCoverage]
public class PatternConditionsTests
{
    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();
    
    /// <summary>
    /// Test that pattern-level conditions are parsed correctly
    /// </summary>
    [Fact]
    public void PatternConditions_ParsingTest()
    {
        // Rule with two patterns, each with their own condition
        const string ruleJson = @"[
    {
        ""id"": ""TEST001"",
        ""name"": ""Pattern Conditions Test"",
        ""tags"": [""Test.PatternConditions""],
        ""severity"": ""Important"",
        ""patterns"": [
            {
                ""pattern"": ""foo"",
                ""type"": ""regex"",
                ""conditions"": [
                    {
                        ""pattern"": {""pattern"": ""bar"", ""type"": ""regex""},
                        ""search_in"": ""same-line""
                    }
                ]
            },
            {
                ""pattern"": ""baz"",
                ""type"": ""regex"",
                ""conditions"": [
                    {
                        ""pattern"": {""pattern"": ""qux"", ""type"": ""regex""},
                        ""search_in"": ""same-line""
                    }
                ]
            }
        ]
    }
]";

        var ruleSet = new RuleSet(NullLoggerFactory.Instance);
        ruleSet.AddString(ruleJson, "test");
        
        var rules = ruleSet.GetAppInspectorRules().ToList();
        Assert.Single(rules);
        
        var rule = rules.First();
        Assert.Equal(2, rule.Patterns.Length);
        
        // Check that first pattern has a condition
        Assert.NotNull(rule.Patterns[0].Conditions);
        Assert.Single(rule.Patterns[0].Conditions);
        
        // Check that second pattern has a condition
        Assert.NotNull(rule.Patterns[1].Conditions);
        Assert.Single(rule.Patterns[1].Conditions);
    }

    /// <summary>
    /// Test that pattern-level conditions match correctly - basic JSON parsing test
    /// </summary>
    [Fact]
    public void PatternConditions_SimpleParsing()
    {
        const string ruleJson = @"[
    {
        ""id"": ""TEST002"",
        ""name"": ""Pattern Match Test"",
        ""tags"": [""Test.PatternMatch""],
        ""severity"": ""Important"",
        ""patterns"": [
            {
                ""pattern"": ""foo"",
                ""type"": ""regex"",
                ""conditions"": [
                    {
                        ""pattern"": {""pattern"": ""bar"", ""type"": ""regex""},
                        ""search_in"": ""same-line""
                    }
                ]
            }
        ]
    }
]";

        var ruleSet = new RuleSet(NullLoggerFactory.Instance);
        ruleSet.AddString(ruleJson, "test");

        var rules = ruleSet.GetAppInspectorRules().ToList();
        Assert.Single(rules);
        
        // Verify the pattern has conditions
        var rule = rules.First();
        Assert.NotNull(rule.Patterns[0].Conditions);
        Assert.Single(rule.Patterns[0].Conditions);
        Assert.Equal("bar", rule.Patterns[0].Conditions[0].Pattern?.Pattern);
    }

    /// <summary>
    /// Test that language filters work on conditions
    /// </summary>
    [Fact]
    public void LanguageFilters_AppliesToTest()
    {
        const string ruleJson = @"[
    {
        ""id"": ""TEST003"",
        ""name"": ""Language Filter Test"",
        ""tags"": [""Test.LanguageFilter""],
        ""severity"": ""Important"",
        ""patterns"": [
            {
                ""pattern"": ""test"",
                ""type"": ""regex""
            }
        ],
        ""conditions"": [
            {
                ""pattern"": {""pattern"": ""javascript"", ""type"": ""regex""},
                ""search_in"": ""same-file"",
                ""applies_to"": [""javascript""]
            }
        ]
    }
]";

        var ruleSet = new RuleSet(NullLoggerFactory.Instance);
        ruleSet.AddString(ruleJson, "test");
        
        var rules = ruleSet.GetAppInspectorRules().ToList();
        Assert.Single(rules);
        
        var rule = rules.First();
        Assert.NotNull(rule.Conditions);
        Assert.Single(rule.Conditions);
        var appliesTo = rule.Conditions[0].AppliesTo;
        Assert.NotNull(appliesTo);
        Assert.Contains("javascript", appliesTo);
    }

    /// <summary>
    /// Test that language filters work with does_not_apply_to
    /// </summary>
    [Fact]
    public void LanguageFilters_DoesNotApplyToTest()
    {
        const string ruleJson = @"[
    {
        ""id"": ""TEST004"",
        ""name"": ""Language Exclusion Test"",
        ""tags"": [""Test.LanguageExclusion""],
        ""severity"": ""Important"",
        ""patterns"": [
            {
                ""pattern"": ""test"",
                ""type"": ""regex""
            }
        ],
        ""conditions"": [
            {
                ""pattern"": {""pattern"": ""specific"", ""type"": ""regex""},
                ""search_in"": ""same-file"",
                ""does_not_apply_to"": [""python"", ""ruby""]
            }
        ]
    }
]";

        var ruleSet = new RuleSet(NullLoggerFactory.Instance);
        ruleSet.AddString(ruleJson, "test");
        
        var rules = ruleSet.GetAppInspectorRules().ToList();
        Assert.Single(rules);
        
        var rule = rules.First();
        Assert.NotNull(rule.Conditions);
        Assert.Single(rule.Conditions);
        var doesNotApplyTo = rule.Conditions[0].DoesNotApplyTo;
        Assert.NotNull(doesNotApplyTo);
        Assert.Contains("python", doesNotApplyTo);
        Assert.Contains("ruby", doesNotApplyTo);
    }

    /// <summary>
    /// Pattern-level conditions should only restrict the pattern they are attached to.
    /// This test uses RuleProcessor to ensure that a pattern without conditions still matches.
    /// </summary>
    [Fact]
    public void PatternLevelConditions_OnlyAffectAttachedPattern()
    {
        const string ruleJson = @"[
    {
        ""id"": ""TEST005"",
        ""name"": ""Pattern-level Condition Runtime Test"",
        ""tags"": [""Test.PatternLevelCondition.Runtime""],
        ""severity"": ""Important"",
        ""patterns"": [
            {
                ""pattern"": ""foo"",
                ""type"": ""regex"",
                ""conditions"": [
                    {
                        ""pattern"": {""pattern"": ""bar"", ""type"": ""regex""},
                        ""search_in"": ""same-file""
                    }
                ]
            },
            {
                ""pattern"": ""baz"",
                ""type"": ""regex""
            }
        ]
    }
]";

        var ruleSet = new RuleSet(NullLoggerFactory.Instance);
        ruleSet.AddString(ruleJson, "test");

        // "foo" is present but its condition requires "bar" in the same file, so only "baz" is reported.
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(ruleSet, new RuleProcessorOptions());

        const string fileName = "test.js";
        const string fileContent = "this line contains foo and baz but not the other token";

        // Derive a language from the file name; JavaScript is a reasonable choice here.
        if (_languages.FromFileNameOut(fileName, out var languageInfo))
        {
            var matches = processor.AnalyzeFile(fileContent, new FileEntry(fileName, new System.IO.MemoryStream()), languageInfo);

            // We expect exactly one match, corresponding to the second pattern ("baz").
            Assert.Single(matches);
            Assert.Contains(matches, m => m.MatchingPattern.Pattern == "baz");
            Assert.DoesNotContain(matches, m => m.MatchingPattern.Pattern == "foo");
        }
        else
        {
            Assert.Fail("Failed to get language info");
        }
    }

    /// <summary>
    /// Language filters on conditions should control whether the condition is evaluated
    /// based on the file language. When the file language is not listed in applies_to,
    /// the condition should be skipped and not block pattern matches.
    /// </summary>
    [Fact]
    public void LanguageFilters_RuntimeBehavior_AppliesToAndDoesNotApplyTo()
    {
        const string ruleJson = @"[
    {
        ""id"": ""TEST006"",
        ""name"": ""Language Filter Runtime Test"",
        ""tags"": [""Test.LanguageFilter.Runtime""],
        ""severity"": ""Important"",
        ""patterns"": [
            {
                ""pattern"": ""test_token"",
                ""type"": ""regex""
            }
        ],
        ""conditions"": [
            {
                ""pattern"": {""pattern"": ""javascript_only"", ""type"": ""regex""},
                ""search_in"": ""same-file"",
                ""applies_to"": [""javascript""],
                ""does_not_apply_to"": [""python""]
            }
        ]
    }
]";

        var ruleSet = new RuleSet(NullLoggerFactory.Instance);
        ruleSet.AddString(ruleJson, "test");

        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(ruleSet, new RuleProcessorOptions());

        const string contentWithBothTokens = "test_token javascript_only";
        const string contentWithPatternOnly = "test_token no_language_marker";

        // Analyze as JavaScript: condition applies (applies_to includes "javascript"),
        // so the presence of "javascript_only" should be required for a match.
        const string jsFileName = "file.js";
        if (_languages.FromFileNameOut(jsFileName, out var jsLanguage))
        {
            var jsResultWithBoth = processor.AnalyzeFile(contentWithBothTokens, new FileEntry(jsFileName, new System.IO.MemoryStream()), jsLanguage);
            var jsResultWithPatternOnly = processor.AnalyzeFile(contentWithPatternOnly, new FileEntry(jsFileName, new System.IO.MemoryStream()), jsLanguage);

            Assert.Single(jsResultWithBoth);
            Assert.Empty(jsResultWithPatternOnly);
        }
        else
        {
            Assert.Fail("Failed to get JavaScript language info");
        }

        // Analyze as Python: condition should be skipped (does_not_apply_to includes "python"),
        // so the pattern should match even without the "javascript_only" token.
        const string pyFileName = "file.py";
        if (_languages.FromFileNameOut(pyFileName, out var pyLanguage))
        {
            var pyResultWithPatternOnly = processor.AnalyzeFile(contentWithPatternOnly, new FileEntry(pyFileName, new System.IO.MemoryStream()), pyLanguage);

            Assert.Single(pyResultWithPatternOnly);
            Assert.Contains(pyResultWithPatternOnly, m => m.MatchingPattern.Pattern == "test_token");
        }
        else
        {
            Assert.Fail("Failed to get Python language info");
        }
    }

    private string[] Analyze(string ruleJson, string fileName, string content)
    {
        var ruleSet = new RuleSet(NullLoggerFactory.Instance);
        ruleSet.AddString(ruleJson, "test");
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(ruleSet,
            new RuleProcessorOptions { Parallel = false });

        Assert.True(_languages.FromFileNameOut(fileName, out var languageInfo), $"No language for {fileName}");

        return processor
            .AnalyzeFile(content, new FileEntry(fileName, new System.IO.MemoryStream()), languageInfo)
            .Select(x => x.MatchingPattern?.Pattern ?? string.Empty)
            .OrderBy(x => x)
            .ToArray();
    }

    /// <summary>
    ///     A pattern carrying a condition must still match when that condition is satisfied. Without this the
    ///     negative-direction tests above would pass for the wrong reason, because a conditioned pattern that can
    ///     never match also never produces a false positive.
    /// </summary>
    [Fact]
    public void PatternLevelCondition_MatchesWhenConditionIsSatisfied()
    {
        const string ruleJson = @"[
    {
        ""id"": ""TEST100"",
        ""name"": ""Conditioned pattern positive case"",
        ""tags"": [""Test.PatternLevelCondition.Positive""],
        ""severity"": ""Important"",
        ""patterns"": [
            {
                ""pattern"": ""foo"",
                ""type"": ""regex"",
                ""conditions"": [
                    {
                        ""pattern"": { ""pattern"": ""bar"", ""type"": ""regex"" },
                        ""search_in"": ""same-file""
                    }
                ]
            },
            { ""pattern"": ""baz"", ""type"": ""regex"" }
        ]
    }
]";

        Assert.Equal(new[] { "baz", "foo" }, Analyze(ruleJson, "test.js", "foo bar baz"));
        Assert.Equal(new[] { "baz" }, Analyze(ruleJson, "test.js", "foo baz"));
        Assert.Equal(new[] { "foo" }, Analyze(ruleJson, "test.js", "foo bar"));
    }

    /// <summary>
    ///     A condition attached to one pattern must not gate the rule's other patterns.
    /// </summary>
    [Fact]
    public void PatternLevelCondition_DoesNotGateSiblingPatterns()
    {
        const string ruleJson = @"[
    {
        ""id"": ""TEST101"",
        ""name"": ""Conditioned middle pattern"",
        ""tags"": [""Test.PatternLevelCondition.Siblings""],
        ""severity"": ""Important"",
        ""patterns"": [
            { ""pattern"": ""alpha"", ""type"": ""regex"" },
            {
                ""pattern"": ""beta"",
                ""type"": ""regex"",
                ""conditions"": [
                    {
                        ""pattern"": { ""pattern"": ""gate"", ""type"": ""regex"" },
                        ""search_in"": ""same-line""
                    }
                ]
            },
            { ""pattern"": ""gamma"", ""type"": ""regex"" }
        ]
    }
]";

        // The condition is satisfied, so all three patterns report.
        Assert.Equal(new[] { "alpha", "beta", "gamma" },
            Analyze(ruleJson, "test.js", "alpha\nbeta gate\ngamma"));

        // The condition is not satisfied, so only the unconditioned siblings report.
        Assert.Equal(new[] { "alpha", "gamma" },
            Analyze(ruleJson, "test.js", "alpha\nbeta\ngamma"));
    }

    /// <summary>
    ///     A rule level condition gates every pattern, including patterns that carry their own conditions.
    /// </summary>
    [Fact]
    public void RuleLevelAndPatternLevelConditionsCombine()
    {
        const string ruleJson = @"[
    {
        ""id"": ""TEST102"",
        ""name"": ""Rule and pattern level conditions"",
        ""tags"": [""Test.PatternLevelCondition.Combined""],
        ""severity"": ""Important"",
        ""patterns"": [
            {
                ""pattern"": ""alpha"",
                ""type"": ""regex"",
                ""conditions"": [
                    {
                        ""pattern"": { ""pattern"": ""gate"", ""type"": ""regex"" },
                        ""search_in"": ""same-line""
                    }
                ]
            },
            { ""pattern"": ""beta"", ""type"": ""regex"" }
        ],
        ""conditions"": [
            {
                ""pattern"": { ""pattern"": ""enabled"", ""type"": ""regex"" },
                ""search_in"": ""same-file""
            }
        ]
    }
]";

        // Rule level condition satisfied, pattern level condition satisfied.
        Assert.Equal(new[] { "alpha", "beta" }, Analyze(ruleJson, "test.js", "enabled\nalpha gate\nbeta"));

        // Rule level condition satisfied, pattern level condition not; beta is unaffected.
        Assert.Equal(new[] { "beta" }, Analyze(ruleJson, "test.js", "enabled\nalpha\nbeta"));

        // Rule level condition not satisfied, so nothing reports regardless of the pattern level condition.
        Assert.Empty(Analyze(ruleJson, "test.js", "alpha gate\nbeta"));
    }

    /// <summary>
    ///     When every pattern carries its own condition, each pattern is gated independently: one pattern failing its
    ///     condition must not suppress another whose condition was satisfied.
    /// </summary>
    [Fact]
    public void PatternLevelConditions_GateEachPatternIndependently()
    {
        const string ruleJson = @"[
    {
        ""id"": ""TEST105"",
        ""name"": ""Both patterns conditioned"",
        ""tags"": [""Test.PatternLevelCondition.Independent""],
        ""severity"": ""Important"",
        ""patterns"": [
            {
                ""pattern"": ""alpha"",
                ""type"": ""regex"",
                ""conditions"": [
                    {
                        ""pattern"": { ""pattern"": ""gateA"", ""type"": ""regex"" },
                        ""search_in"": ""same-line""
                    }
                ]
            },
            {
                ""pattern"": ""beta"",
                ""type"": ""regex"",
                ""conditions"": [
                    {
                        ""pattern"": { ""pattern"": ""gateB"", ""type"": ""regex"" },
                        ""search_in"": ""same-line""
                    }
                ]
            }
        ]
    }
]";

        Assert.Equal(new[] { "alpha", "beta" }, Analyze(ruleJson, "test.js", "alpha gateA\nbeta gateB"));
        Assert.Equal(new[] { "alpha" }, Analyze(ruleJson, "test.js", "alpha gateA\nbeta"));
        Assert.Equal(new[] { "beta" }, Analyze(ruleJson, "test.js", "alpha\nbeta gateB"));
        Assert.Empty(Analyze(ruleJson, "test.js", "alpha\nbeta"));

        // Each pattern is gated by its own condition, not by the other pattern's.
        Assert.Empty(Analyze(ruleJson, "test.js", "alpha gateB\nbeta gateA"));
    }

    /// <summary>
    ///     A condition that is skipped for the file's language must not change the outcome of the conditions
    ///     declared alongside it, whichever order they appear in.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LanguageSkippedCondition_IsOrderIndependent(bool skippedConditionFirst)
    {
        const string realCondition = @"{
                        ""pattern"": { ""pattern"": ""gate"", ""type"": ""regex"" },
                        ""search_in"": ""same-line""
                    }";
        const string skippedCondition = @"{
                        ""pattern"": { ""pattern"": ""never"", ""type"": ""regex"" },
                        ""search_in"": ""same-line"",
                        ""applies_to"": [ ""python"" ]
                    }";

        var conditions = skippedConditionFirst
            ? $"{skippedCondition}, {realCondition}"
            : $"{realCondition}, {skippedCondition}";

        var ruleJson = $@"[
    {{
        ""id"": ""TEST103"",
        ""name"": ""Skipped condition ordering"",
        ""tags"": [""Test.PatternLevelCondition.SkipOrder""],
        ""severity"": ""Important"",
        ""patterns"": [
            {{
                ""pattern"": ""alpha"",
                ""type"": ""regex"",
                ""conditions"": [ {conditions} ]
            }}
        ]
    }}
]";

        // The skipped condition filters nothing, so the real condition alone decides the outcome.
        Assert.Equal(new[] { "alpha" }, Analyze(ruleJson, "test.js", "alpha gate"));
        Assert.Empty(Analyze(ruleJson, "test.js", "alpha"));
    }

    /// <summary>
    ///     The OAT rules generated for conditions must be well formed. OAT silently refuses to evaluate an
    ///     expression whose label matches more than one clause, so a malformed rule fails by never matching rather
    ///     than by raising an error.
    /// </summary>
    [Theory]
    [InlineData("first pattern conditioned", true, false, false, "(0 AND c0) OR 1")]
    [InlineData("second pattern conditioned", false, true, false, "(0 OR (1 AND c0))")]
    [InlineData("both patterns conditioned", true, true, false, "(0 AND c0) OR (1 AND c1)")]
    [InlineData("no conditions", false, false, false, "(0 OR 1)")]
    [InlineData("rule level only", false, false, true, "(0 OR 1) AND c0")]
    [InlineData("pattern and rule level", true, false, true, "(0 AND c0) OR 1 AND c1")]
    public void GeneratedOatRulesAreWellFormed(string because, bool conditionOnFirst, bool conditionOnSecond,
        bool ruleLevelCondition, string expectedExpression)
    {
        const string condition = @",
                ""conditions"": [
                    {
                        ""pattern"": { ""pattern"": ""gate"", ""type"": ""regex"" },
                        ""search_in"": ""same-line""
                    }
                ]";

        var ruleLevel = ruleLevelCondition
            ? @",
        ""conditions"": [
            {
                ""pattern"": { ""pattern"": ""enabled"", ""type"": ""regex"" },
                ""search_in"": ""same-file""
            }
        ]"
            : string.Empty;

        var ruleJson = $@"[
    {{
        ""id"": ""TEST104"",
        ""name"": ""Well formed generated rule"",
        ""tags"": [""Test.PatternLevelCondition.WellFormed""],
        ""severity"": ""Important"",
        ""patterns"": [
            {{ ""pattern"": ""alpha"", ""type"": ""regex""{(conditionOnFirst ? condition : string.Empty)} }},
            {{ ""pattern"": ""beta"", ""type"": ""regex""{(conditionOnSecond ? condition : string.Empty)} }}
        ]{ruleLevel}
    }}
]";

        var ruleSet = new RuleSet(NullLoggerFactory.Instance);
        ruleSet.AddString(ruleJson, "test");
        var oatRule = Assert.Single(ruleSet.GetOatRules());

        Assert.Equal(expectedExpression, oatRule.Expression);

        // Clause labels must be unique, and every label in the expression must resolve to a clause.
        var analyzer = new ApplicationInspectorAnalyzer();
        var violations = analyzer.EnumerateRuleIssues(oatRule).Select(x => x.Description).ToList();
        Assert.True(violations.Count == 0, $"{because}: {string.Join("; ", violations)}");

        // Pattern clauses keep the bare numeric labels the pattern index is recovered from.
        var patternLabels = oatRule.Clauses.Where(x => x is not WithinClause).Select(x => x.Label).ToArray();
        Assert.Equal(new[] { "0", "1" }, patternLabels);
    }
}
