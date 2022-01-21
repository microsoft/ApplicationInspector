namespace Microsoft.ApplicationInspector.RulesEngine
{
    using Microsoft.CST.OAT;
    using Microsoft.CST.OAT.Operations;
    using Microsoft.CST.OAT.Utils;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// The Custom Operation to enable identification of pattern index in result used by Application Inspector to report why a given
    /// result was matched and to retrieve other pattern level meta-data
    /// </summary>
    public class OATSubstringIndexOperation : OatOperation
    {
        /// <summary>
        /// Create an OatOperation given an analyzer
        /// </summary>
        /// <param name="analyzer">The analyzer context to work with</param>
        public OATSubstringIndexOperation(Analyzer analyzer) : base(Operation.Custom, analyzer)
        {
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
        public static OperationResult SubstringIndexOperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            var comparisonType = clause.Arguments.Contains("i") ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            if (state1 is TextContainer tc && clause is OATSubstringIndexClause src)
            {
                if (clause.Data is List<string> stringList && stringList.Count > 0)
                {
                    var outmatches = new List<(int, Boundary)>();//tuple results i.e. pattern index and where

                    for (int i = 0; i < stringList.Count; i++)
                    {
                        var idx = tc.FullContent.IndexOf(stringList[i], comparisonType);
                        while (idx != -1)
                        {
                            bool skip = false;
                            if (src.UseWordBoundaries)
                            {
                                if (idx > 0 && char.IsLetterOrDigit(tc.FullContent[idx - 1]))
                                {
                                    skip = true;
                                }
                                if (idx + stringList[i].Length < tc.FullContent.Length && char.IsLetterOrDigit(tc.FullContent[idx + stringList[i].Length]))
                                {
                                    skip = true;
                                }
                            }
                            if (!skip)
                            {
                                Boundary newBoundary = new()
                                {
                                    Length = stringList[i].Length,
                                    Index = idx
                                };
                                if (tc.ScopeMatch(src.Scopes, newBoundary))
                                {
                                    outmatches.Add((i, newBoundary));
                                }
                            }
                            idx = tc.FullContent.IndexOf(stringList[i], idx + stringList[i].Length, comparisonType);
                        }
                    }

                    var result = src.Invert ? outmatches.Count == 0 : outmatches.Count > 0;
                    return new OperationResult(result, result && src.Capture ? new TypedClauseCapture<List<(int, Boundary)>>(clause, outmatches, state1) : null);
                }
            }
            return new OperationResult(false, null);
        }
    }
}