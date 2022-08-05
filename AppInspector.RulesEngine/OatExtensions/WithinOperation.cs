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
                List<(int, Boundary)> toRemove = new();
                if (captures is IEnumerable<TypedClauseCapture<List<(int, Boundary)>>> castCaptures)
                {
                    foreach (var tcc in castCaptures)
                    {
                        foreach ((int clauseNum, Boundary capture) in tcc.Result)
                        {
                            if (wc.FindingOnly)
                            {
                                if (!ProcessLambda(tc.GetBoundaryText(capture)))
                                {
                                    toRemove.Add((clauseNum, capture));
                                }
                            }
                            else if (wc.SameLineOnly)
                            {
                                var start = tc.LineStarts[tc.GetLocation(capture.Index).Line];
                                var end = tc.LineEnds[tc.GetLocation(start + capture.Length).Line];
                                if(!ProcessLambda(tc.FullContent[start..end]))
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
                                if (!ProcessLambda(tc.FullContent[start..(end + 1)]))
                                {
                                    toRemove.Add((clauseNum, capture));
                                }
                            }
                            else if (wc.SameFile)
                            {
                                var start = tc.LineStarts[0];
                                var end = tc.LineEnds[^1];
                                if (!ProcessLambda(tc.FullContent[start..end]))
                                {
                                    toRemove.Add((clauseNum, capture));
                                }
                            }
                            else if (wc.OnlyBefore)
                            {
                                var start = tc.LineStarts[0];
                                var end = capture.Index;
                                if(!ProcessLambda(tc.FullContent[start..end]))
                                {
                                    toRemove.Add((clauseNum, capture));
                                }
                            }
                            else if (wc.OnlyAfter)
                            {
                                var start = capture.Index + capture.Length;
                                var end = tc.LineEnds[^1];
                                if (!ProcessLambda(tc.FullContent[start..end]))
                                {
                                    toRemove.Add((clauseNum, capture));
                                }
                            }
                        }
                    }
                    
                    var passed = castCaptures.SelectMany(x => x.Result)
                        .Except(toRemove)
                        .Select(x => x.Item2).ToList();

                    return new OperationResult(passed.Any() ^ wc.Invert, passed.Any() ? new TypedClauseCapture<List<Boundary>>(wc, passed) : null);
                }
  

                bool ProcessLambda(string target)
                {
                    return _analyzer.AnalyzeClause(wc.SubClause,
                        new TextContainer(target, tc.Language, tc.Languages,
                            _loggerFactory.CreateLogger<TextContainer>()));
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