using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
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
}
