using System;
using System.Threading;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions;

/// <summary>
///     Wrapper for OAT based processing
/// </summary>
public class ConvertedOatRule : CST.OAT.Rule
{
    private ParsedExpressionCache? _parsedExpressionCache;

    public ConvertedOatRule(string name, Rule rule) : base(name)
    {
        AppInspectorRule = rule;
    }

    /// <summary>
    ///     Native Application Inspector Rule to preserve format and instance data
    /// </summary>
    public Rule AppInspectorRule { get; }

    /// <summary>
    ///     The rule's authored expression parsed for per finding evaluation, or null if there is none or it cannot be
    ///     parsed. This is the expression the rule author wrote, not <see cref="CST.OAT.Rule.Expression" />, which is
    ///     the weaker one handed to the engine. Cached against the rule so it is not reparsed per file.
    /// </summary>
    internal RuleExpression? ParsedExpression
    {
        get
        {
            if (AppInspectorRule.Expression is not { } expression || string.IsNullOrWhiteSpace(expression))
            {
                return null;
            }

            // Files are scanned in parallel, so the source and the parse result are published together behind a
            // single reference. Caching them in two fields would let another thread see a new source paired with a
            // stale or null parse and silently drop that rule's findings.
            var cache = Volatile.Read(ref _parsedExpressionCache);
            if (cache is null || !string.Equals(cache.Source, expression, StringComparison.Ordinal))
            {
                cache = new ParsedExpressionCache(expression, RuleExpression.TryParse(expression));
                Volatile.Write(ref _parsedExpressionCache, cache);
            }

            return cache.Parsed;
        }
    }

    private sealed class ParsedExpressionCache
    {
        internal ParsedExpressionCache(string source, RuleExpression? parsed)
        {
            Source = source;
            Parsed = parsed;
        }

        internal string Source { get; }
        internal RuleExpression? Parsed { get; }
    }
}