using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     The synchronous and asynchronous analysis entry points are conveniences over the same engine and
///     must agree, including for rules whose conditions filter findings.
/// </summary>
[ExcludeFromCodeCoverage]
public class SyncAsyncParityTests
{
    private const string multipleNegatedConditions = @"[
    {
        ""id"": ""SA300001"",
        ""name"": ""Testing.Rules.MultipleNegatedConditions"",
        ""tags"": [ ""Testing.Rules.MultipleNegatedConditions"" ],
        ""severity"": ""moderate"",
        ""description"": ""insecure url with several exclusions"",
        ""patterns"": [
            { ""pattern"": ""http://"", ""type"": ""substring"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            {
                ""pattern"": { ""pattern"": ""xmlns="", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                ""negate_finding"": true,
                ""search_in"": ""finding-region(-1, 0)""
            },
            {
                ""pattern"": { ""pattern"": ""http://localhost"", ""type"": ""regex"", ""scopes"": [ ""code"" ] },
                ""negate_finding"": true,
                ""search_in"": ""same-line""
            }
        ]
    }
]";

    private const string testData = @"
http://
http://localhost
xmlns=
http://
http://
";

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    private Microsoft.ApplicationInspector.RulesEngine.RuleProcessor ProcessorFor(string json)
    {
        RuleSet rules = new();
        rules.AddString(json, "TestRules");
        return new Microsoft.ApplicationInspector.RulesEngine.RuleProcessor(rules,
            new RuleProcessorOptions { Parallel = false });
    }

    private static FileEntry EntryWith(string contents)
    {
        return new FileEntry("test.c", new MemoryStream(Encoding.UTF8.GetBytes(contents)));
    }

    [Fact]
    public async Task AnalyzeFileAsync_AgreesWithAnalyzeFile()
    {
        var processor = ProcessorFor(multipleNegatedConditions);
        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        var syncMatches = processor.AnalyzeFile(testData, EntryWith(testData), info);
        var asyncMatches = await processor.AnalyzeFileAsync(EntryWith(testData), info);

        Assert.Equal(
            syncMatches.Select(x => x.Boundary.Index).OrderBy(x => x),
            asyncMatches.Select(x => x.Boundary.Index).OrderBy(x => x));
    }

    /// <summary>
    ///     Only the findings that satisfy every condition are reported. Lines 3 and 5 are each excluded by
    ///     one of the two conditions.
    /// </summary>
    [Fact]
    public async Task BothPaths_ReportOnlyFindingsPassingEveryCondition()
    {
        var processor = ProcessorFor(multipleNegatedConditions);
        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        var syncMatches = processor.AnalyzeFile(testData, EntryWith(testData), info);
        var asyncMatches = await processor.AnalyzeFileAsync(EntryWith(testData), info);

        Assert.Equal(new[] { 2, 6 }, syncMatches.Select(x => x.StartLocationLine).OrderBy(x => x));
        Assert.Equal(new[] { 2, 6 }, asyncMatches.Select(x => x.StartLocationLine).OrderBy(x => x));
    }

    [Fact]
    public async Task CancelledAnalysis_ReturnsWithoutThrowing()
    {
        var processor = ProcessorFor(multipleNegatedConditions);
        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        using CancellationTokenSource cts = new();
        cts.Cancel();

        Assert.Empty(await processor.AnalyzeFileAsync(EntryWith(testData), info, cts.Token));
    }
}
