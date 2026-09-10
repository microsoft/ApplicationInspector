using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     Regression tests for reporting the pattern that actually matched. String and substring patterns
///     each become their own single-element clause, so the reported index must come from the clause's
///     pattern index rather than the position within the clause's data.
/// </summary>
[ExcludeFromCodeCoverage]
public class PatternIndexReportingTests
{
    private const string twoSubstringPatternsDifferentConfidence = @"[
    {
        ""id"": ""SA100001"",
        ""name"": ""Testing.Rules.TwoSubstrings"",
        ""tags"": [ ""Testing.Rules.TwoSubstrings"" ],
        ""severity"": ""Critical"",
        ""description"": ""first pattern is high confidence, second is low"",
        ""patterns"": [
            { ""pattern"": ""alpha"", ""type"": ""substring"", ""confidence"": ""High"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""beta"",  ""type"": ""substring"", ""confidence"": ""Low"",  ""scopes"": [ ""code"" ] }
        ]
    }
]";

    private const string twoRegexPatternsDifferentConfidence = @"[
    {
        ""id"": ""SA100002"",
        ""name"": ""Testing.Rules.TwoRegexes"",
        ""tags"": [ ""Testing.Rules.TwoRegexes"" ],
        ""severity"": ""Critical"",
        ""description"": ""first pattern is high confidence, second is low"",
        ""patterns"": [
            { ""pattern"": ""alpha"", ""type"": ""regex"", ""confidence"": ""High"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""beta"",  ""type"": ""regex"", ""confidence"": ""Low"",  ""scopes"": [ ""code"" ] }
        ]
    }
]";

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    private Microsoft.ApplicationInspector.RulesEngine.RuleProcessor ProcessorFor(string json,
        RuleProcessorOptions? options = null)
    {
        RuleSet rules = new();
        rules.AddString(json, "TestRules");
        return new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules, options ?? new RuleProcessorOptions());
    }

    [Theory]
    [InlineData(twoSubstringPatternsDifferentConfidence)]
    [InlineData(twoRegexPatternsDifferentConfidence)]
    public void SecondPatternMatch_ReportsSecondPattern(string ruleJson)
    {
        var processor = ProcessorFor(ruleJson);
        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        var matches = processor.AnalyzeFile("beta only", new FileEntry("test.c", new MemoryStream()), info);

        var match = Assert.Single(matches);
        Assert.Equal("beta", match.MatchingPattern?.Pattern);
        Assert.Equal(Confidence.Low, match.MatchingPattern?.Confidence);
    }

    /// <summary>
    ///     Confidence filtering reads the confidence off the reported pattern, so misreporting the index
    ///     would let a low confidence match through a high confidence only filter.
    /// </summary>
    [Theory]
    [InlineData(twoSubstringPatternsDifferentConfidence)]
    [InlineData(twoRegexPatternsDifferentConfidence)]
    public void ConfidenceFilter_AppliesToTheMatchedPattern(string ruleJson)
    {
        var processor = ProcessorFor(ruleJson, new RuleProcessorOptions { ConfidenceFilter = Confidence.High });
        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        Assert.Empty(processor.AnalyzeFile("beta only", new FileEntry("test.c", new MemoryStream()), info));
        Assert.Single(processor.AnalyzeFile("alpha only", new FileEntry("test.c", new MemoryStream()), info));
    }
}
