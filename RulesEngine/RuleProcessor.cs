// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

using Microsoft.CST.OAT;
using Microsoft.CST.RecursiveExtractor;
using NLog;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInspector.RulesEngine
{
    public class RuleProcessorOptions
    {
        public RuleProcessorOptions()
        {

        }
        public bool Parallel = true;
        public Confidence confidenceFilter;
        public Logger? logger;
        public bool treatEverythingAsCode = false;
    }

    /// <summary>
    /// Heart of RulesEngine. Parses code applies rules
    /// </summary>
    public class RuleProcessor
    {
        private readonly int MAX_TEXT_SAMPLE_LENGTH = 200;//char bytes

        private Confidence ConfidenceLevelFilter { get; set; }
        private readonly Logger? _logger;
        private readonly bool _treatEverythingAsCode;
        private readonly Analyzer analyzer;
        private readonly RuleSet _ruleset;
        private readonly ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>> _fileRulesCache = new ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>>();
        private readonly ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>> _languageRulesCache = new ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>>();
        private IEnumerable<ConvertedOatRule>? _universalRulesCache = null;

        /// <summary>
        /// Sets severity levels for analysis
        /// </summary>
        private Severity SeverityLevel { get; set; }

        /// <summary>
        /// Enables caching of rules queries if multiple reuses per instance
        /// </summary>
        private bool EnableCache { get; set; }

        /// <summary>
        /// Creates instance of RuleProcessor
        /// </summary>
        public RuleProcessor(RuleSet rules, RuleProcessorOptions opts)
        {
            _ruleset = rules;
            EnableCache = true;
            _logger = opts.logger;
            _treatEverythingAsCode = opts.treatEverythingAsCode;
            ConfidenceLevelFilter = opts.confidenceFilter;
            SeverityLevel = Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice; //finds all; arg not currently supported

            analyzer = new Analyzer(new AnalyzerOptions(false, opts.Parallel));
            analyzer.SetOperation(new WithinOperation(analyzer));
            analyzer.SetOperation(new OATRegexWithIndexOperation(analyzer));
            analyzer.SetOperation(new OATSubstringIndexOperation(analyzer));
        }


        private string ExtractDependency(TextContainer? text, int startIndex, string? pattern, string? language)
        {
            if (text is null || string.IsNullOrEmpty(text.FullContent) || string.IsNullOrEmpty(language) || string.IsNullOrEmpty(pattern))
            {
                return string.Empty;
            }

            string rawResult = string.Empty;
            int endIndex = text.FullContent.IndexOfAny(new char[] { '\n', '\r' }, startIndex);
            if (-1 != startIndex && -1 != endIndex)
            {
                rawResult = text.FullContent.Substring(startIndex, endIndex - startIndex).Trim();
                Regex regex = new Regex(pattern ?? string.Empty);
                MatchCollection matches = regex.Matches(rawResult);

                //remove surrounding import or trailing comments
                if (matches != null && matches.Any())
                {
                    foreach (Match? match in matches)
                    {
                        if (match?.Groups.Count == 1)//handles cases like "using Newtonsoft.Json"
                        {
                            string[] parseValues = match.Groups[0].Value.Split(' ');
                            if (parseValues.Length == 1)
                            {
                                rawResult = parseValues[0].Trim();
                            }
                            else if (parseValues.Length > 1)
                            {
                                rawResult = parseValues[1].Trim(); //should be value; time will tell if fullproof
                            }
                        }
                        else if (match?.Groups.Count > 1)//handles cases like include <stdio.h>
                        {
                            rawResult = match.Groups[1].Value.Trim();
                        }
                        //else if > 2 too hard to match; do nothing

                        break;//only designed to expect one match per line i.e. not include value include value
                    }
                }

                string finalResult = rawResult.Replace(";", "");

                return System.Net.WebUtility.HtmlEncode(finalResult);
            }

            return rawResult;
        }

        public List<MatchRecord> AnalyzeFile(string contents, FileEntry fileEntry, LanguageInfo languageInfo, IEnumerable<string>? tagsToIgnore = null, int numLinesContext = 3)
        {
            var rulesByLanguage = GetRulesByLanguage(languageInfo.Name).Where(x => !x.AppInspectorRule.Disabled && SeverityLevel.HasFlag(x.AppInspectorRule.Severity));
            var rules = rulesByLanguage.Union(GetRulesByFileName(fileEntry.FullPath).Where(x => !x.AppInspectorRule.Disabled && SeverityLevel.HasFlag(x.AppInspectorRule.Severity)));
            rules = rules.Union(GetUniversalRules());
            if (tagsToIgnore is not null && tagsToIgnore.Any())
            {
                rules = rules.Where(x => x.Tags.Any(y => !tagsToIgnore.Contains(y)));
            }
            List<MatchRecord> resultsList = new List<MatchRecord>();

            TextContainer textContainer = new TextContainer(contents, languageInfo.Name);

            foreach (var ruleCapture in analyzer.GetCaptures(rules, textContainer))
            {
                foreach (var cap in ruleCapture.Captures)
                {
                    resultsList.AddRange(ProcessBoundary(cap));
                }

                List<MatchRecord> ProcessBoundary(ClauseCapture cap)
                {
                    List<MatchRecord> newMatches = new List<MatchRecord>();//matches for this rule clause only

                    if (cap is TypedClauseCapture<List<(int, Boundary)>> tcc)
                    {
                        if (ruleCapture.Rule is ConvertedOatRule oatRule)
                        {
                            if (tcc?.Result is List<(int, Boundary)> captureResults)
                            {
                                foreach (var match in captureResults)
                                {
                                    var patternIndex = match.Item1;
                                    var boundary = match.Item2;

                                    //restrict adds from build files to tags with "metadata" only to avoid false feature positives that are not part of executable code
                                    if (!_treatEverythingAsCode && languageInfo.Type == LanguageInfo.LangFileType.Build && (oatRule.AppInspectorRule.Tags?.Any(v => !v.Contains("Metadata")) ?? false))
                                    {
                                        continue;
                                    }

                                    if (patternIndex < 0 || patternIndex > oatRule.AppInspectorRule.Patterns.Length)
                                    {
                                        _logger?.Error("Index out of range for patterns for rule: " + oatRule.AppInspectorRule.Name);
                                        continue;
                                    }

                                    if (!ConfidenceLevelFilter.HasFlag(oatRule.AppInspectorRule.Patterns[patternIndex].Confidence))
                                    {
                                        continue;
                                    }

                                    Location StartLocation = textContainer.GetLocation(boundary.Index);
                                    Location EndLocation = textContainer.GetLocation(boundary.Index + boundary.Length);
                                    MatchRecord newMatch = new MatchRecord(oatRule.AppInspectorRule)
                                    {
                                        FileName = fileEntry.FullPath,
                                        FullTextContainer = textContainer,
                                        LanguageInfo = languageInfo,
                                        Boundary = boundary,
                                        StartLocationLine = StartLocation.Line,
                                        StartLocationColumn = StartLocation.Column,
                                        EndLocationLine = EndLocation.Line != 0 ? EndLocation.Line : StartLocation.Line + 1, //match is on last line
                                        EndLocationColumn = EndLocation.Column,
                                        MatchingPattern = oatRule.AppInspectorRule.Patterns[patternIndex],
                                        Excerpt = numLinesContext > 0 ? ExtractExcerpt(textContainer, StartLocation.Line, numLinesContext) : string.Empty,
                                        Sample = numLinesContext > -1 ? ExtractTextSample(textContainer.FullContent, boundary.Index, boundary.Length) : string.Empty
                                    };

                                    if (oatRule.AppInspectorRule.Tags?.Contains("Dependency.SourceInclude") ?? false)
                                    {
                                        newMatch.Sample = ExtractDependency(newMatch.FullTextContainer, newMatch.Boundary.Index, newMatch.Pattern, newMatch.LanguageInfo.Name);
                                    }

                                    newMatches.Add(newMatch);
                                }
                            }
                        }
                    }
                    return newMatches;
                }
            }

            List<MatchRecord> removes = new List<MatchRecord>();

            foreach (MatchRecord m in resultsList.Where(x => x.Rule.Overrides != null && x.Rule.Overrides.Length > 0))
            {
                if (m.Rule.Overrides != null && m.Rule.Overrides.Length > 0)
                {
                    foreach (string ovrd in m.Rule.Overrides)
                    {
                        // Find all overriden rules and mark them for removal from issues list
                        foreach (MatchRecord om in resultsList.FindAll(x => x.Rule.Id == ovrd))
                        {
                            if (om.Boundary?.Index >= m.Boundary?.Index &&
                                om.Boundary?.Index <= m.Boundary?.Index + m.Boundary?.Length)
                            {
                                removes.Add(om);
                            }
                        }
                    }
                }
            }

            // Remove overriden rules
            resultsList.RemoveAll(x => removes.Contains(x));

            return resultsList;
        }

        public List<MatchRecord> AnalyzeFile(FileEntry fileEntry, LanguageInfo languageInfo, IEnumerable<string>? tagsToIgnore = null, int numLinesContext = 3)
        {
            using var sr = new StreamReader(fileEntry.Content);
            return AnalyzeFile(sr.ReadToEnd(), fileEntry, languageInfo, tagsToIgnore, numLinesContext);
        }

        public async Task<List<MatchRecord>> AnalyzeFileAsync(FileEntry fileEntry, LanguageInfo languageInfo, CancellationToken cancellationToken, IEnumerable<string>? tagsToIgnore = null, int numLinesContext = 3)
        {
            var rulesByLanguage = GetRulesByLanguage(languageInfo.Name).Where(x => !x.AppInspectorRule.Disabled && SeverityLevel.HasFlag(x.AppInspectorRule.Severity));
            var rules = rulesByLanguage.Union(GetRulesByFileName(fileEntry.FullPath).Where(x => !x.AppInspectorRule.Disabled && SeverityLevel.HasFlag(x.AppInspectorRule.Severity)));
            rules = rules.Union(GetUniversalRules());

            if (tagsToIgnore is not null && tagsToIgnore.Any())
            {
                rules = rules.Where(x => x.Tags.Any(y => !tagsToIgnore.Contains(y)));
            }

            List<MatchRecord> resultsList = new List<MatchRecord>();

            using var sr = new StreamReader(fileEntry.Content);

            TextContainer textContainer = new TextContainer(await sr.ReadToEndAsync(), languageInfo.Name);

            foreach (var ruleCapture in analyzer.GetCaptures(rules, textContainer))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return resultsList;
                }
                foreach (var cap in ruleCapture.Captures)
                {
                    resultsList.AddRange(ProcessBoundary(cap));
                }

                List<MatchRecord> ProcessBoundary(ClauseCapture cap)
                {
                    List<MatchRecord> newMatches = new List<MatchRecord>();//matches for this rule clause only

                    if (cap is TypedClauseCapture<List<(int, Boundary)>> tcc)
                    {
                        if (ruleCapture.Rule is ConvertedOatRule oatRule)
                        {
                            if (tcc?.Result is List<(int, Boundary)> captureResults)
                            {
                                foreach (var match in captureResults)
                                {
                                    var patternIndex = match.Item1;
                                    var boundary = match.Item2;

                                    //restrict adds from build files to tags with "metadata" only to avoid false feature positives that are not part of executable code
                                    if (!_treatEverythingAsCode && languageInfo.Type == LanguageInfo.LangFileType.Build && (oatRule.AppInspectorRule.Tags?.Any(v => !v.Contains("Metadata")) ?? false))
                                    {
                                        continue;
                                    }

                                    if (patternIndex < 0 || patternIndex > oatRule.AppInspectorRule.Patterns.Length)
                                    {
                                        _logger?.Error("Index out of range for patterns for rule: " + oatRule.AppInspectorRule.Name);
                                        continue;
                                    }

                                    if (!ConfidenceLevelFilter.HasFlag(oatRule.AppInspectorRule.Patterns[patternIndex].Confidence))
                                    {
                                        continue;
                                    }

                                    Location StartLocation = textContainer.GetLocation(boundary.Index);
                                    Location EndLocation = textContainer.GetLocation(boundary.Index + boundary.Length);
                                    MatchRecord newMatch = new MatchRecord(oatRule.AppInspectorRule)
                                    {
                                        FileName = fileEntry.FullPath,
                                        FullTextContainer = textContainer,
                                        LanguageInfo = languageInfo,
                                        Boundary = boundary,
                                        StartLocationLine = StartLocation.Line,
                                        EndLocationLine = EndLocation.Line != 0 ? EndLocation.Line : StartLocation.Line + 1, //match is on last line
                                        MatchingPattern = oatRule.AppInspectorRule.Patterns[patternIndex],
                                        Excerpt = numLinesContext > 0 ? ExtractExcerpt(textContainer, StartLocation.Line, numLinesContext) : string.Empty,
                                        Sample = numLinesContext > -1 ? ExtractTextSample(textContainer.FullContent, boundary.Index, boundary.Length) : string.Empty
                                    };

                                    if (oatRule.AppInspectorRule.Tags?.Contains("Dependency.SourceInclude") ?? false)
                                    {
                                        newMatch.Sample = ExtractDependency(newMatch.FullTextContainer, newMatch.Boundary.Index, newMatch.Pattern, newMatch.LanguageInfo.Name);
                                    }

                                    newMatches.Add(newMatch);
                                }
                            }
                        }
                    }
                    return newMatches;
                }
            }

            List<MatchRecord> removes = new List<MatchRecord>();

            foreach (MatchRecord m in resultsList.Where(x => x.Rule.Overrides != null && x.Rule.Overrides.Length > 0))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return resultsList;
                }
                if (m.Rule.Overrides != null && m.Rule.Overrides.Length > 0)
                {
                    foreach (string ovrd in m.Rule.Overrides)
                    {
                        // Find all overriden rules and mark them for removal from issues list
                        foreach (MatchRecord om in resultsList.FindAll(x => x.Rule.Id == ovrd))
                        {
                            if (om.Boundary?.Index >= m.Boundary?.Index &&
                                om.Boundary?.Index <= m.Boundary?.Index + m.Boundary?.Length)
                            {
                                removes.Add(om);
                            }
                        }
                    }
                }
            }

            // Remove overriden rules
            resultsList.RemoveAll(x => removes.Contains(x));

            return resultsList;
        }

        #region Private Support Methods

        /// <summary>
        ///     Filters the rules for those matching the content type. Resolves all the overrides
        /// </summary>
        /// <param name="languages"> Languages to filter rules for </param>
        /// <returns> List of rules </returns>
        private IEnumerable<ConvertedOatRule> GetRulesByLanguage(string input)
        {
            if (EnableCache)
            {
                if (_languageRulesCache.ContainsKey(input))
                    return _languageRulesCache[input];
            }

            IEnumerable<ConvertedOatRule> filteredRules = _ruleset.ByLanguage(input);

            if (EnableCache && filteredRules.Any())
            {
                _languageRulesCache.TryAdd(input, filteredRules);
            }

            return filteredRules;
        }

        /// <summary>
        ///     Filters the rules for those matching the content type. Resolves all the overrides
        /// </summary>
        /// <param name="languages"> Languages to filter rules for </param>
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
        ///     Filters the rules for those matching the content type. Resolves all the overrides
        /// </summary>
        /// <param name="languages"> Languages to filter rules for </param>
        /// <returns> List of rules </returns>
        private IEnumerable<ConvertedOatRule> GetRulesByFileName(string input)
        {
            if (EnableCache)
            {
                if (_fileRulesCache.ContainsKey(input))
                    return _fileRulesCache[input];
            }

            IEnumerable<ConvertedOatRule> filteredRules = _ruleset.ByFilename(input);

            if (EnableCache && filteredRules.Any())
            {
                _fileRulesCache.TryAdd(input, filteredRules);
            }

            return filteredRules;
        }

        /// <summary>
        /// Simple wrapper but keeps calling code consistent
        /// Do not html code result which is accomplished later before out put to report
        /// </summary>
        private string ExtractTextSample(string fileText, int index, int length)
        {
            if (index < 0 || length < 0) { return fileText; }

            length = Math.Min(Math.Min(length, MAX_TEXT_SAMPLE_LENGTH), fileText.Length - index);

            if (length == 0) { return string.Empty; }

            return fileText[index..(index + length)].Trim();
        }

        /// <summary>
        /// Located here to include during Match creation to avoid a call later or putting in constructor
        /// Needed in match ensuring value exists at time of report writing rather than expecting a callback
        /// from the template
        /// </summary>
        /// <returns></returns>
        private string ExtractExcerpt(TextContainer text, int startLineNumber, int context = 3)
        {
            if (context == 0)
            {
                return string.Empty;
            }
            if (startLineNumber < 0)
            {
                startLineNumber = 0;
            }

            if (startLineNumber >= text.LineEnds.Count)
            {
                startLineNumber = text.LineEnds.Count - 1;
            }

            var excerptStartLine = Math.Max(0, startLineNumber - context);
            var excerptEndLine = Math.Min(text.LineEnds.Count - 1, startLineNumber + context);

            return text.FullContent[text.LineStarts[excerptStartLine]..(text.LineEnds[excerptEndLine]+1)];
        }

        #endregion Private Methods
    }
}
