using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CST.OAT;
using Microsoft.CST.OAT.Operations;
using Microsoft.CST.OAT.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions
{
    /// <summary>
    /// The Custom Operation to enable identification of pattern index in result used by Application Inspector to report why a given
    /// result was matched and to retrieve other pattern level meta-data
    /// </summary>
    public class OatSubstringIndexOperation : OatOperation
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Create an OatOperation given an analyzer
        /// </summary>
        /// <param name="analyzer">The analyzer context to work with</param>
        /// <param name="loggerFactory">LoggerFactory to use</param>
        public OatSubstringIndexOperation(Analyzer analyzer, ILoggerFactory? loggerFactory = null) : base(Operation.Custom, analyzer)
        {
            _loggerFactory = loggerFactory ?? new NullLoggerFactory();
            CustomOperation = "SubstringIndex";
            OperationDelegate = SubstringIndexOperationDelegate;
            ValidationDelegate = SubstringIndexValidationDelegate;
        }
        
        public static IEnumerable<Violation> SubstringIndexValidationDelegate(CST.OAT.Rule rule, Clause clause)
        {
            if (clause.Data?.Count is null or 0)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseNoData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
            if (clause.DictData?.Count is not null && clause.DictData.Count > 0)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
            }
        }

        /// <summary>
        /// Returns results with pattern index and Boundary as a tuple to enable retrieval of Rule pattern level meta-data like Confidence and report the
        /// pattern that was responsible for the match
        /// </summary>
        /// <param name="clause"></param>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <param name="captures"></param>
        /// <returns></returns>
        private OperationResult SubstringIndexOperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            var comparisonType = clause.Arguments.Contains("i") ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            if (state1 is TextContainer tc && clause is OatSubstringIndexClause src)
            {
                if (clause.Data is { Count: > 0 } stringList)
                {
                    var outmatches = new List<(int, Boundary)>();//tuple results i.e. pattern index and where

                    for (int i = 0; i < stringList.Count; i++)
                    {
                        if (src.XPaths is not null)
                        {
                            foreach (var xmlPath in src.XPaths)
                            {
                                var targets = tc.GetStringFromXPath(xmlPath);
                                foreach (var target in targets)
                                {
                                    var matches = GetMatches(target.Item1, stringList[i], comparisonType, tc, src);
                                    foreach (var match in matches)
                                    {
                                        match.Index += target.Item2.Index;
                                        outmatches.Add((i,match));
                                    }
                                }
                            }
                        }
                        if (src.JsonPaths is not null)
                        {
                            foreach (var jsonPath in src.JsonPaths)
                            {
                                var targets = tc.GetStringFromJsonPath(jsonPath);
                                foreach (var target in targets)
                                {
                                    var matches = GetMatches(target.Item1, stringList[i], comparisonType, tc, src);
                                    foreach (var match in matches)
                                    {
                                        match.Index += target.Item2.Index;
                                        outmatches.Add((i,match));
                                    }
                                }
                            }
                        }
                        if (src.JsonPaths is null && src.XPaths is null)
                        {
                            var matches = GetMatches(tc.FullContent, stringList[i], comparisonType, tc, src);
                            outmatches.AddRange(matches.Select(x => (i, x)));
                        }
                    }

                    var result = src.Invert ? outmatches.Count == 0 : outmatches.Count > 0;
                    return new OperationResult(result, result && src.Capture ? new TypedClauseCapture<List<(int, Boundary)>>(clause, outmatches, state1) : null);
                }
            }
            return new OperationResult(false, null);
        }

        private static IEnumerable<Boundary> GetMatches(string target, string query, StringComparison comparisonType, TextContainer tc, OatSubstringIndexClause src)
        {
            var idx = target.IndexOf(query, comparisonType);
            while (idx != -1)
            {
                bool skip = false;
                if (src.UseWordBoundaries)
                {
                    if (idx > 0 && char.IsLetterOrDigit(target[idx - 1]))
                    {
                        skip = true;
                    }
                    if (idx + query.Length < target.Length && char.IsLetterOrDigit(target[idx + query.Length]))
                    {
                        skip = true;
                    }
                }
                if (!skip)
                {
                    Boundary newBoundary = new()
                    {
                        Length = query.Length,
                        Index = idx
                    };
                    if (tc.ScopeMatch(src.Scopes, newBoundary))
                    {
                        yield return newBoundary;
                    }
                }
                idx = target.IndexOf(query, idx + query.Length, comparisonType);
            }
        }
    }
}