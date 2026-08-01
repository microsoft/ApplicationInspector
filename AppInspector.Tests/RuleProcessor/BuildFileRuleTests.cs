using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

public class BuildFileRuleTests
{
    private const string BuildFileContents = "{\"value\":\"build-marker\"}";
    private const string BuildFileName = "test.json";
    private const string FeatureTag = "Testing.Build.Feature";
    private const string Marker = "build-marker";
    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitBuildLanguageRuleEmitsFeatureTagByDefault(bool analyzeAsync)
    {
        var languageInfo = GetBuildLanguage();
        var rule = CreateRule("BUILD000001", new[] { languageInfo.Name });

        var matches = await AnalyzeAsync(rule, languageInfo, false, analyzeAsync);

        var match = Assert.Single(matches);
        Assert.Equal(FeatureTag, Assert.Single(match.Rule!.Tags!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UniversalBuildRuleEmitsFeatureTagOnlyWhenAllowed(bool analyzeAsync)
    {
        var languageInfo = GetBuildLanguage();
        var rule = CreateRule("BUILD000002");

        var defaultMatches = await AnalyzeAsync(rule, languageInfo, false, analyzeAsync);
        var allowedMatches = await AnalyzeAsync(rule, languageInfo, true, analyzeAsync);

        Assert.Empty(defaultMatches);
        var match = Assert.Single(allowedMatches);
        Assert.Equal(FeatureTag, Assert.Single(match.Rule!.Tags!));
    }

    private static async Task<List<MatchRecord>> AnalyzeAsync(Rule rule, LanguageInfo languageInfo,
        bool allowAllTagsInBuildFiles, bool analyzeAsync)
    {
        RuleSet rules = new();
        rules.AddRule(rule);
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor = new(rules,
            new RuleProcessorOptions { AllowAllTagsInBuildFiles = allowAllTagsInBuildFiles });
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(BuildFileContents));
        FileEntry fileEntry = new(BuildFileName, stream);

        return analyzeAsync
            ? await processor.AnalyzeFileAsync(fileEntry, languageInfo)
            : processor.AnalyzeFile(BuildFileContents, fileEntry, languageInfo);
    }

    private static Rule CreateRule(string id, string[]? appliesTo = null)
    {
        return new Rule
        {
            Id = id,
            Name = "Build file filtering test",
            AppliesTo = appliesTo,
            Tags = new[] { FeatureTag },
            Patterns = new[]
            {
                new SearchPattern
                {
                    Pattern = Marker,
                    PatternType = PatternType.Substring,
                    Confidence = Confidence.High
                }
            }
        };
    }

    private LanguageInfo GetBuildLanguage()
    {
        Assert.True(_languages.FromFileNameOut(BuildFileName, out var languageInfo));
        Assert.Equal(LanguageInfo.LangFileType.Build, languageInfo.Type);
        return languageInfo;
    }
}
