// Copyright (C) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Microsoft.ApplicationInspector.RulesEngine
{
    using Microsoft.ApplicationInspector.Common;
    using Microsoft.CST.OAT;
    using Microsoft.CST.RecursiveExtractor;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public class RuleProcessorOptions
    {
        public RuleProcessorOptions()
        {
        }
        public bool Parallel = true;
        public Confidence confidenceFilter = Confidence.Unspecified | Confidence.Low | Confidence.Medium | Confidence.High;
        public ILoggerFactory? loggerFactory;
        public bool allowAllTagsInBuildFiles = false;

        [Obsolete("Use allowAllTagsInBuildFiles")]
        public bool treatEverythingAsCode => allowAllTagsInBuildFiles;
    }

    /// <summary>
    /// Heart of RulesEngine. Parses code applies rules
    /// </summary>
    public class RuleProcessor
    {
        private readonly int MAX_TEXT_SAMPLE_LENGTH = 200;//char bytes

        private readonly RuleProcessorOptions _opts;
        private readonly ILogger<RuleProcessor> _logger;

        private Confidence ConfidenceLevelFilter => _opts.confidenceFilter;

        private readonly Analyzer analyzer;
        private readonly RuleSet _ruleset;
        private readonly ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>> _fileRulesCache = new();
        private readonly ConcurrentDictionary<string, IEnumerable<ConvertedOatRule>> _languageRulesCache = new();
        private IEnumerable<ConvertedOatRule>? _universalRulesCache;

        /// <summary>
        /// Sets severity levels for analysis
        /// </summary>
        private Severity SeverityLevel { get; }

        /// <summary>
        /// Enables caching of rules queries if multiple reuses per instance
        /// </summary>
        private bool EnableCache { get; }

        /// <summary>
        /// Creates instance of RuleProcessor
        /// </summary>
        public RuleProcessor(RuleSet rules, RuleProcessorOptions opts)
        {
            _opts = opts;
            _logger = opts.loggerFactory?.CreateLogger<RuleProcessor>() ?? NullLogger<RuleProcessor>.Instance;

            _ruleset = rules;
            EnableCache = true;
            SeverityLevel = Severity.Critical | Severity.Important | Severity.Moderate | Severity.BestPractice; //finds all; arg not currently supported

            analyzer = new Analyzer(new AnalyzerOptions(false, opts.Parallel));
            analyzer.SetOperation(new WithinOperation(analyzer));
            analyzer.SetOperation(new OATRegexWithIndexOperation(analyzer));
            analyzer.SetOperation(new OATSubstringIndexOperation(analyzer));
        }

        private static string ExtractDependency(TextContainer? text, int startIndex, string? pattern, string? language)
        {
            if (text is null || string.IsNullOrEmpty(text.FullContent) || string.IsNullOrEmpty(language) || string.IsNullOrEmpty(pattern))
            {
                return string.Empty;
            }

            string rawResult = string.Empty;
            int endIndex = text.FullContent.IndexOfAny(new char[] { '\n', '\r' }, startIndex);
            if (-1 != startIndex && -1 != endIndex)
            {
                rawResult = text.FullContent[startIndex..endIndex].Trim();
                Regex regex = new(pattern ?? string.Empty);
                MatchCollection matches = regex.Matches(rawResult);

                //remove surrounding import or trailing comments
                if (matches?.Any() == true)
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
            var rules = GetRulesForFile(languageInfo, fileEntry, tagsToIgnore);
            List<MatchRecord> resultsList = new();

            TextContainer textContainer = new(contents, languageInfo.Name);
            var caps = analyzer.GetCaptures(rules, textContainer);
            foreach (var ruleCapture in caps)
            {
                foreach (var cap in ruleCapture.Captures)
                {
                    resultsList.AddRange(ProcessBoundary(cap));
                }

                List<MatchRecord> ProcessBoundary(ClauseCapture cap)
                {
                    List<MatchRecord> newMatches = new();//matches for this rule clause only

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
                                    if (!_opts.allowAllTagsInBuildFiles && languageInfo.Type == LanguageInfo.LangFileType.Build && (oatRule.AppInspectorRule.Tags?.Any(v => !v.Contains("Metadata")) ?? false))
                                    {
                                        continue;
                                    }

                                    if (!ConfidenceLevelFilter.HasFlag(oatRule.AppInspectorRule.Patterns[patternIndex].Confidence))
                                    {
                                        continue;
                                    }

                                    Location StartLocation = textContainer.GetLocation(boundary.Index);
                                    Location EndLocation = textContainer.GetLocation(boundary.Index + boundary.Length);
                                    MatchRecord newMatch = new(oatRule.AppInspectorRule)
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

            List<MatchRecord> removes = new();

            foreach (MatchRecord m in resultsList.Where(x => x.Rule.Overrides?.Length > 0))
            {
                foreach (string ovrd in m.Rule?.Overrides ?? Array.Empty<string>())
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

            // Remove overriden rules
            resultsList.RemoveAll(x => removes.Contains(x));

            return resultsList;
        }

        private IEnumerable<ConvertedOatRule> GetRulesForFile(LanguageInfo languageInfo, FileEntry fileEntry, IEnumerable<string>? tagsToIgnore)
        {
            var rulesByLanguage = GetRulesByLanguage(languageInfo.Name).Where(x => !x.AppInspectorRule.Disabled && SeverityLevel.HasFlag(x.AppInspectorRule.Severity));
            var rules = rulesByLanguage.Union(GetRulesByFileName(fileEntry.FullPath).Where(x => !x.AppInspectorRule.Disabled && SeverityLevel.HasFlag(x.AppInspectorRule.Severity)));
            rules = rules.Union(GetUniversalRules());
            if (tagsToIgnore?.Any() == true)
            {
                rules = rules.Where(x => x.AppInspectorRule?.Tags?.Any(y => !tagsToIgnore.Contains(y)) ?? false);
            }
            return rules;
        }

        public List<MatchRecord> AnalyzeFile(FileEntry fileEntry, LanguageInfo languageInfo, IEnumerable<string>? tagsToIgnore = null, int numLinesContext = 3)
        {
            using var sr = new StreamReader(fileEntry.Content);
            var contents = string.Empty;
            try
            {
                contents = sr.ReadToEnd();
            }
            catch(Exception e)
            {
                _logger.LogDebug("Failed to analyze file {path}. {type}:{message}. ({stackTrace}), fileRecord.FileName", fileEntry.FullPath, e.GetType(), e.Message, e.StackTrace);
            }
            if (contents is not null)
            {
                return AnalyzeFile(contents, fileEntry, languageInfo, tagsToIgnore, numLinesContext);
            }
            return new List<MatchRecord>();
        }

        public async Task<List<MatchRecord>> AnalyzeFileAsync(FileEntry fileEntry, LanguageInfo languageInfo, CancellationToken cancellationToken, IEnumerable<string>? tagsToIgnore = null, int numLinesContext = 3)
        {
            var rules = GetRulesForFile(languageInfo, fileEntry, tagsToIgnore);

            List<MatchRecord> resultsList = new();

            using var sr = new StreamReader(fileEntry.Content);

            TextContainer textContainer = new(await sr.ReadToEndAsync().ConfigureAwait(false), languageInfo.Name);
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
                    List<MatchRecord> newMatches = new();//matches for this rule clause only

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
                                    if (!_opts.allowAllTagsInBuildFiles && languageInfo.Type == LanguageInfo.LangFileType.Build && (oatRule.AppInspectorRule.Tags?.Any(v => !v.Contains("Metadata")) ?? false))
                                    {
                                        continue;
                                    }

                                    if (patternIndex < 0 || patternIndex > oatRule.AppInspectorRule.Patterns.Length)
                                    {
                                        _logger.LogError("Index out of range for patterns for rule: {ruleName}", oatRule.AppInspectorRule.Name);
                                        continue;
                                    }

                                    if (!ConfidenceLevelFilter.HasFlag(oatRule.AppInspectorRule.Patterns[patternIndex].Confidence))
                                    {
                                        continue;
                                    }

                                    Location StartLocation = textContainer.GetLocation(boundary.Index);
                                    Location EndLocation = textContainer.GetLocation(boundary.Index + boundary.Length);
                                    MatchRecord newMatch = new(oatRule.AppInspectorRule)
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

            List<MatchRecord> removes = new();

            foreach (MatchRecord m in resultsList.Where(x => x.Rule.Overrides?.Length > 0))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return resultsList;
                }
                foreach (string ovrd in m.Rule?.Overrides ?? Array.Empty<string>())
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

            // Remove overriden rules
            resultsList.RemoveAll(x => removes.Contains(x));

            return resultsList;
        }

        
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

            if (EnableCache)
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

            if (EnableCache)
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
        private static string ExtractExcerpt(TextContainer text, int startLineNumber, int context = 3)
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
            var startIndex = text.LineStarts[excerptStartLine];
            var endIndex = text.LineEnds[excerptEndLine] + 1;
            var maxCharacterContext = context * 100;
            // Only gather 100*lines context characters to avoid gathering super long lines
            if (text.LineStarts[startLineNumber] - startIndex > maxCharacterContext)
            {
                startIndex = Math.Max(0, startIndex - maxCharacterContext);
            }
            if (endIndex - text.LineEnds[startLineNumber] > maxCharacterContext)
            {
                endIndex = Math.Min(text.FullContent.Length - 1, endIndex + maxCharacterContext);
            }
            return text.FullContent[startIndex..endIndex];
        }

            }
}
