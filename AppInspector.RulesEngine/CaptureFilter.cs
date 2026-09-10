// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInspector.RulesEngine;

/// <summary>
///     Reduces a rule's raw engine captures to the findings that should be reported.
///     The engine only decides whether a rule matched the content at all, which is a weaker question than
///     which findings the rule reports, so every caller that needs the latter must reduce the captures the
///     same way. <see cref="RuleProcessor" /> reports findings and <see cref="RulesVerifier" /> decides
///     whether a self-test sample matched, so both go through here rather than each judging a rule
///     differently.
/// </summary>
internal static class CaptureFilter
{
    /// <summary>
    ///     Reduces a rule's captures to the findings that should be reported.
    ///     Conditions produce "gates": the subset of pattern matches that satisfied that condition. A rule level
    ///     condition gates every match; a pattern level condition gates only the matches of its own pattern. A
    ///     match survives if it is in every gate that applies to it.
    /// </summary>
    internal static List<(int, Boundary)> FilterCaptures(ConvertedOatRule oatRule, List<ClauseCapture> captures,
        ILogger logger)
    {
        // Gate membership assumes conditions are ANDed, which an authored expression need not be.
        if (!string.IsNullOrWhiteSpace(oatRule.AppInspectorRule.Expression))
        {
            return FilterCapturesByExpression(oatRule, captures, logger);
        }

        var ruleGates = new List<HashSet<(int, Boundary)>>();
        var patternGates = new Dictionary<int, List<HashSet<(int, Boundary)>>>();
        var allMatches = new List<(int, Boundary)>();
        var seen = new HashSet<(int, Boundary)>();

        foreach (var capture in captures)
        {
            if (capture is not TypedClauseCapture<List<(int, Boundary)>> tcc || tcc.Result is null)
            {
                continue;
            }

            if (capture.Clause is WithinClause withinClause)
            {
                var gate = new HashSet<(int, Boundary)>(tcc.Result);
                if (withinClause.OwnerPatternIndex is { } owner)
                {
                    if (!patternGates.TryGetValue(owner, out var gatesForPattern))
                    {
                        gatesForPattern = new List<HashSet<(int, Boundary)>>();
                        patternGates[owner] = gatesForPattern;
                    }

                    gatesForPattern.Add(gate);
                }
                else
                {
                    ruleGates.Add(gate);
                }
            }
            else
            {
                foreach (var match in tcc.Result)
                    if (seen.Add(match))
                    {
                        allMatches.Add(match);
                    }
            }
        }

        if (ruleGates.Count == 0 && patternGates.Count == 0)
        {
            return allMatches;
        }

        // A condition that failed outright contributes no gate. Its pattern's matches must still be dropped,
        // so compare against the conditions the rule declares rather than only the gates that materialized.
        var declaredGates = oatRule.Clauses.OfType<WithinClause>().ToList();
        if (ruleGates.Count != declaredGates.Count(x => x.OwnerPatternIndex is null))
        {
            return new List<(int, Boundary)>();
        }

        return allMatches.Where(Survives).ToList();

        bool Survives((int, Boundary) match)
        {
            if (!ruleGates.All(gate => gate.Contains(match)))
            {
                return false;
            }

            var declaredForPattern = declaredGates.Count(x => x.OwnerPatternIndex == match.Item1);
            if (declaredForPattern == 0)
            {
                return true;
            }

            return patternGates.TryGetValue(match.Item1, out var gatesForPattern) &&
                   gatesForPattern.Count == declaredForPattern &&
                   gatesForPattern.All(gate => gate.Contains(match));
        }
    }

    /// <summary>
    ///     Reports the findings that individually satisfy the rule's expression.
    ///     The engine evaluates the same expression, but only to decide whether the rule matched at all; it
    ///     has no notion of which findings satisfied it. Its captures approximate that, because a
    ///     sub-expression that evaluated false contributes none, which is why most shapes report correctly
    ///     without this. It breaks down when sibling conditions each succeed for a different finding: both
    ///     contribute captures, and intersecting them then demands one finding that passed every condition
    ///     and reports nothing. Re-evaluating per finding is what makes those report correctly.
    /// </summary>
    private static List<(int, Boundary)> FilterCapturesByExpression(ConvertedOatRule oatRule,
        List<ClauseCapture> captures, ILogger logger)
    {
        if (oatRule.ParsedExpression is not { } expression)
        {
            // Verification rejects these, so reaching here means the rule set was not verified.
            logger.LogError(
                "Expression '{expression}' in rule {id} could not be parsed, so no findings will be reported for it.",
                oatRule.AppInspectorRule.Expression, oatRule.AppInspectorRule.Id);
            return new List<(int, Boundary)>();
        }

        Dictionary<string, HashSet<(int, Boundary)>> reportedByLabel = new();
        List<(int, Boundary)> candidates = new();
        HashSet<(int, Boundary)> seenCandidates = new();

        foreach (var capture in captures)
        {
            if (capture is not TypedClauseCapture<List<(int, Boundary)>> tcc || capture.Clause?.Label is not { } label)
            {
                continue;
            }

            if (!reportedByLabel.TryGetValue(label, out var reported))
            {
                reported = new HashSet<(int, Boundary)>();
                reportedByLabel[label] = reported;
            }

            foreach (var finding in tcc.Result)
            {
                reported.Add(finding);

                // Only patterns originate findings; conditions merely retain them.
                if (capture.Clause is not WithinClause && seenCandidates.Add(finding))
                {
                    candidates.Add(finding);
                }
            }
        }

        return candidates
            .Where(candidate => expression.Evaluate(label =>
                reportedByLabel.TryGetValue(label, out var reported) && reported.Contains(candidate)))
            .ToList();
    }
}
