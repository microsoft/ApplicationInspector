using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.CST.RecursiveExtractor;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     Expression evaluation recurses once per level of parenthesis nesting, both here and in the
///     underlying engine, so deeply nested input must be refused before anything tries to evaluate it.
///     Stack exhaustion cannot be caught and would take the whole process down.
///     These tests deliberately assert the guards, and never evaluate a deeply nested expression.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExpressionNestingLimitTests
{
    private const int WellPastTheLimit = 5000;

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    private static string Nested(int depth, string inner = "a")
    {
        return new string('(', depth) + inner + new string(')', depth);
    }

    private static string RuleWithExpression(string expression)
    {
        return $@"[
    {{
        ""id"": ""SA800001"",
        ""name"": ""Testing.Rules.Nesting"",
        ""tags"": [ ""Testing.Rules.Nesting"" ],
        ""severity"": ""Critical"",
        ""description"": ""nesting fixture"",
        ""expression"": ""{expression}"",
        ""patterns"": [
            {{ ""pattern"": ""alpha"", ""type"": ""substring"", ""label"": ""a"", ""scopes"": [ ""code"" ] }}
        ]
    }}
]";
    }

    [Fact]
    public void ParserRefusesNestingPastTheLimit()
    {
        Assert.NotNull(RuleExpression.TryParse(Nested(RuleExpression.MaxNestingDepth)));
        Assert.Null(RuleExpression.TryParse(Nested(RuleExpression.MaxNestingDepth + 1)));
        Assert.Null(RuleExpression.TryParse(Nested(WellPastTheLimit)));
    }

    [Fact]
    public void MaxNestingOf_CountsTheDeepestGroup()
    {
        Assert.Equal(0, RuleExpression.MaxNestingOf("a OR b"));
        Assert.Equal(1, RuleExpression.MaxNestingOf("(a OR b) AND (c OR d)"));
        Assert.Equal(2, RuleExpression.MaxNestingOf("((a OR b) AND c)"));
        Assert.Equal(WellPastTheLimit, RuleExpression.MaxNestingOf(Nested(WellPastTheLimit)));
    }

    [Fact]
    public void VerificationRejectsNestingPastTheLimit()
    {
        RuleSet rules = new();
        rules.AddString(RuleWithExpression(Nested(WellPastTheLimit)), "TestRules");

        RulesVerifier verifier = new(new RulesVerifierOptions());
        var status = verifier.CheckIntegrity(rules).Single();

        Assert.Contains(status.Errors, x => x.Contains("nests parentheses more than"));
        Assert.False(status.Verified);
    }

    /// <summary>
    ///     Conversion is the gate that every load path passes through, verified or not, so a rule nested
    ///     past the limit must never reach the analyzer with its expression intact.
    /// </summary>
    [Fact]
    public void ConversionRefusesToBuildARuleNestedPastTheLimit()
    {
        RuleSet rules = new();
        rules.AddString(RuleWithExpression(Nested(WellPastTheLimit)), "TestRules");

        var oatRule = rules.GetOatRules().Single();

        Assert.Empty(oatRule.Clauses);
        Assert.Null(oatRule.Expression);
    }

    /// <summary>
    ///     Analyzing with such a rule present must be safe rather than fatal.
    /// </summary>
    [Fact]
    public void AnalyzingWithAnOverNestedRuleIsSafe()
    {
        RuleSet rules = new();
        rules.AddString(RuleWithExpression(Nested(WellPastTheLimit)), "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor =
            new(rules, new RuleProcessorOptions { Parallel = false });

        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        Assert.Empty(processor.AnalyzeFile("alpha", new FileEntry("test.c", new MemoryStream()), info));
    }

    [Fact]
    public void NestingWithinTheLimitStillWorks()
    {
        RuleSet rules = new();
        rules.AddString(RuleWithExpression("(((a)))"), "TestRules");
        Microsoft.ApplicationInspector.RulesEngine.RuleProcessor processor =
            new(rules, new RuleProcessorOptions { Parallel = false });

        Assert.True(_languages.FromFileNameOut("test.c", out var info));

        Assert.Single(processor.AnalyzeFile("alpha", new FileEntry("test.c", new MemoryStream()), info));
    }
}
