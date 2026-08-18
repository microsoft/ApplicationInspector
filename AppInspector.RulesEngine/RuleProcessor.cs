// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[assembly: InternalsVisibleTo("AppInspector.Tests")]

namespace Microsoft.ApplicationInspector.RulesEngine;

[ExcludeFromCodeCoverage]
public class RuleProcessorOptions
{
    public bool Parallel { get; set; } = true;

    public Confidence ConfidenceFilter { get; set; } =
        Confidence.Unspecified | Confidence.Low | Confidence.Medium | Confidence.High;

    public Severity SeverityFilter { get; set; } =
        Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice;

    public ILoggerFactory? LoggerFactory { get; set; }
    public bool AllowAllTagsInBuildFiles { get; set; }
    public bool EnableCache { get; set; } = true;
    public Languages Languages { get; set; } = new();
}

/// <summary>
///     Heart of RulesEngine. Parses code applies rules
/// </summary>
public class RuleProcessor
{
    private readonly Analyzer _analyzer;
    private readonly ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>> _fileRulesCache = new();
    private readonly ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>> _languageRulesCache = new();
    private readonly Languages _languages;
    private readonly ILogger<RuleProcessor> _logger;

    private readonly RuleProcessorOptions _opts;
    private readonly AbstractRuleSet _ruleset;
    private readonly int MAX_TEXT_SAMPLE_LENGTH = 200; //char bytes
    private IEnumerable<ConvertedOatRule>? _universalRulesCache;

    /// <summary>
    ///     Creates instance of RuleProcessor
    /// </summary>
    public RuleProcessor(AbstractRuleSet rules, RuleProcessorOptions opts)
    {
        _opts = opts;
        _logger = opts.LoggerFactory?.CreateLogger<RuleProcessor>() ?? NullLogger<RuleProcessor>.Instance;
        _languages = opts.Languages;
        _ruleset = rules;
        EnableCache = true;

        _analyzer = new ApplicationInspectorAnalyzer(_opts.LoggerFactory, new AnalyzerOptions(false, opts.Parallel));
    }

    /// <summary>
    ///     Sets severity levels for analysis
    /// </summary>
    private Severity SeverityLevel => _opts.SeverityFilter;

    /// <summary>
    ///     Enables caching of rules queries if multiple reuses per instance
    /// </summary>
    private bool EnableCache { get; }

    private static string ExtractDependency(TextContainer? text, int startIndex, string? pattern, string? language)
    {
        if (text is null || string.IsNullOrEmpty(text.FullContent) || string.IsNullOrEmpty(language) ||
            string.IsNullOrEmpty(pattern))
        {
            return string.Empty;
        }

        var rawResult = string.Empty;
        var endIndex = text.FullContent.IndexOfAny(new[] { '\n', '\r' }, startIndex);
        if (-1 != startIndex && -1 != endIndex)
        {
            rawResult = text.FullContent[startIndex..endIndex].Trim();
            Regex regex = new(pattern);
            var matches = regex.Matches(rawResult);

            //remove surrounding import or trailing comments
            if (matches.Any())
            {
                foreach (Match? match in matches)
                {
                    if (match?.Groups.Count == 1) //handles cases like "using Newtonsoft.Json"
                    {
                        string[] parseValues = match.Groups[0].Value.Split(' ');
                        if (parseValues.Length == 1)
                        {
                            rawResult = parseValues[0].Trim();
                        }
                        else if (parseValues.Length > 1)
                        {
                            rawResult = parseValues[1].Trim();
                        }
                    }
                    else if (match?.Groups.Count > 1) //handles cases like include <stdio.h>
                    {
                        rawResult = match.Groups[1].Value.Trim();
                    }

                    //else if > 2 too hard to match; do nothing
                    break; //only designed to expect one match per line i.e. not include value include value
                }
            }

            var finalResult = rawResult.Replace(";", "");

            return WebUtility.HtmlEncode(finalResult);
        }

        return rawResult;
    }

    /// <summary>
    ///     Analyzes a file and returns a list of <see cref="MatchRecord" />
    /// </summary>
    /// <param name="textContainer">TextContainer which holds the text to analyze</param>
    /// <param name="fileEntry">FileEntry which has the name of the file being analyzed.</param>
    /// <param name="languageInfo">The LanguageInfo for the file</param>
    /// <param name="tagsToIgnore">Ignore rules that match tags that are only in the tags to ignore list</param>
    /// <param name="numLinesContext">
    ///     Number of lines of text to extract for the sample. Set to 0 to disable context gathering.
    ///     Set to -1 to also disable sampling the match.
    /// </param>
    /// <returns>A List of the matches against the Rules the processor is configured with.</returns>
    public List<MatchRecord> AnalyzeFile(TextContainer textContainer, FileEntry fileEntry,
        LanguageInfo languageInfo, IEnumerable<string>? tagsToIgnore = null, int numLinesContext = 3)
    {
        return AnalyzeTextContainer(textContainer, fileEntry, languageInfo, tagsToIgnore, numLinesContext, null);
    }

    /// <summary>
    ///     The single implementation behind every analysis entry point, so the synchronous and asynchronous
    ///     APIs cannot report different results for the same input.
    /// </summary>
    private List<MatchRecord> AnalyzeTextContainer(TextContainer textContainer, FileEntry fileEntry,
        LanguageInfo languageInfo, IEnumerable<string>? tagsToIgnore, int numLinesContext,
        CancellationToken? cancellationToken)
    {
        var rules = GetRulesForFile(languageInfo, fileEntry, tagsToIgnore);
        List<MatchRecord> resultsList = new();

        foreach (var ruleCapture in _analyzer.GetCaptures(rules, textContainer))
        {
            if (cancellationToken?.IsCancellationRequested is true)
            {
                return resultsList;
            }

            if (ruleCapture.Rule is not ConvertedOatRule oatRule)
            {
                continue;
            }

            foreach (var (patternIndex, boundary) in FilterCaptures(oatRule, ruleCapture.Captures))
            {
                if (BuildMatchRecord(oatRule, patternIndex, boundary, textContainer, fileEntry, languageInfo,
                        numLinesContext) is { } newMatch)
                {
                    resultsList.Add(newMatch);
                }
            }
        }

        RemoveOverriddenMatches(resultsList, cancellationToken);

        return resultsList;
    }

    /// <summary>
    ///     Drops findings that a matched overriding rule supersedes. A finding is only superseded when it
    ///     lies entirely within the overriding finding, so an overlapping but wider finding is kept.
    /// </summary>
    private static void RemoveOverriddenMatches(List<MatchRecord> matches,
        CancellationToken? cancellationToken = null)
    {
        HashSet<MatchRecord> removes = new();

        foreach (var overridingMatch in matches.Where(x => x.Rule?.Overrides?.Count > 0))
        {
            if (cancellationToken?.IsCancellationRequested is true)
            {
                return;
            }

            foreach (var idToOverride in overridingMatch.Rule?.Overrides ?? Array.Empty<string>())
            foreach (var overriddenMatch in matches.FindAll(x => x.Rule?.Id == idToOverride))
            {
                if (overriddenMatch.Boundary.Index >= overridingMatch.Boundary.Index &&
                    overriddenMatch.Boundary.Index + overriddenMatch.Boundary.Length <=
                    overridingMatch.Boundary.Index + overridingMatch.Boundary.Length)
                {
                    removes.Add(overriddenMatch);
                }
            }
        }

        matches.RemoveAll(x => removes.Contains(x));
    }

    /// <summary>
    ///     Reduces a rule's captures to the findings that should be reported.
    ///     Conditions produce "gates": the subset of pattern matches that satisfied that condition. A rule level
    ///     condition gates every match; a pattern level condition gates only the matches of its own pattern. A
    ///     match survives if it is in every gate that applies to it.
    /// </summary>
    private List<(int, Boundary)> FilterCaptures(ConvertedOatRule oatRule, List<ClauseCapture> captures)
    {
        // Gate membership assumes conditions are ANDed, which an authored expression need not be.
        if (!string.IsNullOrWhiteSpace(oatRule.AppInspectorRule.Expression))
        {
            return FilterCapturesByExpression(oatRule, captures);
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
    private List<(int, Boundary)> FilterCapturesByExpression(ConvertedOatRule oatRule,
        List<ClauseCapture> captures)
    {
        if (oatRule.ParsedExpression is not { } expression)
        {
            // Verification rejects these, so reaching here means the rule set was not verified.
            _logger.LogError(
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

    /// <summary>
    ///     Builds the <see cref="MatchRecord" /> for one finding, or returns null if the finding is filtered out.
    /// </summary>
    private MatchRecord? BuildMatchRecord(ConvertedOatRule oatRule, int patternIndex, Boundary boundary,
        TextContainer textContainer, FileEntry fileEntry, LanguageInfo languageInfo, int numLinesContext)
    {
        // Universal rules can reach build files incidentally, so suppress their non-Metadata tags by default.
        if (!_opts.AllowAllTagsInBuildFiles &&
            languageInfo.Type == LanguageInfo.LangFileType.Build &&
            oatRule.AppInspectorRule.IsUniversal &&
            (oatRule.AppInspectorRule.Tags?.Any(v => !v.Contains("Metadata")) ?? false))
        {
            return null;
        }

        if (patternIndex < 0 || patternIndex >= oatRule.AppInspectorRule.Patterns.Length)
        {
            _logger.LogError("Index out of range for patterns for rule: {ruleName}", oatRule.AppInspectorRule.Name);
            return null;
        }

        if (!_opts.ConfidenceFilter.HasFlag(oatRule.AppInspectorRule.Patterns[patternIndex].Confidence))
        {
            return null;
        }

        var startLocation = textContainer.GetLocation(boundary.Index);
        var endLocation = textContainer.GetLocation(boundary.Index + boundary.Length);
        MatchRecord newMatch = new(oatRule.AppInspectorRule)
        {
            FileName = fileEntry.FullPath,
            FullTextContainer = textContainer,
            LanguageInfo = languageInfo,
            Boundary = boundary,
            StartLocationLine = startLocation.Line,
            StartLocationColumn = startLocation.Column,
            EndLocationLine =
                endLocation.Line != 0 ? endLocation.Line : startLocation.Line + 1, //match is on last line
            EndLocationColumn = endLocation.Column,
            MatchingPattern = oatRule.AppInspectorRule.Patterns[patternIndex],
            Excerpt = numLinesContext > 0
                ? ExtractExcerpt(textContainer, startLocation, endLocation, boundary, numLinesContext)
                : string.Empty,
            Sample = numLinesContext > -1
                ? ExtractTextSample(textContainer.FullContent, boundary.Index, boundary.Length)
                : string.Empty
        };

        if (oatRule.AppInspectorRule.Tags?.Contains("Dependency.SourceInclude") ?? false)
        {
            newMatch.Sample = ExtractDependency(newMatch.FullTextContainer, newMatch.Boundary.Index,
                newMatch.Pattern, newMatch.LanguageInfo.Name);
        }

        return newMatch;
    }

    /// <summary>
    ///     Analyzes a file and returns a list of <see cref="MatchRecord" />
    /// </summary>
    /// <param name="contents">A string containing the text to analyze</param>
    /// <param name="fileEntry">FileEntry which has the name of the file being analyzed</param>
    /// <param name="languageInfo">The LanguageInfo for the file</param>
    /// <param name="tagsToIgnore">Ignore rules that match tags that are only in the tags to ignore list</param>
    /// <param name="numLinesContext">
    ///     Number of lines of text to extract for the sample. Set to 0 to disable context gathering.
    ///     Set to -1 to also disable sampling the match.
    /// </param>
    /// <returns>A List of the matches against the Rules the processor is configured with.</returns>
    public List<MatchRecord> AnalyzeFile(string contents, FileEntry fileEntry, LanguageInfo languageInfo,
        IEnumerable<string>? tagsToIgnore = null, int numLinesContext = 3)
    {
        TextContainer textContainer = new(contents, languageInfo.Name, _languages,
            _opts.LoggerFactory ?? NullLoggerFactory.Instance, fileEntry.FullPath);
        return AnalyzeFile(textContainer, fileEntry, languageInfo, tagsToIgnore, numLinesContext);
    }

    /// <summary>
    ///     Get the Rules which apply to the FileName of the FileEntry provided.
    /// </summary>
    /// <param name="languageInfo"></param>
    /// <param name="fileEntry"></param>
    /// <param name="tagsToIgnore"></param>
    /// <returns></returns>
    public IEnumerable<ConvertedOatRule> GetRulesForFile(LanguageInfo languageInfo, FileEntry fileEntry,
        IEnumerable<string>? tagsToIgnore)
    {
        return GetRulesByLanguage(languageInfo.Name)
            .Concat(GetRulesByFileName(fileEntry.FullPath))
            .Concat(GetUniversalRules())
                .Where(x => !x.AppInspectorRule.DoesNotApplyTo?.Contains(languageInfo.Name) ?? true)
                .Where(x => !x.AppInspectorRule.Tags?.Any(y => tagsToIgnore?.Contains(y) ?? false) ?? true)
                .Where(x => !x.AppInspectorRule.Disabled && SeverityLevel.HasFlag(x.AppInspectorRule.Severity));
    }

    /// <summary>
    ///     Analyzes a file and returns a list of <see cref="MatchRecord" />
    /// </summary>
    /// <param name="fileEntry">
    ///     FileEntry which holds the name of the file being analyzed as well as a Stream containing the
    ///     contents to analyze
    /// </param>
    /// <param name="languageInfo">The LanguageInfo for the file</param>
    /// <param name="tagsToIgnore">Ignore rules that match tags that are only in the tags to ignore list</param>
    /// <param name="numLinesContext">
    ///     Number of lines of text to extract for the sample. Set to 0 to disable context gathering.
    ///     Set to -1 to also disable sampling the match.
    /// </param>
    /// <returns>A List of the matches against the Rules the processor is configured with.</returns>
    public List<MatchRecord> AnalyzeFile(FileEntry fileEntry, LanguageInfo languageInfo,
        IEnumerable<string>? tagsToIgnore = null, int numLinesContext = 3)
    {
        using var sr = new StreamReader(fileEntry.Content);
        var contents = string.Empty;
        try
        {
            contents = sr.ReadToEnd();
        }
        catch (Exception e)
        {
            // Analysis continues on empty content, so this must not be silent.
            _logger.LogError("Failed to read {path} for analysis, it will be treated as empty. {type}:{message}",
                fileEntry.FullPath, e.GetType(), e.Message);
        }

        return AnalyzeFile(contents, fileEntry, languageInfo, tagsToIgnore, numLinesContext);
    }

    /// <summary>
    ///     Analyzes a file and returns a list of <see cref="MatchRecord" />
    /// </summary>
    /// <param name="fileEntry">
    ///     FileEntry which holds the name of the file being analyzed as well as a Stream containing the
    ///     contents to analyze
    /// </param>
    /// <param name="languageInfo">The LanguageInfo for the file</param>
    /// <param name="cancellationToken">Token to abort the analysis</param>
    /// <param name="tagsToIgnore">Ignore rules that match tags that are only in the tags to ignore list</param>
    /// <param name="numLinesContext">
    ///     Number of lines of text to extract for the sample. Set to 0 to disable context gathering.
    ///     Set to -1 to also disable sampling the match.
    /// </param>
    /// <returns>A List of the matches against the Rules the processor is configured with.</returns>
    public async Task<List<MatchRecord>> AnalyzeFileAsync(FileEntry fileEntry, LanguageInfo languageInfo,
        CancellationToken? cancellationToken = null, IEnumerable<string>? tagsToIgnore = null, int numLinesContext = 3)
    {
        using var sr = new StreamReader(fileEntry.Content);
        var contents = string.Empty;
        try
        {
            contents = await sr.ReadToEndAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // Analysis continues on empty content, so this must not be silent.
            _logger.LogError("Failed to read {path} for analysis, it will be treated as empty. {type}:{message}",
                fileEntry.FullPath, e.GetType(), e.Message);
        }

        TextContainer textContainer = new(contents, languageInfo.Name, _languages,
            _opts.LoggerFactory ?? NullLoggerFactory.Instance, fileEntry.FullPath);

        return AnalyzeTextContainer(textContainer, fileEntry, languageInfo, tagsToIgnore, numLinesContext,
            cancellationToken);
    }


    /// <summary>
    ///     Filters the rules for those matching the specified language.
    /// </summary>
    /// <param name="language"> Language to filter rules for </param>
    /// <returns> List of rules </returns>
    private IEnumerable<ConvertedOatRule> GetRulesByLanguage(string language)
    {
        if (EnableCache)
        {
            if (_languageRulesCache.ContainsKey(language))
            {
                return _languageRulesCache[language];
            }
        }

        IEnumerable<ConvertedOatRule> filteredRules = _ruleset.ByLanguage(language).ToArray();

        if (EnableCache)
        {
            _languageRulesCache.TryAdd(language, filteredRules);
        }

        return filteredRules;
    }

    /// <summary>
    ///     Get all rules that apply to all files.
    /// </summary>
    /// <returns> List of rules </returns>
    private IEnumerable<ConvertedOatRule> GetUniversalRules()
    {
        if (_universalRulesCache is null)
        {
            if (EnableCache)
            {
                _universalRulesCache = _ruleset.GetUniversalRules();
            }
            else
            {
                return _ruleset.GetUniversalRules();
            }
        }

        return _universalRulesCache;
    }

    /// <summary>
    ///     Filters the rules for those matching the filename.
    /// </summary>
    /// <param name="fileName"> Filename to filter for</param>
    /// <returns> List of rules </returns>
    private IEnumerable<ConvertedOatRule> GetRulesByFileName(string fileName)
    {
        if (EnableCache)
        {
            if (_fileRulesCache.ContainsKey(fileName))
            {
                return _fileRulesCache[fileName];
            }
        }

        IEnumerable<ConvertedOatRule> filteredRules = _ruleset.ByFilename(fileName).ToArray();

        if (EnableCache)
        {
            _fileRulesCache.TryAdd(fileName, filteredRules);
        }

        return filteredRules;
    }

    /// <summary>
    ///     Simple wrapper but keeps calling code consistent
    ///     Do not html code result which is accomplished later before out put to report
    /// </summary>
    internal string ExtractTextSample(string fileText, int index, int length)
    {
        if (index < 0 || length < 0)
        {
            return fileText;
        }

        length = Math.Min(Math.Min(length, MAX_TEXT_SAMPLE_LENGTH), fileText.Length - index);

        if (length == 0)
        {
            return string.Empty;
        }

        return fileText[index..(index + length)].Trim();
    }

    /// <summary>
    ///     Located here to include during Match creation to avoid a call later or putting in constructor
    ///     Needed in match ensuring value exists at time of report writing rather than expecting a callback
    ///     from the template
    /// </summary>
    /// <returns></returns>
    internal static string ExtractExcerpt(TextContainer text, Location start, Location end, Boundary matchBoundary, int context = 3)
    {
        if (context == 0)
        {
            return string.Empty;
        }

        int startLineNumber =
            start.Line < 0 ? 0 : start.Line > text.LineEnds.Count ? text.LineEnds.Count - 1 : start.Line;
        int endLineNumber =
            end.Line < 0 ? 0 : end.Line > text.LineEnds.Count ? text.LineEnds.Count - 1 : end.Line;
        // First we try to include the number of lines of context requested
        var excerptStartLine = Math.Max(0, startLineNumber - context);
        var excerptEndLine = Math.Min(text.LineEnds.Count - 1, endLineNumber + context);
        var startIndex = text.LineStarts[excerptStartLine];
        var endIndex = text.LineEnds[excerptEndLine] + 1;
        // Maximum number of characters to capture on each side
        var maxCharacterContext = context * 100;
        // If the number of characters captured for context is larger than 100*number of lines,
        //  instead gather an appropriate number of characters
        if (endIndex - startIndex - matchBoundary.Length > maxCharacterContext * 2)
        {
            // Limit start index to 0
            startIndex = Math.Max(0, matchBoundary.Index - maxCharacterContext);
            // Limit end index to length of overall content
            endIndex = Math.Min(text.FullContent.Length, Math.Max(0, matchBoundary.Index + matchBoundary.Length + maxCharacterContext));
        }

        return text.FullContent[startIndex..endIndex];
    }
}