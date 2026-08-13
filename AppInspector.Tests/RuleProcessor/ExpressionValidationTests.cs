using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     A rule whose expression or labels are malformed must fail verification rather than silently never
///     matching or throwing mid scan.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExpressionValidationTests
{
    private static string RuleWith(string extraRuleFields, string patterns, string conditions)
    {
        return $@"[
    {{
        ""id"": ""SA600001"",
        ""name"": ""Testing.Rules.Validation"",
        ""tags"": [ ""Testing.Rules.Validation"" ],
        ""severity"": ""Critical"",
        ""description"": ""validation fixture"",
        {extraRuleFields}
        ""patterns"": [ {patterns} ],
        ""conditions"": [ {conditions} ]
    }}
]";
    }

    private const string twoPatterns =
        @"{ ""pattern"": ""alpha"", ""type"": ""substring"", ""label"": ""a"", ""scopes"": [ ""code"" ] },
          { ""pattern"": ""beta"",  ""type"": ""substring"", ""label"": ""b"", ""scopes"": [ ""code"" ] }";

    private const string oneCondition =
        @"{ ""pattern"": { ""pattern"": ""guard"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
            ""search_in"": ""same-line"", ""label"": ""c"" }";

    private static RuleStatus Verify(string ruleJson)
    {
        RuleSet rules = new();
        rules.AddString(ruleJson, "TestRules");
        RulesVerifier verifier = new(new RulesVerifierOptions());
        return verifier.CheckIntegrity(rules).Single();
    }

    [Fact]
    public void WellFormedExpression_Verifies()
    {
        var status = Verify(RuleWith(@"""expression"": ""(a AND NOT c) OR b"",", twoPatterns, oneCondition));

        Assert.Empty(status.Errors);
        Assert.Empty(status.OatIssues);
        Assert.True(status.Verified);
    }

    [Fact]
    public void UnbalancedParentheses_FailVerification()
    {
        var status = Verify(RuleWith(@"""expression"": ""(a OR b"",", twoPatterns, oneCondition));

        Assert.Contains(status.Errors, x => x.Contains("unbalanced parentheses"));
        Assert.False(status.Verified);
    }

    [Fact]
    public void DuplicateLabels_FailVerification()
    {
        const string duplicated =
            @"{ ""pattern"": ""alpha"", ""type"": ""substring"", ""label"": ""a"", ""scopes"": [ ""code"" ] },
              { ""pattern"": ""beta"",  ""type"": ""substring"", ""label"": ""a"", ""scopes"": [ ""code"" ] }";

        var status = Verify(RuleWith(@"""expression"": ""a"",", duplicated, oneCondition));

        Assert.Contains(status.Errors, x => x.Contains("more than one pattern or condition"));
        Assert.False(status.Verified);
    }

    /// <summary>
    ///     Mixing operators without parentheses is almost always an authoring mistake, because evaluation
    ///     folds left to right with no precedence.
    /// </summary>
    [Fact]
    public void MixedOperatorsWithoutParentheses_FailVerification()
    {
        var status = Verify(RuleWith(@"""expression"": ""a OR b AND c"",", twoPatterns, oneCondition));

        Assert.Contains(status.Errors, x => x.Contains("without parentheses"));
        Assert.False(status.Verified);
    }

    [Fact]
    public void MixedOperatorsInSeparateGroups_Verify()
    {
        var status = Verify(RuleWith(@"""expression"": ""(a AND c) OR b"",", twoPatterns, oneCondition));

        Assert.Empty(status.Errors);
        Assert.True(status.Verified);
    }

    [Fact]
    public void ExpressionWithNegateFinding_FailsVerification()
    {
        const string negatedCondition =
            @"{ ""pattern"": { ""pattern"": ""guard"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                ""search_in"": ""same-line"", ""label"": ""c"", ""negate_finding"": true }";

        var status = Verify(RuleWith(@"""expression"": ""(a AND NOT c) OR b"",", twoPatterns, negatedCondition));

        Assert.Contains(status.Errors, x => x.Contains("negate_finding"));
        Assert.False(status.Verified);
    }

    [Fact]
    public void UndefinedLabelInExpression_FailsVerification()
    {
        var status = Verify(RuleWith(@"""expression"": ""a OR missing"",", twoPatterns, oneCondition));

        Assert.False(status.Verified);
    }

    [Fact]
    public void LabelContainingWhitespace_FailsVerification()
    {
        const string spacedLabel =
            @"{ ""pattern"": ""alpha"", ""type"": ""substring"", ""label"": ""a b"", ""scopes"": [ ""code"" ] }";

        var status = Verify(RuleWith(string.Empty, spacedLabel, oneCondition));

        Assert.Contains(status.Errors, x => x.Contains("whitespace or parentheses"));
        Assert.False(status.Verified);
    }
}
