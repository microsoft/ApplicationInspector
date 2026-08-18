using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.RulesEngine;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     Rules that do not opt in to labels or an expression must translate exactly as they always have.
/// </summary>
[ExcludeFromCodeCoverage]
public class ExpressionGenerationTests
{
    private readonly Microsoft.ApplicationInspector.RulesEngine.Languages _languages = new();

    private static string GeneratedExpression(int patternClauses, int conditionClauses)
    {
        var expression = "(" + string.Join(" OR ", Enumerable.Range(0, patternClauses)) + ")";
        for (var i = 0; i < conditionClauses; i++) expression += $" AND c{i}";
        return expression;
    }

    /// <summary>
    ///     Golden test over every shipped rule: a rule that opts into neither labels nor an expression must
    ///     generate the same string it would without this feature. Patterns keep bare numeric labels because
    ///     the pattern index is read back from them; conditions live in their own c-prefixed namespace so the
    ///     two cannot collide.
    /// </summary>
    [Fact]
    public void DefaultRules_GenerateUnchangedExpressions()
    {
        var ruleSet = RuleSetUtils.GetDefaultRuleSet();
        var oatRules = ruleSet.GetOatRules().ToList();

        Assert.NotEmpty(oatRules);

        List<string> mismatches = new();

        foreach (var oatRule in oatRules)
        {
            var patternClauses = oatRule.Clauses.Count(x => x is not WithinClause);
            var conditionClauses = oatRule.Clauses.Count(x => x is WithinClause);

            // Every shipped pattern must produce a clause, otherwise indices and labels would shift.
            Assert.Equal(oatRule.AppInspectorRule.Patterns.Length, patternClauses);

            var expected = GeneratedExpression(patternClauses, conditionClauses);

            if (oatRule.Expression != expected)
            {
                mismatches.Add($"{oatRule.AppInspectorRule.Id}: expected '{expected}' but got '{oatRule.Expression}'");
            }
        }

        Assert.Empty(mismatches);
    }

    [Fact]
    public void DefaultRules_LabelPatternsNumericallyAndConditionsSeparately()
    {
        var ruleSet = RuleSetUtils.GetDefaultRuleSet();

        foreach (var oatRule in ruleSet.GetOatRules())
        {
            var patternLabels = oatRule.Clauses.Where(x => x is not WithinClause).Select(x => x.Label);
            var conditionLabels = oatRule.Clauses.OfType<WithinClause>().Select(x => x.Label).ToList();

            Assert.Equal(Enumerable.Range(0, oatRule.AppInspectorRule.Patterns.Length).Select(x => x.ToString()),
                patternLabels);
            Assert.Equal(Enumerable.Range(0, conditionLabels.Count).Select(x => $"c{x}"), conditionLabels);
        }
    }

    /// <summary>
    ///     The engine decides only whether a file is worth examining, so it gets a disjunction of every clause.
    ///     The authored expression stays on the rule and is applied per finding, which is what keeps a file-wide
    ///     NOT from suppressing findings that individually satisfy it.
    /// </summary>
    [Fact]
    public void AuthorSuppliedExpression_IsAppliedPerFindingNotByTheEngine()
    {
        const string ruleJson = @"[
    {
        ""id"": ""SA400001"",
        ""name"": ""Testing.Rules.CustomExpression"",
        ""tags"": [ ""Testing.Rules.CustomExpression"" ],
        ""severity"": ""Critical"",
        ""description"": ""author supplied expression"",
        ""expression"": ""(curl AND NOT tls13) OR wget"",
        ""patterns"": [
            { ""pattern"": ""curl"", ""type"": ""substring"", ""label"": ""curl"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""wget"", ""type"": ""substring"", ""label"": ""wget"", ""scopes"": [ ""code"" ] }
        ],
        ""conditions"": [
            {
                ""pattern"": { ""pattern"": ""--tlsv1.3"", ""type"": ""substring"", ""scopes"": [ ""code"" ] },
                ""search_in"": ""same-line"",
                ""label"": ""tls13""
            }
        ]
    }
]";
        RuleSet rules = new();
        rules.AddString(ruleJson, "TestRules");
        var oatRule = rules.GetOatRules().Single();

        Assert.Equal("curl OR wget OR tls13", oatRule.Expression);
        Assert.Equal("(curl AND NOT tls13) OR wget", oatRule.AppInspectorRule.Expression);
        Assert.Equal(new[] { "curl", "wget", "tls13" }, oatRule.Clauses.Select(x => x.Label));
    }

    [Fact]
    public void AuthorSuppliedLabels_WithoutExpression_AreUsedInGeneratedExpression()
    {
        const string ruleJson = @"[
    {
        ""id"": ""SA400002"",
        ""name"": ""Testing.Rules.LabelsOnly"",
        ""tags"": [ ""Testing.Rules.LabelsOnly"" ],
        ""severity"": ""Critical"",
        ""description"": ""labels but no expression"",
        ""patterns"": [
            { ""pattern"": ""curl"", ""type"": ""substring"", ""label"": ""curl"", ""scopes"": [ ""code"" ] },
            { ""pattern"": ""wget"", ""type"": ""substring"", ""label"": ""wget"", ""scopes"": [ ""code"" ] }
        ]
    }
]";
        RuleSet rules = new();
        rules.AddString(ruleJson, "TestRules");

        Assert.Equal("(curl OR wget)", rules.GetOatRules().Single().Expression);
    }
}
