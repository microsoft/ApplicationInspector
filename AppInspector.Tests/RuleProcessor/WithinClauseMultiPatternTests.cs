using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     Each matching pattern clause contributes its own capture, so a condition must consider all of them
///     rather than only the first.
/// </summary>
[ExcludeFromCodeCoverage]
public class WithinClauseMultiPatternTests
{
    private const string twoPatternsOneCondition = @"[
    {
        ""id"": ""SA200001"",
        ""name"": ""Testing.Rules.TwoPatternsOneCondition"",
        ""tags"": [ ""Testing.Rules.TwoPatternsOneCondition"" ],
        ""severity"": ""Critical"",
        ""description"": ""two patterns guarded by a single same-line condition"",
        ""patterns"": [
            { ""pattern"": ""alpha"", ""type"": ""regex"", ""confidence"": ""High"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""beta"",  ""type"": ""regex"", ""confidence"": ""High"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            {
                ""pattern"": { ""pattern"": ""guard"", ""type"": ""regex"", ""scopes"": [ ""code"" ] },
                ""search_in"": ""same-line""
            }
        ]
    }
]";

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    private Microsoft.ApplicationInspector.RulesEngine.RuleProcessor ProcessorFor(string json)
    {
        RuleSet rules = new();
        rules.AddString(json, "TestRules");
        return new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, new RuleProcessorOptions());
    }

    /// <summary>
    ///     The first pattern's capture fails the condition and the second pattern's capture passes it.
    ///     The passing match must still be reported.
    /// </summary>
    [Fact]
    public void ConditionSatisfiedOnlyBySecondPattern_StillReports()
    {
        var processor = ProcessorFor(twoPatternsOneCondition);
        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        var matches = processor.AnalyzeFile("alpha\nbeta guard\n",
            new FileEntry("test.c", new MemoryStream()), info);

        var match = Assert.Single(matches);
        Assert.Equal("beta", match.MatchingPattern?.Pattern);
        Assert.Equal(2, match.StartLocationLine);
    }

    [Fact]
    public void ConditionSatisfiedOnlyByFirstPattern_StillReports()
    {
        var processor = ProcessorFor(twoPatternsOneCondition);
        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        var matches = processor.AnalyzeFile("alpha guard\nbeta\n",
            new FileEntry("test.c", new MemoryStream()), info);

        var match = Assert.Single(matches);
        Assert.Equal("alpha", match.MatchingPattern?.Pattern);
        Assert.Equal(1, match.StartLocationLine);
    }

    [Fact]
    public void ConditionSatisfiedByBothPatterns_ReportsBoth()
    {
        var processor = ProcessorFor(twoPatternsOneCondition);
        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        var matches = processor.AnalyzeFile("alpha guard\nbeta guard\n",
            new FileEntry("test.c", new MemoryStream()), info);

        Assert.Equal(2, matches.Count);
        Assert.Contains(matches, m => m.MatchingPattern?.Pattern == "alpha");
        Assert.Contains(matches, m => m.MatchingPattern?.Pattern == "beta");
    }

    [Fact]
    public void ConditionSatisfiedByNeitherPattern_ReportsNothing()
    {
        var processor = ProcessorFor(twoPatternsOneCondition);
        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        Assert.Empty(processor.AnalyzeFile("alpha\nbeta\n",
            new FileEntry("test.c", new MemoryStream()), info));
    }
}
