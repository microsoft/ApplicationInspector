using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     A rule listed in another rule's overrides is only suppressed where the overriding finding fully
///     contains it, and both analysis entry points must agree.
/// </summary>
[ExcludeFromCodeCoverage]
public class OverrideTests
{
    private const string overridingRules = @"[
    {
        ""id"": ""SA700001"",
        ""name"": ""Testing.Rules.Overridden"",
        ""tags"": [ ""Testing.Rules.Overridden"" ],
        ""severity"": ""Critical"",
        ""description"": ""the general rule"",
        ""patterns"": [
            { ""pattern"": ""alphabet"", ""type"": ""substring"", ""scopes"": [ ""code"" ] }
        ]
    },
    {
        ""id"": ""SA700002"",
        ""name"": ""Testing.Rules.Overriding"",
        ""tags"": [ ""Testing.Rules.Overriding"" ],
        ""severity"": ""Critical"",
        ""description"": ""the specific rule"",
        ""overrides"": [ ""SA700001"" ],
        ""patterns"": [
            { ""pattern"": ""alphabet soup"", ""type"": ""substring"", ""scopes"": [ ""code"" ] }
        ]
    }
]";

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    private Microsoft.ApplicationInspector.RulesEngine.RuleProcessor Processor()
    {
        RuleSet rules = new();
        rules.AddString(overridingRules, "TestRules");
        return new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules,
            new RuleProcessorOptions { Parallel = false });
    }

    private static FileEntry EntryWith(string contents)
    {
        return new FileEntry("test.c", new MemoryStream(Encoding.UTF8.GetBytes(contents)));
    }

    [Fact]
    public void ContainedMatchIsOverridden()
    {
        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        var matches = Processor().AnalyzeFile("var x = alphabet soup;\n", EntryWith(string.Empty), info);

        var match = Assert.Single(matches);
        Assert.Equal("SA700002", match.Rule?.Id);
    }

    /// <summary>
    ///     The overridden finding starts inside the overriding finding but extends past its end, so it is
    ///     not contained and must survive.
    /// </summary>
    [Fact]
    public void OverlappingButWiderMatchSurvives()
    {
        const string ruleJson = @"[
    {
        ""id"": ""SA700003"",
        ""name"": ""Testing.Rules.Wide"",
        ""tags"": [ ""Testing.Rules.Wide"" ],
        ""severity"": ""Critical"",
        ""description"": ""matches a longer span"",
        ""patterns"": [
            { ""pattern"": ""bravo charlie"", ""type"": ""substring"", ""scopes"": [ ""code"" ] }
        ]
    },
    {
        ""id"": ""SA700004"",
        ""name"": ""Testing.Rules.Narrow"",
        ""tags"": [ ""Testing.Rules.Narrow"" ],
        ""severity"": ""Critical"",
        ""description"": ""matches a shorter span starting at the same place"",
        ""overrides"": [ ""SA700003"" ],
        ""patterns"": [
            { ""pattern"": ""bravo"", ""type"": ""substring"", ""scopes"": [ ""code"" ] }
        ]
    }
]";
        RuleSet rules = new();
        rules.AddString(ruleJson, "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor =
            new(rules, new RuleProcessorOptions { Parallel = false });

        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        var matches = processor.AnalyzeFile("bravo charlie\n", EntryWith(string.Empty), info);

        Assert.Equal(new[] { "SA700003", "SA700004" },
            matches.Select(x => x.Rule?.Id).OrderBy(x => x));
    }

    [Fact]
    public async Task BothPathsApplyOverridesIdentically()
    {
        Assert.True(_languages.FromFileNameOut("test.c", out var info));
        const string content = "var x = alphabet soup;\nvar y = alphabet;\n";

        var processor = Processor();
        var syncMatches = processor.AnalyzeFile(content, EntryWith(content), info);
        var asyncMatches = await processor.AnalyzeFileAsync(EntryWith(content), info);

        Assert.Equal(
            syncMatches.Select(x => $"{x.Rule?.Id}@{x.Boundary.Index}").OrderBy(x => x),
            asyncMatches.Select(x => $"{x.Rule?.Id}@{x.Boundary.Index}").OrderBy(x => x));

        // The standalone occurrence is untouched by the override.
        Assert.Contains(syncMatches, x => x.Rule?.Id == "SA700001");
    }
}
