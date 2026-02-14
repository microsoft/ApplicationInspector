using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
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
        Assert.NotNull(rule.Conditions[0].AppliesTo);
        Assert.Contains("javascript", rule.Conditions[0].AppliesTo);
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
        Assert.NotNull(rule.Conditions[0].DoesNotApplyTo);
        Assert.Contains("python", rule.Conditions[0].DoesNotApplyTo);
        Assert.Contains("ruby", rule.Conditions[0].DoesNotApplyTo);
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

        // Use RuleProcessor to analyze content that only contains "baz".
        // The first pattern ("foo") has a condition requiring "bar" in the same file,
        // so it should not match, while the second pattern ("baz") should match.
        var processor = new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(ruleSet, new RuleProcessorOptions());

        const string fileName = "test.js";
        const string fileContent = "this line contains baz but not the other tokens";

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
}
