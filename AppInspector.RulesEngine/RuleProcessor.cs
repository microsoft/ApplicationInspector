// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInspector.RulesEngine.OatExtensions;
using Microsoft.CST.OAT;
using Microsoft.CST.RecursiveExtractor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        var rules = GetRulesForFile(languageInfo, fileEntry, tagsToIgnore);
        List<MatchRecord> resultsList = new();

        var caps = _analyzer.GetCaptures(rules, textContainer);
        foreach (var ruleCapture in caps)
        {
            var netCaptures = FilterCaptures(ruleCapture.Captures);
            var oatRule = ruleCapture.Rule as ConvertedOatRule;
            foreach (var match in netCaptures)
            {
                var patternIndex = match.Item1;
                var boundary = match.Item2;
                //restrict adds from build files to tags with "metadata" only to avoid false feature positives that are not part of executable code
                if (!_opts.AllowAllTagsInBuildFiles && languageInfo.Type == LanguageInfo.LangFileType.Build &&
                    (oatRule.AppInspectorRule.Tags?.Any(v => !v.Contains("Metadata")) ?? false))
                {
                    continue;
                }

                if (!_opts.ConfidenceFilter.HasFlag(oatRule.AppInspectorRule.Patterns[patternIndex].Confidence))
                {
                    continue;
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

                resultsList.Add(newMatch);
            }

            // If a WithinClause capture is present, use only within captures, otherwise just flattens the list of results from the non-within clause.
            List<(int, Boundary)> FilterCaptures(List<ClauseCapture> captures)
            {
                // If we had a WithinClause we only want the captures that passed the within filter.
                if (captures.Any(x => x.Clause is WithinClause))
                {
                    var onlyWithinCaptures = captures.Where(x => x.Clause is WithinClause)
                        .Cast<TypedClauseCapture<List<(int, Boundary)>>>().ToList();
                    var allCaptured = onlyWithinCaptures.SelectMany(x => x.Result);
                    ConcurrentDictionary<(int, Boundary), int> numberOfInstances = new();
                    // If there are multiple within clauses ensure that we only return matches which passed all clauses
                    // WithinClauses are always ANDed, but each contains all the captures that passed *that* clause.
                    // We need the captures that passed every clause.
                    foreach (var aCapture in allCaptured)
                    {
                        numberOfInstances.AddOrUpdate(aCapture, 1, (tuple, i) => i + 1);
                    }
                    return numberOfInstances.Where(x => x.Value == onlyWithinCaptures.Count).Select(x => x.Key)
                        .ToList();
                }

                var outList = new List<(int, Boundary)>();
                foreach (var cap in captures)
                    if (cap is TypedClauseCapture<List<(int, Boundary)>> tcc)
                    {
                        outList.AddRange(tcc.Result);
                    }

                return outList;
            }
        }

        List<MatchRecord> removes = new();

        foreach (var m in resultsList.Where(x => x.Rule?.Overrides?.Count > 0))
        {
            foreach (var idsToOverride in m.Rule?.Overrides ?? Array.Empty<string>())
            {
                // Find all overriden rules and mark them for removal from issues list
                foreach (var om in resultsList.FindAll(x => x.Rule?.Id == idsToOverride))
                {
                    // If the overridden match is a subset of the overriding match
                    if (om.Boundary.Index >= m.Boundary.Index &&
                        om.Boundary.Index <= m.Boundary.Index + m.Boundary.Length)
                    {
                        removes.Add(om);
                    }
                }
            }
        }

        // Remove overriden rules
        resultsList.RemoveAll(x => removes.Contains(x));

        return resultsList;
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
            _logger.LogDebug("Failed to analyze file {path}. {type}:{message}. ({stackTrace}), fileRecord.FileName",
                fileEntry.FullPath, e.GetType(), e.Message, e.StackTrace);
        }

        return AnalyzeFile(contents, fileEntry, languageInfo, tagsToIgnore, numLinesContext);
    }

    public async Task<List<MatchRecord>> AnalyzeFileAsync(FileEntry fileEntry, LanguageInfo languageInfo,
        CancellationToken? cancellationToken = null, IEnumerable<string>? tagsToIgnore = null, int numLinesContext = 3)
    {
        var rules = GetRulesForFile(languageInfo, fileEntry, tagsToIgnore);

        List<MatchRecord> resultsList = new();

        using var sr = new StreamReader(fileEntry.Content);

        TextContainer textContainer = new(await sr.ReadToEndAsync().ConfigureAwait(false), languageInfo.Name,
            _languages, _opts.LoggerFactory ?? NullLoggerFactory.Instance, fileEntry.FullPath);
        foreach (var ruleCapture in _analyzer.GetCaptures(rules, textContainer))
        {
            // If we had a WithinClause we only want the captures that passed the within filter.
            var filteredCaptures = ruleCapture.Captures.Any(x => x.Clause is WithinClause)
                ? ruleCapture.Captures.Where(x => x.Clause is WithinClause)
                : ruleCapture.Captures;
            if (cancellationToken?.IsCancellationRequested is true)
            {
                return resultsList;
            }

            foreach (var cap in filteredCaptures) resultsList.AddRange(ProcessBoundary(cap));

            List<MatchRecord> ProcessBoundary(ClauseCapture cap)
            {
                List<MatchRecord> newMatches = new(); //matches for this rule clause only

                if (cap is TypedClauseCapture<List<(int, Boundary)>> tcc)
                {
                    if (ruleCapture.Rule is ConvertedOatRule oatRule)
                    {
                        if (tcc.Result is { } captureResults)
                        {
                            foreach (var match in captureResults)
                            {
                                var patternIndex = match.Item1;
                                var boundary = match.Item2;

                                //restrict adds from build files to tags with "metadata" only to avoid false feature positives that are not part of executable code
                                if (!_opts.AllowAllTagsInBuildFiles &&
                                    languageInfo.Type == LanguageInfo.LangFileType.Build &&
                                    (oatRule.AppInspectorRule.Tags?.Any(v => !v.Contains("Metadata")) ?? false))
                                {
                                    continue;
                                }

                                if (patternIndex < 0 || patternIndex > oatRule.AppInspectorRule.Patterns.Length)
                                {
                                    _logger.LogError("Index out of range for patterns for rule: {ruleName}",
                                        oatRule.AppInspectorRule.Name);
                                    continue;
                                }

                                if (!_opts.ConfidenceFilter.HasFlag(oatRule.AppInspectorRule.Patterns[patternIndex]
                                        .Confidence))
                                {
                                    continue;
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
                                    EndLocationLine =
                                        endLocation.Line != 0
                                            ? endLocation.Line
                                            : startLocation.Line + 1, //match is on last line
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
                                    newMatch.Sample = ExtractDependency(newMatch.FullTextContainer,
                                        newMatch.Boundary.Index, newMatch.Pattern, newMatch.LanguageInfo.Name);
                                }

                                newMatches.Add(newMatch);
                            }
                        }
                    }
                }

                return newMatches;
            }
        }

        List<MatchRecord> removes = new();

        foreach (var matchRecord in resultsList.Where(x => x.Rule?.Overrides?.Count > 0))
        {
            if (cancellationToken?.IsCancellationRequested is true)
            {
                return resultsList;
            }

            foreach (var idToOverride in matchRecord.Rule?.Overrides ?? Array.Empty<string>())
            {
                // Find all overriden rules and mark them for removal from issues list
                foreach (var potentialOverriddenMatch in resultsList.FindAll(x => x.Rule?.Id == idToOverride))
                {
                    // Start after or matching start
                    if (potentialOverriddenMatch.Boundary.Index >= matchRecord.Boundary.Index &&
                        // End before or matching end
                        (potentialOverriddenMatch.Boundary.Index + potentialOverriddenMatch.Boundary.Length)
                            <= (matchRecord.Boundary.Index + matchRecord.Boundary.Length))
                    {
                        removes.Add(potentialOverriddenMatch);
                    }
                }
            }
           
        }

        // Remove overriden rules
        resultsList.RemoveAll(x => removes.Contains(x));

        return resultsList;
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
    private string ExtractTextSample(string fileText, int index, int length)
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
    private static string ExtractExcerpt(TextContainer text, Location start, Location end, Boundary matchBoundary, int context = 3)
    {
        if (context == 0)
        {
            return string.Empty;
        }

        int startLineNumber =
            start.Line < 0 ? 0 : start.Line > text.LineEnds.Count ? text.LineEnds.Count - 1 : start.Line;
        int endLineNUmber =
            end.Line < 0 ? 0 : end.Line > text.LineEnds.Count ? text.LineEnds.Count - 1 : end.Line;
        // First we try to include the number of lines of context requested
        var excerptStartLine = Math.Max(0, startLineNumber - context);
        var excerptEndLine = Math.Min(text.LineEnds.Count - 1, endLineNUmber + context);
        var startIndex = text.LineStarts[excerptStartLine];
        var endIndex = text.LineEnds[excerptEndLine] + 1;
        // Maximum number of characters to capture on each side
        var maxCharacterContext = context * 100;
        // If the number of characters captured for context is larger than 100*number of lines,
        //  instead gather an appropriate number of characters
        if (endIndex - startIndex - matchBoundary.Length > maxCharacterContext * 2)
        {
            startIndex = Math.Max(0, matchBoundary.Index - maxCharacterContext);
            endIndex = Math.Max(0, matchBoundary.Index + matchBoundary.Length + maxCharacterContext);
        }

        return text.FullContent[startIndex..endIndex];
    }
}