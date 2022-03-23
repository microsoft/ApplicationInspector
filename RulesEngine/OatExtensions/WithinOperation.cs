using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CST.OAT;
using Microsoft.CST.OAT.Operations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions
{
    public class WithinOperation : OatOperation
    {
        public WithinOperation(Analyzer analyzer, ILoggerFactory? loggerFactory = null) : base(Operation.Custom, analyzer)
        {
            _loggerFactory = loggerFactory ?? new NullLoggerFactory();
            _regexEngine = new RegexOperation(analyzer);
            CustomOperation = "Within";
            OperationDelegate = WithinOperationDelegate;
            ValidationDelegate = WithinValidationDelegate;
        }

        public OperationResult WithinOperationDelegate(Clause c, object? state1, object? _, IEnumerable<ClauseCapture>? captures)
        {
            if (c is WithinClause wc && state1 is TextContainer tc)
            {
                var regexOpts = RegexOptions.Compiled;
                if (wc.Arguments.Contains("i"))
                {
                    regexOpts |= RegexOptions.IgnoreCase;
                }
                if (wc.Arguments.Contains("m"))
                {
                    regexOpts |= RegexOptions.Multiline;
                }
                var passed = new List<Boundary>();
                foreach (var captureHolder in captures ?? Array.Empty<ClauseCapture>())
                {
                    if (captureHolder is TypedClauseCapture<List<(int, Boundary)>> tcc)
                    {
                        List<(int, Boundary)> toRemove = new();
                        foreach ((int clauseNum, Boundary capture) in tcc.Result)
                        {
                            if (wc.FindingOnly)
                            {
                                var res = ProcessLambda(tc.GetBoundaryText(capture), capture);
                                if (res.Result)
                                {
                                    if (res.Capture is TypedClauseCapture<List<Boundary>> boundaryList)
                                    {
                                        passed.AddRange(boundaryList.Result);
                                    }
                                }
                                else
                                {
                                    toRemove.Add((clauseNum, capture));
                                }
                            }
                            else if (wc.SameLineOnly)
                            {
                                var start = tc.LineStarts[tc.GetLocation(capture.Index).Line];
                                var end = tc.LineEnds[tc.GetLocation(start + capture.Length).Line];
                                var res = ProcessLambda(tc.FullContent[start..end], capture);
                                if (res.Result)
                                {
                                    if (res.Capture is TypedClauseCapture<List<Boundary>> boundaryList)
                                    {
                                        passed.AddRange(boundaryList.Result);
                                    }
                                }
                                else
                                {
                                    toRemove.Add((clauseNum, capture));
                                }
                            }
                            else if (wc.FindingRegion)
                            {
                                var startLine = tc.GetLocation(capture.Index).Line;
                                // Before is already a negative number
                                var start = tc.LineStarts[Math.Max(1, startLine + wc.Before)];
                                var end = tc.LineEnds[Math.Min(tc.LineEnds.Count - 1, startLine + wc.After)];
                                var res = ProcessLambda(tc.FullContent[start..(end+1)], capture);
                                if (res.Result)
                                {
                                    if (res.Capture is TypedClauseCapture<List<Boundary>> boundaryList)
                                    {
                                        passed.AddRange(boundaryList.Result);
                                    }
                                }
                                else
                                {
                                    toRemove.Add((clauseNum, capture));
                                }
                            }
                        }
                        tcc.Result.RemoveAll(x => toRemove.Contains(x));
                    }
                }
                // In the case that we have inverted the lambda, the captures are null and thus the passed list will be empty. We thus need to invert this again to get true correctly in that case.
                return new OperationResult(passed.Any() ^ wc.Invert, passed.Any() ? new TypedClauseCapture<List<Boundary>>(wc, passed) : null);

                OperationResult ProcessLambda(string target, Boundary targetBoundary)
                {
                    var boundaries = new List<Boundary>();
                    foreach (var pattern in wc.Data.Select(x => _regexEngine.StringToRegex(x, regexOpts)))
                    {
                        if (pattern is Regex r)
                        {
                            var matches = r.Matches(target);
                            foreach (var match in matches)
                            {
                                if (match is Match m)
                                {
                                    Boundary translatedBoundary = new()
                                    {
                                        Length = m.Length,
                                        Index = targetBoundary.Index + m.Index
                                    };
                                    // Should return only scoped matches
                                    if (tc.ScopeMatch(wc.Scopes, translatedBoundary))
                                    {
                                        boundaries.Add(translatedBoundary);
                                    }
                                }
                            }
                        }
                    }
                    // Invert the result of the operation if requested
                    return new OperationResult(boundaries.Any() ^ wc.Invert, boundaries.Any() ? new TypedClauseCapture<List<Boundary>>(wc, boundaries) : null);
                }
            }
            return new OperationResult(false, null);
        }
        
        public IEnumerable<Violation> WithinValidationDelegate(CST.OAT.Rule rule, Clause clause)
        {
            if (rule is null)
            {
                yield return new Violation($"Rule is null", new CST.OAT.Rule("RuleWasNull"));
                yield break;
            }
            if (clause is null)
            {
                yield return new Violation($"Rule {rule.Name} has a null clause", rule);
                yield break;
            }
            if (clause is WithinClause wc)
            {
                if (!wc.FindingOnly && !wc.SameLineOnly && !wc.FindingRegion)
                {
                    yield return new Violation($"Either FindingOnly, SameLineOnly or Finding Region must be set", rule, clause);
                }
                if (wc.FindingRegion && wc.Before > 0)
                {
                    yield return new Violation($"The first parameter for finding region, representing number of lines before, must be 0 or negative", rule, clause);
                }
                if (wc.FindingRegion && wc.After < 0)
                {
                    yield return new Violation($"The second parameter for finding region, representing number of lines after, must be 0 or positive", rule, clause);
                }
                if (!wc.Data?.Any() ?? true)
                {
                    yield return new Violation($"Must provide some regexes as data.", rule, clause);
                    yield break;
                }
                foreach (var datum in wc.Data ?? new List<string>())
                {
                    if (_regexEngine.StringToRegex(datum, RegexOptions.None) is null)
                    {
                        yield return new Violation($"Regex {datum} in Rule {rule.Name} Clause {clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)} is not a valid regex.", rule, clause);
                    }
                }
            }
            else
            {
                yield return new Violation($"Rule {rule.Name ?? "Null Rule Name"} clause {clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)} is not a WithinClause", rule, clause);
            }
        }

        private readonly RegexOperation _regexEngine;
        private readonly ILoggerFactory _loggerFactory;
    }
}