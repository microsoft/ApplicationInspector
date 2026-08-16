using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     Characterization tests pinning the <see cref="Analyzer" /> contract that AppInspector's expression
///     support depends on. These assert OAT behavior, not AppInspector behavior; if they fail after an OAT
///     upgrade, the expression translation in AbstractRuleSet needs review before the upgrade is taken.
/// </summary>
[ExcludeFromCodeCoverage]
public class OatExpressionSemanticsTests
{
    private const string threePatternRule = @"[
    {
        ""id"": ""SA000001"",
        ""name"": ""Testing.Rules.ThreePattern"",
        ""tags"": [ ""Testing.Rules.ThreePattern"" ],
        ""severity"": ""Critical"",
        ""description"": ""three independent substring patterns"",
        ""patterns"": [
            { ""pattern"": ""alpha"", ""type"": ""substring"", ""confidence"": ""High"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""beta"",  ""type"": ""substring"", ""confidence"": ""High"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""gamma"", ""type"": ""substring"", ""confidence"": ""High"", ""scopes"": [ ""code"" ] }
        ]
    }
]";

    private const string patternWithConditionRule = @"[
    {
        ""id"": ""SA000002"",
        ""name"": ""Testing.Rules.PatternWithCondition"",
        ""tags"": [ ""Testing.Rules.PatternWithCondition"" ],
        ""severity"": ""Critical"",
        ""description"": ""one pattern guarded by a same-line condition"",
        ""patterns"": [
            { ""pattern"": ""alpha"", ""type"": ""substring"", ""confidence"": ""High"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            {
                ""pattern"": { ""pattern"": ""guard"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                ""search_in"": ""same-line""
            }
        ]
    }
]";

    private const string singleRegexRule = @"[
    {
        ""id"": ""SA000003"",
        ""name"": ""Testing.Rules.SingleRegex"",
        ""tags"": [ ""Testing.Rules.SingleRegex"" ],
        ""severity"": ""Critical"",
        ""description"": ""one regex pattern"",
        ""patterns"": [
            { ""pattern"": ""alpha"", ""type"": ""regex"", ""confidence"": ""High"", ""scopes"": [ ""code"" ] }
        ]
    }
]";

    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    private ConvertedOatRule RuleFrom(string json)
    {
        RuleSet rules = new();
        rules.AddString(json, "TestRules");
        return rules.GetOatRules().First();
    }

    private TextContainer TextFrom(string content)
    {
        return new TextContainer(content, "csharp", _languages);
    }

    private bool Matches(ConvertedOatRule rule, string content)
    {
        Analyzer analyzer = new ApplicationInspectorAnalyzer();
        return analyzer.Analyze(new[] { rule }, TextFrom(content)).Any();
    }

    /// <summary>
    ///     OAT folds the expression strictly left to right with no operator precedence, so `0 OR 1 AND 2`
    ///     means `(0 OR 1) AND 2`. Content matching only pattern 0 discriminates the two readings.
    /// </summary>
    [Fact]
    public void Expression_HasNoOperatorPrecedence()
    {
        var leftToRight = RuleFrom(threePatternRule);
        leftToRight.Expression = "0 OR 1 AND 2";

        // (alpha OR beta) AND gamma => (true OR false) AND false => false
        Assert.False(Matches(leftToRight, "alpha only"));

        var parenthesised = RuleFrom(threePatternRule);
        parenthesised.Expression = "0 OR (1 AND 2)";

        // alpha OR (beta AND gamma) => true OR (false AND false) => true
        Assert.True(Matches(parenthesised, "alpha only"));
    }

    /// <summary>
    ///     Parentheses group booleans via recursion. This pins whether that recursion also inherits the
    ///     captures accumulated by clauses evaluated before it, which decides whether a condition's scope
    ///     could ever be derived from expression structure.
    /// </summary>
    [Fact]
    public void ParenthesisedSubExpression_InheritsAccumulatedCaptures()
    {
        var flat = RuleFrom(patternWithConditionRule);
        flat.Expression = "0 AND 1";
        Assert.True(Matches(flat, "alpha guard"));

        var nested = RuleFrom(patternWithConditionRule);
        nested.Expression = "0 AND (1)";

        // The within clause has no boundaries of its own; it can only pass if it sees clause 0's capture.
        Assert.True(Matches(nested, "alpha guard"));
    }

    /// <summary>
    ///     Duplicate labels make OAT abandon evaluation and report no match, rather than raising. Rule
    ///     verification must therefore reject duplicate labels as a hard error.
    /// </summary>
    [Fact]
    public void DuplicateLabels_SilentlyEvaluateFalse()
    {
        var rule = RuleFrom(threePatternRule);
        rule.Clauses[1].Label = "0";
        rule.Expression = "0 OR 2";

        Assert.False(Matches(rule, "alpha beta gamma"));
    }

    /// <summary>
    ///     Clause.Label is OAT's expression identifier, so it must be free for authors to name. The pattern
    ///     index is carried separately on the clause; a non-numeric label must not affect evaluation.
    /// </summary>
    [Fact]
    public void NonNumericLabel_OnRegexClause_IsUsableInExpression()
    {
        var rule = RuleFrom(singleRegexRule);
        rule.Clauses[0].Label = "curl";
        rule.Expression = "curl";

        Assert.True(Matches(rule, "alpha"));
    }

    [Theory]
    [InlineData("0 OR 9")]
    [InlineData("0 OR")]
    [InlineData("0 NOT NOT 1")]
    [InlineData("0 BOGUS 1")]
    [InlineData("0 OR 1)")]
    [InlineData("(0 OR 1))")]
    [InlineData("(0 OR 1")]
    [InlineData("(0 OR (1 AND 2)")]
    public void EnumerateRuleIssues_RejectsMalformedExpressions(string expression)
    {
        var rule = RuleFrom(threePatternRule);
        rule.Expression = expression;

        Analyzer analyzer = new ApplicationInspectorAnalyzer();

        Assert.NotEmpty(analyzer.EnumerateRuleIssues(rule));
    }

    /// <summary>
    ///     Validation reports an unclosed group, but evaluating one still throws, so a rule set must be
    ///     verified before it is run rather than relying on analysis to fail gracefully.
    /// </summary>
    [Theory]
    [InlineData("(0 OR 1")]
    [InlineData("(0 OR (1 AND 2)")]
    public void UnclosedOpeningParenthesis_ThrowsDuringAnalysis(string expression)
    {
        var rule = RuleFrom(threePatternRule);
        rule.Expression = expression;

        Assert.ThrowsAny<System.Exception>(() => Matches(rule, "alpha beta gamma"));
    }

    [Fact]
    public void EnumerateRuleIssues_AcceptsWellFormedExpression()
    {
        var rule = RuleFrom(threePatternRule);
        rule.Expression = "(0 AND NOT 1) OR 2";

        Analyzer analyzer = new ApplicationInspectorAnalyzer();

        Assert.Empty(analyzer.EnumerateRuleIssues(rule));
    }
}
