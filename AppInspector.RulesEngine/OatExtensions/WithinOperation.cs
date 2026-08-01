using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CST.OAT;
using Microsoft.CST.OAT.Operations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions;

public class WithinOperation : OatOperation
{
    private readonly Analyzer _analyzer;

    private readonly ILoggerFactory _loggerFactory;

    public WithinOperation(Analyzer analyzer, ILoggerFactory? loggerFactory = null) : base(Operation.Custom, analyzer)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _analyzer = analyzer;
        CustomOperation = "Within";
        OperationDelegate = WithinOperationDelegate;
        ValidationDelegate = WithinValidationDelegate;
    }

    public OperationResult WithinOperationDelegate(Clause c, object? state1, object? _,
        IEnumerable<ClauseCapture>? captures)
    {
        if (c is WithinClause wc && state1 is TextContainer tc)
        {
            var governed = GovernedMatches(wc, captures);

            // A condition that does not apply to this file's language still has to produce a capture, because
            // RuleProcessor needs one gate per declared condition to know which matches have been vetted. This gate
            // passes through everything the condition governs, so it filters nothing. Building it from the governed
            // matches rather than from whatever captures happen to have accumulated keeps the result independent of
            // the order the conditions are declared in.
            if (!ConditionAppliesToLanguage(wc, tc.Language))
            {
                return new OperationResult(true,
                    governed.Count > 0 ? new TypedClauseCapture<List<(int, Boundary)>>(wc, governed) : null);
            }

            var passed =
                new List<(int, Boundary)>();
            var failed =
                new List<(int, Boundary)>();

            foreach ((var clauseNum, var boundary) in governed)
            {
                var boundaryToCheck = GetBoundaryToCheck();
                if (boundaryToCheck is not null)
                {
                    var operationResult = ProcessLambda(boundaryToCheck);
                    if (operationResult.Result)
                    {
                        passed.Add((clauseNum, boundary));
                    }
                    else
                    {
                        failed.Add((clauseNum, boundary));
                    }
                }

                Boundary? GetBoundaryToCheck()
                {
                    if (wc.FindingOnly)
                    {
                        return boundary;
                    }

                    if (wc.SameLineOnly)
                    {
                        var startInner = tc.LineStarts[tc.GetLocation(boundary.Index).Line];
                        var endInner = tc.LineEnds[tc.GetLocation(startInner + (boundary.Length - 1)).Line];
                        return new Boundary
                        {
                            Index = startInner,
                            Length = endInner - startInner + 1
                        };
                    }

                    if (wc.FindingRegion)
                    {
                        var startLine = tc.GetLocation(boundary.Index).Line;
                        // Before is already a negative number
                        var startInner = tc.LineStarts[Math.Max(1, startLine + wc.Before)];
                        var endInner = tc.LineEnds[Math.Min(tc.LineEnds.Count - 1, startLine + wc.After)];
                        return new Boundary
                        {
                            Index = startInner,
                            Length = endInner - startInner + 1
                        };
                    }

                    if (wc.SameFile)
                    {
                        var startInner = tc.LineStarts[0];
                        var endInner = tc.LineEnds[^1];
                        return new Boundary
                        {
                            Index = startInner,
                            Length = endInner - startInner + 1
                        };
                    }

                    if (wc.OnlyBefore)
                    {
                        var startInner = tc.LineStarts[0];
                        var endInner = boundary.Index;
                        return new Boundary
                        {
                            Index = startInner,
                            Length = endInner - startInner + 1
                        };
                    }

                    if (wc.OnlyAfter)
                    {
                        var startInner = boundary.Index + boundary.Length;
                        var endInner = tc.LineEnds[^1];
                        return new Boundary
                        {
                            Index = startInner,
                            Length = endInner - startInner + 1
                        };
                    }

                    return null;
                }
            }

            // The governed list is built from every pattern capture the clause was handed, so all of a rule's
            // patterns are filtered rather than only the first one.
            var passedOrFailed = wc.Invert ? failed : passed;
            return new OperationResult(passedOrFailed.Any(),
                passedOrFailed.Any()
                    ? new TypedClauseCapture<List<(int, Boundary)>>(wc, passedOrFailed.ToList())
                    : null);

            OperationResult ProcessLambda(Boundary target)
            {
                return _analyzer.GetClauseCapture(wc.SubClause, tc, target, captures);
            }
        }

        return new OperationResult(false);
    }

    /// <summary>
    ///     The pattern matches this condition is responsible for filtering.
    /// </summary>
    /// <remarks>
    ///     Two kinds of capture are excluded. Captures produced by another <see cref="WithinClause" /> are skipped
    ///     because each condition filters the raw pattern matches independently and <c>RuleProcessor</c> intersects the
    ///     survivors to AND the conditions together; consuming an already filtered capture would double count matches
    ///     and break that intersection. Matches belonging to another pattern are skipped for a pattern level condition,
    ///     because OAT hands a clause every capture accumulated so far, which for a later pattern in the rule includes
    ///     the captures of the patterns before it.
    /// </remarks>
    private static List<(int, Boundary)> GovernedMatches(WithinClause wc, IEnumerable<ClauseCapture>? captures)
    {
        var governed = new List<(int, Boundary)>();
        var seen = new HashSet<(int, Boundary)>();

        foreach (var capture in captures ?? Array.Empty<ClauseCapture>())
        {
            if (capture.Clause is WithinClause || capture is not TypedClauseCapture<List<(int, Boundary)>> tcc ||
                tcc.Result is null)
            {
                continue;
            }

            foreach (var match in tcc.Result)
            {
                if (wc.OwnerPatternIndex is { } owner && match.Item1 != owner)
                {
                    continue;
                }

                if (seen.Add(match))
                {
                    governed.Add(match);
                }
            }
        }

        return governed;
    }

    public IEnumerable<Violation> WithinValidationDelegate(CST.OAT.Rule rule, Clause clause)
    {
        if (clause is WithinClause wc)
        {
            if (new[] { wc.FindingOnly, wc.SameLineOnly, wc.FindingRegion, wc.OnlyAfter, wc.OnlyBefore, wc.SameFile }
                    .Count(x => x) !=
                1)
            {
                yield return new Violation(
                    "Exactly one of: FindingOnly, SameLineOnly, OnlyAfter, OnlyBefore, SameFile or FindingRegion must be set",
                    rule, clause);
            }

            if (wc.FindingRegion)
            {
                if (wc.Before == 0 && wc.After == 0)
                {
                    yield return new Violation(
                        "Both parameters for finding-region may not be 0. Use same-line to only analyze the same line.",
                        rule, clause);
                }

                if (wc.Before > 0)
                {
                    yield return new Violation(
                        "The first parameter for finding region, representing number of lines before, must be 0 or negative",
                        rule, clause);
                }

                if (wc.After < 0)
                {
                    yield return new Violation(
                        "The second parameter for finding region, representing number of lines after, must be 0 or positive",
                        rule, clause);
                }
            }

            if (wc.Data.Any())
            {
                yield return new Violation("Don't provide data directly. Instead use SubClause.", rule, clause);
            }

            var subOp = _analyzer
                .GetOperation(wc.SubClause.Operation, wc.SubClause.CustomOperation);

            if (subOp is null)
            {
                yield return new Violation(
                    $"SubClause in Rule {rule.Name} Clause {clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)} is of type '{wc.SubClause.Operation},{wc.SubClause.CustomOperation}' is not present in the analyzer.",
                    rule, clause);
            }
            else
            {
                foreach (var violation in subOp.ValidationDelegate.Invoke(rule, wc.SubClause)) yield return violation;

                if (wc.SubClause is OatRegexWithIndexClause oatRegexWithIndexClause)
                {
                    if ((oatRegexWithIndexClause.JsonPaths?.Any() ?? false) ||
                        (oatRegexWithIndexClause.XPaths?.Any() ?? false)||
                        (oatRegexWithIndexClause.YmlPaths?.Any() ?? false))
                    {
                        if (wc.FindingOnly || wc.SameLineOnly || wc.FindingRegion || wc.OnlyAfter || wc.OnlyBefore)
                        {
                            yield return new Violation("When providing JSONPaths, YMLPaths or XPaths must use same-file region.",
                                rule, clause);
                        }
                    }
                }

                if (wc.SubClause is OatSubstringIndexClause oatSubstringIndexClause)
                {
                    if ((oatSubstringIndexClause.JsonPaths?.Any() ?? false) ||
                        (oatSubstringIndexClause.XPaths?.Any() ?? false) || 
                        (oatSubstringIndexClause.YmlPaths?.Any() ?? false))
                    {
                        if (wc.FindingOnly || wc.SameLineOnly || wc.FindingRegion || wc.OnlyAfter || wc.OnlyBefore)
                        {
                            yield return new Violation("When providing JSONPaths, YMLPaths or XPaths must use same-file region.",
                                rule, clause);
                        }
                    }
                }
            }
        }
        else
        {
            yield return new Violation(
                $"Rule {rule.Name} clause {clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)} is not a WithinClause",
                rule, clause);
        }
    }

    /// <summary>
    /// Check if a WithinClause's language filters allow it to apply to the given language.
    /// </summary>
    private static bool ConditionAppliesToLanguage(WithinClause clause, string currentLanguage)
    {
        // If applies_to is specified and doesn't include current language, condition doesn't apply
        if (clause.LanguageAppliesTo is { Count: > 0 })
        {
            if (!clause.LanguageAppliesTo.Contains(currentLanguage, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // If does_not_apply_to includes current language, condition doesn't apply
        if (clause.LanguageDoesNotApplyTo is { Count: > 0 })
        {
            if (clause.LanguageDoesNotApplyTo.Contains(currentLanguage, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}