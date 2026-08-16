using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine;
using Xunit;

namespace AppInspector.Tests.RuleProcessor;

/// <summary>
///     The per finding evaluator must agree with the rule engine's own folding rules, otherwise a rule
///     could match while none of its findings are reported, or the reverse.
/// </summary>
[ExcludeFromCodeCoverage]
public class RuleExpressionParserTests
{
    private static bool Eval(string expression, params string[] trueLabels)
    {
        var parsed = RuleExpression.TryParse(expression);
        Assert.NotNull(parsed);

        var set = new HashSet<string>(trueLabels, StringComparer.Ordinal);
        return parsed!.Evaluate(label => set.Contains(label));
    }

    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("a", "b", false)]
    [InlineData("NOT a", "b", true)]
    [InlineData("NOT a", "a", false)]
    public void SingleTerms(string expression, string trueLabel, bool expected)
    {
        Assert.Equal(expected, Eval(expression, trueLabel));
    }

    /// <summary>
    ///     `a OR b AND c` folds as `(a OR b) AND c`, so a alone is not enough.
    /// </summary>
    [Fact]
    public void OperatorsFoldLeftToRightWithoutPrecedence()
    {
        Assert.False(Eval("a OR b AND c", "a"));
        Assert.True(Eval("a OR b AND c", "a", "c"));

        Assert.True(Eval("a OR (b AND c)", "a"));
        Assert.False(Eval("a OR (b AND c)", "b"));
    }

    [Fact]
    public void ParenthesesGroup()
    {
        Assert.True(Eval("(a AND NOT c) OR b", "a"));
        Assert.False(Eval("(a AND NOT c) OR b", "a", "c"));
        Assert.True(Eval("(a AND NOT c) OR b", "b", "c"));
    }

    [Fact]
    public void NegatedConjunctionFiresOnPartialSatisfaction()
    {
        Assert.True(Eval("p AND NOT (a AND b)", "p", "a"));
        Assert.True(Eval("p AND NOT (a AND b)", "p", "b"));
        Assert.True(Eval("p AND NOT (a AND b)", "p"));
        Assert.False(Eval("p AND NOT (a AND b)", "p", "a", "b"));
    }

    [Theory]
    [InlineData("a XOR b", new[] { "a" }, true)]
    [InlineData("a XOR b", new[] { "a", "b" }, false)]
    [InlineData("a NAND b", new[] { "a", "b" }, false)]
    [InlineData("a NAND b", new[] { "a" }, true)]
    [InlineData("a NOR b", new string[0], true)]
    [InlineData("a NOR b", new[] { "a" }, false)]
    public void BooleanOperators(string expression, string[] trueLabels, bool expected)
    {
        Assert.Equal(expected, Eval(expression, trueLabels));
    }

    [Fact]
    public void NestedParenthesesGroup()
    {
        Assert.True(Eval("((a OR b) AND c)", "a", "c"));
        Assert.False(Eval("((a OR b) AND c)", "a"));
    }

    [Theory]
    [InlineData("(a OR b")]
    [InlineData("a OR")]
    [InlineData("a b")]
    [InlineData("")]
    [InlineData("AND a")]
    public void MalformedExpressionsDoNotParse(string expression)
    {
        Assert.Null(RuleExpression.TryParse(expression));
    }

    /// <summary>
    ///     Operands are folded iteratively rather than through a left leaning tree, so a long flat
    ///     expression must evaluate without recursing once per operator and exhausting the stack.
    ///     A left leaning tree survives 50,000 operands here but dies at 1,000,000, so the count is chosen
    ///     to actually catch a reversion.
    /// </summary>
    [Fact]
    public void LongFlatExpressionEvaluatesWithoutRecursingPerOperator()
    {
        const int operands = 1000000;

        var expression = string.Join(" OR ", Enumerable.Repeat("b", operands - 1).Prepend("a"));
        var parsed = RuleExpression.TryParse(expression);

        Assert.NotNull(parsed);
        Assert.True(parsed!.Evaluate(label => label == "a"));
        Assert.False(parsed.Evaluate(label => label == "z"));
    }

    [Fact]
    public void LongFlatConjunctionEvaluatesWithoutRecursingPerOperator()
    {
        const int operands = 1000000;

        var expression = string.Join(" AND ", Enumerable.Repeat("a", operands));
        var parsed = RuleExpression.TryParse(expression);

        Assert.NotNull(parsed);
        Assert.True(parsed!.Evaluate(label => label == "a"));
        Assert.False(parsed.Evaluate(_ => false));
    }
}
