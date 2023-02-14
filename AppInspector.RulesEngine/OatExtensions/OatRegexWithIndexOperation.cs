using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CST.OAT;
using Microsoft.CST.OAT.Operations;
using Microsoft.CST.OAT.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.ApplicationInspector.RulesEngine.OatExtensions;

/// <summary>
///     The Custom Operation to enable identification of pattern index in result used by Application Inspector to report
///     why a given
///     result was matched and to retrieve other pattern level meta-data
/// </summary>
public class OatRegexWithIndexOperation : OatOperation
{
    private readonly ILogger<OatRegexWithIndexOperation> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<(string, RegexOptions), Regex?> RegexCache = new();

    /// <summary>
    ///     Create an OatOperation given an analyzer
    /// </summary>
    /// <param name="analyzer">The analyzer context to work with</param>
    /// <param name="loggerFactory">Logger Factory to use</param>
    public OatRegexWithIndexOperation(Analyzer analyzer, ILoggerFactory? loggerFactory = null) : base(Operation.Custom,
        analyzer)
    {
        _loggerFactory = loggerFactory ?? new NullLoggerFactory();
        _logger = _loggerFactory.CreateLogger<OatRegexWithIndexOperation>();
        CustomOperation = "RegexWithIndex";
        OperationDelegate = RegexWithIndexOperationDelegate;
        ValidationDelegate = RegexWithIndexValidationDelegate;
    }

    private static IEnumerable<Violation> RegexWithIndexValidationDelegate(CST.OAT.Rule rule, Clause clause)
    {
        if (clause.Data?.Count is null or 0)
        {
            yield return new Violation(
                string.Format(Strings.Get("Err_ClauseNoData"), rule.Name,
                    clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture)), rule, clause);
        }
        else if (clause.Data is List<string> regexList)
        {
            foreach (var regex in regexList)
                if (!Helpers.IsValidRegex(regex))
                {
                    yield return new Violation(
                        string.Format(Strings.Get("Err_ClauseInvalidRegex"), rule.Name,
                            clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture), regex),
                        rule, clause);
                }
        }

        if (clause.DictData?.Count > 0)
        {
            yield return new Violation(
                string.Format(Strings.Get("Err_ClauseDictDataUnexpected"), rule.Name,
                    clause.Label ?? rule.Clauses.IndexOf(clause).ToString(CultureInfo.InvariantCulture),
                    clause.Operation.ToString()), rule, clause);
        }
    }

    /// <summary>
    ///     Returns results with pattern index and Boundary as a tuple to enable retrieval of Rule pattern level meta-data like
    ///     Confidence and report the
    ///     pattern that was responsible for the match
    /// </summary>
    /// <param name="clause"></param>
    /// <param name="state1"></param>
    /// <param name="state2"></param>
    /// <param name="captures"></param>
    /// <returns></returns>
    private OperationResult RegexWithIndexOperationDelegate(Clause clause, object? state1, object? state2,
        IEnumerable<ClauseCapture>? captures)
    {
        if (state1 is TextContainer tc && clause is OatRegexWithIndexClause src &&
            clause.Data is List<string> RegexList && RegexList.Count > 0)
        {
            RegexOptions regexOpts = new();
            var subBoundary = state2 is Boundary s2 ? s2 : null;

            if (src.Arguments.Contains("i"))
            {
                regexOpts |= RegexOptions.IgnoreCase;
            }

            if (src.Arguments.Contains("m"))
            {
                regexOpts |= RegexOptions.Multiline;
            }

#if NET7_0_OR_GREATER
            if (src.Arguments.Contains("nb"))
            {

                regexOpts |= RegexOptions.NonBacktracking;
            }
#endif

            List<(int, Boundary)> outmatches = new(); //tuple results i.e. pattern index and where

            if (Analyzer != null)
            {
                foreach (var regexString in RegexList)
                    if (StringToRegex(regexString, regexOpts) is { } regex)
                    {
                        if (src.XPaths is not null)
                        {
                            foreach (var xmlPath in src.XPaths)
                            {
                                var targets = tc.GetStringFromXPath(xmlPath);
                                foreach (var target in targets)
                                {
                                    var matches = GetMatches(regex, tc, clause, src.Scopes, target.Item2);
                                    outmatches.AddRange(matches);
                                }
                            }
                        }

                        if (src.JsonPaths is not null)
                        {
                            foreach (var jsonPath in src.JsonPaths)
                            {
                                var targets = tc.GetStringFromJsonPath(jsonPath);
                                foreach (var target in targets)
                                    outmatches.AddRange(GetMatches(regex, tc, clause, src.Scopes, target.Item2));
                            }
                        }

                        if (src.YmlPaths is not null)
                        {
                            foreach (var ymlPath in src.YmlPaths)
                            {
                                var targets = tc.GetStringFromYmlPath(ymlPath).ToList();
                                foreach (var target in targets)
                                {
                                    outmatches.AddRange(GetMatches(regex, tc, clause, src.Scopes, target.Item2));
                                }
                            }
                        }

                        if (src.JsonPaths is null && src.XPaths is null && src.YmlPaths is null)
                        {
                            if (subBoundary is not null)
                            {
                                outmatches.AddRange(GetMatches(regex, tc, clause, src.Scopes, subBoundary));
                            }
                            else
                            {
                                outmatches.AddRange(GetMatches(regex, tc, clause, src.Scopes, null));
                            }
                        }
                    }

                var result = src.Invert ? outmatches.Count == 0 : outmatches.Count > 0;
                return new OperationResult(result,
                    result && src.Capture
                        ? new TypedClauseCapture<List<(int, Boundary)>>(clause, outmatches, state1)
                        : null);
            }
        }

        return new OperationResult(false);
    }

    private IEnumerable<(int, Boundary)> GetMatches(Regex regex, TextContainer tc, Clause clause, PatternScope[] scopes,
        Boundary? boundary)
    {
        foreach (var match in regex.Matches(boundary is null ? tc.FullContent : tc.GetBoundaryText(boundary)))
            if (match is Match m)
            {
                Boundary translatedBoundary = new()
                {
                    Length = m.Length,
                    Index = m.Index + (boundary?.Index ?? 0)
                };

                //regex patterns will be indexed off data while string patterns result in N clauses
                var patternIndex = Convert.ToInt32(clause.Label);

                // Should return only scoped matches
                if (tc.ScopeMatch(scopes, translatedBoundary))
                {
                    yield return (patternIndex, translatedBoundary);
                }
            }
    }

    /// <summary>
    ///     Converts a strings to a compiled regex.
    ///     Uses an internal cache.
    /// </summary>
    /// <param name="built">The regex to build</param>
    /// <param name="regexOptions">The options to use.</param>
    /// <returns>The built Regex</returns>
    private Regex? StringToRegex(string built, RegexOptions regexOptions)
    {
        if (!RegexCache.ContainsKey((built, regexOptions)))
        {
            try
            {
                RegexCache.TryAdd((built, regexOptions), new Regex(built, regexOptions));
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Provided regex {Regex} was not valid and could not be used", built);
                RegexCache.TryAdd((built, regexOptions), null);
            }
        }

        return RegexCache[(built, regexOptions)];
    }
}