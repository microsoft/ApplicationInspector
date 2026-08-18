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

    /// <summary>
    ///     Sibling groups are independent, so different operators in each are explicitly grouped and must
    ///     not be treated as mixed.
    /// </summary>
    [Fact]
    public void DifferentOperatorsInSiblingGroups_Verify()
    {
        var status = Verify(RuleWith(@"""expression"": ""(a AND c) OR (b XOR c)"",", twoPatterns, oneCondition));

        Assert.Empty(status.Errors);
        Assert.True(status.Verified);
    }

    /// <summary>
    ///     A finding comes from one pattern, so an expression requiring two patterns at once matches at the
    ///     rule level and then reports nothing. That must fail verification rather than look like a rule
    ///     that simply never fires.
    /// </summary>
    [Fact]
    public void ExpressionRequiringTwoPatternsAtOnce_FailsVerification()
    {
        var status = Verify(RuleWith(@"""expression"": ""a AND b"",", twoPatterns, oneCondition));

        Assert.Contains(status.Errors, x => x.Contains("can never report a finding"));
        Assert.False(status.Verified);
    }

    /// <summary>
    ///     Self-tests execute the rule, and a malformed expression throws rather than simply not matching,
    ///     so verification must report the problem rather than take the process down with it.
    /// </summary>
    [Fact]
    public void MalformedExpressionWithSelfTest_FailsVerificationWithoutThrowing()
    {
        const string withSelfTests = @"[
    {
        ""id"": ""SA600002"",
        ""name"": ""Testing.Rules.SelfTested"",
        ""tags"": [ ""Testing.Rules.SelfTested"" ],
        ""severity"": ""Critical"",
        ""description"": ""unbalanced expression with self tests"",
        ""expression"": ""(a OR b"",
        ""patterns"": [
            { ""pattern"": ""alpha"", ""type"": ""substring"", ""label"": ""a"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""beta"",  ""type"": ""substring"", ""label"": ""b"", ""scopes"": [ ""code"" ] }
        ],
        ""must-match"": [ ""alpha"" ],
        ""must-not-match"": [ ""gamma"" ]
    }
]";
        var status = Verify(withSelfTests);

        Assert.Contains(status.Errors, x => x.Contains("unbalanced parentheses"));
        Assert.False(status.Verified);
    }

    /// <summary>
    ///     Captures accumulate in evaluation order, so a condition reached before any pattern has nothing
    ///     to test and is always false. Such a rule verifies clean today and then never reports.
    /// </summary>
    [Fact]
    public void ConditionBeforeAnyPattern_FailsVerification()
    {
        var status = Verify(RuleWith(@"""expression"": ""c AND a"",", twoPatterns, oneCondition));

        Assert.Contains(status.Errors, x => x.Contains("before any pattern"));
        Assert.False(status.Verified);
    }

    [Fact]
    public void ConditionAfterAPattern_Verifies()
    {
        var status = Verify(RuleWith(@"""expression"": ""a AND c"",", twoPatterns, oneCondition));

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

    /// <summary>
    ///     An operator used as a label is read as an operator by the per-finding parser and as an operand
    ///     by the engine, so the rule would match and then report nothing.
    /// </summary>
    [Theory]
    [InlineData("AND")]
    [InlineData("or")]
    [InlineData("Not")]
    [InlineData("XOR")]
    [InlineData("nand")]
    [InlineData("NOR")]
    public void OperatorShapedLabel_FailsVerification(string label)
    {
        var patterns =
            $@"{{ ""pattern"": ""alpha"", ""type"": ""substring"", ""label"": ""{label}"", ""scopes"": [ ""code"" ] }}";

        var status = Verify(RuleWith(string.Empty, patterns, oneCondition));

        Assert.Contains(status.Errors, x => x.Contains("is an expression operator"));
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

    /// <summary>
    ///     Counting parentheses to zero is not enough on its own, because a group can be closed before it was
    ///     ever opened. Such an expression balances on a naive count but still throws when evaluated.
    /// </summary>
    [Fact]
    public void ParenthesisClosedBeforeItOpens_FailsVerification()
    {
        var status = Verify(RuleWith(@"""expression"": ""a) AND (b"",", twoPatterns, oneCondition));

        Assert.Contains(status.Errors, x => x.Contains("unbalanced parentheses"));
        Assert.False(status.Verified);
    }

    /// <summary>
    ///     The engine is handed a weaker expression than the authored one, so it never sees these operands and
    ///     cannot flag them. An unresolved operand is simply false when findings are judged, so verification has
    ///     to be what catches it.
    /// </summary>
    [Theory]
    [InlineData("a OR nosuchlabel")]
    [InlineData("a OR 0")]
    public void ExpressionNamingAnUnknownLabel_FailsVerification(string expression)
    {
        var status = Verify(RuleWith($@"""expression"": ""{expression}"",", twoPatterns, oneCondition));

        Assert.Contains(status.Errors, x => x.Contains("is not a pattern or condition label"));
        Assert.False(status.Verified);
    }
}
