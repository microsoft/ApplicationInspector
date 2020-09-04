using Microsoft.CST.OAT;
using Microsoft.CST.OAT.Operations;
using Microsoft.CST.OAT.Utils;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    /// <summary>
    /// The default Regex operation
    /// </summary>
    public class OATRegexWithIndexOperation : OatOperation
    {
        private readonly ConcurrentDictionary<(string, RegexOptions), Regex?> RegexCache = new ConcurrentDictionary<(string, RegexOptions), Regex?>();

        /// <summary>
        /// Create an OatOperation given an analyzer
        /// </summary>
        /// <param name="analyzer">The analyzer context to work with</param>
        public OATRegexWithIndexOperation(Analyzer analyzer) : base(Operation.Custom, analyzer)
        {
            CustomOperation = "RegexWithIndex";
            OperationDelegate = RegexWithIndexOperationDelegate;
            ValidationDelegate = RegexWithIndexValidationDelegate;
        }

        internal IEnumerable<Violation> RegexWithIndexValidationDelegate(CST.OAT.Rule rule, Clause clause)
        {
            if (clause.Data?.Count == null || clause.Data?.Count == 0)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseNoData"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
            }
            else if (clause.Data is List<string> regexList)
            {
                foreach (var regex in regexList)
                {
                    if (!Helpers.IsValidRegex(regex))
                    {
                        yield return new Violation(string.Format(Strings.Get("Err_ClauseInvalidRegex"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), regex), rule, clause);
                    }
                }
            }
            if (clause.DictData != null && clause.DictData?.Count > 0)
            {
                yield return new Violation(string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name, clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), clause.Operation.ToString()), rule, clause);
            }
        }

        internal OperationResult RegexWithIndexOperationDelegate(Clause clause, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            (var stateOneList, _) = Analyzer?.ObjectToValues(state1) ?? (new List<string>(), new List<KeyValuePair<string, string>>());
            (var stateTwoList, _) = Analyzer?.ObjectToValues(state2) ?? (new List<string>(), new List<KeyValuePair<string, string>>());
            if (clause.Data is List<string> RegexList && RegexList.Any())
            {
                var options = RegexOptions.Compiled;

                if (clause.Arguments.Contains("i"))
                {
                    options |= RegexOptions.IgnoreCase;
                }
                if (clause.Arguments.Contains("m"))
                {
                    options |= RegexOptions.Multiline;
                }
                var outmatches = new List<(int, Match)>();

                for (int i = 0; i < RegexList.Count; i++)
                {
                    var regex = StringToRegex(RegexList[i], options);

                    if (regex != null)
                    {
                        foreach (var state in stateOneList)
                        {
                            var matches = regex.Matches(state);

                            if (matches.Count > 0 || (matches.Count == 0 && clause.Invert))
                            {
                                foreach (var match in matches)
                                {
                                    if (match is Match m)
                                    {
                                        outmatches.Add((i, m));
                                    }
                                }
                            }
                        }
                        foreach (var state in stateTwoList)
                        {
                            var matches = regex.Matches(state);

                            if (matches.Count > 0 || (matches.Count == 0 && clause.Invert))
                            {
                                foreach (var match in matches)
                                {
                                    if (match is Match m)
                                    {
                                        outmatches.Add((i, m));
                                    }
                                }
                            }
                        }
                    }
                }
                return new OperationResult(true, !clause.Capture ? null : new TypedClauseCapture<List<(int, Match)>>(clause, outmatches, state1));
            }
            return new OperationResult(false, null);
        }
        /// <summary>
        /// Converts a strings to a compiled regex.
        /// Uses an internal cache.
        /// </summary>
        /// <param name="built">The regex to build</param>
        /// <param name="regexOptions">The options to use.</param>
        /// <returns>The built Regex</returns>
        public Regex? StringToRegex(string built, RegexOptions regexOptions)
        {
            if (!RegexCache.ContainsKey((built, regexOptions)))
            {
                try
                {
                    RegexCache.TryAdd((built, regexOptions), new Regex(built, regexOptions));
                }
                catch (ArgumentException)
                {
                    Log.Warning("InvalidArgumentException when creating regex. Regex {0} is invalid and will be skipped.", built);
                    RegexCache.TryAdd((built, regexOptions), null);
                }
            }
            return RegexCache[(built, regexOptions)];
        }
    }
}