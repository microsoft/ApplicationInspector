using System;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions;

/// <summary>
///     Wrapper for OAT based processing
/// </summary>
public class ConvertedOatRule : CST.OAT.Rule
{
    private RuleExpression? _parsedExpression;
    private string? _parsedExpressionSource;

    public ConvertedOatRule(string name, Rule rule) : base(name)
    {
        AppInspectorRule = rule;
    }

    /// <summary>
    ///     Native Application Inspector Rule to preserve format and instance data
    /// </summary>
    public Rule AppInspectorRule { get; }

    /// <summary>
    ///     The rule's expression parsed for per finding evaluation, or null if it cannot be parsed.
    ///     Cached against the rule so it is not reparsed per file, and reparsed if the expression changes.
    /// </summary>
    internal RuleExpression? ParsedExpression
    {
        get
        {
            if (Expression is not { } expression)
            {
                return null;
            }

            if (!string.Equals(_parsedExpressionSource, expression, StringComparison.Ordinal))
            {
                _parsedExpression = RuleExpression.TryParse(expression);
                _parsedExpressionSource = expression;
            }

            return _parsedExpression;
        }
    }
}