using System;
using System.Collections;
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
            _analyzer = analyzer;
            CustomOperation = "Within";
            OperationDelegate = WithinOperationDelegate;
            ValidationDelegate = WithinValidationDelegate;
        }

        public OperationResult WithinOperationDelegate(Clause c, object? state1, object? _, IEnumerable<ClauseCapture>? captures)
        {
            if (c is WithinClause wc && state1 is TextContainer tc)
            {
                List<(int, Boundary)> passed =
                    new List<(int, Boundary)>();
                List<(int, Boundary)> failed =
                    new List<(int, Boundary)>();

                foreach (var capture in captures)
                {
                    if (capture is TypedClauseCapture<List<(int, Boundary)>> tcc)
                    {
                        foreach ((int clauseNum, Boundary boundary) in tcc.Result)
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
                                    var endInner = tc.LineEnds[tc.GetLocation(startInner + boundary.Length).Line];
                                    return new Boundary()
                                    {
                                        Index = startInner,
                                        Length = endInner - startInner
                                    };
                                }

                                if (wc.FindingRegion)
                                {
                                    var startLine = tc.GetLocation(boundary.Index).Line;
                                    // Before is already a negative number
                                    var startInner = tc.LineStarts[Math.Max(1, startLine + wc.Before)];
                                    var endInner = tc.LineEnds[Math.Min(tc.LineEnds.Count - 1, startLine + wc.After)];
                                    return new Boundary()
                                    {
                                        Index = startInner,
                                        Length = endInner - startInner
                                    };
                                }

                                if (wc.SameFile)
                                {
                                    var startInner = tc.LineStarts[0];
                                    var endInner = tc.LineEnds[^1];
                                    return new Boundary()
                                    {
                                        Index = startInner,
                                        Length = endInner - startInner
                                    };
                                }

                                if (wc.OnlyBefore)
                                {
                                    var startInner = tc.LineStarts[0];
                                    var endInner = boundary.Index;
                                    return new Boundary()
                                    {
                                        Index = startInner,
                                        Length = endInner - startInner
                                    };
                                }

                                if (wc.OnlyAfter)
                                {
                                    var startInner = boundary.Index + boundary.Length;
                                    var endInner = tc.LineEnds[^1];
                                    return new Boundary()
                                    {
                                        Index = startInner,
                                        Length = endInner - startInner
                                    };
                                }

                                return null;
                            }
                        }
                    }

                    var passedOrFailed = wc.Invert ? failed : passed;
                    return new OperationResult(passedOrFailed.Any(), passedOrFailed.Any() ? new TypedClauseCapture<List<(int, Boundary)>>(wc, passedOrFailed.ToList()) : null);
                }

                OperationResult ProcessLambda(Boundary target)
                {
                    return _analyzer.GetClauseCapture(wc.SubClause, tc, target, captures);
                }
            }
            return new OperationResult(false, null);
        }
        
        public IEnumerable<Violation> WithinValidationDelegate(CST.OAT.Rule rule, Clause clause)
        {
            if (clause is WithinClause wc)
            {
                if (new bool[] {wc.FindingOnly, wc.SameLineOnly, wc.FindingRegion, wc.OnlyAfter, wc.OnlyBefore, wc.SameFile}.Count(x => x) != 1)
                {
                    yield return new Violation($"Exactly one of: FindingOnly, SameLineOnly, OnlyAfter, OnlyBefore, SameFile or FindingRegion must be set", rule, clause);
                }

                if (wc.FindingRegion)
                {
                    if (wc.Before == 0 && wc.After == 0)
                    {
                        yield return new Violation(
                            $"Both parameters for finding-region may not be 0. Use same-line to only analyze the same line.",
                            rule, clause);
                    }

                    if (wc.Before > 0)
                    {
                        yield return new Violation(
                            $"The first parameter for finding region, representing number of lines before, must be 0 or negative",
                            rule, clause);
                    }

                    if (wc.After < 0)
                    {
                        yield return new Violation(
                            $"The second parameter for finding region, representing number of lines after, must be 0 or positive",
                            rule, clause);
                    }
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
        private readonly Analyzer _analyzer;
    }
}