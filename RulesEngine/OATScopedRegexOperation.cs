using Microsoft.CST.OAT;
using Microsoft.CST.OAT.Operations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    public class OATScopedRegexOperation : OatOperation
    {
        public OATScopedRegexOperation(Analyzer analyzer) : base(Operation.Custom, analyzer)
        {
            CustomOperation = "ScopedRegex";
            regexEngine = new RegexOperation(analyzer);
            OperationDelegate = ScopedRegexOperationDelegate;
            ValidationDelegate = ScopedRegexValidationDelegate;
        }

        public OperationResult ScopedRegexOperationDelegate(Clause c, object? state1, object? state2, IEnumerable<ClauseCapture>? captures)
        {
            if (state1 is TextContainer tc && c is OATScopedRegexClause src)
            {
                var regexOpts = RegexOptions.Compiled;
                if (src.Arguments.Contains("i"))
                {
                    regexOpts |= RegexOptions.IgnoreCase;
                }
                if (src.Arguments.Contains("m"))
                {
                    regexOpts |= RegexOptions.Multiline;
                }
                var boundaries = new List<Boundary>();
                var target = tc.Target;
                if (Analyzer != null)
                {
                    foreach (var pattern in src.Data.Select(x => regexEngine.StringToRegex(x, regexOpts)))
                    {
                        if (pattern is Regex r)
                        {
                            var matches = r.Matches(target);
                            foreach (var match in matches)
                            {
                                if (match is Match m)
                                {
                                    Boundary translatedBoundary = new Boundary()
                                    {
                                        Length = m.Length,
                                        Index = m.Index + tc.GetLineBoundary(tc.LineNumber).Index
                                    };
                                    // Should return only scoped matches
                                    if (tc.ScopeMatch(src.Scopes, translatedBoundary))
                                    {
                                        boundaries.Add(translatedBoundary);
                                    }
                                }
                            }
                        }

                        var result = src.Invert ? boundaries.Count == 0 : boundaries.Count > 0;
                        return new OperationResult(result, result && src.Capture ? new TypedClauseCapture<List<Boundary>>(c, boundaries, state1) : null);
                    }
                }
            }
            return new OperationResult(false, null);
        }

        public IEnumerable<Violation> ScopedRegexValidationDelegate(CST.OAT.Rule rule, Clause clause)
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
            if (Analyzer is null)
            {
                yield return new Violation($"Rule {rule.Name} Clause {clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)} ScopedRegexClause requires Analyzer be set.", rule, clause);
            }
            if (clause is OATScopedRegexClause src)
            {
                if (!src.Data?.Any() ?? true)
                {
                    yield return new Violation($"Rule {rule.Name} Clause {clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)} Must provide some regexes as data.", rule, clause);
                    yield break;
                }
                foreach (var datum in src.Data ?? new List<string>())
                {
                    if (regexEngine.StringToRegex(datum, RegexOptions.None) is null)
                    {
                        yield return new Violation($"Regex {datum} in Rule {rule.Name} Clause {clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)} is not a valid regex.", rule, clause);
                    }
                }
            }
            else
            {
                yield return new Violation($"Rule {rule.Name ?? "Null Rule Name"} clause {clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)} is not a ScopedRegexClause", rule, clause);
            }
        }

        private RegexOperation regexEngine;
    }
}